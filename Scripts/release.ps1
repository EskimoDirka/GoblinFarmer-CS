param(
    [string]$Version,
    [string]$Message,
    [string]$Remote = "origin",
    [string]$Branch,
    [string]$Runtime = "win-x64",
    [switch]$SkipGitHubRelease,
    [switch]$SkipInstaller
)

# Full local release helper. It assumes docs/source edits are already made, then
# verifies, publishes, commits/pushes, tags, and uploads GitHub release assets
# when the GitHub CLI is installed and authenticated.

$ErrorActionPreference = "Stop"

function Stop-Release {
    param([string]$Text)

    Write-Error $Text
    exit 1
}

function Invoke-ReleaseStep {
    param(
        [string]$Description,
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "==> $Description"
    & $Command
    if ($LASTEXITCODE -ne 0) {
        Stop-Release "$Description failed."
    }
}

function Get-ProjectVersion {
    param([string]$ProjectPath)

    [xml]$projectXml = Get-Content -LiteralPath $ProjectPath
    $version = @($projectXml.Project.PropertyGroup) |
        ForEach-Object { $_.Version } |
        Where-Object { ![string]::IsNullOrWhiteSpace($_) } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($version)) {
        Stop-Release "Version metadata is missing in GoblinFarmer.csproj."
    }

    return $version
}

function Get-ReleaseAssetPath {
    param(
        [string]$Root,
        [string]$ProjectVersion
    )

    $installerPath = Join-Path $Root "artifacts\installer\GoblinFarmerSetup-$ProjectVersion.exe"
    if (Test-Path -LiteralPath $installerPath) {
        return $installerPath
    }

    return $null
}

function Get-GitHubCliPath {
    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if ($null -ne $gh) {
        return $gh.Source
    }

    $candidatePaths = @(
        "$env:ProgramFiles\GitHub CLI\gh.exe",
        "${env:ProgramFiles(x86)}\GitHub CLI\gh.exe",
        "$env:LOCALAPPDATA\Programs\GitHub CLI\gh.exe",
        "$env:USERPROFILE\scoop\apps\gh\current\gh.exe",
        "$env:ProgramData\chocolatey\bin\gh.exe"
    )

    foreach ($candidatePath in $candidatePaths) {
        if (![string]::IsNullOrWhiteSpace($candidatePath) -and (Test-Path -LiteralPath $candidatePath)) {
            return $candidatePath
        }
    }

    return $null
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "GoblinFarmer.csproj"
$publishScript = Join-Path $PSScriptRoot "publish-release.ps1"
$releaseNotesPath = Join-Path $repoRoot "Docs\Release_v1.3.md"

if (!(Test-Path -LiteralPath $projectPath)) {
    Stop-Release "Project file not found: $projectPath"
}

if (!(Test-Path -LiteralPath $publishScript)) {
    Stop-Release "Publish script not found: $publishScript"
}

$projectVersion = Get-ProjectVersion -ProjectPath $projectPath
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = "v$projectVersion"
}

if ($Version -notmatch "^v") {
    $Version = "v$Version"
}

if ([string]::IsNullOrWhiteSpace($Message)) {
    $Message = "Release $Version"
}

Set-Location $repoRoot

if ([string]::IsNullOrWhiteSpace($Branch)) {
    $Branch = (git branch --show-current).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($Branch)) {
        Stop-Release "Could not determine the current git branch."
    }
}

Write-Host "GoblinFarmer release"
Write-Host "Repository: $repoRoot"
Write-Host "Version:    $Version"
Write-Host "Project:    $projectVersion"
Write-Host "Branch:     $Branch"
Write-Host "Runtime:    $Runtime"

Invoke-ReleaseStep "dotnet build" {
    dotnet build $projectPath
}

$publishArgs = @(
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-File", $publishScript,
    "-Version", $projectVersion,
    "-Runtime", $Runtime
)
if ($SkipInstaller) {
    $publishArgs += "-SkipInstaller"
}

Invoke-ReleaseStep "publish Release output" {
    & powershell @publishArgs
}

$publishDir = Join-Path $repoRoot "artifacts\publish\GoblinFarmer"
$portableZip = Join-Path $repoRoot "artifacts\GoblinFarmer-$projectVersion-$Runtime-portable.zip"
if (Test-Path -LiteralPath $portableZip) {
    Remove-Item -LiteralPath $portableZip -Force
}

Invoke-ReleaseStep "create portable zip" {
    Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $portableZip -CompressionLevel Optimal
}

Write-Host ""
Write-Host "==> git status"
git status --short
if ($LASTEXITCODE -ne 0) {
    Stop-Release "git status failed."
}

$changes = git status --porcelain
if (![string]::IsNullOrWhiteSpace($changes)) {
    Invoke-ReleaseStep "git add -A" {
        git add -A
    }

    $stagedChanges = git diff --cached --name-only
    if (![string]::IsNullOrWhiteSpace($stagedChanges)) {
        Invoke-ReleaseStep "git commit" {
            git commit -m $Message
        }
    }
    else {
        Write-Host "No staged changes found after git add."
    }
}
else {
    Write-Host "Working tree is clean; skipping commit."
}

Invoke-ReleaseStep "git push $Remote $Branch" {
    git push $Remote $Branch
}

$existingTag = git tag --list $Version
if ([string]::IsNullOrWhiteSpace($existingTag)) {
    Invoke-ReleaseStep "git tag $Version" {
        git tag $Version
    }
}
else {
    Write-Host "Tag $Version already exists locally; keeping it."
}

Invoke-ReleaseStep "git push $Remote $Version" {
    git push $Remote $Version
}

$installerPath = Get-ReleaseAssetPath -Root $repoRoot -ProjectVersion $projectVersion
$releaseAssets = @()
if ($null -ne $installerPath) {
    $releaseAssets += $installerPath
}
$releaseAssets += $portableZip

if ($SkipGitHubRelease) {
    Write-Warning "Skipping GitHub release upload because -SkipGitHubRelease was supplied."
}
else {
    $ghPath = Get-GitHubCliPath
    if ($null -eq $ghPath) {
        Stop-Release "GitHub CLI is not installed or discoverable. Install it with 'winget install GitHub.cli', run 'gh auth login', then rerun this script to upload release assets."
    }

    Invoke-ReleaseStep "gh auth status" {
        & $ghPath auth status
    }

    $releaseView = & $ghPath release view $Version 2>$null
    if ($LASTEXITCODE -eq 0) {
        foreach ($assetPath in $releaseAssets) {
            Invoke-ReleaseStep "upload $(Split-Path -Leaf $assetPath)" {
                & $ghPath release upload $Version $assetPath --clobber
            }
        }
    }
    else {
        $releaseArgs = @("release", "create", $Version, "--title", $Version)
        if (Test-Path -LiteralPath $releaseNotesPath) {
            $releaseArgs += @("--notes-file", $releaseNotesPath)
        }
        else {
            $releaseArgs += @("--notes", $Message)
        }
        $releaseArgs += $releaseAssets

        Invoke-ReleaseStep "create GitHub release $Version" {
            & $ghPath @releaseArgs
        }
    }
}

Write-Host ""
Write-Host "========== Release Summary =========="
Write-Host "Commit:       $(git rev-parse --short HEAD)"
Write-Host "Tag:          $Version"
Write-Host "Publish dir:  $publishDir"
Write-Host "Portable zip: $portableZip"
if ($null -ne $installerPath) {
    Write-Host "Installer:    $installerPath"
}
else {
    Write-Warning "Installer was not found. Publish output and portable zip were created."
}
Write-Host "Release flow complete."

param(
    [string]$Version,
    [string]$Message,
    [string]$Remote = "origin",
    [string]$Branch,
    [string]$Runtime = "win-x64",
    [string]$UserInstallDir = "E:\GoblinFarmer",
    [string]$GitHubUploadDir = "GitHub Upload",
    [switch]$SkipUserExeRefresh,
    [switch]$SkipGitHubExeUpload,
    [switch]$CreateGitHubRelease,
    [switch]$SkipInstaller
)

# Routine GitHub sync helper. It assumes docs/source edits are already made,
# then verifies, publishes, refreshes the user executable, stages the latest
# published EXE for GitHub, commits, and pushes.
# GitHub Releases are intentionally opt-in for larger app milestones.

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

function Test-IsPathInside {
    param(
        [string]$ChildPath,
        [string]$ParentPath
    )

    $childFull = [System.IO.Path]::GetFullPath($ChildPath).TrimEnd('\')
    $parentFull = [System.IO.Path]::GetFullPath($ParentPath).TrimEnd('\')
    return $childFull.Equals($parentFull, [System.StringComparison]::OrdinalIgnoreCase) -or
        $childFull.StartsWith("$parentFull\", [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-ProcessRunningFromPath {
    param([string]$Path)

    $resolvedPath = [System.IO.Path]::GetFullPath($Path).TrimEnd('\')
    foreach ($process in Get-Process -Name "GoblinFarmer" -ErrorAction SilentlyContinue) {
        try {
            $processPath = $process.MainModule.FileName
            if (![string]::IsNullOrWhiteSpace($processPath) -and (Test-IsPathInside -ChildPath $processPath -ParentPath $resolvedPath)) {
                return $true
            }
        }
        catch {
            return $true
        }
    }

    return $false
}

function Sync-UserExecutable {
    param(
        [string]$SourceDir,
        [string]$DestinationDir
    )

    $sourceFull = [System.IO.Path]::GetFullPath($SourceDir)
    $destinationFull = [System.IO.Path]::GetFullPath($DestinationDir)
    $driveRoot = [System.IO.Path]::GetPathRoot($destinationFull)

    if (!$destinationFull.StartsWith("E:\", [System.StringComparison]::OrdinalIgnoreCase)) {
        Stop-Release "Refusing to refresh user executable outside E:\. Destination was: $destinationFull"
    }

    if ($destinationFull.TrimEnd('\').Equals($driveRoot.TrimEnd('\'), [System.StringComparison]::OrdinalIgnoreCase)) {
        Stop-Release "Refusing to refresh a drive root: $destinationFull"
    }

    if (Test-IsPathInside -ChildPath $destinationFull -ParentPath $repoRoot) {
        Stop-Release "Refusing to refresh user executable inside the repository: $destinationFull"
    }

    if (Test-ProcessRunningFromPath -Path $destinationFull) {
        Stop-Release "GoblinFarmer appears to be running from $destinationFull. Close it before refreshing the user executable."
    }

    New-Item -ItemType Directory -Force -Path $destinationFull | Out-Null

    $preserveNames = @(
        "Config",
        "Logs",
        "DebugPackages",
        "Sessions",
        "Screenshots",
        "debug-screenshots",
        "ScanRegions.json",
        "session-info.txt",
        "route-failure-summary.txt",
        "debug-package-manifest.txt",
        "debug-screenshot-manifest.txt"
    )

    $preserveSet = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($name in $preserveNames) {
        [void]$preserveSet.Add($name)
    }

    Get-ChildItem -LiteralPath $destinationFull -Force | ForEach-Object {
        if (!$preserveSet.Contains($_.Name)) {
            Remove-Item -LiteralPath $_.FullName -Recurse -Force
        }
    }

    Copy-Item -Path (Join-Path $sourceFull "*") -Destination $destinationFull -Recurse -Force
}

function Sync-GitHubExecutable {
    param(
        [string]$SourceExe,
        [string]$DestinationDir
    )

    $destinationFull = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $DestinationDir))
    if (!(Test-IsPathInside -ChildPath $destinationFull -ParentPath $repoRoot)) {
        Stop-Release "Refusing to stage GitHub executable outside the repository: $destinationFull"
    }

    New-Item -ItemType Directory -Force -Path $destinationFull | Out-Null
    Copy-Item -LiteralPath $SourceExe -Destination (Join-Path $destinationFull "GoblinFarmer.exe") -Force
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
    $Message = "Update GoblinFarmer"
}

Set-Location $repoRoot

if ([string]::IsNullOrWhiteSpace($Branch)) {
    $Branch = (git branch --show-current).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($Branch)) {
        Stop-Release "Could not determine the current git branch."
    }
}

Write-Host "GoblinFarmer GitHub Sync"
Write-Host "Repository: $repoRoot"
Write-Host "Version:    $Version"
Write-Host "Project:    $projectVersion"
Write-Host "Branch:     $Branch"
Write-Host "Runtime:    $Runtime"
Write-Host "User exe:   $UserInstallDir"
Write-Host "GitHub exe: $GitHubUploadDir\GoblinFarmer.exe"

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
$publishedExe = Join-Path $publishDir "GoblinFarmer.exe"
$portableZip = Join-Path $repoRoot "artifacts\GoblinFarmer-$projectVersion-$Runtime-portable.zip"
if (Test-Path -LiteralPath $portableZip) {
    Remove-Item -LiteralPath $portableZip -Force
}

Invoke-ReleaseStep "create portable zip" {
    Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $portableZip -CompressionLevel Optimal
}

if ($SkipUserExeRefresh) {
    Write-Warning "Skipping user executable refresh because -SkipUserExeRefresh was supplied."
}
else {
    Invoke-ReleaseStep "refresh user executable" {
        Sync-UserExecutable -SourceDir $publishDir -DestinationDir $UserInstallDir
    }
}

if ($SkipGitHubExeUpload) {
    Write-Warning "Skipping tracked GitHub executable update because -SkipGitHubExeUpload was supplied."
}
else {
    Invoke-ReleaseStep "stage latest exe for GitHub" {
        Sync-GitHubExecutable -SourceExe $publishedExe -DestinationDir $GitHubUploadDir
    }
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

$installerPath = Get-ReleaseAssetPath -Root $repoRoot -ProjectVersion $projectVersion
$releaseAssets = @()
if ($null -ne $installerPath) {
    $releaseAssets += $installerPath
}
$releaseAssets += $portableZip

if ($CreateGitHubRelease) {
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

    $ghPath = Get-GitHubCliPath
    if ($null -eq $ghPath) {
        Stop-Release "GitHub CLI is not installed or discoverable. Install it with 'winget install GitHub.cli', run 'gh auth login', then rerun this script to upload release assets."
    }

    Invoke-ReleaseStep "gh auth status" {
        & $ghPath auth status
    }

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & $ghPath release view $Version *> $null
        $releaseExists = $LASTEXITCODE -eq 0
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($releaseExists) {
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
else {
    Write-Host ""
    Write-Host "Skipping GitHub Release/tag creation. Use -CreateGitHubRelease only for larger app milestones."
}

Write-Host ""
Write-Host "========== GitHub Sync Summary =========="
Write-Host "Commit:       $(git rev-parse --short HEAD)"
Write-Host "Publish dir:  $publishDir"
if (!$SkipUserExeRefresh) {
    Write-Host "User exe:     $UserInstallDir"
}
if (!$SkipGitHubExeUpload) {
    Write-Host "GitHub exe:   $(Join-Path $repoRoot (Join-Path $GitHubUploadDir 'GoblinFarmer.exe'))"
}
Write-Host "Portable zip: $portableZip"
if ($null -ne $installerPath) {
    Write-Host "Installer:    $installerPath"
}
else {
    Write-Warning "Installer was not found. Publish output and portable zip were created."
}
if ($CreateGitHubRelease) {
    Write-Host "GitHub rel:   $Version"
}
Write-Host "GitHub sync complete."

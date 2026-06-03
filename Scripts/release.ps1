param(
    [string]$Version,
    [string]$Message
)

# Optional git/GitHub helper only. It does not publish binaries, build the Inno
# installer, or upload release assets. Use Docs/Release_Checklist.md for the
# full v1.3 release flow.

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

if ([string]::IsNullOrWhiteSpace($Version)) {
    Stop-Release "Version is required. Example: .\Scripts\release.ps1 -Version `"v1.3`" -Message `"Release v1.3`""
}

if ([string]::IsNullOrWhiteSpace($Message)) {
    Stop-Release "Message is required. Example: .\Scripts\release.ps1 -Version `"v1.3`" -Message `"Release v1.3`""
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

Write-Host "Repository: $repoRoot"
Write-Host ""
Write-Host "==> git status"
git status
if ($LASTEXITCODE -ne 0) {
    Stop-Release "git status failed."
}

$changes = git status --porcelain
if ([string]::IsNullOrWhiteSpace($changes)) {
    Write-Warning "Working directory has no changes. Commit may fail if there is nothing staged."
}

Invoke-ReleaseStep "git add ." {
    git add .
}

$stagedChanges = git diff --cached --name-only
if ([string]::IsNullOrWhiteSpace($stagedChanges)) {
    Write-Warning "No staged changes found after git add. Stopping before commit/tag/release."
    exit 0
}

$commitMessage = "$Version $Message"
Invoke-ReleaseStep "git commit" {
    git commit -m $commitMessage
}

Invoke-ReleaseStep "git push" {
    git push
}

Invoke-ReleaseStep "git tag $Version" {
    git tag $Version
}

Invoke-ReleaseStep "git push origin $Version" {
    git push origin $Version
}

$gh = Get-Command gh -ErrorAction SilentlyContinue
if ($null -eq $gh) {
    Write-Warning "GitHub CLI is not installed. Skipping GitHub release creation."
    Write-Host "Install it with:"
    Write-Host "winget install GitHub.cli"
    exit 0
}

$releaseNotesPath = Join-Path $repoRoot "CHANGELOG.md"
if (Test-Path $releaseNotesPath) {
    Invoke-ReleaseStep "gh release create $Version" {
        gh release create $Version --title $Version --notes-file $releaseNotesPath
    }
}
else {
    Invoke-ReleaseStep "gh release create $Version" {
        gh release create $Version --title $Version --notes $Message
    }
}

Write-Host ""
Write-Host "Release complete: $Version"

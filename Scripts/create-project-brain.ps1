$ErrorActionPreference = "Stop"

# Creates a lightweight docs-only Project Brain ZIP for AI-assisted development.
# The archive is intentionally allowlist-only so runtime artifacts, screenshots,
# images, binaries, replay data, and existing ZIPs cannot be included.

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$outputRoot = Join-Path $repoRoot "ProjectBrain"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$zipName = "GoblinFarmer_ProjectBrain_$timestamp.zip"
$zipPath = Join-Path $outputRoot $zipName
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) "GoblinFarmer_ProjectBrain_$timestamp"

$sourceFiles = @(
    "AGENTS.md",
    "README.md",
    "Docs\Project_Status.md",
    "Docs\Test_Results.md",
    "Docs\Known_Issues.md",
    "Docs\Next_Tasks.md",
    "Docs\Release_Notes.md"
)

function Write-Step {
    param([string]$Text)

    Write-Host ""
    Write-Host "==> $Text"
}

function Format-FileSize {
    param([long]$Bytes)

    if ($Bytes -ge 1GB) {
        return "{0:N2} GB" -f ($Bytes / 1GB)
    }

    if ($Bytes -ge 1MB) {
        return "{0:N2} MB" -f ($Bytes / 1MB)
    }

    if ($Bytes -ge 1KB) {
        return "{0:N2} KB" -f ($Bytes / 1KB)
    }

    return "$Bytes bytes"
}

function Copy-ProjectBrainFile {
    param(
        [string]$RelativePath,
        [string]$StagingRoot
    )

    $sourcePath = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        return $false
    }

    $destinationPath = Join-Path $StagingRoot $RelativePath
    $destinationDirectory = Split-Path -Parent $destinationPath
    if (-not (Test-Path -LiteralPath $destinationDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force
    return $true
}

Write-Step "Preparing Project Brain package"
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

if (Test-Path -LiteralPath $stagingRoot -PathType Container) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

$includedFiles = New-Object System.Collections.Generic.List[string]
$skippedFiles = New-Object System.Collections.Generic.List[string]

try {
    foreach ($relativePath in $sourceFiles) {
        if (Copy-ProjectBrainFile -RelativePath $relativePath -StagingRoot $stagingRoot) {
            [void]$includedFiles.Add($relativePath)
        }
        else {
            [void]$skippedFiles.Add($relativePath)
        }
    }

    Write-Step "Included files"
    if ($includedFiles.Count -gt 0) {
        foreach ($file in $includedFiles) {
            Write-Host "  + $file"
        }
    }
    else {
        Write-Host "  (none)"
    }

    Write-Step "Skipped files"
    if ($skippedFiles.Count -gt 0) {
        foreach ($file in $skippedFiles) {
            Write-Host "  - $file"
        }
    }
    else {
        Write-Host "  (none)"
    }

    if ($includedFiles.Count -eq 0) {
        Write-Host ""
        Write-Host "No Project Brain source files were found. No ZIP was created."
        exit 0
    }

    if (Test-Path -LiteralPath $zipPath -PathType Leaf) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    Write-Step "Creating ZIP"
    Push-Location $stagingRoot
    try {
        Compress-Archive -Path ".\*" -DestinationPath $zipPath -CompressionLevel Optimal -Force
    }
    finally {
        Pop-Location
    }

    $zipInfo = Get-Item -LiteralPath $zipPath
    Write-Host "Final ZIP size: $(Format-FileSize -Bytes $zipInfo.Length)"
    Write-Host "Final ZIP path: $($zipInfo.FullName)"
}
finally {
    if (Test-Path -LiteralPath $stagingRoot -PathType Container) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}

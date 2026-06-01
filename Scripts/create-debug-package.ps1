param(
    [int]$MaxScreenshots = 10
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Text)

    Write-Host ""
    Write-Host "==> $Text"
}

function Copy-PackageFile {
    param(
        [string]$RepoRoot,
        [string]$StagingRoot,
        [string]$RelativePath,
        [string]$DestinationRelativePath
    )

    $source = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        Write-Warning "Missing optional file: $RelativePath"
        return $false
    }

    if ([string]::IsNullOrWhiteSpace($DestinationRelativePath)) {
        $DestinationRelativePath = $RelativePath
    }

    $destination = Join-Path $StagingRoot $DestinationRelativePath
    $destinationDirectory = Split-Path -Parent $destination
    if (-not (Test-Path -LiteralPath $destinationDirectory)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    Copy-Item -LiteralPath $source -Destination $destination -Force
    return $true
}

function Get-LatestFileFromFolders {
    param(
        [string[]]$Folders,
        [string[]]$Patterns
    )

    $files = foreach ($folder in $Folders) {
        if (Test-Path -LiteralPath $folder -PathType Container) {
            foreach ($pattern in $Patterns) {
                Get-ChildItem -LiteralPath $folder -Filter $pattern -File -ErrorAction SilentlyContinue
            }
        }
    }

    $files | Sort-Object LastWriteTime -Descending | Select-Object -First 1
}

function Get-LatestFilesFromFolders {
    param(
        [string[]]$Folders,
        [string[]]$Patterns,
        [int]$Limit
    )

    $files = foreach ($folder in $Folders) {
        if (Test-Path -LiteralPath $folder -PathType Container) {
            foreach ($pattern in $Patterns) {
                Get-ChildItem -LiteralPath $folder -Filter $pattern -File -ErrorAction SilentlyContinue
            }
        }
    }

    $files | Sort-Object LastWriteTime -Descending | Select-Object -First $Limit
}

function Save-GitOutput {
    param(
        [string]$RepoRoot,
        [string]$OutputPath,
        [string[]]$Arguments
    )

    try {
        $output = & git -C $RepoRoot @Arguments 2>&1
        $exitCode = $LASTEXITCODE
        $output | Out-File -FilePath $OutputPath -Encoding utf8
        if ($exitCode -ne 0) {
            Write-Warning "git $($Arguments -join ' ') exited with code $exitCode."
            return $false
        }

        return $true
    }
    catch {
        "Failed to capture git $($Arguments -join ' '): $($_.Exception.Message)" | Out-File -FilePath $OutputPath -Encoding utf8
        Write-Warning "Failed to capture git $($Arguments -join ' '): $($_.Exception.Message)"
        return $false
    }
}

if ($MaxScreenshots -lt 1) {
    Write-Warning "MaxScreenshots must be at least 1. Using 1."
    $MaxScreenshots = 1
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$timestamp = Get-Date -Format "yyyyMMdd_HHmm"
$packageDirectory = Join-Path $repoRoot "DebugPackages"
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) "GoblinFarmer_Debug_$timestamp"
$zipPath = Join-Path $packageDirectory "GoblinFarmer_Debug_$timestamp.zip"

Write-Host "GoblinFarmer Debug Package Generator"
Write-Host "Repository: $repoRoot"

if (Test-Path -LiteralPath $stagingRoot) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

try {
    Write-Step "Collecting documentation"
    [void](Copy-PackageFile $repoRoot $stagingRoot "AGENTS.md" "AGENTS.md")
    [void](Copy-PackageFile $repoRoot $stagingRoot "Docs\Project_Status.md" "Docs\Project_Status.md")
    [void](Copy-PackageFile $repoRoot $stagingRoot "Docs\TEST_CHECKLIST.md" "Docs\TEST_CHECKLIST.md")
    [void](Copy-PackageFile $repoRoot $stagingRoot "Docs\TODO.md" "Docs\TODO.md")

    $logFolders = @(
        (Join-Path $repoRoot "Logs"),
        (Join-Path $repoRoot "bin\Debug\net10.0-windows\Logs"),
        (Join-Path $repoRoot "bin\Release\net10.0-windows\Logs")
    )

    Write-Step "Collecting latest log"
    $latestLog = Get-LatestFileFromFolders $logFolders @("*.log", "*.txt")
    if ($null -eq $latestLog) {
        Write-Warning "No log files found in known Logs folders."
    }
    else {
        $logDestinationDirectory = Join-Path $stagingRoot "Logs"
        New-Item -ItemType Directory -Path $logDestinationDirectory -Force | Out-Null
        Copy-Item -LiteralPath $latestLog.FullName -Destination (Join-Path $logDestinationDirectory $latestLog.Name) -Force
        Write-Host "Included latest log: $($latestLog.Name)"
    }

    $screenshotFolders = @(
        (Join-Path $repoRoot "Screenshots"),
        (Join-Path $repoRoot "bin\Debug\net10.0-windows\Screenshots"),
        (Join-Path $repoRoot "bin\Release\net10.0-windows\Screenshots")
    )

    Write-Step "Collecting latest screenshots"
    $latestScreenshots = @(Get-LatestFilesFromFolders $screenshotFolders @("*.png", "*.jpg", "*.jpeg", "*.bmp") $MaxScreenshots)
    if ($latestScreenshots.Count -eq 0) {
        Write-Warning "No debug screenshots found in known Screenshots folders."
    }
    else {
        $screenshotDestinationDirectory = Join-Path $stagingRoot "Screenshots"
        New-Item -ItemType Directory -Path $screenshotDestinationDirectory -Force | Out-Null
        foreach ($screenshot in $latestScreenshots) {
            Copy-Item -LiteralPath $screenshot.FullName -Destination (Join-Path $screenshotDestinationDirectory $screenshot.Name) -Force
        }
        Write-Host "Included screenshots: $($latestScreenshots.Count)"
    }

    Write-Step "Capturing git state"
    $gitStatusCaptured = Save-GitOutput $repoRoot (Join-Path $stagingRoot "git-status.txt") @("status", "--short")
    $gitLogCaptured = Save-GitOutput $repoRoot (Join-Path $stagingRoot "git-log.txt") @("log", "--oneline", "--decorate", "-n", "25")

    if (-not (Test-Path -LiteralPath $packageDirectory)) {
        New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
    }

    if (Test-Path -LiteralPath $zipPath) {
        Write-Warning "Existing package for this minute will be replaced: $zipPath"
        Remove-Item -LiteralPath $zipPath -Force
    }

    Write-Step "Creating zip package"
    Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $zipPath -Force
}
finally {
    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}

Write-Host ""
Write-Host "Debug package created."
Write-Host "Package path: $zipPath"
Write-Host "Included latest log filename: $(if ($null -ne $latestLog) { $latestLog.Name } else { 'none' })"
Write-Host "Screenshots included: $($latestScreenshots.Count)"
Write-Host "Git status captured: $gitStatusCaptured"
Write-Host "Git log captured: $gitLogCaptured"

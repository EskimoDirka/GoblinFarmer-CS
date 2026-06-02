param(
    [int]$MaxScreenshots = 10,
    [int]$MaxFailureScreenshots = 10
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

function Get-ScreenshotFailureType {
    param([System.IO.FileInfo]$File)

    $failureTypes = @(
        "TeleportBlocked",
        "TeleportInterrupted",
        "TeleportConfirmationTimeout",
        "StartGameButtonNotFound",
        "StartGameVerificationFailed",
        "BattleNetPlayButtonNotFound",
        "DiabloTabNotFound",
        "RepairStationNotFound",
        "RepairFailed",
        "WorkflowCancelled",
        "UnexpectedException"
    )

    foreach ($failureType in $failureTypes) {
        if ($File.BaseName -like "*$failureType*") {
            return $failureType
        }
    }

    return ""
}

function Copy-FilesToPackageFolder {
    param(
        [System.IO.FileInfo[]]$Files,
        [string]$DestinationDirectory
    )

    if ($Files.Count -eq 0) {
        return 0
    }

    New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null
    foreach ($file in $Files) {
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $DestinationDirectory $file.Name) -Force
    }

    return $Files.Count
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

if ($MaxFailureScreenshots -lt 1) {
    Write-Warning "MaxFailureScreenshots must be at least 1. Using 1."
    $MaxFailureScreenshots = 1
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$packageDirectory = Join-Path $repoRoot "DebugPackages"
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) "GoblinFarmer_Debug_$timestamp"
$zipPath = Join-Path $packageDirectory "GoblinFarmer_Debug_$timestamp.zip"
$manifestPath = Join-Path $stagingRoot "debug-package-manifest.txt"

Write-Host "GoblinFarmer Debug Package Generator"
Write-Host "Repository: $repoRoot"
Write-Host "Timestamp: $timestamp"
Write-Host "Max normal screenshots: $MaxScreenshots"
Write-Host "Max failure screenshots: $MaxFailureScreenshots"

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
    $allScreenshotCandidates = foreach ($folder in $screenshotFolders) {
        if (Test-Path -LiteralPath $folder -PathType Container) {
            Get-ChildItem -LiteralPath $folder -File -ErrorAction SilentlyContinue |
                Where-Object { $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' }
        }
    }
    $allScreenshots = @($allScreenshotCandidates | Sort-Object LastWriteTime -Descending)

    $failureScreenshots = @($allScreenshots |
        Where-Object { -not [string]::IsNullOrWhiteSpace((Get-ScreenshotFailureType $_)) } |
        Select-Object -First $MaxFailureScreenshots)

    $normalScreenshots = @($allScreenshots |
        Where-Object { [string]::IsNullOrWhiteSpace((Get-ScreenshotFailureType $_)) } |
        Select-Object -First $MaxScreenshots)

    $latestFailureScreenshot = $failureScreenshots | Select-Object -First 1
    $latestFailureType = if ($null -ne $latestFailureScreenshot) { Get-ScreenshotFailureType $latestFailureScreenshot } else { "none" }

    if ($allScreenshots.Count -eq 0) {
        Write-Warning "No debug screenshots found in known Screenshots folders."
    }
    else {
        $failureCount = Copy-FilesToPackageFolder $failureScreenshots (Join-Path $stagingRoot "Screenshots\Failure")
        $normalCount = Copy-FilesToPackageFolder $normalScreenshots (Join-Path $stagingRoot "Screenshots\Recent")

        if ($failureCount -eq 0) {
            Write-Warning "No failure screenshots found. Expected failure type names in screenshot filenames."
        }
        else {
            Write-Host "Included failure screenshots: $failureCount"
            Write-Host "Latest failure screenshot type: $latestFailureType"
            Write-Host "Latest failure screenshot filename: $($latestFailureScreenshot.Name)"
        }

        if ($normalCount -eq 0) {
            Write-Warning "No non-failure debug screenshots found."
        }
        else {
            Write-Host "Included normal debug screenshots: $normalCount"
        }
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
    $manifestLines = @(
        "GoblinFarmer Debug Package",
        "Created: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')",
        "Repository: $repoRoot",
        "Package: $zipPath",
        "",
        "Included files:",
        "- AGENTS.md",
        "- Docs/Project_Status.md",
        "- Docs/TODO.md",
        "- Docs/TEST_CHECKLIST.md",
        "- git-status.txt",
        "- git-log.txt",
        "- Latest log: $(if ($null -ne $latestLog) { $latestLog.FullName } else { 'none' })",
        "- Failure screenshots included: $($failureScreenshots.Count)",
        "- Normal debug screenshots included: $($normalScreenshots.Count)",
        "- Latest failure screenshot type: $latestFailureType",
        "- Latest failure screenshot: $(if ($null -ne $latestFailureScreenshot) { $latestFailureScreenshot.FullName } else { 'none' })",
        "",
        "Exclusions:",
        "- bin folders are not copied",
        "- obj folders are not copied",
        "- build artifacts are not copied except selected runtime logs/screenshots"
    )
    $manifestLines | Out-File -FilePath $manifestPath -Encoding utf8

    Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $zipPath -Force
}
finally {
    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}

Write-Host ""
Write-Host "========== Debug Package Summary =========="
Write-Host "Package path:        $zipPath"
Write-Host "Latest log:          $(if ($null -ne $latestLog) { $latestLog.Name } else { 'none' })"
Write-Host "Failure screenshots: $($failureScreenshots.Count)"
Write-Host "Normal screenshots:  $($normalScreenshots.Count)"
Write-Host "Latest failure type: $latestFailureType"
Write-Host "Git status captured: $gitStatusCaptured"
Write-Host "Git log captured:    $gitLogCaptured"
Write-Host "Manifest:            debug-package-manifest.txt"
Write-Host "==========================================="

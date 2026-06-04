param(
    [string]$UserInstallDir = "",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

function Stop-Updater {
    param([string]$Message)

    Write-Error $Message
    exit 1
}

function Test-IsPathInside {
    param(
        [string]$ChildPath,
        [string]$ParentPath
    )

    $childFull = [System.IO.Path]::GetFullPath($ChildPath).TrimEnd('\') + '\'
    $parentFull = [System.IO.Path]::GetFullPath($ParentPath).TrimEnd('\') + '\'
    return $childFull.StartsWith($parentFull, [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-ProcessRunningFromPath {
    param([string]$Path)

    $target = [System.IO.Path]::GetFullPath($Path).TrimEnd('\') + '\'
    try {
        $processes = Get-CimInstance Win32_Process -Filter "Name = 'GoblinFarmer.exe'" -ErrorAction SilentlyContinue
        foreach ($process in $processes) {
            if (![string]::IsNullOrWhiteSpace($process.ExecutablePath)) {
                $exePath = [System.IO.Path]::GetFullPath($process.ExecutablePath)
                if ($exePath.StartsWith($target, [System.StringComparison]::OrdinalIgnoreCase)) {
                    return $true
                }
            }
        }
    }
    catch {
        Write-Warning "Could not inspect running GoblinFarmer processes: $($_.Exception.Message)"
    }

    return $false
}

function Get-DefaultUserInstallDir {
    $localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    if ([string]::IsNullOrWhiteSpace($localAppData)) {
        $localAppData = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)) "AppData\Local"
    }

    return Join-Path $localAppData "GoblinFarmer"
}

function Copy-PublishPayload {
    param(
        [string]$SourceDir,
        [string]$DestinationDir
    )

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

    New-Item -ItemType Directory -Force -Path $DestinationDir | Out-Null

    Get-ChildItem -LiteralPath $DestinationDir -Force | ForEach-Object {
        if (!$preserveSet.Contains($_.Name)) {
            Remove-Item -LiteralPath $_.FullName -Recurse -Force
        }
    }

    Get-ChildItem -LiteralPath $SourceDir -Force | ForEach-Object {
        $destinationPath = Join-Path $DestinationDir $_.Name
        if ($_.Name.Equals("Config", [System.StringComparison]::OrdinalIgnoreCase) -and
            (Test-Path -LiteralPath $destinationPath)) {
            return
        }

        if ($preserveSet.Contains($_.Name) -and
            !$_.Name.Equals("Config", [System.StringComparison]::OrdinalIgnoreCase)) {
            return
        }

        Copy-Item -LiteralPath $_.FullName -Destination $DestinationDir -Recurse -Force
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$UserInstallDir = if ([string]::IsNullOrWhiteSpace($UserInstallDir)) { Get-DefaultUserInstallDir } else { $UserInstallDir }
$projectPath = Join-Path $repoRoot "GoblinFarmer.csproj"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishRoot = Join-Path $artifactsRoot "exe-updater"
$publishDir = Join-Path $publishRoot "GoblinFarmer"
$sourceExe = Join-Path $publishDir "GoblinFarmer.exe"
$sourceDll = Join-Path $publishDir "GoblinFarmer.dll"
$sourceDeps = Join-Path $publishDir "GoblinFarmer.deps.json"
$sourceRuntimeConfig = Join-Path $publishDir "GoblinFarmer.runtimeconfig.json"
$projectExeCopy = Join-Path $repoRoot "GoblinFarmer.exe"
$githubUploadDir = Join-Path $repoRoot "GitHub Upload"
$githubUploadExe = Join-Path $githubUploadDir "GoblinFarmer.exe"
$destinationFull = [System.IO.Path]::GetFullPath($UserInstallDir)
$driveRoot = [System.IO.Path]::GetPathRoot($destinationFull)

if (!(Test-Path -LiteralPath $projectPath)) {
    Stop-Updater "Project file not found: $projectPath"
}

if ([string]::IsNullOrWhiteSpace($destinationFull)) {
    Stop-Updater "User app destination is empty."
}

if ($destinationFull.TrimEnd('\').Equals($driveRoot.TrimEnd('\'), [System.StringComparison]::OrdinalIgnoreCase)) {
    Stop-Updater "Refusing to refresh a drive root: $destinationFull"
}

if (Test-IsPathInside -ChildPath $destinationFull -ParentPath $repoRoot) {
    Stop-Updater "Refusing to refresh a user folder inside the repository: $destinationFull"
}

if (Test-ProcessRunningFromPath -Path $destinationFull) {
    Stop-Updater "GoblinFarmer appears to be running from $destinationFull. Close it before running Exe Updater."
}

Write-Host "GoblinFarmer Exe Updater"
Write-Host "Repository: $repoRoot"
Write-Host "Publish dir: $publishDir"
Write-Host "Runtime:     $Runtime"
Write-Host "User folder: $destinationFull"
Write-Host ""

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

Write-Host "==> Publishing self-contained Release"
dotnet publish $projectPath `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDir `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true
if ($LASTEXITCODE -ne 0) {
    Stop-Updater "dotnet publish failed."
}

foreach ($requiredPath in @($sourceExe, $sourceDll, $sourceDeps, $sourceRuntimeConfig)) {
    if (!(Test-Path -LiteralPath $requiredPath)) {
        Stop-Updater "Required publish output missing: $requiredPath"
    }
}

Write-Host ""
Write-Host "==> Removing old project exe handoff copies"
foreach ($oldExe in @($projectExeCopy, $githubUploadExe)) {
    $oldExeFull = [System.IO.Path]::GetFullPath($oldExe)
    if (!(Test-IsPathInside -ChildPath $oldExeFull -ParentPath $repoRoot)) {
        Stop-Updater "Refusing to remove exe outside the repository: $oldExeFull"
    }

    if (Test-Path -LiteralPath $oldExeFull) {
        Remove-Item -LiteralPath $oldExeFull -Force
        Write-Host "Removed: $oldExeFull"
    }
}

Write-Host ""
Write-Host "==> Staging fresh project exe copies"
New-Item -ItemType Directory -Force -Path $githubUploadDir | Out-Null
Copy-Item -LiteralPath $sourceExe -Destination $projectExeCopy -Force
Copy-Item -LiteralPath $sourceExe -Destination $githubUploadExe -Force
Write-Host "Project exe: $projectExeCopy"
Write-Host "GitHub exe:  $githubUploadExe"

Write-Host ""
Write-Host "==> Refreshing runnable app folder"
Copy-PublishPayload -SourceDir $publishDir -DestinationDir $destinationFull

Write-Host ""
Write-Host "========== Exe Updater Summary =========="
Write-Host "Built:       $sourceExe"
Write-Host "Project exe: $projectExeCopy"
Write-Host "GitHub exe:  $githubUploadExe"
Write-Host "User app:    $destinationFull"
Write-Host "Preserved:   Existing Config, Logs, Screenshots, debug-screenshots, DebugPackages, ScanRegions/session metadata"

Write-Host ""
Write-Host "========== Git Change Check =========="
$gitCommand = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $gitCommand) {
    Write-Host "Git was not found on PATH, so change status could not be checked."
}
else {
    $repoCheck = & git -C $repoRoot rev-parse --is-inside-work-tree 2>$null
    if ($LASTEXITCODE -ne 0 -or $repoCheck.Trim() -ne "true") {
        Write-Host "Repository folder is not recognized by Git: $repoRoot"
    }
    else {
        $gitStatus = & git -C $repoRoot status --short -- "GoblinFarmer.exe" "GitHub Upload/GoblinFarmer.exe"
        if ([string]::IsNullOrWhiteSpace(($gitStatus -join ""))) {
            Write-Host "No Git changes detected for the copied exe files."
            Write-Host "Visual Studio will not show them until their contents differ from the last commit."
        }
        else {
            Write-Host "Git sees these exe changes:"
            $gitStatus | ForEach-Object { Write-Host $_ }
            Write-Host "Commit and push these changes to sync them with GitHub."
        }
    }
}

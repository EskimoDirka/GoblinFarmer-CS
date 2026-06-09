param(
    [switch]$Delete,
    [switch]$RuntimeArtifacts,
    [int]$DebugPackageRetention = 10,
    [switch]$PruneOldInstallers,
    [switch]$WriteReport = $true,
    [string]$ProjectRoot = ""
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Split-Path -Parent $scriptRoot
}

$resolvedProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\', '/')
$reportsRoot = Join-Path $resolvedProjectRoot "Reports"
$reportPath = Join-Path $reportsRoot "Cleanup_Report.md"
$runMode = if ($Delete) { "DELETE" } else { "DRY RUN" }
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$records = New-Object System.Collections.Generic.List[object]
$protectedPaths = New-Object System.Collections.Generic.List[string]
$errors = New-Object System.Collections.Generic.List[string]
$totalCandidateBytes = [int64]0
$totalDeletedBytes = [int64]0

function Format-FileSize {
    param([int64]$Bytes)

    if ($Bytes -ge 1GB) { return "{0:N2} GB" -f ($Bytes / 1GB) }
    if ($Bytes -ge 1MB) { return "{0:N2} MB" -f ($Bytes / 1MB) }
    if ($Bytes -ge 1KB) { return "{0:N2} KB" -f ($Bytes / 1KB) }
    return "$Bytes bytes"
}

function Write-Step {
    param([string]$Text)

    Write-Host ""
    Write-Host "==> $Text"
}

function ConvertTo-MarkdownTableRow {
    param([string[]]$Values)

    $escaped = foreach ($value in $Values) {
        ($value -replace '\|', '\|')
    }

    return "| $($escaped -join ' | ') |"
}

function Resolve-ProjectPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "Cannot resolve an empty path."
    }

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    }

    return [System.IO.Path]::GetFullPath((Join-Path $resolvedProjectRoot $Path)).TrimEnd('\', '/')
}

function Test-IsInsideRoot {
    param([string]$Path)

    $rootWithSlash = $resolvedProjectRoot.TrimEnd('\', '/') + '\'
    return [string]::Equals($Path, $resolvedProjectRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
        $Path.StartsWith($rootWithSlash, [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-IsSameOrChildPath {
    param(
        [string]$Path,
        [string]$Parent
    )

    $parentWithSlash = $Parent.TrimEnd('\', '/') + '\'
    return [string]::Equals($Path, $Parent, [System.StringComparison]::OrdinalIgnoreCase) -or
        $Path.StartsWith($parentWithSlash, [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-RelativeProjectPath {
    param([string]$Path)

    $rootWithSlash = $resolvedProjectRoot.TrimEnd('\', '/') + '\'
    if ($Path.StartsWith($rootWithSlash, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Path.Substring($rootWithSlash.Length)
    }

    return $Path
}

function Add-Record {
    param(
        [string]$Path,
        [string]$Category,
        [string]$Action,
        [int64]$Bytes = 0,
        [string]$Reason = ""
    )

    $record = [pscustomobject]@{
        Path = $Path
        RelativePath = if ([string]::IsNullOrWhiteSpace($Path)) { "" } else { Get-RelativeProjectPath -Path $Path }
        Category = $Category
        Action = $Action
        Bytes = $Bytes
        Reason = $Reason
    }
    [void]$records.Add($record)

    $sizeText = Format-FileSize -Bytes $Bytes
    if ([string]::IsNullOrWhiteSpace($Reason)) {
        Write-Host "[$Action] $($record.RelativePath) ($sizeText)"
    }
    else {
        Write-Host "[$Action] $($record.RelativePath) ($sizeText) - $Reason"
    }
}

function Get-PathSize {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return 0
    }

    $item = Get-Item -LiteralPath $Path -Force
    if (-not $item.PSIsContainer) {
        return [int64]$item.Length
    }

    $sum = [int64]0
    Get-ChildItem -LiteralPath $Path -File -Recurse -Force -ErrorAction SilentlyContinue |
        ForEach-Object { $sum += [int64]$_.Length }
    return $sum
}

function Assert-ExpectedProjectRoot {
    $requiredFiles = @(
        "GoblinFarmer.csproj",
        "AGENTS.md",
        "README.md",
        "Docs\Project_Status.md"
    )

    foreach ($relativePath in $requiredFiles) {
        $path = Join-Path $resolvedProjectRoot $relativePath
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Refusing to run cleanup: expected project marker missing: $path"
        }
    }
}

function Add-ProtectedPath {
    param([string]$RelativePath)

    $path = Resolve-ProjectPath -Path $RelativePath
    [void]$protectedPaths.Add($path)
}

function Test-IsProtectedPath {
    param([string]$Path)

    foreach ($protectedPath in $protectedPaths) {
        if (Test-IsSameOrChildPath -Path $Path -Parent $protectedPath) {
            return $true
        }
    }

    if ($Path -match '\.(cs|csproj|sln|props|targets)$') {
        return $true
    }

    return $false
}

function Assert-SafeTarget {
    param(
        [string]$Path,
        [string]$Category
    )

    $resolvedPath = Resolve-ProjectPath -Path $Path
    if (-not (Test-IsInsideRoot -Path $resolvedPath)) {
        throw "Refusing to consider path outside project root: $resolvedPath"
    }

    if ([string]::Equals($resolvedPath, $resolvedProjectRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to consider the project root itself: $resolvedPath"
    }

    if (Test-IsProtectedPath -Path $resolvedPath) {
        throw "Refusing to consider protected path: $resolvedPath"
    }

    return $resolvedPath
}

function Invoke-CleanupTarget {
    param(
        [string]$Path,
        [string]$Category,
        [string]$Reason
    )

    try {
        $resolvedPath = Assert-SafeTarget -Path $Path -Category $Category
        if (-not (Test-Path -LiteralPath $resolvedPath)) {
            Add-Record -Path $resolvedPath -Category $Category -Action "Skipped" -Reason "Missing path"
            return
        }

        $item = Get-Item -LiteralPath $resolvedPath -Force
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            Add-Record -Path $resolvedPath -Category $Category -Action "Skipped" -Reason "Reparse point skipped"
            return
        }

        $bytes = Get-PathSize -Path $resolvedPath
        $script:totalCandidateBytes += $bytes

        if ($Delete) {
            Remove-Item -LiteralPath $resolvedPath -Recurse -Force
            $script:totalDeletedBytes += $bytes
            Add-Record -Path $resolvedPath -Category $Category -Action "Deleted" -Bytes $bytes -Reason $Reason
        }
        else {
            Add-Record -Path $resolvedPath -Category $Category -Action "WouldDelete" -Bytes $bytes -Reason $Reason
        }
    }
    catch {
        $message = "$Category target failed: $Path; $($_.Exception.Message)"
        [void]$errors.Add($message)
        Add-Record -Path $Path -Category $Category -Action "Error" -Reason $_.Exception.Message
    }
}

function Invoke-DebugPackageRetention {
    if ($DebugPackageRetention -lt 0) {
        throw "DebugPackageRetention must be zero or greater."
    }

    $packageRoot = Assert-SafeTarget -Path "DebugPackages" -Category "DebugPackages"
    if (-not (Test-Path -LiteralPath $packageRoot -PathType Container)) {
        Add-Record -Path $packageRoot -Category "DebugPackages" -Action "Skipped" -Reason "Missing path"
        return
    }

    $packages = @(Get-ChildItem -LiteralPath $packageRoot -Filter "GoblinFarmer_Debug_*.zip" -File -Force |
        Sort-Object @{ Expression = { $_.LastWriteTimeUtc }; Descending = $true }, @{ Expression = { $_.Name }; Descending = $true })

    $index = 0
    foreach ($package in $packages) {
        $index++
        $resolvedPath = Assert-SafeTarget -Path $package.FullName -Category "DebugPackages"
        $bytes = [int64]$package.Length

        if ($index -le $DebugPackageRetention) {
            Add-Record -Path $resolvedPath -Category "DebugPackages" -Action "Keep" -Bytes $bytes -Reason "Newest package within retention $DebugPackageRetention"
            continue
        }

        $script:totalCandidateBytes += $bytes
        if ($Delete) {
            Remove-Item -LiteralPath $resolvedPath -Force
            $script:totalDeletedBytes += $bytes
            Add-Record -Path $resolvedPath -Category "DebugPackages" -Action "Deleted" -Bytes $bytes -Reason "Older than retention $DebugPackageRetention"
        }
        else {
            Add-Record -Path $resolvedPath -Category "DebugPackages" -Action "WouldDelete" -Bytes $bytes -Reason "Older than retention $DebugPackageRetention"
        }
    }
}

function Invoke-InstallerPrune {
    $installerCandidates = New-Object System.Collections.Generic.List[object]
    $candidateRoots = @(
        "artifacts\installer",
        "."
    )

    foreach ($relativeRoot in $candidateRoots) {
        try {
            $root = Resolve-ProjectPath -Path $relativeRoot
            if (-not (Test-IsInsideRoot -Path $root)) {
                throw "Installer root resolved outside project root: $root"
            }

            if (-not (Test-Path -LiteralPath $root -PathType Container)) {
                Add-Record -Path $root -Category "Installers" -Action "Skipped" -Reason "Missing path"
                continue
            }

            $files = Get-ChildItem -LiteralPath $root -Filter "GoblinFarmerSetup-*.exe" -File -Force
            foreach ($file in $files) {
                [void]$installerCandidates.Add($file)
            }
        }
        catch {
            $message = "Installer root failed: $relativeRoot; $($_.Exception.Message)"
            [void]$errors.Add($message)
            Add-Record -Path $relativeRoot -Category "Installers" -Action "Error" -Reason $_.Exception.Message
        }
    }

    $installers = @($installerCandidates | Sort-Object @{ Expression = { $_.LastWriteTimeUtc }; Descending = $true }, @{ Expression = { $_.Name }; Descending = $true })
    if ($installers.Count -eq 0) {
        return
    }

    $latest = $installers[0]
    foreach ($installer in $installers) {
        $resolvedPath = Assert-SafeTarget -Path $installer.FullName -Category "Installers"
        $bytes = [int64]$installer.Length

        if ([string]::Equals($installer.FullName, $latest.FullName, [System.StringComparison]::OrdinalIgnoreCase)) {
            Add-Record -Path $resolvedPath -Category "Installers" -Action "Keep" -Bytes $bytes -Reason "Latest installer is protected"
            continue
        }

        $script:totalCandidateBytes += $bytes
        if ($Delete) {
            Remove-Item -LiteralPath $resolvedPath -Force
            $script:totalDeletedBytes += $bytes
            Add-Record -Path $resolvedPath -Category "Installers" -Action "Deleted" -Bytes $bytes -Reason "Older installer; -PruneOldInstallers enabled"
        }
        else {
            Add-Record -Path $resolvedPath -Category "Installers" -Action "WouldDelete" -Bytes $bytes -Reason "Older installer; -PruneOldInstallers enabled"
        }
    }
}

function Get-TestsBuildDirectories {
    $testsRoot = Resolve-ProjectPath -Path "Tests"
    if (-not (Test-Path -LiteralPath $testsRoot -PathType Container)) {
        return @()
    }

    return @(Get-ChildItem -LiteralPath $testsRoot -Directory -Recurse -Force |
        Where-Object {
            ($_.Name -ieq "bin" -or $_.Name -ieq "obj") -and
            (($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0)
        } |
        Sort-Object FullName)
}

function Get-RuntimeArtifactDirectories {
    $runtimeNames = @(
        "Debug",
        "Logs",
        "Log",
        "Screenshots",
        "debug-screenshots",
        "GoblinEvidence",
        "DecisionBundles",
        "ReplayCaptures",
        "ReplayCapture",
        "EncounterCaptures",
        "ManualCaptures"
    )

    $buildRoots = @(
        (Resolve-ProjectPath -Path "bin"),
        (Resolve-ProjectPath -Path "obj")
    )

    return @(Get-ChildItem -LiteralPath $resolvedProjectRoot -Directory -Recurse -Force |
        Where-Object {
            $candidate = $_
            $underBuildRoot = @($buildRoots | Where-Object { Test-IsSameOrChildPath -Path $candidate.FullName -Parent $_ }).Count -gt 0
            ($runtimeNames -contains $candidate.Name) -and
            (($candidate.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0) -and
            -not $underBuildRoot -and
            -not (Test-IsProtectedPath -Path $candidate.FullName)
        } |
        Sort-Object FullName)
}

function Write-CleanupReport {
    if (-not $WriteReport) {
        return
    }

    New-Item -ItemType Directory -Path $reportsRoot -Force | Out-Null

    $lines = New-Object System.Collections.Generic.List[string]
    [void]$lines.Add("# Cleanup Report")
    [void]$lines.Add("")
    [void]$lines.Add("- Timestamp: $timestamp")
    [void]$lines.Add("- Project root: ``$resolvedProjectRoot``")
    [void]$lines.Add("- Run mode: **$runMode**")
    [void]$lines.Add("- Runtime artifacts included: $RuntimeArtifacts")
    [void]$lines.Add("- Debug package retention: $DebugPackageRetention")
    [void]$lines.Add("- Prune old installers: $PruneOldInstallers")
    [void]$lines.Add("- Estimated reclaimable bytes: $totalCandidateBytes ($(Format-FileSize -Bytes $totalCandidateBytes))")
    [void]$lines.Add("- Deleted bytes: $totalDeletedBytes ($(Format-FileSize -Bytes $totalDeletedBytes))")
    [void]$lines.Add("")
    [void]$lines.Add("## Paths Considered")
    [void]$lines.Add("")
    [void]$lines.Add((ConvertTo-MarkdownTableRow @("Action", "Category", "Path", "Size", "Bytes", "Reason")))
    [void]$lines.Add("|---|---|---|---:|---:|---|")
    foreach ($record in $records) {
        [void]$lines.Add((ConvertTo-MarkdownTableRow @($record.Action, $record.Category, $record.RelativePath, (Format-FileSize -Bytes $record.Bytes), "$($record.Bytes)", $record.Reason)))
    }

    [void]$lines.Add("")
    [void]$lines.Add("## Protected Paths")
    [void]$lines.Add("")
    foreach ($path in $protectedPaths) {
        [void]$lines.Add("- ``$(Get-RelativeProjectPath -Path $path)``")
    }
    [void]$lines.Add('- Source files: `*.cs`, `*.csproj`, `*.sln`, `*.props`, `*.targets`')

    [void]$lines.Add("")
    [void]$lines.Add("## Errors")
    [void]$lines.Add("")
    if ($errors.Count -eq 0) {
        [void]$lines.Add("- None")
    }
    else {
        foreach ($errorMessage in $errors) {
            [void]$lines.Add("- $errorMessage")
        }
    }

    Set-Content -LiteralPath $reportPath -Value $lines -Encoding UTF8
    Write-Host "Report path: $reportPath"
}

Assert-ExpectedProjectRoot

@(
    ".git",
    "Images",
    "Docs",
    "Scripts",
    "Config",
    "Installer",
    "README.md",
    "AGENTS.md",
    "GoblinFarmer.csproj"
) | ForEach-Object { Add-ProtectedPath -RelativePath $_ }

Write-Host "GoblinFarmer project cleanup"
Write-Host "Project root: $resolvedProjectRoot"
Write-Host "Run mode: $runMode"
Write-Host "Runtime artifacts: $RuntimeArtifacts"
Write-Host "Debug package retention: $DebugPackageRetention"
Write-Host "Prune old installers: $PruneOldInstallers"
if (-not $Delete) {
    Write-Host "No files will be deleted. Pass -Delete to remove listed delete candidates."
}

Write-Step "Always-safe generated build outputs"
Invoke-CleanupTarget -Path "bin" -Category "Build outputs" -Reason "Root build output"
Invoke-CleanupTarget -Path "obj" -Category "Build outputs" -Reason "Root intermediate output"
foreach ($directory in Get-TestsBuildDirectories) {
    Invoke-CleanupTarget -Path $directory.FullName -Category "Test build outputs" -Reason "Tests build/intermediate output"
}

Write-Step "Debug package retention"
Invoke-DebugPackageRetention

if ($RuntimeArtifacts) {
    Write-Step "Optional runtime artifacts"
    foreach ($directory in Get-RuntimeArtifactDirectories) {
        Invoke-CleanupTarget -Path $directory.FullName -Category "Runtime artifacts" -Reason "-RuntimeArtifacts enabled"
    }
}
else {
    Write-Step "Optional runtime artifacts"
    Add-Record -Path (Resolve-ProjectPath -Path "Debug") -Category "Runtime artifacts" -Action "Skipped" -Reason "Pass -RuntimeArtifacts to include Debug/log/screenshot/evidence/replay folders"
}

if ($PruneOldInstallers) {
    Write-Step "Old installer pruning"
    Invoke-InstallerPrune
}
else {
    Write-Step "Old installer pruning"
    Add-Record -Path (Resolve-ProjectPath -Path "artifacts\installer") -Category "Installers" -Action "Skipped" -Reason "Pass -PruneOldInstallers to delete older installers while keeping latest"
}

Write-Step "Summary"
Write-Host "Run mode: $runMode"
Write-Host "Estimated space savings: $(Format-FileSize -Bytes $totalCandidateBytes) ($totalCandidateBytes bytes)"
Write-Host "Deleted: $(Format-FileSize -Bytes $totalDeletedBytes) ($totalDeletedBytes bytes)"
if ($errors.Count -gt 0) {
    Write-Host "Errors: $($errors.Count)"
}
else {
    Write-Host "Errors: 0"
}

Write-CleanupReport

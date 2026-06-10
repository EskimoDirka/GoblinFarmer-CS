param(
    [switch]$WriteStorageBreakdown,
    [switch]$StorageBreakdownOnly
)

$ErrorActionPreference = "Stop"

# Creates a lightweight docs-only Project Brain ZIP for AI-assisted development.
# The archive is intentionally allowlist-only so runtime artifacts, screenshots,
# images, binaries, replay data, and existing ZIPs cannot be included.

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$outputRoot = Join-Path $repoRoot "ProjectBrain"
$reportsRoot = Join-Path $repoRoot "Reports"
$storageReportPath = Join-Path $reportsRoot "Storage_Breakdown.md"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$zipName = "GoblinFarmer_ProjectBrain_$timestamp.zip"
$zipPath = Join-Path $outputRoot $zipName
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) "GoblinFarmer_ProjectBrain_$timestamp"
$ProjectBrainZipRetentionDays = 7

$explicitSourceFiles = @(
    "AGENTS.md",
    "README.md",
    "Docs\Project_Status.md",
    "Docs\TODO.md",
    "Docs\History.md",
    "Docs\CombatProfiles.md",
    "Docs\TeleportLogic.md",
    "Docs\ScanRegions.md",
    "Docs\Release_Checklist.md",
    "Docs\Release_v1.4.md",
    "Docs\Test_Results.md",
    "Docs\Known_Issues.md",
    "Docs\Next_Tasks.md",
    "Docs\Release_Notes.md"
)

$projectBrainSourceRoot = Join-Path $repoRoot "Docs\ProjectBrain"
$agentDocsSourceRoot = Join-Path $repoRoot "Docs\Agent"

function Add-UniqueRelativePath {
    param(
        [System.Collections.Generic.List[string]]$Paths,
        [string]$RelativePath
    )

    foreach ($existingPath in $Paths) {
        if ([string]::Equals($existingPath, $RelativePath, [System.StringComparison]::OrdinalIgnoreCase)) {
            return
        }
    }

    [void]$Paths.Add($RelativePath)
}

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

function Get-RelativeProjectPath {
    param([string]$Path)

    $root = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($root.Length)
    }

    return $fullPath
}

function ConvertTo-MarkdownTableRow {
    param([string[]]$Values)

    $escaped = foreach ($value in $Values) {
        ($value -replace '\|', '\|')
    }

    return "| $($escaped -join ' | ') |"
}

function Get-StorageCategory {
    param([string]$RelativePath)

    if ($RelativePath -match '^\.git(\\|$)') {
        return "Git metadata"
    }

    if ($RelativePath -match '(^|\\)(bin|obj|Debug|DebugPackages|Logs|Screenshots|debug-screenshots|Sessions|ProjectBrain|artifacts|ChatGPT Uploads|GitHub Upload)(\\|$)' -or
        $RelativePath -match '^Installer\\Output(\\|$)' -or
        $RelativePath -match '\\(GoblinEvidence|DecisionBundles|EncounterCaptures|ManualCaptures|ObservationDiagnostics|ReplayCaptures|Captures)(\\|$)' -or
        $RelativePath -match '\.(zip|7z|rar|msi|msix|cab|exe)$') {
        return "Generated/debug/build artifacts"
    }

    if ($RelativePath -match '^(Docs|Scripts|Config|Images|Tests|Properties|Installer)(\\|$)' -or
        $RelativePath -match '\.(cs|csproj|sln|json|md|txt|xml|resx|ico|iss|bat|ps1)$') {
        return "Source/docs"
    }

    return "Other/manual review"
}

function New-StorageSummary {
    param(
        [object[]]$Rows,
        [scriptblock]$KeySelector,
        [int]$Limit = 20
    )

    $summary = @{}
    foreach ($row in $Rows) {
        $key = & $KeySelector $row
        if ([string]::IsNullOrWhiteSpace($key)) {
            $key = "(none)"
        }

        if (-not $summary.ContainsKey($key)) {
            $summary[$key] = [pscustomobject]@{
                Name = $key
                Bytes = [int64]0
                Count = 0
            }
        }

        $summary[$key].Bytes += [int64]$row.Length
        $summary[$key].Count += 1
    }

    return $summary.Values | Sort-Object Bytes -Descending | Select-Object -First $Limit
}

function Write-StorageBreakdownReport {
    Write-Step "Generating storage breakdown report"

    New-Item -ItemType Directory -Path $reportsRoot -Force | Out-Null

    $allFiles = Get-ChildItem -LiteralPath $repoRoot -File -Recurse -Force -ErrorAction SilentlyContinue |
        ForEach-Object {
            $relativePath = Get-RelativeProjectPath -Path $_.FullName
            [pscustomobject]@{
                FullName = $_.FullName
                RelativePath = $relativePath
                Directory = Split-Path -Parent $relativePath
                Name = $_.Name
                Extension = if ([string]::IsNullOrWhiteSpace($_.Extension)) { "[no extension]" } else { $_.Extension.ToLowerInvariant() }
                Length = [int64]$_.Length
                Category = Get-StorageCategory -RelativePath $relativePath
            }
        }

    $totalBytes = ($allFiles | Measure-Object -Property Length -Sum).Sum
    if ($null -eq $totalBytes) {
        $totalBytes = 0
    }

    $topLevel = New-StorageSummary -Rows $allFiles -Limit 50 -KeySelector {
        param($row)
        $parts = $row.RelativePath -split '\\', 2
        if ($parts.Count -gt 1) { $parts[0] } else { "(root files)" }
    }

    $largestSubfolders = New-StorageSummary -Rows ($allFiles | Where-Object { $_.Directory -and $_.Directory -ne "." }) -Limit 30 -KeySelector {
        param($row)
        $row.Directory
    }

    $largestExtensions = New-StorageSummary -Rows $allFiles -Limit 25 -KeySelector {
        param($row)
        $row.Extension
    }

    $categorySummary = New-StorageSummary -Rows $allFiles -Limit 10 -KeySelector {
        param($row)
        $row.Category
    }

    $cleanupTargets = @(
        @{ Name = "bin/"; Pattern = '(^|\\)bin(\\|$)' },
        @{ Name = "obj/"; Pattern = '(^|\\)obj(\\|$)' },
        @{ Name = "DebugPackages/"; Pattern = '(^|\\)DebugPackages(\\|$)' },
        @{ Name = "Debug/"; Pattern = '(^|\\)Debug(\\|$)' },
        @{ Name = "GoblinEvidence/"; Pattern = '(^|\\)GoblinEvidence(\\|$)' },
        @{ Name = "DecisionBundles/"; Pattern = '(^|\\)DecisionBundles(\\|$)' },
        @{ Name = "logs"; Pattern = '(^|\\)(Logs?|log)(\\|$)|\.log$' },
        @{ Name = "screenshots"; Pattern = '(^|\\)(Screenshots|debug-screenshots)(\\|$)|screenshot' },
        @{ Name = "old installers/packages"; Pattern = '(^|\\)(artifacts|Installer\\Output)(\\|$)|\.(zip|msi|msix|cab|exe)$' },
        @{ Name = "replay captures"; Pattern = '(^|\\)(ReplayCaptures|EncounterCaptures|ManualCaptures|Captures)(\\|$)|replay' }
    )

    $lines = New-Object System.Collections.Generic.List[string]
    [void]$lines.Add("# Storage Breakdown")
    [void]$lines.Add("")
    [void]$lines.Add("- Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    [void]$lines.Add("- Project root: ``$repoRoot``")
    [void]$lines.Add("- Total files scanned: $($allFiles.Count)")
    [void]$lines.Add("- Total project folder size: $(Format-FileSize -Bytes $totalBytes) ($totalBytes bytes)")
    [void]$lines.Add("- Report only: no files were deleted or modified outside this markdown report.")
    [void]$lines.Add("")
    [void]$lines.Add("## Source/Generated Split")
    [void]$lines.Add("")
    [void]$lines.Add((ConvertTo-MarkdownTableRow @("Category", "Size", "Bytes", "Files")))
    [void]$lines.Add("|---|---:|---:|---:|")
    foreach ($item in $categorySummary) {
        [void]$lines.Add((ConvertTo-MarkdownTableRow @($item.Name, (Format-FileSize -Bytes $item.Bytes), "$($item.Bytes)", "$($item.Count)")))
    }

    [void]$lines.Add("")
    [void]$lines.Add("## Top-Level Folder Sizes")
    [void]$lines.Add("")
    [void]$lines.Add((ConvertTo-MarkdownTableRow @("Top-level path", "Size", "Bytes", "Files")))
    [void]$lines.Add("|---|---:|---:|---:|")
    foreach ($item in $topLevel) {
        [void]$lines.Add((ConvertTo-MarkdownTableRow @($item.Name, (Format-FileSize -Bytes $item.Bytes), "$($item.Bytes)", "$($item.Count)")))
    }

    [void]$lines.Add("")
    [void]$lines.Add("## Largest Subfolders")
    [void]$lines.Add("")
    [void]$lines.Add((ConvertTo-MarkdownTableRow @("Folder", "Size", "Bytes", "Files")))
    [void]$lines.Add("|---|---:|---:|---:|")
    foreach ($item in $largestSubfolders) {
        [void]$lines.Add((ConvertTo-MarkdownTableRow @($item.Name, (Format-FileSize -Bytes $item.Bytes), "$($item.Bytes)", "$($item.Count)")))
    }

    [void]$lines.Add("")
    [void]$lines.Add("## Largest File Types")
    [void]$lines.Add("")
    [void]$lines.Add((ConvertTo-MarkdownTableRow @("Extension", "Total size", "Bytes", "Files")))
    [void]$lines.Add("|---|---:|---:|---:|")
    foreach ($item in $largestExtensions) {
        [void]$lines.Add((ConvertTo-MarkdownTableRow @($item.Name, (Format-FileSize -Bytes $item.Bytes), "$($item.Bytes)", "$($item.Count)")))
    }

    [void]$lines.Add("")
    [void]$lines.Add("## Largest Files")
    [void]$lines.Add("")
    [void]$lines.Add((ConvertTo-MarkdownTableRow @("File", "Size", "Bytes", "Category")))
    [void]$lines.Add("|---|---:|---:|---|")
    foreach ($file in ($allFiles | Sort-Object Length -Descending | Select-Object -First 30)) {
        [void]$lines.Add((ConvertTo-MarkdownTableRow @($file.RelativePath, (Format-FileSize -Bytes $file.Length), "$($file.Length)", $file.Category)))
    }

    [void]$lines.Add("")
    [void]$lines.Add("## Likely Cleanup Targets")
    [void]$lines.Add("")
    [void]$lines.Add((ConvertTo-MarkdownTableRow @("Target", "Size", "Bytes", "Files", "Notes")))
    [void]$lines.Add("|---|---:|---:|---:|---|")
    foreach ($target in $cleanupTargets) {
        $matches = @($allFiles | Where-Object { $_.RelativePath -match $target.Pattern })
        $bytes = ($matches | Measure-Object -Property Length -Sum).Sum
        if ($null -eq $bytes) {
            $bytes = 0
        }

        $note = if ($matches.Count -gt 0) { "Review before deleting; generated/runtime artifacts only when confirmed stale." } else { "No matching files found." }
        [void]$lines.Add((ConvertTo-MarkdownTableRow @($target.Name, (Format-FileSize -Bytes $bytes), "$bytes", "$($matches.Count)", $note)))
    }

    [void]$lines.Add("")
    [void]$lines.Add("## Notes For Review")
    [void]$lines.Add("")
    [void]$lines.Add("- This report intentionally includes `.git` metadata in the total so repository storage and working-folder storage can be compared.")
    [void]$lines.Add("- `Source/docs` includes tracked source, docs, configuration defaults, image templates, tests, installer definitions, and active scripts.")
    [void]$lines.Add("- `Generated/debug/build artifacts` includes build outputs, runtime evidence, packages, installers, logs, screenshots, and ZIP/EXE/MSI-style artifacts.")
    [void]$lines.Add("- Cleanup should be a separate, explicit task after reviewing this report.")

    Set-Content -LiteralPath $storageReportPath -Value $lines -Encoding UTF8
    Write-Host "Storage report path: $storageReportPath"
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

function Invoke-ProjectBrainRetentionCleanup {
    param(
        [string]$Folder,
        [int]$RetentionDays
    )

    if ($RetentionDays -lt 1) {
        throw "Project Brain ZIP retention days must be one or greater."
    }

    if (-not (Test-Path -LiteralPath $Folder -PathType Container)) {
        Write-Host "ProjectBrainRetentionCleanup deleted=0 retained=0 retentionDays=$RetentionDays folder=$Folder"
        return
    }

    $resolvedFolder = [System.IO.Path]::GetFullPath($Folder)
    $expectedFolder = [System.IO.Path]::GetFullPath($outputRoot)
    if (-not [string]::Equals($resolvedFolder.TrimEnd('\'), $expectedFolder.TrimEnd('\'), [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing Project Brain ZIP cleanup outside expected folder: $resolvedFolder"
    }

    $cutoff = (Get-Date).AddDays(-$RetentionDays)
    $packages = @(Get-ChildItem -LiteralPath $resolvedFolder -Filter "GoblinFarmer_ProjectBrain_*.zip" -File -ErrorAction SilentlyContinue)
    $deleted = 0
    $retained = 0

    foreach ($package in $packages) {
        if ($package.LastWriteTime -lt $cutoff) {
            Write-Host "ProjectBrainRetentionCleanup deletedPath=$($package.FullName)"
            Remove-Item -LiteralPath $package.FullName -Force
            $deleted++
        }
        else {
            $retained++
        }
    }

    Write-Host "ProjectBrainRetentionCleanup deleted=$deleted retained=$retained retentionDays=$RetentionDays folder=$resolvedFolder"
}

if ($StorageBreakdownOnly) {
    Write-StorageBreakdownReport
    exit 0
}

Write-Step "Preparing Project Brain package"
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

if (Test-Path -LiteralPath $stagingRoot -PathType Container) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

$includedFiles = New-Object System.Collections.Generic.List[string]
$skippedFiles = New-Object System.Collections.Generic.List[string]
$sourceFiles = New-Object System.Collections.Generic.List[string]

foreach ($relativePath in $explicitSourceFiles) {
    Add-UniqueRelativePath -Paths $sourceFiles -RelativePath $relativePath
}

if (Test-Path -LiteralPath $projectBrainSourceRoot -PathType Container) {
    foreach ($projectBrainFile in Get-ChildItem -LiteralPath $projectBrainSourceRoot -Filter "*.md" -File | Sort-Object Name) {
        Add-UniqueRelativePath -Paths $sourceFiles -RelativePath "Docs\ProjectBrain\$($projectBrainFile.Name)"
    }
}
else {
    [void]$skippedFiles.Add("Docs\ProjectBrain\*.md")
}

if (Test-Path -LiteralPath $agentDocsSourceRoot -PathType Container) {
    foreach ($agentDocFile in Get-ChildItem -LiteralPath $agentDocsSourceRoot -Filter "*.md" -File | Sort-Object Name) {
        Add-UniqueRelativePath -Paths $sourceFiles -RelativePath "Docs\Agent\$($agentDocFile.Name)"
    }
}
else {
    [void]$skippedFiles.Add("Docs\Agent\*.md")
}

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

    Invoke-ProjectBrainRetentionCleanup -Folder $outputRoot -RetentionDays $ProjectBrainZipRetentionDays

    if ($WriteStorageBreakdown) {
        Write-StorageBreakdownReport
    }
}
finally {
    if (Test-Path -LiteralPath $stagingRoot -PathType Container) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}

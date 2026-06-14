param(
    [string]$DebugPackagePath = "",

    [string]$ReviewVideoPath = "",

    [string]$DebugPackagesRoot = "",

    [string]$VideoReviewRoot = "",

    [string[]]$ReviewTimestamp = @(),

    [string[]]$ReviewNote = @(),

    [string]$ReviewNotesPath = "",

    [string]$OutputPath = "",

    [int]$WindowSeconds = 20,

    [int]$MaxRowsPerNote = 80,

    [int]$MaxEvidenceReferencesPerNote = 40,

    [string]$ScenarioDraftRoot = "",

    [switch]$SkipScenarioDraft
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptRoot "debug-analysis-tools.ps1")
$repoRoot = Split-Path -Parent $scriptRoot

function Resolve-ReviewPackagePath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location).Path $Path))
}

function Resolve-ReviewInputPath {
    param(
        [string]$Path,
        [string[]]$SearchRoots,
        [string]$Description
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ""
    }

    $resolved = Resolve-ReviewPackagePath $Path
    if (Test-Path -LiteralPath $resolved) {
        return $resolved
    }

    foreach ($root in @($SearchRoots)) {
        if ([string]::IsNullOrWhiteSpace($root) -or -not (Test-Path -LiteralPath $root -PathType Container)) {
            continue
        }

        $candidate = Join-Path $root $Path
        if (Test-Path -LiteralPath $candidate) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    throw "$Description path not found: $Path"
}

function Resolve-DefaultRoot {
    param(
        [string]$ConfiguredRoot,
        [string]$DefaultRelativePath
    )

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredRoot)) {
        return Resolve-ReviewPackagePath $ConfiguredRoot
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $DefaultRelativePath))
}

function Resolve-DebugPackageInput {
    param(
        [string]$Path,
        [string]$PackagesRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($Path)) {
        return Resolve-ReviewInputPath -Path $Path -SearchRoots @($PackagesRoot) -Description "Debug package"
    }

    $latest = Get-DgaLatestDebugPackage $PackagesRoot
    if ($null -eq $latest) {
        throw "No debug package path was supplied and no GoblinFarmer_Debug_*.zip files were found in: $PackagesRoot"
    }

    return $latest.FullName
}

function Resolve-ReviewVideoInput {
    param(
        [string]$Path,
        [string]$VideosRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($Path)) {
        return Resolve-ReviewInputPath -Path $Path -SearchRoots @($VideosRoot) -Description "Review video"
    }

    $latest = Get-DgaLatestReviewVideo $VideosRoot
    if ($null -eq $latest) {
        return ""
    }

    return $latest.FullName
}

function Get-DefaultOutputPath {
    param([string]$PackagePath)

    $directory = Split-Path -Parent $PackagePath
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($PackagePath)
    return Join-Path $directory "$baseName`_AutoCountNotes.zip"
}

function Write-ReviewNotesReports {
    param(
        [string]$PackageRoot,
        [string]$VideoPath,
        [string[]]$TimestampValues,
        [string[]]$NoteValues,
        [string]$NotesPath,
        [int]$Window,
        [int]$MaxRows,
        [int]$MaxEvidenceReferences
    )

    $outputRoot = Join-Path $PackageRoot "AutoCountReviewNotes"
    if (Test-Path -LiteralPath $outputRoot -PathType Container) {
        Remove-Item -LiteralPath $outputRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
    $data = New-DgaAutoCountReviewNotesTriageData `
        -Root $PackageRoot `
        -ReviewVideoPath $VideoPath `
        -ReviewTimestamp $TimestampValues `
        -ReviewNote $NoteValues `
        -ReviewNotesPath $NotesPath `
        -WindowSeconds $Window `
        -MaxRowsPerNote $MaxRows `
        -MaxEvidenceReferencesPerNote $MaxEvidenceReferences

    $markdownPath = Join-Path $outputRoot "auto-count-review-notes.md"
    $jsonPath = Join-Path $outputRoot "auto-count-review-notes.json"
    New-DgaAutoCountReviewNotesMarkdownContent -Data $data | Out-File -FilePath $markdownPath -Encoding utf8
    $data | ConvertTo-Json -Depth 9 | Out-File -FilePath $jsonPath -Encoding utf8

    @(
        "Auto-count review notes triage validation",
        "=========================================",
        "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')",
        "Input package root: $PackageRoot",
        "Review video path: $(if ([string]::IsNullOrWhiteSpace($VideoPath)) { 'none' } else { $VideoPath })",
        "Review timestamps: $([string]::Join('; ', @($TimestampValues)))",
        "Review notes: $([string]::Join('; ', @($NoteValues)))",
        "Review notes path: $(if ([string]::IsNullOrWhiteSpace($NotesPath)) { 'none' } else { $NotesPath })",
        "Window seconds: $Window",
        "Max rows per note: $MaxRows",
        "Max evidence references per note: $MaxEvidenceReferences",
        "",
        "Reports:",
        "- AutoCountReviewNotes/auto-count-review-notes.md",
        "- AutoCountReviewNotes/auto-count-review-notes.json",
        "",
        "Package-size policy:",
        "- Full videos are not added.",
        "- Bulk source image folders are not copied.",
        "- Reports reference existing DecisionBundles, GoblinTrackerEvents, Journal/Minimap crops, and notification traces already present in the package."
    ) | Out-File -FilePath (Join-Path $outputRoot "validation.txt") -Encoding utf8

    return [pscustomobject]@{
        MarkdownPath = $markdownPath
        JsonPath = $jsonPath
        NoteCount = @($data.Notes).Count
        MarkdownBytes = (Get-Item -LiteralPath $markdownPath).Length
        JsonBytes = (Get-Item -LiteralPath $jsonPath).Length
    }
}

function Write-AutoCountScenarioDraft {
    param(
        [string]$PackagePath,
        [string[]]$TimestampValues,
        [string[]]$NoteValues,
        [string]$NotesPath,
        [string]$DraftRoot
    )

    $draftScript = Join-Path $scriptRoot "draft-auto-count-scenario.ps1"
    if (-not (Test-Path -LiteralPath $draftScript -PathType Leaf)) {
        return [pscustomobject]@{
            Created = $false
            Reason = "draft-auto-count-scenario.ps1 missing"
            Output = ""
        }
    }

    $hasNotes = @($TimestampValues).Count -gt 0 -or @($NoteValues).Count -gt 0 -or -not [string]::IsNullOrWhiteSpace($NotesPath)
    if (-not $hasNotes) {
        return [pscustomobject]@{
            Created = $false
            Reason = "no review notes supplied"
            Output = ""
        }
    }

    $reviewNotes = New-Object System.Collections.Generic.List[string]
    foreach ($value in @($TimestampValues)) {
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            $reviewNotes.Add($value)
        }
    }

    foreach ($value in @($NoteValues)) {
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            $reviewNotes.Add($value)
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($NotesPath) -and (Test-Path -LiteralPath $NotesPath -PathType Leaf)) {
        foreach ($line in Get-Content -LiteralPath $NotesPath -ErrorAction SilentlyContinue) {
            if (-not [string]::IsNullOrWhiteSpace($line)) {
                $reviewNotes.Add($line)
            }
        }
    }

    $arguments = @(
        "-NoProfile",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        $draftScript,
        "-DebugPackagePath",
        $PackagePath,
        "-ReviewNotes",
        ([string]::Join(" | ", @($reviewNotes)))
    )

    $draftTimestamps = @($TimestampValues | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($draftTimestamps.Count -gt 0) {
        $arguments += "-Timestamp"
        foreach ($timestamp in $draftTimestamps) {
            $arguments += $timestamp
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($DraftRoot)) {
        $arguments += "-OutputRoot"
        $arguments += $DraftRoot
    }

    $output = & powershell.exe @arguments 2>&1
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        return [pscustomobject]@{
            Created = $false
            Reason = "draft helper failed with exit code $exitCode"
            Output = [string]::Join([Environment]::NewLine, @($output))
        }
    }

    return [pscustomobject]@{
        Created = $true
        Reason = "created"
        Output = [string]::Join([Environment]::NewLine, @($output))
    }
}

$resolvedDebugPackagesRoot = Resolve-DefaultRoot -ConfiguredRoot $DebugPackagesRoot -DefaultRelativePath "DebugPackages"
$resolvedVideoReviewRoot = Resolve-DefaultRoot -ConfiguredRoot $VideoReviewRoot -DefaultRelativePath "Video Clip Review"
$resolvedPackagePath = Resolve-DebugPackageInput -Path $DebugPackagePath -PackagesRoot $resolvedDebugPackagesRoot

$isZip = (Test-Path -LiteralPath $resolvedPackagePath -PathType Leaf) -and $resolvedPackagePath.EndsWith(".zip", [System.StringComparison]::OrdinalIgnoreCase)
$isFolder = Test-Path -LiteralPath $resolvedPackagePath -PathType Container
if (-not $isZip -and -not $isFolder) {
    throw "Debug package path must be a ZIP or expanded package folder: $resolvedPackagePath"
}

$tempRoot = ""
$packageRoot = $resolvedPackagePath
try {
    if ($isZip) {
        $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("GoblinFarmerAutoCountNotes_" + [Guid]::NewGuid().ToString("N"))
        New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
        Expand-Archive -LiteralPath $resolvedPackagePath -DestinationPath $tempRoot -Force
        $packageRoot = $tempRoot
    }

    $resolvedNotesPath = ""
    if (-not [string]::IsNullOrWhiteSpace($ReviewNotesPath)) {
        $resolvedNotesPath = Resolve-ReviewPackagePath $ReviewNotesPath
    }

    $resolvedReviewVideoPath = Resolve-ReviewVideoInput -Path $ReviewVideoPath -VideosRoot $resolvedVideoReviewRoot
    $resolvedScenarioDraftRoot = ""
    if (-not [string]::IsNullOrWhiteSpace($ScenarioDraftRoot)) {
        $resolvedScenarioDraftRoot = Resolve-ReviewPackagePath $ScenarioDraftRoot
    }

    $result = Write-ReviewNotesReports `
        -PackageRoot $packageRoot `
        -VideoPath $resolvedReviewVideoPath `
        -TimestampValues $ReviewTimestamp `
        -NoteValues $ReviewNote `
        -NotesPath $resolvedNotesPath `
        -Window $WindowSeconds `
        -MaxRows $MaxRowsPerNote `
        -MaxEvidenceReferences $MaxEvidenceReferencesPerNote

    $updatedPackagePath = ""
    if ($isZip -or -not [string]::IsNullOrWhiteSpace($OutputPath)) {
        if ([string]::IsNullOrWhiteSpace($OutputPath)) {
            $OutputPath = Get-DefaultOutputPath $resolvedPackagePath
        }

        $updatedPackagePath = Resolve-ReviewPackagePath $OutputPath
        if ($updatedPackagePath.Equals($resolvedPackagePath, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "OutputPath must not equal DebugPackagePath. Use a new ZIP path."
        }

        $outputDirectory = Split-Path -Parent $updatedPackagePath
        if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
            New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
        }

        if (Test-Path -LiteralPath $updatedPackagePath -PathType Leaf) {
            Remove-Item -LiteralPath $updatedPackagePath -Force
        }

        Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $updatedPackagePath -Force
    }

    $draftResult = [pscustomobject]@{
        Created = $false
        Reason = "skipped"
        Output = ""
    }
    if (-not $SkipScenarioDraft) {
        $draftResult = Write-AutoCountScenarioDraft `
            -PackagePath $resolvedPackagePath `
            -TimestampValues $ReviewTimestamp `
            -NoteValues $ReviewNote `
            -NotesPath $resolvedNotesPath `
            -DraftRoot $resolvedScenarioDraftRoot
    }
    else {
        $draftResult = [pscustomobject]@{
            Created = $false
            Reason = "skipped by -SkipScenarioDraft"
            Output = ""
        }
    }

    Write-Host "Auto-count review notes triage complete"
    Write-Host "Debug package search folder: $resolvedDebugPackagesRoot"
    Write-Host "Video review search folder: $resolvedVideoReviewRoot"
    Write-Host "Input package: $resolvedPackagePath"
    Write-Host "Review video: $(if ([string]::IsNullOrWhiteSpace($resolvedReviewVideoPath)) { 'none' } else { $resolvedReviewVideoPath })"
    Write-Host "Updated package: $(if ([string]::IsNullOrWhiteSpace($updatedPackagePath)) { 'folder updated in place' } else { $updatedPackagePath })"
    Write-Host "Markdown: AutoCountReviewNotes/auto-count-review-notes.md ($($result.MarkdownBytes) bytes)"
    Write-Host "JSON: AutoCountReviewNotes/auto-count-review-notes.json ($($result.JsonBytes) bytes)"
    Write-Host "Notes processed: $($result.NoteCount)"
    Write-Host "Scenario draft: $(if ($draftResult.Created) { 'created' } else { 'not created' })"
    Write-Host "Scenario draft reason: $($draftResult.Reason)"
    if (-not [string]::IsNullOrWhiteSpace($draftResult.Output)) {
        Write-Host $draftResult.Output
    }
    Write-Host "Validation: open AutoCountReviewNotes/validation.txt inside the package, or rerun this command with the same notes and confirm exit code 0."
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($tempRoot) -and (Test-Path -LiteralPath $tempRoot -PathType Container)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

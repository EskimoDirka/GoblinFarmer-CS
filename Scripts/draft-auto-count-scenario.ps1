param(
    [Parameter(Mandatory = $true)]
    [string]$DebugPackagePath,

    [string[]]$Timestamp = @(),

    [string]$ReviewNotes = "",

    [string]$OutputRoot = ""
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    param([string]$ScriptRoot)

    $candidate = Resolve-Path (Join-Path $ScriptRoot "..")
    return $candidate.Path
}

function Get-RelativePath {
    param(
        [string]$Root,
        [string]$Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $pathFull = [System.IO.Path]::GetFullPath($Path)
    if ($pathFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $pathFull.Substring($rootFull.Length).TrimStart('\', '/')
    }

    return $pathFull
}

function Get-ScenarioSafeName {
    param([string]$Value)

    $safe = [regex]::Replace($Value.ToLowerInvariant(), '[^a-z0-9]+', '-').Trim('-')
    if ([string]::IsNullOrWhiteSpace($safe)) {
        return "auto-count-review"
    }

    return $safe
}

function Expand-PackageIfNeeded {
    param(
        [string]$PackagePath,
        [string]$TempRoot
    )

    if (Test-Path -LiteralPath $PackagePath -PathType Container) {
        return (Resolve-Path $PackagePath).Path
    }

    if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf)) {
        throw "Debug package not found: $PackagePath"
    }

    if (-not $PackagePath.EndsWith(".zip", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Debug package path must be a ZIP or expanded package folder: $PackagePath"
    }

    New-Item -ItemType Directory -Path $TempRoot -Force | Out-Null
    Expand-Archive -LiteralPath $PackagePath -DestinationPath $TempRoot -Force
    return $TempRoot
}

function Get-MatchingLogLines {
    param(
        [string]$PackageRoot,
        [string[]]$TimestampFilters
    )

    $logs = @(Get-ChildItem -LiteralPath (Join-Path $PackageRoot "Logs") -Filter "*.log" -File -Recurse -ErrorAction SilentlyContinue)
    $matches = New-Object System.Collections.Generic.List[string]
    foreach ($log in $logs) {
        $lineNumber = 0
        foreach ($line in Get-Content -LiteralPath $log.FullName -ErrorAction SilentlyContinue) {
            $lineNumber++
            $interesting =
                $line.IndexOf("GoblinAutoCountAccepted", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                $line.IndexOf("GoblinAutoCountSuppressed", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                $line.IndexOf("GoblinEvidenceCandidateSelection", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                $line.IndexOf("GoblinLatencyTrace", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                $line.IndexOf("GoblinDecisionBundleSaved", [System.StringComparison]::OrdinalIgnoreCase) -ge 0
            if (-not $interesting) {
                continue
            }

            if ($TimestampFilters.Count -gt 0) {
                $matchesTimestamp = $false
                foreach ($timestamp in $TimestampFilters) {
                    if (-not [string]::IsNullOrWhiteSpace($timestamp) -and
                        $line.IndexOf($timestamp, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                        $matchesTimestamp = $true
                        break
                    }
                }

                if (-not $matchesTimestamp) {
                    continue
                }
            }

            $matches.Add("$(Get-RelativePath $PackageRoot $log.FullName):${lineNumber}: $line")
        }
    }

    if ($matches.Count -eq 0 -and $TimestampFilters.Count -gt 0) {
        return Get-MatchingLogLines -PackageRoot $PackageRoot -TimestampFilters @()
    }

    return @($matches | Select-Object -First 120)
}

$repoRoot = Resolve-RepoRoot $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "Debug\AutoCountScenarioDrafts"
}

$createdUtc = (Get-Date).ToUniversalTime()
$packageLeaf = Split-Path -Leaf ($DebugPackagePath.TrimEnd([char[]]@('\', '/')))
if ([string]::IsNullOrWhiteSpace($packageLeaf)) {
    $packageLeaf = "debug-package"
}

$packageBase = [System.IO.Path]::GetFileNameWithoutExtension($packageLeaf)
if ([string]::IsNullOrWhiteSpace($packageBase)) {
    $packageBase = $packageLeaf
}

$draftName = Get-ScenarioSafeName "$packageBase-$($createdUtc.ToString('yyyyMMdd-HHmmss'))"
$draftRoot = Join-Path $OutputRoot $draftName
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "GoblinFarmerAutoCountDraft_$([Guid]::NewGuid().ToString('N'))"

try {
    $packageRoot = Expand-PackageIfNeeded -PackagePath $DebugPackagePath -TempRoot $tempRoot
    New-Item -ItemType Directory -Path $draftRoot -Force | Out-Null

    $decisionBundles = @(Get-ChildItem -LiteralPath (Join-Path $packageRoot "Debug\GoblinEvidence\DecisionBundles") -Filter "decision-trace.txt" -File -Recurse -ErrorAction SilentlyContinue)
    $eventFiles = @(Get-ChildItem -LiteralPath (Join-Path $packageRoot "Debug\GoblinEvidence") -Filter "GoblinTrackerEvents.jsonl" -File -Recurse -ErrorAction SilentlyContinue)
    $crops = @(Get-ChildItem -LiteralPath (Join-Path $packageRoot "Debug\GoblinEvidence") -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '(Journal|Minimap)' -and $_.Extension -match '^\.(png|jpg|jpeg|bmp|txt|json)$' })
    $matchingLines = @(Get-MatchingLogLines -PackageRoot $packageRoot -TimestampFilters $Timestamp)

    $scenarioPath = Join-Path $draftRoot "draft-scenario.txt"
    @(
        "# Draft only. Review and manually promote to Tests\Fixtures\GoblinReplayScenarios\AutoCountMatrix when the assertion is understood.",
        "# SourcePackage=$DebugPackagePath",
        "# CreatedUtc=$($createdUtc.ToString('O'))",
        "# ReviewTimestamps=$([string]::Join(', ', $Timestamp))",
        "# ReviewNotes=$ReviewNotes",
        "Scenario=Draft auto-count review $($createdUtc.ToString('yyyyMMdd HHmmss'))",
        "# Add Step=... lines from matching DecisionBundles or replay-ready crops.",
        "# MatrixStep=<step name>|Counted=<true/false>|Decision=<Count/Duplicate/Stale/Block/Suppress>|Reason=<reason>|Source=<JournalCandidate/MinimapCandidate>|GoblinType=<type>|Area=<area>|FreshnessBucket=<Fresh/Stale/Duplicate/AreaLimit/Blocked/Pending>"
    ) | Out-File -FilePath $scenarioPath -Encoding utf8

    $explanationPath = Join-Path $draftRoot "draft-explanation.md"
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Auto-Count Scenario Draft")
    $lines.Add("")
    $lines.Add("- Source package: $DebugPackagePath")
    $lines.Add("- Expanded package root: $packageRoot")
    $lines.Add("- Timestamps: $([string]::Join(', ', $Timestamp))")
    $lines.Add("- Notes: $ReviewNotes")
    $lines.Add("")
    $lines.Add("## Evidence Summary")
    $lines.Add("- Decision bundles: $($decisionBundles.Count)")
    $lines.Add("- GoblinTrackerEvents.jsonl files: $($eventFiles.Count)")
    $lines.Add("- Journal/Minimap crop or metadata files: $($crops.Count)")
    $lines.Add("")
    $lines.Add("## Matching Log Context")
    if ($matchingLines.Count -eq 0) {
        $lines.Add("- No matching auto-count log lines were found. Use the OBS clip and package manually.")
    }
    else {
        foreach ($match in $matchingLines) {
            $lines.Add("- $match")
        }
    }
    $lines.Add("")
    $lines.Add("## Assertion To Add")
    $lines.Add("- Draft only. Decide the expected counted/suppressed outcome, goblin type, accepted area, source, reason, and freshness bucket before promoting.")
    $lines.Add("")
    $lines.Add("## Missing Or Live-Only Evidence")
    $lines.Add("- Scanner timing, Diablo focus/input, notification rendering, OBS/overlay UI, and missing-frame issues remain live-only.")
    $lines.Add("- If no replay-ready Journal/Minimap crops are present, create a template scenario or wait for a package with replay-ready DecisionBundles.")
    $lines | Out-File -FilePath $explanationPath -Encoding utf8

    Write-Host "Draft scenario folder: $draftRoot"
    Write-Host "Draft scenario: $scenarioPath"
    Write-Host "Draft explanation: $explanationPath"
}
finally {
    if (Test-Path -LiteralPath $tempRoot -PathType Container) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

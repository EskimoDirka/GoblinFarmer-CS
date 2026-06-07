$script:DgaTrackerMarkers = @(
    "GoblinDecisionTrace",
    "GoblinAutoCountAccepted",
    "GoblinAutoCountSuppressed",
    "GoblinCountAccepted",
    "GoblinCountSuppressed",
    "GoblinObservationCandidate",
    "GoblinObservationSummary",
    "LastObservationUpdated",
    "LastObservationCleared",
    "LastObservationUpdateSkipped",
    "LastObservationClearSkipped",
    "GoblinDecisionBundleSaved",
    "GoblinEncounterCaptureSaved",
    "EncounterCaptureSaved",
    "ObservationScanAttempted",
    "ObservationScanSkipped",
    "GoblinEvidenceCandidateCheck",
    "GoblinEvidenceScanResult",
    "ManualTestCountOverrideFreshObservationBypass",
    "GoblinTrackerNextTests"
)

function Format-DgaByteSize {
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

function Get-DgaPackageRelativePath {
    param(
        [string]$Root,
        [string]$Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $pathFull = [System.IO.Path]::GetFullPath($Path)
    if ($pathFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $pathFull.Substring($rootFull.Length).TrimStart('\', '/').Replace('\', '/')
    }

    return $pathFull.Replace('\', '/')
}

function Get-DgaFiles {
    param(
        [string]$Root,
        [string]$RelativePath = ""
    )

    $path = if ([string]::IsNullOrWhiteSpace($RelativePath)) { $Root } else { Join-Path $Root $RelativePath }
    if (-not (Test-Path -LiteralPath $path -PathType Container)) {
        return @()
    }

    return @(Get-ChildItem -LiteralPath $path -File -Recurse -ErrorAction SilentlyContinue)
}

function Get-DgaFileTotals {
    param([System.IO.FileInfo[]]$Files)

    $bytes = 0L
    foreach ($file in $Files) {
        $bytes += $file.Length
    }

    [pscustomobject]@{
        Count = $Files.Count
        Bytes = $bytes
        Size = Format-DgaByteSize $bytes
    }
}

function Get-DgaAllLogs {
    param([string]$Root)

    return @(Get-DgaFiles $Root "Logs" |
        Where-Object { $_.Extension -eq ".log" -or $_.Extension -eq ".txt" } |
        Sort-Object LastWriteTime, Name)
}

function Get-DgaLatestLog {
    param([string]$Root)

    return Get-DgaAllLogs $Root |
        Where-Object { $_.Extension -eq ".log" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

function Read-DgaKeyValueFile {
    param([string]$Path)

    $values = [ordered]@{}
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $values
    }

    foreach ($line in Get-Content -LiteralPath $Path -ErrorAction SilentlyContinue) {
        if ($line -match '^\s*([^=]+?)=(.*)$') {
            $values[$matches[1].Trim()] = $matches[2].Trim()
        }
    }

    return $values
}

function Convert-DgaLogFields {
    param([string]$Line)

    $fields = [ordered]@{}
    foreach ($part in ($Line -split ';')) {
        if ($part -match '(?<key>[A-Za-z][A-Za-z0-9_.-]*)=(?<value>.*)$') {
            $fields[$matches["key"].Trim()] = $matches["value"].Trim().Trim("'").Trim('"')
        }
    }

    return $fields
}

function Get-DgaLogTimestamp {
    param([string]$Line)

    if ($Line -match '^\[(?<timestamp>[^\]]+)\]') {
        return $matches["timestamp"]
    }

    if ($Line -match '^(?<timestamp>\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:\.\d+)?)') {
        return $matches["timestamp"]
    }

    return ""
}

function Get-DgaLogEvent {
    param([string]$Line)

    foreach ($marker in $script:DgaTrackerMarkers) {
        if ($Line.IndexOf($marker, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $marker
        }
    }

    return ""
}

function Get-DgaField {
    param(
        [hashtable]$Fields,
        [string[]]$Names
    )

    foreach ($name in $Names) {
        if ($Fields.Contains($name) -and -not [string]::IsNullOrWhiteSpace([string]$Fields[$name])) {
            return [string]$Fields[$name]
        }
    }

    return ""
}

function Convert-DgaMarkdownCell {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return $Value.Replace("|", "\|").Replace("`r", " ").Replace("`n", " ")
}

function Get-DgaManifestValue {
    param(
        [string[]]$Lines,
        [string]$Prefix
    )

    $line = $Lines | Where-Object { $_.StartsWith($Prefix, [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($line)) {
        return ""
    }

    return $line.Substring($Prefix.Length).Trim()
}

function Get-DgaTrackerTimelineRows {
    param([string]$Root)

    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($log in Get-DgaAllLogs $Root) {
        $lineNumber = 0
        foreach ($line in Get-Content -LiteralPath $log.FullName -ErrorAction SilentlyContinue) {
            $lineNumber++
            $event = Get-DgaLogEvent $line
            if ([string]::IsNullOrWhiteSpace($event)) {
                continue
            }

            $fields = Convert-DgaLogFields $line
            $rows.Add([pscustomobject]@{
                Timestamp = Get-DgaLogTimestamp $line
                Event = $event
                Source = Get-DgaField $fields @("source", "observationSource")
                GoblinType = Get-DgaField $fields @("goblinType", "type", "lastObservationType")
                Area = Get-DgaField $fields @("areaKey", "area", "resolvedArea", "lastObservationAreaKey")
                Decision = Get-DgaField $fields @("decision", "wouldCount", "accepted", "counted")
                Reason = Get-DgaField $fields @("reason", "blockReason", "duplicateReason")
                Evidence = Get-DgaField $fields @("evidenceHash", "evidenceId", "correlationId", "bundleId", "signature")
                Details = $line.Trim()
                File = Get-DgaPackageRelativePath $Root $log.FullName
                Line = $lineNumber
            })
        }
    }

    return $rows
}

function New-DgaGoblinTrackerTimelineContent {
    param(
        [string]$Root,
        [int]$MaxRows = 220
    )

    $rows = @(Get-DgaTrackerTimelineRows $Root)
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Goblin Tracker Timeline")
    $lines.Add("")
    $lines.Add("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
    $lines.Add("Root: $Root")
    $lines.Add("Events found: $($rows.Count)")
    $lines.Add("")

    if ($rows.Count -eq 0) {
        $lines.Add("No Goblin Tracker timeline markers were found in packaged logs.")
        return $lines
    }

    $lines.Add("## Event Counts")
    foreach ($group in $rows | Group-Object Event | Sort-Object -Property @{ Expression = "Count"; Descending = $true }, Name) {
        $lines.Add("- $($group.Name): $($group.Count)")
    }

    $lines.Add("")
    $lines.Add("## Latest Events")
    $lines.Add("")
    $lines.Add("| Time | Event | Source | Goblin | Area | Decision | Reason | Evidence | Log |")
    $lines.Add("| --- | --- | --- | --- | --- | --- | --- | --- | --- |")
    foreach ($row in ($rows | Select-Object -Last $MaxRows)) {
        $logCell = "{0}:{1}" -f (Convert-DgaMarkdownCell $row.File), $row.Line
        $lines.Add("| $(Convert-DgaMarkdownCell $row.Timestamp) | $(Convert-DgaMarkdownCell $row.Event) | $(Convert-DgaMarkdownCell $row.Source) | $(Convert-DgaMarkdownCell $row.GoblinType) | $(Convert-DgaMarkdownCell $row.Area) | $(Convert-DgaMarkdownCell $row.Decision) | $(Convert-DgaMarkdownCell $row.Reason) | $(Convert-DgaMarkdownCell $row.Evidence) | $logCell |")
    }

    $lines.Add("")
    $lines.Add("## Last Raw Lines")
    foreach ($row in ($rows | Select-Object -Last 60)) {
        $lines.Add("- [$($row.File):$($row.Line)] $($row.Details)")
    }

    return $lines
}

function New-DgaGoblinEvidenceHealthContent {
    param([string]$Root)

    $warnings = New-Object System.Collections.Generic.List[string]
    $lines = New-Object System.Collections.Generic.List[string]
    $evidenceFiles = @(Get-DgaFiles $Root "Debug\GoblinEvidence")
    $decisionBundles = @(Get-DgaFiles $Root "Debug\GoblinEvidence\DecisionBundles")
    $encounterCaptures = @(Get-DgaFiles $Root "Debug\GoblinEvidence\EncounterCaptures")
    $observationDiagnostics = @(Get-DgaFiles $Root "Debug\GoblinEvidence\ObservationDiagnostics")
    $rootEvidenceImages = @($evidenceFiles | Where-Object {
        $_.DirectoryName.TrimEnd('\', '/').EndsWith("Debug\GoblinEvidence", [System.StringComparison]::OrdinalIgnoreCase) -and
        $_.Name.StartsWith("GoblinEvidence_", [System.StringComparison]::OrdinalIgnoreCase)
    })
    $oversizedRootEvidenceImages = @($rootEvidenceImages | Where-Object { $_.Length -gt 1MB })
    $calibrationFullImages = @($evidenceFiles | Where-Object { $_.Name.EndsWith("_Full.png", [System.StringComparison]::OrdinalIgnoreCase) })
    $latestLog = Get-DgaLatestLog $Root
    $nextTestsRoot = Join-Path $Root "goblin-tracker-next-tests.txt"
    $nextTestsRuntime = Join-Path $Root "Debug\GoblinTrackerNextTests.txt"
    $retiredToken = "Goblin" + "Replay"
    $retiredArtifacts = @(Get-DgaFiles $Root | Where-Object {
        $_.FullName.IndexOf($retiredToken, [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $_.Name.IndexOf("replay", [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    })
    $successArtifacts = @(Get-DgaFiles $Root "Screenshots" | Where-Object {
        $_.FullName.IndexOf("Success", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $_.Name.IndexOf("_Success_", [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    })

    if ($null -eq $latestLog) {
        $warnings.Add("WARN: No .log file was found under Logs.")
    }

    if ($evidenceFiles.Count -eq 0) {
        $warnings.Add("WARN: No Debug/GoblinEvidence files are present in this package.")
    }

    if (-not (Test-Path -LiteralPath $nextTestsRoot -PathType Leaf) -and -not (Test-Path -LiteralPath $nextTestsRuntime -PathType Leaf)) {
        $warnings.Add("WARN: Next Tests metadata is missing; initialize the VS Debug Next Tests tab before packaging.")
    }

    if ($oversizedRootEvidenceImages.Count -gt 0) {
        $warnings.Add("WARN: $($oversizedRootEvidenceImages.Count) root GoblinEvidence event image(s) exceed 1 MB and may be excluded by package limits.")
    }

    if ($retiredArtifacts.Count -gt 0) {
        $warnings.Add("WARN: Found $($retiredArtifacts.Count) retired replay-related artifact(s); active packages should use live runtime evidence only.")
    }

    $lines.Add("Goblin Evidence Health")
    $lines.Add("======================")
    $lines.Add("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
    $lines.Add("Root: $Root")
    $lines.Add("")
    $lines.Add("Summary:")
    foreach ($item in @(
        [pscustomobject]@{ Name = "Debug/GoblinEvidence"; Files = $evidenceFiles },
        [pscustomobject]@{ Name = "DecisionBundles"; Files = $decisionBundles },
        [pscustomobject]@{ Name = "EncounterCaptures"; Files = $encounterCaptures },
        [pscustomobject]@{ Name = "ObservationDiagnostics"; Files = $observationDiagnostics },
        [pscustomobject]@{ Name = "Root GoblinEvidence event images"; Files = $rootEvidenceImages },
        [pscustomobject]@{ Name = "Calibration full images"; Files = $calibrationFullImages },
        [pscustomobject]@{ Name = "Success screenshots packaged"; Files = $successArtifacts }
    )) {
        $totals = Get-DgaFileTotals @($item.Files)
        $lines.Add("- $($item.Name): $($totals.Count) files, $($totals.Size) ($($totals.Bytes) bytes)")
    }

    $lines.Add("- Latest log: $(if ($null -ne $latestLog) { Get-DgaPackageRelativePath $Root $latestLog.FullName } else { 'none' })")
    $lines.Add("- Next Tests metadata: root=$(Test-Path -LiteralPath $nextTestsRoot -PathType Leaf); runtime=$(Test-Path -LiteralPath $nextTestsRuntime -PathType Leaf)")
    $lines.Add("")
    $lines.Add("Health:")
    if ($warnings.Count -eq 0) {
        $lines.Add("OK: No package-structure warnings were detected.")
    }
    else {
        foreach ($warning in $warnings) {
            $lines.Add($warning)
        }
    }

    $timelineRows = @(Get-DgaTrackerTimelineRows $Root)
    $accepted = @($timelineRows | Where-Object { $_.Event -eq "GoblinAutoCountAccepted" -or $_.Event -eq "GoblinCountAccepted" })
    $suppressed = @($timelineRows | Where-Object { $_.Event -eq "GoblinAutoCountSuppressed" -or $_.Event -eq "GoblinCountSuppressed" })
    $lines.Add("")
    $lines.Add("Decision marker counts:")
    $lines.Add("- Accepted count markers: $($accepted.Count)")
    $lines.Add("- Suppressed count markers: $($suppressed.Count)")
    foreach ($group in $timelineRows | Group-Object Event | Sort-Object -Property @{ Expression = "Count"; Descending = $true }, Name | Select-Object -First 20) {
        $lines.Add("- $($group.Name): $($group.Count)")
    }

    return $lines
}

function New-DgaDebugPackageAnalysisContent {
    param([string]$Root)

    $lines = New-Object System.Collections.Generic.List[string]
    $warnings = New-Object System.Collections.Generic.List[string]
    $files = @(Get-DgaFiles $Root)
    $totals = Get-DgaFileTotals $files
    $manifestPath = Join-Path $Root "debug-package-manifest.txt"
    $summaryPath = Join-Path $Root "goblin-tracker-summary.txt"
    $sessionPath = Join-Path $Root "session-info.txt"
    $manifestLines = if (Test-Path -LiteralPath $manifestPath -PathType Leaf) { @(Get-Content -LiteralPath $manifestPath -ErrorAction SilentlyContinue) } else { @() }
    $session = Read-DgaKeyValueFile $sessionPath
    $latestLog = Get-DgaLatestLog $Root
    $timelineRows = @(Get-DgaTrackerTimelineRows $Root)

    foreach ($required in @(
        [pscustomobject]@{ Path = $manifestPath; Name = "debug-package-manifest.txt" },
        [pscustomobject]@{ Path = $summaryPath; Name = "goblin-tracker-summary.txt" },
        [pscustomobject]@{ Path = $sessionPath; Name = "session-info.txt" }
    )) {
        if (-not (Test-Path -LiteralPath $required.Path -PathType Leaf)) {
            $warnings.Add("WARN: Missing $($required.Name).")
        }
    }

    if ($null -eq $latestLog) {
        $warnings.Add("WARN: Missing latest runtime log.")
    }

    $lines.Add("GoblinFarmer Debug Package Analysis")
    $lines.Add("===================================")
    $lines.Add("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
    $lines.Add("Root: $Root")
    $lines.Add("Package files: $($totals.Count), $($totals.Size) ($($totals.Bytes) bytes)")
    $lines.Add("")
    $lines.Add("Key package context:")
    $lines.Add("- Build configuration: $(Get-DgaManifestValue $manifestLines 'Startup build configuration:')")
    $lines.Add("- Launch kind: $(Get-DgaManifestValue $manifestLines 'Startup launch kind:')")
    $lines.Add("- VS/dev profile: $(Get-DgaManifestValue $manifestLines 'Startup VS/dev profile active:')")
    $lines.Add("- AppSettings path: $(Get-DgaManifestValue $manifestLines 'Startup AppSettings path:')")
    $lines.Add("- Session duration: $(Get-DgaManifestValue $manifestLines 'Session duration:')")
    $lines.Add("- Goblins found: $(Get-DgaManifestValue $manifestLines 'Goblin tracker goblins found:')")
    $lines.Add("- Observations: $(Get-DgaManifestValue $manifestLines 'Goblin observations:')")
    $lines.Add("- Last observation: $(Get-DgaManifestValue $manifestLines 'Goblin last observation:')")
    $lines.Add("- Latest log: $(if ($null -ne $latestLog) { Get-DgaPackageRelativePath $Root $latestLog.FullName } else { 'none' })")
    $lines.Add("")
    $lines.Add("Session counters:")
    foreach ($key in @(
        "GoblinCount",
        "GoblinObservationCount",
        "JournalObservationCount",
        "MinimapObservationCount",
        "EligibleObservationCount",
        "BlockedObservationCount",
        "DuplicateObservationCount",
        "LastGoblinObservationSource",
        "LastGoblinObservationType",
        "LastGoblinObservationAreaKey",
        "LastGoblinObservationReason"
    )) {
        if ($session.Contains($key)) {
            $lines.Add("- ${key}: $($session[$key])")
        }
    }

    $lines.Add("")
    $lines.Add("Goblin Tracker marker counts:")
    if ($timelineRows.Count -eq 0) {
        $warnings.Add("WARN: No Goblin Tracker markers were found in packaged logs.")
        $lines.Add("- none")
    }
    else {
        foreach ($group in $timelineRows | Group-Object Event | Sort-Object -Property @{ Expression = "Count"; Descending = $true }, Name) {
            $lines.Add("- $($group.Name): $($group.Count)")
        }
    }

    $lines.Add("")
    $lines.Add("Warnings:")
    if ($warnings.Count -eq 0) {
        $lines.Add("- none")
    }
    else {
        foreach ($warning in $warnings) {
            $lines.Add("- $warning")
        }
    }

    $lines.Add("")
    $lines.Add("Recommended review order:")
    $lines.Add("1. debug-package-analysis.txt")
    $lines.Add("2. goblin-tracker-timeline.md")
    $lines.Add("3. goblin-evidence-health.txt")
    $lines.Add("4. debug-package-manifest.txt")
    $lines.Add("5. goblin-tracker-summary.txt")
    $lines.Add("6. Logs/latest runtime log and Debug/GoblinEvidence evidence folders")

    return $lines
}

function Write-DgaAnalysisFiles {
    param(
        [string]$Root,
        [string]$OutputDirectory
    )

    if (-not (Test-Path -LiteralPath $OutputDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    }

    $analysisPath = Join-Path $OutputDirectory "debug-package-analysis.txt"
    $timelinePath = Join-Path $OutputDirectory "goblin-tracker-timeline.md"
    $healthPath = Join-Path $OutputDirectory "goblin-evidence-health.txt"

    New-DgaDebugPackageAnalysisContent -Root $Root | Out-File -FilePath $analysisPath -Encoding utf8
    New-DgaGoblinTrackerTimelineContent -Root $Root | Out-File -FilePath $timelinePath -Encoding utf8
    New-DgaGoblinEvidenceHealthContent -Root $Root | Out-File -FilePath $healthPath -Encoding utf8

    return [pscustomobject]@{
        AnalysisPath = $analysisPath
        TimelinePath = $timelinePath
        HealthPath = $healthPath
    }
}

function Get-DgaLatestDebugPackage {
    param([string]$DebugPackagesRoot)

    if (-not (Test-Path -LiteralPath $DebugPackagesRoot -PathType Container)) {
        return $null
    }

    return Get-ChildItem -LiteralPath $DebugPackagesRoot -Filter "GoblinFarmer_Debug_*.zip" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

function Expand-DgaPackageForAnalysis {
    param(
        [string]$PackagePath,
        [string]$TempRoot = ""
    )

    if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf)) {
        throw "Debug package not found: $PackagePath"
    }

    if ([string]::IsNullOrWhiteSpace($TempRoot)) {
        $TempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("GoblinFarmer.PackageAnalysis." + [Guid]::NewGuid().ToString("N"))
    }

    if (Test-Path -LiteralPath $TempRoot) {
        Remove-Item -LiteralPath $TempRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $TempRoot -Force | Out-Null
    Expand-Archive -LiteralPath $PackagePath -DestinationPath $TempRoot -Force
    return $TempRoot
}

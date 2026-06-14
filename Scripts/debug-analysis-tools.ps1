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
    "GoblinRecognitionCaptureSaved",
    "GoblinEvidenceCandidateSelection",
    "GoblinLatencyTrace",
    "GoblinOverlayDetectedAreaUpdated",
    "GoblinOverlayUpdated",
    "GoblinAutoCountNotificationQueued",
    "GoblinAutoCountNotificationDisplayed",
    "GoblinAutoCountNotificationDropped",
    "GoblinEvidenceJournalEngagedPromotedToKilledCompanion",
    "GoblinEvidenceJournalKilledCompanionRejected"
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

function Test-DgaPathUnder {
    param(
        [string]$Path,
        [string]$Root
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or [string]::IsNullOrWhiteSpace($Root)) {
        return $false
    }

    try {
        $pathFull = [System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
        $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
        return $pathFull.Equals($rootFull, [System.StringComparison]::OrdinalIgnoreCase) -or
            $pathFull.StartsWith($rootFull + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase) -or
            $pathFull.StartsWith($rootFull + [System.IO.Path]::AltDirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
    }
    catch {
        return $false
    }
}

function New-DgaEvidenceIndex {
    param([string]$Root)

    $allFiles = @(Get-DgaFiles $Root)
    $logsRoot = Join-Path $Root "Logs"
    $goblinEvidenceRoot = Join-Path $Root "Debug\GoblinEvidence"
    $decisionBundleRoot = Join-Path $Root "Debug\GoblinEvidence\DecisionBundles"
    $reviewEvidenceRoot = Join-Path $Root "ReviewEvidence"

    $logs = @($allFiles |
        Where-Object { (Test-DgaPathUnder $_.FullName $logsRoot) -and ($_.Extension -eq ".log" -or $_.Extension -eq ".txt") } |
        Sort-Object LastWriteTime, Name)
    $goblinEvidenceFiles = @($allFiles | Where-Object { Test-DgaPathUnder $_.FullName $goblinEvidenceRoot })
    $decisionBundleFiles = @($allFiles | Where-Object { Test-DgaPathUnder $_.FullName $decisionBundleRoot })
    $reviewEvidenceFiles = @($allFiles | Where-Object { Test-DgaPathUnder $_.FullName $reviewEvidenceRoot })
    $eventFiles = @($goblinEvidenceFiles | Where-Object { $_.Name.Equals("GoblinTrackerEvents.jsonl", [System.StringComparison]::OrdinalIgnoreCase) })
    $decisionTraceFiles = @($decisionBundleFiles | Where-Object { $_.Name.Equals("decision-trace.txt", [System.StringComparison]::OrdinalIgnoreCase) })
    $reviewFrames = @($reviewEvidenceFiles | Where-Object { $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' })
    $journalCrops = @($allFiles | Where-Object { $_.Name.IndexOf("Journal", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' })
    $minimapCrops = @($allFiles | Where-Object { $_.Name.IndexOf("Minimap", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' })

    return [pscustomobject]@{
        Root = $Root
        AllFiles = $allFiles
        Logs = $logs
        GoblinEvidenceFiles = $goblinEvidenceFiles
        GoblinTrackerEventsJsonlFiles = $eventFiles
        DecisionBundleFiles = $decisionBundleFiles
        DecisionTraceFiles = $decisionTraceFiles
        ReviewEvidenceFiles = $reviewEvidenceFiles
        ReviewEvidenceFrames = $reviewFrames
        JournalCrops = $journalCrops
        MinimapCrops = $minimapCrops
    }
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

function Get-DgaAutoCountTriageRows {
    param([string]$Root)

    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($row in @(Get-DgaTrackerTimelineRows $Root)) {
        $rows.Add($row)
    }

    $known = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($row in $rows) {
        [void]$known.Add("$($row.File):$($row.Line)")
    }

    foreach ($log in Get-DgaAllLogs $Root) {
        $lineNumber = 0
        foreach ($line in Get-Content -LiteralPath $log.FullName -ErrorAction SilentlyContinue) {
            $lineNumber++
            $relative = Get-DgaPackageRelativePath $Root $log.FullName
            if ($known.Contains("${relative}:$lineNumber")) {
                continue
            }

            if ($line.IndexOf("GoblinLatencyTrace", [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -and
                $line.IndexOf("GoblinEvidenceCandidateSelection", [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -and
                $line.IndexOf("Area resolved", [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -and
                $line.IndexOf("routeRawArea=", [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -and
                $line.IndexOf("currentArea=", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
                continue
            }

            $fields = Convert-DgaLogFields $line
            $event = if ($line.IndexOf("GoblinLatencyTrace", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                "GoblinLatencyTrace"
            }
            elseif ($line.IndexOf("GoblinEvidenceCandidateSelection", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                "GoblinEvidenceCandidateSelection"
            }
            elseif ($line.IndexOf("Area resolved", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                $line.IndexOf("routeRawArea=", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                "AreaResolutionContext"
            }
            else {
                "GoblinTrackerContext"
            }

            $rows.Add([pscustomobject]@{
                Timestamp = Get-DgaLogTimestamp $line
                Event = $event
                Source = Get-DgaField $fields @("source", "observationSource")
                GoblinType = Get-DgaField $fields @("goblinType", "type", "lastObservationType")
                Area = Get-DgaField $fields @("areaKey", "area", "resolvedArea", "currentArea", "routeRawArea", "acceptedArea")
                Decision = Get-DgaField $fields @("decision", "wouldCount", "accepted", "counted")
                Reason = Get-DgaField $fields @("reason", "blockReason", "duplicateReason")
                Evidence = Get-DgaField $fields @("evidenceHash", "evidenceId", "correlationId", "bundleId", "signature")
                Details = $line.Trim()
                File = $relative
                Line = $lineNumber
            })
        }
    }

    return $rows
}

function Test-DgaRowMatchesAny {
    param(
        [object]$Row,
        [string[]]$Patterns
    )

    $text = "$($Row.Event) $($Row.Decision) $($Row.Reason) $($Row.Details)"
    foreach ($pattern in $Patterns) {
        if ($text.IndexOf($pattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $true
        }
    }

    return $false
}

function Get-DgaMaxNumericField {
    param(
        [string]$Text,
        [string[]]$FieldNames
    )

    $max = 0.0
    foreach ($fieldName in $FieldNames) {
        foreach ($match in [regex]::Matches($Text, "$([regex]::Escape($fieldName))=(?<value>[0-9]+(?:\.[0-9]+)?)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
            $value = 0.0
            if ([double]::TryParse($match.Groups["value"].Value, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$value)) {
                if ($value -gt $max) {
                    $max = $value
                }
            }
        }
    }

    return $max
}

function New-DgaTriageReviewTarget {
    param(
        [object]$Row,
        [string]$Category,
        [string]$Reason
    )

    [pscustomobject]@{
        Category = $Category
        Reason = $Reason
        Timestamp = $Row.Timestamp
        Event = $Row.Event
        Source = $Row.Source
        GoblinType = $Row.GoblinType
        Area = $Row.Area
        Decision = $Row.Decision
        DecisionReason = $Row.Reason
        Log = "$($Row.File):$($Row.Line)"
        Details = $Row.Details
    }
}

function New-DgaAutoCountTriageData {
    param([string]$Root)

    $rows = @(Get-DgaAutoCountTriageRows $Root)
    $evidenceIndex = New-DgaEvidenceIndex $Root

    $accepted = @($rows | Where-Object { $_.Event -eq "GoblinAutoCountAccepted" -or $_.Event -eq "GoblinCountAccepted" })
    $suppressed = @($rows | Where-Object { $_.Event -eq "GoblinAutoCountSuppressed" -or $_.Event -eq "GoblinCountSuppressed" })
    $pending = @($rows | Where-Object { Test-DgaRowMatchesAny $_ @("JournalPending", "PendingKilled", "PendingMinimap") })
    $stale = @($rows | Where-Object { Test-DgaRowMatchesAny $_ @("Stale", "Carryover", "HistoryRow") })
    $duplicate = @($rows | Where-Object { Test-DgaRowMatchesAny $_ @("Duplicate", "AlreadyAutoCounted", "EvidenceAlreadyAutoCounted", "EncounterAlreadyAutoCounted") })
    $areaLimit = @($rows | Where-Object { Test-DgaRowMatchesAny $_ @("AreaLimitReached") })
    $blocked = @($rows | Where-Object { Test-DgaRowMatchesAny $_ @("BlockedArea") })
    $areaResolution = @($rows | Where-Object { Test-DgaRowMatchesAny $_ @("Area resolved", "routeRawArea=", "currentArea=", "acceptedArea=", "displayLocation=", "GoblinOverlayDetectedAreaUpdated") })
    $delayed = @($rows | Where-Object {
        $_.Event -eq "GoblinLatencyTrace" -and
        (Get-DgaMaxNumericField $_.Details @("elapsedMs", "queueAgeMs", "countToDisplayMs", "detectedToDisplayMs", "ageMs")) -ge 1000
    })

    $targets = New-Object System.Collections.Generic.List[object]
    foreach ($row in $suppressed) {
        $confidence = Get-DgaMaxNumericField $row.Details @("evidenceConfidence", "confidence", "bestConfidence")
        if ($confidence -ge 0.85) {
            $targets.Add((New-DgaTriageReviewTarget $row "HighConfidenceSuppressedEvidence" "Suppressed evidence confidence >= 0.85"))
        }

        $isJournalRow = Test-DgaRowMatchesAny $row @("Journal")
        $isStaleOrCrossAreaRow = Test-DgaRowMatchesAny $row @("Stale", "EncounterAlreadyAutoCounted", "AreaChanged", "CrossArea", "HistoryRow")
        if ($isJournalRow -and $isStaleOrCrossAreaRow) {
            $targets.Add((New-DgaTriageReviewTarget $row "CrossAreaJournalSuppression" "Journal suppression with stale/cross-area/duplicate shape"))
        }

        $isEmptyOrShiftedBucket = Test-DgaRowMatchesAny $row @("emptyBucket", "empty-line", "LineBucket=0", "bucket=0", "shifted", "HistoryRow")
        if ($isEmptyOrShiftedBucket) {
            $targets.Add((New-DgaTriageReviewTarget $row "EmptyOrShiftedLineBucket" "Suppression references an empty, shifted, or history Journal row bucket"))
        }
    }

    foreach ($row in $rows) {
        $isTwoCountAreaDecision = Test-DgaRowMatchesAny $row @("Pandemonium Fortress", "Stinging Winds", "areaLimit=2", "AreaLimitReached", "PfMultiCount")
        if ($isTwoCountAreaDecision) {
            $targets.Add((New-DgaTriageReviewTarget $row "TwoCountAreaDecision" "PF1/PF2/Stinging Winds count-limit or duplicate decision"))
        }

        $hasBlockedAreaContext = Test-DgaRowMatchesAny $row @("BlockedArea", "Ancient Waterway", "New Tristram", "Caldeum Bazaar", "Flooded Causeway")
        if ($hasBlockedAreaContext) {
            $targets.Add((New-DgaTriageReviewTarget $row "BlockedAreaContext" "Blocked/current-area context present"))
        }

        $isAcceptedRow = $row.Event -eq "GoblinAutoCountAccepted" -or $row.Event -eq "GoblinCountAccepted"
        $hasStaleAgeContext = Test-DgaRowMatchesAny $row @("Stale", "old", "evidenceAgeSeconds=4", "evidenceAgeSeconds=5", "evidenceAgeSeconds=6", "evidenceAgeSeconds=7", "evidenceAgeSeconds=8", "evidenceAgeSeconds=9")
        $hasAcceptedCurrentAreaContext = Test-DgaRowMatchesAny $row @("acceptedArea=", "currentAreaAtAcceptance=")
        if ($isAcceptedRow -and ($hasStaleAgeContext -or $hasAcceptedCurrentAreaContext)) {
            $targets.Add((New-DgaTriageReviewTarget $row "AcceptedCountAreaOrAgeReview" "Accepted count includes stale/age/current-area context worth checking"))
        }

        $isMinimapRow = Test-DgaRowMatchesAny $row @("Minimap")
        $isAutoCountedDuplicateRow = Test-DgaRowMatchesAny $row @("EncounterAlreadyAutoCounted", "EvidenceAlreadyAutoCounted")
        $hasAreaFields = Test-DgaRowMatchesAny $row @("currentArea", "areaKey", "acceptedArea")
        if ($isMinimapRow -and $isAutoCountedDuplicateRow -and $hasAreaFields) {
            $targets.Add((New-DgaTriageReviewTarget $row "SameTypeCrossAreaMinimapCollision" "Minimap duplicate suppression may need same-area/cross-area review"))
        }
    }

    foreach ($row in $delayed) {
        $targets.Add((New-DgaTriageReviewTarget $row "DelayedNotificationOrAcceptance" "Latency trace exceeded 1000 ms"))
    }

    $targetRows = @($targets | Sort-Object Timestamp, Category -Unique | Select-Object -First 160)
    [pscustomobject]@{
        Generated = (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz")
        Root = $Root
        SourceAvailability = [pscustomobject]@{
            Logs = $evidenceIndex.Logs.Count
            GoblinTrackerEventsJsonl = $evidenceIndex.GoblinTrackerEventsJsonlFiles.Count
            DecisionBundles = $evidenceIndex.DecisionTraceFiles.Count
            ReviewEvidenceFiles = $evidenceIndex.ReviewEvidenceFiles.Count
            ReviewEvidenceFrames = $evidenceIndex.ReviewEvidenceFrames.Count
            JournalCrops = $evidenceIndex.JournalCrops.Count
            MinimapCrops = $evidenceIndex.MinimapCrops.Count
        }
        Groups = [pscustomobject]@{
            AcceptedCounts = $accepted.Count
            SuppressedCandidates = $suppressed.Count
            PendingEvidence = $pending.Count
            StaleJournalRows = $stale.Count
            DuplicateSuppressions = $duplicate.Count
            AreaLimitSuppressions = $areaLimit.Count
            BlockedAreaSuppressions = $blocked.Count
            AreaResolutionChanges = $areaResolution.Count
            DelayedCountToNotificationPaths = $delayed.Count
        }
        ReviewTargets = $targetRows
        LiveOnly = @(
            "Scanner timing and frame arrival order",
            "Diablo focus/input state",
            "Notification rendering and sound playback",
            "OBS/overlay UI behavior",
            "Missing-frame or no-crop image recognition issues"
        )
        PackageSizePolicy = [pscustomobject]@{
            Included = "goblin-auto-count-triage.md and goblin-auto-count-triage.json"
            Excluded = "Full videos, bulk source image folders, legacy replay image folders"
            Notes = "Triage reports are text/JSON only; package size summary reports their byte contribution after ZIP creation."
        }
    }
}

function New-DgaAutoCountTriageMarkdownContent {
    param([object]$Data)

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Goblin Auto-Count Triage")
    $lines.Add("")
    $lines.Add("Generated: $($Data.Generated)")
    $lines.Add("Root: $($Data.Root)")
    $lines.Add("")
    $lines.Add("## Source Availability")
    foreach ($property in $Data.SourceAvailability.PSObject.Properties) {
        $lines.Add("- $($property.Name): $($property.Value)")
    }

    $lines.Add("")
    $lines.Add("## Event Groups")
    foreach ($property in $Data.Groups.PSObject.Properties) {
        $lines.Add("- $($property.Name): $($property.Value)")
    }

    $lines.Add("")
    $lines.Add("## Likely Review Targets")
    if (@($Data.ReviewTargets).Count -eq 0) {
        $lines.Add("- none")
    }
    else {
        $lines.Add("| Category | Time | Event | Goblin | Area | Decision | Reason | Log | Why |")
        $lines.Add("| --- | --- | --- | --- | --- | --- | --- | --- | --- |")
        foreach ($target in @($Data.ReviewTargets | Select-Object -First 80)) {
            $lines.Add("| $(Convert-DgaMarkdownCell $target.Category) | $(Convert-DgaMarkdownCell $target.Timestamp) | $(Convert-DgaMarkdownCell $target.Event) | $(Convert-DgaMarkdownCell $target.GoblinType) | $(Convert-DgaMarkdownCell $target.Area) | $(Convert-DgaMarkdownCell $target.Decision) | $(Convert-DgaMarkdownCell $target.DecisionReason) | $(Convert-DgaMarkdownCell $target.Log) | $(Convert-DgaMarkdownCell $target.Reason) |")
        }
    }

    $lines.Add("")
    $lines.Add("## Live-Only")
    foreach ($item in $Data.LiveOnly) {
        $lines.Add("- $item")
    }

    $lines.Add("")
    $lines.Add("## Package Size Policy")
    $lines.Add("- Included: $($Data.PackageSizePolicy.Included)")
    $lines.Add("- Excluded: $($Data.PackageSizePolicy.Excluded)")
    $lines.Add("- Notes: $($Data.PackageSizePolicy.Notes)")
    return $lines
}

function Convert-DgaReviewTimestampToSeconds {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return -1.0
    }

    $trimmed = $Value.Trim()
    $parts = @($trimmed -split ':')
    if ($parts.Count -lt 2 -or $parts.Count -gt 3) {
        return -1.0
    }

    $hours = 0.0
    $minutes = 0.0
    $seconds = 0.0
    try {
        if ($parts.Count -eq 3) {
            $hours = [double]::Parse($parts[0], [System.Globalization.CultureInfo]::InvariantCulture)
            $minutes = [double]::Parse($parts[1], [System.Globalization.CultureInfo]::InvariantCulture)
            $seconds = [double]::Parse($parts[2], [System.Globalization.CultureInfo]::InvariantCulture)
        }
        else {
            $minutes = [double]::Parse($parts[0], [System.Globalization.CultureInfo]::InvariantCulture)
            $seconds = [double]::Parse($parts[1], [System.Globalization.CultureInfo]::InvariantCulture)
        }
    }
    catch {
        return -1.0
    }

    return ($hours * 3600.0) + ($minutes * 60.0) + $seconds
}

function Try-ParseDgaLocalTime {
    param(
        [string]$Value,
        [ref]$Parsed
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    $styles = [System.Globalization.DateTimeStyles]::AllowWhiteSpaces -bor [System.Globalization.DateTimeStyles]::AssumeLocal
    $candidate = [DateTime]::MinValue
    if ([DateTime]::TryParse($Value, [System.Globalization.CultureInfo]::InvariantCulture, $styles, [ref]$candidate)) {
        $Parsed.Value = $candidate
        return $true
    }

    return $false
}

function Get-DgaReviewVideoStart {
    param([string]$Root)

    $manifestPath = Join-Path $Root "ReviewEvidence\manifest.json"
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        return $null
    }

    try {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        $text = [string]$manifest.AutoReviewVideoStartLocal
        if ([string]::IsNullOrWhiteSpace($text)) {
            $text = [string]$manifest.VideoStartLocal
        }

        $parsed = [DateTime]::MinValue
        if (Try-ParseDgaLocalTime $text ([ref]$parsed)) {
            return $parsed
        }
    }
    catch {
        return $null
    }

    return $null
}

function Find-DgaFfprobeCommand {
    $command = Get-Command ffprobe -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    $command = Get-Command ffprobe.cmd -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    return ""
}

function Get-DgaVideoDurationSeconds {
    param([string]$VideoPath)

    $ffprobe = Find-DgaFfprobeCommand
    if ([string]::IsNullOrWhiteSpace($ffprobe) -or -not (Test-Path -LiteralPath $VideoPath -PathType Leaf)) {
        return 0.0
    }

    try {
        $output = & $ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 $VideoPath 2>$null
        $text = (@($output) | Select-Object -First 1).ToString()
        $duration = 0.0
        if ([double]::TryParse($text, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$duration)) {
            return [Math]::Max(0.0, $duration)
        }
    }
    catch {
        return 0.0
    }

    return 0.0
}

function Try-ParseDgaVideoStartFromName {
    param(
        [string]$FileName,
        [ref]$StartTime
    )

    $match = [regex]::Match(
        $FileName,
        '(?<year>20\d{2})[.\-_](?<month>\d{2})[.\-_](?<day>\d{2})[\s_\-]+(?<hour>\d{2})[.\-_:](?<minute>\d{2})[.\-_:](?<second>\d{2})(?:[.\-_:](?<fraction>\d{1,3}))?')
    if (-not $match.Success) {
        return $false
    }

    try {
        $fraction = if ($match.Groups["fraction"].Success) { $match.Groups["fraction"].Value.PadRight(3, '0').Substring(0, 3) } else { "000" }
        $text = "$($match.Groups["year"].Value)-$($match.Groups["month"].Value)-$($match.Groups["day"].Value) $($match.Groups["hour"].Value):$($match.Groups["minute"].Value):$($match.Groups["second"].Value).$fraction"
        $StartTime.Value = [DateTime]::ParseExact($text, "yyyy-MM-dd HH:mm:ss.fff", [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::AssumeLocal)
        return $true
    }
    catch {
        return $false
    }
}

function Get-DgaReviewVideoStartFromPath {
    param([string]$ReviewVideoPath)

    if ([string]::IsNullOrWhiteSpace($ReviewVideoPath) -or -not (Test-Path -LiteralPath $ReviewVideoPath -PathType Leaf)) {
        return $null
    }

    $file = Get-Item -LiteralPath $ReviewVideoPath
    $parsedStart = [DateTime]::MinValue
    if (Try-ParseDgaVideoStartFromName $file.Name ([ref]$parsedStart)) {
        return [pscustomobject]@{
            Start = $parsedStart
            Source = "FilenameTimestamp"
            DurationSeconds = 0.0
        }
    }

    $durationSeconds = Get-DgaVideoDurationSeconds $file.FullName
    if ($durationSeconds -gt 0) {
        return [pscustomobject]@{
            Start = $file.LastWriteTime.AddSeconds(-1 * $durationSeconds)
            Source = "LastWriteMinusFfprobeDuration"
            DurationSeconds = $durationSeconds
        }
    }

    return [pscustomobject]@{
        Start = $file.CreationTime
        Source = "CreationTimeFallback"
        DurationSeconds = 0.0
    }
}

function Split-DgaReviewEntry {
    param(
        [string]$Value,
        [int]$Index,
        [string]$Source
    )

    $timestamp = ""
    $note = $Value.Trim()
    $separatorIndex = $note.IndexOf("=")
    if ($separatorIndex -ge 0) {
        $timestamp = $note.Substring(0, $separatorIndex).Trim()
        $note = $note.Substring($separatorIndex + 1).Trim()
    }
    elseif ($note -match '^\s*(?<timestamp>(?:\d{1,2}:)?\d{1,2}:\d{2}(?:\.\d{1,3})?)\s*[-:]\s*(?<note>.*)$') {
        $timestamp = $matches["timestamp"].Trim()
        $note = $matches["note"].Trim()
    }

    [pscustomobject]@{
        Index = $Index
        Timestamp = $timestamp
        Note = $note
        Source = $Source
        Raw = $Value
    }
}

function Get-DgaReviewNoteEntries {
    param(
        [string[]]$ReviewTimestamp = @(),
        [string[]]$ReviewNote = @(),
        [string]$ReviewNotesPath = ""
    )

    $entries = New-Object System.Collections.Generic.List[object]
    $index = 0
    foreach ($value in @($ReviewTimestamp)) {
        if ([string]::IsNullOrWhiteSpace($value)) {
            continue
        }

        $index++
        [void]$entries.Add((Split-DgaReviewEntry $value $index "ReviewTimestamp"))
    }

    foreach ($value in @($ReviewNote)) {
        if ([string]::IsNullOrWhiteSpace($value)) {
            continue
        }

        $index++
        [void]$entries.Add((Split-DgaReviewEntry $value $index "ReviewNote"))
    }

    if (-not [string]::IsNullOrWhiteSpace($ReviewNotesPath) -and (Test-Path -LiteralPath $ReviewNotesPath -PathType Leaf)) {
        foreach ($line in Get-Content -LiteralPath $ReviewNotesPath -ErrorAction SilentlyContinue) {
            if ([string]::IsNullOrWhiteSpace($line)) {
                continue
            }

            $index++
            [void]$entries.Add((Split-DgaReviewEntry $line $index "ReviewNotesPath"))
        }
    }

    return $entries.ToArray()
}

function Get-DgaReviewTerms {
    param([object]$Note)

    $terms = New-Object System.Collections.Generic.List[string]
    foreach ($value in @($Note.Note, $Note.Timestamp)) {
        if ([string]::IsNullOrWhiteSpace($value)) {
            continue
        }

        foreach ($part in ($value -split '[^A-Za-z0-9]+')) {
            if ($part.Length -lt 4) {
                continue
            }

            if (@("area", "note", "timestamp", "goblin", "count", "should", "when", "with").Contains($part.ToLowerInvariant())) {
                continue
            }

            if (-not $terms.Contains($part)) {
                $terms.Add($part)
            }
        }
    }

    return $terms.ToArray()
}

function Test-DgaTextMatchesAnyTerm {
    param(
        [string]$Text,
        [string[]]$Terms
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $false
    }

    foreach ($term in @($Terms)) {
        if (-not [string]::IsNullOrWhiteSpace($term) -and
            $Text.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $true
        }
    }

    return $false
}

function Get-DgaNoteTargetTime {
    param(
        [object]$Note,
        [object]$VideoStart
    )

    $parsed = [DateTime]::MinValue
    if (Try-ParseDgaLocalTime $Note.Timestamp ([ref]$parsed)) {
        return $parsed
    }

    $offsetSeconds = Convert-DgaReviewTimestampToSeconds $Note.Timestamp
    if ($offsetSeconds -ge 0 -and $null -ne $VideoStart) {
        return ([DateTime]$VideoStart).AddSeconds($offsetSeconds)
    }

    return $null
}

function Get-DgaRowsForReviewNote {
    param(
        [object[]]$Rows,
        [object]$Note,
        [object]$VideoStart,
        [int]$WindowSeconds,
        [int]$MaxRows
    )

    $targetTime = Get-DgaNoteTargetTime -Note $Note -VideoStart $VideoStart
    $terms = @(Get-DgaReviewTerms $Note)
    $matches = New-Object System.Collections.Generic.List[object]
    foreach ($row in @($Rows)) {
        $include = $false
        if ($null -ne $targetTime) {
            $rowTime = [DateTime]::MinValue
            if (Try-ParseDgaLocalTime $row.Timestamp ([ref]$rowTime)) {
                $include = [Math]::Abs(($rowTime - ([DateTime]$targetTime)).TotalSeconds) -le $WindowSeconds
            }
        }

        if (-not $include -and $terms.Count -gt 0) {
            $include = Test-DgaTextMatchesAnyTerm $row.Details $terms
        }

        if ($include) {
            $matches.Add($row)
        }
    }

    return @($matches | Sort-Object Timestamp, File, Line | Select-Object -First $MaxRows)
}

function Get-DgaSuppressionReasonSummary {
    param([object[]]$Rows)

    $reasons = New-Object System.Collections.Generic.List[string]
    foreach ($row in @($Rows)) {
        $isSuppressionEvent = $row.Event -eq "GoblinAutoCountSuppressed" -or $row.Event -eq "GoblinCountSuppressed"
        $hasSuppressionMarker = Test-DgaRowMatchesAny $row @("Suppress", "Duplicate", "Stale", "BlockedArea", "AreaLimitReached")
        if (-not $isSuppressionEvent -and -not $hasSuppressionMarker) {
            continue
        }

        $reason = if ([string]::IsNullOrWhiteSpace($row.Reason)) { $row.Decision } else { $row.Reason }
        if ([string]::IsNullOrWhiteSpace($reason)) { $reason = "Unknown" }
        $reasons.Add($reason)
    }

    return @($reasons |
        Group-Object |
        Sort-Object -Property @{ Expression = "Count"; Descending = $true }, @{ Expression = "Name"; Ascending = $true } |
        ForEach-Object { [pscustomobject]@{ Reason = $_.Name; Count = $_.Count } })
}

function Get-DgaEvidenceReferencesForReviewNote {
    param(
        [string]$Root,
        [object]$Note,
        [object[]]$Rows,
        [int]$MaxReferences,
        [object]$EvidenceIndex = $null
    )

    if ($null -eq $EvidenceIndex) {
        $EvidenceIndex = New-DgaEvidenceIndex $Root
    }

    $terms = New-Object System.Collections.Generic.List[string]
    foreach ($term in @(Get-DgaReviewTerms $Note)) {
        if (-not $terms.Contains($term)) {
            $terms.Add($term)
        }
    }

    foreach ($row in @($Rows)) {
        foreach ($value in @($row.Evidence, $row.GoblinType, $row.Area, $row.Reason)) {
            if ([string]::IsNullOrWhiteSpace($value)) {
                continue
            }

            foreach ($part in ($value -split '[^A-Za-z0-9]+')) {
                if ($part.Length -ge 4 -and -not $terms.Contains($part)) {
                    $terms.Add($part)
                }
            }
        }
    }

    $references = New-Object System.Collections.Generic.List[object]
    foreach ($file in @($EvidenceIndex.DecisionBundleFiles |
        Where-Object { $_.Extension -match '^\.(txt|json|png|jpg|jpeg|bmp)$' } |
        Sort-Object FullName)) {
        $relative = Get-DgaPackageRelativePath $Root $file.FullName
        $text = $relative
        if ($file.Extension -match '^\.(txt|json)$') {
            try {
                $text = $text + " " + (Get-Content -LiteralPath $file.FullName -Raw -ErrorAction Stop)
            }
            catch {
            }
        }

        if ($terms.Count -eq 0 -or (Test-DgaTextMatchesAnyTerm $text $terms)) {
            $references.Add([pscustomobject]@{
                Kind = "DecisionBundle"
                Path = $relative
                SizeBytes = $file.Length
            })
        }

        if ($references.Count -ge $MaxReferences) {
            break
        }
    }

    foreach ($file in @($EvidenceIndex.GoblinEvidenceFiles |
        Where-Object {
            ($_.Name.IndexOf("Journal", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
             $_.Name.IndexOf("Minimap", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
             $_.Name.Equals("GoblinTrackerEvents.jsonl", [System.StringComparison]::OrdinalIgnoreCase)) -and
            $_.FullName.IndexOf("DecisionBundles", [System.StringComparison]::OrdinalIgnoreCase) -lt 0
        } |
        Sort-Object FullName)) {
        if ($references.Count -ge $MaxReferences) {
            break
        }

        $references.Add([pscustomobject]@{
            Kind = if ($file.Name.Equals("GoblinTrackerEvents.jsonl", [System.StringComparison]::OrdinalIgnoreCase)) { "GoblinTrackerEvents" } else { "JournalMinimapCrop" }
            Path = Get-DgaPackageRelativePath $Root $file.FullName
            SizeBytes = $file.Length
        })
    }

    return $references.ToArray()
}

function New-DgaAutoCountReviewNotesTriageData {
    param(
        [string]$Root,
        [string]$ReviewVideoPath = "",
        [string[]]$ReviewTimestamp = @(),
        [string[]]$ReviewNote = @(),
        [string]$ReviewNotesPath = "",
        [int]$WindowSeconds = 20,
        [int]$MaxRowsPerNote = 80,
        [int]$MaxEvidenceReferencesPerNote = 40
    )

    $rows = @(Get-DgaAutoCountTriageRows $Root)
    $notes = @(Get-DgaReviewNoteEntries -ReviewTimestamp $ReviewTimestamp -ReviewNote $ReviewNote -ReviewNotesPath $ReviewNotesPath)
    $evidenceIndex = New-DgaEvidenceIndex $Root
    $videoStart = $null
    $videoStartSource = ""
    $resolvedReviewVideoPath = ""
    if (-not [string]::IsNullOrWhiteSpace($ReviewVideoPath) -and (Test-Path -LiteralPath $ReviewVideoPath -PathType Leaf)) {
        $resolvedReviewVideoPath = [System.IO.Path]::GetFullPath($ReviewVideoPath)
        $videoStartInfo = Get-DgaReviewVideoStartFromPath $resolvedReviewVideoPath
        if ($null -ne $videoStartInfo) {
            $videoStart = $videoStartInfo.Start
            $videoStartSource = $videoStartInfo.Source
        }
    }

    if ($null -eq $videoStart) {
        $videoStart = Get-DgaReviewVideoStart $Root
        if ($null -ne $videoStart) {
            $videoStartSource = "PackageReviewEvidenceManifest"
        }
    }

    $noteReports = New-Object System.Collections.Generic.List[object]
    foreach ($note in $notes) {
        $noteRows = @(Get-DgaRowsForReviewNote -Rows $rows -Note $note -VideoStart $videoStart -WindowSeconds $WindowSeconds -MaxRows $MaxRowsPerNote)
        $targets = @($noteRows | ForEach-Object {
            if (Test-DgaRowMatchesAny $_ @("HighConfidence", "confidence=0.8", "confidence=0.9", "Stale", "Duplicate", "AreaLimitReached", "BlockedArea", "currentAreaAtAcceptance", "JournalPending", "Notification", "GoblinLatencyTrace")) {
                New-DgaTriageReviewTarget $_ "NoteScopedReviewTarget" "Matched note window or note keywords with auto-count risk marker"
            }
        } | Select-Object -First 40)

        $targetTime = Get-DgaNoteTargetTime -Note $note -VideoStart $videoStart
        $targetLocalTime = if ($null -ne $targetTime) { ([DateTime]$targetTime).ToString("yyyy-MM-dd HH:mm:ss.fff zzz") } else { "" }
        $missingSources = New-Object System.Collections.Generic.List[string]
        if ($evidenceIndex.DecisionBundleFiles.Count -eq 0) {
            $missingSources.Add("DecisionBundles missing")
        }

        if ($evidenceIndex.GoblinTrackerEventsJsonlFiles.Count -eq 0) {
            $missingSources.Add("GoblinTrackerEvents.jsonl missing")
        }

        if ($evidenceIndex.JournalCrops.Count -eq 0) {
            $missingSources.Add("Journal crops missing")
        }

        if ($evidenceIndex.MinimapCrops.Count -eq 0) {
            $missingSources.Add("Minimap crops missing")
        }

        $noteReports.Add([pscustomobject]@{
            Index = $note.Index
            Timestamp = $note.Timestamp
            Note = $note.Note
            Source = $note.Source
            TargetLocalTime = $targetLocalTime
            Groups = [pscustomobject]@{
                AcceptedCounts = @($noteRows | Where-Object { $_.Event -eq "GoblinAutoCountAccepted" -or $_.Event -eq "GoblinCountAccepted" }).Count
                SuppressedCandidates = @($noteRows | Where-Object { $_.Event -eq "GoblinAutoCountSuppressed" -or $_.Event -eq "GoblinCountSuppressed" }).Count
                PendingEvidence = @($noteRows | Where-Object { Test-DgaRowMatchesAny $_ @("JournalPending", "PendingKilled", "PendingMinimap") }).Count
                StaleJournalRows = @($noteRows | Where-Object { Test-DgaRowMatchesAny $_ @("Stale", "Carryover", "HistoryRow") }).Count
                NotificationTraces = @($noteRows | Where-Object { Test-DgaRowMatchesAny $_ @("GoblinLatencyTrace", "GoblinAutoCountNotificationQueued", "GoblinAutoCountNotificationDisplayed", "GoblinAutoCountNotificationDropped") }).Count
            }
            SuppressionReasons = @(Get-DgaSuppressionReasonSummary $noteRows)
            Events = @($noteRows | Select-Object -First $MaxRowsPerNote)
            EvidenceReferences = @(Get-DgaEvidenceReferencesForReviewNote -Root $Root -Note $note -Rows $noteRows -MaxReferences $MaxEvidenceReferencesPerNote -EvidenceIndex $evidenceIndex)
            LikelyReviewTargets = $targets
            MissingSources = $missingSources.ToArray()
        })
    }

    [pscustomobject]@{
        Generated = (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz")
        Root = $Root
        ReviewVideoPath = $resolvedReviewVideoPath
        WindowSeconds = $WindowSeconds
        VideoStartLocal = if ($null -ne $videoStart) { ([DateTime]$videoStart).ToString("yyyy-MM-dd HH:mm:ss.fff zzz") } else { "" }
        VideoStartSource = $videoStartSource
        SourceAvailability = [pscustomobject]@{
            Logs = $evidenceIndex.Logs.Count
            GoblinTrackerEventsJsonl = $evidenceIndex.GoblinTrackerEventsJsonlFiles.Count
            DecisionBundles = $evidenceIndex.DecisionTraceFiles.Count
            JournalCrops = $evidenceIndex.JournalCrops.Count
            MinimapCrops = $evidenceIndex.MinimapCrops.Count
        }
        Notes = $noteReports.ToArray()
        PackageSizePolicy = [pscustomobject]@{
            Included = "AutoCountReviewNotes/auto-count-review-notes.md and AutoCountReviewNotes/auto-count-review-notes.json"
            Excluded = "Full videos, bulk source image folders, and duplicated evidence image folders"
            Notes = "Reports reference existing package evidence paths and cap rows/evidence references per note."
        }
    }
}

function New-DgaAutoCountReviewNotesMarkdownContent {
    param([object]$Data)

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Auto-Count Review Notes Triage")
    $lines.Add("")
    $lines.Add("Generated: $($Data.Generated)")
    $lines.Add("Root: $($Data.Root)")
    $lines.Add("Review video: $(if ([string]::IsNullOrWhiteSpace($Data.ReviewVideoPath)) { 'not supplied' } else { $Data.ReviewVideoPath })")
    $lines.Add("Window seconds: $($Data.WindowSeconds)")
    $lines.Add("Video start: $(if ([string]::IsNullOrWhiteSpace($Data.VideoStartLocal)) { 'unavailable' } else { $Data.VideoStartLocal })")
    $lines.Add("Video start source: $(if ([string]::IsNullOrWhiteSpace($Data.VideoStartSource)) { 'unavailable' } else { $Data.VideoStartSource })")
    $lines.Add("")
    $lines.Add("## Source Availability")
    foreach ($property in $Data.SourceAvailability.PSObject.Properties) {
        $lines.Add("- $($property.Name): $($property.Value)")
    }

    $lines.Add("")
    $lines.Add("## Notes")
    if (@($Data.Notes).Count -eq 0) {
        $lines.Add("- No review notes or timestamps were supplied.")
    }

    foreach ($note in @($Data.Notes)) {
        $lines.Add("")
        $lines.Add("### Note $($note.Index)")
        $lines.Add("- Timestamp: $(if ([string]::IsNullOrWhiteSpace($note.Timestamp)) { 'none' } else { $note.Timestamp })")
        $lines.Add("- Target local time: $(if ([string]::IsNullOrWhiteSpace($note.TargetLocalTime)) { 'unavailable' } else { $note.TargetLocalTime })")
        $lines.Add("- Note: $(Convert-DgaMarkdownCell $note.Note)")
        $lines.Add("- Source: $($note.Source)")
        $lines.Add("")
        $lines.Add("Groups:")
        foreach ($property in $note.Groups.PSObject.Properties) {
            $lines.Add("- $($property.Name): $($property.Value)")
        }

        $lines.Add("")
        $lines.Add("Suppression reasons:")
        if (@($note.SuppressionReasons).Count -eq 0) {
            $lines.Add("- none")
        }
        else {
            foreach ($reason in @($note.SuppressionReasons)) {
                $lines.Add("- $($reason.Reason): $($reason.Count)")
            }
        }

        $lines.Add("")
        $lines.Add("Likely review targets:")
        if (@($note.LikelyReviewTargets).Count -eq 0) {
            $lines.Add("- none")
        }
        else {
            $lines.Add("| Time | Event | Goblin | Area | Decision | Reason | Log | Why |")
            $lines.Add("| --- | --- | --- | --- | --- | --- | --- | --- |")
            foreach ($target in @($note.LikelyReviewTargets | Select-Object -First 25)) {
                $lines.Add("| $(Convert-DgaMarkdownCell $target.Timestamp) | $(Convert-DgaMarkdownCell $target.Event) | $(Convert-DgaMarkdownCell $target.GoblinType) | $(Convert-DgaMarkdownCell $target.Area) | $(Convert-DgaMarkdownCell $target.Decision) | $(Convert-DgaMarkdownCell $target.DecisionReason) | $(Convert-DgaMarkdownCell $target.Log) | $(Convert-DgaMarkdownCell $target.Reason) |")
            }
        }

        $lines.Add("")
        $lines.Add("Evidence references:")
        if (@($note.EvidenceReferences).Count -eq 0) {
            $lines.Add("- none")
        }
        else {
            foreach ($reference in @($note.EvidenceReferences | Select-Object -First 40)) {
                $lines.Add("- $($reference.Kind): `$($reference.Path)` ($($reference.SizeBytes) bytes)")
            }
        }

        if (@($note.MissingSources).Count -gt 0) {
            $lines.Add("")
            $lines.Add("Missing sources:")
            foreach ($missing in @($note.MissingSources)) {
                $lines.Add("- $missing")
            }
        }
    }

    $lines.Add("")
    $lines.Add("## Package Size Policy")
    $lines.Add("- Included: $($Data.PackageSizePolicy.Included)")
    $lines.Add("- Excluded: $($Data.PackageSizePolicy.Excluded)")
    $lines.Add("- Notes: $($Data.PackageSizePolicy.Notes)")
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
    $triageMarkdownPath = Join-Path $OutputDirectory "goblin-auto-count-triage.md"
    $triageJsonPath = Join-Path $OutputDirectory "goblin-auto-count-triage.json"

    New-DgaDebugPackageAnalysisContent -Root $Root | Out-File -FilePath $analysisPath -Encoding utf8
    New-DgaGoblinTrackerTimelineContent -Root $Root | Out-File -FilePath $timelinePath -Encoding utf8
    New-DgaGoblinEvidenceHealthContent -Root $Root | Out-File -FilePath $healthPath -Encoding utf8
    $triageData = New-DgaAutoCountTriageData -Root $Root
    New-DgaAutoCountTriageMarkdownContent -Data $triageData | Out-File -FilePath $triageMarkdownPath -Encoding utf8
    $triageData | ConvertTo-Json -Depth 8 | Out-File -FilePath $triageJsonPath -Encoding utf8

    return [pscustomobject]@{
        AnalysisPath = $analysisPath
        TimelinePath = $timelinePath
        HealthPath = $healthPath
        AutoCountTriageMarkdownPath = $triageMarkdownPath
        AutoCountTriageJsonPath = $triageJsonPath
    }
}

function Get-DgaLatestDebugPackage {
    param([string]$DebugPackagesRoot)

    if (-not (Test-Path -LiteralPath $DebugPackagesRoot -PathType Container)) {
        return $null
    }

    return Get-ChildItem -LiteralPath $DebugPackagesRoot -Filter "GoblinFarmer_Debug_*.zip" -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "*_AutoCountNotes.zip" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

function Get-DgaLatestReviewVideo {
    param([string]$VideoReviewRoot)

    if (-not (Test-Path -LiteralPath $VideoReviewRoot -PathType Container)) {
        return $null
    }

    return Get-ChildItem -LiteralPath $VideoReviewRoot -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -match '^\.(mkv|mp4|mov|avi)$' } |
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

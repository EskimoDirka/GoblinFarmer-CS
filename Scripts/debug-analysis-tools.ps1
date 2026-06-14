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
    $allFiles = @(Get-DgaFiles $Root)
    $eventFiles = @(Get-DgaFiles $Root "Debug\GoblinEvidence" | Where-Object { $_.Name.Equals("GoblinTrackerEvents.jsonl", [System.StringComparison]::OrdinalIgnoreCase) })
    $decisionBundles = @(Get-DgaFiles $Root "Debug\GoblinEvidence\DecisionBundles" | Where-Object { $_.Name.Equals("decision-trace.txt", [System.StringComparison]::OrdinalIgnoreCase) })
    $reviewEvidenceFiles = @(Get-DgaFiles $Root "ReviewEvidence")
    $reviewFrames = @($reviewEvidenceFiles | Where-Object { $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' })
    $journalCrops = @($allFiles | Where-Object { $_.Name.IndexOf("Journal", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' })
    $minimapCrops = @($allFiles | Where-Object { $_.Name.IndexOf("Minimap", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' })

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
            Logs = @(Get-DgaAllLogs $Root).Count
            GoblinTrackerEventsJsonl = $eventFiles.Count
            DecisionBundles = $decisionBundles.Count
            ReviewEvidenceFiles = $reviewEvidenceFiles.Count
            ReviewEvidenceFrames = $reviewFrames.Count
            JournalCrops = $journalCrops.Count
            MinimapCrops = $minimapCrops.Count
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

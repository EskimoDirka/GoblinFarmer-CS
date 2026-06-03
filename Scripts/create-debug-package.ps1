param(
    [int]$MaxScreenshots = 10,
    [int]$MaxFailureScreenshots = 10,
    [int]$MaxSuccessScreenshots = 10
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

function Read-KeyValueFile {
    param([string]$Path)

    $values = [ordered]@{}
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $values
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match '^\s*([^=]+?)=(.*)$') {
            $values[$matches[1].Trim()] = $matches[2].Trim()
        }
    }

    return $values
}

function Get-CurrentSessionInfo {
    param(
        [string]$RepoRoot,
        [System.IO.FileInfo]$LatestLog
    )

    $sessionFiles = @(
        (Join-Path $RepoRoot "session-info.txt"),
        (Join-Path $RepoRoot "bin\Debug\net10.0-windows\session-info.txt"),
        (Join-Path $RepoRoot "bin\Release\net10.0-windows\session-info.txt")
    ) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        ForEach-Object { Get-Item -LiteralPath $_ } |
        Sort-Object LastWriteTime -Descending

    foreach ($sessionFile in $sessionFiles) {
        $values = Read-KeyValueFile $sessionFile.FullName
        foreach ($key in @("SessionStartLocal", "SessionStartUtc")) {
            if ($values.Contains($key) -and -not [string]::IsNullOrWhiteSpace($values[$key])) {
                try {
                    $start = [DateTime]::Parse($values[$key], [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind)
                    if ($start.Kind -eq [DateTimeKind]::Utc) {
                        $start = $start.ToLocalTime()
                    }

                    return [pscustomobject]@{
                        Start = $start
                        Source = $sessionFile.FullName
                        SourceKind = $key
                    }
                }
                catch {
                    Write-Warning "Could not parse $key from $($sessionFile.FullName): $($values[$key])"
                }
            }
        }
    }

    if ($null -ne $LatestLog) {
        return [pscustomobject]@{
            Start = $LatestLog.CreationTime
            Source = $LatestLog.FullName
            SourceKind = "LatestLogCreationTimeFallback"
        }
    }

    return [pscustomobject]@{
        Start = Get-Date
        Source = "script start fallback"
        SourceKind = "ScriptStartFallback"
    }
}

function Format-ByteSize {
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

function Get-ScreenshotFailureType {
    param([System.IO.FileInfo]$File)

    $failureTypes = @(
        "TeleportBlocked",
        "TeleportInterrupted",
        "TeleportConfirmationTimeout",
        "StartGameButtonNotFound",
        "StartGameVerificationFailed",
        "BattleNetPlayButtonNotFound",
        "BattleNetPlayButtonNotClickedByApp",
        "BattleNetPlayClickAccepted",
        "BattleNetManualPlaySuspected",
        "BattleNetStillOpenAfterDiabloLaunch",
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

function Get-DiagnosticScreenshotInfo {
    param([System.IO.FileInfo]$File)

    $name = $File.BaseName
    if ($name -match '^(?<date>\d{4}-\d{2}-\d{2})_(?<time>\d{6})_(?<ms>\d{3})_(?<outcome>Success|Failure)_(?<workflow>[^_]+)_(?<action>.+)_(?<surface>Diablo|App)$') {
        $timestampText = "$($matches.date) $($matches.time.Substring(0, 2)):$($matches.time.Substring(2, 2)):$($matches.time.Substring(4, 2)).$($matches.ms)"
        $pairKey = "$($matches.date)_$($matches.time)_$($matches.ms)_$($matches.outcome)_$($matches.workflow)_$($matches.action)"
        return [pscustomobject]@{
            IsDiagnosticPair = $true
            Timestamp = $timestampText
            Outcome = $matches.outcome.ToUpperInvariant()
            Workflow = $matches.workflow
            Action = $matches.action
            Surface = $matches.surface
            PairKey = $pairKey
        }
    }

    $failureType = Get-ScreenshotFailureType $File
    if (-not [string]::IsNullOrWhiteSpace($failureType)) {
        return [pscustomobject]@{
            IsDiagnosticPair = $false
            Timestamp = $File.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
            Outcome = "FAILURE"
            Workflow = "Legacy"
            Action = $failureType
            Surface = "Diablo"
            PairKey = $File.BaseName
        }
    }

    return [pscustomobject]@{
        IsDiagnosticPair = $false
        Timestamp = $File.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
        Outcome = "DEBUG"
        Workflow = "Legacy"
        Action = $File.BaseName
        Surface = "Unknown"
        PairKey = $File.BaseName
    }
}

function Select-DiagnosticScreenshotGroups {
    param(
        [System.IO.FileInfo[]]$Files,
        [string]$Outcome,
        [int]$Limit
    )

    @($Files |
        ForEach-Object {
            $info = Get-DiagnosticScreenshotInfo $_
            if ($info.Outcome -eq $Outcome) {
                [pscustomobject]@{ File = $_; Info = $info }
            }
        } |
        Group-Object { $_.Info.PairKey } |
        Sort-Object { ($_.Group | ForEach-Object { $_.File.LastWriteTime } | Measure-Object -Maximum).Maximum } -Descending |
        Select-Object -First $Limit)
}

function Get-FilesFromScreenshotGroups {
    param($Groups)

    @($Groups | ForEach-Object { $_.Group | ForEach-Object { $_.File } } | Sort-Object LastWriteTime -Descending)
}

function New-ScreenshotManifest {
    param(
        $Groups,
        [string]$OutputPath
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("GoblinFarmer Screenshot Manifest")
    $lines.Add("Created: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
    $lines.Add("")

    $added = 0
    foreach ($group in $Groups) {
        $first = $group.Group | Select-Object -First 1
        if ($null -eq $first) {
            continue
        }

        $info = $first.Info
        $diablo = ($group.Group | Where-Object { $_.Info.Surface -eq "Diablo" } | Select-Object -First 1).File
        $app = ($group.Group | Where-Object { $_.Info.Surface -eq "App" } | Select-Object -First 1).File
        $other = @($group.Group | Where-Object { $_.Info.Surface -ne "Diablo" -and $_.Info.Surface -ne "App" } | ForEach-Object { $_.File.Name })

        $lines.Add($info.Timestamp)
        $lines.Add($info.Outcome)
        $lines.Add("Workflow=$($info.Workflow)")
        $lines.Add("Action=$($info.Action)")
        $lines.Add("Diablo=$(if ($null -ne $diablo) { $diablo.Name } else { 'none' })")
        $lines.Add("App=$(if ($null -ne $app) { $app.Name } else { 'none' })")
        if ($other.Count -gt 0) {
            $lines.Add("Other=$($other -join ', ')")
        }
        $lines.Add("")
        $added++
    }

    if ($added -eq 0) {
        $lines.Add("No screenshots selected for this package.")
    }

    $lines | Out-File -FilePath $OutputPath -Encoding utf8
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

function Copy-DebugScreenshotsToPackage {
    param(
        [System.IO.FileInfo[]]$Files,
        [string[]]$SourceRoots,
        [string]$DestinationRoot
    )

    if ($Files.Count -eq 0) {
        return 0
    }

    $copied = 0
    foreach ($file in $Files) {
        $sourceRoot = $SourceRoots |
            Where-Object { $file.FullName.StartsWith($_, [System.StringComparison]::OrdinalIgnoreCase) } |
            Sort-Object Length -Descending |
            Select-Object -First 1

        if ([string]::IsNullOrWhiteSpace($sourceRoot)) {
            $relativePath = $file.Name
        }
        else {
            $relativePath = $file.FullName.Substring($sourceRoot.Length).TrimStart('\', '/')
        }

        $destination = Join-Path $DestinationRoot $relativePath
        $destinationDirectory = Split-Path -Parent $destination
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
        $copied++
    }

    return $copied
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

function Convert-LogKeyValueText {
    param([string]$Text)

    $result = [ordered]@{}
    foreach ($part in ($Text -split ";\s*")) {
        if ($part -match "^\s*([^=]+?)=(.*)$") {
            $result[$matches[1].Trim()] = $matches[2].Trim()
        }
    }

    return $result
}

function Get-LogValue {
    param(
        $Values,
        [string]$Name,
        [string]$Fallback = "Unknown"
    )

    if ($Values.Contains($Name) -and -not [string]::IsNullOrWhiteSpace($Values[$Name])) {
        return $Values[$Name]
    }

    return $Fallback
}

function Get-RouteResultFromEvent {
    param([string]$Event)

    if ($Event -like "*Allowed*") {
        return "Allowed"
    }
    if ($Event -like "*Blocked*") {
        return "Blocked"
    }
    if ($Event -like "*Cancelled*" -or $Event -like "*Interrupted*") {
        return "Cancelled"
    }
    if ($Event -like "*Failed*" -or $Event -like "*Timeout*") {
        return "Failed"
    }

    return "Unknown"
}

function New-RouteSummaryBlock {
    param(
        [string]$Timestamp,
        [string]$Event,
        [string]$Workflow,
        [string]$Result,
        [string]$RequestedTarget,
        [string]$Source,
        [string]$RawLocation,
        [string]$NormalizedLocation,
        [string]$DisplayLocation,
        [string]$BlockingLocation,
        [string]$CurrentButton,
        [string]$NextButton,
        [string]$QueuedRetryTarget,
        [string]$BlockingReason,
        [string]$ScreenshotPath,
        [string]$LikelyExplanation
    )

    @(
        "[$Timestamp] $Event",
        "  workflow: $Workflow",
        "  result: $Result",
        "  requested target: $RequestedTarget",
        "  source: $Source",
        "  raw location: $RawLocation",
        "  normalized location: $NormalizedLocation",
        "  display location: $DisplayLocation",
        "  blocking location: $BlockingLocation",
        "  current button: $CurrentButton",
        "  next button: $NextButton",
        "  queued retry target: $QueuedRetryTarget",
        "  blocking reason: $BlockingReason",
        "  screenshot references: $ScreenshotPath",
        "  likely explanation: $LikelyExplanation",
        ""
    )
}

function New-WorkflowSummaryBlock {
    param(
        [string]$Timestamp,
        [string]$Event,
        [string]$Workflow,
        [string]$LaunchSuccessful,
        [string]$AppClickedBattleNetPlay,
        [string]$BattleNetPlayClickSentByApp,
        [string]$BattleNetPlayClickAcceptedByBattleNet,
        [string]$BattleNetPlayClickAcceptedReason,
        [string]$BattleNetPlayClickPoint,
        [string]$BattleNetPlayClickTimestamp,
        [string]$BattleNetPlayClickAcceptedTimestamp,
        [string]$DiabloLaunched,
        [string]$DiabloLaunchedAfterAppPlayClick,
        [string]$DiabloLaunchedWithoutAppPlayClick,
        [string]$BattleNetManualPlaySuspected,
        [string]$BattleNetStillOpenAfterLaunch,
        [string]$BattleNetCloseRequested,
        [string]$BattleNetCloseSucceeded,
        [string]$BattleNetCloseTimedOut,
        [string]$BattleNetCloseProcessRemaining,
        [string]$BattleNetCloseVisibleWindowRemaining,
        [string]$ScreenshotPath,
        [string]$LikelyExplanation
    )

    @(
        "[$Timestamp] $Event",
        "  workflow: $Workflow",
        "  launch successful: $LaunchSuccessful",
        "  app clicked Battle.net Play: $AppClickedBattleNetPlay",
        "  app play click sent: $BattleNetPlayClickSentByApp",
        "  app play click accepted: $BattleNetPlayClickAcceptedByBattleNet",
        "  app play click accepted reason: $BattleNetPlayClickAcceptedReason",
        "  app play click point: $BattleNetPlayClickPoint",
        "  app play click timestamp: $BattleNetPlayClickTimestamp",
        "  app play click accepted timestamp: $BattleNetPlayClickAcceptedTimestamp",
        "  Diablo launched: $DiabloLaunched",
        "  Diablo launched after app Play click: $DiabloLaunchedAfterAppPlayClick",
        "  Diablo launched without app Play click: $DiabloLaunchedWithoutAppPlayClick",
        "  manual play suspected: $BattleNetManualPlaySuspected",
        "  Battle.net still open after launch: $BattleNetStillOpenAfterLaunch",
        "  Battle.net close requested: $BattleNetCloseRequested",
        "  Battle.net close succeeded: $BattleNetCloseSucceeded",
        "  Battle.net close timed out: $BattleNetCloseTimedOut",
        "  Battle.net process remaining: $BattleNetCloseProcessRemaining",
        "  Battle.net visible window remaining: $BattleNetCloseVisibleWindowRemaining",
        "  screenshot references: $ScreenshotPath",
        "  likely explanation: $LikelyExplanation",
        ""
    )
}

function New-RouteFailureSummary {
    param(
        [System.IO.FileInfo]$LogFile,
        [string]$OutputPath
    )

    $linesOut = New-Object System.Collections.Generic.List[string]
    $linesOut.Add("GoblinFarmer Route Failure Summary")
    $linesOut.Add("Source log: $(if ($null -ne $LogFile) { $LogFile.FullName } else { 'none' })")
    $linesOut.Add("Created: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
    $linesOut.Add("")

    if ($null -eq $LogFile -or -not (Test-Path -LiteralPath $LogFile.FullName -PathType Leaf)) {
        $linesOut.Add("No log file available.")
        $linesOut | Out-File -FilePath $OutputPath -Encoding utf8
        return
    }

    $currentRun = @{}
    $lastButtons = @{}
    $lastBlocking = @{}
    $lastScreenshotPath = "Unknown"
    $lastScreenshotType = "Unknown"
    $startAttempt = @{}
    $workflowSummaryKeys = @{}
    $added = 0

    foreach ($line in Get-Content -LiteralPath $LogFile.FullName) {
        if ($line -notmatch "^(?<ts>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[INFO\] (?<msg>.*)$") {
            continue
        }

        $timestamp = $matches.ts
        $message = $matches.msg

        if ($message -match "^BattleNetLaunchSummary: (?<kv>.*)$" -or $message -match "^BattleNetPostLaunchCloseSummary: (?<kv>.*)$") {
            $values = Convert-LogKeyValueText $matches.kv
            $appClicked = Get-LogValue $values "appClickedBattleNetPlay" (Get-LogValue $values "battleNetPlayClickSentByApp")
            $appClickAccepted = Get-LogValue $values "battleNetPlayClickAcceptedByBattleNet"
            $launchedAfterAppClick = Get-LogValue $values "diabloLaunchedAfterAppClick" (Get-LogValue $values "diabloLaunchedAfterAppPlayClick")
            $diabloLaunched = Get-LogValue $values "diabloLaunched"
            if ($diabloLaunched -eq "Unknown") {
                $diabloLaunched = (($launchedAfterAppClick -eq "True") -or ((Get-LogValue $values "diabloLaunchedWithoutAppPlayClick") -eq "True")).ToString()
            }
            $stillOpenAfterLaunch = Get-LogValue $values "battleNetStillOpenAfterLaunch" (Get-LogValue $values "battleNetStillOpen")
            $event = Get-LogValue $values "event" "BattleNetLaunchSummary"
            $summaryKey = @(
                $event,
                $appClicked,
                $appClickAccepted,
                (Get-LogValue $values "battleNetPlayClickPoint"),
                (Get-LogValue $values "battleNetPlayClickTimestamp"),
                (Get-LogValue $values "battleNetPlayClickAcceptedTimestamp"),
                $diabloLaunched,
                $launchedAfterAppClick,
                (Get-LogValue $values "diabloLaunchedWithoutAppPlayClick"),
                (Get-LogValue $values "battleNetManualPlaySuspected"),
                $stillOpenAfterLaunch,
                (Get-LogValue $values "battleNetCloseRequested"),
                (Get-LogValue $values "battleNetCloseSucceeded"),
                (Get-LogValue $values "battleNetCloseTimedOut")
            ) -join "|"
            if ($workflowSummaryKeys.ContainsKey($summaryKey)) {
                continue
            }

            $workflowSummaryKeys[$summaryKey] = $true
            $block = New-WorkflowSummaryBlock `
                -Timestamp $timestamp `
                -Event $event `
                -Workflow "BattleNetLaunch" `
                -LaunchSuccessful (Get-LogValue $values "launchSuccessful") `
                -AppClickedBattleNetPlay $appClicked `
                -BattleNetPlayClickSentByApp (Get-LogValue $values "battleNetPlayClickSentByApp") `
                -BattleNetPlayClickAcceptedByBattleNet $appClickAccepted `
                -BattleNetPlayClickAcceptedReason (Get-LogValue $values "battleNetPlayClickAcceptedReason") `
                -BattleNetPlayClickPoint (Get-LogValue $values "battleNetPlayClickPoint") `
                -BattleNetPlayClickTimestamp (Get-LogValue $values "battleNetPlayClickTimestamp") `
                -BattleNetPlayClickAcceptedTimestamp (Get-LogValue $values "battleNetPlayClickAcceptedTimestamp") `
                -DiabloLaunched $diabloLaunched `
                -DiabloLaunchedAfterAppPlayClick $launchedAfterAppClick `
                -DiabloLaunchedWithoutAppPlayClick (Get-LogValue $values "diabloLaunchedWithoutAppPlayClick") `
                -BattleNetManualPlaySuspected (Get-LogValue $values "battleNetManualPlaySuspected") `
                -BattleNetStillOpenAfterLaunch $stillOpenAfterLaunch `
                -BattleNetCloseRequested (Get-LogValue $values "battleNetCloseRequested") `
                -BattleNetCloseSucceeded (Get-LogValue $values "battleNetCloseSucceeded") `
                -BattleNetCloseTimedOut (Get-LogValue $values "battleNetCloseTimedOut") `
                -BattleNetCloseProcessRemaining (Get-LogValue $values "battleNetCloseProcessRemaining") `
                -BattleNetCloseVisibleWindowRemaining (Get-LogValue $values "battleNetCloseVisibleWindowRemaining") `
                -ScreenshotPath (Get-LogValue $values "screenshotPath") `
                -LikelyExplanation (Get-LogValue $values "likelyExplanation" "Review Battle.net launch logs and screenshots.")
            $linesOut.AddRange([string[]]$block)
            $added++
            continue
        }

        if ($message -match "^RouteFailureSummary: (?<kv>.*)$" -or $message -match "^RouteDebugSummary: (?<kv>.*)$") {
            $values = Convert-LogKeyValueText $matches.kv
            $event = Get-LogValue $values "event" "RouteSummary"
            $workflow = Get-LogValue $values "workflow" ""
            if ([string]::IsNullOrWhiteSpace($workflow)) {
                if ($event -like "StartGame*") {
                    $workflow = "StartGame"
                }
                elseif ($event -like "Teleport*") {
                    $workflow = "Teleport"
                }
                else {
                    $workflow = Get-LogValue $values "source" "Workflow"
                }
            }

            $result = Get-LogValue $values "result" (Get-RouteResultFromEvent $event)
            $block = New-RouteSummaryBlock `
                -Timestamp $timestamp `
                -Event $event `
                -Workflow $workflow `
                -Result $result `
                -RequestedTarget (Get-LogValue $values "requestedTarget") `
                -Source (Get-LogValue $values "source") `
                -RawLocation (Get-LogValue $values "rawLocation") `
                -NormalizedLocation (Get-LogValue $values "normalizedLocation") `
                -DisplayLocation (Get-LogValue $values "displayLocation") `
                -BlockingLocation (Get-LogValue $values "blockingLocation") `
                -CurrentButton (Get-LogValue $values "currentButton") `
                -NextButton (Get-LogValue $values "nextButton") `
                -QueuedRetryTarget (Get-LogValue $values "queuedRetryTarget") `
                -BlockingReason (Get-LogValue $values "blockingReason") `
                -ScreenshotPath (Get-LogValue $values "screenshotPath") `
                -LikelyExplanation (Get-LogValue $values "likelyExplanation" "Review the log event.")
            $linesOut.AddRange([string[]]$block)
            $added++
            continue
        }

        if ($message -match "^StartGameVerificationFailureSummary: (?<kv>.*)$") {
            $values = Convert-LogKeyValueText $matches.kv
            $block = New-RouteSummaryBlock `
                -Timestamp $timestamp `
                -Event "StartGameVerificationFailed" `
                -Workflow "StartGame" `
                -Result "Failed" `
                -RequestedTarget "Start Game" `
                -Source "Workflow" `
                -RawLocation "Unknown" `
                -NormalizedLocation "Unknown" `
                -DisplayLocation "Main Menu" `
                -BlockingLocation "None" `
                -CurrentButton "Unknown" `
                -NextButton "Unknown" `
                -QueuedRetryTarget "Unknown" `
                -BlockingReason "Start Game click verification failed" `
                -ScreenshotPath (Get-LogValue $values "screenshotPath") `
                -LikelyExplanation (Get-LogValue $values "likelyExplanation" "Start Game did not verify before retry.")
            $linesOut.AddRange([string[]]$block)
            $added++
            continue
        }

        if ($message -match "^Teleport run start: (?<kv>.*)$") {
            $currentRun = Convert-LogKeyValueText $matches.kv
            continue
        }

        if ($message -match "^Teleport blocking decision: (?<kv>.*)$") {
            $lastBlocking = Convert-LogKeyValueText $matches.kv
            continue
        }

        if ($message -match "^ButtonCurrent=(?<kv>.*)$") {
            $lastButtons = Convert-LogKeyValueText $message
            continue
        }

        if ($message -match "^Failure screenshot saved: type=(?<type>.*?); path=(?<path>.*)$") {
            $lastScreenshotType = $matches.type.Trim()
            $lastScreenshotPath = $matches.path.Trim()
            continue
        }

        if ($message -match "^Start Game click attempt (?<attempt>\d+)/(?<max>\d+): clicking center at (?<point>.*)$") {
            $startAttempt = @{
                attempt = $matches.attempt
                max = $matches.max
                point = $matches.point
            }
            continue
        }

        if ($message -match "^Start Game click attempt (?<attempt>\d+)/(?<max>\d+) not verified") {
            $block = New-RouteSummaryBlock `
                -Timestamp $timestamp `
                -Event "StartGameVerificationFailed" `
                -Workflow "StartGame" `
                -Result "Failed" `
                -RequestedTarget "Start Game" `
                -Source "Workflow" `
                -RawLocation "Unknown" `
                -NormalizedLocation "Unknown" `
                -DisplayLocation "Main Menu" `
                -BlockingLocation "None" `
                -CurrentButton "Unknown" `
                -NextButton "Unknown" `
                -QueuedRetryTarget "Unknown" `
                -BlockingReason "Start Game click verification failed" `
                -ScreenshotPath $lastScreenshotPath `
                -LikelyExplanation "Start Game attempt $($matches.attempt)/$($matches.max) did not transition before verification timeout. The retry behavior should continue; inspect screenshot and nearby PERF Start Game visibility lines."
            $linesOut.AddRange([string[]]$block)
            $added++
            continue
        }

        if ($message -match "^Teleport blocked location: (?<kv>.*)$") {
            $blocked = Convert-LogKeyValueText $matches.kv
            $raw = Get-LogValue $blocked "raw"
            $normalized = Get-LogValue $blocked "normalized"
            $target = Get-LogValue $blocked "target" (Get-LogValue $currentRun "requested")
            $reason = Get-LogValue $lastBlocking "reason"
            $explanation = if ($reason -ne "Unknown") { $reason } else { "Teleport was blocked by route safety before opening or clicking the map." }
            $block = New-RouteSummaryBlock `
                -Timestamp $timestamp `
                -Event "TeleportBlocked" `
                -Workflow "Teleport" `
                -Result (Get-LogValue $lastBlocking "result" "Blocked") `
                -RequestedTarget $target `
                -Source (Get-LogValue $blocked "source" (Get-LogValue $currentRun "source")) `
                -RawLocation $raw `
                -NormalizedLocation $normalized `
                -DisplayLocation (Get-LogValue $lastBlocking "display" (Get-LogValue $lastButtons "DisplayLocation")) `
                -BlockingLocation $raw `
                -CurrentButton (Get-LogValue $lastButtons "ButtonCurrent" (Get-LogValue $currentRun "displayBefore")) `
                -NextButton (Get-LogValue $lastButtons "ButtonNext" (Get-LogValue $currentRun "queuedBefore")) `
                -QueuedRetryTarget (Get-LogValue $currentRun "retryQueuedBefore") `
                -BlockingReason $reason `
                -ScreenshotPath $lastScreenshotPath `
                -LikelyExplanation $explanation
            $linesOut.AddRange([string[]]$block)
            $added++
            continue
        }

        if ($message -match "^Teleport failed/interrupted: (?<kv>.*)$" -or $message -match "^Button teleport failed/interrupted: (?<kv>.*)$") {
            $failed = Convert-LogKeyValueText $matches.kv
            $cancelled = Get-LogValue $failed "cancelled" "False"
            $blocked = Get-LogValue $failed "blocked" "False"
            if ($cancelled -eq "True" -or $blocked -ne "True") {
                $target = Get-LogValue $failed "requested" (Get-LogValue $currentRun "requested")
                $explanation = if ($cancelled -eq "True") {
                    "Automation was cancelled before arrival confirmation completed; route state should remain on the previous confirmed location."
                }
                else {
                    "Teleport did not confirm. Inspect map act, destination click, arrival confirmation, and screenshot evidence near this timestamp."
                }
                $block = New-RouteSummaryBlock `
                    -Timestamp $timestamp `
                    -Event $(if ($cancelled -eq "True") { "TeleportCancelled" } else { "TeleportFailed" }) `
                    -Workflow "Teleport" `
                    -Result $(if ($cancelled -eq "True") { "Cancelled" } else { "Failed" }) `
                    -RequestedTarget $target `
                    -Source (Get-LogValue $failed "source" (Get-LogValue $currentRun "source")) `
                    -RawLocation (Get-LogValue $failed "confirmedAfter") `
                    -NormalizedLocation (Get-LogValue $failed "confirmedAfter") `
                    -DisplayLocation (Get-LogValue $lastButtons "DisplayLocation") `
                    -BlockingLocation (Get-LogValue $failed "confirmedBefore") `
                    -CurrentButton (Get-LogValue $lastButtons "ButtonCurrent" (Get-LogValue $currentRun "displayBefore")) `
                    -NextButton (Get-LogValue $lastButtons "ButtonNext" (Get-LogValue $currentRun "queuedBefore")) `
                    -QueuedRetryTarget (Get-LogValue $currentRun "retryQueuedBefore") `
                    -BlockingReason (Get-LogValue $lastBlocking "reason" "arrival confirmation did not complete") `
                    -ScreenshotPath $lastScreenshotPath `
                    -LikelyExplanation $explanation
                $linesOut.AddRange([string[]]$block)
                $added++
            }
        }
    }

    if ($added -eq 0) {
        $linesOut.Add("No route failure, block, cancel, or Start Game verification events were detected in the latest log.")
    }

    $linesOut | Out-File -FilePath $OutputPath -Encoding utf8
}

if ($MaxScreenshots -lt 1) {
    Write-Warning "MaxScreenshots must be at least 1. Using 1."
    $MaxScreenshots = 1
}

if ($MaxFailureScreenshots -lt 1) {
    Write-Warning "MaxFailureScreenshots must be at least 1. Using 1."
    $MaxFailureScreenshots = 1
}

if ($MaxSuccessScreenshots -lt 1) {
    Write-Warning "MaxSuccessScreenshots must be at least 1. Using 1."
    $MaxSuccessScreenshots = 1
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Write-Host "PSScriptRoot = $PSScriptRoot"
Write-Host "RepoRoot     = $repoRoot"
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
Write-Host "Max success screenshot groups: $MaxSuccessScreenshots"

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

    $sessionInfo = Get-CurrentSessionInfo $repoRoot $latestLog
    $sessionStart = [DateTime]$sessionInfo.Start
    $sessionDuration = (Get-Date) - $sessionStart
    Write-Host "Session start: $($sessionStart.ToString('yyyy-MM-dd HH:mm:ss.fff zzz'))"
    Write-Host "Session source: $($sessionInfo.SourceKind) ($($sessionInfo.Source))"
    Write-Host "Session duration: $($sessionDuration.ToString('hh\:mm\:ss'))"

    $screenshotFolders = @(
        (Join-Path $repoRoot "Screenshots"),
        (Join-Path $repoRoot "bin\Debug\net10.0-windows\Screenshots"),
        (Join-Path $repoRoot "bin\Release\net10.0-windows\Screenshots")
    )

    $debugScreenshotFolders = @(
        (Join-Path $repoRoot "debug-screenshots"),
        (Join-Path $repoRoot "bin\Debug\net10.0-windows\debug-screenshots"),
        (Join-Path $repoRoot "bin\Release\net10.0-windows\debug-screenshots")
    )

    Write-Step "Collecting latest screenshots"
    $allScreenshotCandidates = foreach ($folder in $screenshotFolders) {
        if (Test-Path -LiteralPath $folder -PathType Container) {
            Get-ChildItem -LiteralPath $folder -File -ErrorAction SilentlyContinue |
                Where-Object { $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' }
        }
    }
    $allScreenshots = @($allScreenshotCandidates | Sort-Object LastWriteTime -Descending)
    $sessionScreenshots = @($allScreenshots |
        Where-Object { $_.LastWriteTime -ge $sessionStart -or $_.CreationTime -ge $sessionStart } |
        Sort-Object LastWriteTime -Descending)
    $excludedStaleScreenshots = [Math]::Max(0, $allScreenshots.Count - $sessionScreenshots.Count)

    Write-Host "All discovered screenshots: $($allScreenshots.Count)"
    Write-Host "Current-session screenshots: $($sessionScreenshots.Count)"
    Write-Host "Excluded stale screenshots: $excludedStaleScreenshots"

    $failureGroups = @(Select-DiagnosticScreenshotGroups $sessionScreenshots "FAILURE" $MaxFailureScreenshots)
    $successGroups = @(Select-DiagnosticScreenshotGroups $sessionScreenshots "SUCCESS" $MaxSuccessScreenshots)
    $selectedPairKeys = @{}
    foreach ($group in @($failureGroups + $successGroups)) {
        $selectedPairKeys[$group.Name] = $true
    }

    $failureScreenshots = @(Get-FilesFromScreenshotGroups $failureGroups)
    $successScreenshots = @(Get-FilesFromScreenshotGroups $successGroups)

    $normalScreenshots = @($sessionScreenshots |
        Where-Object {
            $info = Get-DiagnosticScreenshotInfo $_
            $info.Outcome -eq "DEBUG" -and -not $selectedPairKeys.ContainsKey($info.PairKey)
        } |
        Select-Object -First $MaxScreenshots)

    $latestFailureScreenshot = $failureScreenshots | Select-Object -First 1
    $latestFailureType = "none"
    if ($null -ne $latestFailureScreenshot) {
        $latestFailureType = Get-ScreenshotFailureType $latestFailureScreenshot
        if ([string]::IsNullOrWhiteSpace($latestFailureType)) {
            $latestFailureType = (Get-DiagnosticScreenshotInfo $latestFailureScreenshot).Action
        }
    }
    $screenshotManifestPath = Join-Path $stagingRoot "debug-screenshot-manifest.txt"

    if ($sessionScreenshots.Count -eq 0) {
        Write-Warning "No current-session debug screenshots found in known Screenshots folders."
        New-ScreenshotManifest @() $screenshotManifestPath
        Write-Host "Included screenshot manifest: debug-screenshot-manifest.txt"
    }
    else {
        $failureCount = Copy-FilesToPackageFolder $failureScreenshots (Join-Path $stagingRoot "Screenshots\Failure")
        $successCount = Copy-FilesToPackageFolder $successScreenshots (Join-Path $stagingRoot "Screenshots\Success")
        $normalCount = Copy-FilesToPackageFolder $normalScreenshots (Join-Path $stagingRoot "Screenshots\Recent")
        New-ScreenshotManifest @($failureGroups + $successGroups) $screenshotManifestPath

        if ($failureCount -eq 0) {
            Write-Warning "No failure screenshots found. Expected failure type names in screenshot filenames."
        }
        else {
                Write-Host "Included failure screenshots: $failureCount"
                Write-Host "Latest failure screenshot type: $latestFailureType"
                Write-Host "Latest failure screenshot filename: $($latestFailureScreenshot.Name)"
        }

        if ($successCount -eq 0) {
            Write-Warning "No success screenshots found."
        }
        else {
            Write-Host "Included success screenshots: $successCount"
        }

        if ($normalCount -eq 0) {
            Write-Warning "No non-failure debug screenshots found."
        }
        else {
            Write-Host "Included normal debug screenshots: $normalCount"
        }

        Write-Host "Included screenshot manifest: debug-screenshot-manifest.txt"
    }

    Write-Step "Collecting debug screenshots"
    $debugScreenshotCandidates = foreach ($folder in $debugScreenshotFolders) {
        if (Test-Path -LiteralPath $folder -PathType Container) {
            Get-ChildItem -LiteralPath $folder -File -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' }
        }
    }
    $debugScreenshots = @($debugScreenshotCandidates |
        Where-Object { $_.LastWriteTime -ge $sessionStart -or $_.CreationTime -ge $sessionStart } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 50)
    $debugScreenshotCount = Copy-DebugScreenshotsToPackage $debugScreenshots $debugScreenshotFolders (Join-Path $stagingRoot "debug-screenshots")
    if ($debugScreenshotCount -eq 0) {
        Write-Warning "No current-session debug-screenshots found."
    }
    else {
        Write-Host "Included debug screenshots: $debugScreenshotCount"
    }

    Write-Step "Generating route failure summary"
    $routeFailureSummaryPath = Join-Path $stagingRoot "route-failure-summary.txt"
    New-RouteFailureSummary $latestLog $routeFailureSummaryPath
    Write-Host "Included route failure summary: route-failure-summary.txt"

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
    $totalScreenshotCount = $failureScreenshots.Count + $successScreenshots.Count + $normalScreenshots.Count
    $buildManifestLines = {
        param(
            [long]$PackageSizeBytes,
            [string]$PackageSizeDisplay
        )

        @(
            "GoblinFarmer Debug Package",
            "Created: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')",
            "Repository: $repoRoot",
            "Package: $zipPath",
            "Package size: $PackageSizeDisplay",
            "Package size bytes: $PackageSizeBytes",
            "Session start: $($sessionStart.ToString('yyyy-MM-dd HH:mm:ss.fff zzz'))",
            "Session start source: $($sessionInfo.SourceKind) ($($sessionInfo.Source))",
            "Session duration: $($sessionDuration.ToString('hh\:mm\:ss'))",
            "",
            "Included files:",
            "- AGENTS.md",
            "- Docs/Project_Status.md",
            "- Docs/TODO.md",
            "- Docs/TEST_CHECKLIST.md",
            "- git-status.txt",
            "- git-log.txt",
            "- route-failure-summary.txt",
            "- debug-screenshot-manifest.txt",
            "- Latest log: $(if ($null -ne $latestLog) { $latestLog.FullName } else { 'none' })",
            "- Total screenshots included: $totalScreenshotCount",
            "- Failure screenshots included: $($failureScreenshots.Count)",
            "- Success screenshots included: $($successScreenshots.Count)",
            "- Normal debug screenshots included: $($normalScreenshots.Count)",
            "- Debug screenshots included: $debugScreenshotCount",
            "- All discovered screenshots: $($allScreenshots.Count)",
            "- Current-session screenshots: $($sessionScreenshots.Count)",
            "- Excluded stale screenshots: $excludedStaleScreenshots",
            "- Latest failure screenshot type: $latestFailureType",
            "- Latest failure screenshot: $(if ($null -ne $latestFailureScreenshot) { $latestFailureScreenshot.FullName } else { 'none' })",
            "",
            "Exclusions:",
            "- Screenshots older than the current GoblinFarmer session start are not copied",
            "- bin folders are not copied",
            "- obj folders are not copied",
            "- build artifacts are not copied except selected runtime logs/screenshots"
        )
    }

    $packageSizeBytes = 0L
    for ($i = 0; $i -lt 10; $i++) {
        $packageSizeDisplay = if ($packageSizeBytes -gt 0) { Format-ByteSize $packageSizeBytes } else { "calculated during package creation" }
        & $buildManifestLines $packageSizeBytes $packageSizeDisplay | Out-File -FilePath $manifestPath -Encoding utf8
        Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $zipPath -Force
        $newPackageSizeBytes = (Get-Item -LiteralPath $zipPath).Length
        if ($newPackageSizeBytes -eq $packageSizeBytes) {
            break
        }

        $packageSizeBytes = $newPackageSizeBytes
    }

    $packageSizeDisplay = Format-ByteSize $packageSizeBytes
}
finally {
    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}

Write-Host ""
Write-Host "========== Debug Package Summary =========="
Write-Host "Package path:        $zipPath"
Write-Host "Package size:        $packageSizeDisplay ($packageSizeBytes bytes)"
Write-Host "Session start:       $($sessionStart.ToString('yyyy-MM-dd HH:mm:ss.fff zzz'))"
Write-Host "Session duration:    $($sessionDuration.ToString('hh\:mm\:ss'))"
Write-Host "Latest log:          $(if ($null -ne $latestLog) { $latestLog.Name } else { 'none' })"
Write-Host "Total screenshots:   $totalScreenshotCount"
Write-Host "Failure screenshots: $($failureScreenshots.Count)"
Write-Host "Success screenshots: $($successScreenshots.Count)"
Write-Host "Normal screenshots:  $($normalScreenshots.Count)"
Write-Host "Debug screenshots:   $debugScreenshotCount"
Write-Host "Stale screenshots:   $excludedStaleScreenshots excluded"
Write-Host "Latest failure type: $latestFailureType"
Write-Host "Git status captured: $gitStatusCaptured"
Write-Host "Git log captured:    $gitLogCaptured"
Write-Host "Manifest:            debug-package-manifest.txt"
Write-Host "==========================================="

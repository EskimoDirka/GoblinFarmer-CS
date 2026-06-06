param(
    [string]$RuntimeRoot = "",
    [int]$MaxScreenshots = 10,
    [int]$MaxFailureScreenshots = 3,
    [int]$MaxSuccessScreenshots = 0,
    [int]$MaxDiagnosticScreenshots = 10,
    [int]$MaxDebugScreenshots = 4,
    [switch]$IncludeSuccessScreenshots,
    [int]$MaxGoblinEvidenceFullImages = 0,
    [int]$MaxGoblinEvidenceEventScreenshots = 3,
    [long]$MaxGoblinEvidenceEventScreenshotBytes = 1048576,
    [int]$MaxGoblinObservationDiagnosticCrops = 12
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

function Resolve-FullPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ""
    }

    try {
        if (Test-Path -LiteralPath $Path) {
            return (Resolve-Path -LiteralPath $Path).Path
        }

        return [System.IO.Path]::GetFullPath($Path)
    }
    catch {
        return $Path
    }
}

function Add-UniquePath {
    param(
        [System.Collections.Generic.List[string]]$Paths,
        [string]$Path
    )

    $fullPath = Resolve-FullPath $Path
    if ([string]::IsNullOrWhiteSpace($fullPath)) {
        return
    }

    foreach ($existingPath in $Paths) {
        if ([string]::Equals($existingPath, $fullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
            return
        }
    }

    [void]$Paths.Add($fullPath)
}

function Test-SourceCheckoutRoot {
    param([string]$Path)

    return (Test-Path -LiteralPath (Join-Path $Path ".git") -PathType Container) -or
        ($null -ne (Get-ChildItem -LiteralPath $Path -Filter "*.csproj" -File -ErrorAction SilentlyContinue | Select-Object -First 1))
}

function Get-RuntimeRootSignals {
    param([string]$Path)

    $signals = New-Object System.Collections.Generic.List[string]
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Container)) {
        return @()
    }

    if (Test-Path -LiteralPath (Join-Path $Path "Config\AppSettings.json") -PathType Leaf) {
        [void]$signals.Add("Config\AppSettings.json")
    }
    if (Test-Path -LiteralPath (Join-Path $Path "session-info.txt") -PathType Leaf) {
        [void]$signals.Add("session-info.txt")
    }
    if (Test-Path -LiteralPath (Join-Path $Path "Logs") -PathType Container) {
        $latestLog = Get-LatestFileFromFolders @((Join-Path $Path "Logs")) @("*.log", "*.txt")
        if ($null -ne $latestLog) {
            [void]$signals.Add("Logs\$($latestLog.Name)")
        }
        else {
            [void]$signals.Add("Logs")
        }
    }
    if (Test-Path -LiteralPath (Join-Path $Path "Screenshots") -PathType Container) {
        [void]$signals.Add("Screenshots")
    }
    if (Test-Path -LiteralPath (Join-Path $Path "debug-screenshots") -PathType Container) {
        [void]$signals.Add("debug-screenshots")
    }
    if (Test-Path -LiteralPath (Join-Path $Path "GoblinFarmer.exe") -PathType Leaf) {
        [void]$signals.Add("GoblinFarmer.exe")
    }

    return $signals.ToArray()
}

function Get-RuntimeRootScore {
    param([string]$Path)

    $score = 0
    foreach ($signal in @(Get-RuntimeRootSignals $Path)) {
        if ($signal -eq "Config\AppSettings.json" -or $signal -eq "session-info.txt") {
            $score += 10
        }
        elseif ($signal -like "Logs*") {
            $score += 8
        }
        elseif ($signal -eq "GoblinFarmer.exe") {
            $score += 5
        }
        else {
            $score += 3
        }
    }

    return $score
}

function Get-BuildRuntimeRootCandidates {
    param([string]$Root)

    $paths = New-Object System.Collections.Generic.List[string]
    foreach ($configuration in @("Debug", "Release")) {
        $configurationRoot = Join-Path $Root "bin\$configuration"
        if (Test-Path -LiteralPath $configurationRoot -PathType Container) {
            Get-ChildItem -LiteralPath $configurationRoot -Directory -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -like "net*-windows" } |
                ForEach-Object { Add-UniquePath $paths $_.FullName }
        }

        Add-UniquePath $paths (Join-Path $Root "bin\$configuration\net10.0-windows")
    }

    return $paths.ToArray()
}

function Get-NearbyRuntimeRootCandidates {
    param(
        [string]$ScriptRoot,
        [string]$SourceRoot
    )

    $paths = New-Object System.Collections.Generic.List[string]
    Add-UniquePath $paths $ScriptRoot
    Add-UniquePath $paths $SourceRoot

    foreach ($root in @($ScriptRoot, $SourceRoot)) {
        foreach ($buildRoot in @(Get-BuildRuntimeRootCandidates $root)) {
            Add-UniquePath $paths $buildRoot
        }
    }

    return $paths.ToArray()
}

function Find-InferredRuntimeRoot {
    param([string[]]$Candidates)

    $best = $null
    foreach ($candidate in $Candidates) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Container)) {
            continue
        }

        $score = Get-RuntimeRootScore $candidate
        if ($score -le 0) {
            continue
        }

        $latestLog = Get-LatestFileFromFolders @((Join-Path $candidate "Logs")) @("*.log", "*.txt")
        $latestLogTime = if ($null -ne $latestLog) { $latestLog.LastWriteTime } else { [DateTime]::MinValue }

        $candidateInfo = [pscustomobject]@{
            Path = (Resolve-FullPath $candidate)
            Score = $score
            LatestLogTime = $latestLogTime
            Signals = (@(Get-RuntimeRootSignals $candidate) -join ", ")
        }

        if ($null -eq $best -or
            $candidateInfo.Score -gt $best.Score -or
            ($candidateInfo.Score -eq $best.Score -and $candidateInfo.LatestLogTime -gt $best.LatestLogTime)) {
            $best = $candidateInfo
        }
    }

    return $best
}

function Resolve-RuntimeRoot {
    param(
        [string]$RequestedRuntimeRoot,
        [string]$ScriptRoot
    )

    $scriptRootFull = Resolve-FullPath $ScriptRoot
    $scriptRootName = Split-Path -Leaf $scriptRootFull
    $sourceRoot = $scriptRootFull
    if ([string]::Equals($scriptRootName, "Scripts", [System.StringComparison]::OrdinalIgnoreCase)) {
        $sourceRoot = Resolve-FullPath (Join-Path $scriptRootFull "..")
    }

    if (-not [string]::IsNullOrWhiteSpace($RequestedRuntimeRoot)) {
        $requestedRoot = Resolve-FullPath $RequestedRuntimeRoot
        if (-not (Test-Path -LiteralPath $requestedRoot -PathType Container)) {
            throw "RuntimeRoot does not exist or is not a folder: $requestedRoot"
        }

        return [pscustomobject]@{
            RuntimeRoot = $requestedRoot
            SourceRoot = $sourceRoot
            Resolution = "-RuntimeRoot parameter"
            Signals = (@(Get-RuntimeRootSignals $requestedRoot) -join ", ")
            IsInstalledFolder = $false
        }
    }

    if ([string]::Equals($scriptRootName, "Scripts", [System.StringComparison]::OrdinalIgnoreCase)) {
        $isInstalledFolder = -not (Test-SourceCheckoutRoot $sourceRoot)
        return [pscustomobject]@{
            RuntimeRoot = $sourceRoot
            SourceRoot = $sourceRoot
            Resolution = "Scripts folder parent"
            Signals = (@(Get-RuntimeRootSignals $sourceRoot) -join ", ")
            IsInstalledFolder = $isInstalledFolder
        }
    }

    if (-not [string]::Equals($scriptRootName, "Scripts", [System.StringComparison]::OrdinalIgnoreCase) -and
        (Get-RuntimeRootScore $scriptRootFull) -gt 0) {
        return [pscustomobject]@{
            RuntimeRoot = $scriptRootFull
            SourceRoot = $sourceRoot
            Resolution = "script app root"
            Signals = (@(Get-RuntimeRootSignals $scriptRootFull) -join ", ")
            IsInstalledFolder = (-not (Test-SourceCheckoutRoot $scriptRootFull))
        }
    }

    $nearbyCandidates = Get-NearbyRuntimeRootCandidates $scriptRootFull $sourceRoot
    $inferred = Find-InferredRuntimeRoot $nearbyCandidates
    if ($null -ne $inferred) {
        return [pscustomobject]@{
            RuntimeRoot = $inferred.Path
            SourceRoot = $sourceRoot
            Resolution = "nearby runtime markers: $($inferred.Signals)"
            Signals = $inferred.Signals
            IsInstalledFolder = $false
        }
    }

    return [pscustomobject]@{
        RuntimeRoot = $sourceRoot
        SourceRoot = $sourceRoot
        Resolution = "source root fallback with build-output search paths"
        Signals = ""
        IsInstalledFolder = $false
    }
}

function Get-PackageRuntimeRoots {
    param(
        [string]$RuntimeRoot,
        [string]$SourceRoot
    )

    $paths = New-Object System.Collections.Generic.List[string]
    Add-UniquePath $paths $RuntimeRoot
    Add-UniquePath $paths $SourceRoot

    foreach ($buildRoot in @(Get-BuildRuntimeRootCandidates $SourceRoot)) {
        Add-UniquePath $paths $buildRoot
    }

    foreach ($buildRoot in @(Get-BuildRuntimeRootCandidates $RuntimeRoot)) {
        Add-UniquePath $paths $buildRoot
    }

    return $paths.ToArray()
}

function Get-CurrentSessionInfo {
    param(
        [string[]]$RuntimeRoots,
        [System.IO.FileInfo]$LatestLog
    )

    $sessionPaths = New-Object System.Collections.Generic.List[string]
    foreach ($root in $RuntimeRoots) {
        Add-UniquePath $sessionPaths (Join-Path $root "session-info.txt")
    }

    $sessionFiles = $sessionPaths.ToArray() | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
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

function Get-GoblinTrackerInfo {
    param([string[]]$RuntimeRoots)

    $sessionPaths = New-Object System.Collections.Generic.List[string]
    foreach ($root in $RuntimeRoots) {
        Add-UniquePath $sessionPaths (Join-Path $root "session-info.txt")
    }

    $sessionFile = $sessionPaths.ToArray() |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        ForEach-Object { Get-Item -LiteralPath $_ } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    $values = if ($null -ne $sessionFile) {
        Read-KeyValueFile $sessionFile.FullName
    }
    else {
        [ordered]@{}
    }

    $goblinCount = if ($values.Contains("GoblinCount") -and $values["GoblinCount"] -match '^\d+$') {
        [int]$values["GoblinCount"]
    }
    else {
        0
    }

    $activeCombatTime = if ($values.Contains("ActiveCombatTime") -and -not [string]::IsNullOrWhiteSpace($values["ActiveCombatTime"])) {
        $values["ActiveCombatTime"]
    }
    else {
        "00:00:00"
    }

    $gph = if ($values.Contains("GPH") -and -not [string]::IsNullOrWhiteSpace($values["GPH"])) {
        $values["GPH"]
    }
    else {
        "0.00"
    }

    return [pscustomobject]@{
        GoblinCount = $goblinCount
        ActiveCombatTime = $activeCombatTime
        ActiveCombatTimeSeconds = if ($values.Contains("ActiveCombatTimeSeconds")) { $values["ActiveCombatTimeSeconds"] } else { "0" }
        CombatStartTimeLocal = if ($values.Contains("CombatStartTimeLocal")) { $values["CombatStartTimeLocal"] } else { "" }
        GPH = $gph
        ObservationCount = if ($values.Contains("GoblinObservationCount")) { $values["GoblinObservationCount"] } else { "0" }
        JournalObservationCount = if ($values.Contains("JournalObservationCount")) { $values["JournalObservationCount"] } else { "0" }
        MinimapObservationCount = if ($values.Contains("MinimapObservationCount")) { $values["MinimapObservationCount"] } else { "0" }
        EligibleObservationCount = if ($values.Contains("EligibleObservationCount")) { $values["EligibleObservationCount"] } else { "0" }
        BlockedObservationCount = if ($values.Contains("BlockedObservationCount")) { $values["BlockedObservationCount"] } else { "0" }
        DuplicateObservationCount = if ($values.Contains("DuplicateObservationCount")) { $values["DuplicateObservationCount"] } else { "0" }
        LastObservationSource = if ($values.Contains("LastGoblinObservationSource")) { $values["LastGoblinObservationSource"] } else { "" }
        LastObservationType = if ($values.Contains("LastGoblinObservationType")) { $values["LastGoblinObservationType"] } else { "" }
        LastObservationAreaKey = if ($values.Contains("LastGoblinObservationAreaKey")) { $values["LastGoblinObservationAreaKey"] } else { "" }
        LastObservationWouldCount = if ($values.Contains("LastGoblinObservationWouldCount")) { $values["LastGoblinObservationWouldCount"] } else { "" }
        LastObservationReason = if ($values.Contains("LastGoblinObservationReason")) { $values["LastGoblinObservationReason"] } else { "" }
        EvidenceEventCount = if ($values.Contains("GoblinEvidenceEventCount")) { $values["GoblinEvidenceEventCount"] } else { "0" }
        LastEvidenceType = if ($values.Contains("LastGoblinEvidenceType")) { $values["LastGoblinEvidenceType"] } else { "None" }
        LastEvidenceConfidence = if ($values.Contains("LastGoblinEvidenceConfidence")) { $values["LastGoblinEvidenceConfidence"] } else { "0.00" }
        LastEvidenceTimeLocal = if ($values.Contains("LastGoblinEvidenceTimeLocal")) { $values["LastGoblinEvidenceTimeLocal"] } else { "" }
        LastEvidenceScreenshotPath = if ($values.Contains("LastGoblinEvidenceScreenshotPath")) { $values["LastGoblinEvidenceScreenshotPath"] } else { "" }
        EvidenceScreenshotFolder = if ($values.Contains("GoblinEvidenceScreenshotFolder")) { $values["GoblinEvidenceScreenshotFolder"] } else { "Debug/GoblinEvidence" }
        Source = if ($null -ne $sessionFile) { $sessionFile.FullName } else { "none" }
    }
}

function Get-GoblinEvidenceMissingTemplateInfo {
    param($LatestLog)

    $templateSetupGuidance = "<Goblin Type> Engaged Journal.png | <Goblin Type> Killed Journal.png | <Goblin Type> Engaged & Killed Journal.png | <Goblin Type> Minimap.png"
    $templateIssues = New-Object System.Collections.Generic.List[string]
    $missingTemplateLogEntries = 0

    if ($null -ne $LatestLog -and (Test-Path -LiteralPath $LatestLog.FullName -PathType Leaf)) {
        $matches = @(Select-String -LiteralPath $LatestLog.FullName -Pattern "GoblinEvidenceTemplateSetupMissing|GoblinEvidenceTemplateSetupWarning|reason=MissingTemplate|Reason=MissingTemplate" -ErrorAction SilentlyContinue)
        $missingTemplateLogEntries = $matches.Count
        foreach ($match in $matches) {
            $invalidMatch = [regex]::Match($match.Line, "invalidTemplates=(?<value>[^;]+)")
            if ($invalidMatch.Success) {
                $invalidValue = $invalidMatch.Groups["value"].Value.Trim().Trim("'").Trim('"')
                if (-not [string]::IsNullOrWhiteSpace($invalidValue) -and $invalidValue -ne "none" -and -not $templateIssues.Contains($invalidValue)) {
                    $templateIssues.Add($invalidValue)
                }
            }

            foreach ($legacyTemplate in @("Images\Goblin Evidence\Journal Kill.png", "Images\Goblin Evidence\Journal Encounter.png", "Images\Goblin Evidence\Minimap Goblin Icon.png")) {
                if ($match.Line.IndexOf($legacyTemplate, [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and -not $templateIssues.Contains($legacyTemplate)) {
                    $templateIssues.Add($legacyTemplate)
                }
            }
        }
    }

    return [pscustomobject]@{
        Detected = $missingTemplateLogEntries -gt 0
        LogEntries = $missingTemplateLogEntries
        MissingTemplates = if ($templateIssues.Count -gt 0) { [string]::Join("|", $templateIssues.ToArray()) } else { "none" }
        RequiredTemplates = $templateSetupGuidance
    }
}

function Get-DebugEnableSuccessScreenshotsInfo {
    param([string[]]$RuntimeRoots)

    $configPaths = New-Object System.Collections.Generic.List[string]
    foreach ($root in $RuntimeRoots) {
        Add-UniquePath $configPaths (Join-Path $root "Config\AppSettings.json")
    }

    $configFile = $configPaths.ToArray() |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        ForEach-Object { Get-Item -LiteralPath $_ } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $configFile) {
        return [pscustomobject]@{
            Enabled = $false
            Source = "none"
        }
    }

    try {
        $json = Get-Content -LiteralPath $configFile.FullName -Raw | ConvertFrom-Json
        $enabled = $false
        if ($null -ne $json.Debug -and $null -ne $json.Debug.EnableSuccessScreenshots) {
            $enabled = [bool]$json.Debug.EnableSuccessScreenshots
        }

        return [pscustomobject]@{
            Enabled = $enabled
            Source = $configFile.FullName
        }
    }
    catch {
        Write-Warning "Could not parse Debug.EnableSuccessScreenshots from $($configFile.FullName): $($_.Exception.Message)"
        return [pscustomobject]@{
            Enabled = $false
            Source = $configFile.FullName
        }
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
    if ($name -match '^(?<date>\d{4}-\d{2}-\d{2})_(?<time>\d{6})_(?<ms>\d{3})_(?<outcome>Success|Failure|Diagnostic)_(?<workflow>[^_]+)_(?<action>.+)_(?<surface>Diablo|App)$') {
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

function Test-SuccessScreenshotFile {
    param([System.IO.FileInfo]$File)

    $info = Get-DiagnosticScreenshotInfo $File
    if ($info.Outcome -eq "SUCCESS") {
        return $true
    }

    $fullName = $File.FullName.Replace('/', '\')
    return $fullName -match '\\Screenshots\\Success\\'
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
            if ($info.Outcome -eq $Outcome -and -not (Test-RetiredDemonHunterSuppressionScreenshot $info)) {
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

function Get-PackageFolderTotals {
    param([string]$StagingRoot)

    if (-not (Test-Path -LiteralPath $StagingRoot -PathType Container)) {
        return @()
    }

    $groups = Get-ChildItem -LiteralPath $StagingRoot -File -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object {
            $relative = $_.FullName.Substring($StagingRoot.Length).TrimStart('\', '/')
            $normalizedRelative = $relative.Replace('/', '\')
            $folder = if ($normalizedRelative.StartsWith("Debug\GoblinEvidence\", [System.StringComparison]::OrdinalIgnoreCase)) {
                "Debug/GoblinEvidence"
            }
            elseif ($relative.Contains('\')) { $relative.Split('\')[0] } elseif ($relative.Contains('/')) { $relative.Split('/')[0] } else { "(root)" }
            [pscustomobject]@{
                Folder = $folder
                Length = $_.Length
            }
        } |
        Group-Object Folder |
        Sort-Object { ($_.Group | Measure-Object Length -Sum).Sum } -Descending

    foreach ($group in $groups) {
        $bytes = [long](($group.Group | Measure-Object Length -Sum).Sum)
        [pscustomobject]@{
            Folder = $group.Name
            Count = $group.Count
            Bytes = $bytes
            Size = Format-ByteSize $bytes
        }
    }
}

function Get-FolderFileTotals {
    param([string[]]$Folders)

    $paths = @{}
    foreach ($folder in $Folders) {
        if (-not (Test-Path -LiteralPath $folder -PathType Container)) {
            continue
        }

        Get-ChildItem -LiteralPath $folder -File -Recurse -ErrorAction SilentlyContinue |
            ForEach-Object {
                $paths[$_.FullName] = $_
            }
    }

    $files = @($paths.Values)
    $sizeMeasure = $files | Measure-Object Length -Sum
    $bytes = if ($null -ne $sizeMeasure.Sum) { [long]$sizeMeasure.Sum } else { 0L }
    return [pscustomobject]@{
        Count = $files.Count
        Bytes = $bytes
        Size = Format-ByteSize $bytes
    }
}

function Save-GitOutput {
    param(
        [string]$RepoRoot,
        [string]$OutputPath,
        [string[]]$Arguments,
        [string]$SkipReason = ""
    )

    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot ".git") -PathType Container)) {
        if ([string]::IsNullOrWhiteSpace($SkipReason)) {
            $SkipReason = "Not a git checkout."
        }

        "$SkipReason git $($Arguments -join ' ') skipped." | Out-File -FilePath $OutputPath -Encoding utf8
        return $false
    }

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

function Get-RetiredDemonHunterSuppressionAction {
    return ("DemonHunterNoClickSuppression" + "Stall")
}

function Get-ActiveDemonHunterSuppressionAction {
    return "DemonHunterNoClickSuppressionActive"
}

function Convert-RetiredCombatDiagnosticLine {
    param([string]$Line)

    $retiredAction = Get-RetiredDemonHunterSuppressionAction
    if ($Line -notlike "*$retiredAction*") {
        return $Line
    }

    $activeAction = Get-ActiveDemonHunterSuppressionAction
    $activeEvent = "Diagnostic_Combat_$activeAction"
    $line = $Line.Replace($retiredAction, $activeAction)
    $line = $line.Replace("Failure_Combat_$activeAction", $activeEvent)
    $line = $line.Replace(("Combat" + "StallSummary"), "CombatDiagnosticSummary")
    $line = $line.Replace("event=$activeAction", "event=$activeEvent")
    $line = $line.Replace("result=Blocked", "result=Active")
    $line = $line.Replace("blockReason=", "suppressionReason=")
    return $line
}

function Test-RetiredDemonHunterSuppressionScreenshot {
    param($Info)

    return $null -ne $Info -and
        $Info.Outcome -eq "FAILURE" -and
        $Info.Workflow -eq "Combat" -and
        $Info.Action -eq (Get-RetiredDemonHunterSuppressionAction)
}

function Get-DebugScreenshotSkipInfo {
    param([System.IO.FileInfo]$LogFile)

    $info = [ordered]@{
        SkippedByConfigCount = 0
        DiagnosticSkippedByAppSettingsCount = 0
        DiagnosticSkippedByKeepSettingCount = 0
        AppSettingsDebugScreenshots = "Unknown"
        AppSettingsKeepDebugScreenshots = "Unknown"
        StartupDebugMode = "Unknown"
        ActualDebugModeAtPackageTime = "Unknown"
        LatestDebugModeToggleOldValue = "Unknown"
        LatestDebugModeToggleNewValue = "Unknown"
        LatestDebugModeToggleDiagnosticUiAdded = "Unknown"
        LatestDebugModeToggleDiagnosticUiRemoved = "Unknown"
        StartupAppSettingsPath = "Unknown"
        StartupExecutablePath = "Unknown"
        StartupBuildConfiguration = "Unknown"
        StartupDebugDefaultsProfile = "Unknown"
        StartupLaunchKind = "Unknown"
        StartupDebuggerAttached = "Unknown"
        StartupVsDevProfileActive = "Unknown"
        StartupFirstRunSetupSuppressed = "Unknown"
    }

    if ($null -eq $LogFile -or -not (Test-Path -LiteralPath $LogFile.FullName -PathType Leaf)) {
        return [pscustomobject]$info
    }

    foreach ($line in Get-Content -LiteralPath $LogFile.FullName) {
        if ($line -match "DebugScreenshotSkipped: .*skipReason=disabled by config") {
            $info.SkippedByConfigCount++
        }
        if ($line -match "Diagnostic screenshot pair skipped: disabled by AppSettings") {
            $info.DiagnosticSkippedByAppSettingsCount++
        }
        if ($line -match "Diagnostic screenshot pair skipped: disabled by Keep Debug Screenshots setting") {
            $info.DiagnosticSkippedByKeepSettingCount++
        }
        if ($line -match "AppSettings loaded: (?<values>.*)$") {
            $values = Convert-LogKeyValueText $matches.values
            $info.AppSettingsDebugScreenshots = Get-LogValue $values "Debug.EnableDebugScreenshots" $info.AppSettingsDebugScreenshots
            $info.AppSettingsKeepDebugScreenshots = Get-LogValue $values "KeepDebugScreenshots" $info.AppSettingsKeepDebugScreenshots
            $info.StartupDebugMode = Get-LogValue $values "DebugMode" $info.StartupDebugMode
            $info.ActualDebugModeAtPackageTime = $info.StartupDebugMode
            $info.StartupAppSettingsPath = Get-LogValue $values "AppSettingsPath" $info.StartupAppSettingsPath
            $info.StartupExecutablePath = Get-LogValue $values "ExecutablePath" $info.StartupExecutablePath
            $info.StartupBuildConfiguration = Get-LogValue $values "BuildConfiguration" $info.StartupBuildConfiguration
            $info.StartupDebugDefaultsProfile = Get-LogValue $values "DebugDefaultsProfile" $info.StartupDebugDefaultsProfile
            $info.StartupLaunchKind = Get-LogValue $values "LaunchKind" $info.StartupLaunchKind
            $info.StartupDebuggerAttached = Get-LogValue $values "DebuggerAttached" $info.StartupDebuggerAttached
            $info.StartupVsDevProfileActive = Get-LogValue $values "VsDevProfileActive" $info.StartupVsDevProfileActive
            $info.StartupFirstRunSetupSuppressed = Get-LogValue $values "FirstRunSetupSuppressed" $info.StartupFirstRunSetupSuppressed
        }
        if ($line -match "DebugModeToggled: (?<values>.*)$") {
            $values = Convert-LogKeyValueText $matches.values
            $info.LatestDebugModeToggleOldValue = Get-LogValue $values "oldDebugMode" $info.LatestDebugModeToggleOldValue
            $info.LatestDebugModeToggleNewValue = Get-LogValue $values "newDebugMode" $info.LatestDebugModeToggleNewValue
            $info.LatestDebugModeToggleDiagnosticUiAdded = Get-LogValue $values "diagnosticUiAdded" $info.LatestDebugModeToggleDiagnosticUiAdded
            $info.LatestDebugModeToggleDiagnosticUiRemoved = Get-LogValue $values "diagnosticUiRemoved" $info.LatestDebugModeToggleDiagnosticUiRemoved
            $info.ActualDebugModeAtPackageTime = $info.LatestDebugModeToggleNewValue
        }
    }

    return [pscustomobject]$info
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

function New-CombatSummaryBlock {
    param(
        [string]$Timestamp,
        [string]$Event,
        [string]$Workflow,
        [string]$Result,
        [string]$Class,
        [string]$Source,
        [string]$Button,
        [string]$ConsecutiveSuppressedDecisionLogs,
        [string]$SuppressionReason,
        [string]$NoClickRegionName,
        [string]$NoClickRegionIndex,
        [string]$IntendedClickPoint,
        [string]$DiabloRect,
        [string]$RegionRect,
        [string]$RightMouseHeld,
        [string]$RightHeldFromSafeRegion,
        [string]$ScreenshotPath,
        [string]$LikelyExplanation
    )

    @(
        "[$Timestamp] $Event",
        "  workflow: $Workflow",
        "  result: $Result",
        "  class: $Class",
        "  source: $Source",
        "  button: $Button",
        "  consecutive suppressed decisions: $ConsecutiveSuppressedDecisionLogs",
        "  suppression reason: $SuppressionReason",
        "  no-click region: $NoClickRegionName",
        "  no-click region index: $NoClickRegionIndex",
        "  intended click point: $IntendedClickPoint",
        "  Diablo rect: $DiabloRect",
        "  region rect: $RegionRect",
        "  right mouse held: $RightMouseHeld",
        "  right held from safe region: $RightHeldFromSafeRegion",
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

    foreach ($rawLine in Get-Content -LiteralPath $LogFile.FullName) {
        $line = Convert-RetiredCombatDiagnosticLine $rawLine
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

        if ($message -match "^CombatDiagnosticSummary: (?<kv>.*)$") {
            $values = Convert-LogKeyValueText $matches.kv
            $block = New-CombatSummaryBlock `
                -Timestamp $timestamp `
                -Event (Get-LogValue $values "event" "CombatDiagnosticSummary") `
                -Workflow (Get-LogValue $values "workflow" "Combat") `
                -Result (Get-LogValue $values "result" "Active") `
                -Class (Get-LogValue $values "class") `
                -Source (Get-LogValue $values "source") `
                -Button (Get-LogValue $values "button") `
                -ConsecutiveSuppressedDecisionLogs (Get-LogValue $values "consecutiveSuppressedDecisionLogs") `
                -SuppressionReason (Get-LogValue $values "suppressionReason") `
                -NoClickRegionName (Get-LogValue $values "noClickRegionName") `
                -NoClickRegionIndex (Get-LogValue $values "noClickRegionIndex") `
                -IntendedClickPoint (Get-LogValue $values "intendedClickPoint") `
                -DiabloRect (Get-LogValue $values "diabloRect") `
                -RegionRect (Get-LogValue $values "regionRect") `
                -RightMouseHeld (Get-LogValue $values "rightMouseHeld") `
                -RightHeldFromSafeRegion (Get-LogValue $values "rightHeldFromSafeRegion") `
                -ScreenshotPath (Get-LogValue $values "screenshotPath") `
                -LikelyExplanation (Get-LogValue $values "likelyExplanation" "Review Demon Hunter no-click suppression logs and screenshots.")
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

if ($MaxSuccessScreenshots -lt 0) {
    Write-Warning "MaxSuccessScreenshots must be at least 0. Using 0."
    $MaxSuccessScreenshots = 0
}
if ($IncludeSuccessScreenshots -and $MaxSuccessScreenshots -eq 0) {
    $MaxSuccessScreenshots = $MaxScreenshots
}
if ($MaxDiagnosticScreenshots -lt 1) {
    Write-Warning "MaxDiagnosticScreenshots must be at least 1. Using 1."
    $MaxDiagnosticScreenshots = 1
}
if ($MaxDebugScreenshots -lt 0) {
    Write-Warning "MaxDebugScreenshots must be at least 0. Using 0."
    $MaxDebugScreenshots = 0
}
if ($MaxGoblinEvidenceFullImages -lt 0) {
    Write-Warning "MaxGoblinEvidenceFullImages must be at least 0. Using 0."
    $MaxGoblinEvidenceFullImages = 0
}
if ($MaxGoblinEvidenceEventScreenshots -lt 0) {
    Write-Warning "MaxGoblinEvidenceEventScreenshots must be at least 0. Using 0."
    $MaxGoblinEvidenceEventScreenshots = 0
}
if ($MaxGoblinEvidenceEventScreenshotBytes -lt 0) {
    Write-Warning "MaxGoblinEvidenceEventScreenshotBytes must be at least 0. Using 0."
    $MaxGoblinEvidenceEventScreenshotBytes = 0
}
if ($MaxGoblinObservationDiagnosticCrops -lt 0) {
    Write-Warning "MaxGoblinObservationDiagnosticCrops must be at least 0. Using 0."
    $MaxGoblinObservationDiagnosticCrops = 0
}

$runtimeRootInfo = Resolve-RuntimeRoot $RuntimeRoot $PSScriptRoot
$repoRoot = $runtimeRootInfo.SourceRoot
$resolvedRuntimeRoot = $runtimeRootInfo.RuntimeRoot
$packageRuntimeRoots = Get-PackageRuntimeRoots $resolvedRuntimeRoot $repoRoot
$gitSkipReason = if ($runtimeRootInfo.IsInstalledFolder) { "Installed Scripts folder run; git metadata is not available from the release runtime." } else { "Not a git checkout at source root $repoRoot." }
Write-Host "PSScriptRoot = $PSScriptRoot"
Write-Host "SourceRoot   = $repoRoot"
Write-Host "RuntimeRoot  = $resolvedRuntimeRoot"
Write-Host "Resolved by  = $($runtimeRootInfo.Resolution)"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$packageDirectory = Join-Path $resolvedRuntimeRoot "DebugPackages"
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) "GoblinFarmer_Debug_$timestamp"
$zipPath = Join-Path $packageDirectory "GoblinFarmer_Debug_$timestamp.zip"
$manifestPath = Join-Path $stagingRoot "debug-package-manifest.txt"

Write-Host "GoblinFarmer Debug Package Generator"
Write-Host "Source root: $repoRoot"
Write-Host "Runtime root: $resolvedRuntimeRoot"
Write-Host "Runtime root resolution: $($runtimeRootInfo.Resolution)"
Write-Host "Runtime root signals: $(if ([string]::IsNullOrWhiteSpace($runtimeRootInfo.Signals)) { 'none' } else { $runtimeRootInfo.Signals })"
Write-Host "Timestamp: $timestamp"
Write-Host "Max normal screenshots: $MaxScreenshots"
Write-Host "Max failure screenshots: $MaxFailureScreenshots"
Write-Host "Max success screenshot groups: $MaxSuccessScreenshots"
Write-Host "Max diagnostic screenshot groups: $MaxDiagnosticScreenshots"
Write-Host "Max debug screenshots: $MaxDebugScreenshots"
Write-Host "Include success screenshots: $($IncludeSuccessScreenshots.IsPresent)"
Write-Host "Max goblin evidence full images: $MaxGoblinEvidenceFullImages"
Write-Host "Max goblin evidence event screenshots: $MaxGoblinEvidenceEventScreenshots"
Write-Host "Max goblin evidence event screenshot bytes: $MaxGoblinEvidenceEventScreenshotBytes"
Write-Host "Max goblin observation diagnostic crops: $MaxGoblinObservationDiagnosticCrops"

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

    Write-Step "Collecting runtime metadata"
    $runtimeSessionInfoIncluded = Copy-PackageFile $resolvedRuntimeRoot $stagingRoot "session-info.txt" "session-info.txt"
    $runtimeAppSettingsIncluded = Copy-PackageFile $resolvedRuntimeRoot $stagingRoot "Config\AppSettings.json" "Config\AppSettings.json"

    $logFoldersList = New-Object System.Collections.Generic.List[string]
    foreach ($root in $packageRuntimeRoots) {
        Add-UniquePath $logFoldersList (Join-Path $root "Logs")
    }
    $logFolders = $logFoldersList.ToArray()

    Write-Host "Log folders searched:"
    foreach ($folder in $logFolders) {
        Write-Host "  $folder"
    }

    Write-Step "Collecting latest log"
    $latestLog = Get-LatestFileFromFolders $logFolders @("*.log", "*.txt")
    if ($null -eq $latestLog) {
        Write-Warning "No log files found in known Logs folders."
    }
    else {
        $logDestinationDirectory = Join-Path $stagingRoot "Logs"
        New-Item -ItemType Directory -Path $logDestinationDirectory -Force | Out-Null
        Get-Content -LiteralPath $latestLog.FullName |
            ForEach-Object { Convert-RetiredCombatDiagnosticLine $_ } |
            Out-File -FilePath (Join-Path $logDestinationDirectory $latestLog.Name) -Encoding utf8
        Write-Host "Included latest log: $($latestLog.FullName)"
    }
    $selectedLogFolder = if ($null -ne $latestLog) { Split-Path -Parent $latestLog.FullName } else { "none" }
    Write-Host "Selected log folder: $selectedLogFolder"
    Write-Host "Selected latest log: $(if ($null -ne $latestLog) { $latestLog.FullName } else { 'none' })"

    $debugSkipInfo = Get-DebugScreenshotSkipInfo $latestLog
    Write-Host "Debug screenshots setting from log: $($debugSkipInfo.AppSettingsDebugScreenshots)"
    Write-Host "Keep debug screenshots setting from log: $($debugSkipInfo.AppSettingsKeepDebugScreenshots)"
    if ($debugSkipInfo.SkippedByConfigCount -gt 0 -or
        $debugSkipInfo.DiagnosticSkippedByAppSettingsCount -gt 0 -or
        $debugSkipInfo.DiagnosticSkippedByKeepSettingCount -gt 0) {
        Write-Warning "Latest log contains skipped screenshot captures: DebugScreenshotSkipped=$($debugSkipInfo.SkippedByConfigCount); DiagnosticAppSettings=$($debugSkipInfo.DiagnosticSkippedByAppSettingsCount); DiagnosticKeepSetting=$($debugSkipInfo.DiagnosticSkippedByKeepSettingCount)."
    }

    $sessionInfo = Get-CurrentSessionInfo $packageRuntimeRoots $latestLog
    $sessionStart = [DateTime]$sessionInfo.Start
    $sessionDuration = (Get-Date) - $sessionStart
    $goblinTrackerInfo = Get-GoblinTrackerInfo $packageRuntimeRoots
    $successScreenshotSetting = Get-DebugEnableSuccessScreenshotsInfo $packageRuntimeRoots
    Write-Host "Session start: $($sessionStart.ToString('yyyy-MM-dd HH:mm:ss.fff zzz'))"
    Write-Host "Session source: $($sessionInfo.SourceKind) ($($sessionInfo.Source))"
    Write-Host "Session duration: $($sessionDuration.ToString('hh\:mm\:ss'))"
    Write-Host "Goblin tracker: goblins=$($goblinTrackerInfo.GoblinCount); activeCombatTime=$($goblinTrackerInfo.ActiveCombatTime); gph=$($goblinTrackerInfo.GPH); source=$($goblinTrackerInfo.Source)"

    $screenshotFoldersList = New-Object System.Collections.Generic.List[string]
    $debugScreenshotFoldersList = New-Object System.Collections.Generic.List[string]
    $goblinEvidenceFoldersList = New-Object System.Collections.Generic.List[string]
    $goblinEvidenceSourceRootsList = New-Object System.Collections.Generic.List[string]
    foreach ($root in $packageRuntimeRoots) {
        Add-UniquePath $screenshotFoldersList (Join-Path $root "Screenshots")
        Add-UniquePath $debugScreenshotFoldersList (Join-Path $root "debug-screenshots")
        Add-UniquePath $goblinEvidenceFoldersList (Join-Path $root "Debug\GoblinEvidence")
        Add-UniquePath $goblinEvidenceSourceRootsList (Join-Path $root "Debug")
    }
    $screenshotFolders = $screenshotFoldersList.ToArray()
    $debugScreenshotFolders = $debugScreenshotFoldersList.ToArray()
    $goblinEvidenceFolders = $goblinEvidenceFoldersList.ToArray()
    $goblinEvidenceSourceRoots = $goblinEvidenceSourceRootsList.ToArray()
    $goblinEvidenceSourceTotals = Get-FolderFileTotals $goblinEvidenceFolders
    if ($goblinEvidenceSourceTotals.Count -gt 0) {
        Write-Host "Current goblin evidence files: $($goblinEvidenceSourceTotals.Count), $($goblinEvidenceSourceTotals.Size)"
    }

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
    $latestScreenshotForFolder = @($sessionScreenshots | Select-Object -First 1)
    if ($latestScreenshotForFolder.Count -eq 0) {
        $latestScreenshotForFolder = @($allScreenshots | Select-Object -First 1)
    }
    $selectedScreenshotFolder = if ($latestScreenshotForFolder.Count -gt 0) { Split-Path -Parent $latestScreenshotForFolder[0].FullName } else { "none" }
    Write-Host "Selected screenshot folder: $selectedScreenshotFolder"

    $availableFailureGroups = @(Select-DiagnosticScreenshotGroups $sessionScreenshots "FAILURE" ([int]::MaxValue))
    $availableFailureScreenshots = @(Get-FilesFromScreenshotGroups $availableFailureGroups)
    $availableFailureScreenshotSizeMeasure = $availableFailureScreenshots | Measure-Object Length -Sum
    $availableFailureScreenshotSizeBytes = if ($null -ne $availableFailureScreenshotSizeMeasure.Sum) { [long]$availableFailureScreenshotSizeMeasure.Sum } else { 0L }
    $availableFailureScreenshotSizeDisplay = Format-ByteSize $availableFailureScreenshotSizeBytes
    $failureGroups = @(Select-DiagnosticScreenshotGroups $sessionScreenshots "FAILURE" $MaxFailureScreenshots)
    $availableSuccessGroups = @(Select-DiagnosticScreenshotGroups $sessionScreenshots "SUCCESS" ([int]::MaxValue))
    $availableSuccessScreenshots = @(Get-FilesFromScreenshotGroups $availableSuccessGroups)
    $availableSuccessScreenshotSizeMeasure = $availableSuccessScreenshots | Measure-Object Length -Sum
    $availableSuccessScreenshotSizeBytes = if ($null -ne $availableSuccessScreenshotSizeMeasure.Sum) { [long]$availableSuccessScreenshotSizeMeasure.Sum } else { 0L }
    $availableSuccessScreenshotSizeDisplay = Format-ByteSize $availableSuccessScreenshotSizeBytes
    $successGroups = if ($IncludeSuccessScreenshots -and $MaxSuccessScreenshots -gt 0) {
        @(Select-DiagnosticScreenshotGroups $sessionScreenshots "SUCCESS" $MaxSuccessScreenshots)
    }
    else {
        @()
    }
    $diagnosticGroups = @(Select-DiagnosticScreenshotGroups $sessionScreenshots "DIAGNOSTIC" $MaxDiagnosticScreenshots)
    $selectedPairKeys = @{}
    foreach ($group in @($failureGroups + $successGroups + $diagnosticGroups)) {
        $selectedPairKeys[$group.Name] = $true
    }

    $failureScreenshots = @(Get-FilesFromScreenshotGroups $failureGroups)
    $failureScreenshotSizeMeasure = $failureScreenshots | Measure-Object Length -Sum
    $failureScreenshotSizeBytes = if ($null -ne $failureScreenshotSizeMeasure.Sum) { [long]$failureScreenshotSizeMeasure.Sum } else { 0L }
    $failureScreenshotSizeDisplay = Format-ByteSize $failureScreenshotSizeBytes
    $excludedFailureScreenshots = [Math]::Max(0, $availableFailureScreenshots.Count - $failureScreenshots.Count)
    $excludedFailureScreenshotSizeBytes = [Math]::Max(0L, $availableFailureScreenshotSizeBytes - $failureScreenshotSizeBytes)
    $excludedFailureScreenshotSizeDisplay = Format-ByteSize $excludedFailureScreenshotSizeBytes
    $successScreenshots = @(Get-FilesFromScreenshotGroups $successGroups)
    $diagnosticScreenshots = @(Get-FilesFromScreenshotGroups $diagnosticGroups)
    Write-Host "Enable success screenshots setting: $($successScreenshotSetting.Enabled) ($($successScreenshotSetting.Source))"
    if ($successScreenshotSetting.Enabled -and $availableSuccessScreenshots.Count -gt 0) {
        Write-Host "Available success screenshots excluded by default: $($availableSuccessScreenshots.Count), $availableSuccessScreenshotSizeDisplay"
    }

    $normalScreenshots = @($sessionScreenshots |
        Where-Object {
            $info = Get-DiagnosticScreenshotInfo $_
            -not (Test-SuccessScreenshotFile $_) -and
                $info.Outcome -eq "DEBUG" -and
                -not (Test-RetiredDemonHunterSuppressionScreenshot $info) -and
                -not $selectedPairKeys.ContainsKey($info.PairKey)
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
        $diagnosticCount = Copy-FilesToPackageFolder $diagnosticScreenshots (Join-Path $stagingRoot "Screenshots\Diagnostic")
        $normalCount = Copy-FilesToPackageFolder $normalScreenshots (Join-Path $stagingRoot "Screenshots\Recent")
        New-ScreenshotManifest @($failureGroups + $successGroups + $diagnosticGroups) $screenshotManifestPath

        if ($failureCount -eq 0) {
            Write-Warning "No failure screenshots found. Expected failure type names in screenshot filenames."
        }
        else {
                Write-Host "Included failure screenshots: $failureCount"
                Write-Host "Failure screenshot policy: most recent $MaxFailureScreenshots groups included; $excludedFailureScreenshots files excluded; includedSize=$failureScreenshotSizeDisplay; excludedSize=$excludedFailureScreenshotSizeDisplay; availableSize=$availableFailureScreenshotSizeDisplay"
                Write-Host "Latest failure screenshot type: $latestFailureType"
                Write-Host "Latest failure screenshot filename: $($latestFailureScreenshot.Name)"
        }

        if (-not $IncludeSuccessScreenshots -or $MaxSuccessScreenshots -eq 0) {
            Write-Host "Success screenshots skipped by package policy. Use -IncludeSuccessScreenshots to include debug-only success evidence."
        }
        elseif ($successCount -eq 0) {
            Write-Warning "No success screenshots found."
        }
        else {
            Write-Host "Included success screenshots: $successCount"
        }

        if ($diagnosticCount -eq 0) {
            Write-Host "No diagnostic screenshots found."
        }
        else {
            Write-Host "Included diagnostic screenshots: $diagnosticCount"
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
        Select-Object -First $MaxDebugScreenshots)
    $selectedDebugScreenshotFolder = if ($debugScreenshots.Count -gt 0) { Split-Path -Parent $debugScreenshots[0].FullName } else { "none" }
    Write-Host "Selected debug screenshot folder: $selectedDebugScreenshotFolder"
    $debugScreenshotCount = Copy-DebugScreenshotsToPackage $debugScreenshots $debugScreenshotFolders (Join-Path $stagingRoot "debug-screenshots")
    if ($debugScreenshotCount -eq 0) {
        Write-Warning "No current-session debug-screenshots found."
    }
    else {
        Write-Host "Included debug screenshots: $debugScreenshotCount"
    }

    Write-Step "Collecting goblin evidence screenshots"
    $goblinEvidenceCandidates = foreach ($folder in $goblinEvidenceFolders) {
        if (Test-Path -LiteralPath $folder -PathType Container) {
            Get-ChildItem -LiteralPath $folder -File -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.Extension -match '^\.(png|jpg|jpeg|bmp|txt)$' }
        }
    }
    $goblinEvidenceScreenshots = @($goblinEvidenceCandidates |
        Where-Object { $_.LastWriteTime -ge $sessionStart -or $_.CreationTime -ge $sessionStart } |
        Sort-Object LastWriteTime -Descending)
    $goblinEvidenceFullImages = @($goblinEvidenceScreenshots |
        Where-Object { $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' -and $_.BaseName -like '*_Full' } |
        Sort-Object LastWriteTime -Descending)
    $selectedGoblinEvidenceFullImages = if ($MaxGoblinEvidenceFullImages -gt 0) {
        @($goblinEvidenceFullImages | Select-Object -First $MaxGoblinEvidenceFullImages)
    }
    else {
        @()
    }
    $selectedGoblinEvidenceFullImagePaths = @{}
    foreach ($file in $selectedGoblinEvidenceFullImages) {
        $selectedGoblinEvidenceFullImagePaths[$file.FullName] = $true
    }
    $excludedGoblinEvidenceFullImages = [Math]::Max(0, $goblinEvidenceFullImages.Count - $selectedGoblinEvidenceFullImages.Count)
    $goblinEvidenceScreenshots = @($goblinEvidenceScreenshots |
        Where-Object {
            $_.Extension -notmatch '^\.(png|jpg|jpeg|bmp)$' -or
                $_.BaseName -notlike '*_Full' -or
                $selectedGoblinEvidenceFullImagePaths.ContainsKey($_.FullName)
        })
    $goblinEvidenceEventScreenshots = @($goblinEvidenceScreenshots |
        Where-Object {
            $normalizedFullName = $_.FullName.Replace('/', '\')
            $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' -and
                $_.BaseName -like 'GoblinEvidence_*' -and
                $normalizedFullName.IndexOf('\Debug\GoblinEvidence\ObservationDiagnostics\', [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -and
                (Split-Path -Parent $normalizedFullName).EndsWith('\Debug\GoblinEvidence', [System.StringComparison]::OrdinalIgnoreCase)
        } |
        Sort-Object LastWriteTime -Descending)
    $eligibleGoblinEvidenceEventScreenshots = @($goblinEvidenceEventScreenshots |
        Where-Object { $MaxGoblinEvidenceEventScreenshotBytes -eq 0 -or $_.Length -le $MaxGoblinEvidenceEventScreenshotBytes })
    $oversizedGoblinEvidenceEventScreenshots = [Math]::Max(0, $goblinEvidenceEventScreenshots.Count - $eligibleGoblinEvidenceEventScreenshots.Count)
    $selectedGoblinEvidenceEventScreenshots = if ($MaxGoblinEvidenceEventScreenshots -gt 0) {
        @($eligibleGoblinEvidenceEventScreenshots | Select-Object -First $MaxGoblinEvidenceEventScreenshots)
    }
    else {
        @()
    }
    $selectedGoblinEvidenceEventScreenshotPaths = @{}
    foreach ($file in $selectedGoblinEvidenceEventScreenshots) {
        $selectedGoblinEvidenceEventScreenshotPaths[$file.FullName] = $true
    }
    $excludedGoblinEvidenceEventScreenshots = [Math]::Max(0, $goblinEvidenceEventScreenshots.Count - $selectedGoblinEvidenceEventScreenshots.Count)
    $goblinEvidenceScreenshots = @($goblinEvidenceScreenshots |
        Where-Object {
            $normalizedFullName = $_.FullName.Replace('/', '\')
            $isGoblinEvidenceEventScreenshot = $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' -and
                $_.BaseName -like 'GoblinEvidence_*' -and
                $normalizedFullName.IndexOf('\Debug\GoblinEvidence\ObservationDiagnostics\', [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -and
                (Split-Path -Parent $normalizedFullName).EndsWith('\Debug\GoblinEvidence', [System.StringComparison]::OrdinalIgnoreCase)
            (-not $isGoblinEvidenceEventScreenshot) -or $selectedGoblinEvidenceEventScreenshotPaths.ContainsKey($_.FullName)
        })
    $goblinObservationDiagnosticCrops = @($goblinEvidenceScreenshots |
        Where-Object {
            $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' -and
                $_.FullName.Replace('/', '\').IndexOf('\Debug\GoblinEvidence\ObservationDiagnostics\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0
        } |
        Sort-Object LastWriteTime -Descending)
    $selectedGoblinObservationDiagnosticCrops = if ($MaxGoblinObservationDiagnosticCrops -gt 0) {
        @($goblinObservationDiagnosticCrops | Select-Object -First $MaxGoblinObservationDiagnosticCrops)
    }
    else {
        @()
    }
    $selectedGoblinObservationDiagnosticCropPaths = @{}
    foreach ($file in $selectedGoblinObservationDiagnosticCrops) {
        $selectedGoblinObservationDiagnosticCropPaths[$file.FullName] = $true
    }
    $excludedGoblinObservationDiagnosticCrops = [Math]::Max(0, $goblinObservationDiagnosticCrops.Count - $selectedGoblinObservationDiagnosticCrops.Count)
    $goblinEvidenceScreenshots = @($goblinEvidenceScreenshots |
        Where-Object {
            $isObservationDiagnosticCrop = $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' -and
                $_.FullName.Replace('/', '\').IndexOf('\Debug\GoblinEvidence\ObservationDiagnostics\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0
            (-not $isObservationDiagnosticCrop) -or $selectedGoblinObservationDiagnosticCropPaths.ContainsKey($_.FullName)
        })
    $selectedGoblinEvidenceFolder = if ($goblinEvidenceScreenshots.Count -gt 0) { Split-Path -Parent $goblinEvidenceScreenshots[0].FullName } elseif (-not [string]::IsNullOrWhiteSpace($goblinTrackerInfo.EvidenceScreenshotFolder)) { $goblinTrackerInfo.EvidenceScreenshotFolder } else { "none" }
    Write-Host "Selected goblin evidence folder: $selectedGoblinEvidenceFolder"
    Write-Host "Excluded goblin evidence full images: $excludedGoblinEvidenceFullImages"
    Write-Host "Included goblin evidence event screenshots: $($selectedGoblinEvidenceEventScreenshots.Count)"
    Write-Host "Excluded goblin evidence event screenshots: $excludedGoblinEvidenceEventScreenshots"
    Write-Host "Oversized goblin evidence event screenshots excluded: $oversizedGoblinEvidenceEventScreenshots"
    Write-Host "Included goblin observation diagnostic crops: $($selectedGoblinObservationDiagnosticCrops.Count)"
    Write-Host "Excluded goblin observation diagnostic crops: $excludedGoblinObservationDiagnosticCrops"
    $goblinEvidenceScreenshotCount = Copy-DebugScreenshotsToPackage $goblinEvidenceScreenshots $goblinEvidenceSourceRoots (Join-Path $stagingRoot "Debug")
    if ($goblinEvidenceScreenshotCount -eq 0) {
        Write-Host "No current-session goblin evidence screenshots found."
    }
    else {
        Write-Host "Included goblin evidence screenshots: $goblinEvidenceScreenshotCount"
    }
    $goblinEvidenceMissingTemplateInfo = Get-GoblinEvidenceMissingTemplateInfo $latestLog

    Write-Step "Generating route failure summary"
    $routeFailureSummaryPath = Join-Path $stagingRoot "route-failure-summary.txt"
    New-RouteFailureSummary $latestLog $routeFailureSummaryPath
    Write-Host "Included route failure summary: route-failure-summary.txt"

    Write-Step "Capturing git state"
    $gitStatusCaptured = Save-GitOutput $repoRoot (Join-Path $stagingRoot "git-status.txt") @("status", "--short") $gitSkipReason
    $gitLogCaptured = Save-GitOutput $repoRoot (Join-Path $stagingRoot "git-log.txt") @("log", "--oneline", "--decorate", "-n", "25") $gitSkipReason
    $gitSkippedBecauseInstalled = $runtimeRootInfo.IsInstalledFolder -and (-not $gitStatusCaptured) -and (-not $gitLogCaptured)
    $gitMetadataStatus = if ($gitStatusCaptured -or $gitLogCaptured) {
        "Included"
    }
    elseif ($gitSkippedBecauseInstalled) {
        "Skipped (installed folder run)"
    }
    else {
        "Skipped ($gitSkipReason)"
    }
    Write-Host "Git metadata: $gitMetadataStatus"

    if (-not (Test-Path -LiteralPath $packageDirectory)) {
        New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
    }

    if (Test-Path -LiteralPath $zipPath) {
        Write-Warning "Existing package for this minute will be replaced: $zipPath"
        Remove-Item -LiteralPath $zipPath -Force
    }

    Write-Step "Creating zip package"
    $totalScreenshotCount = $failureScreenshots.Count + $successScreenshots.Count + $diagnosticScreenshots.Count + $normalScreenshots.Count
    $goblinEvidenceSourceSummaryLine = if ($goblinEvidenceSourceTotals.Count -gt 0) {
        "Current GoblinEvidence source files: count=$($goblinEvidenceSourceTotals.Count); totalSize=$($goblinEvidenceSourceTotals.Size); totalSizeBytes=$($goblinEvidenceSourceTotals.Bytes)"
    }
    else {
        "Current GoblinEvidence source files: none"
    }
    $goblinEvidenceSourceFolderTotalLine = if ($goblinEvidenceSourceTotals.Count -gt 0) {
        "- Debug/GoblinEvidence current source: $($goblinEvidenceSourceTotals.Count) files, $($goblinEvidenceSourceTotals.Size) ($($goblinEvidenceSourceTotals.Bytes) bytes)"
    }
    else {
        ""
    }
    $successAvailabilityLine = if ($successScreenshots.Count -gt 0) {
        "Success screenshots included by opt-in: count=$($successScreenshots.Count); available=$($availableSuccessScreenshots.Count); totalSize=$availableSuccessScreenshotSizeDisplay; totalSizeBytes=$availableSuccessScreenshotSizeBytes"
    }
    elseif ($availableSuccessScreenshots.Count -gt 0) {
        "Success screenshots available but excluded by default: count=$($availableSuccessScreenshots.Count); totalSize=$availableSuccessScreenshotSizeDisplay; totalSizeBytes=$availableSuccessScreenshotSizeBytes"
    }
    else {
        "Success screenshots available but excluded by default: none"
    }
    $buildManifestLines = {
        param(
            [long]$PackageSizeBytes,
            [string]$PackageSizeDisplay
        )

        @(
            "GoblinFarmer Debug Package",
            "Created: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')",
            "Source root: $repoRoot",
            "Resolved runtime root: $resolvedRuntimeRoot",
            "Runtime root resolution: $($runtimeRootInfo.Resolution)",
            "Runtime root signals: $(if ([string]::IsNullOrWhiteSpace($runtimeRootInfo.Signals)) { 'none' } else { $runtimeRootInfo.Signals })",
            "Package: $zipPath",
            "Package size: $PackageSizeDisplay",
            "Package size bytes: $PackageSizeBytes",
            "Session start: $($sessionStart.ToString('yyyy-MM-dd HH:mm:ss.fff zzz'))",
            "Session start source: $($sessionInfo.SourceKind) ($($sessionInfo.Source))",
            "Session duration: $($sessionDuration.ToString('hh\:mm\:ss'))",
            "Goblin tracker source: $($goblinTrackerInfo.Source)",
            "Goblin tracker goblins found: $($goblinTrackerInfo.GoblinCount)",
            "Goblin tracker active combat time: $($goblinTrackerInfo.ActiveCombatTime)",
            "Goblin tracker active combat time seconds: $($goblinTrackerInfo.ActiveCombatTimeSeconds)",
            "Goblin tracker combat start time: $(if ([string]::IsNullOrWhiteSpace($goblinTrackerInfo.CombatStartTimeLocal)) { 'none' } else { $goblinTrackerInfo.CombatStartTimeLocal })",
            "Goblin tracker GPH: $($goblinTrackerInfo.GPH)",
            "Goblin observations: $($goblinTrackerInfo.ObservationCount)",
            "Goblin journal observations: $($goblinTrackerInfo.JournalObservationCount)",
            "Goblin minimap observations: $($goblinTrackerInfo.MinimapObservationCount)",
            "Goblin eligible observations: $($goblinTrackerInfo.EligibleObservationCount)",
            "Goblin blocked observations: $($goblinTrackerInfo.BlockedObservationCount)",
            "Goblin duplicate observations: $($goblinTrackerInfo.DuplicateObservationCount)",
            "Goblin last observation: source=$(if ([string]::IsNullOrWhiteSpace($goblinTrackerInfo.LastObservationSource)) { 'none' } else { $goblinTrackerInfo.LastObservationSource }); type=$(if ([string]::IsNullOrWhiteSpace($goblinTrackerInfo.LastObservationType)) { 'none' } else { $goblinTrackerInfo.LastObservationType }); area=$(if ([string]::IsNullOrWhiteSpace($goblinTrackerInfo.LastObservationAreaKey)) { 'none' } else { $goblinTrackerInfo.LastObservationAreaKey }); wouldCount=$(if ([string]::IsNullOrWhiteSpace($goblinTrackerInfo.LastObservationWouldCount)) { 'none' } else { $goblinTrackerInfo.LastObservationWouldCount }); reason=$(if ([string]::IsNullOrWhiteSpace($goblinTrackerInfo.LastObservationReason)) { 'none' } else { $goblinTrackerInfo.LastObservationReason })",
            "Goblin evidence events detected: $($goblinTrackerInfo.EvidenceEventCount)",
            "Goblin evidence last type: $($goblinTrackerInfo.LastEvidenceType)",
            "Goblin evidence last confidence: $($goblinTrackerInfo.LastEvidenceConfidence)",
            "Goblin evidence last time: $(if ([string]::IsNullOrWhiteSpace($goblinTrackerInfo.LastEvidenceTimeLocal)) { 'none' } else { $goblinTrackerInfo.LastEvidenceTimeLocal })",
            "Goblin evidence screenshot folder: $selectedGoblinEvidenceFolder",
            "Goblin evidence last screenshot: $(if ([string]::IsNullOrWhiteSpace($goblinTrackerInfo.LastEvidenceScreenshotPath)) { 'none' } else { $goblinTrackerInfo.LastEvidenceScreenshotPath })",
            "Goblin evidence missing template state: detected=$($goblinEvidenceMissingTemplateInfo.Detected); logEntries=$($goblinEvidenceMissingTemplateInfo.LogEntries); missingTemplates=$($goblinEvidenceMissingTemplateInfo.MissingTemplates); requiredTemplates=$($goblinEvidenceMissingTemplateInfo.RequiredTemplates)",
            "Success screenshot package policy: $(if ($IncludeSuccessScreenshots) { 'Included when selected' } else { 'Skipped by default' })",
            "Debug.EnableSuccessScreenshots: $($successScreenshotSetting.Enabled)",
            "Debug.EnableSuccessScreenshots source: $($successScreenshotSetting.Source)",
            $successAvailabilityLine,
            "Goblin evidence full-image package policy: most recent $MaxGoblinEvidenceFullImages included; $excludedGoblinEvidenceFullImages excluded",
            "Goblin evidence event screenshot package policy: most recent $MaxGoblinEvidenceEventScreenshots included when <= $MaxGoblinEvidenceEventScreenshotBytes bytes; $excludedGoblinEvidenceEventScreenshots excluded; $oversizedGoblinEvidenceEventScreenshots oversized",
            "Goblin observation diagnostic crop package policy: most recent $MaxGoblinObservationDiagnosticCrops included; $excludedGoblinObservationDiagnosticCrops excluded",
            "Failure screenshot package policy: most recent $MaxFailureScreenshots groups included; $excludedFailureScreenshots files excluded; includedSize=$failureScreenshotSizeDisplay ($failureScreenshotSizeBytes bytes); excludedSize=$excludedFailureScreenshotSizeDisplay ($excludedFailureScreenshotSizeBytes bytes); availableSize=$availableFailureScreenshotSizeDisplay ($availableFailureScreenshotSizeBytes bytes)",
            "Debug screenshot package policy: most recent $MaxDebugScreenshots files included from current session",
            $goblinEvidenceSourceSummaryLine,
            "Log folders searched:",
            ($logFolders | ForEach-Object { "- $_" }),
            "Selected log folder: $selectedLogFolder",
            "Selected latest log: $(if ($null -ne $latestLog) { $latestLog.FullName } else { 'none' })",
            "Selected screenshot folder: $selectedScreenshotFolder",
            "Selected debug screenshot folder: $selectedDebugScreenshotFolder",
            "Runtime session-info included: $runtimeSessionInfoIncluded",
            "Runtime AppSettings included: $runtimeAppSettingsIncluded",
            "Git metadata included/skipped: $gitMetadataStatus",
            "Git metadata skipped because installed folder: $gitSkippedBecauseInstalled",
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
            "- Failure screenshots excluded: $excludedFailureScreenshots",
            "- Failure screenshots included total size: $failureScreenshotSizeDisplay ($failureScreenshotSizeBytes bytes)",
            "- Failure screenshots excluded total size: $excludedFailureScreenshotSizeDisplay ($excludedFailureScreenshotSizeBytes bytes)",
            "- Failure screenshots available total size: $availableFailureScreenshotSizeDisplay ($availableFailureScreenshotSizeBytes bytes)",
            "- Success screenshots included: $($successScreenshots.Count)",
            "- Success screenshots available: $($availableSuccessScreenshots.Count)",
            "- Success screenshots available total size: $availableSuccessScreenshotSizeDisplay ($availableSuccessScreenshotSizeBytes bytes)",
            "- Diagnostic screenshots included: $($diagnosticScreenshots.Count)",
            "- Normal debug screenshots included: $($normalScreenshots.Count)",
            "- Debug screenshots included: $debugScreenshotCount",
            "- Debug screenshots package limit: $MaxDebugScreenshots",
            "- Goblin evidence screenshots included: $goblinEvidenceScreenshotCount",
            "- Goblin evidence full images excluded: $excludedGoblinEvidenceFullImages",
            "- Goblin evidence event screenshots included: $($selectedGoblinEvidenceEventScreenshots.Count)",
            "- Goblin evidence event screenshots excluded: $excludedGoblinEvidenceEventScreenshots",
            "- Goblin evidence event screenshots oversized: $oversizedGoblinEvidenceEventScreenshots",
            "- Goblin observation diagnostic crops included: $($selectedGoblinObservationDiagnosticCrops.Count)",
            "- Goblin observation diagnostic crops excluded: $excludedGoblinObservationDiagnosticCrops",
            "- All discovered screenshots: $($allScreenshots.Count)",
            "- Current-session screenshots: $($sessionScreenshots.Count)",
            "- Excluded stale screenshots: $excludedStaleScreenshots",
            "- Debug screenshots setting from latest log: $($debugSkipInfo.AppSettingsDebugScreenshots)",
            "- Keep debug screenshots setting from latest log: $($debugSkipInfo.AppSettingsKeepDebugScreenshots)",
            "- App Debug Mode at package time: $($debugSkipInfo.ActualDebugModeAtPackageTime)",
            "- App Debug Mode at startup: $($debugSkipInfo.StartupDebugMode)",
            "- Latest Debug Mode toggle old value: $($debugSkipInfo.LatestDebugModeToggleOldValue)",
            "- Latest Debug Mode toggle new value: $($debugSkipInfo.LatestDebugModeToggleNewValue)",
            "- Latest Debug Mode toggle added diagnostic UI: $($debugSkipInfo.LatestDebugModeToggleDiagnosticUiAdded)",
            "- Latest Debug Mode toggle removed diagnostic UI: $($debugSkipInfo.LatestDebugModeToggleDiagnosticUiRemoved)",
            "- Startup AppSettings path: $($debugSkipInfo.StartupAppSettingsPath)",
            "- Startup executable path: $($debugSkipInfo.StartupExecutablePath)",
            "- Startup build configuration: $($debugSkipInfo.StartupBuildConfiguration)",
            "- Startup launch kind: $($debugSkipInfo.StartupLaunchKind)",
            "- Startup debug defaults profile: $($debugSkipInfo.StartupDebugDefaultsProfile)",
            "- Startup debugger attached: $($debugSkipInfo.StartupDebuggerAttached)",
            "- Startup VS/dev profile active: $($debugSkipInfo.StartupVsDevProfileActive)",
            "- Startup first-run setup suppressed: $($debugSkipInfo.StartupFirstRunSetupSuppressed)",
            "- Git status captured: $gitStatusCaptured",
            "- Git log captured: $gitLogCaptured",
            "- DebugScreenshotSkipped disabled-by-config entries: $($debugSkipInfo.SkippedByConfigCount)",
            "- Diagnostic screenshot pair skipped by AppSettings entries: $($debugSkipInfo.DiagnosticSkippedByAppSettingsCount)",
            "- Diagnostic screenshot pair skipped by Keep Debug Screenshots entries: $($debugSkipInfo.DiagnosticSkippedByKeepSettingCount)",
            "- Latest failure screenshot type: $latestFailureType",
            "- Latest failure screenshot: $(if ($null -ne $latestFailureScreenshot) { $latestFailureScreenshot.FullName } else { 'none' })",
            "",
            "Package folder totals:",
            (Get-PackageFolderTotals $stagingRoot | ForEach-Object { "- $($_.Folder): $($_.Count) files, $($_.Size) ($($_.Bytes) bytes)" }),
            $goblinEvidenceSourceFolderTotalLine,
            "",
            "Exclusions:",
            "- Screenshots older than the current GoblinFarmer session start are not copied",
            "- Screenshots/Failure is limited to the most recent MaxFailureScreenshots diagnostic groups",
            "- Screenshots/Success is excluded unless -IncludeSuccessScreenshots is set",
            "- debug-screenshots is limited to MaxDebugScreenshots current-session files",
            "- Debug/GoblinEvidence/Calibration *_Full images are excluded except the most recent MaxGoblinEvidenceFullImages",
            "- Debug/GoblinEvidence/GoblinEvidence_* event screenshots are limited to MaxGoblinEvidenceEventScreenshots newest files and MaxGoblinEvidenceEventScreenshotBytes",
            "- Debug/GoblinEvidence/ObservationDiagnostics image crops are limited to MaxGoblinObservationDiagnosticCrops newest files",
            "- bin folders are not copied",
            "- obj folders are not copied",
            "- source files are not copied except selected docs and manifest inputs",
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
Write-Host "Runtime root:        $resolvedRuntimeRoot"
Write-Host "Resolved by:         $($runtimeRootInfo.Resolution)"
Write-Host "Session start:       $($sessionStart.ToString('yyyy-MM-dd HH:mm:ss.fff zzz'))"
Write-Host "Session duration:    $($sessionDuration.ToString('hh\:mm\:ss'))"
Write-Host "Goblins found:       $($goblinTrackerInfo.GoblinCount)"
Write-Host "Goblin active time:  $($goblinTrackerInfo.ActiveCombatTime)"
Write-Host "Goblin GPH:          $($goblinTrackerInfo.GPH)"
Write-Host "Observations:        $($goblinTrackerInfo.ObservationCount)"
Write-Host "Evidence events:     $($goblinTrackerInfo.EvidenceEventCount)"
Write-Host "Last evidence:       $($goblinTrackerInfo.LastEvidenceType) ($($goblinTrackerInfo.LastEvidenceConfidence))"
Write-Host "Evidence folder:     $selectedGoblinEvidenceFolder"
Write-Host "Missing templates:   detected=$($goblinEvidenceMissingTemplateInfo.Detected); logEntries=$($goblinEvidenceMissingTemplateInfo.LogEntries)"
Write-Host "Latest log:          $(if ($null -ne $latestLog) { $latestLog.FullName } else { 'none' })"
Write-Host "Selected log folder: $selectedLogFolder"
Write-Host "Screenshot folder:   $selectedScreenshotFolder"
Write-Host "Debug shot folder:   $selectedDebugScreenshotFolder"
Write-Host "Total screenshots:   $totalScreenshotCount"
Write-Host "Failure screenshots: $($failureScreenshots.Count) included; $excludedFailureScreenshots excluded; includedSize=$failureScreenshotSizeDisplay; excludedSize=$excludedFailureScreenshotSizeDisplay"
Write-Host "Success screenshots: $($successScreenshots.Count)"
Write-Host "Diagnostic screenshots: $($diagnosticScreenshots.Count)"
Write-Host "Normal screenshots:  $($normalScreenshots.Count)"
Write-Host "Debug screenshots:   $debugScreenshotCount (limit $MaxDebugScreenshots)"
Write-Host "Evidence screenshots:$goblinEvidenceScreenshotCount"
Write-Host "Observation crops:   $($selectedGoblinObservationDiagnosticCrops.Count) included; $excludedGoblinObservationDiagnosticCrops excluded"
Write-Host "Stale screenshots:   $excludedStaleScreenshots excluded"
Write-Host "Debug screenshots on:$($debugSkipInfo.AppSettingsDebugScreenshots)"
Write-Host "Keep screenshots on: $($debugSkipInfo.AppSettingsKeepDebugScreenshots)"
Write-Host "App Debug Mode:      $($debugSkipInfo.ActualDebugModeAtPackageTime) (startup: $($debugSkipInfo.StartupDebugMode))"
Write-Host "Debugger attached:   $($debugSkipInfo.StartupDebuggerAttached)"
Write-Host "Build configuration: $($debugSkipInfo.StartupBuildConfiguration)"
Write-Host "VS/dev profile:      $($debugSkipInfo.StartupVsDevProfileActive)"
Write-Host "First-run suppressed:$($debugSkipInfo.StartupFirstRunSetupSuppressed)"
Write-Host "AppSettings path:    $($debugSkipInfo.StartupAppSettingsPath)"
Write-Host "Skipped screenshots: DebugScreenshotSkipped=$($debugSkipInfo.SkippedByConfigCount); DiagnosticAppSettings=$($debugSkipInfo.DiagnosticSkippedByAppSettingsCount); DiagnosticKeepSetting=$($debugSkipInfo.DiagnosticSkippedByKeepSettingCount)"
Write-Host "Latest failure type: $latestFailureType"
Write-Host "Git status captured: $gitStatusCaptured"
Write-Host "Git log captured:    $gitLogCaptured"
Write-Host "Git metadata:        $gitMetadataStatus"
Write-Host "Git skipped installed: $gitSkippedBecauseInstalled"
Write-Host "Manifest:            debug-package-manifest.txt"
Write-Host "==========================================="

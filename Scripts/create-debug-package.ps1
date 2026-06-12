param(
    [string]$RuntimeRoot = "",
    [int]$MaxScreenshots = 10,
    [int]$MaxFailureScreenshots = 3,
    [int]$MaxSuccessScreenshots = 0,
    [int]$MaxDiagnosticScreenshots = 10,
    [int]$MaxDebugScreenshots = 4,
    [long]$MaxPackagedScreenshotBytes = 3145728,
    [switch]$IncludeSuccessScreenshots,
    [int]$MaxGoblinEvidenceFullImages = 0,
    [int]$MaxGoblinEvidenceEventScreenshots = 3,
    [long]$MaxGoblinEvidenceEventScreenshotBytes = 1048576,
    [int]$MaxGoblinObservationDiagnosticCrops = 12,
    [int]$MaxImageRecognitionBestSampleSets = 3,
    [int]$MaxReviewEvidenceFrames = 12,
    [int]$DebugPackageRetentionCount = 20,
    [switch]$IncludeGoblinDecisionBundleFullImages,
    [switch]$IncludeGoblinCaptureFullscreenImages,
    [string]$ReviewVideoPath = "",
    [string[]]$ReviewTimestamp = @(),
    [string]$ReviewNotesPath = "",
    [string]$ReviewEvidenceFolder = ""
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Supported debug ZIP export path for both VS Debug and Release.
# Normal app diagnostics stay in DebugManager/runtime folders; this script is the
# single intentional review package workflow and app shutdown does not create
# packages or loose review exports.

$debugAnalysisToolsPath = Join-Path $PSScriptRoot "debug-analysis-tools.ps1"
$debugAnalysisToolsAvailable = $false
if (Test-Path -LiteralPath $debugAnalysisToolsPath -PathType Leaf) {
    . $debugAnalysisToolsPath
    $debugAnalysisToolsAvailable = $true
}
else {
    Write-Warning "Optional debug analysis helper missing: $debugAnalysisToolsPath"
}

function Write-Step {
    param([string]$Text)

    Write-Host ""
    Write-Host "==> $Text"
}

function Invoke-DebugPackageRetentionCleanup {
    param(
        [string]$PackageDirectory,
        [int]$RetentionCount
    )

    if ($RetentionCount -le 0) {
        Write-Host "Debug package retention cleanup disabled: retentionCount=$RetentionCount; folder=$PackageDirectory"
        return
    }

    if (-not (Test-Path -LiteralPath $PackageDirectory -PathType Container)) {
        Write-Host "Debug package retention cleanup skipped: folder missing; folder=$PackageDirectory"
        return
    }

    $root = [System.IO.Path]::GetFullPath($PackageDirectory)
    $packages = @(Get-ChildItem -LiteralPath $root -Filter "GoblinFarmer_Debug_*.zip" -File -ErrorAction SilentlyContinue |
        Sort-Object @{ Expression = { $_.LastWriteTimeUtc }; Descending = $true }, @{ Expression = { $_.Name }; Descending = $true })

    $deleted = 0
    $skipped = 0
    foreach ($package in ($packages | Select-Object -Skip $RetentionCount)) {
        try {
            $fullPath = [System.IO.Path]::GetFullPath($package.FullName)
            $insideRoot = $fullPath.StartsWith($root.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
            if (-not $insideRoot) {
                $skipped++
                Write-Warning "Debug package retention cleanup skipped outside folder: path=$fullPath; folder=$root"
                continue
            }

            Remove-Item -LiteralPath $fullPath -Force
            $deleted++
            Write-Host "Debug package retention cleanup deleted: $fullPath"
        }
        catch {
            $skipped++
            Write-Warning "Debug package retention cleanup delete failed: path=$($package.FullName); error=$($_.Exception.Message)"
        }
    }

    $kept = [Math]::Min($packages.Count, [Math]::Max($RetentionCount, 0))
    Write-Host "Debug package retention cleanup complete: scanned=$($packages.Count); deleted=$deleted; skipped=$skipped; kept=$kept; retentionCount=$RetentionCount; folder=$root"
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

function Test-PackagedScreenshotSizeEligible {
    param(
        [System.IO.FileInfo]$File,
        [long]$MaxBytes
    )

    return $MaxBytes -le 0 -or $File.Length -le $MaxBytes
}

function ConvertTo-SafeFileNamePart {
    param(
        [string]$Value,
        [string]$Fallback
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $Fallback
    }

    $safe = -join ($Value.ToCharArray() | ForEach-Object {
        if ([char]::IsLetterOrDigit($_)) { $_ } else { "_" }
    })
    $safe = $safe.Trim("_")
    if ([string]::IsNullOrWhiteSpace($safe)) {
        return $Fallback
    }

    return $safe
}

function Get-ReviewTimestampEntries {
    param([string[]]$TimestampValues)

    $entries = New-Object System.Collections.Generic.List[object]
    $index = 0
    foreach ($value in @($TimestampValues)) {
        if ([string]::IsNullOrWhiteSpace($value)) {
            continue
        }

        $index++
        $timestamp = $value
        $label = "frame_$index"
        $separatorIndex = $value.IndexOf("=")
        if ($separatorIndex -ge 0) {
            $timestamp = $value.Substring(0, $separatorIndex).Trim()
            $labelValue = $value.Substring($separatorIndex + 1).Trim()
            if (-not [string]::IsNullOrWhiteSpace($labelValue)) {
                $label = $labelValue
            }
        }

        if ([string]::IsNullOrWhiteSpace($timestamp)) {
            continue
        }

        [void]$entries.Add([pscustomobject]@{
            Index = $index
            Timestamp = $timestamp
            Label = $label
            FileName = "$(("{0:D2}" -f $index))_$(ConvertTo-SafeFileNamePart $timestamp "timestamp")_$(ConvertTo-SafeFileNamePart $label "review").png"
        })
    }

    return $entries.ToArray()
}

function Find-FfmpegCommand {
    $command = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    $command = Get-Command ffmpeg.cmd -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    return ""
}

function Find-FfprobeCommand {
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

function Get-VideoDurationSeconds {
    param([string]$VideoPath)

    $ffprobe = Find-FfprobeCommand
    if ([string]::IsNullOrWhiteSpace($ffprobe) -or -not (Test-Path -LiteralPath $VideoPath -PathType Leaf)) {
        return 0.0
    }

    try {
        $output = & $ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 $VideoPath 2>$null
        $text = (@($output) | Select-Object -First 1).ToString()
        $duration = 0.0
        if ([double]::TryParse($text, [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$duration)) {
            return [Math]::Max(0.0, $duration)
        }
    }
    catch {
        return 0.0
    }

    return 0.0
}

function Try-ParseVideoStartFromName {
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
        $StartTime.Value = [DateTime]::ParseExact($text, "yyyy-MM-dd HH:mm:ss.fff", [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AssumeLocal)
        return $true
    }
    catch {
        return $false
    }
}

function Get-ReviewVideoStartInfo {
    param([System.IO.FileInfo]$VideoFile)

    $durationSeconds = Get-VideoDurationSeconds $VideoFile.FullName
    if ($durationSeconds -gt 0) {
        return [pscustomobject]@{
            Start = $VideoFile.LastWriteTime.AddSeconds(-1 * $durationSeconds)
            Source = "LastWriteMinusFfprobeDuration"
            DurationSeconds = $durationSeconds
        }
    }

    $parsedStart = [DateTime]::MinValue
    if (Try-ParseVideoStartFromName $VideoFile.Name ([ref]$parsedStart)) {
        return [pscustomobject]@{
            Start = $parsedStart
            Source = "FilenameTimestamp"
            DurationSeconds = 0.0
        }
    }

    return [pscustomobject]@{
        Start = $VideoFile.CreationTime
        Source = "CreationTimeFallback"
        DurationSeconds = 0.0
    }
}

function Find-DefaultReviewVideo {
    param(
        [string[]]$RuntimeRoots,
        [string]$SourceRoot
    )

    $folders = New-Object System.Collections.Generic.List[string]
    Add-UniquePath $folders (Join-Path $SourceRoot "Video Clip Review")
    foreach ($root in @($RuntimeRoots)) {
        Add-UniquePath $folders (Join-Path $root "Video Clip Review")
    }

    $videos = foreach ($folder in $folders.ToArray()) {
        if (Test-Path -LiteralPath $folder -PathType Container) {
            Get-ChildItem -LiteralPath $folder -File -ErrorAction SilentlyContinue |
                Where-Object { $_.Extension -match '^\.(mkv|mp4|mov|avi|webm)$' }
        }
    }

    return @($videos | Sort-Object LastWriteTime -Descending | Select-Object -First 1)
}

function Try-ParseLogLineTimestamp {
    param(
        [string]$Line,
        [ref]$Timestamp
    )

    $match = [regex]::Match($Line, '^\[(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})\]')
    if (-not $match.Success) {
        return $false
    }

    try {
        $Timestamp.Value = [DateTime]::ParseExact($match.Groups["timestamp"].Value, "yyyy-MM-dd HH:mm:ss.fff", [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AssumeLocal)
        return $true
    }
    catch {
        return $false
    }
}

function Format-ReviewOffset {
    param([double]$Seconds)

    $safeSeconds = [Math]::Max(0.0, $Seconds)
    $timeSpan = [TimeSpan]::FromSeconds($safeSeconds)
    $totalHours = [Math]::Floor($timeSpan.TotalHours)
    return "{0:00}:{1:00}:{2:00}.{3:000}" -f $totalHours, $timeSpan.Minutes, $timeSpan.Seconds, $timeSpan.Milliseconds
}

function Get-AutoReviewLogEvents {
    param(
        [System.IO.FileInfo]$LatestLog,
        [DateTime]$VideoStart,
        [double]$VideoDurationSeconds,
        [int]$Limit
    )

    if ($null -eq $LatestLog -or -not (Test-Path -LiteralPath $LatestLog.FullName -PathType Leaf) -or $Limit -le 0) {
        return @()
    }

    $patterns = @(
        [pscustomobject]@{ Pattern = 'SalvageActionableLeftoversRemain'; Label = 'salvage-actionable-leftovers-remain'; Priority = 1 },
        [pscustomobject]@{ Pattern = 'AutoStashGemLeftoversRemain'; Label = 'gem-stash-leftovers-remain'; Priority = 1 },
        [pscustomobject]@{ Pattern = 'SalvageExpectedConfirmationMissing'; Label = 'salvage-confirmation-missing'; Priority = 1 },
        [pscustomobject]@{ Pattern = 'UnexpectedException|WorkflowFailed|Workflow failed|Exception'; Label = 'workflow-exception'; Priority = 1 },
        [pscustomobject]@{ Pattern = 'PostSalvageLeftoverWarning'; Label = 'post-salvage-leftover-warning'; Priority = 2 },
        [pscustomobject]@{ Pattern = 'Auto gem stash summary'; Label = 'auto-gem-stash-summary'; Priority = 2 },
        [pscustomobject]@{ Pattern = 'Auto gem stash failed|RecordStashFailure'; Label = 'auto-gem-stash-failure'; Priority = 2 },
        [pscustomobject]@{ Pattern = 'Salvage inventory summary|Salvage completed|salvageSuccess=False'; Label = 'salvage-summary'; Priority = 3 },
        [pscustomobject]@{ Pattern = 'Bulk salvage category'; Label = 'bulk-salvage-category'; Priority = 4 },
        [pscustomobject]@{ Pattern = 'Gem stash candidate: .*accepted=True'; Label = 'gem-stash-accepted-candidate'; Priority = 4 }
    )

    $events = New-Object System.Collections.Generic.List[object]
    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $LatestLog.FullName) {
        $lineNumber++
        $timestamp = [DateTime]::MinValue
        if (-not (Try-ParseLogLineTimestamp $line ([ref]$timestamp))) {
            continue
        }

        foreach ($patternInfo in $patterns) {
            if ($line -notmatch $patternInfo.Pattern) {
                continue
            }

            $offsetSeconds = ($timestamp - $VideoStart).TotalSeconds
            $withinVideo = $offsetSeconds -ge -2.0 -and ($VideoDurationSeconds -le 0 -or $offsetSeconds -le ($VideoDurationSeconds + 2.0))
            [void]$events.Add([pscustomobject]@{
                Timestamp = $timestamp
                OffsetSeconds = $offsetSeconds
                OffsetText = Format-ReviewOffset $offsetSeconds
                Label = $patternInfo.Label
                Priority = $patternInfo.Priority
                LineNumber = $lineNumber
                Line = $line
                WithinVideo = $withinVideo
            })
            break
        }
    }

    $deduped = New-Object System.Collections.Generic.List[object]
    foreach ($event in @($events | Where-Object { $_.WithinVideo } | Sort-Object Priority, Timestamp)) {
        $tooClose = $false
        foreach ($selected in $deduped) {
            if ($selected.Label -eq $event.Label -and [Math]::Abs(($selected.Timestamp - $event.Timestamp).TotalSeconds) -lt 2.0) {
                $tooClose = $true
                break
            }
        }

        if ($tooClose) {
            continue
        }

        [void]$deduped.Add($event)
        if ($deduped.Count -ge $Limit) {
            break
        }
    }

    return $deduped.ToArray()
}

function Get-AutoReviewEvidenceSelection {
    param(
        [string[]]$RuntimeRoots,
        [string]$SourceRoot,
        [System.IO.FileInfo]$LatestLog,
        [int]$MaxReviewEvidenceFrames
    )

    $video = @(Find-DefaultReviewVideo $RuntimeRoots $SourceRoot | Select-Object -First 1)
    if ($video.Count -eq 0) {
        return [pscustomobject]@{
            VideoPath = ""
            TimestampArgs = @()
            Status = "Auto review skipped: no OBS video found in Video Clip Review"
            SourceLog = if ($null -ne $LatestLog) { $LatestLog.FullName } else { "none" }
            VideoStartLocal = ""
            VideoStartSource = ""
            EventCount = 0
            SelectedEvents = @()
        }
    }

    $startInfo = Get-ReviewVideoStartInfo $video[0]
    $events = @(Get-AutoReviewLogEvents $LatestLog $startInfo.Start $startInfo.DurationSeconds $MaxReviewEvidenceFrames)
    $timestampArgs = @($events | ForEach-Object { "$($_.OffsetText)=$($_.Label)-line$($_.LineNumber)" })
    $status = if ($events.Count -gt 0) {
        "Auto review selected $($events.Count) log-aligned video frame(s)"
    }
    else {
        "Auto review found OBS video but no high-value log events aligned to the estimated video window"
    }

    return [pscustomobject]@{
        VideoPath = $video[0].FullName
        TimestampArgs = $timestampArgs
        Status = $status
        SourceLog = if ($null -ne $LatestLog) { $LatestLog.FullName } else { "none" }
        VideoStartLocal = $startInfo.Start.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")
        VideoStartSource = $startInfo.Source
        EventCount = $events.Count
        SelectedEvents = $events
    }
}

function Add-ReviewEvidenceToPackage {
    param(
        [string]$StagingRoot,
        [string]$ReviewVideoPath,
        [string[]]$ReviewTimestamp,
        [string]$ReviewNotesPath,
        [string]$ReviewEvidenceFolder,
        [int]$MaxReviewEvidenceFrames,
        [string]$AutoReviewStatus = "",
        [string]$AutoReviewSourceLog = "",
        [string]$AutoReviewVideoStartLocal = "",
        [string]$AutoReviewVideoStartSource = "",
        [object[]]$AutoReviewEvents = @()
    )

    $timestampEntries = @(Get-ReviewTimestampEntries $ReviewTimestamp | Select-Object -First $MaxReviewEvidenceFrames)
    $manualFolderProvided = -not [string]::IsNullOrWhiteSpace($ReviewEvidenceFolder)
    $notesProvided = -not [string]::IsNullOrWhiteSpace($ReviewNotesPath)
    $videoProvided = -not [string]::IsNullOrWhiteSpace($ReviewVideoPath)
    $autoReviewProvided = -not [string]::IsNullOrWhiteSpace($AutoReviewStatus)
    if (-not $manualFolderProvided -and -not $notesProvided -and -not $videoProvided -and $timestampEntries.Count -eq 0 -and -not $autoReviewProvided) {
        return [pscustomobject]@{
            Included = $false
            Status = "Not requested"
            FrameCount = 0
            ManualFileCount = 0
            TimestampCount = 0
            ManifestPath = "none"
            SourceVideo = "none"
            Ffmpeg = "not requested"
        }
    }

    $reviewRoot = Join-Path $StagingRoot "ReviewEvidence"
    $framesRoot = Join-Path $reviewRoot "frames"
    $cropsRoot = Join-Path $reviewRoot "crops"
    New-Item -ItemType Directory -Path $reviewRoot -Force | Out-Null

    $resolvedVideoPath = Resolve-FullPath $ReviewVideoPath
    $videoExists = $videoProvided -and (Test-Path -LiteralPath $resolvedVideoPath -PathType Leaf)
    $ffmpeg = Find-FfmpegCommand
    $extractedFrames = New-Object System.Collections.Generic.List[object]
    $statusParts = New-Object System.Collections.Generic.List[string]

    if ($timestampEntries.Count -gt 0) {
        if (-not $videoExists) {
            [void]$statusParts.Add("Frame extraction skipped: review video missing")
        }
        elseif ([string]::IsNullOrWhiteSpace($ffmpeg)) {
            [void]$statusParts.Add("Frame extraction skipped: ffmpeg unavailable")
        }
        else {
            New-Item -ItemType Directory -Path $framesRoot -Force | Out-Null
            foreach ($entry in $timestampEntries) {
                $outputPath = Join-Path $framesRoot $entry.FileName
                $exitCode = 0
                $errorText = ""
                try {
                    & $ffmpeg -y -ss $entry.Timestamp -i $resolvedVideoPath -frames:v 1 $outputPath 2>$null | Out-Null
                    $exitCode = $LASTEXITCODE
                    if ($exitCode -ne 0 -or -not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
                        $errorText = "ffmpeg exitCode=$exitCode"
                    }
                }
                catch {
                    $exitCode = -1
                    $errorText = $_.Exception.Message
                }

                $relative = "ReviewEvidence/frames/$($entry.FileName)"
                [void]$extractedFrames.Add([pscustomobject]@{
                    Timestamp = $entry.Timestamp
                    Label = $entry.Label
                    File = if ([string]::IsNullOrWhiteSpace($errorText)) { $relative } else { "" }
                    Status = if ([string]::IsNullOrWhiteSpace($errorText)) { "Extracted" } else { "Skipped" }
                    Error = $errorText
                })
            }
        }
    }

    $manualFiles = New-Object System.Collections.Generic.List[object]
    if ($manualFolderProvided) {
        $resolvedManualFolder = Resolve-FullPath $ReviewEvidenceFolder
        if (Test-Path -LiteralPath $resolvedManualFolder -PathType Container) {
            New-Item -ItemType Directory -Path $cropsRoot -Force | Out-Null
            foreach ($file in Get-ChildItem -LiteralPath $resolvedManualFolder -File -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.Extension -match '^\.(png|jpg|jpeg|bmp|txt|md|json)$' } |
                Sort-Object FullName |
                Select-Object -First 100) {
                $relativeSource = $file.FullName.Substring($resolvedManualFolder.Length).TrimStart('\', '/')
                $destination = Join-Path $cropsRoot $relativeSource
                New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
                Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
                [void]$manualFiles.Add([pscustomobject]@{
                    Source = $file.FullName
                    File = (Get-PackageRelativePath $StagingRoot $destination)
                })
            }
        }
        else {
            [void]$statusParts.Add("Manual evidence folder missing: $resolvedManualFolder")
        }
    }

    $resolvedNotesPath = Resolve-FullPath $ReviewNotesPath
    $issuePath = Join-Path $reviewRoot "issue.md"
    if ($notesProvided -and (Test-Path -LiteralPath $resolvedNotesPath -PathType Leaf)) {
        Copy-Item -LiteralPath $resolvedNotesPath -Destination $issuePath -Force
    }
    elseif ($manualFolderProvided -and (Test-Path -LiteralPath (Join-Path (Resolve-FullPath $ReviewEvidenceFolder) "issue.md") -PathType Leaf)) {
        Copy-Item -LiteralPath (Join-Path (Resolve-FullPath $ReviewEvidenceFolder) "issue.md") -Destination $issuePath -Force
    }
    else {
        $autoEventLines = @($AutoReviewEvents | ForEach-Object {
            "- `$($_.OffsetText)` `$($_.Label)` line `$($_.LineNumber)`: $($_.Line)"
        })
        if ($autoEventLines.Count -eq 0) {
            $autoEventLines = @("- none")
        }

        @(
            "# Review Evidence",
            "",
            "Issue: auto-generated debug package review evidence",
            "",
            "Observed behavior: see selected log-aligned OBS frames and logs.",
            "",
            "Source video: $(if ($videoProvided) { $resolvedVideoPath } else { 'none' })",
            "Source log: $(if ([string]::IsNullOrWhiteSpace($AutoReviewSourceLog)) { 'none' } else { $AutoReviewSourceLog })",
            "Auto review status: $(if ([string]::IsNullOrWhiteSpace($AutoReviewStatus)) { 'not requested' } else { $AutoReviewStatus })",
            "Estimated video start: $(if ([string]::IsNullOrWhiteSpace($AutoReviewVideoStartLocal)) { 'unknown' } else { $AutoReviewVideoStartLocal })",
            "Estimated video start source: $(if ([string]::IsNullOrWhiteSpace($AutoReviewVideoStartSource)) { 'unknown' } else { $AutoReviewVideoStartSource })",
            "",
            "Selected log-aligned timestamps:",
            $autoEventLines,
            "",
            "Planned fix: not supplied",
            "",
            "Implementation notes: not supplied"
        ) | ForEach-Object { $_ } | Out-File -FilePath $issuePath -Encoding utf8
    }

    $status = if ($statusParts.Count -gt 0) { $statusParts -join "; " } else { "Included" }
    $autoEventsForManifest = @($AutoReviewEvents | ForEach-Object {
        [pscustomobject]@{
            TimestampLocal = $_.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")
            Offset = $_.OffsetText
            OffsetSeconds = [Math]::Round([double]$_.OffsetSeconds, 3)
            Label = $_.Label
            Priority = $_.Priority
            LineNumber = $_.LineNumber
            Line = $_.Line
        }
    })
    $manifest = [ordered]@{
        SchemaVersion = 1
        CreatedLocal = (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz")
        SourceVideoPath = if ($videoProvided) { $resolvedVideoPath } else { "" }
        SourceVideoName = if ($videoExists) { (Split-Path -Leaf $resolvedVideoPath) } else { "" }
        VideoIncluded = $false
        Ffmpeg = if ([string]::IsNullOrWhiteSpace($ffmpeg)) { "" } else { $ffmpeg }
        TimestampCount = $timestampEntries.Count
        MaxReviewEvidenceFrames = $MaxReviewEvidenceFrames
        Frames = $extractedFrames
        ManualEvidenceFolder = if ($manualFolderProvided) { Resolve-FullPath $ReviewEvidenceFolder } else { "" }
        ManualFiles = $manualFiles
        NotesPath = if ($notesProvided) { $resolvedNotesPath } else { "" }
        IssueFile = "ReviewEvidence/issue.md"
        Status = $status
        AutoReviewStatus = $AutoReviewStatus
        AutoReviewSourceLog = $AutoReviewSourceLog
        AutoReviewVideoStartLocal = $AutoReviewVideoStartLocal
        AutoReviewVideoStartSource = $AutoReviewVideoStartSource
        AutoReviewEvents = $autoEventsForManifest
    }
    $manifestPath = Join-Path $reviewRoot "manifest.json"
    $manifest | ConvertTo-Json -Depth 6 | Out-File -FilePath $manifestPath -Encoding utf8

    return [pscustomobject]@{
        Included = $true
        Status = $status
        FrameCount = @($extractedFrames | Where-Object { $_.Status -eq "Extracted" }).Count
        ManualFileCount = $manualFiles.Count
        TimestampCount = $timestampEntries.Count
        ManifestPath = "ReviewEvidence/manifest.json"
        SourceVideo = if ($videoProvided) { $resolvedVideoPath } else { "none" }
        Ffmpeg = if ([string]::IsNullOrWhiteSpace($ffmpeg)) { "unavailable" } else { $ffmpeg }
        AutoReviewStatus = $AutoReviewStatus
        AutoReviewEventCount = @($AutoReviewEvents).Count
    }
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

function Get-PackageRelativePath {
    param(
        [string]$Root,
        [string]$Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $pathFull = [System.IO.Path]::GetFullPath($Path)
    if ($pathFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $pathFull.Substring($rootFull.Length).Replace('\', '/')
    }

    return $pathFull.Replace('\', '/')
}

function Add-ReviewIndexLink {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$Href,
        [string]$Text
    )

    if ([string]::IsNullOrWhiteSpace($Href)) {
        return
    }

    $safeHref = [System.Net.WebUtility]::HtmlEncode($Href.Replace('\', '/'))
    $safeText = [System.Net.WebUtility]::HtmlEncode($Text)
    $Lines.Add("<li><a href=""$safeHref"">$safeText</a></li>")
}

function New-GoblinTrackerPackageSummary {
    param(
        [string]$StagingRoot,
        [string]$OutputPath
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("Goblin Tracker Package Summary")
    $lines.Add("Created: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
    $lines.Add("")

    $liveDecisionBundles = @(Get-ChildItem -LiteralPath (Join-Path $StagingRoot "Debug\GoblinEvidence\DecisionBundles") -Filter "decision-trace.txt" -File -Recurse -ErrorAction SilentlyContinue)
    $encounterCaptures = @(Get-ChildItem -LiteralPath (Join-Path $StagingRoot "Debug\GoblinEvidence\EncounterCaptures") -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -match '^\.(png|jpg|jpeg|bmp|txt)$' })
    $observationDiagnostics = @(Get-ChildItem -LiteralPath (Join-Path $StagingRoot "Debug\GoblinEvidence\ObservationDiagnostics") -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -match '^\.(png|jpg|jpeg|bmp|txt)$' })
    $lines.Add("Live evidence artifacts:")
    $lines.Add("  Decision bundles: $($liveDecisionBundles.Count)")
    $lines.Add("  Encounter captures: $($encounterCaptures.Count)")
    $lines.Add("  Observation diagnostics: $($observationDiagnostics.Count)")
    $lines.Add("")

    $latestLog = Get-ChildItem -LiteralPath (Join-Path $StagingRoot "Logs") -Filter "*.log" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($null -ne $latestLog) {
        $traceLines = @(Select-String -LiteralPath $latestLog.FullName -Pattern "GoblinAutoCountAccepted|GoblinAutoCountSuppressed|GoblinCountAccepted|GoblinCountSuppressed|LastObservationUpdated|LastObservationCleared" -ErrorAction SilentlyContinue)
        $lines.Add("Latest live log Goblin Tracker markers: $($traceLines.Count)")
        foreach ($match in $traceLines | Select-Object -Last 80) {
            $lines.Add("  $($match.Line)")
        }
    }

    $lines | Out-File -FilePath $OutputPath -Encoding utf8
}

function New-DebugPackageSizeSummaryFromZip {
    param(
        [string]$ZipPath,
        [string]$OutputPath
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("GoblinFarmer Debug Package Size Summary")
    $lines.Add("Created: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')")
    $lines.Add("Source ZIP: $ZipPath")
    $lines.Add("")

    if (-not (Test-Path -LiteralPath $ZipPath -PathType Leaf)) {
        $lines.Add("Status: ZIP not created yet")
        $lines | Out-File -FilePath $OutputPath -Encoding utf8
        return
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $entries = @($archive.Entries | Where-Object { -not [string]::IsNullOrWhiteSpace($_.Name) })
        $totalCompressed = [long]0
        $totalUncompressed = [long]0
        foreach ($entry in $entries) {
            $totalCompressed += [long]$entry.CompressedLength
            $totalUncompressed += [long]$entry.Length
        }

        $lines.Add("Total files: $($entries.Count)")
        $lines.Add("Total compressed bytes: $totalCompressed")
        $lines.Add("Total compressed size: $(Format-ByteSize $totalCompressed)")
        $lines.Add("Total uncompressed bytes: $totalUncompressed")
        $lines.Add("Total uncompressed size: $(Format-ByteSize $totalUncompressed)")
        $lines.Add("Retention applied before packaging:")
        $lines.Add("  Debug packages: $DebugPackageRetentionCount newest GoblinFarmer_Debug_*.zip after package creation")
        $lines.Add("  Failure screenshots: $MaxFailureScreenshots newest diagnostic groups")
        $lines.Add("  Debug screenshots: $MaxDebugScreenshots current-session files")
        $lines.Add("  Goblin evidence event screenshots: $MaxGoblinEvidenceEventScreenshots newest under $MaxGoblinEvidenceEventScreenshotBytes bytes")
        $lines.Add("  Observation diagnostics: $MaxGoblinObservationDiagnosticCrops newest crops")
        $lines.Add("  Image recognition best-sample sets: $MaxImageRecognitionBestSampleSets newest accepted action folders")
        $lines.Add("  Image recognition promoted samples: $MaxImageRecognitionBestSampleSets newest sidecar-backed promoted samples")
        $lines.Add("  Replay image folders: excluded by default")
        $lines.Add("")

        $lines.Add("Size by extension:")
        $entries |
            Group-Object { [System.IO.Path]::GetExtension($_.FullName).ToLowerInvariant() } |
            Sort-Object @{ Expression = { ($_.Group | Measure-Object CompressedLength -Sum).Sum }; Descending = $true } |
            ForEach-Object {
                $compressed = [long](($_.Group | Measure-Object CompressedLength -Sum).Sum)
                $uncompressed = [long](($_.Group | Measure-Object Length -Sum).Sum)
                $extension = if ([string]::IsNullOrWhiteSpace($_.Name)) { "(none)" } else { $_.Name }
                $lines.Add("  $extension files=$($_.Count) compressed=$(Format-ByteSize $compressed) ($compressed bytes) uncompressed=$(Format-ByteSize $uncompressed) ($uncompressed bytes)")
            }
        $lines.Add("")

        $lines.Add("Size by top folder:")
        $entries |
            Group-Object { ($_.FullName -split '/')[0] } |
            Sort-Object @{ Expression = { ($_.Group | Measure-Object CompressedLength -Sum).Sum }; Descending = $true } |
            ForEach-Object {
                $compressed = [long](($_.Group | Measure-Object CompressedLength -Sum).Sum)
                $uncompressed = [long](($_.Group | Measure-Object Length -Sum).Sum)
                $lines.Add("  $($_.Name) files=$($_.Count) compressed=$(Format-ByteSize $compressed) ($compressed bytes) uncompressed=$(Format-ByteSize $uncompressed) ($uncompressed bytes)")
            }
        $lines.Add("")

        $lines.Add("Largest 20 files:")
        $rank = 1
        foreach ($entry in ($entries | Sort-Object Length -Descending | Select-Object -First 20)) {
            $lines.Add("  $rank. $($entry.FullName) compressed=$(Format-ByteSize ([long]$entry.CompressedLength)) ($($entry.CompressedLength) bytes) uncompressed=$(Format-ByteSize ([long]$entry.Length)) ($($entry.Length) bytes)")
            $rank++
        }
    }
    finally {
        $archive.Dispose()
    }

    $lines | Out-File -FilePath $OutputPath -Encoding utf8
}

function New-GoblinTrackerReviewIndex {
    param(
        [string]$StagingRoot,
        [string]$OutputPath
    )

    $links = New-Object System.Collections.Generic.List[string]
    foreach ($relative in @(
        "debug-package-analysis.txt",
        "goblin-tracker-timeline.md",
        "goblin-evidence-health.txt",
        "goblin-tracker-summary.txt",
        "debug-package-manifest.txt",
        "ReviewEvidence\manifest.json",
        "ReviewEvidence\issue.md",
        "route-failure-summary.txt",
        "debug-screenshot-manifest.txt",
        "debug-package-size-summary.txt",
        "session-info.txt"
    )) {
        $path = Join-Path $StagingRoot $relative
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            Add-ReviewIndexLink $links ($relative.Replace('\', '/')) $relative
        }
    }

    foreach ($file in Get-ChildItem -LiteralPath $StagingRoot -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.FullName.Replace('/', '\').IndexOf('\Logs\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and ($_.Extension -eq ".log" -or $_.Extension -eq ".txt") -or
            $_.FullName.Replace('/', '\').IndexOf('\Screenshots\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' -or
            $_.FullName.Replace('/', '\').IndexOf('\ReviewEvidence\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $_.Extension -match '^\.(png|jpg|jpeg|bmp|md|json)$' -or
            $_.FullName.Replace('/', '\').IndexOf('\Debug\GoblinEvidence\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and $_.Extension -match '^\.(png|jpg|jpeg|bmp)$'
        } |
        Sort-Object FullName |
        Select-Object -First 250) {
        Add-ReviewIndexLink $links (Get-PackageRelativePath $StagingRoot $file.FullName) (Get-PackageRelativePath $StagingRoot $file.FullName)
    }

    $html = @(
        "<!doctype html>",
        "<html><head><meta charset=""utf-8""><title>Goblin Tracker Review</title>",
        "<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#202124}li{margin:4px 0}code{background:#f6f8fa;padding:2px 4px}</style>",
        "</head><body>",
        "<h1>Goblin Tracker Review</h1>",
        "<p>Open <code>goblin-tracker-summary.txt</code> first. Reviewed OBS frames, when requested, live under <code>ReviewEvidence/</code>.</p>",
        "<ul>",
        $links,
        "</ul>",
        "</body></html>"
    )
    $html | Out-File -FilePath $OutputPath -Encoding utf8
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
if ($MaxPackagedScreenshotBytes -lt 0) {
    Write-Warning "MaxPackagedScreenshotBytes must be at least 0. Using 0."
    $MaxPackagedScreenshotBytes = 0
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
if ($MaxImageRecognitionBestSampleSets -lt 0) {
    Write-Warning "MaxImageRecognitionBestSampleSets must be at least 0. Using 0."
    $MaxImageRecognitionBestSampleSets = 0
}
if ($MaxReviewEvidenceFrames -lt 0) {
    Write-Warning "MaxReviewEvidenceFrames must be at least 0. Using 0."
    $MaxReviewEvidenceFrames = 0
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
Write-Host "Max packaged screenshot bytes: $MaxPackagedScreenshotBytes"
Write-Host "Include success screenshots: $($IncludeSuccessScreenshots.IsPresent)"
Write-Host "Max goblin evidence full images: $MaxGoblinEvidenceFullImages"
Write-Host "Max goblin evidence event screenshots: $MaxGoblinEvidenceEventScreenshots"
Write-Host "Max goblin evidence event screenshot bytes: $MaxGoblinEvidenceEventScreenshotBytes"
Write-Host "Max goblin observation diagnostic crops: $MaxGoblinObservationDiagnosticCrops"
Write-Host "Max image recognition best-sample sets: $MaxImageRecognitionBestSampleSets"
Write-Host "Max review evidence frames: $MaxReviewEvidenceFrames"
Write-Host "Include goblin decision bundle full images: $($IncludeGoblinDecisionBundleFullImages.IsPresent)"
Write-Host "Include goblin capture fullscreen images: $($IncludeGoblinCaptureFullscreenImages.IsPresent)"
Write-Host "Review video path: $(if ([string]::IsNullOrWhiteSpace($ReviewVideoPath)) { 'none' } else { $ReviewVideoPath })"
Write-Host "Review timestamp count: $(@($ReviewTimestamp).Count)"
Write-Host "Review notes path: $(if ([string]::IsNullOrWhiteSpace($ReviewNotesPath)) { 'none' } else { $ReviewNotesPath })"
Write-Host "Review evidence folder: $(if ([string]::IsNullOrWhiteSpace($ReviewEvidenceFolder)) { 'none' } else { $ReviewEvidenceFolder })"

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
    $replayLogFoldersList = New-Object System.Collections.Generic.List[string]
    $replayLogSourceRootsList = New-Object System.Collections.Generic.List[string]
    $goblinEvidenceFoldersList = New-Object System.Collections.Generic.List[string]
    $goblinEvidenceSourceRootsList = New-Object System.Collections.Generic.List[string]
    foreach ($root in $packageRuntimeRoots) {
        Add-UniquePath $screenshotFoldersList (Join-Path $root "Screenshots")
        Add-UniquePath $debugScreenshotFoldersList (Join-Path $root "debug-screenshots")
        Add-UniquePath $replayLogFoldersList (Join-Path $root "Debug\ReplayLogs")
        Add-UniquePath $replayLogSourceRootsList (Join-Path $root "Debug")
        Add-UniquePath $goblinEvidenceFoldersList (Join-Path $root "Debug\GoblinEvidence")
        Add-UniquePath $goblinEvidenceFoldersList (Join-Path $root "Debug\GemAutoStash")
        Add-UniquePath $goblinEvidenceSourceRootsList (Join-Path $root "Debug")
    }
    $screenshotFolders = $screenshotFoldersList.ToArray()
    $debugScreenshotFolders = $debugScreenshotFoldersList.ToArray()
    $replayLogFolders = $replayLogFoldersList.ToArray()
    $replayLogSourceRoots = $replayLogSourceRootsList.ToArray()
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

    $rawFailureScreenshots = @(Get-FilesFromScreenshotGroups $failureGroups)
    $oversizedFailureScreenshots = @($rawFailureScreenshots | Where-Object { -not (Test-PackagedScreenshotSizeEligible $_ $MaxPackagedScreenshotBytes) })
    $failureScreenshots = @($rawFailureScreenshots | Where-Object { Test-PackagedScreenshotSizeEligible $_ $MaxPackagedScreenshotBytes })
    $failureScreenshotSizeMeasure = $failureScreenshots | Measure-Object Length -Sum
    $failureScreenshotSizeBytes = if ($null -ne $failureScreenshotSizeMeasure.Sum) { [long]$failureScreenshotSizeMeasure.Sum } else { 0L }
    $failureScreenshotSizeDisplay = Format-ByteSize $failureScreenshotSizeBytes
    $excludedFailureScreenshots = [Math]::Max(0, $availableFailureScreenshots.Count - $failureScreenshots.Count)
    $excludedFailureScreenshotSizeBytes = [Math]::Max(0L, $availableFailureScreenshotSizeBytes - $failureScreenshotSizeBytes)
    $excludedFailureScreenshotSizeDisplay = Format-ByteSize $excludedFailureScreenshotSizeBytes
    $successScreenshots = @(Get-FilesFromScreenshotGroups $successGroups | Where-Object { Test-PackagedScreenshotSizeEligible $_ $MaxPackagedScreenshotBytes })
    $diagnosticScreenshots = @(Get-FilesFromScreenshotGroups $diagnosticGroups | Where-Object { Test-PackagedScreenshotSizeEligible $_ $MaxPackagedScreenshotBytes })
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
                -not $selectedPairKeys.ContainsKey($info.PairKey) -and
                (Test-PackagedScreenshotSizeEligible $_ $MaxPackagedScreenshotBytes)
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
                Write-Host "Failure screenshot policy: most recent $MaxFailureScreenshots groups included; $excludedFailureScreenshots files excluded; oversizedSelected=$($oversizedFailureScreenshots.Count); maxBytes=$MaxPackagedScreenshotBytes; includedSize=$failureScreenshotSizeDisplay; excludedSize=$excludedFailureScreenshotSizeDisplay; availableSize=$availableFailureScreenshotSizeDisplay"
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
        Where-Object { Test-PackagedScreenshotSizeEligible $_ $MaxPackagedScreenshotBytes } |
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

    Write-Step "Collecting replay logs"
    $replayLogCandidates = foreach ($folder in $replayLogFolders) {
        if (Test-Path -LiteralPath $folder -PathType Container) {
            Get-ChildItem -LiteralPath $folder -File -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.Extension -match '^\.(jsonl|json|txt)$' }
        }
    }
    $selectedReplayLogFiles = @($replayLogCandidates |
        Where-Object { $_.LastWriteTime -ge $sessionStart -or $_.CreationTime -ge $sessionStart } |
        Sort-Object FullName)
    $replayLogFileCount = Copy-DebugScreenshotsToPackage $selectedReplayLogFiles $replayLogSourceRoots (Join-Path $stagingRoot "Debug")
    Write-Host "Included replay log files: $replayLogFileCount"

    Write-Step "Collecting goblin evidence screenshots"
    $goblinEvidenceCandidates = foreach ($folder in $goblinEvidenceFolders) {
        if (Test-Path -LiteralPath $folder -PathType Container) {
            Get-ChildItem -LiteralPath $folder -File -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.Extension -match '^\.(png|jpg|jpeg|bmp|txt|json|jsonl)$' }
        }
    }
    $goblinEvidenceScreenshots = @($goblinEvidenceCandidates |
        Where-Object { $_.LastWriteTime -ge $sessionStart -or $_.CreationTime -ge $sessionStart } |
        Sort-Object LastWriteTime -Descending)
    $goblinDecisionBundleFullEvidenceImages = @($goblinEvidenceScreenshots |
        Where-Object {
            $normalizedFullName = $_.FullName.Replace('/', '\')
            $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' -and
                $_.BaseName -ieq 'evidence' -and
                $normalizedFullName.IndexOf('\Debug\GoblinEvidence\DecisionBundles\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0
        } |
        Sort-Object LastWriteTime -Descending)
    $excludedGoblinDecisionBundleFullEvidenceImages = if ($IncludeGoblinDecisionBundleFullImages) { 0 } else { $goblinDecisionBundleFullEvidenceImages.Count }
    $excludedGoblinDecisionBundleFullEvidenceImageBytes = if ($IncludeGoblinDecisionBundleFullImages) { 0L } else { [long](($goblinDecisionBundleFullEvidenceImages | Measure-Object Length -Sum).Sum) }
    if (-not $IncludeGoblinDecisionBundleFullImages) {
        $goblinEvidenceScreenshots = @($goblinEvidenceScreenshots |
            Where-Object {
                $normalizedFullName = $_.FullName.Replace('/', '\')
                -not ($_.Extension -match '^\.(png|jpg|jpeg|bmp)$' -and
                    $_.BaseName -ieq 'evidence' -and
                    $normalizedFullName.IndexOf('\Debug\GoblinEvidence\DecisionBundles\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0)
            })
    }
    $goblinCaptureFullscreenImages = @($goblinEvidenceScreenshots |
        Where-Object {
            $normalizedFullName = $_.FullName.Replace('/', '\')
            $_.Extension -match '^\.(png|jpg|jpeg|bmp)$' -and
                $_.BaseName -like '*_Fullscreen' -and
                ($normalizedFullName.IndexOf('\Debug\GoblinEvidence\EncounterCaptures\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                    $normalizedFullName.IndexOf('\Debug\GoblinEvidence\ManualCaptures\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0)
        } |
        Sort-Object LastWriteTime -Descending)
    $excludedGoblinCaptureFullscreenImages = if ($IncludeGoblinCaptureFullscreenImages) { 0 } else { $goblinCaptureFullscreenImages.Count }
    $excludedGoblinCaptureFullscreenImageBytes = if ($IncludeGoblinCaptureFullscreenImages) { 0L } else { [long](($goblinCaptureFullscreenImages | Measure-Object Length -Sum).Sum) }
    if (-not $IncludeGoblinCaptureFullscreenImages) {
        $goblinEvidenceScreenshots = @($goblinEvidenceScreenshots |
            Where-Object {
                $normalizedFullName = $_.FullName.Replace('/', '\')
                -not ($_.Extension -match '^\.(png|jpg|jpeg|bmp)$' -and
                    $_.BaseName -like '*_Fullscreen' -and
                    ($normalizedFullName.IndexOf('\Debug\GoblinEvidence\EncounterCaptures\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                        $normalizedFullName.IndexOf('\Debug\GoblinEvidence\ManualCaptures\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0))
            })
    }
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
    $imageRecognitionBestSampleFiles = @($goblinEvidenceScreenshots |
        Where-Object {
            $normalizedFullName = $_.FullName.Replace('/', '\')
            $normalizedFullName.IndexOf('\Debug\GoblinEvidence\AcceptedEvidenceCandidates\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                $normalizedFullName.IndexOf('\Debug\GemAutoStash\AcceptedGemCandidates\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0
        })
    $imageRecognitionBestSampleMetadata = @($imageRecognitionBestSampleFiles |
        Where-Object { $_.Name -ieq 'metadata.json' } |
        Sort-Object LastWriteTime -Descending)
    $selectedImageRecognitionBestSampleSetFolders = @{}
    foreach ($metadata in @($imageRecognitionBestSampleMetadata | Select-Object -First $MaxImageRecognitionBestSampleSets)) {
        $selectedImageRecognitionBestSampleSetFolders[(Split-Path -Parent $metadata.FullName)] = $true
    }
    $selectedImageRecognitionBestSampleFiles = @($imageRecognitionBestSampleFiles |
        Where-Object {
            $selectedImageRecognitionBestSampleSetFolders.ContainsKey((Split-Path -Parent $_.FullName))
        })
    $selectedImageRecognitionBestSampleFilePaths = @{}
    foreach ($file in $selectedImageRecognitionBestSampleFiles) {
        $selectedImageRecognitionBestSampleFilePaths[$file.FullName] = $true
    }
    $excludedImageRecognitionBestSampleFiles = [Math]::Max(0, $imageRecognitionBestSampleFiles.Count - $selectedImageRecognitionBestSampleFiles.Count)
    $goblinEvidenceScreenshots = @($goblinEvidenceScreenshots |
        Where-Object {
            $normalizedFullName = $_.FullName.Replace('/', '\')
            $isBestSampleFile = $normalizedFullName.IndexOf('\Debug\GoblinEvidence\AcceptedEvidenceCandidates\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                $normalizedFullName.IndexOf('\Debug\GemAutoStash\AcceptedGemCandidates\', [System.StringComparison]::OrdinalIgnoreCase) -ge 0
            (-not $isBestSampleFile) -or $selectedImageRecognitionBestSampleFilePaths.ContainsKey($_.FullName)
        })
    $imageRecognitionPromotedFolders = @(
        (Join-Path $repoRoot "Images\Goblin Evidence\Promoted"),
        (Join-Path $repoRoot "Images\Gems\Promoted")
    )
    $imageRecognitionPromotedFiles = foreach ($folder in $imageRecognitionPromotedFolders) {
        if (Test-Path -LiteralPath $folder -PathType Container) {
            Get-ChildItem -LiteralPath $folder -File -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.Extension -match '^\.(png|json)$' }
        }
    }
    $imageRecognitionPromotedMetadata = @($imageRecognitionPromotedFiles |
        Where-Object { $_.Extension -ieq '.json' } |
        Sort-Object LastWriteTime -Descending)
    $selectedImageRecognitionPromotedSampleKeys = @{}
    foreach ($metadata in @($imageRecognitionPromotedMetadata | Select-Object -First $MaxImageRecognitionBestSampleSets)) {
        $selectedImageRecognitionPromotedSampleKeys[[System.IO.Path]::ChangeExtension($metadata.FullName, $null)] = $true
    }
    $selectedImageRecognitionPromotedFiles = @($imageRecognitionPromotedFiles |
        Where-Object {
            $selectedImageRecognitionPromotedSampleKeys.ContainsKey([System.IO.Path]::ChangeExtension($_.FullName, $null))
        })
    $excludedImageRecognitionPromotedFiles = [Math]::Max(0, $imageRecognitionPromotedFiles.Count - $selectedImageRecognitionPromotedFiles.Count)
    $selectedGoblinEvidenceFolder = if ($goblinEvidenceScreenshots.Count -gt 0) { Split-Path -Parent $goblinEvidenceScreenshots[0].FullName } elseif (-not [string]::IsNullOrWhiteSpace($goblinTrackerInfo.EvidenceScreenshotFolder)) { $goblinTrackerInfo.EvidenceScreenshotFolder } else { "none" }
    Write-Host "Selected goblin evidence folder: $selectedGoblinEvidenceFolder"
    Write-Host "Excluded goblin evidence full images: $excludedGoblinEvidenceFullImages"
    Write-Host "Excluded goblin decision bundle full evidence images: $excludedGoblinDecisionBundleFullEvidenceImages ($(Format-ByteSize $excludedGoblinDecisionBundleFullEvidenceImageBytes))"
    Write-Host "Excluded goblin capture fullscreen images: $excludedGoblinCaptureFullscreenImages ($(Format-ByteSize $excludedGoblinCaptureFullscreenImageBytes))"
    Write-Host "Included goblin evidence event screenshots: $($selectedGoblinEvidenceEventScreenshots.Count)"
    Write-Host "Excluded goblin evidence event screenshots: $excludedGoblinEvidenceEventScreenshots"
    Write-Host "Oversized goblin evidence event screenshots excluded: $oversizedGoblinEvidenceEventScreenshots"
    Write-Host "Included goblin observation diagnostic crops: $($selectedGoblinObservationDiagnosticCrops.Count)"
    Write-Host "Excluded goblin observation diagnostic crops: $excludedGoblinObservationDiagnosticCrops"
    Write-Host "Included image recognition best-sample sets: $($selectedImageRecognitionBestSampleSetFolders.Count)"
    Write-Host "Excluded image recognition best-sample files: $excludedImageRecognitionBestSampleFiles"
    $goblinEvidenceScreenshotCount = Copy-DebugScreenshotsToPackage $goblinEvidenceScreenshots $goblinEvidenceSourceRoots (Join-Path $stagingRoot "Debug")
    $imageRecognitionPromotedFileCount = Copy-DebugScreenshotsToPackage $selectedImageRecognitionPromotedFiles @($repoRoot) $stagingRoot
    Write-Host "Included image recognition promoted sample files: $imageRecognitionPromotedFileCount"
    Write-Host "Excluded image recognition promoted sample files: $excludedImageRecognitionPromotedFiles"
    if ($goblinEvidenceScreenshotCount -eq 0) {
        Write-Host "No current-session goblin evidence screenshots found."
    }
    else {
        Write-Host "Included goblin evidence screenshots: $goblinEvidenceScreenshotCount"
    }
    $goblinEvidenceMissingTemplateInfo = Get-GoblinEvidenceMissingTemplateInfo $latestLog

    Write-Step "Collecting review evidence"
    $autoReviewSelection = Get-AutoReviewEvidenceSelection `
        -RuntimeRoots $packageRuntimeRoots `
        -SourceRoot $repoRoot `
        -LatestLog $latestLog `
        -MaxReviewEvidenceFrames $MaxReviewEvidenceFrames
    $effectiveReviewVideoPath = $ReviewVideoPath
    $effectiveReviewTimestamp = @($ReviewTimestamp)
    if ([string]::IsNullOrWhiteSpace($effectiveReviewVideoPath) -and -not [string]::IsNullOrWhiteSpace($autoReviewSelection.VideoPath)) {
        $effectiveReviewVideoPath = $autoReviewSelection.VideoPath
    }
    if ($effectiveReviewTimestamp.Count -eq 0 -and @($autoReviewSelection.TimestampArgs).Count -gt 0) {
        $effectiveReviewTimestamp = @($autoReviewSelection.TimestampArgs)
    }
    Write-Host "Auto review evidence: status=$($autoReviewSelection.Status); video=$(if ([string]::IsNullOrWhiteSpace($autoReviewSelection.VideoPath)) { 'none' } else { $autoReviewSelection.VideoPath }); events=$($autoReviewSelection.EventCount); videoStart=$($autoReviewSelection.VideoStartLocal); videoStartSource=$($autoReviewSelection.VideoStartSource)"
    $reviewEvidenceInfo = Add-ReviewEvidenceToPackage `
        -StagingRoot $stagingRoot `
        -ReviewVideoPath $effectiveReviewVideoPath `
        -ReviewTimestamp $effectiveReviewTimestamp `
        -ReviewNotesPath $ReviewNotesPath `
        -ReviewEvidenceFolder $ReviewEvidenceFolder `
        -MaxReviewEvidenceFrames $MaxReviewEvidenceFrames `
        -AutoReviewStatus $autoReviewSelection.Status `
        -AutoReviewSourceLog $autoReviewSelection.SourceLog `
        -AutoReviewVideoStartLocal $autoReviewSelection.VideoStartLocal `
        -AutoReviewVideoStartSource $autoReviewSelection.VideoStartSource `
        -AutoReviewEvents $autoReviewSelection.SelectedEvents
    Write-Host "Review evidence: included=$($reviewEvidenceInfo.Included); status=$($reviewEvidenceInfo.Status); frames=$($reviewEvidenceInfo.FrameCount); manualFiles=$($reviewEvidenceInfo.ManualFileCount); manifest=$($reviewEvidenceInfo.ManifestPath)"

    Write-Step "Generating route failure summary"
    $routeFailureSummaryPath = Join-Path $stagingRoot "route-failure-summary.txt"
    New-RouteFailureSummary $latestLog $routeFailureSummaryPath
    Write-Host "Included route failure summary: route-failure-summary.txt"

    Write-Step "Generating Goblin Tracker review artifacts"
    $goblinTrackerSummaryPath = Join-Path $stagingRoot "goblin-tracker-summary.txt"
    $goblinTrackerReviewIndexPath = Join-Path $stagingRoot "goblin-tracker-review.html"
    New-GoblinTrackerPackageSummary $stagingRoot $goblinTrackerSummaryPath
    Write-Host "Included Goblin Tracker summary: goblin-tracker-summary.txt"

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

    $debugAnalysisFilesIncluded = $false
    $debugAnalysisGenerationStatus = if ($debugAnalysisToolsAvailable) { "Pending" } else { "Skipped (helper missing)" }

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
            "Debug workflow: Scripts\Create Debug Package.bat -> Scripts\create-debug-package.ps1",
            "Review export path: single batch/PowerShell ZIP package for VS Debug and Release",
            "App shutdown artifact creation: skipped by design",
            "Debug analysis files included: $debugAnalysisFilesIncluded",
            "Debug analysis generation status: $debugAnalysisGenerationStatus",
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
            "Goblin decision bundle full-image package policy: excluded by default; $excludedGoblinDecisionBundleFullEvidenceImages excluded; excludedSize=$(Format-ByteSize $excludedGoblinDecisionBundleFullEvidenceImageBytes) ($excludedGoblinDecisionBundleFullEvidenceImageBytes bytes); replay-ready Journal/Minimap crops and metadata are kept",
            "Goblin encounter/manual capture fullscreen package policy: excluded by default; $excludedGoblinCaptureFullscreenImages excluded; excludedSize=$(Format-ByteSize $excludedGoblinCaptureFullscreenImageBytes) ($excludedGoblinCaptureFullscreenImageBytes bytes); Journal/Minimap crops and metadata are kept",
            "Goblin evidence event screenshot package policy: most recent $MaxGoblinEvidenceEventScreenshots included when <= $MaxGoblinEvidenceEventScreenshotBytes bytes; $excludedGoblinEvidenceEventScreenshots excluded; $oversizedGoblinEvidenceEventScreenshots oversized",
            "Goblin observation diagnostic crop package policy: most recent $MaxGoblinObservationDiagnosticCrops included; $excludedGoblinObservationDiagnosticCrops excluded",
            "Inventory replay package policy: automatic replay screenshot folders are excluded; structured replay logs are included from Debug\ReplayLogs",
            "Replay log files included: $replayLogFileCount",
            "Review evidence package policy: selected OBS frames only; full videos are excluded by default; maxReviewEvidenceFrames=$MaxReviewEvidenceFrames",
            "Review evidence included: $($reviewEvidenceInfo.Included)",
            "Review evidence status: $($reviewEvidenceInfo.Status)",
            "Review evidence source video: $($reviewEvidenceInfo.SourceVideo)",
            "Review evidence ffmpeg: $($reviewEvidenceInfo.Ffmpeg)",
            "Auto review evidence status: $($reviewEvidenceInfo.AutoReviewStatus)",
            "Auto review evidence selected events: $($reviewEvidenceInfo.AutoReviewEventCount)",
            "Review evidence timestamps requested: $($reviewEvidenceInfo.TimestampCount)",
            "Review evidence frames included: $($reviewEvidenceInfo.FrameCount)",
            "Review evidence manual files included: $($reviewEvidenceInfo.ManualFileCount)",
            "Review evidence manifest: $($reviewEvidenceInfo.ManifestPath)",
            "Failure screenshot package policy: most recent $MaxFailureScreenshots groups included; $excludedFailureScreenshots files excluded; oversizedSelected=$($oversizedFailureScreenshots.Count); maxBytes=$MaxPackagedScreenshotBytes; includedSize=$failureScreenshotSizeDisplay ($failureScreenshotSizeBytes bytes); excludedSize=$excludedFailureScreenshotSizeDisplay ($excludedFailureScreenshotSizeBytes bytes); availableSize=$availableFailureScreenshotSizeDisplay ($availableFailureScreenshotSizeBytes bytes)",
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
            "- debug-package-analysis.txt included: $debugAnalysisFilesIncluded",
            "- goblin-tracker-timeline.md included: $debugAnalysisFilesIncluded",
            "- goblin-evidence-health.txt included: $debugAnalysisFilesIncluded",
            "- goblin-tracker-summary.txt",
            "- goblin-tracker-review.html",
            "- debug-package-size-summary.txt",
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
            "- Replay log files included: $replayLogFileCount",
            "- Review evidence included: $($reviewEvidenceInfo.Included)",
            "- Review evidence status: $($reviewEvidenceInfo.Status)",
            "- Auto review evidence status: $($reviewEvidenceInfo.AutoReviewStatus)",
            "- Auto review evidence selected events: $($reviewEvidenceInfo.AutoReviewEventCount)",
            "- Review evidence frames included: $($reviewEvidenceInfo.FrameCount)",
            "- Review evidence manual files included: $($reviewEvidenceInfo.ManualFileCount)",
            "- Review evidence manifest: $($reviewEvidenceInfo.ManifestPath)",
            "- Goblin evidence screenshots included: $goblinEvidenceScreenshotCount",
            "- Goblin evidence full images excluded: $excludedGoblinEvidenceFullImages",
            "- Goblin decision bundle full evidence images excluded: $excludedGoblinDecisionBundleFullEvidenceImages",
            "- Goblin decision bundle full evidence excluded total size: $(Format-ByteSize $excludedGoblinDecisionBundleFullEvidenceImageBytes) ($excludedGoblinDecisionBundleFullEvidenceImageBytes bytes)",
            "- Goblin capture fullscreen images excluded: $excludedGoblinCaptureFullscreenImages",
            "- Goblin capture fullscreen excluded total size: $(Format-ByteSize $excludedGoblinCaptureFullscreenImageBytes) ($excludedGoblinCaptureFullscreenImageBytes bytes)",
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
            "- Debug\GoblinEvidence\DecisionBundles\evidence.* full images are excluded by default; replay-ready *_Metadata.txt, *_Journal.png, *_Minimap.png, and decision-trace.txt are kept",
            "- Debug\GoblinEvidence\EncounterCaptures and ManualCaptures *_Fullscreen images are excluded by default; replay-ready *_Metadata.txt, *_Journal.png, and *_Minimap.png are kept",
            "- Debug/GoblinEvidence/GoblinEvidence_* event screenshots are limited to MaxGoblinEvidenceEventScreenshots newest files and MaxGoblinEvidenceEventScreenshotBytes",
            "- Debug/GoblinEvidence/ObservationDiagnostics image crops are limited to MaxGoblinObservationDiagnosticCrops newest files",
            "- Debug/GoblinEvidence/AcceptedEvidenceCandidates and Debug/GemAutoStash/AcceptedGemCandidates are limited to MaxImageRecognitionBestSampleSets newest accepted action folders",
            "- Images/Goblin Evidence/Promoted and Images/Gems/Promoted are limited to MaxImageRecognitionBestSampleSets newest sidecar-backed promoted samples and are not bulk-copied",
            "- Debug/InventoryReplay/Salvage and Debug/InventoryReplay/Stash replay image folders are excluded by default; use ReviewEvidence frames/crops for image replay",
            "- Debug/ReplayLogs structured replay logs are copied when present for the current session",
            "- ReviewEvidence includes only selected OBS frames and manually supplied evidence; full video files are not copied by default",
            "- bin folders are not copied",
            "- obj folders are not copied",
            "- source files are not copied except selected docs and manifest inputs",
            "- build artifacts are not copied except selected runtime logs/screenshots"
        )
    }

    & $buildManifestLines 0 "calculated during package creation" | Out-File -FilePath $manifestPath -Encoding utf8
    if ($debugAnalysisToolsAvailable) {
        Write-Step "Generating debug analysis files"
        try {
            $analysisResult = Write-DgaAnalysisFiles -Root $stagingRoot -OutputDirectory $stagingRoot
            $debugAnalysisFilesIncluded = $true
            $debugAnalysisGenerationStatus = "Included"
            Write-Host "Included debug package analysis: $(Split-Path -Leaf $analysisResult.AnalysisPath)"
            Write-Host "Included Goblin Tracker timeline: $(Split-Path -Leaf $analysisResult.TimelinePath)"
            Write-Host "Included Goblin Evidence health report: $(Split-Path -Leaf $analysisResult.HealthPath)"
        }
        catch {
            $debugAnalysisFilesIncluded = $false
            $debugAnalysisGenerationStatus = "Skipped (generation failed: $($_.Exception.Message))"
            Write-Warning $debugAnalysisGenerationStatus
        }
    }

    $packageSizeBytes = 0L
    $packageSizeSummaryPath = Join-Path $stagingRoot "debug-package-size-summary.txt"
    New-DebugPackageSizeSummaryFromZip $zipPath $packageSizeSummaryPath
    for ($i = 0; $i -lt 10; $i++) {
        $packageSizeDisplay = if ($packageSizeBytes -gt 0) { Format-ByteSize $packageSizeBytes } else { "calculated during package creation" }
        & $buildManifestLines $packageSizeBytes $packageSizeDisplay | Out-File -FilePath $manifestPath -Encoding utf8
        New-GoblinTrackerReviewIndex $stagingRoot $goblinTrackerReviewIndexPath
        Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $zipPath -Force
        New-DebugPackageSizeSummaryFromZip $zipPath $packageSizeSummaryPath
        $newPackageSizeBytes = (Get-Item -LiteralPath $zipPath).Length
        if ($newPackageSizeBytes -eq $packageSizeBytes) {
            break
        }

        $packageSizeBytes = $newPackageSizeBytes
    }

    $packageSizeDisplay = Format-ByteSize $packageSizeBytes
    Write-Host "Included Goblin Tracker review index: goblin-tracker-review.html"
}
finally {
    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}

Invoke-DebugPackageRetentionCleanup $packageDirectory $DebugPackageRetentionCount

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
Write-Host "Replay logs:         $replayLogFileCount files included"
Write-Host "Evidence screenshots:$goblinEvidenceScreenshotCount"
Write-Host "Observation crops:   $($selectedGoblinObservationDiagnosticCrops.Count) included; $excludedGoblinObservationDiagnosticCrops excluded"
Write-Host "Review evidence:     included=$($reviewEvidenceInfo.Included); frames=$($reviewEvidenceInfo.FrameCount); manualFiles=$($reviewEvidenceInfo.ManualFileCount)"
Write-Host "Auto review:         status=$($reviewEvidenceInfo.AutoReviewStatus); events=$($reviewEvidenceInfo.AutoReviewEventCount)"
$reviewVideoSummaryPath = if ($null -ne $reviewEvidenceInfo -and -not [string]::IsNullOrWhiteSpace($reviewEvidenceInfo.SourceVideo) -and $reviewEvidenceInfo.SourceVideo -ne "none") { $reviewEvidenceInfo.SourceVideo } else { "" }
if (-not [string]::IsNullOrWhiteSpace($reviewVideoSummaryPath) -and (Test-Path -LiteralPath $reviewVideoSummaryPath -PathType Leaf)) {
    $reviewVideoFile = Get-Item -LiteralPath $reviewVideoSummaryPath
    $reviewVideoDurationSeconds = Get-VideoDurationSeconds $reviewVideoFile.FullName
    $reviewVideoDurationDisplay = if ($reviewVideoDurationSeconds -gt 0) { ([TimeSpan]::FromSeconds($reviewVideoDurationSeconds)).ToString("hh\:mm\:ss") } else { "unknown" }
    Write-Host "Video Clip Review:   $($reviewVideoFile.FullName)"
    Write-Host "Review video info:   duration=$reviewVideoDurationDisplay; size=$(Format-ByteSize $reviewVideoFile.Length); modified=$($reviewVideoFile.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss zzz'))"
}
else {
    Write-Host "Video Clip Review:   none"
    Write-Host "Review video info:   none"
}
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
Write-Host "Analysis files:      $debugAnalysisGenerationStatus"
Write-Host "Manifest:            debug-package-manifest.txt"
Write-Host "==========================================="

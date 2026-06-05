# GoblinFarmer Test Checklist

## Make New Game
- Battle.net launches/focuses
- Diablo III tab selected
- Play clicked
- Paired success screenshots captured for Battle.net Play click accepted
- Diablo launches
- Paired success screenshots captured for Diablo process detected
- Start Game clicked
- Paired success screenshots captured for Start Game click accepted
- Southern Highlands loaded
- Paired success screenshots captured for Southern Highlands teleport confirmation

## Teleport Route
- Southern Highlands -> Northern Highlands
- Northern Highlands -> Weeping Hollow
- Weeping Hollow -> Festering Woods
- Festering Woods -> Cathedral
- Cathedral Level 1 blocks Royal Crypts
- Cathedral Level 3 allows Royal Crypts
- City of Caldeum only allows Ancient Waterway from Ruined Cistern
- Stinging Winds blocks Battlefields
- Black Canyon Mines allows Battlefields
- Caverns of Frost Level 1 blocks Teleport Next
- Caverns of Frost Level 2 allows Rakkis Crossing
- Each successful teleport confirmation captures paired Diablo/App success screenshots
- Route blocks/failures capture paired Diablo/App failure screenshots

## Failure Recovery
- Interrupted teleport preserves green/orange buttons
- Retry hotkey uses same intended target
- Failed teleport does not advance route
- Failure screenshots include both Diablo and GoblinFarmer app evidence

## Goblin Tracker
- Default resolved areas allow one goblin count per area per game
- Manual `X` logs `AreaDetectionAmbiguous` when PF and known channel/cavern, Moon Clan, or Cathedral title-template matches are extremely close
- Manual `X` uses route context to select Western Channel Level 1/2 or Eastern Channel Level 1/2 from Ancient Waterway context instead of consuming PF slots
- Manual `X` uses route context to select Caverns of Frost Level 1/2 from Battlefields context instead of consuming PF slots
- Manual `X` uses route context to select Cave Of The Moon Clan Level 1 from Southern Highlands context instead of consuming PF1 slots
- Manual `X` uses route context to select Cathedral Level 1/2 from exact Cathedral level context instead of consuming PF slots
- Unresolved PF-leading ambiguity suppresses the count with `GoblinCountSuppressed reason=AmbiguousAreaDetection`
- Manual `X` in WhimsyDale suppresses with `GoblinCountSuppressed areaKey=WhimsyDale reason=BlockedArea source=ManualHotkey`
- Repeated manual `X` in WhimsyDale remains blocked
- City of Caldeum, Gates of Caldeum, Caldeum Bazaar, Flooded Causeway, Ancient Waterway, and The Bridge Of Korsikk suppress with `GoblinCountSuppressed`, `reason=BlockedArea`, and `source=ManualHotkey`
- Manual-count blocked areas do not increment the counter and do not consume an area-count slot
- Cave Of The Moon Clan Level 1 counts once and suppresses the second press as the same area
- Cathedral Level 1 counts once and suppresses the second press as the same area
- Cathedral Level 2 counts once and suppresses the second press as the same area
- Western Channel Level 1 counts once and suppresses the second press as the same area
- Western Channel Level 2 counts once and suppresses the second press as the same area
- Eastern Channel Level 1 counts once and suppresses the second press as the same area
- Eastern Channel Level 2 counts once and suppresses the second press as the same area
- Caverns of Frost Level 1 counts once and suppresses the second press as the same area
- Caverns of Frost Level 2 counts once and suppresses the second press as the same area
- Pandemonium Fortress Level 1 accepts goblin counts 1 and 2 in the same game
- Pandemonium Fortress Level 1 suppresses goblin count 3 with `GoblinCountSuppressed`, `areaCount=2`, `areaLimit=2`, and `reason=AreaLimitReached`
- Pandemonium Fortress Level 2 accepts goblin counts 1 and 2 in the same game
- Pandemonium Fortress Level 2 suppresses goblin count 3 with `GoblinCountSuppressed`, `areaCount=2`, `areaLimit=2`, and `reason=AreaLimitReached`
- Sewers of Caldeum, Ruined Cistern, Channel, Cave, Cathedral, and Battlefields subregions still resolve separately where expected and remain capped at one count per area per game unless explicitly blocked from manual counts
- Reset Stats clears goblin count, tracker active time, GPH, and per-area count state
- New Game clears per-area count state while preserving the current session tracker statistics
- Unknown-area manual fallback still counts through the existing fallback behavior and does not create a counted area key
- Accepted manual `X` counts show a no-activate notification for 5 seconds with goblin counted, area/location, goblin type or `Unknown`, and current total
- Automation Observation Mode is enabled for `JournalCandidate` and `MinimapCandidate`
- Combat start logs `GoblinEvidenceScannerStartRequested` and `GoblinEvidenceScannerStarted`
- Combat stop logs `GoblinEvidenceScannerStopped`
- Evidence loop logs `GoblinEvidenceScanAttempted` during eligible combat scans
- Evidence loop logs `GoblinEvidenceScanSkipped` with a reason when combat/scanner conditions are not eligible
- Evidence loop logs Journal/Minimap crop paths from `Debug\GoblinEvidence\ObservationDiagnostics`
- If Goblin Evidence templates are missing, startup/scanner diagnostics log one clear `GoblinEvidenceTemplateSetupMissing` line and throttled `GoblinEvidenceScanResult reason=MissingTemplate` summaries
- Missing Goblin Evidence templates do not spam `GoblinEvidenceCandidateCheck reason=MissingTemplate` on every scan
- VS Debug missing-template notification names the needed templates without activating over Diablo
- Goblin Evidence template discovery accepts `<Goblin Type> Engaged Journal.png`, `<Goblin Type> Killed Journal.png`, `<Goblin Type> Engaged & Killed Journal.png`, and `<Goblin Type> Minimap.png`
- Combined `Engaged & Killed Journal` templates are accepted
- Invalid Goblin Evidence template names log a clear setup warning without scan spam
- Journal ObservationDiagnostics crops use the calibrated `64,736,645,417` reference region
- Minimap ObservationDiagnostics crops use the calibrated `2108,66,421,423` reference region
- `GoblinEvidenceScanResult source=Journal` and `source=Minimap` include `scanRegion`, `screenRegion`, template count, matched goblin type, and confidence
- Evidence detector logs `GoblinEvidenceCandidateCheck` for candidate found/not found and below-threshold reasons once templates are present
- `JournalCandidate` logs `GoblinObservationCandidate` and `GoblinObservationSummary` without changing GoblinCount, GPH, tracker active time, found records, or counted-area slots
- `MinimapCandidate` logs `GoblinObservationCandidate` and `GoblinObservationSummary` without changing GoblinCount, GPH, tracker active time, found records, or counted-area slots
- Observation candidates report the matched goblin type when a Journal or Minimap template passes confidence
- Blocked observation areas such as WhimsyDale log `wouldCount=False` and `reason=BlockedArea`
- Duplicate observation areas log `wouldCount=False` without consuming another area-count slot
- PF1/PF2 observations report eligibility against `areaLimit=2` without changing real count state
- The Goblin Tracker UI shows the compact read-only Last Observation block with goblin type, area, source, and reason

## Debug Package Evidence
- Debug package includes latest log
- Debug package includes `route-failure-summary.txt`
- Debug package includes `debug-screenshot-manifest.txt`
- Screenshot manifest pairs success/failure Diablo and App screenshots by timestamp, workflow, and action
- Screenshot manifest includes only current-session screenshots
- Failure screenshots are included by default
- Debug screenshots are included according to the active debug screenshot settings
- Success screenshots are disabled by default in normal Release, installed app, and VS Debug
- Success screenshots are excluded from packages by default
- Success screenshots are included in packages only with explicit `-IncludeSuccessScreenshots`
- If `Debug.EnableSuccessScreenshots=true`, package manifest reports available success screenshot count and total size without automatically including them
- Failure screenshots are included under `Screenshots\Failure`
- Recent debug screenshots are included under `Screenshots\Recent`
- Screenshots from previous sessions are excluded from the package
- Debug package manifest reports package size, session start, session duration, total screenshots, success screenshot availability, failure screenshots, folder totals, and stale screenshot exclusions
- Debug package manifest reports Goblin observation counters and last-observation metadata from `session-info.txt`
- Debug package manifest reports Goblin Evidence missing-template state from the latest log
- Debug package includes only a small recent sample from `Debug\GoblinEvidence\ObservationDiagnostics` and reports included/excluded observation crop counts
- GoblinEvidence Calibration full images remain excluded by default unless `-MaxGoblinEvidenceFullImages` is explicitly raised
- Screenshot retention cleanup still controls all runtime screenshots in the shared `Screenshots` folder

## Debug Manager / Session Summary
- VS Debug starts with Debug Mode, diagnostic overlay, route inspector, debug screenshots, missing-asset prompts, and verbose logging enabled in memory
- VS Debug forced evidence settings are not saved as Release/user preferences
- Normal Release starts quiet: Debug Mode off, diagnostics hidden, debug screenshot controls hidden
- Release Debug Mode opt-in enables diagnostic overlay, route inspector, missing-asset prompts, and seeds Keep Debug Screenshots on
- Turning Release Debug Mode off returns to the compact/quiet diagnostics UI
- Session counters increase from existing events: games created, teleports attempted/confirmed/blocked/failed, Start Game failures, Battle.net launch failures, repair/salvage failures, workflow cancellations, unexpected exceptions
- Combat active time increases only while combat is running
- App exit writes `Sessions\Session_YYYYMMDD_HHMMSS.md`
- Session summary includes app version/build mode, start/end/duration, latest log, latest debug package, latest screenshot/failure screenshot, latest failure type, and last known issue
- Session summary includes observation-only counters for Goblin Observations, Journal Observations, Minimap Observations, Eligible Observations, Blocked Observations, and Duplicate Observations
- Startup retention keeps only the newest `Debug.SessionSummaryRetentionCount` matching `Sessions\Session_*.md` files
- Startup retention keeps only the newest `Debug.DebugPackageRetentionCount` matching `DebugPackages\GoblinFarmer_Debug_*.zip` files
- Retention cleanup does not delete unrelated files in `Sessions\` or unrelated zip files in `DebugPackages\`
- Image-recognition diagnostic logs appear only when diagnostic logging is enabled and are throttled; no extra scans occur

## Town And Exit
- Repair complete captures paired Diablo/App success screenshots
- Salvage complete or skipped captures paired Diablo/App success screenshots
- Leave Game main menu confirmation captures paired Diablo/App success screenshots
- Exit Game complete captures paired Diablo/App success screenshots

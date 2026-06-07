# GoblinFarmer Test Checklist

Current active validation lives in `Docs/TODO.md`. This checklist is a compact smoke-test reference.

## Build And Packaging

- `dotnet build GoblinFarmer.csproj` succeeds.
- `dotnet run --project Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false` succeeds.
- `git diff --check` has no whitespace errors.
- `Scripts\Create Debug Package.bat` creates a ZIP when explicit review artifacts are needed.

## Goblin Tracker

- Observation Mode and Auto Goblin Count can be toggled from VS Debug Settings.
- VS Debug Settings shows `Capture`.
- Physical `X` does not increment Goblin Tracker.
- Automatic counts still create decision bundles and encounter captures automatically.
- The Capture button creates manual recognition files only when clicked.
- VS Debug can use ignored `Config\AppSettings.local.json` for private local paths/toggles while tracked `Config\AppSettings.json` stays sanitized.
- VS Debug/debug sessions write `Debug\GoblinEvidence\GoblinTrackerEvents.jsonl` when observations or automatic-count decisions occur.
- Logs include throttled `GoblinEvidenceTimingSummary` scan-stage timing output during active Goblin Evidence scanning.
- Default areas count once per game.
- PF1, PF2, and Stinging Winds allow two counts per game and suppress the third.
- Blocked count areas suppress with `BlockedArea` and do not consume area slots.
- Reset Stats clears count, GPH, active time, duplicate guard, evidence state, and Last Observation.
- New Game clears per-game duplicate/evidence state.
- Cave Of The Moon Clan Level 1/2, Eastern Channel Level 1/2, Western Channel Level 1/2, and Caverns of Frost Level 1/2 remain separate where expected.
- Battlefields journal history does not replay stale/non-goblin rows into a fresh count.
- Journal logs include name-validation/history-row/history-input suppression when those protections fire.

## Route Logic

- Cave Of The Moon Clan Level 1 blocks Teleport Next; Level 2 can continue the Southern Highlands route.
- Cathedral blocks Royal Crypts except Cathedral Level 3.
- City Of Caldeum blocks Ancient Waterway except Ruined Cistern.
- Plain Ancient Waterway blocks Teleport Next to Stinging Winds.
- Eastern Channel Level 2 allows Stinging Winds.
- Western Channel Level 2 returns to Ancient Waterway.
- Stinging Winds blocks Battlefields except Black Canyon Mines.
- Caverns of Frost Level 1 blocks Teleport Next; Level 2 allows Rakkis Crossing.
- Walking to queued Rakkis Crossing then pressing Teleport Next advances and starts PF1 in the same press.

## Debug Artifacts

- Debug package includes latest logs, manifests, session info, route summaries, Goblin Tracker summary, Goblin Evidence, decision bundles, encounter captures, and observation diagnostics.
- Debug package/log review can use `GoblinTrackerEvents.jsonl` plus timing summaries to correlate observation, suppression, and count latency.
- Encounter captures save fullscreen, minimap, journal, and metadata for accepted Goblin Tracker counts in VS Debug.
- Manual recognition captures save fullscreen, minimap, journal, and metadata only when `Capture` is clicked.

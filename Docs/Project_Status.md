# GoblinFarmer Project Status

This file is the current source of truth for the active release, stable behavior, route/blocking rules, and next development plans. Historical notes and older package reviews live in `Docs/History.md`. Open work and validation items live in `Docs/TODO.md`.

## Current Release

- Version: `v1.4.0`
- Current focus: Goblin Tracker full automatic counting readiness in VS Debug.
- Automatic counting: implemented behind `GoblinTracker.EnableObservationMode=true` and `GoblinTracker.EnableAutomaticCounting=true`.
- Manual count hotkey: physical `X` has been retired. Goblin counts should come from automatic eligible evidence.
- VS Debug Goblin Tracker controls: `Observation Mode`, `Auto Goblin Count`, `Enable Goblin Decision Trace`, and `Capture`.
- VS Debug `Capture` button: manual image-recognition aid only. It writes fullscreen, minimap, journal, and metadata files under `Debug\GoblinEvidence\ManualCaptures` only when clicked.
- Automatic debug artifacts: accepted Goblin Tracker count workflows still automatically write decision bundles and encounter captures needed for debugging.
- `Next Tests` tab: removed. Current validation steps are tracked in `Docs/TODO.md`.
- Latest change: Goblin Replay now accepts explicit capture folders, specific `*_Metadata.txt` files, specific capture prefixes, and DecisionBundle folders from the developer harness. This changed replay usability only; live Goblin Tracker counting behavior was not changed.

## Latest Review Finding

- Latest reviewed package `GoblinFarmer_Debug_20260607_140848.zip` showed stale Battlefields journal-history replay after Enter/journal history.
- Current fix validates the goblin-name portion of Journal templates, ignores upper/history rows, and briefly suppresses Journal candidates after physical Enter/journal-history input.

## Stable Systems

- Battle.net/Diablo launch, Start Game, Make New Game, route teleporting, interrupted teleport recovery, repair/salvage, Kadala timing, and Witch Doctor combat are stable enough for ongoing monitoring.
- Release and VS Debug Goblin Tracker layouts keep evidence and Last Observation fields readable.
- Debug package workflow remains `Scripts\Create Debug Package.bat`, which creates a ZIP for VS Debug and Release review.
- Form close remains quiet and does not generate packages or loose review files.
- VS Debug and release/debug-mode artifact retention is 7 days; Goblin Evidence folders also use count/package limits.
- Tracked `Config\AppSettings.json` is sanitized for release/defaults. Private VS Debug paths and toggles should live in ignored `Config\AppSettings.local.json` when needed.
- Generated EXEs, installer output, portable ZIPs, debug packages, logs, screenshots, source-upload output, and retired `GitHub Upload` copies are not source files.

## Goblin Tracker Current Behavior

- Observation scanner interval: `500ms`.
- Journal scan region: `64,736,645,417`.
- Minimap scan region: `2108,66,421,423`.
- Automatic counting requires Observation Mode and Auto Goblin Count to both be enabled.
- Automatic counting uses existing area resolution, block list, duplicate guard, stale journal protection, and area-limit logic.
- Goblin Evidence scanning caches discovered template metadata and loaded OpenCV template mats, captures each source scan region once per pass, and scans Minimap before Journal while preserving Journal as the primary confirmation if both match.
- Goblin Replay Fixture Runner Phase 2E is implemented as explicit/on-demand harness support. Live scans still use `CopyFromScreen` by default, saved Journal/Minimap PNG fixtures can be fed through the shared frame/template matching path, and real saved encounter/manual capture folders can be loaded from the test harness command line for stale-location regression coverage without normal startup, VS Debug startup, scanner, route, combat, town, or debug package workflows invoking replay.
- Goblin Replay supported inputs: shared capture folders through `--goblin-replay-captures`, specific `*_Metadata.txt` files through `--goblin-replay-metadata`, exact capture prefixes through `--goblin-replay-prefix`, and DecisionBundle folders through `--goblin-replay-decision-bundle`. DecisionBundle replay runs only when replay-ready Journal/Minimap capture frames can be resolved; otherwise it reports the trace/evidence files it found and explains the limitation.
- Goblin Evidence timing summaries log stage histograms through `GoblinEvidenceTimingSummary`.
- VS Debug/debug/decision-trace sessions write structured Goblin Tracker JSONL events to `Debug\GoblinEvidence\GoblinTrackerEvents.jsonl` alongside human-readable logs.
- Default area limit: 1 count per resolved area per game.
- Two-count exceptions: Pandemonium Fortress Level 1, Pandemonium Fortress Level 2, and Stinging Winds.
- Blocked count areas include New Tristram, WhimsyDale, City of Caldeum, Gates of Caldeum, Caldeum Bazaar, Flooded Causeway, Ancient Waterway, and The Bridge Of Korsikk.
- Treasure/Odious and Gilded/Malevolent minimap ambiguity pairs use color diagnostics/overrides.
- Reset Stats clears count, GPH/active time, duplicate guard, auto-count evidence state, observation state, and Last Observation.
- New Game clears per-game duplicate/evidence state so fresh evidence can count again.

## Route Logic

- Southern Highlands -> Northern Highlands.
- Cave Of The Moon Clan Level 1 blocks Teleport Next; Cave Of The Moon Clan Level 2 is allowed as a Southern Highlands sublocation.
- Northern Highlands -> The Weeping Hollow -> The Festering Woods -> Cathedral.
- Cathedral blocks Royal Crypts except Cathedral Level 3.
- Royal Crypts -> City Of Caldeum.
- City Of Caldeum blocks Ancient Waterway except Ruined Cistern.
- Ancient Waterway -> Stinging Winds. Plain Ancient Waterway blocks Teleport Next to Stinging Winds.
- Western Channel Level 1 blocks; Western Channel Level 2 returns to Ancient Waterway.
- Eastern Channel Level 1 blocks; Eastern Channel Level 2 allows Stinging Winds.
- Stinging Winds blocks Battlefields except Black Canyon Mines.
- Battlefields -> Rakkis Crossing. Caverns of Frost Level 1 blocks; Caverns of Frost Level 2 allows Rakkis Crossing.
- Rakkis Crossing -> Pandemonium Fortress Level 1 -> Pandemonium Fortress Level 2 -> Make New Game.

## Next Development Plans

- Validate the latest journal-history suppression, name-validation fix, scan timing summaries, and JSONL event output during normal VS Debug use.
- Next Goblin Replay work should use the explicit harness command against real capture folders, specific metadata files/prefixes, or DecisionBundle folders from suspicious live sessions and only promote narrowly proven stale-location fixes into production code.
- Latest validation for the replay usability change: `dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false`, `dotnet test .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`, and `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false` passed. Still needs testing: use the new metadata/prefix/DecisionBundle commands against a real suspicious live session.
- Continue using automatic counting in real runs instead of focused specific-goblin hunts.
- Use the `Capture` button only when an image-recognition issue is visible and extra minimap/journal/fullscreen evidence would help.
- Keep `Docs/TODO.md` synchronized with remaining work and next test steps.

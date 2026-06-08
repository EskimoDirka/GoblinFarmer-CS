# GoblinFarmer Project Status

This file is the current source of truth for the active release, stable behavior, route/blocking rules, and next development plans. Historical notes and older package reviews live in `Docs/History.md`. Open work and validation items live in `Docs/TODO.md`.

## Current Release

- Version: `v1.4.0`
- Current focus: Goblin Tracker full automatic counting readiness in VS Debug.
- Automatic counting: implemented behind `GoblinTracker.EnableObservationMode=true` and `GoblinTracker.EnableAutomaticCounting=true`.
- Manual count hotkey: physical `X` has been retired. Goblin counts should come from automatic eligible evidence.
- VS Debug Goblin Tracker controls: `Observation Mode`, `Auto Goblin Count`, `Enable Goblin Decision Trace`, `Capture`, a debug-only simulation area selector, and `Sim Count`.
- VS Debug `Capture` button: manual image-recognition aid only. It writes fullscreen, minimap, journal, and metadata files under `Debug\GoblinEvidence\ManualCaptures` only when clicked.
- VS Debug `Sim Count` button: developer-only count-policy simulator. It uses existing area-key resolution, blocked-area checks, duplicate guard, and area-limit logic so accepted counts, duplicate suppression, and PF1/PF2/Stinging Winds `AreaLimitReached` behavior can be tested without waiting for rare live spawns.
- Automatic debug artifacts: accepted Goblin Tracker count workflows still automatically write decision bundles and encounter captures needed for debugging.
- `Next Tests` tab: removed. Current validation steps are tracked in `Docs/TODO.md`.
- Latest change: normal Goblin Evidence scanner events no longer write redundant root `GoblinEvidence_*` full/event images by default. Decision bundles and encounter captures now carry the replay-ready Journal/Minimap crops, metadata, JSONL, and trace data needed for debugging; the explicit VS Debug `Capture` button remains the only path that intentionally saves a fullscreen image for image-recognition troubleshooting.

## Latest Review Finding

- Latest reviewed package `GoblinFarmer_Debug_20260608_114121.zip` was a VS Debug run with Observation Mode, Auto Goblin Count, and Decision Trace enabled.
- Confirmed Eastern Channel Level 1 is now corrected: a Treasure Goblin counted once and Last Observation/notification reported `Eastern Channel Level 1`, not Pandemonium Fortress Level 1.
- Found a Stinging Winds miss: the Stinging Winds Treasure Goblin did not count because stale Treasure Goblin Journal evidence from the earlier Eastern Channel Level 1 count refreshed the counted-encounter state, then a later fresh Stinging Winds Minimap hit was suppressed as `EncounterAlreadyAutoCounted`.
- Found a Rakkis Crossing stale recount: a Black Canyon Mines Odious Collector counted correctly from fresh Minimap evidence, but repeated same-area duplicate Journal evidence did not refresh the counted encounter's Journal row state; after teleporting to Rakkis Crossing, the same visible Odious Collector Journal row was accepted as a new Rakkis count.
- Fix implemented: cross-area stale Journal suppression no longer refreshes the counted encounter state, so an old Journal row cannot block a later fresh Minimap hit in the new area. Same-area duplicate Journal evidence now refreshes the counted encounter state, so a visible Journal row that was already tied to a counted area cannot become a fresh count after the next teleport. Goblin Replay template scenarios and direct policy tests cover the Stinging stale-Journal/fresh-Minimap case and the Rakkis same-row stale Journal guard.
- Confirmed Make New Game carryover behavior from the package: opening the old Journal text after Make New Game did not recount or update Last Observation.
- Latest reviewed package `GoblinFarmer_Debug_20260608_062030.zip` was a VS Debug run with Observation Mode, Auto Goblin Count, and Decision Trace enabled.
- Eastern Channel Level 1, Stinging Winds, and Rakkis Crossing did not spawn goblins in that run, so those route-specific live validations remain open.
- Confirmed the package matched the user's Make New Game note: Blood Thief counted in `The Weeping Hollow`, then Make New Game reset count/evidence state at `06:19:13`, then the still-visible Blood Thief killed Journal row was treated as fresh first-seen evidence in `Southern Highlands` and counted again at `06:19:19`.
- Root cause: `NewGameCreated` correctly cleared the duplicate guard and auto-count/evidence state, but clearing journal first-seen memory made already-visible previous-game Journal rows eligible again when they shifted upward in the feed under the new starting area.
- Fix implemented: Reset Stats/New Game now remembers visible Journal line families before clearing first-seen state and suppresses those reset-carryover rows while they remain visible, even if their row bucket moves slightly. This preserves the stat reset while preventing previous-game journal text from becoming a new-game count.
- Goblin Replay usability improvement implemented: the explicit test harness now supports `--goblin-replay-scenario` text files that synthesize temporary Journal/Minimap crop frames from `Images\Goblin Evidence` templates and can resolve area context from `Images\Current Location` templates through a shared current-location image resolver. The first targeted scenarios validate fresh Journal evidence before New Game, a New Game reset action, suppression of the same shifted visible Journal row as `JournalCandidateIgnoredResetCarryover`, Eastern Channel Level 1/2 independent area resolution, Stinging Winds -> Black Canyon stale-journal suppression, and Rakkis Crossing fresh minimap counting.
- Latest reviewed package `GoblinFarmer_Debug_20260608_055247.zip` was a VS Debug run with Observation Mode, Auto Goblin Count, and Decision Trace enabled.
- Confirmed Cave Of The Moon Clan Level 1 and Level 2 can count independently in the same game from fresh evidence: Level 1 Gem Hoarder counted from Journal, then Level 2 Gem Hoarder counted from Minimap and subsequent Level 2 Journal evidence suppressed as the same encounter.
- Confirmed Eastern Channel Level 2 can count independently after Level 1 stale evidence: Level 1 Blood Thief evidence was later suppressed on Level 2, then a fresh Eastern Channel Level 2 Treasure Goblin counted.
- Found an Eastern Channel Level 1/Pandemonium false-positive issue: a Blood Thief minimap candidate resolved to `Eastern Channel Level 1`, but the later Journal confirmation resolved/count accepted as `Pandemonium Fortress Level 1`, consuming a PF1 area slot and showing the wrong Last Observation area.
- Fix implemented: recent same-goblin Minimap observations in Eastern/Western Channel Level 1 or 2 now provide a short-lived area context for Journal follow-up evidence when the Journal current-location scan resolves to the matching Pandemonium Fortress level while route context is Ancient Waterway/channel. This preserves real PF1/PF2 behavior but prevents channel journal follow-ups from consuming PF slots.
- Goblin Replay against the Battlefields/Rakkis Gilded Baron decision bundles from the same package counted the fresh Battlefields evidence and suppressed the later Rakkis journal row as `EncounterAlreadyAutoCounted`, matching intended stale-transition behavior.
- Latest reviewed package `GoblinFarmer_Debug_20260607_212052.zip` was a VS Debug run with Observation Mode, Auto Goblin Count, and Decision Trace enabled.
- Confirmed package behavior matched live notes: Southern Highlands Treasure Goblin auto-counted correctly from fresh Minimap evidence, then Cave Of The Moon Clan Level 1 incorrectly auto-counted the stale Southern Highlands Treasure Goblin Journal row.
- Goblin Replay against the exact accepted Cave DecisionBundle `gdt-20260608022012960-08083e38` reproduced the bad decision as `Count/Eligible`, confirming this was a policy/state bug rather than only live timing.
- Root cause: after the fresh Southern Highlands Minimap count, the matching Journal row was suppressed as `RecentSourceVariant:Minimap->Journal`, but the remembered encounter state refreshed only source/last-seen data and kept the old Minimap evidence key. When the same Journal row was later detected after the area changed to Cave Of The Moon Clan Level 1, encounter suppression could not compare against a Journal evidence key and area duplicate logic accepted it as a new count. Suppressed source variants now refresh the encounter evidence key as well, so later visible Journal rows can be recognized as the same counted encounter.
- Goblin Replay scenario evaluation now mirrors the shared counted-encounter source-variant policy so an explicit multi-step replay can cover Minimap count -> suppressed Journal row -> later stale Journal row after an area transition.
- Latest reviewed package `GoblinFarmer_Debug_20260607_192644.zip` was a VS Debug form run with Observation Mode, Auto Goblin Count, and Decision Trace enabled. The package already contained the prior recent-last-seen suppression fix.
- Confirmed good behavior from the package: Highlands Cave, Weeping Hollow, Eastern Channel Level 1, and Eastern Channel Level 2 all produced accepted automatic counts; Eastern Channel Level 2 Gelatinous Sire counted quickly; accepted-count Last Observation persistence stayed active.
- Root cause of the Stinging Winds miss and Rakkis Crossing miss: fresh Minimap observations were detected, but `SameEvidenceKey` compared the area-independent Minimap icon signature against a much older same-type encounter in another area. Tight Minimap templates can naturally produce the same evidence signature across different maps, so Minimap-to-Minimap exact-key suppression is now same-area only.
- Root cause of the later Black Canyon Mines stale count: the fresh Stinging Winds Treasure Goblin Minimap hit was suppressed by that cross-area Minimap collision, then the delayed Treasure Goblin journal row appeared about 29 seconds later in Black Canyon Mines and was treated as eligible. Cross-source Minimap-to-Journal encounter suppression now uses a 45-second window to match the journal freshness window, so delayed journal rows after recent Minimap counts stay attached to the original encounter.
- The same package showed Rakkis Crossing Gem Hoarder Minimap evidence at high confidence (`0.972`) but suppressed as a stale Western Channel Level 1 Gem Hoarder by the same cross-area Minimap collision. The same policy fix should allow the fresh Rakkis count while still suppressing same-area duplicate Minimap scans.
- Notification latency remains mainly evidence-detection latency, not splash display latency. Timing summaries show a 500ms scanner interval with average total scan time around 460-470ms in this run. Continue live-validation after the stale/minimap collision fix before lowering thresholds or adding broader timing changes.
- VS Debug `Sim Count` controls remain useful for deterministic policy checks until full automatic counting has enough live confidence. Re-evaluate removal after remaining live auto-count validation is stable.
- Latest reviewed package `GoblinFarmer_Debug_20260607_183831.zip` showed Battlefields Treasure Goblin auto-counting correctly from fresh minimap evidence, then incorrectly counting stale Battlefields Treasure Goblin journal text as a new Fields of Slaughter count at `encounterAgeSeconds=20.8`.
- Root cause: same-encounter source-variant protection used only the original count timestamp for its 20-second recent-variant guard. The stale journal row was repeatedly suppressed until `19.6s`, refreshed encounter last-seen state, then slipped through just after the original count age crossed 20 seconds. The fix uses recent encounter `LastSeenUtc` continuity for source variants, while still keeping the suppression bounded so a later unrelated same-type goblin is not blocked forever.
- The same package showed Caverns of Frost Level 1 Gilded Baron auto-counting. User clarified Caverns of Frost Level 1 should allow one Goblin count even though it blocks Teleport Next route advancement; Caverns of Frost Level 2 should also allow one independent fresh count. Stale Level 1 evidence must still not become a Level 2 count.
- Last Observation was clearing repeatedly on no-candidate scans and area changes. Accepted manual, simulation, and automatic counts now publish a `Counted` Last Observation state that suppressed/stale/no-candidate scans and area changes preserve until Reset Stats, Make New Game, app restart, or a newer accepted count replaces it.

## Stable Systems

- Battle.net/Diablo launch, Start Game, Make New Game, route teleporting, interrupted teleport recovery, repair/salvage, Kadala timing, and Witch Doctor combat are stable enough for ongoing monitoring.
- Release and VS Debug Goblin Tracker layouts keep evidence and Last Observation fields readable.
- Debug package workflow remains `Scripts\Create Debug Package.bat`, which creates a ZIP for VS Debug and Release review.
- Form close remains quiet and does not generate packages or loose review files.
- VS Debug and release/debug-mode artifact retention is 7 days; Goblin Evidence folders also use count/package limits.
- Debug ZIPs keep Goblin Tracker evidence replayable without carrying every full screenshot: Journal/Minimap crops, metadata, JSONL, logs, manifests, summaries, observation diagnostics, encounter captures, and decision traces are kept; normal root `GoblinEvidence_*` full/event images are skipped by default, and old full DecisionBundle evidence images plus capture fullscreen images are excluded from ZIPs by default and reported in the manifest.
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
- Replay scenario evaluation uses the same `GoblinAutoCountEncounterSuppressionPolicy` as live automatic counting for counted-encounter source variants, while still remaining explicit/on-demand only.
- Goblin Replay supported inputs: shared capture folders through `--goblin-replay-captures`, specific `*_Metadata.txt` files through `--goblin-replay-metadata`, exact capture prefixes through `--goblin-replay-prefix`, DecisionBundle folders through `--goblin-replay-decision-bundle`, and synthetic template scenario files through `--goblin-replay-scenario`. DecisionBundle replay runs only when replay-ready Journal/Minimap capture frames can be resolved; otherwise it reports the trace/evidence files it found and explains the limitation. Template scenarios remain explicit/on-demand and create only temporary crop frames that are deleted after replay. Scenario `Location=...` steps can use current-location title PNGs to resolve the count area instead of manually specifying `Area=...`.
- New DecisionBundles are directly replay-ready through `--goblin-replay-decision-bundle` because each bundle carries its own metadata plus Journal/Minimap crops. Older bundles that contain only `decision-trace.txt` and fullscreen `evidence.png` remain manual-review evidence but are not crop-replayable unless matching capture frames exist elsewhere.
- Normal scanner evidence events record type/confidence/source and feed observation/auto-count, but skip the old root screenshot file. Replay/debug imagery should come from DecisionBundles, EncounterCaptures, ObservationDiagnostics, or the explicit manual `Capture` button.
- VS Debug `Sim Count` is not a replay feature and is not wired into live scanning. It is an explicit developer button that records through the existing session count path only when clicked.
- Goblin Evidence timing summaries log stage histograms through `GoblinEvidenceTimingSummary`.
- VS Debug/debug/decision-trace sessions write structured Goblin Tracker JSONL events to `Debug\GoblinEvidence\GoblinTrackerEvents.jsonl` alongside human-readable logs.
- Default area limit: 1 count per resolved area per game.
- Two-count exceptions: Pandemonium Fortress Level 1, Pandemonium Fortress Level 2, and Stinging Winds.
- Blocked count areas include New Tristram, WhimsyDale, City of Caldeum, Gates of Caldeum, Caldeum Bazaar, Flooded Causeway, Ancient Waterway, and The Bridge Of Korsikk.
- Last Observation displays the most recent accepted goblin count after one exists. Suppressed, stale, duplicate, no-candidate, and area-change scans do not clear or replace that accepted-count display.
- Treasure/Odious and Gilded/Malevolent minimap ambiguity pairs use color diagnostics/overrides.
- Reset Stats clears count, GPH/active time, duplicate guard, auto-count evidence state, observation state, and Last Observation.
- New Game clears count, GPH/active time, per-game duplicate/evidence state, observation state, and Last Observation so the next game starts clean.

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

- Continue validating real auto-count during normal VS Debug use, with special attention to Level 2 area independence, stale journal/location transitions, and notification latency.
- Use VS Debug `Sim Count` when a rare count-policy edge case needs deterministic verification, especially PF1/PF2/Stinging Winds third-count suppression.
- Next Goblin Replay work should use the explicit harness command against real capture folders, specific metadata files/prefixes, DecisionBundle folders, or small `--goblin-replay-scenario` files from suspicious live sessions and only promote narrowly proven stale-location fixes into production code. Replay can now cover image-template area resolution plus count policy, but live runs are still needed for scanner timing, notification latency, and whether real in-game frames arrive before/after transitions.
- Latest replay validation used `--goblin-replay-metadata` against the `GoblinFarmer_Debug_20260607_175901.zip` Battlefields manual Capture and accepted EncounterCapture. Latest Sim Count validation used loose VS Debug logs from `GoblinFarmer_20260607_181728.log`; no new debug package was created for that run.
- Continue using automatic counting in real runs instead of focused specific-goblin hunts.
- Use the `Capture` button only when an image-recognition issue is visible and extra minimap/journal/fullscreen evidence would help.
- Keep `Docs/TODO.md` synchronized with remaining work and next test steps.
- Next validation should confirm both loose runtime storage and the next package are smaller while still containing replay-ready DecisionBundles, EncounterCapture Journal/Minimap crops, manifests, JSONL, and analysis files.

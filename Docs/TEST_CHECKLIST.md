# GoblinFarmer Test Checklist

Current active validation lives in `Docs/TODO.md`. This checklist is a compact smoke-test reference.

## Build And Packaging

- `dotnet build GoblinFarmer.csproj` succeeds.
- `dotnet run --project Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false` succeeds.
- Explicit Goblin Replay capture-folder command reports clear load/decision output when run with quoted capture folder paths.
- `git diff --check` has no whitespace errors.
- `Scripts\Create Debug Package.bat` creates a ZIP when explicit review artifacts are needed.
- New debug packages include `debug-package-size-summary.txt` with extension totals, top-folder totals, largest files, and applied retention limits.

## Goblin Tracker

- Observation Mode and Auto Goblin Count can be toggled from VS Debug Settings.
- VS Debug Settings shows `Capture`.
- Physical `X` does not increment Goblin Tracker.
- Automatic counts still create decision bundles and encounter captures automatically.
- The Capture button creates manual recognition files only when clicked.
- VS Debug can use ignored `Config\AppSettings.local.json` for private local paths/toggles while tracked `Config\AppSettings.json` stays sanitized.
- VS Debug/debug sessions write `Debug\GoblinEvidence\GoblinTrackerEvents.jsonl` when observations or automatic-count decisions occur.
- Logs include throttled `GoblinEvidenceTimingSummary` scan-stage timing output during active Goblin Evidence scanning.
- Test harness confirms saved fixture PNGs can reach the shared Goblin Evidence frame/template matching path without Diablo running, the explicit runner logs start/restoration while remaining on-demand only, multi-step replay suppresses stale Moon Clan Level 1 evidence in Level 2 plus Battlefields journal-history rows, and real-style encounter/manual capture folders can be loaded explicitly through the harness command for the same stale-location checks.
- Default areas count once per game.
- PF1, PF2, and Stinging Winds allow two counts per game and suppress the third.
- Blocked count areas suppress with `BlockedArea` and do not consume area slots.
- Reset Stats clears count, GPH, active time, duplicate guard, evidence state, and Last Observation.
- New Game clears per-game duplicate/evidence state.
- Cave Of The Moon Clan Level 1/2, Eastern Channel Level 1/2, Western Channel Level 1/2, and Caverns of Frost Level 1/2 remain separate where expected.
- Battlefields journal history does not replay stale/non-goblin rows into a fresh count.
- Journal logs include name-validation/history-row/history-input suppression when those protections fire.
- Suppressed/duplicate automatic-count paths do not show user-facing notifications, and an invalid automatic-count total logs `AutoCountNotificationSkipped reason=InvalidTotal`.
- Non-counting idle Journal evidence cannot repopulate Last Observation while combat is inactive; expected diagnostic is `LastObservationUpdateSkippedPublishGuard`.
- Treasure/Odious minimap template and color evidence must agree before minimap-only counting; mismatches should log `GoblinEvidenceMinimapColorDisagreement` and wait for safer evidence instead of showing the wrong goblin type.

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
- Runtime inventory replay remains log-first by default; debug package replay can load structured replay logs, while image replay requires explicit review crops/frames.
- VS Debug OBS auto-recording starts when a visible Diablo game window appears and stops when the visible Diablo window closes, even if a no-window Diablo process lingers until the debug form exits.

## Town Flow

- Repair diagnostics log `RepairButtonColorSample` before and after actionable repair clicks, including `visualState`, center/wide active-pixel metrics, and `RepairCompletionVerification`; inactive or uncertain debug-mode samples should save a focused `RepairButtonFocused_*.png` crop under `debug-screenshots\Repair`.
- Repair may soft-confirm a likely successful repair click when focused active pixels collapse after clicking even if residual edge pixels still look active, allowing salvage and auto-stash to continue.
- Unidentified legendary and set salvage templates are used when present; template-matched unidentified gear and low-color muted-top-edge unidentified legendary leftovers remain confirmation-expected salvage, not gems or detached footprints.
- Auto-stash only right-clicks stack-verified gem targets. Unidentified gear or non-gem leftovers should log `RejectedNonGemFootprint` or `RejectedGemStackVerification`.

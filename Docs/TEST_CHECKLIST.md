# GoblinFarmer Test Checklist

Current active validation lives in `Docs/TODO.md`. This checklist is a compact smoke-test reference.

## Build And Packaging

- `dotnet build GoblinFarmer.csproj` succeeds.
- `dotnet run --project Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false` succeeds.
- Explicit Goblin Replay capture-folder command reports clear load/decision output when run with quoted capture folder paths.
- `git diff --check` has no whitespace errors.
- `Scripts\Create Debug Package.bat` creates a ZIP when explicit review artifacts are needed.
- New debug packages include `debug-package-size-summary.txt` with extension totals, top-folder totals, largest files, and applied retention limits.
- Generic full-screen screenshot packaging is size-gated by default; package manifests report oversized selected screenshots without dropping replay-ready Journal/Minimap crops or JSONL evidence.

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
- Suppressed/duplicate automatic-count paths do not show user-facing notifications, and an invalid automatic-count total logs `AutoCountAcceptedDiscarded reason=InvalidTotal` before any accepted-count UI or splash is published, with `duplicateGuardReleased=True` when the just-consumed area slot is released.
- Auto-count notifications are single-slot ordered: queued/displayed/dropped events are mirrored into `GoblinTrackerEvents.jsonl`, superseded payloads log `SupersededByNewerNotification`, and over-age payloads log `StaleQueuedNotification`.
- Non-counting idle Journal evidence cannot repopulate Last Observation while combat is inactive; expected diagnostic is `LastObservationUpdateSkippedPublishGuard`.
- Accepted Last Observation persists across route/title/teleport area changes until the next accepted goblin or New Game/Reset Stats. Pending or duplicate Journal evidence must not replace it before a confirmed count publishes; area-change clears should log `LastObservationClearSkipped preserveKind=AcceptedCountPersistent`.
- Treasure/Odious minimap template and color evidence must agree before minimap-only counting; mismatches should log `GoblinEvidenceMinimapColorDisagreement` and wait for safer evidence instead of showing the wrong goblin type.
- Gelatinous Sire Journal Engaged/Killed templates use the targeted lower threshold from the 2026-06-11 PF2 diagnostics; other goblin journal templates keep the global threshold.
- Area-changed Journal Killed confirmations require a same-type Engaged anchor inside the short first-seen area-lock window; older cross-area killed rows should suppress with `KilledEvidenceStaleWithoutFreshEngagedAnchor` while same-area journal freshness remains unchanged.

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
- VS Debug startup has no external recorder dependency; live review videos come from NVIDIA/attached clips plus the shared Google Sheet notes.

## Town Flow

- Repair diagnostics log `RepairButtonColorSample` before and after actionable repair clicks, including `visualState`, center/wide active-pixel metrics, and `RepairCompletionVerification`; inactive or uncertain debug-mode samples should save a focused `RepairButtonFocused_*.png` crop under `debug-screenshots\Repair`.
- Repair may soft-confirm a likely successful repair click when focused active pixels collapse after clicking even if residual edge pixels still look active, allowing salvage and auto-stash to continue.
- Unidentified legendary and set salvage templates are used when present; template-matched unidentified gear and low-color muted-top-edge unidentified legendary leftovers remain confirmation-expected salvage, not gems or detached footprints.
- Auto-stash only right-clicks stack-verified gem targets. Unidentified gear or non-gem leftovers should log `RejectedNonGemFootprint` or `RejectedGemStackVerification`.

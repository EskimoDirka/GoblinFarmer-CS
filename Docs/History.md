# GoblinFarmer History

This file keeps backlog and historical context that no longer belongs in the short current `Docs/Project_Status.md`.

## v1.4 Goblin Tracker History

- Manual `X` counting was used to validate area resolution, duplicate suppression, block lists, PF1/PF2 two-count exceptions, Stinging Winds two-count behavior, Channel/Cavern/Moon Clan disambiguation, and reset/new-game cleanup.
- Automation Observation Mode was added first as diagnostic-only scanning from Journal and Minimap evidence.
- Observation Mode was then enabled by default while automatic counting remained gated behind a separate `GoblinTracker.EnableAutomaticCounting` setting.
- Automatic counting was introduced as an opt-in VS Debug path and hardened with:
  - fresh evidence requirements,
  - blocked-area priority,
  - duplicate/area-limit guards,
  - evidence-first-seen tracking,
  - cross-area journal-row suppression,
  - stale Engaged/Killed journal protection,
  - Treasure/Odious minimap color disambiguation,
  - Gilded/Malevolent minimap color diagnostics.
- VS Debug previously had a `Next Tests` tab and `Test Count Override` checkbox for focused validation. Those were removed when testing moved toward normal automatic-count usage. Remaining test steps now live in `Docs/TODO.md`.
- Physical `X` manual counting was retired once the workflow moved to automatic counting and a separate manual `Capture` button for image-recognition troubleshooting.

## Debug Workflow History

- Goblin Replay and derived evidence reprocessing were removed from the active workflow.
- `Scripts\Create Debug Package.bat` became the single ZIP package export path for VS Debug and Release.
- Form close was kept quiet and does not generate packages or loose review files.
- Tracked root/GitHub-upload EXE copies and retired local GitHub Sync / Exe Updater helper files were removed from the active workflow. Release artifacts now belong under generated `artifacts\` output and GitHub Releases, not source control.
- Tracked `Config\AppSettings.json` was returned to sanitized defaults; private VS Debug paths/toggles can live in ignored `Config\AppSettings.local.json`.
- `frmMain.GoblinEvidence.cs` capture helpers and `frmMain.SessionStats.cs` automatic-count helpers were split into dedicated partial files to keep active Goblin Tracker development safer.
- Goblin Evidence scanning added cached template catalogs, cached OpenCV template mats, one captured scan context per source pass, minimap-first scanning with Journal still preferred as the primary confirmation, throttled `GoblinEvidenceTimingSummary` stage histograms, and structured `GoblinTrackerEvents.jsonl` output.
- Debug package analysis helpers were added at the ZIP root:
  - `debug-package-analysis.txt`
  - `goblin-tracker-timeline.md`
  - `goblin-evidence-health.txt`
- VS Debug and release/debug-mode retention converged on 7 days.

## Notable Historical Debug Packages

- `GoblinFarmer_Debug_20260606_063031.zip`: showed Observation Mode disabled in Release, leading to default-on Observation Mode.
- `GoblinFarmer_Debug_20260606_064757.zip`: confirmed Release Observation Mode enabled and diagnostic-only.
- `GoblinFarmer_Debug_20260606_081149.zip`: showed stale evidence could count when Auto Count was toggled on, leading to evidence-first-seen arming protection.
- `GoblinFarmer_Debug_20260606_181752.zip`: confirmed duplicate/stale suppression and exposed notification type mismatch risk.
- `GoblinFarmer_Debug_20260606_203300.zip`: exposed Caverns Level 1 -> Level 2 journal reuse, leading to stricter area/row evidence handling.
- `GoblinFarmer_Debug_20260607_050158.zip`: confirmed PF2 two-count behavior and highlighted missed location-based auto-count cases.
- `GoblinFarmer_Debug_20260607_140848.zip`: confirmed stale Battlefields journal-history replay and no matching Cave Of The Moon Clan Blood Thief evidence in the package, leading to Journal name validation and history-row/input suppression.

## Stable Historical Work

- Battle.net launch diagnostics were hardened to avoid false failure screenshots when Diablo starts successfully.
- Start Game uses stable-button verification and manual-click recovery.
- Teleport routing keeps raw, normalized, display, and blocking locations separate.
- Interrupted teleport fail-safes preserve route/button state.
- Repair, salvage, and Kadala timing were optimized while preserving safety.
- Witch Doctor combat moved to mouse-wheel plus cursor-change left-click behavior instead of held mouse modes.

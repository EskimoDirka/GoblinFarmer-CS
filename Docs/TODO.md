# GoblinFarmer TODO

This file contains only open work and remaining test/verifications. Historical completed work belongs in `Docs/History.md`.

## Current Goblin Tracker Work

- [x] Phase 2B: build a small explicit replay fixture runner on top of the Phase 2A `IGoblinEvidenceFrameSource` seam. It consumes saved Journal/Minimap PNG fixtures on demand and does not auto-run in normal app startup, VS Debug startup, scanner start, combat, route, town, or debug package workflows.
- [x] Phase 2C: add multi-step replay scenarios for stale journal/location transitions using explicit saved fixture PNG inputs. Covered Moon Clan Level 1 evidence replaying into Level 2 and Battlefields journal-history rows.
- [x] Phase 2D: add optional fixture loading from real saved encounter/manual capture folders for broader replay coverage. It stays explicit/on-demand only, uses shared matcher/policy paths, and does not change production count behavior.
- [x] Phase 2E: add a small explicit developer harness command for selecting real capture folders without editing test code. It stays out of startup, scanner, route, combat, town, and debug package workflows.
- [x] Review `GoblinFarmer_Debug_20260607_175901.zip`: Southern Highlands and Eastern Channel Level 2 auto-counted correctly, stale Battlefields killed evidence suppressed after teleport, and the later Battlefields accepted count came from fresh engaged evidence.
- [x] Add VS Debug-only `Sim Count` controls for deterministic accepted-count, duplicate-suppression, and area-limit-suppression testing without changing live detection/counting behavior.
- [ ] Use the explicit Goblin Replay commands on real suspicious evidence when stale-location behavior needs a fast regression check without waiting for live goblin spawns. Prefer `--goblin-replay-metadata` or `--goblin-replay-prefix` for a specific older encounter in shared capture folders, `--goblin-replay-captures` for whole capture folders, and `--goblin-replay-decision-bundle` when starting from a DecisionBundle folder.
- [ ] Live-validate the `500ms` scanner interval improves notification latency without lowering evidence thresholds or increasing false positives.
- [ ] Live-validate Battlefields no longer auto-counts stale/non-goblin journal history after pressing Enter or opening journal history.
- [ ] Confirm logs show the new journal protections when applicable:
  - `GoblinEvidenceJournalNameValidationFailed`
  - `JournalNameValidationBelowThreshold`
  - `JournalCandidateIgnoredHistoryRow`
  - `GoblinJournalHistorySuppressionArmed`
  - `JournalCandidateIgnoredHistoryInput`
- [ ] Confirm `GoblinEvidenceTimingSummary` appears during normal VS Debug scans and shows useful scan-stage timing.
- [ ] Confirm `Debug\GoblinEvidence\GoblinTrackerEvents.jsonl` records observation, suppression, and automatic-count events alongside human logs.
- [ ] Live-validate Cave Of The Moon Clan Level 1 and Level 2 each auto-count independently in the same game when each level has fresh evidence from any goblin type.
- [ ] Live-validate Eastern Channel Level 2 fresh evidence from any goblin type auto-counts once and does not inherit stale evidence from another area.
- [ ] Live-validate Caverns of Frost Level 1 and Level 2 can each auto-count once only when the second level has evidence first seen after Level 2 is detected.
- [ ] Live-validate stale Last Observation entries do not reappear after route/location changes.
- [ ] Live-validate Reset Stats clears count, GPH, active time, duplicate guard, auto-count evidence state, observation state, and Last Observation.
- [ ] Live-validate New Game clears per-game Goblin Tracker duplicate/evidence state and allows fresh evidence to count again.
- [ ] Live-validate blocked count areas still suppress and notify without consuming area slots when evidence is visible.
- [ ] If encountered, live-validate PF1, PF2, and Stinging Winds still allow exactly two automatic counts and suppress the third with `AreaLimitReached`.
- [ ] In VS Debug, click through the new `Sim Count` area selector for Southern Highlands, PF1, PF2, Stinging Winds, and New Tristram to confirm accepted, duplicate, area-limit, and blocked notifications/logs are readable.
- [ ] If encountered, live-validate Gilded Baron and Malevolent Tormentor classification remains correct.

## VS Debug Capture Workflow

- [ ] Click `Capture` only when diagnosing image recognition; confirm it writes files under `Debug\GoblinEvidence\ManualCaptures`.
- [ ] Confirm automatic count workflows still create decision bundles and encounter captures without using the Capture button.
- [ ] Keep ignored `Config\AppSettings.local.json` for private VS Debug paths/toggles and keep tracked `Config\AppSettings.json` sanitized.

## Debug Package Workflow

- [ ] Generate a ZIP with `Scripts\Create Debug Package.bat` after a run with any suspicious Goblin Tracker behavior.
- [ ] Confirm packages still include logs, manifests, session info, decision bundles, encounter captures, observation diagnostics, and Goblin Evidence samples.
- [ ] Confirm VS Debug and release/debug-mode retention keeps artifacts for 7 days and does not keep stale troubleshooting files indefinitely.

## Repo Hygiene

- [ ] Keep generated EXEs, installer output, portable ZIPs, debug packages, logs, screenshots, source-upload output, and retired upload folders out of Git.
- [ ] Before each release, confirm `.gitignore` still protects user-specific paths and generated artifacts.

## Route And Workflow Monitoring

- [ ] Continue monitoring combat hotkey during `Waiting For Location Confirmation`; it should cancel the wait and start combat from the same press.
- [ ] Continue monitoring teleport route behavior around Ancient Waterway, Eastern/Western Channel Level 2, Stinging Winds, Battlefields, and Rakkis Crossing.
- [ ] Validate successful app-click Battle.net launches do not create false failure screenshots for `BattleNetManualPlaySuspected` or `BattleNetStillOpenAfterDiabloLaunch`.
- [ ] Validate Kadala timing/feel once blood shards are available.

## Future Improvements

- [ ] Consider a future minimap color-marker fallback if template recognition remains unreliable for small goblin icons.
- [ ] Consider a release-facing toggle or documentation once full automatic Goblin Tracker behavior has enough live-run confidence.

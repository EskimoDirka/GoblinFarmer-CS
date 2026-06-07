# GoblinFarmer TODO

This file contains only open work and remaining test/verifications. Historical completed work belongs in `Docs/History.md`.

## Current Goblin Tracker Work

- [ ] Live-validate the `500ms` scanner interval improves notification latency without lowering evidence thresholds or increasing false positives.
- [ ] Live-validate Battlefields no longer auto-counts stale/non-goblin journal history after pressing Enter or opening journal history.
- [ ] Confirm logs show the new journal protections when applicable:
  - `GoblinEvidenceJournalNameValidationFailed`
  - `JournalNameValidationBelowThreshold`
  - `JournalCandidateIgnoredHistoryRow`
  - `GoblinJournalHistorySuppressionArmed`
  - `JournalCandidateIgnoredHistoryInput`
- [ ] Live-validate Cave Of The Moon Clan Level 1 and Level 2 each auto-count independently in the same game when each level has fresh evidence from any goblin type.
- [ ] Live-validate Eastern Channel Level 2 fresh evidence from any goblin type auto-counts once and does not inherit stale evidence from another area.
- [ ] Live-validate Caverns of Frost Level 1 and Level 2 can each auto-count once only when the second level has evidence first seen after Level 2 is detected.
- [ ] Live-validate stale Last Observation entries do not reappear after route/location changes.
- [ ] Live-validate Reset Stats clears count, GPH, active time, duplicate guard, auto-count evidence state, observation state, and Last Observation.
- [ ] Live-validate New Game clears per-game Goblin Tracker duplicate/evidence state and allows fresh evidence to count again.
- [ ] Live-validate blocked count areas still suppress and notify without consuming area slots when evidence is visible.
- [ ] If encountered, live-validate PF1, PF2, and Stinging Winds still allow exactly two automatic counts and suppress the third with `AreaLimitReached`.
- [ ] If encountered, live-validate Gilded Baron and Malevolent Tormentor classification remains correct.

## VS Debug Capture Workflow

- [ ] Verify physical `X` no longer counts goblins.
- [ ] Verify VS Debug Settings shows `Capture` and no longer shows `Test Count Override`.
- [ ] Click `Capture` only when diagnosing image recognition; confirm it writes files under `Debug\GoblinEvidence\ManualCaptures`.
- [ ] Confirm automatic count workflows still create decision bundles and encounter captures without using the Capture button.

## Debug Package Workflow

- [ ] Generate a ZIP with `Scripts\Create Debug Package.bat` after a run with any suspicious Goblin Tracker behavior.
- [ ] Confirm packages no longer include `GoblinTrackerNextTests.txt` or `goblin-tracker-next-tests.txt`.
- [ ] Confirm packages still include logs, manifests, session info, decision bundles, encounter captures, observation diagnostics, and Goblin Evidence samples.
- [ ] Confirm VS Debug and release/debug-mode retention keeps artifacts for 7 days and does not keep stale troubleshooting files indefinitely.

## Route And Workflow Monitoring

- [ ] Continue monitoring combat hotkey during `Waiting For Location Confirmation`; it should cancel the wait and start combat from the same press.
- [ ] Continue monitoring teleport route behavior around Ancient Waterway, Eastern/Western Channel Level 2, Stinging Winds, Battlefields, and Rakkis Crossing.
- [ ] Validate successful app-click Battle.net launches do not create false failure screenshots for `BattleNetManualPlaySuspected` or `BattleNetStillOpenAfterDiabloLaunch`.
- [ ] Validate Kadala timing/feel once blood shards are available.

## Future Improvements

- [ ] Consider a future minimap color-marker fallback if template recognition remains unreliable for small goblin icons.
- [ ] Consider a release-facing toggle or documentation once full automatic Goblin Tracker behavior has enough live-run confidence.

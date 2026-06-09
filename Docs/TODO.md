# GoblinFarmer TODO

This file contains only open work and remaining test/verifications. Historical completed work belongs in `Docs/History.md`.

## Current Goblin Tracker Work

- [x] Phase 2B: build a small explicit replay fixture runner on top of the Phase 2A `IGoblinEvidenceFrameSource` seam. It consumes saved Journal/Minimap PNG fixtures on demand and does not auto-run in normal app startup, VS Debug startup, scanner start, combat, route, town, or debug package workflows.
- [x] Phase 2C: add multi-step replay scenarios for stale journal/location transitions using explicit saved fixture PNG inputs. Covered Moon Clan Level 1 evidence replaying into Level 2 and Battlefields journal-history rows.
- [x] Phase 2D: add optional fixture loading from real saved encounter/manual capture folders for broader replay coverage. It stays explicit/on-demand only, uses shared matcher/policy paths, and does not change production count behavior.
- [x] Phase 2E: add a small explicit developer harness command for selecting real capture folders without editing test code. It stays out of startup, scanner, route, combat, town, and debug package workflows.
- [x] Add an explicit Goblin Replay template-scenario command for small policy simulations from `Images\Goblin Evidence` templates. Current covered scenario: fresh Journal count before New Game, New Game reset action, then shifted old Journal row suppression as `JournalCandidateIgnoredResetCarryover`.
- [x] Add a current-location image resolver seam for Goblin Replay scenarios. `Location=...` steps can now resolve the count area from `Images\Current Location` title PNGs instead of requiring a manual `Area=...` override.
- [x] Review `GoblinFarmer_Debug_20260608_114121.zip`: Eastern Channel Level 1 Treasure Goblin counted correctly as Eastern Channel Level 1; Make New Game did not recount old visible Journal history; Stinging Winds missed because stale prior-area Treasure Journal state blocked a fresh Minimap hit; Rakkis Crossing recounted stale Odious Collector Journal text from Black Canyon Mines.
- [x] Fix/replay-cover the `GoblinFarmer_Debug_20260608_114121.zip` stale-row policy edge: cross-area stale Journal suppression no longer refreshes counted-encounter state, same-area duplicate Journal evidence refreshes counted-encounter state, and replay/policy tests cover stale Journal -> fresh Minimap and same visible Journal row after teleport.
- [x] Review `GoblinFarmer_Debug_20260608_120532.zip`: Stinging Winds/Rakkis did not spawn live validation goblins; Make New Game recounted a Northern Highlands Blood Thief killed Journal row in Southern Highlands because the visible row shifted from bucket 11 to bucket 8.
- [x] Fix/replay-cover the `GoblinFarmer_Debug_20260608_120532.zip` New Game row-shift carryover edge: reset-carryover matching now allows a four-bucket movement for the same visible goblin/template line family, and explicit Goblin Replay suppresses the package-shaped Blood Thief `11 -> 8` row as `JournalCandidateIgnoredResetCarryover`.
- [x] Review `GoblinFarmer_Debug_20260608_122007.zip`: Make New Game -> Southern Highlands did not recount the old visible previous-game Journal row. Logs show `JournalCandidateIgnoredResetCarryover` for the same killed Journal line moving from bucket 11 to bucket 8, with no stale count, notification, or Last Observation update after reset.
- [x] Review `GoblinFarmer_Debug_20260608_164403.zip`: Southern Highlands Gem Hoarder counted correctly, Last Observation persistence remained stable, Reset Stats cleared displayed state, and PF2 Rainbow Goblin counted/notified correctly.
- [x] Fix the `GoblinFarmer_Debug_20260608_164403.zip` Royal Crypts -> Sewers pause/row-shift stale recount. Counted-encounter Journal bucket matching now uses the shared four-bucket journal-line tolerance, and policy coverage includes the package-shaped Odious Collector `11 -> 8` shifted row.
- [x] Review `GoblinFarmer_Debug_20260608_173101.zip`: Weeping Hollow Treasure Goblin and Stinging Winds Gelatinous Sire counted correctly; Leoric's Passage Treasure Goblin was ignored as stale Weeping Hollow journal evidence; the later Gelatinous candidate was resolved in package metadata as `Battlefields`, not Black Canyon Mines, and was suppressed as the same visible encounter because `Gelatinous Spawn` and `Gelatinous Sire` journal rows shared type/bucket matching.
- [x] Fix/replay-cover the `GoblinFarmer_Debug_20260608_173101.zip` Gelatinous journal-family edge. Counted-encounter Journal bucket matching now requires the same journal line family/template kind, so `Gelatinous Spawn Killed Journal.png` is not suppressed as the same row as `Gelatinous Sire Killed Journal.png` merely because the normalized type and nearby row bucket match.
- [x] Review `GoblinFarmer_Debug_20260608_180056.zip`: Leoric's Passage Odious Collector counted correctly, Western Channel Level 1 Blood Thief counted correctly, stale Western Channel Blood Thief Journal variants incorrectly consumed both Stinging Winds count slots, and a previous-game Malevolent Tormentor Journal row counted in Southern Highlands shortly after Make New Game.
- [x] Fix/replay-cover the `GoblinFarmer_Debug_20260608_180056.zip` stale Journal variant edge. Stale killed/area-changed Journal rows now seed nearby same-goblin visible-line suppression, and explicit Goblin Replay covers Western Channel killed -> Stinging old killed -> Stinging old engaged partner suppressing as `JournalCandidateIgnoredStaleVisibleLine`.
- [x] Fix/replay-cover the `GoblinFarmer_Debug_20260608_180056.zip` late New Game carryover edge. `NewGameCreated` now arms a short Journal-only carryover window, and explicit Goblin Replay covers a late previous-game Malevolent Tormentor Journal row suppressing as `JournalCandidateIgnoredNewGameCarryoverWindow`.
- [x] Add the reliability gate for automatic counting: Journal Engaged-only evidence now reports `JournalPendingKilledOrMinimapConfirmation` and does not increment, while Minimap and fresh Journal Killed evidence remain eligible through the existing stale/duplicate/block/area-limit guards. Explicit Goblin Replay covers Engaged waiting for Killed confirmation.
- [x] Review/fix `GoblinFarmer_Debug_20260608_185449.zip`: Northern Highlands and Ruined Cistern counted from Journal Killed evidence, while Leoric's Hunting Grounds and Cathedral Level 1 were missed because only Engaged journal evidence was initially countable. Same-area active-combat Engaged evidence can now count after a short sustained-confirmation window, and fresh same-area Engaged can re-anchor a later Killed row that was stale in another area.
- [x] Review/fix `GoblinFarmer_Debug_20260608_191706.zip` plus recording: Leoric's Passage replay-ready decision bundles showed first Engaged evidence suppressing correctly, then live logs showed sustained Engaged reliability at about 2.4s still being suppressed by the earlier pending observation state. Pending Journal observations can now be promoted only by `JournalEngagedSustainedActiveCombat` and still pass through stale/block/duplicate/area-limit guards. Recording/logs showed Demon Hunter click-safe left-click suppression with right mouse held and recovery, not an unexplained combat stop.
- [x] Review/fix `GoblinFarmer_Debug_20260608_193932.zip` plus recording: Northern Highlands reproduced Demon Hunter blocked-cursor stalling around the 30-second mark. Logs showed combat active, right mouse held, and repeated left-click suppression in no-click UI regions. Demon Hunter now uses a verified safe playfield fallback point for blocked-cursor left-click pulses while preserving the no-click blacklist. The package also validated Eastern Channel Level 1 Blood Thief auto-counting with the correct area key.
- [x] Review/fix `GoblinFarmer_Debug_20260608_204337.zip`: Northern Highlands Treasure Goblin counted correctly, but notification appeared late because candidate selection chose pending Journal Engaged evidence over a strong same-scan Minimap candidate and waited for sustained active-combat confirmation. Pending Journal Engaged no longer masks strong Minimap confirmation, and `GoblinLatencyTrace` now logs evidence detected, count accepted, notification queued, and notification displayed timestamps.
- [x] Treat Stinging Winds -> Black Canyon Mines and Black Canyon Mines -> Rakkis Crossing stale-Journal transition policy as validated for now by explicit Goblin Replay. Reopen only if future live use shows a stale count in those transitions.
- [x] Treat Southern Highlands -> Cave Of The Moon Clan Level 1 stale-Journal transition policy as replay-validated. The explicit replay scenario covers fresh Minimap count -> suppressed same-encounter Journal row -> stale Journal row after area transition suppressing as `EncounterAlreadyAutoCounted`.
- [x] Treat Battlefields journal-history suppression as replay-validated. Explicit replay covers history-row evidence suppressing as `JournalCandidateIgnoredHistoryRow`, and policy coverage includes Battlefields -> Fields of Slaughter source-variant suppression after a fresh Battlefields count.
- [x] Treat Caverns/Cave Level 1 -> Level 2 stale freshness policy as replay/policy-validated. The freshness policy requires first-seen area to match the current level, and duplicate guard tests keep Level 1 and Level 2 as independent default-one-count areas.
- [x] Treat default area duplicate limits and PF1/PF2/Stinging Winds two-count limits as policy-validated by duplicate-guard tests and VS Debug Sim Count validation. Keep casual live monitoring, but do not force rare triple-spawn hunts unless a package contradicts the policy.
- [x] Treat blocked-area count suppression as policy/simulation-validated. New Tristram and other blocked areas should still be watched casually during normal use, but they are not a forced route-specific test item.
- [x] Review `GoblinFarmer_Debug_20260607_175901.zip`: Southern Highlands and Eastern Channel Level 2 auto-counted correctly, stale Battlefields killed evidence suppressed after teleport, and the later Battlefields accepted count came from fresh engaged evidence.
- [x] Add VS Debug-only `Sim Count` controls for deterministic accepted-count, duplicate-suppression, and area-limit-suppression testing without changing live detection/counting behavior.
- [x] Validate VS Debug `Sim Count` controls from loose runtime logs: Southern Highlands accepted once then duplicate-suppressed, PF1 and Stinging Winds accepted two then `AreaLimitReached`, New Tristram suppressed as `BlockedArea`, and Reset Stats cleared simulated count state.
- [ ] Use the explicit Goblin Replay commands on real suspicious evidence when stale-location behavior needs a fast regression check without waiting for live goblin spawns. Prefer `--goblin-replay-metadata` or `--goblin-replay-prefix` for a specific older encounter in shared capture folders, `--goblin-replay-captures` for whole capture folders, and `--goblin-replay-decision-bundle` when starting from a DecisionBundle folder.
- [ ] Add more small `--goblin-replay-scenario` files only when a scenario can be modeled from existing Goblin Evidence templates, current-location templates, and explicit area/reset steps. Useful next candidates: Reset Stats carryover, Level 1 -> Level 2 fresh-vs-stale, and PF1/PF2/Stinging Winds area-limit simulations with resolved current-location titles.
- [ ] Live-validate the `500ms` scanner interval improves notification latency without lowering evidence thresholds or increasing false positives.
- [x] Replay/policy-validate Battlefields -> Fields of Slaughter no longer auto-counts stale Battlefields journal text after a fresh Battlefields count; expected suppression reason is `EncounterAlreadyAutoCounted` with a source-variant or last-seen encounter match.
- [x] Post-fix validate Stinging Winds stale-policy path: explicit Goblin Replay confirms old prior-area Journal evidence does not block a fresh Stinging Winds Minimap count and does not recount later in Black Canyon Mines. Keep casual live monitoring during normal use.
- [x] Post-fix validate Rakkis Crossing stale-policy path: explicit Goblin Replay confirms a visible Journal row from a Black Canyon Mines count does not recount after teleporting to Rakkis Crossing. Keep casual live monitoring that real fresh Rakkis goblins count when naturally encountered.
- [x] Replay/policy-validate Battlefields no longer auto-counts stale/non-goblin journal history after pressing Enter or opening journal history; expected suppression includes `JournalCandidateIgnoredHistoryRow` or `JournalCandidateIgnoredHistoryInput` depending on input context.
- [ ] Confirm logs show the new journal protections when applicable:
  - `GoblinEvidenceJournalNameValidationFailed`
  - `JournalNameValidationBelowThreshold`
  - `JournalCandidateIgnoredHistoryRow`
  - `GoblinJournalHistorySuppressionArmed`
  - `JournalCandidateIgnoredHistoryInput`
- [ ] Confirm `GoblinEvidenceTimingSummary` appears during normal VS Debug scans and shows useful scan-stage timing.
- [ ] Confirm `Debug\GoblinEvidence\GoblinTrackerEvents.jsonl` records observation, suppression, and automatic-count events alongside human logs.
- [x] Replay-validate Southern Highlands -> Cave Of The Moon Clan Level 1 no longer replays a stale Southern Highlands Journal row as a Cave Level 1 count after a fresh Southern Highlands Minimap count.
- [x] Live-validate Cave Of The Moon Clan Level 1 and Level 2 each auto-count independently in the same game when each level has fresh evidence from any goblin type.
- [x] Live-validate Eastern Channel Level 2 fresh evidence from any goblin type auto-counts once and does not inherit stale evidence from another area.
- [x] Live-validate Eastern Channel Level 1 after the Journal/Minimap channel-context fix: Treasure Goblin counted as `Eastern Channel Level 1`, not `Pandemonium Fortress Level 1`, in `GoblinFarmer_Debug_20260608_114121.zip`.
- [x] Replay/policy-validate Caverns of Frost Level 1 and Level 2 can each auto-count once only when the second level has evidence first seen after Level 2 is detected.
- [ ] Live-validate notification latency after the strong-Minimap-over-pending-Journal fix. If Journal Engaged and strong Minimap are both present, expected logs include `GoblinEvidenceCandidateSelection reason=JournalPendingMinimapConfirmed`, `GoblinLatencyTrace stage=CountAccepted`, and `GoblinLatencyTrace stage=NotificationDisplayed` with low count-to-display time. If notifications still feel late, compare detection-to-count versus count-to-display in the latency trace before changing thresholds.
- [ ] Live-validate the reliability gate during normal play: first-seen Engaged-only Journal evidence should not count immediately; fresh Journal Killed or Minimap evidence should still count promptly and log `evidenceReliability=JournalKilledConfirmed` or `evidenceReliability=MinimapConfirmed`; sustained same-area active-combat Engaged evidence should count after the short confirmation window and log `evidenceReliability=JournalEngagedSustainedActiveCombat`.
- [x] Live-validate the notification splash is click-through and no longer blocks route/teleport mouse clicks.
- [ ] Live-validate Rainbow Goblin automatic counts show the distinct alert and play the local alert sound.
- [ ] Live-validate Last Observation keeps the latest accepted count across normal scans/area changes, then clears on Reset Stats, Make New Game, or app restart.
- [ ] Live-validate Reset Stats clears count, GPH, active time, duplicate guard, auto-count evidence state, observation state, and Last Observation.
- [ ] Live-validate Make New Game clears count, GPH, active time, duplicate guard, auto-count evidence state, observation state, and Last Observation, then allows fresh evidence to count again.
- [x] Post-fix live-validate Make New Game does not recount a previous-game visible Journal row after the reset, including rows that move up about three buckets; `GoblinFarmer_Debug_20260608_122007.zip` shows `JournalCandidateIgnoredResetCarryover` for bucket `11 -> 8` and no stale recount.
- [x] Policy/simulation-validate blocked count areas still suppress without consuming area slots when evidence is visible.
- [x] Policy/simulation-validate PF1, PF2, and Stinging Winds still allow exactly two counts and suppress the third with `AreaLimitReached`.
- [ ] If a future package contradicts the replay/policy-validated stale or area-limit behavior, reopen the smallest matching replay scenario and fix only the proven mismatch.
- [ ] If Western Channel Level 1 misses another fresh goblin, capture/review the package before changing freshness logic. The `20260608_164403` package shows the WCL1 Treasure Goblin Journal hit was treated as stale Northern Highlands evidence, followed by a fresh Western Channel Level 2 count.
- [ ] If Leoric's Passage misses another fresh Treasure Goblin after an older same-type visible Journal row, capture/review the package before changing stale-Engaged logic. The `20260608_173101` package shows the detected Treasure Goblin Journal row was first seen in The Weeping Hollow and suppressed in Leoric's Passage as old visible evidence.
- [ ] If Black Canyon Mines misses another fresh goblin, capture/review the package before changing thresholds. The `20260608_180056` current-session log did not contain a Black Canyon decision for the noted Malevolent Tormentor; it only showed below-threshold Malevolent checks during the Stinging Winds flow.
- [ ] Post-fix live-validate Demon Hunter blocked-cursor fallback: in Northern Highlands or another open route area, combat should keep visibly attacking when the cursor drifts over skill bar/HUD regions, and logs should show `DemonHunterBlockedCursorFallbackLeftClickSent` instead of only repeated `DemonHunterNoClickSuppressionActive`.
- [ ] If Demon Hunter combat actually stops without an explicit Escape/hotkey/teleport/focus/app-close event after the fallback fix, capture the package and investigate as an unexpected-stop bug instead of a no-click suppression case.
- [ ] If encountered, live-validate Gilded Baron and Malevolent Tormentor classification remains correct.

## VS Debug Capture Workflow

- [ ] Click `Capture` only when diagnosing image recognition; confirm it writes files under `Debug\GoblinEvidence\ManualCaptures`.
- [ ] Confirm automatic count workflows still create decision bundles and encounter captures without using the Capture button.
- [ ] Confirm new automatic DecisionBundles are replay-ready by default: each should include `decision-trace.txt`, `*_Metadata.txt`, `*_Journal.png`, and `*_Minimap.png`, with no full `evidence.png` unless explicitly opted in.
- [ ] Confirm normal scanner events log `GoblinEvidenceRootScreenshotSkipped` and no longer create root `GoblinEvidence_*` full/event images during automatic counting.
- [ ] Keep ignored `Config\AppSettings.local.json` for private VS Debug paths/toggles and keep tracked `Config\AppSettings.json` sanitized.

## Debug Package Workflow

- [ ] Generate a ZIP with `Scripts\Create Debug Package.bat` after a run with any suspicious Goblin Tracker behavior.
- [ ] Confirm packages still include logs, manifests, session info, decision bundles, encounter captures, observation diagnostics, and Goblin Evidence samples.
- [ ] Confirm package size is reduced by default: old DecisionBundle `evidence.*` full images and Encounter/ManualCapture `*_Fullscreen` images should be excluded, while Journal/Minimap crops and metadata remain included.
- [ ] Confirm loose runtime `Debug\GoblinEvidence` growth is reduced now that redundant root `GoblinEvidence_*` images are skipped by default.
- [ ] Confirm `debug-package-manifest.txt` reports included/excluded Goblin Evidence counts and excluded full-image sizes.
- [ ] Confirm VS Debug and release/debug-mode retention keeps artifacts for 7 days and does not keep stale troubleshooting files indefinitely.

## Repo Hygiene

- [ ] Keep generated EXEs, installer output, portable ZIPs, debug packages, logs, screenshots, source-upload output, and retired upload folders out of Git.
- [ ] Before each release, confirm `.gitignore` still protects user-specific paths and generated artifacts.

## Route And Workflow Monitoring

- [ ] Continue monitoring combat hotkey during `Waiting For Location Confirmation`; it should cancel the wait and start combat from the same press.
- [ ] Investigate Demon Hunter combat stopping/click-safe-region behavior in a focused combat pass. The `20260608_173101` package shows intentional combat-hotkey stop events plus click-safe suppression/recovery logs; do not mix this with Goblin Tracker policy changes unless a package proves tracker notifications are involved.
- [ ] Continue monitoring teleport route behavior around Ancient Waterway, Eastern/Western Channel Level 2, Stinging Winds, Battlefields, and Rakkis Crossing.
- [ ] Validate successful app-click Battle.net launches do not create false failure screenshots for `BattleNetManualPlaySuspected` or `BattleNetStillOpenAfterDiabloLaunch`.
- [ ] Validate Kadala timing/feel once blood shards are available.
- [ ] Review salvage item-to-item delay in a future town-workflow performance pass without weakening confirmation safety.

## Future Improvements

- [ ] Consider a future minimap color-marker fallback if template recognition remains unreliable for small goblin icons.
- [ ] Consider a release-facing toggle or documentation once full automatic Goblin Tracker behavior has enough live-run confidence.
- [ ] Future stash feature: consider auto-stashing gems and other configured item classes after Goblin Tracker reaches full automatic-count confidence.

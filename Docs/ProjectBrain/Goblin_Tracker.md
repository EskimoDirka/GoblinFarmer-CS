# Goblin Tracker

Source of truth: `Docs\Project_Status.md`. This file summarizes current Goblin Tracker behavior for quick context loading.

## Automatic Count Logic

- Automatic counting requires both `GoblinTracker.EnableObservationMode=true` and `GoblinTracker.EnableAutomaticCounting=true`.
- VS Debug exposes this as `Observation Mode` and `Auto Goblin Count`.
- Observation Mode alone can report candidates without changing GoblinCount, GPH, found records, or area slots.
- Eligible count-confirming evidence:
  - Fresh Minimap evidence after confidence checks.
  - Fresh Journal Killed evidence.
  - Same-area Journal Engaged evidence only after it remains visible during active combat for the sustained-confirmation window.
- First-seen Journal Engaged-only evidence is diagnostic/pending and reports `JournalPendingKilledOrMinimapConfirmation`.
- If pending Journal Engaged evidence and a strong Minimap confirmation appear in the same scan, Minimap can drive the immediate count and logs `GoblinEvidenceCandidateSelection reason=JournalPendingMinimapConfirmed`.

## Manual Count Behavior

- The physical `X` Goblin Tracker hotkey is retired.
- Goblin counts should come from automatic eligible evidence.
- VS Debug `Sim Count` is a developer-only count-policy simulator. It keeps `Current Area` first, alphabetizes the centralized `GoblinAreaResolver.KnownAreas` entries, and uses normal area resolution, blocked-area checks, duplicate guard, and area-limit logic.
- VS Debug `Capture` is a supplemental image-recognition aid only. It writes fullscreen, minimap, journal, and metadata files under `Debug\GoblinEvidence\ManualCaptures` when clicked.

## Countable And Blocked Areas

- Default area limit: one count per resolved area per game.
- Two-count exceptions: Pandemonium Fortress Level 1, Pandemonium Fortress Level 2, and Stinging Winds.
- PF1/PF2 can count a second real same-type goblin even when the Minimap evidence signature repeats, but only after the conservative 8-second PF duplicate-bypass threshold, while an area slot remains, and with fresh supporting Minimap or sustained Journal Engaged evidence.
- Blocked count areas include New Tristram, WhimsyDale, City of Caldeum, Gates of Caldeum, Caldeum Bazaar, Flooded Causeway, Ancient Waterway, and The Bridge Of Korsikk.
- Caverns of Frost Level 1 and Level 2 can each count independently when evidence is fresh for that level, even though Level 1 blocks Teleport Next.
- Cave Of The Moon Clan Level 1 and Level 2 can each count independently when evidence is fresh for that level.
- Sim Count coverage has been audited against every known area key. All countable known areas and blocked validation areas are present in the dropdown.

## Suppression Rules

- Counts use resolved area, route context, block list, duplicate guard, stale Journal protection, and area-limit rules.
- Automatic counts require fresh eligible evidence first seen after automatic counting was armed.
- Journal matches validate goblin-name text, ignore upper/history rows, and suppress candidates after journal-history input.
- Old visible Journal rows are prevented from recounting after route transitions, area changes, Reset Stats, or Make New Game.
- Same-area duplicate Journal evidence refreshes the remembered encounter row so visible text cannot become fresh after the next teleport.
- Immediate same-signature PF duplicates still suppress; PF1/PF2 third counts still suppress as `AreaLimitReached`.
- Cross-area stale Journal suppression does not block a later fresh Minimap hit in the new area.
- Minimap-only duplicate checks stay area-aware so tight icon signatures in different route areas do not suppress fresh counts.
- Recent same-goblin Minimap context helps Channel Level 1/2 Journal follow-up evidence avoid false Pandemonium Fortress area assignment.
- Reset Stats and Make New Game clear count/GPH/active time, duplicate guard, evidence state, observation state, and Last Observation.

## Current Test Status

- Replay/policy validation is strong for Make New Game carryover, Stinging Winds to Black Canyon Mines, Black Canyon Mines to Rakkis Crossing, Southern Highlands to Cave Of The Moon Clan, Cave/Caverns Level 1 to Level 2, Battlefields history-row suppression, same-area duplicate suppression, and default/two-count area limits.
- VS Debug Sim Count validated default duplicate suppression, PF1/PF2/Stinging Winds two-count limits, blocked-area suppression, Reset Stats clearing, and Eastern Channel Level 2 count simulation.
- Replay/policy validation covers PF2 first count, immediate same-signature duplicate suppression, delayed same-signature second count, and third PF count suppression. Policy tests also cover PF1 parity and non-PF unchanged duplicate behavior.
- Latest live validation passed for notification latency, blocked-area suppression, cached salvage scanning, debug package retention, and Sim Count area availability.
- Live packages validated several fresh-count and stale-suppression fixes, including Eastern Channel Level 1 area binding, Eastern Channel Level 2 coverage, and Northern Highlands count acceptance.

## Remaining Issues And Validation

- Live-validate reliability gate behavior during normal play: pending first Engaged, prompt Minimap/Killed count, and sustained Engaged count.
- Live-validate Rainbow Goblin distinct alert and local alert sound.
- Live-validate Last Observation clearing on Reset Stats, Make New Game, and app restart.
- Live-validate a real PF1/PF2 two-goblin encounter: first and second real goblins should count, and a third PF count should suppress.
- If future packages contradict replay/policy-validated stale behavior, reopen the smallest matching replay scenario before changing production logic.

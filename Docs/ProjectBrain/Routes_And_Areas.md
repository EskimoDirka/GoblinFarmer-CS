# Routes And Areas

Source of truth: `Docs\Project_Status.md`, especially the `Route Logic` and `Goblin Tracker Current Behavior` sections.

## Route Order

1. Southern Highlands.
2. Northern Highlands.
3. The Weeping Hollow.
4. The Festering Woods.
5. Cathedral.
6. Royal Crypts.
7. City Of Caldeum.
8. Ancient Waterway.
9. Stinging Winds.
10. Battlefields.
11. Rakkis Crossing.
12. Pandemonium Fortress Level 1.
13. Pandemonium Fortress Level 2.
14. Make New Game.

## Area Grouping And Transition Rules

- Southern Highlands can branch into Cave Of The Moon Clan.
- Northern Highlands leads to The Weeping Hollow, The Festering Woods, and Cathedral.
- Cathedral blocks Royal Crypts unless the area is Cathedral Level 3.
- City Of Caldeum blocks Ancient Waterway except Ruined Cistern.
- Ancient Waterway leads toward Stinging Winds, but plain Ancient Waterway blocks Teleport Next to Stinging Winds.
- Western and Eastern Channel levels are Ancient Waterway/channel subareas with special route handling.
- Stinging Winds blocks Battlefields except Black Canyon Mines.
- Battlefields leads toward Rakkis Crossing and has Caverns of Frost special handling.
- Rakkis Crossing leads to PF1, then PF2, then Make New Game.

## Countable Vs Blocked

- Normal countable areas allow one count per resolved area per game.
- Blocked count areas include New Tristram, WhimsyDale, City of Caldeum, Gates of Caldeum, Caldeum Bazaar, Flooded Causeway, Ancient Waterway, and The Bridge Of Korsikk.
- Blocked count suppression should not consume area slots.
- Teleport blocking and count blocking are related but not identical. Some areas can count while still blocking route advancement.
- VS Debug Sim Count includes every centralized known area key for countable-area and blocked-area validation. `Current Area` is pinned first; all explicit area keys are alphabetized.

## Special Cases

- Pandemonium Fortress Level 1 and Level 2 each allow two goblin counts per game. A third count is suppressed as `AreaLimitReached`.
- Stinging Winds allows two goblin counts per game. A third count is suppressed as `AreaLimitReached`.
- Cave Of The Moon Clan Level 1 blocks Teleport Next, but Level 1 and Level 2 can each count independently when each level has fresh evidence.
- Caverns of Frost Level 1 blocks Teleport Next, but Level 1 and Level 2 can each count independently when each level has fresh evidence.
- Western Channel Level 1 blocks route advancement; Western Channel Level 2 returns to Ancient Waterway.
- Eastern Channel Level 1 blocks route advancement; Eastern Channel Level 2 allows Stinging Winds.
- Channel Level 1/2 evidence can be confused with Pandemonium Fortress title templates, so recent Minimap context helps keep Journal follow-up evidence bound to the Channel area.
- WhimsyDale is a blocked count area and should not consume count slots.

## Route-Specific Test Ordering

When listing Goblin Tracker next test steps, follow the official route order first, then list general reset, stale evidence, display, classification, package, and documentation checks.

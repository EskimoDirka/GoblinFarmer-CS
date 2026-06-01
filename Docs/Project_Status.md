# GoblinFarmer Project Status

## Current Focus
Teleport route blocking and location normalization now follow the official route source of truth below.

## Official Route Logic
- Southern Highlands: next Northern Highlands; no block.
- Northern Highlands: next The Weeping Hollow; no block.
- The Weeping Hollow: next The Festering Woods; no block.
- The Festering Woods: next Cathedral; no block.
- Cathedral: next Royal Crypts; block all Cathedral locations except Cathedral Level 3.
- Royal Crypts: next City Of Caldeum; no block.
- City Of Caldeum: next Ancient Waterway; block all City Of Caldeum sublocations except Ruined Cistern.
- Ancient Waterway: next Stinging Winds; Western Channel Level 1 blocks; Western Channel Level 2 allows teleport back to Ancient Waterway; Eastern Channel Level 1 blocks; Eastern Channel Level 2 allows Stinging Winds; manual Ancient Waterway button click while already inside Ancient Waterway blocks.
- Stinging Winds: next Battlefields; Stinging Winds blocks; Black Canyon Mines allows Battlefields.
- Battlefields: next Rakkis Crossing; no block.
- Rakkis Crossing: next Pandemonium Fortress Level 1; no block.
- Pandemonium Fortress Level 1: next Pandemonium Fortress Level 2; no block.
- Pandemonium Fortress Level 2: next Make New Game flow; no block.

## Last Known Good
- Images moved into project and pushed to GitHub.
- Battle.net can relaunch/focus if process exists but no visible window exists.
- Diablo launch grace period prevents false cancellation.
- Start Game verified successfully.
- Make New Game flow created 1 game and completed first teleport to Southern Highlands.
- Route state now preserves the previous confirmed location when teleport confirmation fails or is blocked.
- Teleport blocking now blocks only exact intended blocked locations instead of blocking normal route locations.
- Leoric's Passage is detected as unavailable as a waypoint because it is not present in `Images\Teleport Function\Map X Y Coordinates.txt`; Northern Highlands falls back to the configured route.
- Gates of Caldeum now normalizes to City Of Caldeum for blocking output.
- Waterway sub-regions now keep their raw identity for blocking decisions; Western Channel Level 1 and Eastern Channel Level 1 block, Western Channel Level 2 returns to Ancient Waterway, and Eastern Channel Level 2 continues to Stinging Winds.
- Stinging Winds blocks the Battlefields teleport unless the current detected sub-region is Black Canyon Mines.
- Waterway button state keeps the Waterway button current/green while selecting the next intended target instead of clearing orange next state.
- Interrupted teleport retry behavior remains preserved.
- Blocking rules are now target-specific instead of using a generic blocked-location list.
- Cathedral blocks Royal Crypts unless the raw detected location is Cathedral Level 3.
- City Of Caldeum blocks Ancient Waterway unless the raw detected location is Ruined Cistern.
- Western Channel Level 2 now selects Ancient Waterway as the next target; Eastern Channel Level 2 selects Stinging Winds.
- Manual Ancient Waterway button clicks are blocked when the raw detected location is already Ancient Waterway.

## Active Issues
- Battle.net scan regions were captured fullscreen and may fail when Battle.net is windowed.
- Need to test full teleport route from Southern Highlands through Northern Highlands and onward.
- Need to test interrupted teleport recovery.
- Need to test Exit Game workflow.
- Need to verify repair + salvage flow.
- Need to validate publish/release folder includes Images.
- Need waypoint coordinates before routing Northern Highlands directly to Leoric's Passage.
- Start Game button is still buggy, suspected cursor interference with image recognition.

## Next Test
Run the full teleport-next route manually and verify:
- Cathedral Level 1/2 block Royal Crypts; Cathedral Level 3 allows Royal Crypts.
- City Of Caldeum/Gates/Caldeum Bazaar/Sewers/Flooded Causeway block Ancient Waterway; Ruined Cistern allows Ancient Waterway.
- Western Channel Level 1 blocks; Western Channel Level 2 selects/allows Ancient Waterway.
- Eastern Channel Level 1 blocks; Eastern Channel Level 2 selects/allows Stinging Winds.
- Manual Ancient Waterway button click while already inside Ancient Waterway blocks without changing route colors.
- Stinging Winds blocks Battlefields; Black Canyon Mines allows Battlefields.
- Interrupted teleport retry still preserves confirmed/current/next button state and colors.

Next recommended task: isolate Start Game button cursor/image-recognition interference without changing route logic.

## Last Validation
- Built `GoblinFarmer.csproj` successfully.
- Confirmed `Map X Y Coordinates.txt` has Southern Highlands and Northern Highlands but no Leoric's Passage coordinate.
- Static route review confirmed Make New Game and Exit Game use `bypassFailsafe: true`.
- Static route review confirmed teleport-next hotkey uses blocking checks with `ignoreBlocking: false`.
- Built after route blocking changes with 0 warnings and 0 errors.
- Static review confirmed Battle.net launch flow, combat logic, and repair/salvage logic were not changed for the route-blocking fix.
- Built after official route-source update; Battle.net launch flow, combat logic, and repair/salvage logic were not changed.

## Backlog
- Make Battle.net scan regions relative to Battle.net window.
- Clean up nested GoblinFarmer folder structure later.
- Review right-click behavior after Battle.net Play.
- Improve Start Game diagnostics if it fails again.

## Important Paths
Project:
D:\D3\Projects\GoblinFarmer\GoblinFarmer\GoblinFarmer

Runtime Images:
bin\Debug\net10.0-windows\Images

Release Target:
D:\GoblinFarmer

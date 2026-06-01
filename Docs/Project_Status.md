# GoblinFarmer Project Status

## Current Focus
Diagnostic overlay for live runtime state during manual testing.

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
- City Of Caldeum blocking works correctly; Gates of Caldeum displays as City Of Caldeum.
- Western Channel Level 1 blocks correctly; Western Channel Level 2 does not block incorrectly.
- Eastern Channel Level 1 blocks correctly; Eastern Channel Level 2 teleports to Stinging Winds correctly.
- Stinging Winds blocks correctly; Black Canyon Mines teleports to Battlefields correctly.
- Interrupted teleport fail-safes preserve route and button state.
- Manual teleport buttons now preserve failed/interrupted intended targets as retry state. Clicking the same intended button again uses preserved route state while still bypassing teleport blocking like the original manual button request.
- Manual same-button clicks while a teleport is already waiting for arrival confirmation are ignored and logged to avoid overlapping waypoint workflows.
- In-game notifications now use a no-activate overlay so blocked/already-here messages should not steal Diablo focus.
- Ancient Waterway self-click now blocks before opening the map and preserves current/next button state.
- Repair flow now waits for New Tristram/vendor readiness and logs repair-station click timing before using the repair-station coordinate fallback.

## Active Issues
- Battle.net scan regions were captured fullscreen and may fail when Battle.net is windowed.
- Need to test full teleport route from Southern Highlands through Northern Highlands and onward.
- Need to test interrupted teleport recovery.
- Need to manually confirm repeated failed/interrupted button retry from Cathedral Level 1 to Royal Crypts preserves current=Cathedral, next/retry=Royal Crypts, bypasses manual-button blocking, and does not advance until arrival is confirmed.
- Need to test Exit Game workflow.
- Need to verify repair + salvage flow.
- Need to validate publish/release folder includes Images.
- Need waypoint coordinates before routing Northern Highlands directly to Leoric's Passage.
- Start Game button detection/click verification is still inconsistent, suspected cursor interference with image recognition.
- Battle.net can launch windowed/not full-window; eventually maximize/focus Battle.net after launching.
- Battle.net scan regions are still fullscreen-position dependent and should become Battle.net-window-relative.

## Debug Package Generator
- Added `Scripts\create-debug-package.ps1`.
- Run from the project root with: `powershell -ExecutionPolicy Bypass -File .\Scripts\create-debug-package.ps1`.
- Creates `DebugPackages\GoblinFarmer_Debug_YYYYMMDD_HHMM.zip`.
- Includes the latest app log if found, up to 10 latest debug screenshots if found, `AGENTS.md`, `Docs\Project_Status.md`, `Docs\TEST_CHECKLIST.md`, `Docs\TODO.md`, `git-status.txt`, and `git-log.txt`.
- The script warns for missing optional folders/files and collects explicit files only, so `bin`, `obj`, and large build artifacts are not packaged.

## Diagnostic Overlay
- Added a read-only WinForms `Diagnostic Overlay` panel on the main form.
- Refreshes from the existing status timer with no new image-recognition scans.
- Shows raw location, normalized location, display location, blocking location, current teleport target, next teleport target, route state, combat state, failure counter, Diablo running status, active workflow, last log file, screenshot count, log count, and latest debug package path if one exists.
- Also shows queued retry target, last requested target, and failed/interrupted retry state for manual teleport retry diagnostics.
- Uses existing runtime state and file counts only; route, combat, Battle.net, repair, and salvage logic were not changed.

## Next Test
Run a focused regression pass:
- Trigger blocked/already-here notifications and confirm Diablo keeps focus.
- Click Ancient Waterway while exactly inside Ancient Waterway and confirm no map open and no button-state change.
- Confirm Western Channel Level 2 selects/allows Ancient Waterway, while Eastern Channel Level 2 selects/allows Stinging Winds.
- Run Make New Game through New Tristram repair and confirm repair-station click timing is stable.
- Re-test interrupted teleport retry and confirm current/next button colors remain preserved.
- Re-test manual Royal Crypts button retry after an interrupted button teleport from Cathedral Level 1 and confirm the retry uses preserved state without applying hotkey blocking checks.

Next recommended task: isolate Start Game button cursor/image-recognition interference, then make Battle.net scan regions window-relative.

## Last Validation
- Built `GoblinFarmer.csproj` successfully.
- Confirmed `Map X Y Coordinates.txt` has Southern Highlands and Northern Highlands but no Leoric's Passage coordinate.
- Static route review confirmed Make New Game and Exit Game use `bypassFailsafe: true`.
- Static route review confirmed teleport-next hotkey uses blocking checks with `ignoreBlocking: false`.
- Built after route blocking changes with 0 warnings and 0 errors.
- Static review confirmed Battle.net launch flow, combat logic, and repair/salvage logic were not changed for the route-blocking fix.
- Built after official route-source update; Battle.net launch flow, combat logic, and repair/salvage logic were not changed.
- Built after post-route-test fixes; build succeeded with the pre-existing `portMonkKeyIndex` unused-field warning.
- Static review confirmed combat logic and Battle.net Play flow were not changed.
- Added and ran the debug package generator; it created a timestamped package with docs, latest runtime log, latest screenshots, and git status/log snapshots.
- Built after adding the debug package generator with 0 warnings and 0 errors.
- Built after adding the diagnostic overlay with 0 warnings and 0 errors.
- Static review confirmed the diagnostic overlay reads existing state only and does not change route, combat, Battle.net, repair, or salvage logic.
- Built after manual button retry fix; final build succeeded with 0 warnings and 0 errors.
- Static review confirmed manual button retry state is scoped to teleport button handling/diagnostics and does not change combat logic, Battle.net flow, repair, or salvage logic.
- Built after ButtonRetry manual-blocking fix; build succeeded with 0 warnings and 0 errors.
- Static review confirmed only the manual button retry/blocking path and same-button confirmation guard changed; hotkey blocking behavior, combat, Battle.net, Start Game, repair, salvage, normalization, and focus-safe notifications were not changed.

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

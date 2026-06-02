# GoblinFarmer Project Status

## Current Focus
Post-exit input cleanup safety for Exit Game workflow shutdown.

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
- Battle.net Diablo tab and Play button image searches now treat cached scan regions as Battle.net-window-local pixel offsets, add the current Battle.net window left/top, and retain full-screen search as fallback.
- Runtime input cleanup now releases only tracked held left/right/Shift inputs while Diablo is available, and clears held-input state without sending mouse events after Diablo closes.

## Active Issues
- Need to manually validate Battle.net tab/Play button detection with Battle.net fullscreen, windowed, moved, and on another monitor.
- Need to test full teleport route from Southern Highlands through Northern Highlands and onward.
- Need to test interrupted teleport recovery.
- Need to manually confirm repeated failed/interrupted button retry from Cathedral Level 1 to Royal Crypts preserves current=Cathedral, next/retry=Royal Crypts, bypasses manual-button blocking, and does not advance until arrival is confirmed.
- Need to test Exit Game workflow.
- Need to manually validate Exit Game workflow no longer produces a desktop right-click or closes the app after Diablo exits.
- Need to verify repair + salvage flow.
- Need to validate publish/release folder includes Images.
- Need waypoint coordinates before routing Northern Highlands directly to Leoric's Passage.
- Start Game button detection/click verification is still inconsistent, suspected cursor interference with image recognition.
- Battle.net can launch windowed/not full-window; eventually maximize/focus Battle.net after launching.
- Need runtime log validation that `BattleNetD3Tab` and `BattleNetPlayButton` cached regions resolve by adding the Battle.net window origin, not by scaling fullscreen reference coordinates.

## Debug Package Generator
- Added `Scripts\create-debug-package.ps1`.
- Run from the project root with: `powershell -ExecutionPolicy Bypass -File .\Scripts\create-debug-package.ps1`.
- Creates `DebugPackages\GoblinFarmer_Debug_YYYYMMDD_HHMMSS.zip`.
- Includes the latest app log if found, latest failure screenshots, latest normal debug screenshots if found, `AGENTS.md`, `Docs\Project_Status.md`, `Docs\TEST_CHECKLIST.md`, `Docs\TODO.md`, `git-status.txt`, `git-log.txt`, and `debug-package-manifest.txt`.
- The manifest records the package path, latest log path, latest failure screenshot type, latest failure screenshot path, screenshot counts, and explicit build-artifact exclusions.
- The console summary clearly reports package path, latest log, failure screenshot count, normal screenshot count, latest failure type, git capture status, and manifest name.
- The script warns for missing optional folders/files and collects explicit files only, so `bin`, `obj`, and large build artifacts are not packaged.

## Battle.net Window-Relative Scan Regions
- Battle.net image searches now restore/focus the visible Battle.net window, read its current window rectangle, and resolve cached scan regions by adding `window.Left/window.Top` to cached region `Left/Top`.
- Applies to Diablo III tab detection (`BattleNetD3Tab`) and Battle.net Play button detection (`BattleNetPlayButton`).
- Logs cached region, Battle.net window rect, resolved screen scan region, found/not-found result in the window-relative region, warning when a cached region is outside the Battle.net window, and final full-screen fallback search result.
- Preserves the existing scan-region cache format and keeps full-screen image search as fallback.
- Static review confirmed this changes only Battle.net scan-region resolution; Diablo in-game Start Game logic, teleport logic, combat logic, repair/salvage logic, debug package generator, and diagnostics UI were not changed.

## Runtime Input Cleanup
- `ForceReleaseAllRuntimeInputs` now logs the cleanup reason, tracked held left/right/Shift state before cleanup, Diablo window handle, and Diablo rect availability.
- Cleanup sends `LEFTUP`, `RIGHTUP`, or Shift key-up only when runtime state says that input is currently held and the Diablo window rectangle is available.
- When Diablo is unavailable, cleanup clears tracked held-input state without generating mouse or key events, preventing post-exit desktop right-click behavior.
- Duplicate cleanup calls are idempotent: later cleanup calls log skipped releases when no tracked input remains held.
- Combat and hotkey runtime click paths now update tracked held-input state, including Demon Hunter right-hold cleanup, Shift-left clicks, loot-click pulses, Kadala right-click pulses, and automation safe clicks.
- Static review confirmed this does not change Battle.net logic, teleport logic, repair/salvage logic, or repair-station coordinate clicks.

## Diagnostic Overlay
- Added a read-only WinForms `Diagnostic Overlay` panel on the main form.
- Refreshes from the existing status timer with no new image-recognition scans.
- Shows raw location, normalized location, display location, blocking location, current teleport target, next teleport target, route state, combat state, failure counter, Diablo running status, active workflow, last log file, screenshot count, log count, and latest debug package path if one exists.
- Also shows queued retry target, last requested target, and failed/interrupted retry state for manual teleport retry diagnostics.
- Uses existing runtime state and file counts only; route, combat, Battle.net, repair, and salvage logic were not changed.

## Route State Inspector
- Added a read-only `Route State` diagnostics tab beside the compact overlay.
- Refreshes from the existing status timer and cached diagnostic/file state; it does not run extra image-recognition scans or activate Diablo.
- Shows raw detected location, normalized app location, display location, blocking location, current/next button locations, queued teleport target, retry queued target, last requested teleport target, last teleport source, last blocking decision and reason, last route decision output, arrival-confirmation wait state and target, failed/interrupted retry state, failure counter, latest log path, latest debug screenshot path, screenshot/log counts, active workflow, Diablo running status, and Diablo focused/active status.
- Added small read-only state fields for last teleport source, last blocking decision/reason, last route output, and latest screenshot path.
- Static review confirmed this is diagnostics-only; route logic, combat logic, Battle.net logic, Start Game flow, repair/salvage logic, City of Caldeum/Gates normalization, and focus-safe notifications were not changed.

## Screenshot-On-Failure Expansion
- Added `PortCaptureFailureScreenshot` on top of the existing debug screenshot infrastructure.
- Failure screenshots now record the latest failure screenshot type and log the saved path when capture succeeds.
- Screenshot capture still catches/logs failures and does not throw back into workflows.
- Capture now falls back to the virtual screen when a Diablo client capture is unavailable, so Battle.net/startup failures can still produce evidence.
- Added failure screenshot coverage for TeleportBlocked, TeleportInterrupted, TeleportConfirmationTimeout, StartGameButtonNotFound, StartGameVerificationFailed, BattleNetPlayButtonNotFound, DiabloTabNotFound, RepairStationNotFound, RepairFailed, WorkflowCancelled, and UnexpectedException.
- Compact Diagnostic Overlay and Route State Inspector now show latest screenshot path and latest failure screenshot type.
- Static review confirmed this is diagnostics-only; route logic, combat logic, Battle.net behavior, repair/salvage behavior, and teleport behavior were not changed.

## Next Test
Run a focused regression pass:
- Trigger blocked/already-here notifications and confirm Diablo keeps focus.
- Click Ancient Waterway while exactly inside Ancient Waterway and confirm no map open and no button-state change.
- Confirm Western Channel Level 2 selects/allows Ancient Waterway, while Eastern Channel Level 2 selects/allows Stinging Winds.
- Run Make New Game through New Tristram repair and confirm repair-station click timing is stable.
- Re-test interrupted teleport retry and confirm current/next button colors remain preserved.
- Re-test manual Royal Crypts button retry after an interrupted button teleport from Cathedral Level 1 and confirm the retry uses preserved state without applying hotkey blocking checks.
- Open the Route State tab during retry testing and confirm source, blocking decision, retry target, waiting-confirmation state, latest log, and latest screenshot fields update as expected.
- Trigger one safe failure path in a controlled manual run and confirm latest screenshot path plus latest failure screenshot type update in both diagnostics views.
- Generate a debug package after a fresh manual failure and confirm the newest failure screenshot type is reflected in `debug-package-manifest.txt`.
- Test Battle.net launch flow with Battle.net fullscreen, windowed, moved, and on another monitor; confirm logs show window-relative scan regions before fallback.
- Confirm logs for `BattleNetD3Tab=120,76,81,76` and `BattleNetPlayButton=1200,1070,156,72` show screen regions calculated as Battle.net window origin plus cached offsets.
- Run Exit Game, confirm logs show `ForceReleaseAllRuntimeInputs` with `diabloWindow=0x0`/`diabloRect=unavailable`, skipped right release, cleared held state, and no desktop right-click or app close after Diablo exits.

Next recommended task: manually validate the Exit Game cleanup fix, then isolate Start Game button cursor/image-recognition interference.

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
- Built after adding the Route State Inspector; build succeeded with 0 warnings and 0 errors.
- Static review confirmed the Route State Inspector uses existing timer/state and passive diagnostic fields only, with no gameplay-flow changes.
- Built after Screenshot-On-Failure expansion; build succeeded with 0 warnings and 0 errors.
- Built after post-exit input cleanup fix; build succeeded with 0 warnings and 0 errors.
- Static review confirmed cleanup changes are limited to tracked runtime input release behavior and do not change Battle.net, teleport, repair/salvage, or repair-station coordinate click logic.
- Static review confirmed screenshot additions are failure-diagnostics only and do not alter route, combat, Battle.net, repair/salvage, or teleport decisions.
- Polished and ran `Scripts\create-debug-package.ps1`; it created `DebugPackages\GoblinFarmer_Debug_20260601_184528.zip`.
- Inspected the generated package: included `AGENTS.md`, `Docs\Project_Status.md`, `Docs\TODO.md`, `Docs\TEST_CHECKLIST.md`, latest log, 10 failure screenshots, 10 normal debug screenshots, `git-status.txt`, `git-log.txt`, and `debug-package-manifest.txt`.
- Verified the generated package has no `bin/` or `obj/` entries; manifest reported latest failure screenshot type `TeleportBlocked`.
- Built after Debug Package Generator polish; build succeeded with 0 warnings and 0 errors.
- Built after Battle.net window-relative scan-region change; build succeeded with 0 warnings and 0 errors.
- Static review confirmed only Battle.net tab/Play button scan-region resolution changed; gameplay route, teleport, combat, Start Game, repair/salvage, debug package, and diagnostics UI logic were not changed.
- Built after correcting Battle.net cached-region interpretation to window-local pixel offsets; build succeeded with 0 warnings and 0 errors.
- Static review confirmed cached Battle.net regions are no longer scaled as fullscreen reference regions; invalid/outside-window cached regions warn and fall back to full-screen search.

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

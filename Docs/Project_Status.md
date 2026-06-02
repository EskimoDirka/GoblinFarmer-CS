# GoblinFarmer Project Status

This file is the source of truth for current route logic, stable behavior, active work, known issues, recent fixes, and the next recommended task.

## Current Focus
Combat mouse input behavior over no-click regions.

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

## Known Stable Systems
- Images are project-relative and copied into the build output.
- Battle.net can relaunch/focus if the process exists but no visible window exists.
- Diablo launch grace period prevents false cancellation while Diablo starts.
- Start Game has verified successfully in prior runs, though reliability work remains open.
- Make New Game flow has created a game and completed the first teleport to Southern Highlands in prior validation.
- Teleport routing preserves raw detected location, normalized app location, display location, and blocking location as separate concepts.
- Route state preserves the previous confirmed location when teleport confirmation fails or is blocked.
- Interrupted teleport fail-safes preserve route and button state.
- Manual teleport buttons preserve failed/interrupted intended targets as retry state.
- Manual button retries bypass teleport blocking like the original manual button request.
- Manual same-button clicks while a teleport is waiting for arrival confirmation are ignored and logged to avoid overlapping waypoint workflows.
- In-game notifications use a no-activate overlay so blocked/already-here messages should not steal Diablo focus.
- Gates of Caldeum normalizes to City Of Caldeum for blocking output.
- Waterway sub-regions keep their raw identity for blocking decisions.
- Cathedral blocks Royal Crypts unless the raw detected location is Cathedral Level 3.
- City Of Caldeum blocks Ancient Waterway unless the raw detected location is Ruined Cistern.
- Western Channel Level 2 selects Ancient Waterway as the next target; Eastern Channel Level 2 selects Stinging Winds.
- Stinging Winds blocks Battlefields unless the current detected sub-region is Black Canyon Mines.
- Ancient Waterway self-click blocks before opening the map and preserves current/next button state.
- Repair flow waits for New Tristram/vendor readiness and logs repair-station click timing before using the repair-station coordinate fallback.
- Combat automation is stable enough for current route work; do not change combat logic unless explicitly requested.
- Combat click safety blocks existing UI no-click regions and now includes the extended lower-right hover menu area without moving the cursor or stopping combat.
- Combat no-click suppression keeps non-mouse combat actions running where safe; Demon Hunter key rotation continues while mouse clicks are suppressed over UI regions.
- Combat keyboard-hook filtering matches the old Python app's injected-key behavior for combat-relevant number keys: physical `1`/`2` are suppressed during combat, while injected automation key events pass through.
- Demon Hunter right mouse now follows the old Python app's pattern: start holding right mouse only in a safe region, then keep the hold active through hover/no-click regions without sending new click events.
- Demon Hunter sustained combat now treats shared cursor-loop left-click suppression as active right-held combat when right mouse is already held from a safe region.
- Battle.net tab and Play button image searches use Battle.net-window-local cached scan regions plus the current Battle.net window left/top, with full-screen search as fallback.
- Runtime input cleanup releases only tracked held left/right/Shift inputs while Diablo is available and clears tracked state without mouse events after Diablo closes.
- Diagnostic Overlay, Route State Inspector, Screenshot-On-Failure, and Debug Package Generator are implemented.
- Exit Game workflow no longer generates desktop right-clicks after Diablo exits.
- Exit Game workflow no longer closes GoblinFarmer after Diablo exits.
- Battle.net Play button window-relative scan region has dedicated fallback comparison diagnostics; current region remains `30,853,292,75` pending runtime validation.

## Under Active Improvement
- Combat hover-menu no-click validation using `ExtendedRightMenuNoClickRegion`.
- Combat mouse input behavior comparison against the old Python app.
- BattleNetPlayButton region accuracy validation using fallback point comparison.
- Battle.net launch speed optimization (skip Diablo tab selection when Play button is already visible).
- Battle.net tab/Play detection across fullscreen, windowed, moved-window, and multi-monitor setups.
- Start Game image recognition reliability, especially possible cursor interference with detection/click verification.
- Repair/salvage timing validation after route and launch changes.
- Full route validation from Southern Highlands through Pandemonium Fortress Level 2.
- Publish/release folder validation to confirm Images are included.

## Known Issues
- Need manual combat validation that hovering over the extended lower-right menu blocks combat clicks with `blockReason=ExtendedRightMenuNoClickRegion`, while combat continues and normal clicks resume after moving away.
- Need manual validation that Demon Hunter right-hold continues through no-click hover regions without clicking UI, while left/Shift-left clicks remain suppressed in protected regions.
- Need manual validation that logs show `DemonHunterRightHeldNoClickSuppressionActive` while right-hold remains active and shared cursor-loop left clicks are suppressed in no-click regions.
- Need runtime log validation that `BattleNetD3Tab=120,76,81,76` and `BattleNetPlayButton=30,853,292,75` resolve by adding the Battle.net window origin and that the Play button is found before fallback.
- If BattleNetPlayButton still falls back, inspect the new fallback region diagnostic log for fallback point, expected region center, delta, distance, fallback-inside-region state, and suggested same-size cached region.
- Need to manually validate Battle.net tab/Play button detection with Battle.net fullscreen, windowed, moved, and on another monitor.
- Need to test full teleport route from Southern Highlands through Northern Highlands and onward.
- Need to test interrupted teleport recovery.
- Need to manually confirm repeated failed/interrupted button retry from Cathedral Level 1 to Royal Crypts preserves current=Cathedral, next/retry=Royal Crypts, bypasses manual-button blocking, and does not advance until arrival is confirmed.
- Need to verify repair + salvage flow.
- Need to validate publish/release folder includes Images.
- Need waypoint coordinates before routing Northern Highlands directly to Leoric's Passage.
- Start Game button detection/click verification is still inconsistent, suspected cursor interference with image recognition.
- Battle.net can launch windowed/not full-window; eventually maximize/focus Battle.net after launching.
- Nested project folder structure remains messy and should be cleaned up later.

## Recently Fixed
- Matched the old Python app's combat keyboard-hook behavior more closely: C# now suppresses physical `2` during combat/cleanup while still allowing injected combat key events, preserving Demon Hunter key rotation without letting user input conflict with automation.
- Added Demon Hunter-specific right-held no-click suppression state so the shared combat cursor loop logs `DemonHunterRightHeldNoClickSuppressionActive` instead of making sustained combat look stopped/interrupted while right mouse is still held.
- Diagnostic combat state now shows `DemonHunterRightHeld` and `RuntimeRightHeld` so UI/debug text reflects active right-held combat.
- Compared against the old Python repo `GoblinFarming.py`: left/right combat mouse actions use physical Win32 `mouse_event`, not `pyautogui`, `pydirectinput`, `pynput`, `SendInput`, or window-targeted `PostMessage` mouse clicks.
- Matched the Python Demon Hunter right-hold behavior more closely: C# now starts right-hold only when safe, keeps the hold active when the cursor later enters no-click regions, and avoids sending new right-click down/up events over UI.
- Improved combat mouse suppression behavior to better match the old app feel without allowing UI clicks: Demon Hunter right-hold no longer skips its `1/2/3/4` key rotation when the cursor is over a no-click region.
- Combat logs now identify `combatInputMode`, `clickSendMethod`, whether clicks were sent or suppressed, block reason, no-click region name, cursor position, Diablo rect, and foreground window.
- Added `ExtendedRightMenuNoClickRegion`, a Diablo-window-relative combat-only no-click rectangle covering the lower-right hover menu shown in `Images\Combat\Hover Menu No Click Region.png`.
- Combat click logging now reports named no-click block reasons, including `blockReason=ExtendedRightMenuNoClickRegion`, current mouse position, intended click point, Diablo rect, region rectangle, combat active state, and blocked state.
- Demon Hunter Shift-left clicks now skip sending the click when combat safety reports an unsafe region, and Demon Hunter right-hold now remains held when the cursor enters an unsafe region while combat continues.
- Added BattleNetPlayButton fallback comparison diagnostics. When full-screen fallback finds the Play button after a window-relative miss, logs now show Battle.net window rect, cached region, resolved screen region, fallback point, expected region center, delta, distance, and suggested same-size cached region.
- Updated only `BattleNetPlayButton` from old fullscreen-style region `1200,1070,156,72` to Battle.net-window-local region `30,853,292,75`, measured from `Images\Start Game\Battlet Net Windowed Scan Region 2560x1440.png`; `BattleNetD3Tab` was not changed.
- Corrected Battle.net cached-region interpretation so tab/Play cached regions are window-local pixel offsets, not fullscreen-scaled regions.
- Added Battle.net window-relative logs for cached region, Battle.net window rect, resolved screen region, window-relative found/not-found result, outside-window warnings, and fallback full-screen search.
- Added runtime input cleanup tracking for held left mouse, right mouse, and Shift.
- Prevented post-Diablo cleanup from sending mouse/key events when Diablo is unavailable; duplicate cleanup calls now skip releases when no tracked input is held.
- Added Screenshot-On-Failure coverage for TeleportBlocked, TeleportInterrupted, TeleportConfirmationTimeout, StartGameButtonNotFound, StartGameVerificationFailed, BattleNetPlayButtonNotFound, DiabloTabNotFound, RepairStationNotFound, RepairFailed, WorkflowCancelled, and UnexpectedException.
- Added a read-only Route State Inspector diagnostics tab.
- Added compact Diagnostic Overlay fields for queued retry target, last requested target, failed/interrupted retry state, latest screenshot path, and latest failure screenshot type.
- Polished `Scripts\create-debug-package.ps1`; generated packages include docs, latest log, latest failure screenshots, latest normal debug screenshots, git status/log snapshots, and a manifest while excluding `bin`/`obj`.
- Fixed ButtonRetry so manual retries skip teleport blocking and preserve retry state until success or explicit cancellation.
- Fixed manual teleport button retry after failed/interrupted button teleports.

## Next Recommended Task

Validate combat no-click suppression behavior.

Validation:
- Start combat in a normal safe world area and confirm combat clicks continue normally.
- Hover over the lower-right menu shown in `Images\Combat\Hover Menu No Click Region.png`.
- Confirm combat continues running and does not move the cursor.
- Confirm combat clicks in the menu area are suppressed and logs show `combatInputMode=PhysicalCursorNoClickSuppression`, `clickSendMethod=suppressed`, and `blockReason=ExtendedRightMenuNoClickRegion`.
- Confirm Demon Hunter keyboard rotation continues while left/Shift-left clicks are suppressed.
- Confirm Demon Hunter right-hold, once started in a safe region, stays held while hovering over no-click regions and logs `combatInputMode=PhysicalCursorHeldFromSafeRegion`.
- Confirm shared cursor-loop suppression logs `DemonHunterRightHeldNoClickSuppressionActive` while right mouse remains held, and diagnostic combat state shows `DemonHunterRightHeld=True`.
- Move the cursor away and confirm normal combat clicks resume.
- Spot-check unrelated workflows: teleport, repair, salvage, Battle.net launch/start-game, and exit-game.

After that, run Battle.net Play button detection and inspect fallback comparison diagnostics.

## System Notes

### Battle.net Window-Relative Scan Regions
- `BattleNetD3Tab`: `120,76,81,76`.
- `BattleNetPlayButton`: `30,853,292,75`.
- Cached regions are interpreted as Battle.net-window-local pixel offsets.
- Full-screen image search remains as a safety fallback.
- Fallback diagnostics compare fallback detection point against resolved region center and log delta/distance plus a suggested same-size cached region.
- Static review confirmed these changes do not alter Diablo in-game Start Game logic, teleport logic, combat logic, repair/salvage logic, debug package generator, or diagnostics UI.

### Runtime Input Cleanup
- `ForceReleaseAllRuntimeInputs` logs cleanup reason, tracked held left/right/Shift state before cleanup, Diablo window handle, and Diablo rect availability.
- Cleanup sends `LEFTUP`, `RIGHTUP`, or Shift key-up only when runtime state says that input is currently held and the Diablo window rectangle is available.
- When Diablo is unavailable, cleanup clears tracked held-input state without generating mouse or key events.
- Combat and hotkey runtime click paths update tracked held-input state, including Demon Hunter right-hold cleanup, Shift-left clicks, loot-click pulses, Kadala right-click pulses, and automation safe clicks.

### Combat No-Click Regions
- Combat no-click regions are Diablo-window-relative rectangles, not absolute screen coordinates.
- `ExtendedRightMenuNoClickRegion`: reference-space rectangle `2410,1120,150,240`, based on the green-box hover menu reference image.
- This region is combat-only and does not affect teleport, repair, salvage, Battle.net launch, Start Game, or Exit Game flows.
- Hover menus are expected in-game behavior; combat should continue running while mouse clicks in the unsafe area are suppressed.
- Current combat mouse input uses physical cursor Win32 `mouse_event`, not `SendInput`, `PostMessage`, or `SendMessage`.
- Window-targeted mouse input has not been implemented because Diablo may ignore or inconsistently handle posted mouse messages, and unsafe coordinates could still interact with UI.
- Safe mode is `PhysicalCursorNoClickSuppression`: suppress new mouse clicks in no-click regions while continuing non-mouse combat actions where possible.
- Demon Hunter right-hold uses `PhysicalCursorHeldFromSafeRegion`: the hold must start in a safe region, then no new right-click is sent while hovering over protected UI.
- Demon Hunter-only `DemonHunterRightHeldNoClickSuppressionActive` means right mouse is already held, shared left-click cursor-loop input is suppressed, keyboard/timers continue, and right mouse is not released.
- Combat keyboard hook suppresses physical `1`/`2` while combat is active or stopping, but allows `LLKHF_INJECTED` key events so automation-generated combat keys are not filtered.

### Diagnostic Overlay
- Read-only WinForms panel on the main form.
- Refreshes from the existing status timer with no new image-recognition scans.
- Shows raw location, normalized location, display location, blocking location, current teleport target, next teleport target, queued retry target, last requested target, failed/interrupted retry state, route state, combat state, failure counter, Diablo running status, active workflow, last log file, screenshot count, log count, and latest debug package path if one exists.

### Route State Inspector
- Read-only `Route State` diagnostics tab beside the compact overlay.
- Refreshes from the existing status timer and cached diagnostic/file state; it does not run extra image-recognition scans or activate Diablo.
- Shows raw detected location, normalized app location, display location, blocking location, current/next button locations, queued teleport target, retry queued target, last requested teleport target, last teleport source, last blocking decision and reason, last route decision output, arrival-confirmation wait state and target, failed/interrupted retry state, failure counter, latest log path, latest debug screenshot path, screenshot/log counts, active workflow, Diablo running status, and Diablo focused/active status.

### Screenshot-On-Failure
- Built on top of the existing debug screenshot infrastructure.
- Failure screenshots record the latest failure screenshot type and log the saved path when capture succeeds.
- Screenshot capture catches/logs failures and does not throw back into workflows.
- Capture falls back to the virtual screen when a Diablo client capture is unavailable, so Battle.net/startup failures can still produce evidence.

### Debug Package Generator
- Script: `Scripts\create-debug-package.ps1`.
- Run from the project root with: `powershell -ExecutionPolicy Bypass -File .\Scripts\create-debug-package.ps1`.
- Creates `DebugPackages\GoblinFarmer_Debug_YYYYMMDD_HHMMSS.zip`.
- Includes latest app log if found, latest failure screenshots, latest normal debug screenshots if found, `AGENTS.md`, `Docs\Project_Status.md`, `Docs\TEST_CHECKLIST.md`, `Docs\TODO.md`, `git-status.txt`, `git-log.txt`, and `debug-package-manifest.txt`.
- Manifest records package path, latest log path, latest failure screenshot type, latest failure screenshot path, screenshot counts, and explicit build-artifact exclusions.

## Last Validation
- Built after adding combat keyboard-hook filtering for physical `2`; build succeeded with 0 warnings and 0 errors.
- Static review confirmed the change is scoped to combat-active keyboard filtering and does not add a mouse hook, move the cursor, alter mouse click safety, or change teleport, repair, salvage, Battle.net launch, Start Game, or Exit Game flows.
- Built after adding Demon Hunter right-held no-click suppression state/logging; build succeeded with 0 warnings and 0 errors.
- Static review confirmed the change is scoped to Demon Hunter sustained combat diagnostics/left-click suppression handling and does not change Monk/Witch Doctor, teleport, repair, salvage, Battle.net launch, Start Game, or Exit Game flows.
- Built after aligning Demon Hunter right-hold behavior with the old Python app pattern; build succeeded with 0 warnings and 0 errors.
- Static review confirmed the change is scoped to combat right-hold behavior/logging and does not alter teleport, repair, salvage, Battle.net launch, Start Game, or Exit Game flows.
- Built after improving combat no-click suppression behavior; build succeeded with 0 warnings and 0 errors.
- Static review confirmed the change is scoped to combat input/logging. Teleport, repair, salvage, Battle.net launch, Start Game, and Exit Game flows were not changed.
- Built after adding `ExtendedRightMenuNoClickRegion`; build succeeded with 0 warnings and 0 errors.
- Static review confirmed the change is scoped to combat click safety/logging and does not alter teleport, repair, salvage, Battle.net launch, Start Game, or Exit Game flows.
- Built after adding BattleNetPlayButton fallback comparison diagnostics; build succeeded with 0 warnings and 0 errors.
- Static review confirmed this only adds Battle.net Play scan diagnostics and preserves fallback behavior; Start Game, teleport, combat, and repair/salvage logic were not changed.
- Built `GoblinFarmer.csproj` successfully after updating only `BattleNetPlayButton` to `30,853,292,75`; build succeeded with 0 warnings and 0 errors.
- Static review confirmed `BattleNetD3Tab`, Start Game logic, teleport logic, combat logic, and repair/salvage logic were not changed by the Play button region update.
- Built after correcting Battle.net cached-region interpretation to window-local pixel offsets; build succeeded with 0 warnings and 0 errors.
- Static review confirmed cached Battle.net regions are no longer scaled as fullscreen reference regions; invalid/outside-window cached regions warn and fall back to full-screen search.
- Built after post-exit input cleanup fix; build succeeded with 0 warnings and 0 errors.
- Static review confirmed cleanup changes are limited to tracked runtime input release behavior and do not change Battle.net, teleport, repair/salvage, or repair-station coordinate click logic.
- Polished and ran `Scripts\create-debug-package.ps1`; it created `DebugPackages\GoblinFarmer_Debug_20260601_184528.zip`.
- Inspected the generated package: included `AGENTS.md`, `Docs\Project_Status.md`, `Docs\TODO.md`, `Docs\TEST_CHECKLIST.md`, latest log, 10 failure screenshots, 10 normal debug screenshots, `git-status.txt`, `git-log.txt`, and `debug-package-manifest.txt`.
- Verified the generated package has no `bin/` or `obj/` entries; manifest reported latest failure screenshot type `TeleportBlocked`.
- Built after Screenshot-On-Failure expansion; build succeeded with 0 warnings and 0 errors.
- Built after adding the Route State Inspector; build succeeded with 0 warnings and 0 errors.
- Built after ButtonRetry manual-blocking fix; build succeeded with 0 warnings and 0 errors.
- Built after manual button retry fix; final build succeeded with 0 warnings and 0 errors.

## Backlog
- Validate Battle.net Play button window-relative scan region across fullscreen/windowed/moved monitor setups.
- Clean up nested GoblinFarmer folder structure later.
- Review right-click behavior after Battle.net Play.
- Improve Start Game diagnostics if it fails again.
- Consider moving route logic into configuration after current reliability work stabilizes.
- Add developer/test utilities for current location detection, map detection, Battle.net Play testing, Start Game testing, and image-recognition diagnostics.

## Important Paths
Project:
`D:\D3\Projects\GoblinFarmer\GoblinFarmer\GoblinFarmer`

Runtime Images:
`bin\Debug\net10.0-windows\Images`

Release Target:
`D:\GoblinFarmer`

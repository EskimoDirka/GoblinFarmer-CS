# GoblinFarmer Project Status

This file is the source of truth for current route logic, stable behavior, active work, known issues, recent fixes, and the next recommended task.

## Current Focus
v1.3 GitHub release polish: public README cleanup, release notes, changelog, release checklist, and post-release validation tracking. GoblinFarmer v1.3 is considered stable for release, with completed reliability work around Battle.net launch, Diablo launch detection, Start Game stability/recovery, Make New Game workflow protection, teleport routing accuracy, and synchronized app/installer versioning.

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
- Battlefields: next Rakkis Crossing; Caverns of Frost Level 1 blocks Teleport Next hotkey routing; Caverns of Frost Level 2 allows Teleport Next to Rakkis Crossing.
- Rakkis Crossing: next Pandemonium Fortress Level 1; no block.
- Pandemonium Fortress Level 1: next Pandemonium Fortress Level 2; no block.
- Pandemonium Fortress Level 2: next Make New Game flow; no block.
- WhimsyDale is not part of the farming route; if the fresh Teleport Next hotkey current-location scan detects WhimsyDale, Teleport Next is blocked and the notification uses WhimsyDale as the displayed location.
- Cave Of The Moon Clan Level 1 is not part of the farming route; if the fresh Teleport Next hotkey current-location scan detects Cave Of The Moon Clan Level 1, Teleport Next is blocked and the notification uses Cave Of The Moon Clan Level 1 as the displayed location.
- Cave Of The Moon Clan Level 2 is an allowed Southern Highlands route sublocation; if the fresh Teleport Next hotkey current-location scan detects Cave Of The Moon Clan Level 2 while Northern Highlands is queued, Teleport Next should continue to Northern Highlands.
- Caverns of Frost Level 1 blocks Teleport Next hotkey routing and uses Caverns of Frost Level 1 as the displayed location; Caverns of Frost Level 2 remains allowed and can continue to Rakkis Crossing.

## Known Stable Systems
- Images are project-relative and copied into the build output.
- Release versioning is owned by `GoblinFarmer.csproj`; the app title reads `AssemblyInformationalVersion`, the published EXE carries the project file/product versions, and Inno Setup derives installer naming from the published executable.
- Battle.net can relaunch/focus if the process exists but no visible window exists.
- Diablo launch grace period prevents false cancellation while Diablo starts.
- Start Game uses practical stable-button verification, 100ms polling, state-based click acceptance, manual-click recovery, and `WorkflowAlreadyActive` protection for Make New Game overlap.
- Make New Game flow has created a game and completed the first teleport to Southern Highlands in prior validation.
- Teleport routing preserves raw detected location, normalized app location, display location, and blocking location as separate concepts.
- Route state preserves the previous confirmed location when teleport confirmation fails or is blocked.
- Interrupted teleport fail-safes preserve route and button state.
- Manual teleport buttons preserve failed/interrupted intended targets as retry state.
- Manual teleport buttons are intentionally allowed and bypass route blocking; Teleport Next hotkey routing uses the route-blocking current-location scan.
- Manual button retries bypass teleport blocking like the original manual button request.
- Manual same-button clicks while a teleport is waiting for arrival confirmation are ignored and logged to avoid overlapping waypoint workflows.
- Teleport Next uses the fresh hotkey current-location scan to advance route state when the player is already at the queued destination, skipping the redundant teleport attempt without incrementing completed teleports; the latest implementation also scans queued-target templates so non-blocking route destinations can be detected instead of returning `Unknown`.
- Teleport Next already-at-queued-destination advancement logs `AlreadyAtQueuedDestinationCheck`, `AlreadyAtQueuedDestinationDetected`, `skippedDestination`, `newRequestedTarget`, and whether the following teleport was actually started.
- Teleport Next may remain unavailable for a brief intentional delay after combat stops so combat/input state can settle before route hotkeys resume.
- In-game notifications use a no-activate overlay so blocked/already-here messages should not steal Diablo focus.
- Gates of Caldeum and City Of Caldeum remain distinct detected/blocking strings in logs and notifications; route button state still groups Gates and other Caldeum sublocations under the City Of Caldeum route checkpoint.
- Waterway sub-regions keep their raw identity for blocking decisions.
- Ancient Waterway waypoint arrival confirmation requires the exact Ancient Waterway title; channel child locations still participate in route and blocking decisions but no longer complete an Ancient Waterway waypoint click by alias.
- Cathedral blocks Royal Crypts unless the raw detected location is Cathedral Level 3.
- City Of Caldeum blocks Ancient Waterway unless the raw detected location is Ruined Cistern.
- Western Channel Level 2 selects Ancient Waterway as the next target; Eastern Channel Level 2 selects Stinging Winds.
- Stinging Winds blocks Battlefields unless the current detected sub-region is Black Canyon Mines.
- Cave Of The Moon Clan Level 2 remains allowed and maps back to the Southern Highlands checkpoint so the next target stays Northern Highlands; Cave Of The Moon Clan Level 1 remains the only Moon Clan cave level that blocks Teleport Next hotkey routing.
- Caverns of Frost Level 1 blocks Teleport Next hotkey routing before it can route as the Battlefields group; Caverns of Frost Level 2 remains an allowed Battlefields alias that can continue to Rakkis Crossing.
- Ancient Waterway self-click blocks before opening the map and preserves current/next button state.
- Repair flow waits for New Tristram/vendor readiness and logs repair-station click timing before using the repair-station coordinate fallback.
- Repair flow logs New Tristram readiness, minimal post-arrival settle timing, repair-station click attempts, elapsed time, whether clicks were sent, blacksmith/repair menu detection, and total repair workflow duration while preserving the repair plus salvage sequence.
- Combat automation is stable enough for current route work; do not change combat logic unless explicitly requested.
- Combat click safety blocks existing UI no-click regions and now includes the extended lower-right hover menu area without moving the cursor or stopping combat.
- Combat no-click suppression keeps non-mouse combat actions running where safe; Demon Hunter key rotation continues while mouse clicks are suppressed over UI regions.
- Combat keyboard-hook filtering matches the old Python app's injected-key behavior for combat-relevant number keys: physical `1`/`2` are suppressed during combat, while injected automation key events pass through.
- The Hotkeys group now shows default-on entries for physical `1 - Teleport Next Location` and `2 - Exit Game` beside the existing hotkey controls; unchecking either opt-out disables only that physical hotkey path.
- Physical `2` starts Exit Game when Diablo is focused and combat is inactive; combat and combat-stop cleanup suppress it before the Exit Game hotkey path can run.
- Demon Hunter right mouse now follows the old Python app's pattern: start holding right mouse only in a safe region, then keep the hold active through hover/no-click regions without sending new click events.
- Demon Hunter sustained combat now treats shared cursor-loop left-click suppression as active right-held combat when right mouse is already held from a safe region.
- Witch Doctor held/channel mouse input starts only from a safe world region, remains held through combat no-click regions without sending new clicks, and continues keyboard/Hex timers while UI clicks are suppressed.
- Battle.net tab and Play button image searches use Battle.net-window-local cached scan regions plus the current Battle.net window left/top, with full-screen search as fallback.
- Battle.net startup uses the configured executable path first and falls back to installation discovery when the configured path is unavailable, then retries launch requests every 1s for up to 5s until a visible Battle.net window exists. Background `Battle.net.exe` process status is logged for diagnostics only and does not count as launch-ready.
- Battle.net Play button detection polls every 100ms and clicks only from a current image match that passes the configured confidence threshold.
- Battle.net launch diagnostics explicitly track whether GoblinFarmer sent the Play click, the click point/timestamp, whether Diablo launched after that app click, whether Diablo launched without an app click, whether Battle.net remained open after Diablo launch, and whether the post-launch Battle.net close was requested, succeeded, failed, or timed out.
- Start Game detection preserves the existing stable-button image verification, now logs each detection/stable-scan attempt, polls the stable scan at about 100ms, clicks only from the current stable image match, and accepts stability by repeated in-tolerance scans or stable duration.
- Start Game click acceptance logs the exact confirmation reason: character load confirmation, loaded location title, player in-game state, or Start Game button disappearance.
- If the user manually clicks Start Game while the app is waiting, loaded-game evidence is treated as `StartGameAcceptedByLoadedGameState` so the Make New Game flow can continue instead of waiting for a button that is gone.
- Make New Game clicks during an active workflow now show/log `WorkflowAlreadyActive` instead of silently doing nothing.
- Runtime input cleanup releases only tracked held left/right/Shift inputs while Diablo is available and clears tracked state without mouse events after Diablo closes.
- Diagnostic Overlay, Route State Inspector, Screenshot-On-Failure, Success Screenshot Capture, and Debug Package Generator are implemented.
- Major workflow success/failure milestones can capture paired Diablo/App screenshots using matching timestamps and action names, controlled by the existing `Keep Debug Screenshots` setting.
- Debug packages include `route-failure-summary.txt`, which summarizes route blocks, failures, cancels, allowed debug decisions, Start Game verification failures, route state, screenshots, and likely explanations from the latest log.
- Debug packages include `debug-screenshot-manifest.txt`, which pairs selected success/failure Diablo and GoblinFarmer app screenshots by timestamp, workflow, action, and surface.
- Debug package screenshot collection is session-only: screenshots older than the current GoblinFarmer session start are excluded from the package while existing screenshot retention remains unchanged.
- Debug package console output and `debug-package-manifest.txt` report total/success/failure/normal screenshot counts, excluded stale screenshot count, package size, session start, and session duration.
- Missing screenshot/template assets are logged with asset name, calling flow, scan-region context when known, and current app/game state; when combat is inactive, a modeless debug prompt can optionally capture the current Diablo window or known scan region into the expected Images folder.
- Combat-active bounty menu auto-close now mirrors the old Python app: a dedicated combat-menu watcher starts with combat, polls the Bounty Complete title region about every 100ms, uses threshold `0.740`, and sends a single automation-safe injected Escape with about a 1s cooldown.
- Bounty close logging now includes `CombatMenuWatcherStarted`, `BountyMenuDetected`, `BountyMenuEscapeSent`, `InjectedEscapeIgnoredByStopWatcher`, and `CombatMenuWatcherStopped`, with combat state and `automationCancelled=false` where relevant.
- `Config\AppSettings.json` centralizes release/debug configuration, auto-creates when missing, logs loaded values, and currently controls diagnostic panes, debug screenshots, missing-asset prompts, notification display, repair timing, teleport confirmation timeout, and Bounty Complete watcher poll/cooldown timing.
- `Config\AppSettings.json` now also stores Diablo III and Battle.net executable paths, the relative Images root, scan-region cache path, launch timings, and image-recognition thresholds. Missing executable paths trigger auto-discovery and first-run setup instead of relying on source-machine install paths.
- `Debug.EnableDebugScreenshots` defaults to enabled in Debug builds and disabled in Release builds only when no saved preference exists. Existing saved preferences are honored, and the Debug Mode toggle no longer overwrites the screenshot preference.
- Normal UI mode keeps diagnostic panes and debug screenshot controls hidden unless Debug Mode is enabled from the Settings panel.
- The startup form client size is tall enough to show the full Settings group and Debug Mode checkbox without manual resizing while preserving the compact layout.
- Release publishing uses `Scripts\publish-release.ps1` for a self-contained `win-x64` folder and `Installer\GoblinFarmer.iss` for an Inno Setup installer targeting `%LOCALAPPDATA%\Programs\GoblinFarmer`; `artifacts\` is generated/ignored, the app must be published before compiling the `.iss`, and the installer shows the directory page, shows the selected path on the ready page, and starts from the default app-local path instead of silently reusing a previous custom install directory.
- Exit Game workflow no longer generates desktop right-clicks after Diablo exits.
- Exit Game workflow no longer closes GoblinFarmer after Diablo exits.
- Battle.net Play button window-relative scan region has dedicated fallback comparison diagnostics; current region is `16,1256,292,75`, recalibrated from fallback diagnostics.

## Post-Release Validation Backlog
- Full route validation from Southern Highlands through Pandemonium Fortress Level 2, with route-failure-summary evidence checked after each run.
- Fresh run validation that success and failure screenshot pairs are generated for major workflow milestones and appear in `debug-screenshot-manifest.txt`.
- Missing-asset prompt validation should confirm missing templates log state, show a non-blocking capture/skip prompt outside combat, save accepted captures to the expected Images folder, and suppress prompts during combat.
- Battle.net launch validation should confirm window-based startup detection, configured-path-first launch selection, installation-discovery fallback when no valid configured path exists, 1s launch retries up to 5s, 100ms Play polling, app Play click, suspected manual Play click, Diablo launch without app click, successful Diablo launch after app click, and post-launch visible-window close warnings without treating background/tray Battle.net processes as failures.
- Ancient Waterway/channel retest: manual Ancient Waterway from channel levels must not advance route state until exact Ancient Waterway arrival is confirmed.
- Caldeum to Ancient Waterway and Stinging Winds to Battlefields validation should prove the allowing raw locations (`Ruined Cistern`, `Black Canyon Mines`) in the summary/logs.
- Optional broader Start Game validation can continue across unusual cursor, display, and monitor conditions; the v1.3 stable-detection deadlock and Make New Game stuck state are fixed.
- Battle.net tab/Play detection across fullscreen, windowed, moved-window, and multi-monitor setups.
- Repair/salvage monitoring after route and launch changes; current repair timing is acceptable and should prioritize reliability over further speed changes.
- Teleport Next WhimsyDale validation should confirm the fresh current-location scan detects WhimsyDale, blocks routing, and shows WhimsyDale in the notification/logs.
- Teleport Next Cave Of The Moon Clan Level 1 validation should confirm the fresh current-location scan detects Cave Of The Moon Clan Level 1, blocks routing, and shows Cave Of The Moon Clan Level 1 in the notification/logs.
- Teleport Next Cave Of The Moon Clan Level 2 validation should confirm the fresh current-location scan detects Cave Of The Moon Clan Level 2, allows routing, and continues to Northern Highlands.
- Teleport Next Caverns of Frost validation should confirm Caverns of Frost Level 1 blocks with the raw/display location in logs and notification, while Caverns of Frost Level 2 allows Rakkis Crossing.
- Teleport Next route advancement validation should confirm that when the player is already at the queued destination, the hotkey logs `teleportSkipped=True`, advances the route, and sends the next valid destination instead.
- Release configuration validation should confirm diagnostic overlay/route inspector are hidden by default, reappear when enabled in config, and debug screenshot/missing-asset prompt settings behave as configured.
- Publish/release folder validation to confirm Images are included.

## Open Follow-Up Validation
No v1.3 release-blocking issues are currently documented. The items below are manual validation or future hardening notes, not indicators that the v1.3 launch, Start Game, Make New Game, installer versioning, or route-blocking fixes are still broken.
- Need manual combat validation that hovering over the extended lower-right menu blocks combat clicks with `blockReason=ExtendedRightMenuNoClickRegion`, while combat continues and normal clicks resume after moving away.
- Need manual validation that Demon Hunter right-hold continues through no-click hover regions without clicking UI, while left/Shift-left clicks remain suppressed in protected regions.
- Need manual validation that logs show `DemonHunterRightHeldNoClickSuppressionActive` while right-hold remains active and shared cursor-loop left clicks are suppressed in no-click regions.
- Need manual validation that Witch Doctor held/channel input starts from a safe region, stays held through no-click regions without UI clicks, continues keyboard/Hex timers, and releases cleanly on combat stop/focus loss.
- Need runtime log validation that `BattleNetD3Tab=120,76,81,76` and `BattleNetPlayButton=16,1256,292,75` resolve by adding the Battle.net window origin and that the Play button is found before fallback.
- Need live validation that Battle.net startup fails when no visible Battle.net window appears within 5s even if the background `Battle.net.exe` process exists.
- Need live validation that Battle.net launch logs show the selected executable path, launch attempts, retry count, elapsed time, process-observed state, and window-detected launch-ready state.
- Need live validation that Play button detection attempts occur at 100ms cadence and click only after a current confidence-passing image match.
- Need manual validation of the new `BattleNetManualPlaySuspected`, `BattleNetPlayButtonNotClickedByApp`, `BattleNetStillOpenAfterDiabloLaunch`, and `BattleNetPostLaunchCloseSummary` diagnostics during a run where the user manually clicks Play or the visible Battle.net window remains open.
- Need manual validation that background/tray Battle.net processes after Diablo launch log as informational and do not cause close timeout/failure when no visible Battle.net window remains.
- If BattleNetPlayButton still falls back, inspect the new fallback region diagnostic log for fallback point, expected region center, delta, distance, fallback-inside-region state, and suggested same-size cached region.
- Need to manually validate Battle.net tab/Play button detection with Battle.net fullscreen, windowed, moved, and on another monitor.
- Need manual validation that Ancient Waterway requested from Western/Eastern Channel levels preserves correct Waterway state and next target until exact arrival confirmation.
- Need to validate `route-failure-summary.txt` on the next generated package from a fresh run with new `RouteFailureSummary`, `RouteDebugSummary`, and `StartGameVerificationFailureSummary` log lines.
- Need to validate fresh runtime paired success screenshots for Battle.net Play clicked, Diablo process detected, Start Game clicked, teleport confirmed, repair complete, salvage complete/skipped, and Exit Game complete.
- Need to validate fresh runtime paired failure screenshots for workflow failures and verify `debug-screenshot-manifest.txt` pairs Diablo/App evidence correctly.
- Need live validation of the optional missing-asset capture prompt for a deliberately missing template, including accept, skip, and combat-suppressed paths.
- Need to validate `session-info.txt` session-start metadata on the next app launch so package screenshot filtering uses explicit app startup time instead of latest-log fallback.
- Need to test interrupted teleport recovery.
- Need to manually confirm repeated failed/interrupted button retry from Cathedral Level 1 to Royal Crypts preserves current=Cathedral, next/retry=Royal Crypts, bypasses manual-button blocking, and does not advance until arrival is confirmed.
- Need live validation that the new 50ms repair station settle remains responsive after New Tristram arrival without missing the blacksmith click/menu.
- Need to validate WhimsyDale Teleport Next blocking in a live run.
- Need to validate Caverns of Frost Level 1 Teleport Next blocking and Caverns of Frost Level 2 allowance to Rakkis Crossing in a live run.
- Need to validate bounty menu auto-close diagnostics after the Python-style combat watcher port: the configured `Images\Combat\Bounty Complete Scan Region.png` region should drive `CombatMenuWatcherStarted`, detect with `BountyMenuDetected`, send `BountyMenuEscapeSent`, and log `InjectedEscapeIgnoredByStopWatcher` while combat continues.
- Need release-style validation of `Config\AppSettings.json` defaults and toggles.
- Need to validate publish/release folder includes Images.
- Need waypoint coordinates before routing Northern Highlands directly to Leoric's Passage.
- Battle.net can launch windowed/not full-window; eventually maximize/focus Battle.net after launching.
- Nested project folder structure remains messy and should be cleaned up later.

## Recently Fixed
- Fixed release versioning drift for v1.3 by keeping `<Version>1.3.0</Version>`, `<AssemblyVersion>1.3.0.0</AssemblyVersion>`, `<FileVersion>1.3.0.0</FileVersion>`, and `<InformationalVersion>1.3.0</InformationalVersion>` in `GoblinFarmer.csproj` as the release source of truth.
- Disabled SDK source-revision decoration for informational version metadata so Windows `ProductVersion` stays `1.3.0` instead of `1.3.0+<commit>`.
- Updated the app title to display `GoblinFarmer v1.3.0` from `AssemblyInformationalVersion` instead of relying on a hard-coded designer title or publish-time overrides.
- Updated `Scripts\publish-release.ps1` to stop overriding MSBuild version properties, verify the published EXE `FileVersion` / `ProductVersion`, and pass only the publish folder to Inno Setup.
- Updated `Installer\GoblinFarmer.iss` to read the published executable version with `GetFileVersion(...)` and generate a versioned installer filename from that published EXE.
- Updated installer install-path behavior so setup displays the install directory page, shows the selected directory on the ready page, and avoids silently reusing a previous custom install path.
- Updated the Inno Setup preprocessor block so it checks for the published `GoblinFarmer.exe` before reading file version metadata and fails with a clear "publish first" error if `artifacts\publish\GoblinFarmer` has not been generated.
- Fixed Start Game stable-detection deadlock from `GoblinFarmer_Debug_20260603_072805.zip` / `Logs\GoblinFarmer_20260603_072517.log`: the log showed repeated Start Game matches around `316,688` / `320,691`, but the old stable gate kept emitting `StartGameButtonUnstable` and timed out with `clickAttempts=0`.
- Replaced fragile Start Game stability reset behavior with practical in-tolerance acceptance by consecutive visible scans or stable duration, with detailed timeout logs for first/latest point, dx/dy, tolerance, visible count, stable duration, and required thresholds.
- Added `StartGameAcceptedByLoadedGameState` recovery so manual Start Game clicks detected by character-load, loaded-location, or in-game evidence continue the Make New Game flow.
- Added visible/logged `WorkflowAlreadyActive` handling when Make New Game is clicked while another Make New Game / Start Game workflow is active.
- Added Battle.net-style resilience logging to Start Game detection while preserving the existing image asset, confidence threshold, stable-button verification, loading checks, and game-load logic.
- Replaced the fixed post-click Start Game wait with state-based acceptance polling that succeeds when the button disappears steadily, character load confirmation appears, a loaded location title appears, or the existing in-game evidence check succeeds.
- Added Caverns of Frost Level 1 to raw Teleport Next hotkey blocking while preserving Caverns of Frost Level 2 as an allowed Battlefields alias for routing to Rakkis Crossing.
- Updated the README with a user-facing note that Teleport Next may have a short intentional delay after combat stops while combat/input state settles.
- Latest test context confirmed Cathedral Level 1, Leoric's Passage, Caldeum-style blocking, Ancient Waterway/Western/Eastern Channel rules, Stinging Winds blocking, teleport interruption state preservation, Battle.net launch, and Exit Game flow are working; the remaining route edge fixed here was Caverns of Frost Level 1.
- Added Debug/Release build defaults for `EnableDebugScreenshots` when no saved preference exists, while honoring existing saved preferences and avoiding routine config rewrites on startup.
- Prevented the Debug Mode checkbox from overwriting `EnableDebugScreenshots`; the Keep Debug Screenshots checkbox remains the source of truth for that setting.
- Increased the normal startup client size/minimum size so the Settings group and Debug Mode checkbox are visible without manual resizing.
- Updated Battle.net post-launch close/status logging so only a visible Battle.net window remaining after Diablo launch is treated as a warning; background/tray Battle.net processes are informational.
- Added Hotkeys group entries for `1 - Teleport Next Location` and `2 - Exit Game`, defaulted on with small opt-out gates for their existing physical hotkey paths.
- Updated Battle.net startup to select the configured executable path first, fall back to installation discovery when needed, retry launch requests every 1s for up to 5s, and treat only a visible Battle.net window as launch-ready.
- Updated Battle.net Play button detection to poll every 100ms and log each detection attempt before clicking the current confidence-passing image match.
- Added release-ready runtime path configuration, auto-discovery for Diablo III and Battle.net, first-run setup, Settings panel validation/change controls, and automation blocking while required config is invalid.
- Added configurable launch timings and image-recognition thresholds for user-adjustable release tuning without source edits.
- Added a self-contained publish script and Inno Setup installer definition with Start Menu/desktop shortcuts, icon preservation, and config preservation across installer upgrades.
- Removed obsolete launch stubs and routed image/scan-region locations through config-backed helpers while preserving route, combat, repair, salvage, Battle.net Play, and bounty-menu behavior.
- Ported the Python combat bounty-menu watcher behavior from `GoblinFarming.py`: combat start now launches a watcher task, scans `Bounty Menu Title.png` in the refreshed `Bounty Complete Scan Region.png` region, uses threshold `0.740`, polls at `100ms`, sends injected Escape with a `1000ms` cooldown, and does not call combat/automation stop or input cleanup.
- Added explicit `InjectedEscapeIgnoredByStopWatcher` logging in the C# Escape stop path so the app records when the bounty watcher Escape is passed through to Diablo instead of stopping combat.
- Added a `Bounty` config section with `PollIntervalMs=100` and `EscapeCooldownMs=1000`.
- Reviewed `GoblinFarmer_Debug_20260602_221905.zip`: Bounty Complete detection worked with `confidence=1.000` while `combatActive=True`, followed by `BountyMenuEscapeSent`, then a later `Combat stopping: reason=Escape`. The new C# behavior matches Python by isolating bounty close in a combat-only watcher and ensuring the app stop watcher ignores the injected Escape.
- Reviewed `GoblinFarmer_Debug_20260602_220408.zip`: Teleport Next already-at-queued-destination worked live with `AlreadyAtQueuedDestinationDetected` and `AlreadyAtQueuedDestinationTeleportStart` / `teleportStarted=True`; Cathedral blocked the hotkey to Royal Crypts while the manual Royal Crypts button remained allowed with `source=Button` and `ignoreBlocking=True`; bounty menu scanning used `Bounty Menu Title.png`, `Bounty Menu Scan Region.png`, `referenceRegion=997,977,564,286`, and captured low-confidence region screenshots, but combat-active detections were skipped until combat stopped.
- Refreshed `Images\Combat\Bounty Menu Title.png` from the latest live Bounty Complete popup asset. The old gray template scored about `0.777` against the marked scan-region asset; the refreshed gold-title template scores `1.000` against the refreshed Bounty Complete scan region asset.
- Superseded the earlier timer-driven bounty close path. Bounty Complete close detection now runs only from the combat menu watcher while combat is active.
- Reviewed `GoblinFarmer_Debug_20260602_213950.zip`: Exit Game / repair / salvage completed, repair timing was acceptable at `waitAfterArrivalMs=60` and `totalRepairWorkflowDurationMs=3259`, Make New Game / Start Game succeeded in this run, Teleport Next already-at-queued-destination still did not dispatch the following target, and bounty menu scanning repeatedly used `Bounty Menu Title.png` plus the `BountyMenuTitle` scan region `997,977,564,286` from `Bounty Menu Scan Region.png` but stayed below the `0.780` threshold.
- Fixed the Teleport Next already-at-queued-destination miss by adding queued-target templates to the hotkey fresh current-location scan, logging `AlreadyAtQueuedDestinationCheck`, and preserving the explicit `AlreadyAtQueuedDestinationDetected` / `AlreadyAtQueuedDestinationTeleportStart` dispatch proof when the fresh scan matches the queued target.
- Tightened bounty menu scan diagnostics and behavior: the scanner logs `BountyMenuScanResult` with `imagePath`, `scanRegionImagePath=Images\Combat\Bounty Complete Scan Region.png`, reference/screen region, best confidence, threshold, near-match threshold, and detection source; low-confidence scans capture the configured scan region; narrow near matches just below threshold can close with `detectionSource=RegionNearMatch`.
- Reviewed `GoblinFarmer_Debug_20260602_210617.zip`: Cave Of The Moon Clan Level 1 blocked Teleport Next correctly with raw/display notification text, repair/salvage/Exit Game completed, repair timing showed `waitAfterArrivalMs` around `77-81` and `totalRepairWorkflowDurationMs` around `3337-3341`, Start Game retry failed once then succeeded, and no useful bounty detected/escape evidence appeared beyond scan-region loading.
- Fixed Teleport Next already-at-queued-destination advancement logging and start behavior: the hotkey now logs `AlreadyAtQueuedDestinationDetected`, advances route state, logs `AlreadyAtQueuedDestinationTeleportStart`, and only reports `teleportStarted=True` when the following route teleport is actually dispatched.
- Changed bounty menu auto-close to use automation-safe injected Escape with `injectedEscape=true` logging from the combat watcher.
- Added stable Start Game button confirmation before clicking and before treating Leave Game as fully back at the main menu, with `StartGameButtonStable`, `StartGameButtonUnstable`, and `StartGameButtonStableNotFound` diagnostics.
- Added centralized `Config\AppSettings.json`, startup auto-create, config value logging, release UI gating for diagnostic panes, config gates for debug screenshots/missing-asset prompts, notification display settings, repair timing settings, and teleport confirmation timeout support.
- Reduced the repair post-New-Tristram settle default to 50ms while keeping blacksmith menu polling visual and retry-based because the latest logs show the menu appears near the existing 1.5s wait window.
- Reviewed `GoblinFarmer_Debug_20260602_204323.zip`: session completed with Games Created `1`, Teleports Completed `15`, Failures `0`, Blocked Teleports `0`; route-failure-summary confirmed Ruined Cistern, Black Canyon Mines, Western Channel Level 2, Eastern Channel Level 2, Rakkis Crossing, and Pandemonium Fortress route decisions; success screenshots covered Ancient Waterway, Stinging Winds, Battlefields, Pandemonium Fortress Level 1/2, New Tristram, Repair Complete, Salvage skipped/completed path, and Exit Game Complete.
- Added Cave Of The Moon Clan Level 1 to Teleport Next hotkey blocking with the raw detected location name used in logs, route-failure-summary explanation, and blocked notification text.
- Added Teleport Next route advancement from the fresh hotkey current-location scan so an already-at-queued-destination state skips a redundant waypoint click and advances toward the next route target.
- Reduced the repair post-New-Tristram settle from 150ms to 75ms after the latest package showed `waitAfterArrivalMs=164`, `timeUntilRepairStationDetectedMs=1705`, `timeUntilRepairMenuOpenedMs=1878`, and `totalRepairWorkflowDurationMs=3313`; menu-open latency was the remaining bottleneck, not the arrival settle.
- Expanded bounty menu auto-close diagnostics with active Diablo/combat state and richer `BountyMenuDetected` / `BountyMenuEscapeSent` context.
- Expanded the generic debug screenshot support with an optional missing-asset capture prompt for missing screenshot/template files, including asset/flow/region/state logging, combat suppression, accept/skip logging, and saving accepted captures into the expected Images folder.
- Optimized repair station timing by reducing the fixed New Tristram post-arrival settle to a short input settle and replacing the fixed post-click delay with visual polling for the blacksmith menu before retrying.
- Added repair workflow timing logs for wait after arrival, time until repair station/blacksmith menu detection, time until repair menu opened, blacksmith click attempts, and total repair workflow duration.
- Added WhimsyDale to Teleport Next hotkey route-blocking detection and normalized the `Whimsydale.png` template name to display as WhimsyDale.
- Added physical `2` as an Exit Game hotkey when Diablo is focused and combat is inactive, with combat-active suppression logs that preserve injected combat key events.
- Kept normal manual teleport buttons intentionally allowed with `ignoreBlocking=true`, matching the corrected button behavior.
- Expanded repair-station and repair-menu timing logs with click attempt, elapsed time, click-sent result, and blacksmith/repair menu visibility while preserving repair plus salvage integration.
- Split Battle.net launch success from post-launch Battle.net close status: when Diablo launches after an app Play click, launch is successful even if Battle.net remains open, and the close result is reported separately as requested/succeeded/failed/timed out.
- Added a one-shot Battle.net launch outcome guard and one-shot post-launch close evaluation so the route/workflow summary no longer emits conflicting success/failure interpretations for the same launch event.
- Updated the debug package workflow summary parser to consume `BattleNetLaunchSummary` and `BattleNetPostLaunchCloseSummary` entries with the explicit `appClickedBattleNetPlay`, `diabloLaunchedAfterAppClick`, `battleNetStillOpenAfterLaunch`, `battleNetCloseRequested`, and `battleNetCloseSucceeded` fields.
- Added Battle.net launch state tracking for app-sent Play click, Play click point/timestamp, Diablo launch after app click, and Diablo launch without app click.
- Added Battle.net launch diagnostics for suspected manual Play intervention, Play not clicked by app, and Battle.net still open after Diablo launch, with paired failure screenshots and workflow summary entries.
- Updated the debug package route/workflow summary parser so Battle.net launch verdicts are not treated as fully successful unless GoblinFarmer recorded a Play click and Diablo launched afterward.
- Improved existing Battle.net close diagnostics to report whether Battle.net remained running/visible after the safe `CloseMainWindow` request.
- Added runtime session-start metadata so debug packages can identify the active GoblinFarmer session and avoid pulling screenshots from previous runs.
- Updated debug package screenshot selection to use current-session screenshots only, excluding stale screenshots from previous sessions and legacy folders without changing retention cleanup.
- Improved `debug-package-manifest.txt` and console output with package size, session start timestamp, session duration, total screenshot count, success screenshot count, failure screenshot count, normal screenshot count, and stale screenshot exclusions.
- Improved route-failure summary blocks with explicit workflow and screenshot reference fields so route incidents read more like standalone debugging notes.
- Added paired diagnostic screenshot capture for major workflow milestones. Success and failure captures now write Diablo and GoblinFarmer app screenshots with matching timestamps/action names into the existing runtime `Screenshots` folder.
- Upgraded failure screenshot capture so existing failure categories continue to be captured while adding a GoblinFarmer app screenshot beside the Diablo evidence.
- Added milestone success screenshots for Battle.net Play clicked, Diablo process detected, Start Game verified, teleport confirmed, repair complete, salvage complete/skipped, Leave Game main menu confirmed, and Exit Game complete.
- Updated the debug package generator to include success screenshots, paired failure screenshots, and `debug-screenshot-manifest.txt` while keeping screenshot selection bounded by package limits.
- Confirmed success screenshots participate in the existing screenshot retention cleanup because they are stored in the same runtime `Screenshots` directory and matched by the existing `*.png` cleanup.
- Reviewed `GoblinFarmer_Debug_20260602_063044.zip` and identified route/debuggability issues in Ancient Waterway channel confirmation, Caldeum-to-Waterway blocking explanation, Stinging Winds arrival diagnostics, Black Canyon Mines/Battlefields proof, and Start Game verification failure explanation.
- Tightened Ancient Waterway arrival confirmation so channel aliases can still drive route/blocking rules but cannot complete an Ancient Waterway waypoint click unless the exact Ancient Waterway title is detected.
- Added route failure/debug summary log lines for teleport blocks, cancels, failed confirmations, and allowed blocking decisions, including requested target, source, raw/normalized/display/blocking locations, current/next/retry buttons, blocking reason, screenshot path, and likely explanation.
- Added Start Game verification failure summary logging with click point, cursor position, Start Game scan region, button visibility, loaded-state evidence, screenshot path, and likely explanation while preserving retry behavior.
- Updated the debug package generator to include `route-failure-summary.txt`, generated from the latest log so route failures can be understood without relying on user memory.
- Recalibrated the cached Battle.net Play button scan region from `30,853,292,75` to the diagnostic-suggested `16,1256,292,75` while preserving Diablo III tab fallback, full-screen Play fallback, and fallback comparison diagnostics.
- Added Witch Doctor-only held/channel input handling: the shared cursor loop now starts Witch Doctor held input only in a safe region, keeps it held through combat no-click regions without new mouse clicks, logs Witch Doctor suppression state, and releases via existing runtime cleanup.
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

Validate release config toggles, Whimsy hotkey blocking, Teleport Next already-at-queued-destination advancement, 50ms repair settle, Start Game stable retry behavior, and bounty menu auto-close diagnostics in the next live run.

Validation:
- Confirm Teleport Next blocks from WhimsyDale with raw/display notification text `WhimsyDale`.
- Confirm Teleport Next route advancement logs `AlreadyAtQueuedDestinationDetected`, `skippedDestination`, `newRequestedTarget`, and `AlreadyAtQueuedDestinationTeleportStart` with `teleportStarted=True`.
- Confirm manual teleport buttons remain allowed and show `source=Button`, `ignoreBlocking=True`, and blocking skipped because `ignoreBlocking=true`.
- Confirm internal workflow teleports like Make New Game / Exit Game still bypass blocking where intentionally required.
- Confirm repair logs show New Tristram confirmation, the configured 50ms settle, blacksmith click attempts, elapsed time, menu detection, and successful repair/salvage integration.
- Confirm optimized repair logs include `waitAfterArrivalMs`, `timeUntilRepairStationDetectedMs`, `timeUntilRepairMenuOpenedMs`, and `totalRepairWorkflowDurationMs`.
- Confirm bounty menu logs show `CombatMenuWatcherStarted`, `BountyMenuDetected`, `BountyMenuEscapeSent`, and `InjectedEscapeIgnoredByStopWatcher`.
- Confirm combat-active bounty close keeps combat loops running before/during/after the injected Escape, while manual user Escape can still stop automation.
- Confirm Start Game retry after Leave Game logs stable Start Game button evidence and does not fail from stale main-menu state.
- Confirm default config hides diagnostic panes, and enabling `Debug.ShowDiagnosticOverlay` / `Debug.ShowRouteInspector` restores them.
- Confirm a deliberately missing template logs `MissingAssetDetected`, shows the modeless capture prompt outside combat, saves an accepted capture into the expected Images folder, logs `MissingAssetManualCaptureSkipped` when declined, and logs prompt suppression while combat is active.
- Confirm manual Ancient Waterway from Western/Eastern Channel levels does not count as successful unless raw confirmed location becomes exact Ancient Waterway.
- Confirm Caldeum/Gates/Sewers/Flooded Causeway blocks explain that `Ruined Cistern` is required, and that a later allowed Ancient Waterway decision shows raw/normalized `Ruined Cistern`.
- Confirm Stinging Winds failures include map act, target act, destination reference/click point, post-click location confirmation, screenshot, and cancellation/timeout explanation.
- Confirm Battlefields is allowed only when raw/normalized location evidence shows `Black Canyon Mines`.
- Confirm Start Game retry behavior remains intact and a failed verification logs cursor, scan region, button visibility, screenshot path, and likely explanation.
- Generate a debug package and inspect `route-failure-summary.txt` before asking the user to interpret the run.
- Inspect `debug-screenshot-manifest.txt` and verify every new success/failure milestone has matching Diablo/App screenshots when the workflow reaches that milestone.
- Confirm the package excludes screenshots from previous app sessions and reports stale screenshot exclusions in the manifest/console.
- Confirm Battle.net launch summary entries distinguish fully automated launch from suspected manual Play intervention.

### Route Rule Config Design Review
Do not migrate live route rules yet. Recommended future JSON shape:

```json
{
  "Locations": {
    "City Of Caldeum": {
      "displayName": "City Of Caldeum",
      "aliases": ["Gates of Caldeum", "Caldeum Bazaar", "Sewers of Caldeum", "Flooded Causeway", "Ruined Cistern"],
      "arrivalRequiresExactMatch": false
    },
    "Ancient Waterway": {
      "displayName": "Ancient Waterway",
      "aliases": ["Western Channel Level 1", "Western Channel Level 2", "Eastern Channel Level 1", "Eastern Channel Level 2"],
      "arrivalRequiresExactMatch": true
    }
  },
  "Route": [
    { "from": "Southern Highlands", "to": "Northern Highlands" },
    { "from": "Stinging Winds", "to": "Battlefields" }
  ],
  "BlockingRules": [
    {
      "target": "Battlefields",
      "blockedWhen": ["Stinging Winds"],
      "allowedWhen": ["Black Canyon Mines"],
      "notification": "Clear {location} before teleporting next.",
      "summaryReason": "Stinging Winds blocks Battlefields; Black Canyon Mines is required."
    }
  ],
  "HotkeyBlockLocations": [
    "WhimsyDale",
    "Cave Of The Moon Clan Level 1",
    "Caverns of Frost Level 1"
  ],
  "DisplayNames": {
    "whimsydale": "WhimsyDale",
    "the battlefields": "Battlefields"
  }
}
```

Migration plan:
- Keep current hardcoded route dictionaries as the source of truth until the next reliability cycle is complete.
- Add a parser and validator that loads a proposed route config, reports unknown templates/coordinates, and compares generated route decisions against current hardcoded decisions without changing behavior.
- Move display-name normalization first because it is low risk and directly improves logs/notifications.
- Move aliases and exact-arrival requirements second, with tests for Ancient Waterway/channel and Pandemonium exact-match behavior.
- Move block/allow rules last, with route-failure-summary golden examples for Cathedral, Leoric's Passage, Gates/Caldeum, WhimsyDale, Cave Of The Moon Clan Level 1, Ancient Waterway channels, Stinging Winds, and Black Canyon Mines.

## System Notes

### Battle.net Window-Relative Scan Regions
- `BattleNetD3Tab`: `120,76,81,76`.
- `BattleNetPlayButton`: `16,1256,292,75`.
- Cached regions are interpreted as Battle.net-window-local pixel offsets.
- Full-screen image search remains as a safety fallback.
- Fallback diagnostics compare fallback detection point against resolved region center and log delta/distance plus a suggested same-size cached region.
- Static review confirmed these changes do not alter Diablo in-game Start Game logic, teleport logic, combat logic, repair/salvage logic, debug package generator, or diagnostics UI.

### Battle.net Launch Diagnostics
- Launch state is reset at Battle.net launch start.
- Launch startup readiness is based on a visible Battle.net window, not the background `Battle.net.exe` process. Process status is retained in logs only as `processObserved`.
- Launch startup logs configured/detected executable path selection, each launch attempt, retry count, elapsed time, and whether a launch-ready window was detected.
- Play button scanning logs each 100ms polling attempt and sends the click only from the current confidence-passing image match.
- Post-launch close status is based on whether the visible Battle.net window remains. Background/tray Battle.net processes may remain after Diablo launches and are logged as informational, not failures.
- `BattleNetPlayClickSentByApp` logs the app click point and timestamp when GoblinFarmer sends the Play click.
- `BattleNetManualPlaySuspected` logs when Diablo appears during launch flow without a recorded app Play click.
- `BattleNetLaunchSummary` records whether launch should be considered fully automated. It is fully automated only when the app recorded the Play click and Diablo launched afterward.
- `BattleNetStillOpenAfterDiabloLaunch` logs and captures evidence when the visible Battle.net window remains open after Diablo launches, then requests the existing safe Battle.net close path again.

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
- Witch Doctor held/channel input uses the same safe-start principle: the left-held channel starts only in a safe region, then no new mouse input is sent and the hold is not released solely because the cursor enters a combat no-click region.
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
- Failure screenshots record the latest failure screenshot type and log the saved Diablo path when capture succeeds.
- Failure screenshots now capture both Diablo and GoblinFarmer app evidence with matching timestamp/action filenames.
- Screenshot capture catches/logs failures and does not throw back into workflows.
- Capture falls back to the virtual screen when a Diablo client capture is unavailable, so Battle.net/startup failures can still produce evidence.

### Success Screenshot Capture
- Built on the same diagnostic screenshot infrastructure and controlled by the existing `Keep Debug Screenshots` checkbox.
- Captures paired Diablo/App screenshots for sparse workflow milestones only, not combat loops or polling loops.
- Current success milestones: Battle.net Play clicked, Diablo process detected, Start Game verified, teleport confirmed, repair complete, salvage complete/skipped, Leave Game main menu confirmed, and Exit Game complete.
- Success screenshots use the same runtime `Screenshots` folder as failure/debug screenshots, so existing retention cleanup controls storage growth.

### Session-Only Screenshot Packaging
- App startup writes `session-info.txt` in the runtime base directory with the local/UTC session start timestamp, process ID, and base directory.
- The debug package generator reads the latest session metadata when available and falls back to the latest log creation time for older runs.
- Screenshot inclusion uses file creation/write time relative to the session start. Older screenshots stay on disk until normal retention cleanup, but they are not copied into new debug packages.

### Debug Package Generator
- Script: `Scripts\create-debug-package.ps1`.
- Run from the project root with: `powershell -ExecutionPolicy Bypass -File .\Scripts\create-debug-package.ps1`.
- Creates `DebugPackages\GoblinFarmer_Debug_YYYYMMDD_HHMMSS.zip`.
- Includes latest app log if found, latest failure screenshots, latest success screenshots, latest normal debug screenshots if found, `AGENTS.md`, `Docs\Project_Status.md`, `Docs\TEST_CHECKLIST.md`, `Docs\TODO.md`, `git-status.txt`, `git-log.txt`, `debug-screenshot-manifest.txt`, and `debug-package-manifest.txt`.
- Includes `route-failure-summary.txt` generated from the latest log, covering route blocks, cancels, failed confirmations, allowed debug decisions, Start Game verification failures, screenshots, and likely explanations.
- Screenshot manifest records timestamp, success/failure outcome, workflow, action, and paired Diablo/App screenshot filenames.
- Package manifest records package path, latest log path, latest failure screenshot type, latest failure screenshot path, screenshot counts, and explicit build-artifact exclusions.
- Package manifest and console summary report package size, session start, session duration, total screenshots, success screenshots, failure screenshots, normal screenshots, and stale screenshot exclusions.

## Last Validation
- Built after adding Cave Of The Moon Clan Level 2 to Teleport Next hotkey route scans, mapping it back to the Southern Highlands checkpoint so the next route target remains Northern Highlands, and preserving specific Caldeum detected/display strings for Gates/City sublocations; build succeeded with 0 warnings and 0 errors.
- Reviewed `E:\GoblinFarmer\Logs\GoblinFarmer_20260603_105145.log`: the blocked 11:04 attempts showed the hotkey scan only considered Moon Clan Level 1 and blocked Northern Highlands from a false Level 1 detection; the 11:11-11:12 Caldeum entries showed Gates of Caldeum was being normalized/displayed as City Of Caldeum in diagnostic fields, so Caldeum detection now preserves the specific Gates/City/Bazaar/Sewers/Causeway/Cistern string while retaining City route grouping.
- Static review confirmed Cave Of The Moon Clan Level 1 remains the only Moon Clan cave level blocked by the hotkey route rule; Level 2 is scanned separately and allowed to continue to Northern Highlands.
- Built after fixing the Start Game stable-detection deadlock, manual-click loaded-state recovery, and Make New Game active-workflow guard; build succeeded with 0 warnings and 0 errors.
- Static review confirmed Battle.net launch, teleport/route logic, combat, Exit Game, bounty, repair, and salvage behavior were not changed.
- Built after adding Start Game detection attempt logging, 100ms stable-scan polling, and state-based click acceptance reasons; build succeeded with 0 warnings and 0 errors.
- Static review confirmed Start Game image assets/confidence thresholds, existing stable-button logic, existing loading/game-load checks, Battle.net launch behavior, teleport/route logic, combat, Exit Game, and bounty behavior were not changed.
- Built after adding Caverns of Frost Level 1 Teleport Next hotkey blocking and the README combat-settle hotkey-delay note; build succeeded with 0 warnings and 0 errors.
- Static review confirmed Caverns of Frost Level 2 remains an allowed Battlefields alias and that normal workflow/button teleports still bypass route blocking where intended.
- Built after Debug screenshot defaulting, startup form sizing, and Battle.net visible-window close/status polish; build succeeded with 0 warnings and 0 errors.
- Static review confirmed no changes were made to Battle.net launch retry behavior, Play button detection/click behavior, Diablo launch verification, combat, teleport, route, bounty, repair, or salvage logic.
- Built after adding Hotkeys visibility entries and Battle.net window-based startup retry/100ms Play polling; build succeeded with 0 warnings and 0 errors.
- Static review confirmed Battle.net launch readiness is based on the visible Battle.net window, not the background process, and that Diablo post-Play launch verification remains in the existing flow.
- Built after reviewing `GoblinFarmer_Debug_20260602_210617.zip`, adding the AppSettings config foundation, fixing already-at-queued-destination teleport start logging/dispatch, changing bounty menu Escape to automation-safe injected Escape, adding Start Game stable-button diagnostics, and reducing repair settle to the config default 50ms; build succeeded with 0 warnings and 0 errors.
- Built after reviewing `GoblinFarmer_Debug_20260602_204323.zip` and adding Cave Of The Moon Clan Level 1 blocking, Teleport Next fresh-scan route advancement, 75ms repair settle, and expanded bounty menu skip diagnostics; build succeeded with 0 warnings and 0 errors.
- Inspected `GoblinFarmer_Debug_20260602_204323.zip`: session summary reported Games Created `1`, Teleports Completed `15`, Blocked Teleports `0`, Failures `0`.
- Inspected `route-failure-summary.txt` from `GoblinFarmer_Debug_20260602_204323.zip`: Ruined Cistern allowed Ancient Waterway, Western Channel Level 2 allowed Ancient Waterway, Eastern Channel Level 2 allowed Stinging Winds, Black Canyon Mines allowed Battlefields, and Rakkis/Pandemonium progression reached Pandemonium Fortress Level 2.
- Inspected `debug-screenshot-manifest.txt` from `GoblinFarmer_Debug_20260602_204323.zip`: current-session success screenshot pairs existed for Ancient Waterway, Stinging Winds, Battlefields, Pandemonium Fortress Level 1, Pandemonium Fortress Level 2, New Tristram, Repair Complete, Salvage skipped, and Exit Game Complete.
- Reviewed repair timing in the latest package: `waitAfterArrivalMs=164`, `timeUntilRepairStationDetectedMs=1705`, `timeUntilRepairMenuOpenedMs=1878`, and `totalRepairWorkflowDurationMs=3313`; the post-arrival settle was shortened from 150ms to 75ms while keeping visual menu polling and retry behavior.
- Built after adding the optional missing-asset capture prompt and wiring common missing-template paths into it; build succeeded with 0 warnings and 0 errors.
- Built after optimizing repair station timing and adding workflow duration logs; build succeeded with 0 warnings and 0 errors.
- Built after adding WhimsyDale Teleport Next blocking, physical `2` Exit Game hotkey handling, corrected manual-button allowed behavior, and expanded repair timing logs; build succeeded with 0 warnings and 0 errors.
- Static review confirmed normal manual teleport buttons still pass `ignoreBlocking=true`, `source=Button`/`source=ButtonRetry` remain non-blocking by design, internal workflow teleports still pass `bypassFailsafe=true`, and injected combat key events still pass through the keyboard hook before physical hotkey handling.
- Built after adding Battle.net launch intervention diagnostics and workflow summary parsing; build succeeded with 0 warnings and 0 errors.
- Static review confirmed the changes preserve existing Battle.net scan fallback and tab-selection behavior, and only add launch state tracking, logs, screenshots, close-result diagnostics, and package summary parsing.
- Built after adding session-only screenshot package filtering and package size/session reporting; build succeeded with 0 warnings and 0 errors.
- Ran the debug package generator against the latest runtime evidence; it discovered 82 screenshots, treated 24 as current-session candidates, excluded 58 stale screenshots, selected 6 success screenshots under the package limit, and reported final package size `18.43 MB`.
- Inspected the generated package `DebugPackages\GoblinFarmer_Debug_20260602_113500.zip`; `debug-package-manifest.txt` contains package size/session/screenshot counts, `debug-screenshot-manifest.txt` contains only current-session success entries, and `route-failure-summary.txt` still generated successfully.
- Static review confirmed the changes are limited to session metadata, debug package filtering/reporting, route summary text, and documentation. Combat, route logic, Battle.net, Start Game, repair, salvage, and Exit Game behavior were not changed.
- Built after adding paired success/failure screenshot capture and screenshot manifest packaging; build succeeded with 0 warnings and 0 errors.
- Ran the debug package generator against the real current screenshot set; it created a timestamped package under `DebugPackages\`, included `debug-screenshot-manifest.txt`, selected existing failure/debug screenshots, and correctly reported no success screenshots from the old run.
- Ran a temporary synthetic paired screenshot test for one success and one failure pair; the debug package generator included both Diablo/App files and wrote the expected manifest entries. Temporary synthetic screenshots and package were removed afterward.
- Static review confirmed the changes are diagnostic-only: combat behavior, route logic, Battle.net logic, Start Game logic, repair logic, salvage logic, and Exit Game logic were not changed.
- Built after adding strict Ancient Waterway arrival confirmation, route/debug summary logging, Start Game verification failure summaries, and debug-package route summaries; build succeeded with 0 warnings and 0 errors.
- Ran `Scripts\create-debug-package.ps1 -MaxScreenshots 1 -MaxFailureScreenshots 3`; it created a timestamped debug package under `DebugPackages\` and included `route-failure-summary.txt`.
- Inspected the generated `route-failure-summary.txt` from the legacy 2026-06-02 route log; it summarized Caldeum/Waterway blocks, Stinging Winds cancellation, Waterway/channel blocks, Battlefields blocking, and Start Game verification failure evidence.
- Static review confirmed the code changes are scoped to teleport arrival confirmation, route/debug logging, Start Game verification logging, and debug package summary generation. Combat logic, Battle.net Play button cached scan region, repair/salvage, and Exit Game behavior were not changed.
- Built after recalibrating `BattleNetPlayButton` to `16,1256,292,75` and adding Witch Doctor-only held/channel no-click handling; build succeeded with 0 warnings and 0 errors.
- Static review confirmed the code changes are limited to the Battle.net Play cached region and Witch Doctor combat cursor handling/state. Demon Hunter, Monk, debug package generation, repair, salvage, teleport, Exit Game, and Start Game workflows were not changed.
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
- Consider optional voice recording attachments in debug packages later; do not implement audio recording until explicitly requested.
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
`%LOCALAPPDATA%\Programs\GoblinFarmer`

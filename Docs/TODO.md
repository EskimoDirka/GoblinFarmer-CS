# GoblinFarmer TODO

## High Priority (Current Development Phase)

### Teleport Route Reliability

* [x] Fix blocked notification wording so Gates of Caldeum displays as Gates of Caldeum instead of City Of Caldeum.
* [x] Fix blocked notification wording so Leoric's Passage displays as Leoric's Passage instead of Cathedral.
* [x] Fix Western Channel Level 1 hotkey routing block with explicit reason.
* [x] Fix Western/Eastern Channel alias behavior so channel levels do not complete Ancient Waterway waypoint arrival confirmation.
* [x] Fix Waterway button clearing next-target button color by requiring exact Ancient Waterway arrival confirmation before advancing state.
* [x] Keep Eastern Channel Level 2 as the only channel level that allows direct hotkey routing to Stinging Winds.
* [x] Block Ancient Waterway hotkey routing away from Ancient Waterway with explicit reason.
* [x] Fix Western Channel Level 2 corrective target to Ancient Waterway instead of direct Stinging Winds.
* [x] Force a fresh raw current-location scan on every teleport hotkey press before choosing allowed/blocked routing.
* [x] Use raw hotkey scan location as the primary blocking/route-decision location so aliases do not override channel sublocations.
* [x] Fix Eastern Channel Level 1 hotkey routing block with explicit reason.
* [x] Fix Stinging Winds blocking rules.
* [x] Fix manual teleport button retry after failed/interrupted button teleports.
* [x] Fix ButtonRetry so manual retries bypass teleport blocking.
* [x] Keep normal manual teleport buttons intentionally allowed by bypassing route blocking.
* [x] Log and ignore same-button clicks while waiting for teleport arrival confirmation.
* [x] Add WhimsyDale to Teleport Next hotkey blocking with WhimsyDale display wording.
* [x] Add Cave Of The Moon Clan Level 1 to Teleport Next hotkey blocking with the raw location name in logs/notifications.
* [x] Add Teleport Next route advancement when the fresh hotkey scan already matches the queued route destination.
* [x] Fix Teleport Next already-at-queued-destination path so it starts the following route teleport instead of only updating button colors.
* [x] Include queued-target templates in the Teleport Next fresh hotkey scan so already-at-queued detection does not return `Unknown` for normal route destinations.
* [x] Add `AlreadyAtQueuedDestinationCheck` diagnostics before the already-at skip/advance branch.
* [x] Verify Black Canyon Mines allows Battlefields.
* [x] Verify Ruined Cistern allows Ancient Waterway and City Of Caldeum/Sewers/Flooded Causeway remain blocked with clear summary explanations.
* [x] Validate Teleport Next blocks from Cave Of The Moon Clan Level 1 and the notification/logs display `Cave Of The Moon Clan Level 1`.
* [ ] Validate Teleport Next blocks from WhimsyDale and the notification/logs display `WhimsyDale`.
* [x] Validate Teleport Next route advancement logs `AlreadyAtQueuedDestinationDetected`, skips the queued destination, and actually starts teleporting to `newRequestedTarget`.
* [x] Validate manual teleport buttons remain allowed and log `source=Button`, `ignoreBlocking=True`, and blocking skipped.
* [ ] Verify all route logic matches Project_Status.md.

### Start Game Reliability

* [ ] Investigate Start Game image recognition failures.
* [ ] Determine if cursor is interfering with Start Game image detection.
* [x] Add Start Game diagnostic logging.
* [x] Capture debug screenshots when Start Game verification fails.
* [x] Add stable Start Game button confirmation before clicking and before treating Leave Game as back at main menu.
* [x] Validate Make New Game from already-in-game succeeded in `GoblinFarmer_Debug_20260602_213950.zip`.
* [ ] Continue monitoring Make New Game / Start Game consistency across fresh sessions.

### Repair And Salvage Reliability

* [x] Preserve repair plus salvage integration while tuning repair station timing.
* [x] Reduce New Tristram post-arrival fixed settle delay to the minimum safe input-settle delay.
* [x] Replace fixed post-blacksmith-click delay with visual polling for the blacksmith menu.
* [x] Preserve retry logic when the blacksmith menu does not open.
* [x] Log time spent waiting after arrival.
* [x] Log time until repair station/blacksmith menu is detected.
* [x] Log time until repair menu is opened.
* [x] Log total repair workflow duration.
* [x] Validate optimized repair station flow in a live Exit Game / Make New Game prep run.
* [x] Reduce post-New-Tristram repair settle from 150ms to 75ms after reviewing `GoblinFarmer_Debug_20260602_204323.zip` timings.
* [x] Add config-controlled repair settle and polling interval settings.
* [x] Reduce default post-New-Tristram repair settle to 50ms after reviewing `GoblinFarmer_Debug_20260602_210617.zip` timings.
* [x] Accept latest repair timing as good enough after `GoblinFarmer_Debug_20260602_213950.zip`; preserve reliability over further speed changes.

### Testing

* [ ] Validate `ExtendedRightMenuNoClickRegion`: hover over the lower-right menu, confirm combat continues, cursor does not move, clicks are blocked with `blockReason=ExtendedRightMenuNoClickRegion`, and clicks resume after moving away.
* [ ] Validate combat no-click suppression mode: logs show `combatInputMode=PhysicalCursorNoClickSuppression`, `clickSendMethod=suppressed`, and Demon Hunter key rotation continues while mouse clicks are suppressed.
* [ ] Validate Demon Hunter right-hold mode: start right-hold in a safe area, hover over no-click UI, confirm no new right click is sent, UI is not clicked, and logs show `combatInputMode=PhysicalCursorHeldFromSafeRegion`.
* [ ] Validate Demon Hunter shared cursor-loop suppression: while right-hold is active over no-click UI, logs show `DemonHunterRightHeldNoClickSuppressionActive` and diagnostics show `DemonHunterRightHeld=True`.
* [ ] Validate Witch Doctor held/channel mode: start in a safe world area, move into SkillBar/ResourceGlobe/BottomRightButtons/ExtendedRightMenu no-click regions, confirm held input stays active without UI clicks, keyboard/Hex timers continue, and logs show `WitchDoctorHeldInputNoClickSuppressionActive`.
* [ ] Validate combat keyboard filtering: while combat is active, physical `1`/`2` are suppressed, injected automation key rotation continues, and logs show physical `2` suppression if pressed by the user.
* [x] Validate physical `2` starts Exit Game only when combat is inactive.
* [x] Validate physical `2` is suppressed during combat, logs `exitGameHotkeySuppressed=True`, and does not block injected combat key events.
* [x] Complete full route test from Southern Highlands through Pandemonium Fortress Level 2.
* [ ] Validate `route-failure-summary.txt` after the next fresh route run and confirm each block/cancel/failure includes target, source, locations, route state, screenshot, and likely explanation.
* [ ] Validate `debug-screenshot-manifest.txt` after the next fresh run and confirm success/failure milestones have paired Diablo/App screenshots.
* [ ] Validate `session-info.txt` after the next app launch and confirm debug packages filter screenshots from that session start.
* [ ] Retest manual Ancient Waterway from Western/Eastern Channel levels and confirm current/next/retry state is preserved until exact Ancient Waterway confirmation.
* [x] Validate Western Channel Level 2 hotkey routes to Ancient Waterway and logs `WesternChannelLevel2AllowsAncientWaterway`.
* [ ] Validate hotkey block notifications display raw/blocking location names for Leoric's Passage and Gates of Caldeum.
* [ ] Complete interrupted teleport testing.
* [ ] Test failed/interrupted Royal Crypts button retry from Cathedral Level 1 and confirm retry preserves Cathedral/Royal Crypts state, bypasses manual-button blocking, and does not advance until arrival confirmation.
* [ ] Confirm Battle.net Play button is found in recalibrated window-relative region `16,1256,292,75` before full-screen fallback.
* [ ] Validate Battle.net launch diagnostics distinguish app Play click sent, Battle.net Play click accepted, Diablo launched because of the accepted app click, and manual Play suspected.
* [ ] Validate Battle.net Play click waits for 1500ms stable Play detection at a same/similar point while Battle.net remains foreground before clicking.
* [ ] Validate Battle.net Play click waits 500ms after stable detection, immediately reconfirms Play exists, then sends the click.
* [ ] Validate `BattleNetPlayClickInputSequence` logs the click point, 100ms move settle, and 75ms mouse down-to-up timing.
* [ ] Validate `BattleNetPlayClickRetry` retries up to two more times with 1500ms delay when Play remains visible and Diablo has not started after an unaccepted app click.
* [ ] Validate `BattleNetPlayClickSentByApp` appears immediately after the app sends the mouse click and is not treated as a successful launch by itself.
* [ ] Validate `BattleNetPlayClickAccepted` appears only after Battle.net UI transition, Battle.net window/process transition, or Diablo process start confirms the app Play click was accepted.
* [ ] Validate `BattleNetManualPlaySuspected` appears if Diablo launches without `battleNetPlayClickAcceptedByBattleNet=True`, including when the app sent a click but acceptance was not verified.
* [ ] Validate `BattleNetStillOpenAfterDiabloLaunch` appears if Battle.net remains open after Diablo launches.
* [ ] Validate `BattleNetPostLaunchCloseSummary` marks Diablo launch successful only after an accepted app Play click while reporting Battle.net close requested/succeeded/failed/timed out separately.
* [ ] Validate debug package workflow output reports app play click sent, app play click accepted, manual play suspected, Diablo launched, and Battle.net still open after launch as separate fields.
* [ ] Validate route-failure-summary/debug workflow output has one Battle.net launch verdict and one post-launch close verdict, with no conflicting duplicate entries for the same event.
* [ ] Validate `CaptureDebugScreenshot(actionName, reason, region)` saves failed/suspicious detection captures under `debug-screenshots/<category>` with timestamp, action, reason, and optional region in the filename.
* [ ] Validate debug screenshot logs include `DebugScreenshotCaptured` metadata with action, reason, path, region, and window title.
* [ ] Validate debug screenshot throttling logs `DebugScreenshotSkipped` and does not capture the same action/reason more than once every 10 seconds or more than 50 screenshots per app run.
* [ ] Validate debug packages include current-session `debug-screenshots` folders and report the debug screenshot count.
* [ ] Validate missing screenshot/template prompt logs `MissingAssetDetected` with asset, flow, scan region, and app/game state.
* [ ] Validate missing screenshot/template prompt is suppressed during active combat.
* [ ] Validate accepting the missing-asset prompt captures the Diablo window or known scan region into the expected Images folder.
* [ ] Validate declining the missing-asset prompt logs `MissingAssetManualCaptureSkipped` and continues safely.
* [ ] If Battle.net Play button falls back, inspect fallback region diagnostics and update `BattleNetPlayButton` from the suggested same-size cached region if the fallback point is outside the resolved region.
* [x] Complete Exit Game workflow testing.
* [x] Confirm Exit Game no longer causes a post-Diablo desktop right-click or app close.
* [x] Complete Repair workflow testing.
* [x] Validate optimized repair station timing after New Tristram arrival remained reliable in `GoblinFarmer_Debug_20260602_213950.zip`.
* [x] Confirm repair logs include `waitAfterArrivalMs`, `timeUntilRepairStationDetectedMs`, `timeUntilRepairMenuOpenedMs`, and `totalRepairWorkflowDurationMs`.
* [x] Complete Salvage workflow testing.
* [x] Add bounty menu diagnostics for `BountyMenuDetected`, `BountyMenuEscapeSent`, `BountyMenuEscapeSkipped`, and skip reasons.
* [x] Make bounty menu auto-close send automation-safe injected Escape so it does not cancel automation.
* [x] Log bounty menu scan image path, `Bounty Menu Scan Region.png` path, reference/screen region, best confidence, threshold, and detection source.
* [x] Capture low-confidence bounty menu scan-region screenshots for visual comparison against the configured scan-region asset.
* [x] Treat narrow bounty menu near-matches below `0.780` as `RegionNearMatch` instead of ignoring repeated `0.777` detections.
* [x] Refresh `Bounty Menu Title.png` from the latest live gold `BOUNTY COMPLETE` popup title.
* [x] Allow detected Bounty Complete popup close during combat by sending automation-safe injected Escape without stopping combat automation.
* [ ] Validate passive bounty menu detection logs `BountyMenuDetected` and sends Escape when Diablo is foreground, including while combat is active.
* [ ] Validate bounty menu Escape throttling logs `BountyMenuEscapeSkipped` for inactive Diablo, missing image data, low confidence, or throttle.
* [ ] Validate the configured `Images\Combat\Bounty Menu Scan Region.png` green-box region is used in live logs via `BountyMenuScanResult`.
* [ ] Validate paired App screenshot diagnostics log visibility, minimized/foreground state, possible occlusion, and capture bounds.

### Configuration And Release Readiness

* [x] Add centralized `Config/AppSettings.json`.
* [x] Auto-create config with safe defaults when missing.
* [x] Log loaded config values on startup.
* [x] Add config support for Debug, UI, Repair, and Teleport sections.
* [x] Hide Diagnostic Overlay and Route Inspector unless enabled by config.
* [x] Gate debug screenshots and missing-asset prompts through config.
* [x] Keep route-rule config as design review only; do not migrate live route rules yet.
* [ ] Validate a fresh release-style run with diagnostic overlay and route inspector hidden by default.
* [ ] Validate enabling `ShowDiagnosticOverlay` and `ShowRouteInspector` restores the diagnostic tabs.
* [ ] Validate disabling `EnableDebugScreenshots` suppresses success/failure/debug screenshot capture.
* [ ] Validate disabling `EnableMissingAssetPrompts` logs missing assets but suppresses manual prompts.

---

## Debugging Improvements

### Diagnostic Overlay

Status: Implemented as a read-only WinForms panel on the main form.

Shows:

* [x] Raw Location.
* [x] Normalized Location.
* [x] Display Location.
* [x] Blocking Location.
* [x] Current Teleport Target.
* [x] Next Teleport Target.
* [x] Queued Retry Target.
* [x] Last Requested Target.
* [x] Failed/Interrupted retry state.
* [x] Route State.
* [x] Combat State.
* [x] Failure Counter.
* [x] Diablo Running Status.
* [x] Active Workflow.
* [x] Last Log File.
* [x] Screenshot Count.
* [x] Log Count.
* [x] Debug Package Path when available.

Notes:

* Refreshes through the existing status timer.
* Uses existing runtime state and file counts only.
* Does not run extra image-recognition scans.

### Route State Inspector

Status: Implemented as a read-only `Route State` diagnostics tab beside the compact overlay.

Shows:

* [x] Raw detected location.
* [x] Normalized app location.
* [x] Display location.
* [x] Blocking location.
* [x] Current button location.
* [x] Next button location.
* [x] Queued teleport target.
* [x] Retry queued target.
* [x] Last requested teleport target.
* [x] Last teleport source.
* [x] Last blocking decision.
* [x] Last blocking reason.
* [x] Last route decision output.
* [x] Whether currently waiting for arrival confirmation.
* [x] Waiting confirmation target.
* [x] Whether failed/interrupted retry state is active.
* [x] Failure counter.
* [x] Latest log path.
* [x] Latest debug screenshot path.
* [x] Screenshot count.
* [x] Log count.
* [x] Active workflow.
* [x] Diablo running status.
* [x] Diablo focused/active status.

Notes:

* Refreshes through the existing status timer.
* Uses existing route/session state and small passive diagnostic fields.
* Does not run extra image-recognition scans or activate Diablo.
* Does not change route, combat, Battle.net, Start Game, repair, salvage, normalization, or notification logic.

### Debug Package Generator

Status: Implemented and validated with `Scripts\create-debug-package.ps1`.

Run from the project root:

`powershell -ExecutionPolicy Bypass -File .\Scripts\create-debug-package.ps1`

Optional screenshot limits:

`powershell -ExecutionPolicy Bypass -File .\Scripts\create-debug-package.ps1 -MaxScreenshots 10 -MaxFailureScreenshots 10`

Requirements:

* [x] Include latest log.
* [x] Include latest failure screenshots.
* [x] Include latest success screenshots.
* [x] Include latest normal debug screenshots.
* [x] Include Project_Status.md.
* [x] Include TEST_CHECKLIST.md.
* [x] Include TODO.md and AGENTS.md.
* [x] Include current git status.
* [x] Include recent git log.
* [x] Include debug-package-manifest.txt.
* [x] Include debug-screenshot-manifest.txt.
* [x] Include package size, session start, session duration, total screenshot count, success screenshot count, failure screenshot count, and stale screenshot exclusions in debug-package-manifest.txt.
* [x] Include latest screenshot failure type when available.
* [x] Include route-failure-summary.txt.
* [x] Use package filename timestamp with seconds.
* [x] Display clear console summary.
* [x] Warn for missing optional files or folders.
* [x] Exclude bin and obj folders.
* [x] Avoid huge build artifacts.
* [x] Document how to run the script.
* [x] Export zip package.
* [x] Filter packaged screenshots to the current GoblinFarmer session only.
* [x] Exclude stale screenshots from previous sessions while preserving retention cleanup behavior.

Example output:

DebugPackages/

* GoblinFarmer_Debug_YYYYMMDD_HHMMSS.zip

### Enhanced Failure Logging

* [ ] Save screenshots automatically when image recognition fails.
* [x] Add optional missing screenshot/template capture prompt when an expected image asset is missing.
* [x] Log missing asset name, calling flow, scan region when known, and current app/game state.
* [x] Suppress missing-asset prompts during active combat.
* [x] Save accepted missing-asset captures to the expected Images folder when the target path is known.
* [x] Log missing-asset prompt accept/skip outcomes.
* [x] Save screenshots automatically when teleports fail.
* [x] Save screenshots automatically when Start Game fails.
* [x] Save screenshots automatically when Battle.net Play button is not found.
* [x] Save screenshots automatically when Diablo tab is not found.
* [x] Save screenshots automatically when repair station is not found.
* [x] Save screenshots automatically when repair fails.
* [x] Save screenshots automatically when workflows are cancelled.
* [x] Save screenshots automatically for unexpected exceptions.
* [x] Show latest screenshot path in diagnostics.
* [x] Show latest failure screenshot type in diagnostics.
* [x] Log route failure summaries for teleport blocks, cancels, failed confirmations, allowed route-debug decisions, and Start Game verification failures.
* [x] Capture paired Diablo/App screenshots for major workflow failures.
* [x] Capture paired Diablo/App screenshots for `BattleNetPlayButtonNotClickedByApp`.
* [x] Capture paired Diablo/App screenshots for `BattleNetManualPlaySuspected`.
* [x] Capture paired Diablo/App screenshots for `BattleNetStillOpenAfterDiabloLaunch`.
* [ ] Record image name, confidence, scan region, threshold, and best match information.

### Success Screenshot Capture

Status: Implemented for sparse workflow milestones; needs manual validation during a fresh run.

* [x] Capture paired Diablo/App screenshots for Battle.net Play clicked.
* [x] Capture paired Diablo/App screenshots for Diablo process detected.
* [x] Capture paired Diablo/App screenshots for Start Game verified.
* [x] Capture paired Diablo/App screenshots for teleport confirmed.
* [x] Capture paired Diablo/App screenshots for repair complete.
* [x] Capture paired Diablo/App screenshots for salvage complete or skipped.
* [x] Capture paired Diablo/App screenshots for Leave Game main menu confirmed.
* [x] Capture paired Diablo/App screenshots for Exit Game complete.
* [x] Reuse the existing `Screenshots` directory and retention cleanup.
* [x] Avoid combat loops and rapid polling loops.
* [ ] Validate fresh runtime success screenshots in a real route run.

### Package Size And Relevance

Status: Implemented in debug package generation; needs validation after the next full fresh run.

* [x] Track application session start in runtime `session-info.txt`.
* [x] Prefer session metadata for screenshot cutoff.
* [x] Fall back to latest log creation time for older runs without session metadata.
* [x] Exclude screenshots older than session start from packages.
* [x] Report package size in console output.
* [x] Report package size in `debug-package-manifest.txt`.
* [x] Report stale screenshot exclusion count.
* [x] Keep existing screenshot retention cleanup unchanged.
* [ ] Compare package size before/after on the next full route debug package.

Failure screenshot types:

* [x] TeleportBlocked.
* [x] TeleportInterrupted.
* [x] TeleportConfirmationTimeout.
* [x] StartGameButtonNotFound.
* [x] StartGameVerificationFailed.
* [x] BattleNetPlayButtonNotFound.
* [x] DiabloTabNotFound.
* [x] RepairStationNotFound.
* [x] RepairFailed.
* [x] WorkflowCancelled.
* [x] UnexpectedException.

### Runtime Input Cleanup

Status: Implemented; needs manual Exit Game validation.

* [x] Track runtime-held left mouse, right mouse, and Shift state.
* [x] Release only tracked held inputs during cleanup while Diablo is available.
* [x] Clear tracked held-input state without sending mouse events after Diablo closes.
* [x] Log held left/right/Shift state before cleanup.
* [x] Log cleanup reason and Diablo window/rect availability.
* [x] Log whether left/right/Shift release was sent or skipped.
* [x] Avoid duplicate cleanup calls generating extra mouse events.
* [x] Preserve combat input cleanup behavior while Diablo is running.
* [x] Preserve repair-station coordinate-based clicks.
* [ ] Validate with a real Exit Game run and confirm no desktop right-click/app close after Diablo exits.

### Diagnostic Tools

* [ ] Add Test Mode panel.
* [ ] Add Current Location detection button.
* [ ] Add Map detection button.
* [ ] Add Battle.net Play test button.
* [ ] Add Start Game test button.
* [ ] Add image recognition test utility.

### Combat Safety

* [x] Add named combat no-click region for the extended lower-right hover menu.
* [x] Keep the extended hover menu handling rectangle-based; no image recognition.
* [x] Keep combat running when the cursor is inside the hover menu region.
* [x] Do not move the cursor when the hover menu appears.
* [x] Log `blockReason=ExtendedRightMenuNoClickRegion` when this region blocks combat clicks.
* [x] Confirm current C# combat mouse input uses physical cursor `mouse_event`, not `SendInput`, `PostMessage`, or `SendMessage`.
* [x] Keep no-click regions protected by suppressing physical mouse clicks in unsafe UI zones.
* [x] Continue Demon Hunter non-mouse key rotation while mouse clicks are suppressed in no-click regions.
* [x] Compare old Python input path: combat mouse uses physical Win32 `mouse_event`, while Demon Hunter right mouse starts in a safe region and remains held.
* [x] Align C# Demon Hunter right-hold with Python pattern by keeping the safe-started right hold active through no-click hover regions.
* [x] Add Demon Hunter-only `DemonHunterRightHeldNoClickSuppressionActive` logging for shared cursor-loop left-click suppression while right mouse remains held.
* [x] Add Witch Doctor-only safe-start held/channel behavior so held input stays active through no-click regions without sending new mouse clicks.
* [x] Show Demon Hunter right-held state in diagnostics so combat does not appear stopped while right-hold is active.
* [x] Match old Python keyboard-hook behavior for combat-relevant number keys by suppressing physical `1`/`2` during combat while allowing injected automation key events.
* [x] Add physical `2` Exit Game hotkey path for non-combat use while preserving combat precedence.
* [x] Log combat input mode and click send method for allowed/suppressed combat clicks.
* [ ] Manually validate that unrelated workflows still work after the combat-only no-click update.

---

## Development Automation

### Publish Script

Create:

scripts/publish-release.ps1

Requirements:

* Build Release.
* Publish Release.
* Deploy to D:\GoblinFarmer.
* Verify Images folder exists.
* Verify executable exists.
* Verify icon exists.
* Display publish summary.

### Git Helper Script

Create:

scripts/git-sync.ps1

Requirements:

* git status
* git pull
* dotnet build
* stop on build failure
* git add
* git commit
* git push

### Asset Validation Tool

Create image asset validator.

Checks:

* Missing image files.
* Duplicate image names.
* Missing scan region files.
* Missing coordinate files.
* Images not marked Content.
* Images not copied to output.

---

## Architecture Improvements

### Route Rules Configuration

Move route logic into configuration.

Possible file:

Config/RouteRules.json

Benefits:

* No recompilation required for route changes.
* Easier testing.
* Easier maintenance.
* Easier future expansion.

### Battle.net Scan Regions

* [x] Convert Battle.net tab and Play button scan regions to window-relative coordinates.
* [x] Interpret cached `BattleNetD3Tab` and `BattleNetPlayButton` regions as Battle.net-window-local pixel offsets.
* [x] Update only `BattleNetPlayButton` to diagnostic-suggested window-local region `16,1256,292,75`.
* [x] Add BattleNetPlayButton fallback diagnostics showing window rect, cached region, resolved screen region, fallback point, expected center, delta, distance, inside/outside assessment, and suggested same-size cached region.
* [x] Restore/focus Battle.net before resolving scan regions.
* [x] Keep fullscreen search as fallback.
* [x] Preserve existing scan-region cache/reference format.
* [x] Log cached region, Battle.net window rect, resolved screen region, window-relative result, outside-window warnings, and fallback search.
* [x] Track Battle.net app Play click state, click point, click timestamp, Diablo launch after app click, and Diablo launch without app click.
* [x] Log suspected manual Battle.net Play intervention when Diablo launches without a recorded app Play click.
* [x] Log and capture Battle.net still-open-after-launch evidence, then request existing safe Battle.net close behavior.
* [ ] Manually test Battle.net detection when fullscreen, windowed, moved, and on another monitor.

### Folder Structure Cleanup

Future cleanup task:

Current:
D:\D3\Projects\GoblinFarmer\GoblinFarmer\GoblinFarmer

Goal:
Simplify repository structure after debugging phase is complete.

---

## User Experience Improvements

### Session Summary Export

Create session export feature.

Output:

Sessions/

* Session_YYYYMMDD.md

Include:

* Games created.
* Teleports completed.
* Failures.
* Runtime.
* Last known issue.
* Relevant screenshots.

### Debug Package Attachments

Future possibility:

* [ ] Include optional voice recording attachments in debug packages.
* [ ] Do not implement audio recording unless explicitly requested.

### Notifications

* [ ] Verify all blocked notifications are consistent.
* [ ] Add "Already Here" notification.
* [ ] Standardize notification duration.
* [ ] Standardize notification styling.

---

## Documentation

### AGENTS.md

* [ ] Review periodically.
* [ ] Keep debugging workflow current.
* [ ] Keep development workflow current.

### Project_Status.md

* [ ] Update after major testing sessions.
* [ ] Keep route logic authoritative.
* [ ] Record known issues.
* [ ] Record latest verified working behavior.

### TEST_CHECKLIST.md

* [ ] Add new tests as features are added.
* [ ] Keep manual testing procedures current.

---

## Nice-To-Have Future Features

### Visual Route Viewer

* [ ] Display current route.
* [ ] Display next target.
* [ ] Display blocked state.
* [ ] Display current detected location.

### Route Replay

* [ ] Save route history.
* [ ] Save teleport history.
* [ ] Save failed teleport history.

### Developer Dashboard

* [ ] Live location display.
* [ ] Live route display.
* [ ] Image recognition diagnostics.
* [ ] Session statistics.
* [ ] Current workflow state.

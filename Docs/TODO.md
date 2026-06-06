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
* [x] Add Caverns of Frost Level 1 to Teleport Next hotkey blocking while keeping Caverns of Frost Level 2 allowed to Rakkis Crossing.
* [x] Add Teleport Next route advancement when the fresh hotkey scan already matches the queued route destination.
* [x] Continue Teleport Next from an already-reached queued destination to that destination's next route target in the same hotkey press when a next target exists.
* [x] Fix Teleport Next already-at-queued-destination path so it starts the following route teleport instead of only updating button colors.
* [x] Include queued-target templates in the Teleport Next fresh hotkey scan so already-at-queued detection does not return `Unknown` for normal route destinations.
* [x] Add `AlreadyAtQueuedDestinationCheck` diagnostics before the already-at skip/advance branch.
* [x] Verify Black Canyon Mines allows Battlefields.
* [x] Verify Ruined Cistern allows Ancient Waterway and City Of Caldeum/Sewers/Flooded Causeway remain blocked with clear summary explanations.
* [x] Validate Teleport Next blocks from Cave Of The Moon Clan Level 1 and the notification/logs display `Cave Of The Moon Clan Level 1`.
* [x] Validate Teleport Next blocks from Caverns of Frost Level 1 and the notification/logs display `Caverns of Frost Level 1`.
* [x] Validate Teleport Next from Caverns of Frost Level 2 still allows Rakkis Crossing.
* [ ] Validate Teleport Next blocks from WhimsyDale and the notification/logs display `WhimsyDale`.
* [x] Validate Teleport Next route advancement logs `AlreadyAtQueuedDestinationDetected`, skips the queued destination, and actually starts teleporting to `newRequestedTarget`.
* [ ] Live-validate walking from Battlefields to Rakkis Crossing, then pressing Teleport Next, updates route/button state and starts Pandemonium Fortress Level 1 without requiring a second hotkey press.
* [x] Validate manual teleport buttons remain allowed and log `source=Button`, `ignoreBlocking=True`, and blocking skipped.
* [x] Verify release-facing route logic matches Project_Status.md.

### Start Game Reliability

* [x] Investigate Start Game image recognition failures from the v1.3 debug package evidence.
* [x] Resolve the Start Game stable-detection deadlock without requiring pixel-perfect match stability.
* [x] Add Start Game diagnostic logging.
* [x] Capture debug screenshots when Start Game verification fails.
* [x] Add stable Start Game button confirmation before clicking and before treating Leave Game as back at main menu.
* [x] Add Battle.net-style Start Game detection attempt logging, 100ms stable-scan polling, and state-based click acceptance reasons.
* [x] Fix Start Game stable detection so repeated in-tolerance matches are accepted by consecutive scan count or stable duration instead of being reset as unstable.
* [x] Add manual Start Game recovery: if loaded-game state appears while waiting for Start Game, continue the Make New Game flow and log manual click suspicion.
* [x] Add `WorkflowAlreadyActive` status/logging when Make New Game is clicked while another workflow is active.
* [x] Validate Make New Game from already-in-game succeeded in a debug package.
* [ ] Continue broad post-release monitoring of Make New Game / Start Game consistency across unusual display and cursor conditions.

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
* [x] Reduce post-New-Tristram repair settle from 150ms to 75ms after reviewing debug package timings.
* [x] Add config-controlled repair settle and polling interval settings.
* [x] Reduce default post-New-Tristram repair settle to 50ms after reviewing debug package timings.
* [x] Accept latest repair timing as good enough after reviewing a debug package; preserve reliability over further speed changes.
* [x] Reduce salvage confirmation wait/post-slot delay and add per-slot salvage timing logs after reviewing `GoblinFarmer_Debug_20260606_040604.zip`.
* [x] Replace the salvage per-slot confirmation wait helper with a fast bounded confirmation probe after reviewing `GoblinFarmer_Debug_20260606_042740.zip`.
* [x] Add Kadala right-click timing diagnostics and reduce the Up Arrow click cadence.
* [x] Add a faster salvage slot-click helper plus shorter confirmation probe after reviewing `GoblinFarmer_Debug_20260606_044942.zip`.
* [ ] Investigate repeated New Tristram repair workflow cancellations where the blacksmith menu did not become visible after one or two sent clicks.
* [ ] Validate faster salvage slot timing in the next repair/salvage run; latest pre-fix package showed 10 slots salvaged and total salvage time around 3858ms.
* [ ] Validate Kadala feels faster and logs `Kadala timing` start/active/stop summaries without causing unsafe clicks.

### Testing

* [x] Review `GoblinFarmer_Debug_20260606_034050.zip` for Observation Mode pre-auto-count readiness issues.
* [x] Add explicit Last Observation UI update/clear diagnostics for continuous scanner state.
* [x] Suppress stale visible journal Engaged-line signatures after the freshness window until they disappear or change.
* [x] Require fresh same-area observation evidence before resolved-area manual `X` can accept an `Unknown` goblin type by default.
* [x] Review `GoblinFarmer_Debug_20260606_040604.zip` for the fresh Killed journal/manual gate issue.
* [x] Allow manual `X` refresh to accept fresh same-area Killed-only journal evidence without enabling auto-count.
* [x] Track first-seen/resolved-area state for Killed journal lines and suppress stale Killed lines.
* [x] Review `GoblinFarmer_Debug_20260606_042740.zip` for stale journal reuse, Reset Stats cleanup, blocked-area priority, and town workflow timing.
* [x] Make Engaged/Killed journal freshness signatures area-insensitive and match-point-insensitive so old visible journal text cannot refresh itself after moving areas or shifting rows.
* [x] Review `GoblinFarmer_Debug_20260606_044942.zip` for proven stale-journal suppression and accepted manual-count Last Observation display sync.
* [x] Publish accepted manual `X` counts into Last Observation display state and briefly preserve that display from no-candidate scanner clears.
* [x] Review `GoblinFarmer_Debug_20260606_050819.zip` for accepted manual-count display hold behavior, stale/block/reset regressions, package size, and salvage timing.
* [x] Preserve `ManualHotkey` / `Counted` Last Observation from Journal/Minimap scanner overwrites during the 5-second manual-count display hold.
* [x] Update AGENTS.md to require next test steps and commit/push follow-up unless otherwise specified.
* [x] Add AGENTS workflow-maintenance guidance backed by `Docs/Worflow blocklist.md`.
* [x] Prepare v1.4.0 release metadata, release notes, README text, release checklist, and Inno Setup script metadata.
* [x] Create the v1.4.0 self-contained release EXE and Inno Setup installer with matching version metadata.
* [ ] Live-validate Last Observation visibly updates to `ManualHotkey` / `Counted` immediately after accepted manual `X` counts and remains visible for the full 5-second count notification, with scanner overwrites logging `LastObservationUpdateSkippedDuringManualHold`.
* [ ] Live-validate stale Treasure Goblin journal lines stay ignored and do not keep producing eligible observations after moving areas.
* [ ] Live-validate manual `X` in a resolved allowed area with no fresh observation suppresses with `NoFreshObservation` and does not increment GoblinCount.
* [ ] Live-validate manual `X` with a fresh same-area observation/candidate still counts and reuses the goblin type.
* [x] Live-validate immediate manual `X` after a Killed journal line counts the recognized goblin type and logs `JournalKilledAcceptedFreshManual`.
* [ ] Live-validate old visible Killed journal lines suppress with `JournalKilledIgnoredStale` after the freshness window.
* [ ] Live-validate old visible Engaged journal lines suppress with `JournalEngagedIgnoredStale` after the freshness window and do not create a fresh same-area observation after moving.
* [ ] Live-validate `Reset Stats` clears suppression/observation state and allows a count again only after fresh evidence exists.
* [ ] Live-validate blocked manual-count areas still suppress with `BlockedArea` even if goblin evidence is visible.
* [x] Live-validate salvage is faster with `Salvage timing` logs and no missed confirmation prompts.
* [ ] Live-validate Kadala timing/feel once blood shards are available.
* [ ] Validate `ExtendedRightMenuNoClickRegion`: hover over the lower-right menu, confirm combat continues, cursor does not move, clicks are blocked with `blockReason=ExtendedRightMenuNoClickRegion`, and clicks resume after moving away.
* [ ] Validate combat no-click suppression mode: logs show `combatInputMode=PhysicalCursorNoClickSuppression`, `clickSendMethod=suppressed`, and Demon Hunter key rotation continues while mouse clicks are suppressed.
* [ ] Validate Demon Hunter right-hold mode: start right-hold in a safe area, hover over no-click UI, confirm no new right click is sent, UI is not clicked, Shift+Left maintenance skips unsafe injected mouse clicks without stopping combat, and logs show `combatInputMode=PhysicalCursorHeldFromSafeRegion`.
* [ ] Validate Demon Hunter shared cursor-loop suppression: while right-hold is active over no-click UI, logs show `DemonHunterRightHeldNoClickSuppressionActive` and diagnostics show `DemonHunterRightHeld=True`.
* [ ] Validate form preference persistence: choose Demon Hunter, toggle each Hotkeys checkbox, close/reopen the app, and confirm `User.CombatProfile=demon_hunter` plus the Hotkeys checkbox fields restore the form state.
* [ ] Validate Witch Doctor mouse-wheel/cursor-click mode: select Witch Doctor, toggle combat on, confirm logs show `WitchDoctorMouseWheelLoopStarted`, `WitchDoctorCursorChangeLeftClickLoopStarted`, `combatClass=witch_doctor`, `combatInputMode=MouseWheelScroll`, repeated `WitchDoctorMouseWheelScrollSent`, `WitchDoctorCursorChangeLeftClickCheck`, key loop order `2,3,1`, `heldLeftMode=false`, `heldRightMode=false`, discrete `WitchDoctorCursorChangeLeftClickSent` only after Diablo cursor changes, `WitchDoctorCursorChangeLeftClickSkipped` over no-click UI, no Witch Doctor cursor-change right-click logs, and no Witch Doctor held-left or held-right release.
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
* [x] Make Battle.net startup detection window-based instead of process-based, because `Battle.net.exe` can remain running in the background without a launch-ready UI.
* [x] Retry Battle.net launch requests every 1s for up to 5s using the configured executable path first and installation discovery as fallback.
* [x] Poll Battle.net Play button detection every 100ms and click only from a current confidence-passing image match.
* [ ] Validate Battle.net launch diagnostics distinguish app Play click sent, Battle.net Play click accepted, Diablo launched because of the accepted app click, and manual Play suspected.
* [ ] Future hardening: add and validate 1500ms stable Play detection at a same/similar point while Battle.net remains foreground before clicking.
* [ ] Future hardening: add and validate a final Play-button reconfirmation immediately before sending the click.
* [ ] Future hardening: add `BattleNetPlayClickInputSequence` logging if click timing becomes an issue.
* [ ] Future hardening: add limited Battle.net Play click retry handling if Play remains visible and Diablo has not started after an unaccepted app click.
* [ ] Validate `BattleNetPlayClickSentByApp` appears immediately after the app sends the mouse click and is not treated as a successful launch by itself.
* [ ] Validate `BattleNetPlayClickAccepted` appears only after Battle.net UI transition, Battle.net window/process transition, or Diablo process start confirms the app Play click was accepted.
* [ ] Validate `BattleNetManualPlaySuspected` appears if Diablo launches without `battleNetPlayClickAcceptedByBattleNet=True`, including when the app sent a click but acceptance was not verified.
* [ ] Validate `BattleNetStillOpenAfterDiabloLaunch` appears only if the visible Battle.net window remains open after Diablo launches.
* [x] Treat background/tray Battle.net processes after Diablo launch as informational, not as close or launch failures.
* [ ] Validate `BattleNetPostLaunchCloseSummary` marks Diablo launch successful only after an accepted app Play click while reporting Battle.net close requested/succeeded/failed/timed out separately.
* [ ] Validate debug package workflow output reports app play click sent, app play click accepted, manual play suspected, Diablo launched, and Battle.net still open after launch as separate fields.
* [ ] Validate route-failure-summary/debug workflow output has one Battle.net launch verdict and one post-launch close verdict, with no conflicting duplicate entries for the same event.
* [ ] Validate Start Game logs show detection attempts, stable match confirmation, click sent, click accepted, and the exact acceptance reason.
* [ ] Validate Start Game stable detection accepts repeated points such as `316,688` / `320,691` and logs `StartGameButtonStableAccepted`.
* [ ] Validate manual Start Game click recovery logs `StartGameAcceptedByLoadedGameState` and continues to game-load/map flow.
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
* [x] Validate optimized repair station timing after New Tristram arrival remained reliable in a debug package.
* [x] Confirm repair logs include `waitAfterArrivalMs`, `timeUntilRepairStationDetectedMs`, `timeUntilRepairMenuOpenedMs`, and `totalRepairWorkflowDurationMs`.
* [x] Complete Salvage workflow testing.
* [x] Add bounty menu diagnostics for `BountyMenuDetected` and `BountyMenuEscapeSent`.
* [x] Keep app-injected Escape automation-safe so bounty watcher Escape does not cancel automation.
* [x] Log bounty menu scan image path, `Bounty Complete Scan Region.png` path, reference/screen region, best confidence, threshold, and detection source.
* [x] Capture low-confidence bounty menu scan-region screenshots for visual comparison against the configured scan-region asset.
* [x] Treat narrow bounty menu near-matches below `0.780` as `RegionNearMatch` instead of ignoring repeated `0.777` detections.
* [x] Refresh `Bounty Menu Title.png` from the latest live gold `BOUNTY COMPLETE` popup title.
* [x] Port the Python combat-menu watcher pattern for Bounty Complete: combat-only loop, 100ms poll, 0.740 threshold, injected Escape, and 1000ms cooldown.
* [x] Add bounty close diagnostics for `CombatMenuWatcherStarted`, `BountyMenuDetected`, `BountyMenuEscapeSent`, and `InjectedEscapeIgnoredByStopWatcher`.
* [x] Add config-controlled Bounty Complete watcher poll interval and Escape cooldown.
* [ ] Validate combat-active bounty menu detection logs `BountyMenuDetected` and sends `BountyMenuEscapeSent` while Diablo is foreground and combat is active.
* [ ] Validate bounty watcher injected Escape logs `InjectedEscapeIgnoredByStopWatcher` and does not stop combat or automation.
* [ ] Validate bounty menu Escape cooldown prevents repeated Escape spam.
* [ ] Validate the configured `Images\Combat\Bounty Complete Scan Region.png` green-box region is used in live logs via `BountyMenuScanResult`.
* [ ] Validate paired App screenshot diagnostics log visibility, minimized/foreground state, possible occlusion, and capture bounds.

### Configuration And Release Readiness

* [x] Add centralized `Config/AppSettings.json`.
* [x] Auto-create config with safe defaults when missing.
* [x] Log loaded config values on startup.
* [x] Add config support for Debug, UI, Repair, and Teleport sections.
* [x] Add config support for Diablo III path, Battle.net path, Images root, scan-region cache path, launch timings, and image-recognition thresholds.
* [x] Add first-run setup dialog when required runtime configuration is missing.
* [x] Add Settings panel to view, change, and validate Diablo III and Battle.net executable paths.
* [x] Block farming automation until required runtime configuration validates.
* [x] Add Diablo III and Battle.net executable auto-discovery before first-run prompting.
* [x] Hide Diagnostic Overlay and Route Inspector unless enabled by config.
* [x] Add Debug Mode toggle and keep debug-only controls hidden during normal use.
* [x] Add visible default-on Hotkeys entries for `1 - Teleport Next Location` and `2 - Exit Game`.
* [x] Default `EnableDebugScreenshots` to on in Debug builds and off in Release builds only when no saved user preference exists.
* [x] Keep Debug Mode from overwriting the saved `EnableDebugScreenshots` preference.
* [x] Increase the startup form client height enough to show the full Settings area and Debug Mode checkbox without resizing.
* [x] Gate debug screenshots and missing-asset prompts through config.
* [x] Keep route-rule config as design review only; do not migrate live route rules yet.
* [x] Add self-contained release publish script.
* [x] Add Inno Setup installer script with Start Menu and optional desktop shortcuts.
* [x] Add release checklist.
* [x] Make `GoblinFarmer.csproj` the release version source of truth for app title, publish metadata, and installer naming.
* [x] Update the release publish script to verify published EXE `FileVersion` and `ProductVersion` instead of overriding version metadata.
* [x] Update the Inno Setup script to read the installer version from the published `GoblinFarmer.exe`.
* [x] Polish public README for v1.3 release.
* [x] Add v1.3 changelog and GitHub release draft notes.
* [x] Document expected v1.3 installer artifact name as `GoblinFarmerSetup-1.3.0.exe`.
* [x] Validate a fresh release-style run with diagnostic overlay and route inspector hidden by default.
* [x] Validate enabling Debug Mode restores the diagnostic tabs and debug screenshot controls in Release builds.
* [ ] Validate disabling `EnableDebugScreenshots` suppresses success/failure/debug screenshot capture.
* [ ] Validate disabling `EnableMissingAssetPrompts` logs missing assets but suppresses manual prompts.

---

## Debugging Improvements

### Debug Surface Parity

Goal: keep Visual Studio Debug runs, published `.exe` runs, and installed `.exe` runs producing comparable diagnostic evidence.

Requirements:

* [x] VS Debug resolves `Config\AppSettings.json` from the project root so form/debug preferences survive rebuilds.
* [ ] When adding or changing debug settings, update both the project config and the published/installed app-local config path behavior.
* [ ] When adding diagnostic fields, summaries, screenshots, or package attachments, verify they appear in VS Debug logs and generated debug packages from `.exe` runs.
* [ ] Keep forced VS Debug evidence settings in memory only unless the setting is a real user/app preference.
* [ ] Keep debug package scripts aligned with runtime log names, summary event names, screenshot folders, and any new diagnostic artifacts.
* [ ] Document new debug toggles and package artifacts in this section before treating them as validated.

### Debug Manager Candidate

Status: first pass implemented as `DebugManager` plus `DiagnosticsSessionState`.

* [x] Centralize Debug Mode/profile decisions behind `DebugManager` while preserving the existing `AppSettings` surface.
* [x] Clearly separate Visual Studio Debug defaults, Release executable Debug Mode, and normal Release user mode.
* [x] Keep forced VS Debug evidence settings in memory only; saved Release Debug Mode preferences still come from `Config\AppSettings.json`.
* [x] Centralize screenshot enablement checks, debug screenshot throttling, and shared artifact paths for logs, screenshots, debug screenshots, session metadata, and session summaries.
* [x] Add lightweight session counters for games, teleports attempted/confirmed/blocked/failed, Start Game failures, Battle.net launch failures, repair/salvage failures, workflow cancellations, unexpected exceptions, and combat active time.
* [x] Export `Sessions\Session_YYYYMMDD_HHMMSS.md` safely at app shutdown.
* [x] Add retention cleanup for `Sessions\Session_*.md`, keeping the newest `Debug.SessionSummaryRetentionCount` entries by default.
* [x] Add retention cleanup for `DebugPackages\GoblinFarmer_Debug_*.zip`, keeping the newest `Debug.DebugPackageRetentionCount` entries by default.
* [x] Route existing current-location image-recognition metadata through the manager without adding extra scans.
* [ ] Validate the new session summary after the next live app exit.
* [ ] Validate session/debug package retention after enough artifacts exist to exceed the configured counts.

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
* [x] Runtime retention cleanup now applies only to `DebugPackages\GoblinFarmer_Debug_*.zip` and keeps the newest configured package count.

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

* [x] Capture paired Diablo/App screenshots for Battle.net Play click accepted.
* [x] Capture paired Diablo/App screenshots for Diablo process detected.
* [x] Capture paired Diablo/App screenshots for Start Game click accepted.
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

Status: Implemented and validated with Exit Game.

* [x] Track runtime-held left mouse, right mouse, and Shift state.
* [x] Release only tracked held inputs during cleanup while Diablo is available.
* [x] Clear tracked held-input state without sending mouse events after Diablo closes.
* [x] Log held left/right/Shift state before cleanup.
* [x] Log cleanup reason and Diablo window/rect availability.
* [x] Log whether left/right/Shift release was sent or skipped.
* [x] Avoid duplicate cleanup calls generating extra mouse events.
* [x] Preserve combat input cleanup behavior while Diablo is running.
* [x] Preserve repair-station coordinate-based clicks.
* [x] Validate with a real Exit Game run and confirm no desktop right-click/app close after Diablo exits.

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
* [x] Replace Witch Doctor held/channel behavior with mouse-wheel scrolling plus discrete cursor-change left-click pulses while preserving the `2,3,1` key loop and no-click safety logs; the Witch Doctor startup branch now launches key, mouse-wheel, and cursor-change left-click loops together.
* [x] Show Demon Hunter right-held state in diagnostics so combat does not appear stopped while right-hold is active.
* [x] Match the Python shared combat cursor loop more closely by sending extra left clicks only when the cursor handle changes from the combat-start cursor and the 120ms click gap has elapsed.
* [x] Match old Python keyboard-hook behavior for combat-relevant number keys by suppressing physical `1`/`2` during combat while allowing injected automation key events.
* [x] Add physical `2` Exit Game hotkey path for non-combat use while preserving combat precedence.
* [x] Log combat input mode and click send method for allowed/suppressed combat clicks.
* [ ] Manually validate that unrelated workflows still work after the combat-only no-click update.

---

## Development Automation

Local developer automation scripts are intentionally ignored by Git. Keep `Scripts\create-debug-package.ps1` tracked as the supported troubleshooting script for users and testers, and keep `Scripts\publish-release.ps1` tracked for release publishing.

---

## Architecture Improvements

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
* [x] Treat only a visible Battle.net window as launch-ready; process status is diagnostic-only during startup because Battle.net can run in the background.
* [x] Add 5s Battle.net launch retry loop with 1s retry spacing and detailed path/attempt/window-detection logging.
* [x] Change Battle.net Play polling to 100ms and log detection attempts before sending the current-match click.
* [x] Treat only the visible Battle.net window remaining after Diablo launch as a post-launch close warning; background tray processes are expected and informational.
* [x] Log suspected manual Battle.net Play intervention when Diablo launches without a recorded app Play click.
* [x] Log and capture Battle.net still-open-after-launch evidence, then request existing safe Battle.net close behavior.
* [ ] Manually test Battle.net detection when fullscreen, windowed, moved, and on another monitor.

### Folder Structure Cleanup

Future cleanup task:

Current:
Nested project folder.

Goal:
Simplify repository structure after debugging phase is complete.

---

## User Experience Improvements

### Session Summary Export

Status: first pass implemented.

Output:

Sessions/

* Session_YYYYMMDD.md
* Session_YYYYMMDD_HHMMSS.md

Include:

* Games created.
* Teleports attempted, confirmed, blocked, and failed/timed out.
* Start Game, Battle.net, repair, salvage, workflow cancellation, and unexpected exception counters.
* Combat/farming active time tracked passively from combat start/stop.
* Session start/end/duration, app version, build mode, debug profile, latest log, latest debug package, latest screenshot/failure screenshot, and last known issue.
* Retention cleanup applies only to `Sessions\Session_*.md` and keeps the newest configured summary count.

### Debug Package Attachments

Future possibility:

* [ ] Include optional voice recording attachments in debug packages.
* [ ] Do not implement audio recording unless explicitly requested.

### Notifications

* [ ] Verify all blocked notifications are consistent.
* [ ] Add "Already Here" notification.
* [ ] Standardize notification duration.
* [ ] Standardize notification styling.
* [x] Remove the intentional post-combat Teleport Next delay and document the snappier stop-then-teleport behavior.

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

## High Priority Future Enhancements

These are future enhancements and nice-to-haves, not active blockers.

### Goblin Statistics / GPH

* [x] Track session goblin count with manual physical `X` hotkey.
* [x] Track goblins per hour while combat/farming is active, using tracker active combat time only.
* [ ] Track session runtime.
* [ ] Optionally track lifetime goblin count.
* [ ] Optionally write a goblin history log.
* [x] Add main UI Goblin Tracker stats and Reset Stats button.
* [x] Include Goblin Tracker count, active combat time, and GPH in session summaries, runtime metadata, diagnostics, and debug package manifests.
* [x] Add explicit Goblin Tracker area-count overrides for Pandemonium Fortress Level 1 and Pandemonium Fortress Level 2 so each can count twice per game.
* [x] Add Stinging Winds to the explicit Goblin Tracker two-count-per-game override list.
* [x] Keep all non-Pandemonium Fortress/non-Stinging Winds Goblin Tracker areas capped at one count per game.
* [x] Fix manual `X` false PF counts caused by very close current-location title matches against Western/Eastern Channel and Caverns of Frost templates.
* [x] Add `AreaDetectionAmbiguous` / `AmbiguousAreaDetection` diagnostics for known PF/channel/cavern manual-count ambiguity.
* [x] Add explicit Goblin Tracker manual no-count block list and block WhimsyDale from manual `X` counts.
* [x] Extend manual `X` false PF disambiguation to Cave Of The Moon Clan Level 1, Cathedral Level 1, and Cathedral Level 2.
* [x] Extend the manual no-count block list to City of Caldeum, Gates of Caldeum, Caldeum Bazaar, Flooded Causeway, Ancient Waterway, and The Bridge Of Korsikk.
* [x] Add Automation Observation Mode for `JournalCandidate` and `MinimapCandidate` so candidates log what would count without incrementing GoblinCount, changing GPH, or consuming area-count slots.
* [x] Include Automation Observation Mode counters and last-observation details in the UI diagnostics, session summaries, runtime `session-info.txt`, and debug package manifests.
* [x] Start the GoblinEvidence scanner from combat so Observation Mode can run during normal farming.
* [x] Add scanner/evidence-loop diagnostics for scanner start/stop, scan attempts/skips, Journal/Minimap crop paths, candidate found/not-found, and skipped reasons.
* [x] Move Observation Mode scanning off the combat token so it runs continuously while Diablo is active/focused and observation/debug diagnostics are enabled.
* [x] Add `ObservationScannerStarted`, `ObservationScannerStopped`, `ObservationScanSkipped`, and `ObservationScanAttempted` diagnostics with combat, automation, Diablo, current-area, and cooldown state.
* [x] Add a no-activate 5-second manual `X` count notification with goblin counted, area/location, goblin type, and current total.
* [x] Reuse a recent same-area Observation Mode goblin type in accepted manual `X` notifications while preserving `Unknown` for stale or different-area observations.
* [x] Run a current observation refresh before accepted manual `X` notifications so same-area evidence that appears at count time can supply the goblin type without enabling automatic counting.
* [x] Skip the manual `X` evidence refresh when a recent same-area Last Observation already has a reusable goblin type.
* [x] Prefer Minimap evidence first for manual `X` notification refreshes to reduce notification delay while keeping the normal Observation Mode scanner Journal-primary.
* [x] Clear the Goblin Tracker Last Observation UI to a no-current/no-candidate state when evidence scans run and find no candidate.
* [x] Replace the generic Goblin Evidence template placeholders with per-goblin template discovery under `Images\Goblin Evidence`.
* [x] Support `Engaged Journal`, `Killed Journal`, `Engaged & Killed Journal`, and `Minimap` Goblin Evidence filename patterns.
* [x] Support cropped journal template names with optional `Journal` suffix, such as `<Goblin Type> Engaged.png` and `<Goblin Type> Killed.png`.
* [x] Support prefix-form cropped journal template names, such as `Engaged <Goblin Type> Journal.png` and `Killed <Goblin Type> Journal.png`.
* [x] Add tracked `Images\Goblin Evidence\README.md` guidance for the per-goblin folder structure.
* [x] Add startup/scanner missing-template diagnostics that log `GoblinEvidenceTemplateSetupMissing` once and throttle `GoblinEvidenceScanResult reason=MissingTemplate` summaries.
* [x] Log invalid Goblin Evidence template names without scan spam.
* [x] Normalize Goblin Evidence template aliases such as `Menagerist Goblin`, `Gelatinous Spawn`, and `Oddius Collector` to canonical Goblin Tracker types.
* [x] Consolidate Observation Mode Journal/Minimap scan regions onto the calibrated GoblinEvidence reference regions.
* [x] Log per-source GoblinEvidence scan results with `source`, `scanRegion`, `screenRegion`, matched goblin type, and confidence.
* [x] Verify discovery loads all 10 updated tight minimap goblin-icon templates.
* [x] Add minimap/template match diagnostics for template name, goblin type, normalized source, best confidence, threshold, scan region, match point, screen match point, and template size.
* [x] Route Observation Mode area resolution through the same PF ambiguity disambiguation used by manual `X` so Cathedral/channel/cavern/Moon Clan contexts do not become false PF observations.
* [x] Extend PF ambiguity disambiguation to allow strong route-context runner-up matches, such as Western Channel Level 1 at 0.887 against a false PF1 best match at 0.960 in Ancient Waterway context.
* [x] Add journal candidate diagnostics for template coverage and full-region-template diagnosis before changing journal thresholds.
* [x] Include best-template journal diagnostics in `GoblinEvidenceScanResult source=Journal` even when no candidate passes threshold.
* [x] Keep Journal template matches primary when Journal and Minimap evidence both match in the same scan.
* [x] Add journal freshness protection so Engaged journal templates anchor the current encounter and Killed-only journal templates require a recent same-goblin/same-area Engaged line.
* [x] Add `JournalEngagedAccepted`, `JournalKilledIgnoredNoRecentEngaged`, `JournalKilledAcceptedAfterEngaged`, and `JournalEngagedIgnoredStale` diagnostics.
* [x] Keep ObservationDiagnostics crops bounded by throttling capture and retaining only a recent runtime sample.
* [x] Limit debug package inclusion for ObservationDiagnostics image crops and report missing-template state in `debug-package-manifest.txt`.
* [x] Limit direct `Debug\GoblinEvidence\GoblinEvidence_*` event screenshots by count and size, and report included/excluded/oversized counts in `debug-package-manifest.txt`.
* [x] Limit default debug package inclusion of large failure screenshot groups and `debug-screenshots`; manifest reports included/excluded failure screenshot counts and sizes.
* [x] Allow combat hotkey and physical `2` Exit Game hotkey to cancel active arrival-confirmation waits with `ArrivalConfirmationCancelled` diagnostics.
* [x] Clear Goblin Evidence cooldowns and Last Observation/manual observation state when New Game or Reset Stats clears the Goblin Tracker area duplicate guard.
* [x] Live-validate Western Channel Level 1 manual `X` counts once, suppresses the second press, and does not consume PF1 slots.
* [x] Live-validate Western Channel Level 2 manual `X` counts once, suppresses the second press, and does not consume PF2 slots.
* [x] Live-validate Eastern Channel Level 1 and Eastern Channel Level 2 remain separate area keys and do not consume PF slots during close title matches.
* [x] Live-validate Caverns of Frost Level 1 manual `X` counts once, suppresses the second press, and does not consume PF1 slots.
* [x] Live-validate Caverns of Frost Level 2 manual `X` counts once, suppresses the second press, and does not consume PF2 slots.
* [x] Live-validate Pandemonium Fortress Level 1 accepts counts 1 and 2, then suppresses count 3 with `reason=AreaLimitReached`.
* [x] Live-validate Pandemonium Fortress Level 2 accepts counts 1 and 2, then suppresses count 3 with `reason=AreaLimitReached`.
* [ ] Live-validate WhimsyDale manual `X` suppresses with `reason=BlockedArea`, does not increment the counter, and does not consume an area-count slot.
* [ ] Live-validate repeated WhimsyDale manual `X` remains blocked with `reason=BlockedArea`.
* [ ] Live-validate City of Caldeum, Gates of Caldeum, Caldeum Bazaar, Flooded Causeway, Ancient Waterway, and The Bridge Of Korsikk manual `X` suppress with `reason=BlockedArea`.
* [ ] Live-validate Cave Of The Moon Clan Level 1 manual `X` counts once, suppresses the second press, and does not consume PF1 slots.
* [ ] Live-validate Cathedral Level 1 manual `X` counts once, suppresses the second press, and does not consume PF1 slots.
* [ ] Live-validate Cathedral Level 2 manual `X` counts once, suppresses the second press, and does not consume PF2 slots.
* [ ] Live-validate real PF1/PF2 counts remain available after Cave/Cathedral close-match checks.
* [x] Live-validate `JournalCandidate` observations log `GoblinObservationCandidate` and `GoblinObservationSummary` while leaving GoblinCount, GPH, tracker active time, found records, and counted-area slots unchanged.
* [ ] Live-validate `MinimapCandidate` observations log `GoblinObservationCandidate` and `GoblinObservationSummary` while leaving GoblinCount, GPH, tracker active time, found records, and counted-area slots unchanged.
* [ ] Live-validate the latest log contains `ObservationScannerStarted`, `ObservationScanAttempted`, `ObservationScanSkipped reason=...`, crop-path diagnostics, and `ObservationScannerStopped` during normal Diablo-active/non-combat time.
* [ ] Live-validate missing or invalid Journal/Minimap evidence template setup logs one clear `GoblinEvidenceTemplateSetupMissing` or `GoblinEvidenceTemplateSetupWarning` line plus throttled `GoblinEvidenceScanResult reason=MissingTemplate` summaries instead of repeated per-scan `GoblinEvidenceCandidateCheck` spam.
* [ ] Live-validate calibrated per-goblin Journal/Minimap evidence templates produce `GoblinEvidenceCandidateCheck` results and, when confidence passes, typed observation candidates without changing GoblinCount.
* [ ] Live-validate the newly cropped journal templates are discovered and can produce `JournalCandidate` observations while the journal scan region remains `64,736,645,417`.
* [ ] Live-validate old/stale visible journal lines stop producing eligible observations after the journal freshness window.
* [ ] Live-validate Killed-only journal matches log `JournalKilledIgnoredNoRecentEngaged` unless a recent same-goblin/same-area Engaged line exists.
* [ ] Live-validate `GoblinEvidenceScanResult source=Minimap scanRegion=2108,66,421,423` logs template name, best confidence, threshold, and match point for tight minimap icon templates.
* [ ] Live-validate matching ObservationDiagnostics minimap crop framing in a live run.
* [ ] If tight minimap icon matching is unreliable after one or two targeted tuning passes, document the failing template(s) and prepare a future color-matching fallback for the goblin minimap marker within the calibrated minimap region.
* [ ] Live-validate accepted manual `X` counts show the no-activate 5-second count notification without stealing Diablo focus.
* [x] Live-validate accepted manual `X` notifications show the observed goblin type when the pre-count refresh or a recent same-area observation exists, and remain `Unknown` without one.
* [x] Live-validate Last Observation displays no-current/no-candidate after a scan or manual refresh finds no goblin evidence instead of leaving stale type/area text visible.
* [ ] Live-validate Observation Mode no longer reports PF1/PF2 when current route context is Cathedral, Channel, Caverns, or Cave Of The Moon Clan.
* [x] Live-validate Observation Mode no longer reports PF1 for Western Channel Level 1 when route context is Ancient Waterway and the channel title is a strong runner-up.
* [x] Live-retest manual `X` notification latency after the recent-observation skip and Minimap-first manual refresh changes.
* [x] Re-test Sewers of Caldeum / Menagerist notification type reuse after the Minimap-first manual refresh change.
* [ ] Investigate the Blood Thief / Cave Of The Moon Clan Level 1 live miss where the manual count worked but notification type and Last Observation data were absent; latest package confirms Cave Of The Moon Clan Level 1 can count from Journal, but Blood Thief-specific recognition still needs a same-type retest.
* [x] Investigate Cathedral Level 3 accepting two manual count notifications in one game; latest package confirmed one accepted count followed by duplicate suppressions with `AreaAlreadyCounted`.
* [ ] Live-validate Stinging Winds accepts manual goblin counts 1 and 2 in the same game and suppresses count 3 with `AreaLimitReached`.
* [ ] Live-validate combat hotkey cancels active `Waiting For Location Confirmation` and logs `ArrivalConfirmationCancelled reason=CombatHotkey`.
* [ ] Live-validate physical `2` cancels active `Waiting For Location Confirmation` and logs `ArrivalConfirmationCancelled reason=ExitGameHotkey`.
* [ ] Live-validate journal candidate checks and `GoblinEvidenceScanResult source=Journal` include template name, best confidence, threshold, match point, template size, `templateCoveragePct`, and `journalDiagnosis`; if full-region journal templates remain below threshold, capture cropped journal text-line templates before lowering thresholds.
* [ ] Live-validate Automation Observation Mode reports `wouldCount=False reason=BlockedArea` in blocked locations such as WhimsyDale.
* [ ] Live-validate Automation Observation Mode reports duplicate areas as `wouldCount=False` without consuming any additional slots.
* [ ] Live-validate Automation Observation Mode reports PF1/PF2 eligibility against the two-count exception without changing the real count state.
* [ ] Live-validate a generated debug package includes Goblin observation counters and last-observation metadata from `session-info.txt`.
* [ ] Live-validate a generated missing-template debug package includes only a small recent sample from `Debug\GoblinEvidence\ObservationDiagnostics` and reports included/excluded crop counts plus missing-template state.
* [ ] Live-validate a generated normal debug package remains size-bounded and reports failure screenshot included/excluded counts plus included/excluded/available failure screenshot sizes.
* [ ] Manually validate physical `X` increments the counter once per press while GoblinFarmer is running.
* [ ] Manually validate tracker active time advances only during combat automation and pauses while idle, in menus, waiting for game creation, waiting for Diablo launch, or paused.
* [ ] Manually validate Reset Stats clears goblin count, tracker active time, GPH, per-area count state, Goblin Evidence cooldowns, and Last Observation/manual observation state, and restarts tracker timing from the reset moment if combat is active.
* [ ] Manually validate New Game clears per-area count state so the same resolved areas can count again in the next game.
* [ ] Manually validate a generated debug package includes Goblin Tracker metadata from `session-info.txt`.

### Asset Validator

* [ ] Validate required image/template assets exist.
* [ ] Validate route/location templates exist.
* [ ] Validate combat templates exist.
* [ ] Validate required scripts are included in publish/debug output.
* [ ] Warn clearly when assets are missing, duplicated, misnamed, or not copied to output.

### Debug Manager

* [x] Centralize debug mode behavior.
* [x] Clearly separate VS Debug defaults, Release Debug Mode, and normal user release behavior.
* [x] Centralize debug screenshot retention settings.
* [x] Centralize session summary and debug package count retention settings.
* [ ] Centralize debug package inclusion rules.
* [x] Provide consistent diagnostic logging controls.
* [x] Keep expensive diagnostics opt-in, throttled, or tied to existing events.
* [x] Keep wrappers allocation-light in hot loops and prefer state-change logging over per-tick logging.

### Developer Dashboard

* [ ] Add a debug-only live state panel.
* [ ] Show current detected location.
* [ ] Show best template match and confidence.
* [ ] Show runner-up match/confidence when useful.
* [ ] Show combat state.
* [ ] Show last route decision.
* [ ] Show last blocked hotkey reason.
* [ ] Show last Battle.net launch/click action.
* [ ] Show last diagnostic screenshot path.

## Medium Priority Future Enhancements

These are future enhancements and nice-to-haves, not active blockers.

### Route Rules Configuration

* [ ] Evaluate moving hardcoded route allow/block behavior into a JSON/config-driven route rules file.
* [ ] Keep route names, allowed transitions, blocked transitions, and return behavior easier to inspect.
* [ ] Include startup validation so bad route config fails clearly.

### Enhanced Session Statistics

* [ ] Track runs completed.
* [ ] Track failed runs.
* [ ] Track average run length.
* [ ] Track combat uptime.
* [ ] Track route detection failures.
* [ ] Track Battle.net launch failures.
* [ ] Add useful reliability counters for long farming sessions.

### Recognition Confidence Overlay

* [ ] Add a debug-only overlay or panel section.
* [ ] Display current detected location.
* [ ] Display confidence percentage.
* [ ] Display runner-up template/confidence.
* [ ] Help diagnose close matches like City Of Caldeum vs Gates of Caldeum.

## Low Priority / Nice-To-Have Future Enhancements

These are future enhancements and nice-to-haves, not active blockers.

### Route Replay

* [ ] Save route decisions from a session.
* [ ] Replay route decision logs after the fact.
* [ ] Help debug route logic without needing a live farming run.

### Visual Route Viewer

* [ ] Add a visual or tree-style route map.
* [ ] Show allowed transitions.
* [ ] Show blocked transitions.
* [ ] Show return/teleport behavior.
* [ ] Keep this focused on route visibility/debugging, separate from manual route buttons.

### Historical Analytics

* [ ] Track daily goblin averages.
* [ ] Track weekly GPH.
* [ ] Track lifetime statistics.
* [ ] Track best session record.
* [ ] Track long-term farming trends.

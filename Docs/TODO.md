# GoblinFarmer TODO

## High Priority (Current Development Phase)

### Teleport Route Reliability

* [ ] Fix City of Caldeum / Gates of Caldeum normalization.
* [ ] Fix Western Channel Level 1 normalization.
* [ ] Fix Western Channel Level 2 blocking behavior.
* [ ] Fix Waterway button clearing next-target button color.
* [ ] Fix Eastern Channel Level 2 routing to Waterway instead of Stinging Winds.
* [ ] Fix Stinging Winds blocking rules.
* [x] Fix manual teleport button retry after failed/interrupted button teleports.
* [x] Fix ButtonRetry so manual retries bypass teleport blocking.
* [x] Log and ignore same-button clicks while waiting for teleport arrival confirmation.
* [ ] Verify Black Canyon Mines allows Battlefields.
* [ ] Verify all route logic matches Project_Status.md.

### Start Game Reliability

* [ ] Investigate Start Game image recognition failures.
* [ ] Determine if cursor is interfering with Start Game image detection.
* [ ] Add Start Game diagnostic logging.
* [ ] Capture debug screenshots when Start Game verification fails.

### Testing

* [ ] Complete full route test from Southern Highlands through Pandemonium Fortress Level 2.
* [ ] Complete interrupted teleport testing.
* [ ] Test failed/interrupted Royal Crypts button retry from Cathedral Level 1 and confirm retry preserves Cathedral/Royal Crypts state, bypasses manual-button blocking, and does not advance until arrival confirmation.
* [ ] Confirm Battle.net Play button is found in the window-relative region before full-screen fallback.
* [ ] If Battle.net Play button falls back, inspect fallback region diagnostics and update `BattleNetPlayButton` from the suggested same-size cached region if the fallback point is outside the resolved region.
* [ ] Complete Exit Game workflow testing.
* [ ] Confirm Exit Game no longer causes a post-Diablo desktop right-click or app close.
* [ ] Complete Repair workflow testing.
* [ ] Complete Salvage workflow testing.

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
* [x] Include latest normal debug screenshots.
* [x] Include Project_Status.md.
* [x] Include TEST_CHECKLIST.md.
* [x] Include TODO.md and AGENTS.md.
* [x] Include current git status.
* [x] Include recent git log.
* [x] Include debug-package-manifest.txt.
* [x] Include latest screenshot failure type when available.
* [x] Use package filename timestamp with seconds.
* [x] Display clear console summary.
* [x] Warn for missing optional files or folders.
* [x] Exclude bin and obj folders.
* [x] Avoid huge build artifacts.
* [x] Document how to run the script.
* [x] Export zip package.

Example output:

DebugPackages/

* GoblinFarmer_Debug_YYYYMMDD_HHMMSS.zip

### Enhanced Failure Logging

* [ ] Save screenshots automatically when image recognition fails.
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
* [ ] Record image name, confidence, scan region, threshold, and best match information.

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
* [x] Update only `BattleNetPlayButton` to window-local region `30,853,292,75` from the green-box reference screenshot.
* [x] Add BattleNetPlayButton fallback diagnostics showing window rect, cached region, resolved screen region, fallback point, expected center, delta, distance, inside/outside assessment, and suggested same-size cached region.
* [x] Restore/focus Battle.net before resolving scan regions.
* [x] Keep fullscreen search as fallback.
* [x] Preserve existing scan-region cache/reference format.
* [x] Log cached region, Battle.net window rect, resolved screen region, window-relative result, outside-window warnings, and fallback search.
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

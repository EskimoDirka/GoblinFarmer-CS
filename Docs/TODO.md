# GoblinFarmer TODO

## High Priority (Current Development Phase)

### Teleport Route Reliability

* [ ] Fix City of Caldeum / Gates of Caldeum normalization.
* [ ] Fix Western Channel Level 1 normalization.
* [ ] Fix Western Channel Level 2 blocking behavior.
* [ ] Fix Waterway button clearing next-target button color.
* [ ] Fix Eastern Channel Level 2 routing to Waterway instead of Stinging Winds.
* [ ] Fix Stinging Winds blocking rules.
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
* [ ] Complete Exit Game workflow testing.
* [ ] Complete Repair workflow testing.
* [ ] Complete Salvage workflow testing.

---

## Debugging Improvements

### Debug Package Generator

Create a one-click debug package tool.

Requirements:

* Include latest log.
* Include latest debug screenshots.
* Include Project_Status.md.
* Include TEST_CHECKLIST.md.
* Include current git status.
* Export zip package.

Example output:

DebugPackages/

* GoblinFarmer_Debug_YYYYMMDD_HHMM.zip

### Enhanced Failure Logging

* [ ] Save screenshots automatically when image recognition fails.
* [ ] Save screenshots automatically when teleports fail.
* [ ] Save screenshots automatically when Start Game fails.
* [ ] Record image name, confidence, scan region, threshold, and best match information.

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

* [ ] Convert Battle.net scan regions to window-relative coordinates.
* [ ] Keep fullscreen search as fallback.

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

# Changelog

Release notes are maintained manually. Before running `Scripts\publish-release.ps1` or creating a GitHub Release, update the next release section with the user-facing changes worth publishing.

## v1.4

GoblinFarmer v1.4 is focused on Goblin Tracker automatic-count readiness, Observation Mode diagnostics, Witch Doctor input reliability, startup validation, and package-size hardening.

### Highlights

- Added Goblin Tracker Automation Observation Mode for Journal and Minimap evidence.
- Added gated automatic Goblin Tracker counting behind Observation Mode and Auto Goblin Count.
- Retired the physical `X` Goblin Tracker count hotkey and added a VS Debug `Capture` button for image-recognition troubleshooting snapshots.
- Hardened automatic counting with fresh-evidence gating, stale journal protection, blocked-area suppression, and per-area duplicate limits.
- Added two-count exceptions for Pandemonium Fortress Level 1, Pandemonium Fortress Level 2, and Stinging Winds.
- Improved Last Observation diagnostics and accepted-count display behavior.
- Added Witch Doctor mouse-wheel plus cursor-change left-click combat input.
- Improved startup path validation and Diablo III auto-discovery.
- Reduced default debug package size from Goblin Evidence and screenshot artifacts.

### Quality Improvements

- Added bounded Goblin Evidence diagnostics and clearer scanner/candidate logs.
- Preserved automatic Goblin Tracker debug artifacts while making the manual `Capture` button supplemental only.
- Improved Reset Stats/New Game cleanup for Goblin Tracker observation and duplicate state.
- Improved salvage timing diagnostics and per-slot speed.
- Synchronized release version metadata and installer EXE metadata for v1.4.

## v1.3

GoblinFarmer v1.3 is considered a stable release focused on reliability, launch flow improvements, teleport routing accuracy, and overall usability.

### Highlights

- Improved Battle.net launch reliability.
- Improved Diablo launch detection.
- Fixed Start Game workflow stability.
- Added Start Game recovery handling.
- Improved Make New Game workflow protection.
- Improved teleport routing and location handling.
- Added dynamic application version display.
- Installer version now synchronized with application version.

### Quality Improvements

- Updated release workflow.
- Improved logging and diagnostics.
- Installer packaging improvements.
- Version metadata cleanup.
- Cleaned up Release/VS Debug options so forced debug defaults do not leak into user builds.
- Removed the Skill 1 teleport-protection checkbox while keeping the protection always enabled internally.
- Included the debug package script in published/installed builds.
- Updated the routine GitHub sync flow to build, publish, refresh the user executable, stage the latest tracked executable, create a portable zip, commit, and push without creating a GitHub Release unless explicitly requested.

## v1.0

### Added

- Initial C# WinForms release.

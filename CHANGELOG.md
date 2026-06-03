# Changelog

Release notes are maintained manually. Before running `Scripts\GitHub Sync.bat` or creating a GitHub Release, update the next release section with the user-facing changes worth publishing.

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

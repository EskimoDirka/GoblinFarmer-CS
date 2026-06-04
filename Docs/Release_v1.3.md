# GoblinFarmer v1.3

## Highlights

* Improved Battle.net launch reliability
* Improved Diablo launch detection
* Fixed Start Game workflow stability
* Added Start Game recovery handling
* Improved Make New Game workflow protection
* Improved teleport routing and location handling
* Preserved selected combat profile across app restarts
* Improved Demon Hunter no-click-region suppression
* Added dynamic application version display
* Installer version now synchronized with application version

## Quality Improvements

* Updated release workflow
* Improved logging and diagnostics
* Installer packaging improvements
* Version metadata cleanup
* Cleaned up Debug/Release options so debug defaults stay internal to debug builds
* Added form user preferences under `User` in `Config\AppSettings.json`, including combat profile and Hotkeys checkbox states
* Demon Hunter protected UI regions now suppress unsafe injected mouse clicks without stopping key rotation
* Included the debug package script in published and installed builds

## Notes

GoblinFarmer v1.3 is considered a stable release focused on reliability, launch flow improvements, teleport routing accuracy, and overall usability.

The app is designed to support flexible install paths and configurable Diablo III, Battle.net, and Images paths. After installing, use the built-in `Verify Paths` button to confirm paths for the current machine.

GoblinFarmer is designed to be resolution-scalable through image recognition regions and configurable path handling. Users should still validate behavior on unusual display, DPI, or multi-monitor setups.

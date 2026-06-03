# Scan Regions

GoblinFarmer uses image templates and scan regions to recognize Battle.net, Diablo III UI, current locations, menus, and workflow states.

## Runtime Assets

The release package includes the `Images` folder beside `GoblinFarmer.exe`. Required subfolders include:

- `Images/Combat`
- `Images/Current Location`
- `Images/Leave Game`
- `Images/Repair`
- `Images/Salvage`
- `Images/Start Game`
- `Images/Teleport Function`

## Configuration

`Config/AppSettings.json` stores the image root and scan-region cache path. Defaults are release-friendly and can be adjusted without rebuilding the app.

## Debugging

When Debug Mode is enabled, missing-template diagnostics and screenshot capture can help refresh or replace stale templates. Debug packages include scan-region evidence and route/workflow summaries for troubleshooting.

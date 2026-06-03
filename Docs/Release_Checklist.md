# GoblinFarmer Release Checklist

Use this before publishing a final Windows release.

## Version

- [ ] Update `GoblinFarmer.csproj` with the intended `<Version>`, `<AssemblyVersion>`, `<FileVersion>`, and `<InformationalVersion>`.
- [ ] Confirm `IncludeSourceRevisionInInformationalVersion` is disabled so Windows `ProductVersion` remains clean.

## Build

- [ ] Run `dotnet build GoblinFarmer.csproj`.
- [ ] Publish from Visual Studio with the `GoblinFarmerRelease` profile or run `powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\publish-release.ps1`.
- [ ] Confirm publish output includes `GoblinFarmer.exe`, `GoblinFarmerIcon.ico`, `Config\AppSettings.json`, and `Images\`.
- [ ] If Inno Setup is installed, confirm `artifacts\installer\GoblinFarmerSetup-1.3.0.exe` is created for v1.3.
- [ ] Confirm the app title bar shows the expected version, for example `GoblinFarmer v1.3.0`.
- [ ] Confirm Windows Properties show `FileVersion: 1.3.0.0` and `ProductVersion: 1.3.0`.

## Install

- [ ] Install to `%LOCALAPPDATA%\Programs\GoblinFarmer`.
- [ ] Confirm the Start Menu shortcut launches the app.
- [ ] Confirm optional desktop shortcut uses the GoblinFarmer icon.
- [ ] Confirm uninstall removes installed files but does not require source code.

## First Run

- [ ] Launch with blank executable paths.
- [ ] Confirm Diablo III and Battle.net auto-discovery runs.
- [ ] If discovery fails, browse to `Diablo III64.exe` or `Diablo III.exe`.
- [ ] Browse to `Battle.net.exe`.
- [ ] Verify the `Images` template folder and required subfolders.
- [ ] Confirm automation controls remain disabled until settings validate.

## Runtime

- [ ] Confirm Debug Mode is off by default and diagnostic panes are hidden.
- [ ] Enable Debug Mode and confirm diagnostic/debug screenshot controls appear.
- [ ] Run a focused Make New Game flow on a machine with valid paths.
- [ ] Confirm Battle.net launch uses the configured executable path.
- [ ] Confirm logs clearly report invalid or missing paths.

## Package

- [ ] Confirm `Config\AppSettings.json` is preserved on installer upgrade.
- [ ] Confirm `ScanRegions.json` is preserved on installer upgrade when present.
- [ ] Confirm no `bin`, `obj`, `Logs`, `Screenshots`, or debug screenshots are packaged as release content.

## GitHub Release

- [ ] Create or update the GitHub Release for `v1.3`.
- [ ] Use `Docs/Release_v1.3.md` as the release body.
- [ ] Upload `GoblinFarmerSetup-1.3.0.exe`.
- [ ] Confirm the release page links to the installer asset and shows the expected release notes.

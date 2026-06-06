# GoblinFarmer Release Checklist

Use this before publishing a final Windows release.

## Version

- [ ] Update `GoblinFarmer.csproj` with the intended `<Version>`, `<AssemblyVersion>`, `<FileVersion>`, and `<InformationalVersion>`.
- [ ] Confirm `IncludeSourceRevisionInInformationalVersion` is disabled so Windows `ProductVersion` remains clean.

## Build

- [ ] Run `dotnet build GoblinFarmer.csproj`.
- [ ] Publish from Visual Studio with the `GoblinFarmerRelease` profile or run `powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\publish-release.ps1`.
- [ ] Confirm the generated publish folder exists at `artifacts\publish\GoblinFarmer`.
- [ ] Confirm publish output includes `GoblinFarmer.exe`, `GoblinFarmerIcon.ico`, `Config\AppSettings.json`, `Images\`, and `Scripts\create-debug-package.ps1`.
- [ ] Compile `Installer\GoblinFarmer.iss` only after `artifacts\publish\GoblinFarmer\GoblinFarmer.exe` exists.
- [ ] If Inno Setup is installed, confirm `artifacts\installer\GoblinFarmerSetup-1.4.0.exe` is created for v1.4.
- [ ] Confirm the app title bar shows the expected version, for example `GoblinFarmer v1.4.0`.
- [ ] Confirm Windows Properties show `FileVersion: 1.4.0.0` and `ProductVersion: 1.4.0`.

## Install

- [ ] Confirm the installer shows the install directory page.
- [ ] Confirm the default install path is `%LOCALAPPDATA%\Programs\GoblinFarmer`.
- [ ] Confirm the ready page displays the selected install directory before install.
- [ ] If testing an upgrade after a previous custom install path, confirm setup does not silently reuse the old path without showing the directory choice.
- [ ] Confirm the Start Menu shortcut launches the app.
- [ ] Confirm optional desktop shortcut uses the GoblinFarmer icon.
- [ ] Confirm uninstall removes installed files but does not require source code.

## First Run

- [ ] Launch with blank executable paths.
- [ ] Confirm Diablo III and Battle.net auto-discovery runs.
- [ ] If discovery fails, browse to `Diablo III64.exe` or `Diablo III.exe`.
- [ ] Browse to `Battle.net.exe`.
- [ ] Verify the `Images` template folder and required subfolders.
- [ ] Use `Verify Paths` after install to confirm Diablo III, Battle.net, and Images paths.
- [ ] Confirm automation controls remain disabled until settings validate.

## Runtime

- [ ] Confirm Debug Mode is off by default and diagnostic panes are hidden.
- [ ] Confirm `Block Skill 1 During Teleport Hotkey` is not visible in Release or VS Debug.
- [ ] Enable Debug Mode and confirm diagnostic/debug screenshot controls appear.
- [ ] Confirm enabling Debug Mode auto-checks `Keep Debug Screenshots`, and disabling Debug Mode hides it.
- [ ] Confirm disabling Debug Mode shrinks the form back to compact width with no unused right-side space.
- [ ] Confirm VS Debug shows the diagnostic overlay and route inspector on startup without editable Debug Mode or Keep Debug Screenshots checkboxes.
- [ ] Run a focused Make New Game flow on a machine with valid paths.
- [ ] Confirm Battle.net launch uses the configured executable path.
- [ ] Confirm logs clearly report invalid or missing paths.
- [ ] On any unusual display, DPI, or multi-monitor setup, validate image-recognition scan regions before broad runtime testing.

## Package

- [ ] Confirm `Config\AppSettings.json` is preserved on installer upgrade.
- [ ] Confirm `ScanRegions.json` is preserved on installer upgrade when present.
- [ ] Confirm no `bin`, `obj`, `Logs`, `Screenshots`, or debug screenshots are packaged as release content.
- [ ] Confirm generated `artifacts\` output is not committed to Git.

## GitHub Release

- [ ] Create or update the GitHub Release for `v1.4.0`.
- [ ] Use `Docs/Release_v1.4.md` as the release body.
- [ ] Confirm the README screenshot renders from `Docs/Goblin%20Farmer%20v1.3.0.png`.
- [ ] Upload `GoblinFarmerSetup-1.4.0.exe` and, if useful, the portable `GoblinFarmer-1.4.0-win-x64-portable.zip`.
- [ ] Confirm the release page links to the installer asset and shows the expected release notes.

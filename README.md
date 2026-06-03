# GoblinFarmer

GoblinFarmer is a personal Windows Forms automation project for Diablo III farming workflows. It combines image recognition, route-aware teleporting, Battle.net and Diablo launch support, combat helpers, town automation, and diagnostic tooling for troubleshooting long sessions.

This is a personal automation project, not an official Blizzard product. Use it at your own risk and only in environments where you understand and accept the relevant game, account, and automation-policy implications.

## Screenshot

![GoblinFarmer v1.3 main window](Docs/Goblin%20Farmer%20v1.3.0.png)

## Features

- Battle.net launch flow with configured-path-first startup, visible-window detection, retry logging, and Play button image recognition.
- Diablo launch detection and Start Game handling with stable-button verification, click acceptance checks, and manual-click recovery.
- Route-aware Teleport Next workflow for the configured farming path.
- Hotkey route blocking for known unsafe or incorrect location transitions.
- Make New Game, Exit Game, repair, salvage, and town workflow helpers.
- Monk, Demon Hunter, and Witch Doctor combat support.
- Debug Mode with diagnostic panes, screenshot controls, route state inspection, and debug package generation.
- Runtime configuration for Diablo III, Battle.net, image templates, launch timings, and diagnostic behavior.
- Self-contained Windows release publishing plus optional Inno Setup installer packaging.
- Dynamic app title and synchronized installer/application versioning.

## Installation

For the v1.3 installer, use:

```text
GoblinFarmerSetup-1.3.0.exe
```

The installer places GoblinFarmer under:

```text
%LOCALAPPDATA%\Programs\GoblinFarmer
```

Setup shows the install directory during installation so users can review or change it before files are copied. The default path is user-local and does not require administrator rights, but custom install locations are supported.

The installer also creates a Start Menu shortcut and can optionally create a desktop shortcut. No source checkout or .NET SDK is required for the installed self-contained release.

If using the publish folder directly, run `GoblinFarmer.exe` from the published `GoblinFarmer` folder.

## First-Run Setup

On first launch, GoblinFarmer attempts to discover the required runtime paths. If a required path is missing, the setup dialog opens and asks for:

- Diablo III executable: `Diablo III64.exe` or `Diablo III.exe`
- Battle.net executable: `Battle.net.exe`
- Images template folder: usually the bundled `Images` folder beside the app

Automation stays disabled until the required paths and template folders validate successfully. Paths can also be reviewed and changed later from the Settings area in the app.

After install, use the built-in `Verify Paths` button to confirm the Diablo III, Battle.net, and Images paths for the current machine.

Required template folders:

```text
Images/Combat
Images/Current Location
Images/Leave Game
Images/Repair
Images/Salvage
Images/Start Game
Images/Teleport Function
```

## Basic Usage

1. Launch GoblinFarmer.
2. Confirm the Diablo III, Battle.net, and Images paths are valid.
3. Choose the active character profile.
4. Start Diablo through the Battle.net / Make New Game workflow or use the available game-flow buttons once Diablo is ready.
5. Use route buttons or the Teleport Next hotkey for route progression.
6. Use Exit Game when the current run should return to the menu.

After combat stops, Teleport Next may have a slight intentional delay before it is available again. This lets combat/input state settle and helps avoid accidental or unsafe teleport attempts.

## Combat Profile Skill Setup

GoblinFarmer expects these exact Diablo III skills and runes to be assigned to the listed keybinds for each combat profile. Incorrect skill placement may cause combat automation to behave incorrectly.

### Monk

| Keybind | Skill | Rune |
|---|---|---|
| 1 | Epiphany | Insight |
| 2 | Mystic Ally | Air Ally |
| 3 | Dashing Strike | Radiance |

### Demon Hunter

| Keybind | Skill | Rune |
|---|---|---|
| Left Mouse | Hungering Arrow | Devouring Arrow |
| Right Mouse | Strafe | Drifting Shadow |
| 1 | Preparation | Focused Mind |
| 2 | Companion | Bat Companion |
| 3 | Vengeance | Seethe |
| 4 | Smoke Screen | Displacement |

### Witch Doctor

| Keybind | Skill | Rune |
|---|---|---|
| 1 | Hex | Angry Chicken |
| 2 | Horrify | Stalker |
| 3 | Spirit Walk | Severance |

## Compatibility Notes

GoblinFarmer is designed to support flexible install paths. Runtime paths for Diablo III, Battle.net, and image templates are configurable where applicable, and the app validates those paths before enabling automation.

GoblinFarmer is also designed to be resolution-scalable through image recognition regions and configurable path handling. Users should still verify paths with the built-in `Verify Paths` button after install and expect to validate image-recognition behavior on unusual display, DPI, or multi-monitor setups.

## Hotkeys

Default visible hotkeys:

- `` ` `` / tilde: combat hotkey
- `1`: Teleport Next Location
- `2`: Exit Game
- `Alt` + `` ` ``: Loot
- `Up Arrow`: Kadala

The Hotkeys group in the app shows the available hotkeys and includes checkboxes for the default physical hotkey paths. Combat-state and teleport safeguards can suppress hotkeys when using them would be unsafe; these stability protections are not user preferences.

## Debug Mode

Debug Mode is off by default for normal release use. When enabled from Settings, it shows diagnostic controls and debug-only views such as the route state inspector and screenshot options, and it enables Keep Debug Screenshots by default until the user turns screenshot retention off. Visual Studio Debug runs force debug evidence internally, so those settings are not shown as editable options there.

Debug tooling includes:

- Paired Diablo/app screenshots for major workflow success and failure milestones.
- Optional debug screenshot retention.
- Missing-template diagnostics and optional capture prompts.
- Route and workflow summaries in generated debug packages.
- Detailed launch, Start Game, teleport, repair, salvage, bounty, and Exit Game logs.

Generate a debug package from the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\create-debug-package.ps1
```

## Release v1.3

GoblinFarmer v1.3 is considered a stable release focused on reliability, launch flow improvements, teleport routing accuracy, and overall usability.

Highlights:

- Improved Battle.net launch reliability.
- Improved Diablo launch detection.
- Fixed Start Game workflow stability.
- Added Start Game recovery handling.
- Improved Make New Game workflow protection.
- Improved teleport routing and location handling.
- Preserved matched current-location names for hotkey blocked-location notifications while keeping route buttons grouped.
- Added dynamic application version display.
- Installer version now synchronized with application version.

Quality improvements:

- Updated release workflow.
- Improved logging and diagnostics.
- Installer packaging improvements.
- Version metadata cleanup.

Version details:

- App title: `GoblinFarmer v1.3.0`
- EXE `FileVersion`: `1.3.0.0`
- EXE `ProductVersion`: `1.3.0`
- Installer version: `1.3.0`
- Expected installer artifact: `GoblinFarmerSetup-1.3.0.exe`

See [Docs/Release_v1.3.md](Docs/Release_v1.3.md) for ready-to-copy GitHub release notes.

## Build From Source

Requirements:

- Windows
- .NET SDK compatible with `net10.0-windows`
- Diablo III and Battle.net installed for runtime use

Build from the project root:

```powershell
dotnet build GoblinFarmer.csproj
```

## Release Build

Release versioning starts in `GoblinFarmer.csproj`. Before publishing a new release, update:

```xml
<Version>1.3.0</Version>
<AssemblyVersion>1.3.0.0</AssemblyVersion>
<FileVersion>1.3.0.0</FileVersion>
<InformationalVersion>1.3.0</InformationalVersion>
```

Then publish the self-contained Windows build. You can publish from Visual Studio using the `GoblinFarmerRelease` publish profile, or run the release script from the project root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\publish-release.ps1
```

The script publishes to `artifacts\publish\GoblinFarmer`, verifies executable version metadata, icon, config, Images output, and `Scripts\create-debug-package.ps1`, then compiles `Installer\GoblinFarmer.iss` if Inno Setup is installed. The installer reads the version from the published executable, so the app must be published before compiling the `.iss` file.

`artifacts\` is generated output and is intentionally ignored by Git. Do not commit published app folders or installer binaries.

If Inno Setup is installed separately, compile the installer after publishing:

```powershell
ISCC.exe "/DSourceDir=artifacts\publish\GoblinFarmer" "Installer\GoblinFarmer.iss"
```

Final release flow:

1. Update version metadata in `GoblinFarmer.csproj`.
2. Publish from Visual Studio or run `Scripts\publish-release.ps1`.
3. Build the Inno Setup installer from `Installer\GoblinFarmer.iss`.
4. Confirm the installer name and version, such as `GoblinFarmerSetup-1.3.0.exe`.
5. Upload the installer to the GitHub Release with the notes from `Docs/Release_v1.3.md`.

See [Docs/Release_Checklist.md](Docs/Release_Checklist.md) before publishing a final build.

## Project Docs

- [CHANGELOG.md](CHANGELOG.md): user-facing release history.
- [Docs/Release_v1.3.md](Docs/Release_v1.3.md): ready-to-copy GitHub release notes.
- [Docs/CombatProfiles.md](Docs/CombatProfiles.md): supported combat profiles and required skill setup.
- [Docs/Project_Status.md](Docs/Project_Status.md): detailed development status and implementation history.
- [Docs/TODO.md](Docs/TODO.md): development and validation backlog.
- [Docs/Release_Checklist.md](Docs/Release_Checklist.md): release validation checklist.

## License

GoblinFarmer is released under the [MIT License](LICENSE).

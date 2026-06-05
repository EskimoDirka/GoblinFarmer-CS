# GoblinFarmer

GoblinFarmer is a personal Windows Forms automation project for Diablo III farming workflows. It combines image recognition, route-aware teleporting, Battle.net and Diablo launch support, combat helpers, town automation, and diagnostic tooling for troubleshooting long sessions.

This is a personal automation project, not an official Blizzard product. Use it at your own risk and only in environments where you understand and accept the relevant game, account, and automation-policy implications.

## Screenshot

![GoblinFarmer main window](Docs/Goblin%20Farmer%20v1.3.0.png)

## Features

- Battle.net launch flow with configured-path-first startup, visible-window detection, retry logging, and Play button image recognition.
- Diablo launch detection and Start Game handling with stable-button verification, click acceptance checks, and manual-click recovery.
- Route-aware Teleport Next workflow for the configured farming path.
- Hotkey route blocking for known unsafe or incorrect location transitions.
- Make New Game, Exit Game, repair, salvage, and town workflow helpers.
- Monk, Demon Hunter, and Witch Doctor combat support.
- Location-aware Goblin Tracker with `X` hotkey counting, per-area duplicate protection, active-combat-time GPH, live UI stats, accepted-count notifications, and reset support.
- Debug Mode with diagnostic panes, screenshot controls, route state inspection, and debug package generation.
- Visual Studio Debug layout that keeps the expanded Goblin Tracker evidence and Last Observation fields visible while normal release launches stay compact.
- Runtime configuration for Diablo III, Battle.net, image templates, launch timings, diagnostic behavior, and VS Debug project-root config persistence.
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

Diablo III discovery checks common install locations on fixed local drives, including drive-root installs such as `D:\Diablo III\Diablo III.exe`. It prefers `Diablo III64.exe` when present, otherwise uses `Diablo III.exe`, and does not select `Diablo III Launcher.exe`.

Automation stays disabled until the required paths and template folders validate successfully. Paths can also be reviewed and changed later from the Settings area in the app.

The selected combat profile and Hotkeys checkbox states are saved in `Config\AppSettings.json` under `User` and restored when GoblinFarmer starts. Visual Studio Debug runs use the project-level `Config\AppSettings.json` directly so form or rebuild changes do not reset developer paths; release and installed runs use the app-local copy beside the executable.

When config defaults are added later, GoblinFarmer preserves existing Diablo III, Battle.net, and custom Images paths instead of replacing them with blank/default values.

GoblinFarmer does not enable automation unless the Diablo III executable path is valid. If Diablo III is missing and cannot be auto-discovered, the app stays in `Setup Required` instead of showing `Idle`.

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

After combat stops, Teleport Next is available as soon as combat is marked stopped. Cleanup still releases tracked mouse and Shift inputs before the queued teleport flow runs, Witch Doctor cleanup uses only a short best-effort Hex/chicken exit, and only user `1` presses trigger Teleport Next.

## Goblin Tracker

The Goblin Tracker is a counter foundation with resolved-area duplicate protection. Press `X` while GoblinFarmer is running to add one goblin for the current detected area; a second count in the same resolved area is suppressed for normal areas. Pandemonium Fortress Level 1 and Pandemonium Fortress Level 2 are explicit exceptions and allow up to two counts per game before further counts are suppressed.

Manual `X` detection guards against known close title-template matches where Pandemonium Fortress can look nearly identical to Western/Eastern Channel, Caverns of Frost, Moon Clan cave, or Cathedral titles. When that happens, GoblinFarmer uses the current route context to select the real area, or suppresses unresolved ambiguity instead of consuming the wrong PF count slot.

Some non-spawn route and event areas are excluded from manual Goblin Tracker counts, including WhimsyDale, City of Caldeum subareas that should not count, Ancient Waterway, and The Bridge Of Korsikk. Pressing `X` there logs a blocked-area suppression and does not increase the counter or consume an area-count slot.

The counter does not require combat to be active. If the current area cannot be resolved, the manual hotkey falls back to the existing count behavior and logs the unresolved area.

Automation Observation Mode watches Journal and Minimap goblin candidates during combat and records what the automation would have counted. It discovers per-goblin templates from `Images\Goblin Evidence` using names such as `<Goblin Type> Engaged Journal.png`, `<Goblin Type> Killed Journal.png`, `<Goblin Type> Engaged & Killed Journal.png`, and tight `<Goblin Type> Minimap.png` icon crops, then reports the matched goblin type when possible. Journal evidence is primary when both Journal and Minimap match in the same scan. It uses the same area resolution, blocked-area, duplicate, and PF1/PF2 exception rules, but automatic counting is not enabled yet; observations do not change GoblinCount, GPH, active time, or counted-area slots. Debug logs include scanner start/stop, scan attempts/skips, candidate checks with match points, calibrated Journal/Minimap scan regions, and throttled diagnostic crop paths.

Journal evidence treats both killed and escaped goblin lines as encounters. `Gelatinous Spawn` journal kills are normalized to `Gelatinous Sire`.

Current found list: Gem Hoarder, Malevolent Tormentor, Treasure Goblin, Rainbow Goblin, Insufferable Miscreant, Blood Thief, Odious Collector, Gelatinous Sire.

Still needed: Gilded Baron (journal captured; minimap still needed), Menagerist.

The main window shows:

```text
Goblins: 0
GPH: 0.00
Active Time: 00:00:00
Last Observation:
--
--
--
--
```

GPH uses tracker active combat time only and calculates as soon as active combat time is greater than zero. `Reset Stats` clears the count, tracker active time, GPH, and the in-memory counted-area duplicate guard; if combat is currently running, tracker active time restarts from the reset moment. Starting a new game clears only the counted-area guard so the same area can be counted again in the next game.

Accepted manual `X` counts show a no-activate notification for 5 seconds with the counted area, goblin type, and current total.

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

Witch Doctor combat repeats the key loop in `2, 3, 1` order and uses mouse wheel scrolling during active combat. It does not hold left mouse down.

## Compatibility Notes

GoblinFarmer is designed to support flexible install paths. Runtime paths for Diablo III, Battle.net, and image templates are configurable where applicable, and the app validates those paths before enabling automation.

GoblinFarmer is also designed to be resolution-scalable through image recognition regions and configurable path handling. Users should still verify paths with the built-in `Verify Paths` button after install and expect to validate image-recognition behavior on unusual display, DPI, or multi-monitor setups.

## Hotkeys

Default visible hotkeys:

- `` ` `` / tilde: combat hotkey
- `1`: Teleport Next Location
- `2`: Exit Game
- `X`: manually count a goblin for the current resolved area
- `Alt` + `` ` ``: Loot
- `Up Arrow`: Kadala

The Hotkeys group in the app shows the available hotkeys and includes checkboxes for the default physical hotkey paths. Combat-state and teleport safeguards can suppress hotkeys when using them would be unsafe; these stability protections are not user preferences.

## Debug Mode

Debug Mode is off by default for normal release use. When enabled from Settings, it shows diagnostic controls and debug-only views such as the route state inspector and screenshot options, and it enables Keep Debug Screenshots by default until the user turns screenshot retention off. Visual Studio Debug runs force debug evidence internally, so those settings are not shown as editable options there. Success screenshots remain disabled unless `Debug.EnableSuccessScreenshots` is explicitly set to `true`.

Visual Studio Debug uses a taller diagnostic window so the Goblin Tracker evidence and Last Observation fields remain readable. Release-user launches stay compact and show diagnostics only after Debug Mode is enabled.

Debug tooling includes:

- Paired Diablo/app screenshots for failure milestones by default, with success milestone capture only when explicitly enabled.
- Optional debug screenshot retention.
- Missing-template diagnostics and optional capture prompts.
- Route and workflow summaries in generated debug packages.
- Goblin Tracker count, counted-area summary, active combat time, and GPH in session summaries, runtime metadata, diagnostics, and debug package manifests.
- Automation Observation Mode diagnostics for per-goblin Goblin Evidence setup, with manual template guidance under `Images\Goblin Evidence`; automatic goblin counting is not enabled.
- Count-based GoblinEvidence retention under `Debug\GoblinEvidence`, defaulting to the newest 250 files, plus bounded ObservationDiagnostics crop samples in debug packages.
- Detailed launch, Start Game, teleport, repair, salvage, bounty, and Exit Game logs.

Generate a debug package from the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\create-debug-package.ps1
```

Debug packages include failure screenshots by default and include debug screenshots according to the active screenshot settings. Success screenshots are excluded from package ZIPs unless the script is run with `-IncludeSuccessScreenshots`; the manifest can still report how many success screenshots are available on disk. Goblin Evidence ObservationDiagnostics crops are limited to a recent sample so packages remain compact while still showing the latest scanner evidence.

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

## Local Source Uploads

Local developer checkouts can keep private ChatGPT source-upload helpers under `Scripts\Local Tools\`. Those helpers and their timestamped ZIP output under `ChatGPT Uploads\` are intentionally ignored by Git; the user-facing debug package launcher remains tracked separately under `Scripts\`.

## Git Sync Helper

`Scripts\Git Commit Push.bat` is a safe local helper for reviewing, committing, and optionally pushing repository changes from Windows Explorer. It shows the current branch and full `git status`, waits for confirmation before running `git add -A`, refuses blank commit messages, and only pushes after the commit succeeds and the user confirms the push.

Usage:

```text
Double-click Scripts\Git Commit Push.bat
```

Example workflow:

1. Review the displayed branch and git status.
2. Press any key to stage all changes.
3. Enter a commit message.
4. Choose whether to push the commit to the current tracked branch.

## Release Build

Release versioning starts in `GoblinFarmer.csproj`. Before publishing a new release, update:

```xml
<Version>1.3.0</Version>
<AssemblyVersion>1.3.0.0</AssemblyVersion>
<FileVersion>1.3.0.0</FileVersion>
<InformationalVersion>1.3.0</InformationalVersion>
```

Then publish the self-contained Windows build. You can publish from Visual Studio using the `GoblinFarmerRelease` publish profile, or run the publish script from the project root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\publish-release.ps1
```

The script publishes to `artifacts\publish\GoblinFarmer`, verifies executable version metadata, icon, config, Images output, and `Scripts\create-debug-package.ps1`, then compiles `Installer\GoblinFarmer.iss` if Inno Setup is installed. The installer reads the version from the published executable, so the app must be published before compiling the `.iss` file.

`artifacts\` is generated output and is intentionally ignored by Git. Do not commit published app folders or installer binaries.

If Inno Setup is installed separately, compile the installer after publishing:

```powershell
ISCC.exe "/DSourceDir=artifacts\publish\GoblinFarmer" "Installer\GoblinFarmer.iss"
```

Manual release flow:

1. Update version metadata in `GoblinFarmer.csproj`.
2. Update `Docs\Project_Status.md`, `README.md`, and release notes/checklists for the changes being shipped.
3. Run `Scripts\publish-release.ps1`, or publish from Visual Studio and upload assets manually.
4. Confirm the installer name and version, such as `GoblinFarmerSetup-1.3.0.exe`.
5. For major app milestones only, create a GitHub Release manually, then confirm the release uses the notes from `Docs/Release_v1.3.md` and includes the installer asset.

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

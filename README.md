# GoblinFarmer

GoblinFarmer is a personal Windows Forms automation project for Diablo III farming workflows. It combines image recognition, route-aware teleporting, Battle.net and Diablo launch support, combat helpers, town automation, and diagnostic tooling for troubleshooting long sessions.

This is a personal automation project, not an official Blizzard product. Use it at your own risk and only in environments where you understand and accept the relevant game, account, and automation-policy implications.

## Screenshot

![GoblinFarmer main window](Docs/Goblin%20Farmer%20v1.4.0.png)

## Features

- Battle.net launch flow with configured-path-first startup, visible-window detection, retry logging, and Play button image recognition.
- Diablo launch detection and Start Game handling with stable-button verification, click acceptance checks, and manual-click recovery.
- Route-aware Teleport Next workflow for the configured farming path.
- Hotkey route blocking for known unsafe or incorrect location transitions.
- Make New Game, Exit Game, repair, salvage, and town workflow helpers.
- Monk, Demon Hunter, and Witch Doctor combat support.
- Location-aware Goblin Tracker with automatic evidence-based counting, per-area duplicate protection, active-combat-time GPH, live UI stats, accepted-count notifications, and reset support.
- Debug Mode with diagnostic panes, screenshot controls, route state inspection, and one batch-driven debug package workflow for VS Debug and Release.
- Release and Visual Studio Debug layouts that keep the expanded Goblin Tracker evidence and Last Observation fields readable while release diagnostic tabs stay hidden until Debug Mode is enabled.
- Runtime configuration for Diablo III, Battle.net, image templates, launch timings, diagnostic behavior, and VS Debug project-root config persistence.
- Self-contained Windows release publishing plus optional Inno Setup installer packaging.
- Dynamic app title and synchronized installer/application versioning.

## Installation

For the v1.4 installer, use:

```text
GoblinFarmerSetup-1.4.0.exe
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

Manual route button clicks show brief no-activate feedback when a teleport is queued or when the app detects it is already at the requested target, so fast route-button completions do not steal Diablo focus or appear silent.

## Goblin Tracker

The Goblin Tracker counts goblins from fresh Journal or Minimap evidence when both `GoblinTracker.EnableObservationMode=true` and `GoblinTracker.EnableAutomaticCounting=true`. VS Debug builds expose these as `Observation Mode` and `Auto Goblin Count` checkboxes. Observation Mode can still run without automatic counting, in which case Last Observation updates but GoblinCount, GPH, found records, and area slots are unchanged.

Goblin evidence templates are discovered from `Images\Goblin Evidence` using per-goblin Journal and Minimap file names such as `<Goblin Type> Engaged Journal.png`, `<Goblin Type> Killed Journal.png`, `Killed <Goblin Type> Journal.png`, and `<Goblin Type> Minimap.png`. Journal evidence is primary when both Journal and Minimap match, while minimap evidence provides fast presence/type support. Treasure Goblin / Odious Collector and Gilded Baron / Malevolent Tormentor minimap matches use targeted color diagnostics to reduce type swaps.

Automatic counting uses the same resolved-area, route-context, block-list, duplicate, stale-evidence, and area-limit rules as the validated manual workflow. Normal areas allow one count per game. Pandemonium Fortress Level 1, Pandemonium Fortress Level 2, and Stinging Winds allow two counts per game before further counts are suppressed. Blocked count areas include New Tristram, WhimsyDale, City of Caldeum, Gates of Caldeum, Caldeum Bazaar, Flooded Causeway, Ancient Waterway, and The Bridge Of Korsikk.

The physical `X` Goblin Tracker hotkey has been retired. In VS Debug, the `Capture` button is a manual image-recognition aid only: it writes fullscreen, minimap, journal, and metadata files under `Debug\GoblinEvidence\ManualCaptures` when clicked. Normal Goblin Tracker and counter workflows still automatically write the debug files needed for troubleshooting, including decision bundles and accepted-encounter fullscreen/minimap/journal captures.

Journal matches validate the goblin-name portion of the template, ignore upper/history rows, and briefly suppress Journal candidates after physical Enter/journal-history input so old history rows do not replay into a new area. Automatic counts require fresh eligible evidence first seen after automatic counting was armed, scope evidence by resolved area, suppress already-used evidence signatures, suppress same-encounter source variants, and prevent counted Journal evidence from being reused in another area. Last Observation stays on the last real goblin until a new observation/count, reset/new-game cleanup, or confirmed current-area change replaces it.

Debug logs include Last Observation update/clear/preserve state, Observation Mode configuration, scanner start/stop with a 500ms interval, scan attempts/skips, candidate checks with match points, calibrated Journal/Minimap scan regions, journal freshness/name/history diagnostics, minimap color diagnostics, automatic-count accepted/suppressed decisions, structured `GoblinDecisionTrace` lines, route-button cancellation/start diagnostics, encounter-capture paths, and manual recognition-capture paths.

Journal evidence treats both killed and escaped goblin lines as encounters. `Gelatinous Spawn` journal kills are normalized to `Gelatinous Sire`.

Current found list: Gem Hoarder, Malevolent Tormentor, Treasure Goblin, Rainbow Goblin, Insufferable Miscreant, Blood Thief, Odious Collector, Gelatinous Sire, Menagerist, Gilded Baron.

Still useful: Gilded Baron / Malevolent Tormentor minimap-specific validation.

The main window shows:

```text
Goblins: 0
GPH: 0.00
Active Time: 00:00:00
Last Observation:
--
--
--
No current observation
```

GPH uses tracker active combat time only and calculates as soon as active combat time is greater than zero. `Reset Stats` clears the count, tracker active time, GPH, the in-memory counted-area duplicate guard, Goblin Evidence cooldowns, and Last Observation/manual observation reuse state; if combat is currently running, tracker active time restarts from the reset moment. Starting a new game clears the counted-area guard plus Goblin Evidence observation/cooldown state so the same area can be counted again in the next game with fresh evidence context.

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

Witch Doctor combat starts the `2, 3, 1` key loop, mouse-wheel loop, and cursor-change left-click loop together. It uses mouse wheel scrolling during active combat and sends discrete left-click pulses only when the Diablo cursor changes. It does not hold left or right mouse down.

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

During an active teleport arrival-confirmation wait, the combat hotkey cancels the wait and starts combat from that same hotkey press after the interrupted teleport workflow unwinds. Physical `2` Exit Game cancels the wait and leaves the normal interrupted-teleport recovery path in control. Teleport Next from plain Ancient Waterway blocks when Stinging Winds is queued; Eastern Channel Level 2 remains the child route that can continue to Stinging Winds, Western Channel Level 2 returns to Ancient Waterway, and clicking the Ancient Waterway route button while already inside Ancient Waterway remains blocked.

If Teleport Next is accepted but no queued route target exists for the current route state, GoblinFarmer shows a short no-activate notification instead of silently ignoring the hotkey.

## Debug Mode

Debug Mode is off by default for normal release use. When enabled from Settings, it shows diagnostic controls and debug-only views such as the route state inspector and screenshot options, and it enables Keep Debug Screenshots by default until the user turns screenshot retention off. Visual Studio Debug runs force debug evidence internally, so those settings are not shown as editable options there. Success screenshots remain disabled unless `Debug.EnableSuccessScreenshots` is explicitly set to `true`.

Debug and release artifacts keep evidence useful but bounded by default. VS Debug troubleshooting artifacts and release/debug-mode artifacts use a 7-day age-retention window. Success screenshots are excluded from release/export packages unless explicitly requested, failure screenshot groups and current-session `debug-screenshots` are capped to recent samples, and Goblin Evidence crops/event screenshots use their own count and package limits. Successful Battle.net app-click launches do not create false failure screenshots for manual-play suspicion or brief post-launch Battle.net visibility. The package manifest reports included and excluded screenshot counts and sizes.

For both VS Debug and Release troubleshooting, use the single ZIP export path: `Scripts\Create Debug Package.bat`, which launches `Scripts\create-debug-package.ps1`. The batch script self-discovers the runtime/package roots, writes the ZIP under `DebugPackages`, and includes the latest logs, manifests, route summaries, Goblin Tracker summaries, screenshots, GoblinEvidence, encounter captures, decision bundles, observation diagnostics, and root analysis files: `debug-package-analysis.txt`, `goblin-tracker-timeline.md`, and `goblin-evidence-health.txt`. Closing the VS Debug form is intentionally quiet and does not generate packages or loose review files.

Release and Visual Studio Debug use a taller Goblin Tracker group so the evidence and Last Observation fields remain readable. Release-user launches keep diagnostic tabs hidden until Debug Mode is enabled.

Debug tooling includes:

- Paired Diablo/app screenshots for failure milestones by default, with success milestone capture only when explicitly enabled.
- Optional debug screenshot retention.
- Missing-template diagnostics and optional capture prompts.
- Route and workflow summaries in generated debug packages.
- Root debug package analysis, Goblin Tracker timeline, Goblin Evidence health report, Goblin Tracker review index and summary, encounter captures, decision bundles, and observation diagnostics collected by the package batch when GoblinEvidence is available.
- Goblin Tracker count, counted-area summary, active combat time, and GPH in session summaries, runtime metadata, diagnostics, and debug package manifests.
- Automation Observation Mode diagnostics for per-goblin Goblin Evidence setup, with manual template guidance under `Images\Goblin Evidence`; automatic goblin counting is a separate opt-in gate and defaults off.
- Age-based debug artifact retention: 7 days for VS Debug troubleshooting folders and release/debug-mode artifacts, plus count-based GoblinEvidence retention under `Debug\GoblinEvidence` defaulting to the newest 250 files and bounded ObservationDiagnostics crop samples in debug packages.
- Detailed launch, Start Game, teleport, repair, per-slot salvage timing, Kadala timing, bounty, and Exit Game logs.

Generate a debug package by double-clicking `Scripts\Create Debug Package.bat`. The batch creates one ZIP from the existing live runtime evidence. From the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\create-debug-package.ps1
```

Debug packages include failure screenshots by default and include debug screenshots according to the active screenshot settings. Success screenshots are excluded from package ZIPs unless the script is run with `-IncludeSuccessScreenshots`; the manifest can still report how many success screenshots are available on disk. Goblin Evidence ObservationDiagnostics crops are limited to a recent sample so packages remain compact while still showing the latest scanner evidence. Optional focused helper scripts are available under `Scripts\` for package analysis, timeline generation, evidence-health checks, and local development verification, but normal test handoff still requires only the batch-created ZIP.

## Release v1.4

GoblinFarmer v1.4 is focused on Goblin Tracker automatic-count readiness, Observation Mode diagnostics, Witch Doctor input reliability, startup validation, and package-size hardening.

Highlights:

- Added Goblin Tracker Automation Observation Mode for Journal and Minimap evidence.
- Added gated automatic Goblin Tracker counting that requires Observation Mode and Auto Goblin Count to both be enabled.
- Retired the physical `X` Goblin Tracker hotkey and added a VS Debug `Capture` button for image-recognition troubleshooting snapshots.
- Hardened automatic counting with fresh-evidence gating, stale journal protection, blocked-area suppression, and per-area duplicate limits.
- Added two-count exceptions for Pandemonium Fortress Level 1, Pandemonium Fortress Level 2, and Stinging Winds.
- Improved Last Observation diagnostics and accepted-count display behavior.
- Added Witch Doctor mouse-wheel plus cursor-change left-click combat input.
- Improved startup path validation and Diablo III auto-discovery.
- Reduced default debug package size from Goblin Evidence and screenshot artifacts.

Quality improvements:

- Added bounded Goblin Evidence diagnostics and clearer scanner/candidate logs.
- Preserved automatic Goblin Tracker debug artifacts while making the manual `Capture` button supplemental only.
- Improved Reset Stats/New Game cleanup for Goblin Tracker observation and duplicate state.
- Improved salvage timing diagnostics and per-slot speed.
- Synchronized release version metadata and installer EXE metadata for v1.4.

Version details:

- App title: `GoblinFarmer v1.4.0`
- EXE `FileVersion`: `1.4.0.0`
- EXE `ProductVersion`: `1.4.0`
- Installer version: `1.4.0`
- Expected installer artifact: `GoblinFarmerSetup-1.4.0.exe`

See [Docs/Release_v1.4.md](Docs/Release_v1.4.md) for ready-to-copy GitHub release notes.

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
<Version>1.4.0</Version>
<AssemblyVersion>1.4.0.0</AssemblyVersion>
<FileVersion>1.4.0.0</FileVersion>
<InformationalVersion>1.4.0</InformationalVersion>
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
4. Confirm the installer name and version, such as `GoblinFarmerSetup-1.4.0.exe`.
5. For major app milestones, create a GitHub Release, then confirm the release uses the notes from the matching `Docs/Release_v*.md` file and includes the installer asset when available.

See [Docs/Release_Checklist.md](Docs/Release_Checklist.md) before publishing a final build.

## Project Docs

- [CHANGELOG.md](CHANGELOG.md): user-facing release history.
- [Docs/Release_v1.4.md](Docs/Release_v1.4.md): ready-to-copy GitHub release notes.
- [Docs/Release_v1.3.md](Docs/Release_v1.3.md): ready-to-copy GitHub release notes.
- [Docs/CombatProfiles.md](Docs/CombatProfiles.md): supported combat profiles and required skill setup.
- [Docs/Project_Status.md](Docs/Project_Status.md): detailed development status and implementation history.
- [Docs/TODO.md](Docs/TODO.md): development and validation backlog.
- [Docs/Release_Checklist.md](Docs/Release_Checklist.md): release validation checklist.

## License

GoblinFarmer is released under the [MIT License](LICENSE).

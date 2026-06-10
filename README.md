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
- Make New Game, Exit Game, repair, salvage, and town workflow helpers. Salvage uses a single cached inventory scan per open salvage UI cycle, can run a gated blue/yellow bulk category pass when enabled, skips normal gems as non-salvageable, then clicks grouped top-cell item anchors with confirmation-aware handling for set and legendary-style targets.
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

The selected combat profile and Hotkeys checkbox states are saved under `User` and restored when GoblinFarmer starts. Visual Studio Debug runs prefer an ignored project-level `Config\AppSettings.local.json` when present, then fall back to the sanitized tracked `Config\AppSettings.json`; release and installed runs use the app-local copy beside the executable.

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

The Goblin Tracker counts goblins from fresh Minimap evidence, fresh Journal Killed evidence, or same-area Journal Engaged evidence that remains visible during active combat for a short sustained-confirmation window when both `GoblinTracker.EnableObservationMode=true` and `GoblinTracker.EnableAutomaticCounting=true`. First-seen Journal Engaged-only evidence is treated as diagnostic/pending confirmation and does not increment immediately. If Journal Engaged and a strong Minimap confirmation are found in the same scan, the Minimap confirmation is allowed to drive the immediate count through the normal stale, blocked-area, duplicate, and area-limit guards. VS Debug builds expose these as `Observation Mode` and `Auto Goblin Count` checkboxes. Observation Mode can still run without automatic counting, in which case Last Observation updates but GoblinCount, GPH, found records, and area slots are unchanged.

Goblin evidence templates are discovered from `Images\Goblin Evidence` using per-goblin Journal and Minimap file names such as `<Goblin Type> Engaged Journal.png`, `<Goblin Type> Killed Journal.png`, `Killed <Goblin Type> Journal.png`, and `<Goblin Type> Minimap.png`. Journal evidence is primary when both Journal and Minimap match and both are actionable, while minimap evidence provides fast presence/type support and can override a pending Journal Engaged match when its confidence is strong enough for automatic counting. Treasure Goblin / Odious Collector and Gilded Baron / Malevolent Tormentor minimap matches use targeted color diagnostics to reduce type swaps.

Automatic counting uses the same resolved-area, route-context, block-list, duplicate, stale-evidence, and area-limit rules as the validated manual workflow. Normal areas allow one count per game. Pandemonium Fortress Level 1, Pandemonium Fortress Level 2, and Stinging Winds allow two counts per game before further counts are suppressed. PF1/PF2 can count a second real same-type goblin when a repeated Minimap signature appears after a conservative delay with fresh supporting evidence, while immediate duplicates and third PF counts still suppress. Blocked count areas include New Tristram, WhimsyDale, City of Caldeum, Gates of Caldeum, Caldeum Bazaar, Flooded Causeway, Ancient Waterway, and The Bridge Of Korsikk.

The physical `X` Goblin Tracker hotkey has been retired. In VS Debug, the `Capture` button is a manual image-recognition aid only: it writes fullscreen, minimap, journal, and metadata files under `Debug\GoblinEvidence\ManualCaptures` when clicked. VS Debug also includes a developer-only `Sim Count` area selector that pins `Current Area` first, alphabetizes the centralized route/policy area list, and uses the same block-list, duplicate-guard, and area-limit rules as real counts. Normal Goblin Tracker and counter workflows still automatically write the debug files needed for troubleshooting, including replay-ready decision bundles and accepted-encounter fullscreen/minimap/journal captures. Normal scanner events skip redundant root `GoblinEvidence_*` full/event images by default to keep local storage growth lower.

Journal matches validate the goblin-name portion of the template, ignore upper/history rows, and briefly suppress Journal candidates after physical Enter/journal-history input so old history rows do not replay into a new area. Automatic counts require fresh eligible evidence first seen after automatic counting was armed, scope evidence by resolved area, suppress already-used evidence signatures, suppress same-encounter source variants while the stale source variant remains visible, and prevent counted Journal evidence from being reused in another area. Journal Engaged-only evidence first reports `JournalPendingKilledOrMinimapConfirmation`; it can count later only when the same area/evidence remains visible during active combat long enough to log `JournalEngagedSustainedActiveCombat`, or when fresh Journal Killed/Minimap evidence confirms the encounter. Same-area duplicate Journal evidence refreshes the remembered encounter row so that visible text cannot become a fresh count after the next teleport, while cross-area stale Journal suppression does not block a later fresh Minimap hit in the new area. Reset Stats and Make New Game remember already-visible Journal rows before clearing state so previous-game text cannot immediately recount under the new area, even if the visible line shifts upward by several row buckets. Minimap-only duplicate checks stay area-aware so repeated tight icon signatures in different route areas do not block fresh counts, while delayed Journal rows after recent Minimap counts remain attached to the original encounter. Recent same-goblin Minimap context also helps Journal follow-up evidence stay bound to Channel Level 1/2 instead of false Pandemonium Fortress title matches. Last Observation stays on the most recent accepted count until Reset Stats, Make New Game, app restart, or a newer accepted count replaces it; stale, suppressed, no-candidate, teleport, and area-change paths do not clear that accepted-count display.

Accepted-count notifications are no-activate and click-through so they do not steal focus or block route clicks. Rainbow Goblin automatic counts use a distinct alert message and local Windows alert sound.

Debug logs include Last Observation update/clear/preserve state, Observation Mode configuration, scanner start/stop with a 500ms interval, scan attempts/skips, scan-stage timing summaries through `GoblinEvidenceTimingSummary`, candidate checks with match points, calibrated Journal/Minimap scan regions, journal freshness/name/history diagnostics, minimap color diagnostics, automatic-count accepted/suppressed decisions, notification latency traces through `GoblinLatencyTrace`, structured `GoblinDecisionTrace` lines, structured JSONL Goblin Tracker events, route-button cancellation/start diagnostics, encounter-capture paths, manual recognition-capture paths, and salvage bulk/category/cache/timing diagnostics with `cacheMode=SingleInventoryScan`, item footprint metadata, regular-gem skip metrics, and expected-confirmation results.

Goblin Replay is an explicit developer/test harness for feeding saved Journal/Minimap PNG fixtures, including real-style encounter/manual capture folders, through the shared Goblin Evidence matcher and stale-location policy without Diablo running. It is on-demand only and is not part of normal app startup, VS Debug startup, live scanning, combat, route, town, or debug package workflows. Real capture evidence can be replayed from the repository root with:

```powershell
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -- --goblin-replay-captures --templates ".\Images\Goblin Evidence" "D:\Path\To\CaptureFolder1" "D:\Path\To\CaptureFolder2"
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -- --goblin-replay-metadata --templates ".\Images\Goblin Evidence" "D:\Path\To\Capture_Metadata.txt"
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -- --goblin-replay-prefix --templates ".\Images\Goblin Evidence" "D:\Path\To\CapturePrefix"
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -- --goblin-replay-decision-bundle --templates ".\Images\Goblin Evidence" "D:\Path\To\DecisionBundles\gdt-xxxxxxxx"
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -- --goblin-replay-scenario --templates ".\Images\Goblin Evidence" "D:\Path\To\Scenario.txt"
```

Capture folders should contain `_Metadata.txt`, `_Journal.png`, and/or `_Minimap.png` files. Metadata and prefix commands target one specific older capture inside a shared `EncounterCaptures` or `ManualCaptures` folder without copying files. New DecisionBundles are replay-ready by default because they contain `decision-trace.txt`, local metadata, and local Journal/Minimap crop frames. Older DecisionBundles that only have `decision-trace.txt` plus fullscreen `evidence.png` still report the available evidence and explain that crop-accurate replay is not possible from that bundle alone. Scenario files synthesize temporary crop frames from `Images\Goblin Evidence` templates and can resolve area context from `Images\Current Location` title templates with `Location=...`. They can model small action sequences such as `Scan`, `NewGame`, `ResetStats`, and `Wait`. Example scenario lines:

```text
Scenario=New Game reset carryover
Step=Weeping fresh killed|Scan|Area=The Weeping Hollow|Journal=Blood Thief Killed Journal.png|JournalLineBucket=10|AdvanceSeconds=1
Step=Make New Game|NewGame|AdvanceSeconds=6
Step=Southern old visible killed|Scan|Area=Southern Highlands|Journal=Blood Thief Killed Journal.png|JournalLineBucket=9|AdvanceSeconds=1
Step=Eastern Channel fresh killed|Scan|Location=Eastern Channel Level 1.png|Journal=Blood Thief Killed Journal.png|JournalLineBucket=10|AdvanceSeconds=1
```

The replay commands print load results, replay decisions, and a summary, and they do not create persistent debug files by default.

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

GPH uses tracker active combat time only and calculates as soon as active combat time is greater than zero. `Reset Stats` clears the count, tracker active time, GPH, the in-memory counted-area duplicate guard, Goblin Evidence cooldowns, and Last Observation/manual observation reuse state; if combat is currently running, tracker active time restarts from the reset moment. Starting a new game applies the same Goblin Tracker reset behavior so the next game starts with clean counts, active time, GPH, duplicate guard, evidence state, and Last Observation.

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

Demon Hunter combat keeps right mouse held after a safe start, continues the key loop independently, and preserves the no-click UI blacklist. If the physical cursor drifts into a protected HUD region during the sustained right-hold loop, the app can send the left-click pulse at a verified safe playfield fallback point instead of repeatedly clicking or suppressing over the UI.

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

Debug and release artifacts keep evidence useful but bounded by default. VS Debug troubleshooting artifacts and release/debug-mode artifacts use a 7-day age-retention window, and debug package ZIP count retention keeps the newest 20 matching packages by default. Success screenshots are excluded from release/export packages unless explicitly requested, failure screenshot groups and current-session `debug-screenshots` are capped to recent samples, and Goblin Evidence crops/event screenshots use their own count and package limits. Normal scanner events skip redundant root `GoblinEvidence_*` full/event images by default, DecisionBundles keep replay-ready Journal/Minimap crops and metadata without copying full evidence images by default, and debug ZIPs exclude old DecisionBundle full images plus Encounter/ManualCapture fullscreen images unless explicitly opted in. Successful Battle.net app-click launches do not create false failure screenshots for manual-play suspicion or brief post-launch Battle.net visibility. The package manifest reports included and excluded screenshot counts and sizes.

For both VS Debug and Release troubleshooting, use the single ZIP export path: `Scripts\Create Debug Package.bat`, which launches `Scripts\create-debug-package.ps1`. The batch script self-discovers the runtime/package roots, writes the ZIP under `DebugPackages`, and includes the latest logs, manifests, route summaries, Goblin Tracker summaries, bounded screenshots, GoblinEvidence, replay-ready encounter captures, decision bundles, observation diagnostics, and root analysis files: `debug-package-analysis.txt`, `goblin-tracker-timeline.md`, and `goblin-evidence-health.txt`. Closing the VS Debug form is intentionally quiet and does not generate packages or loose review files.

For AI-assisted development handoff context, `Scripts\Create Project Brain.bat` creates a timestamped docs-only ZIP under `ProjectBrain` from the allowlisted project instructions, README, and current status/reference docs when present.

The tracked `Scripts` folder keeps `Create Debug Package.bat` and `Create Project Brain.bat` as the normal package launchers. Their backing scripts are `create-debug-package.ps1` and `create-project-brain.ps1`; `debug-analysis-tools.ps1` remains as a direct dependency of the debug package workflow. `Cleanup Project.bat`, `Cleanup Project Delete.bat`, and `cleanup-project.ps1` are maintenance-only exceptions for generated artifact cleanup. Older manual helpers are archived under `Docs\ScriptArchive\2026-06-09` for reference rather than active use.

Project Brain topic summaries live under `Docs\ProjectBrain`. They organize current context for ChatGPT/Codex while keeping `Docs\Project_Status.md` as the source of truth.

Release and Visual Studio Debug use a taller Goblin Tracker group so the evidence and Last Observation fields remain readable. Release-user launches keep diagnostic tabs hidden until Debug Mode is enabled.

Debug tooling includes:

- Paired Diablo/app screenshots for failure milestones by default, with success milestone capture only when explicitly enabled.
- Optional debug screenshot retention.
- Missing-template diagnostics and optional capture prompts.
- Route and workflow summaries in generated debug packages.
- Root debug package analysis, Goblin Tracker timeline, Goblin Evidence health report, Goblin Tracker review index and summary, encounter captures, decision bundles, and observation diagnostics collected by the package batch when GoblinEvidence is available.
- Structured Goblin Tracker JSONL events under `Debug\GoblinEvidence\GoblinTrackerEvents.jsonl` during VS Debug, Debug Mode, or decision-trace sessions, alongside human-readable logs.
- Goblin Evidence scan-stage timing summaries for spotting slow template, crop, or candidate stages without adding one-off stopwatch logs.
- Explicit/on-demand Goblin Replay fixture runner support for saved Journal/Minimap PNG detection tests, targeted multi-step stale-evidence fixtures, real capture folders, specific metadata files, capture prefixes, replay-ready DecisionBundle references, and template-scenario files synthesized from `Images\Goblin Evidence` plus optional `Images\Current Location` area resolution; replay is not wired into startup, live automation, or debug package creation.
- Goblin Tracker count, counted-area summary, active combat time, and GPH in session summaries, runtime metadata, diagnostics, and debug package manifests.
- Automation Observation Mode diagnostics for per-goblin Goblin Evidence setup, with manual template guidance under `Images\Goblin Evidence`; automatic goblin counting is a separate opt-in gate and defaults off.
- Age-based debug artifact retention: 7 days for VS Debug troubleshooting folders and release/debug-mode artifacts, plus count-based debug package retention defaulting to the newest 20 ZIPs, GoblinEvidence retention under `Debug\GoblinEvidence` defaulting to the newest 250 files, and bounded ObservationDiagnostics crop samples in debug packages.
- Detailed launch, Start Game, teleport, repair, bulk/per-slot salvage timing, Kadala timing, bounty, and Exit Game logs.

Generate a debug package by double-clicking `Scripts\Create Debug Package.bat`. The batch creates one ZIP from the existing live runtime evidence. From the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\create-debug-package.ps1
```

Debug packages include failure screenshots by default and include debug screenshots according to the active screenshot settings. Success screenshots are excluded from package ZIPs unless the script is run with `-IncludeSuccessScreenshots`; the manifest can still report how many success screenshots are available on disk. Goblin Evidence ObservationDiagnostics crops are limited to a recent sample, old DecisionBundle `evidence.*` full images are excluded by default, and Encounter/ManualCapture fullscreen images are excluded by default while their Journal/Minimap crops and metadata remain included for replay.

The Project Brain script can also generate a paste-friendly storage report without deleting files:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\create-project-brain.ps1 -StorageBreakdownOnly
```

The report is written to `Reports\Storage_Breakdown.md` and highlights build output, debug packages, runtime evidence, logs, screenshots, installers/packages, and replay/capture storage before any separate cleanup decision.

Generated artifact cleanup is dry-run by default:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\cleanup-project.ps1
```

Real deletion requires `-Delete`. `Scripts\Cleanup Project Delete.bat` is a confirmation-gated launcher for the default delete mode: generated build outputs plus debug package retention pruning. Use direct PowerShell with `-RuntimeArtifacts` only for disposable runtime logs/screenshots/evidence/replay captures, and `-PruneOldInstallers` only when older installers should be removed while keeping the latest installer. The cleanup report is written to `Reports\Cleanup_Report.md`.

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
- Improved salvage timing diagnostics, per-slot speed, and gated blue/yellow bulk salvage validation support.
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

Local developer checkouts can keep private ChatGPT source-upload helpers under `Scripts\Local Tools\`. Those helpers and their timestamped ZIP output under `ChatGPT Uploads\` are intentionally ignored by Git; tracked package launchers remain limited to `Scripts\Create Debug Package.bat` and `Scripts\Create Project Brain.bat`, with `Scripts\Cleanup Project.bat` and `Scripts\Cleanup Project Delete.bat` as maintenance-only cleanup exceptions.

## Release Build

Release versioning starts in `GoblinFarmer.csproj`. Before publishing a new release, update:

```xml
<Version>1.4.0</Version>
<AssemblyVersion>1.4.0.0</AssemblyVersion>
<FileVersion>1.4.0.0</FileVersion>
<InformationalVersion>1.4.0</InformationalVersion>
```

Then publish the self-contained Windows build. Use Visual Studio with the `GoblinFarmerRelease` publish profile, or run the equivalent command from the project root:

```powershell
dotnet publish .\GoblinFarmer.csproj --configuration Release --runtime win-x64 --self-contained true --output .\artifacts\publish\GoblinFarmer -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=true
```

The publish output goes to `artifacts\publish\GoblinFarmer`. Confirm executable version metadata, icon, config, Images output, `Scripts\create-debug-package.ps1`, `Scripts\debug-analysis-tools.ps1`, and `Scripts\Create Debug Package.bat` before compiling the installer. The installer reads the version from the published executable, so the app must be published before compiling the `.iss` file.

`artifacts\` is generated output and is intentionally ignored by Git. Do not commit published app folders or installer binaries.

If Inno Setup is installed separately, compile the installer after publishing:

```powershell
ISCC.exe "/DSourceDir=artifacts\publish\GoblinFarmer" "Installer\GoblinFarmer.iss"
```

Manual release flow:

1. Update version metadata in `GoblinFarmer.csproj`.
2. Update `Docs\Project_Status.md`, `README.md`, and release notes/checklists for the changes being shipped.
3. Publish from Visual Studio or run the `dotnet publish` command above, then upload assets manually.
4. Confirm the installer name and version, such as `GoblinFarmerSetup-1.4.0.exe`.
5. For major app milestones, create a GitHub Release, then confirm the release uses the notes from the matching `Docs/Release_v*.md` file and includes the installer asset when available.

See [Docs/Release_Checklist.md](Docs/Release_Checklist.md) before publishing a final build.

## Project Docs

- [CHANGELOG.md](CHANGELOG.md): user-facing release history.
- [Docs/Release_v1.4.md](Docs/Release_v1.4.md): ready-to-copy GitHub release notes.
- [Docs/Release_v1.3.md](Docs/Release_v1.3.md): ready-to-copy GitHub release notes.
- [Docs/CombatProfiles.md](Docs/CombatProfiles.md): supported combat profiles and required skill setup.
- [Docs/Project_Status.md](Docs/Project_Status.md): brief current release status, stable behavior, and next development plans.
- [Docs/ProjectBrain/README.md](Docs/ProjectBrain/README.md): index for compact ChatGPT/Codex context summaries.
- [Docs/TODO.md](Docs/TODO.md): development and validation backlog.
- [Docs/History.md](Docs/History.md): historical package reviews, retired workflows, and backlog context.
- [Docs/Release_Checklist.md](Docs/Release_Checklist.md): release validation checklist.

## License

GoblinFarmer is released under the [MIT License](LICENSE).

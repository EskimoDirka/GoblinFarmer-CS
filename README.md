# GoblinFarmer

GoblinFarmer is a Windows Forms C# assistant for Diablo III farming workflows. It combines image recognition, route-aware teleporting, Battle.net launch support, combat helpers, town automation, and diagnostic tooling for troubleshooting long farming sessions.

This README is intentionally high-level and user-facing. The detailed developer source of truth is [Docs/Project_Status.md](Docs/Project_Status.md).

## Current Status

The project is in an active reliability and release-readiness phase. Core route, combat, diagnostic, and launch systems are implemented, while current validation is focused on centralized config, release UI cleanup, Debug screenshot defaults, startup form sizing, route failure summaries, Hotkeys visibility polish, Whimsy hotkey route blocking, Start Game retry monitoring, Battle.net visible-window startup/close behavior, and Bounty Complete auto-close during combat.

The latest reviewed package, `GoblinFarmer_Debug_20260602_221905.zip`, showed Bounty Complete detection now works during combat, but the app needed the old Python behavior ported more directly. Combat-active bounty popups now use a dedicated combat-menu watcher that polls the bounty title region and sends an automation-safe injected Escape with a 1s cooldown, while the app stop watcher ignores that injected Escape. The current recommended validation target is a focused run for WhimsyDale blocking, Bounty Complete combat-close, Start Game consistency, and config toggles. See [Docs/Project_Status.md](Docs/Project_Status.md) for the exact current focus and latest known issues.

## Stable Systems

- Teleport Routing tracks the configured farming route and preserves raw, normalized, display, and blocking locations separately.
- Teleport Blocking prevents known bad Teleport Next route transitions, such as blocked Cathedral, City of Caldeum, Ancient Waterway, Stinging Winds, Caverns of Frost Level 1, WhimsyDale, and Cave Of The Moon Clan Level 1 cases.
- Manual teleport buttons are intentionally allowed and bypass route blocking.
- Teleport Next can advance route state from a fresh current-location scan when the player is already at the queued destination, skipping redundant waypoint clicks and starting the following route teleport.
- After combat stops, Teleport Next may have a slight intentional delay before it is available again. This lets combat/input state settle and helps avoid accidental or unsafe teleport attempts.
- Teleport Retry Logic preserves failed or interrupted manual and hotkey teleport state until arrival confirmation succeeds or the user explicitly changes course.
- Ancient Waterway arrival confirmation requires the exact Ancient Waterway title, while channel child locations remain available for route and blocking decisions.
- Combat Automation includes current Monk, Demon Hunter, and Witch Doctor support.
- Combat no-click safety suppresses physical mouse clicks in known UI regions, including the extended lower-right hover menu area, without moving the cursor or stopping combat.
- Demon Hunter right-hold starts only from a safe world area and remains held through hover/no-click regions to better match the old Python app feel.
- Demon Hunter sustained combat reports active right-held no-click suppression instead of appearing stopped when shared left-click input is suppressed over UI.
- Witch Doctor held/channel input starts only from a safe world area and remains held through combat no-click regions without sending new mouse clicks.
- Combat keyboard filtering suppresses physical `1`/`2` during combat while allowing injected automation key events through.
- The Hotkeys panel shows default-on entries for `1 - Teleport Next Location` and `2 - Exit Game`.
- Physical `2` starts Exit Game when Diablo is focused and combat is inactive.
- Battle.net Launch Flow can relaunch or focus Battle.net, treats only a visible Battle.net window as launch-ready, and uses window-relative tab/Play button image searches with full-screen fallback.
- Battle.net startup retries launch requests every 1s for up to 5s using the configured executable first and installation discovery as fallback.
- Battle.net can remain as a background/tray process after Diablo launches; only a visible Battle.net window remaining is treated as a post-launch close warning.
- Battle.net Launch Diagnostics distinguish app-sent Play clicks, suspected manual Play clicks, Diablo launches without app Play clicks, successful Diablo launches after app clicks, and post-launch Battle.net close failures.
- Start Game Flow is implemented with stable-button click confirmation and retry diagnostics, though image-recognition reliability is still being improved.
- Repair and salvage workflows are implemented, with repair still using coordinate-based station clicks, visual menu polling, and detailed station/menu duration logs.
- Bounty menu auto-close has foreground/combat/throttle diagnostics for detected, sent, and skipped injected-Escape decisions.
- `Config/AppSettings.json` centralizes release/debug configuration and auto-creates with safe defaults when missing.
- Debug screenshot capture defaults to on for Debug builds and off for Release builds only when no saved preference exists.
- Runtime input cleanup tracks held mouse/Shift state to avoid unsafe post-exit releases.
- Diagnostic screenshot capture records paired Diablo/App evidence for major workflow success and failure milestones.
- Missing screenshot/template assets can trigger an optional non-combat debug prompt to capture the current Diablo window or known scan region into the expected Images folder.
- Debug package screenshot selection is session-only, so stale screenshots from previous app runs are excluded while normal retention cleanup remains unchanged.

## Systems Under Active Improvement

- Full route validation with generated route-failure summaries.
- Fresh runtime validation of paired success/failure screenshots and `debug-screenshot-manifest.txt`.
- Missing-asset prompt validation outside combat, including accept/skip behavior and saved capture location.
- Validation that debug packages include only current-session screenshots and report package size/session details.
- Ancient Waterway/channel route-state validation.
- Caldeum-to-Waterway and Black Canyon Mines/Battlefields allowed-location proof in logs and debug packages.
- Battle.net Play button detection across fullscreen, windowed, moved-window, and multi-monitor setups after cached-region recalibration.
- Battle.net window-based startup retry validation.
- Battle.net visible-window close/status validation while background tray processes remain.
- Battle.net launch and post-launch close diagnostics validation.
- Battle.net Play button fallback comparison diagnostics for region accuracy.
- Start Game image recognition and possible cursor interference.
- Repair and salvage timing validation.
- Optimized repair station timing validation after the new 50ms New Tristram arrival settle.
- WhimsyDale Teleport Next blocking validation.
- Teleport Next route advancement validation when the player is already at the queued destination.
- Bounty menu auto-close diagnostic validation.
- Release config validation for diagnostic panes, debug screenshots, missing-asset prompts, notification behavior, repair timing, and teleport timeout.
- Release/publish validation to ensure runtime images are included.

## Diagnostics And Debugging

- Diagnostic Overlay: compact read-only live status panel for location, route, combat, retry, failure, log, and screenshot state.
- Route State Inspector: fuller read-only diagnostics tab for route decisions, blocking state, retry state, active workflow, Diablo focus/running status, and latest evidence paths.
- Debug Mode: off by default for normal use. Enable it from the Settings panel to show diagnostic panes and optional debug screenshot controls.
- Configuration: `Config/AppSettings.json` controls executable paths, image/template paths, launch timings, release/debug UI, notifications, image-recognition thresholds, repair timing, teleport timeout settings, and bounty watcher timing.
- Screenshot-On-Failure: captures paired Diablo/App failure screenshots for major workflow failures, including teleport, Start Game, Battle.net, repair, cancellation, and unexpected exception cases.
- Missing-Asset Capture Prompt: logs missing template context and offers an optional modeless capture helper while combat is inactive.
- Success Screenshot Capture: captures paired Diablo/App screenshots for sparse workflow milestones such as Battle.net Play clicked, Diablo launch detected, Start Game verified, teleport confirmed, repair/salvage complete, and Exit Game complete.
- Debug Package Generator: packages logs, current-session success screenshots, current-session failure screenshots, current-session debug screenshots, key docs, git status/log output, `route-failure-summary.txt`, `debug-screenshot-manifest.txt`, package size/session details, and a manifest for troubleshooting.
- Route/workflow summaries include Battle.net launch verdicts and separate post-launch close verdicts, so a successful app-click launch is not confused with a Battle.net close failure.

Run the debug package generator from the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\create-debug-package.ps1
```

## Build Instructions

Requirements:

- Windows
- .NET SDK compatible with `net10.0-windows`
- Diablo III and Battle.net installed for runtime use

Build from the project root:

```powershell
dotnet build GoblinFarmer.csproj
```

The project uses project-relative image assets and copies runtime images into the build output.

## Install

GoblinFarmer releases use a self-contained Windows publish folder plus an optional Inno Setup installer.

For users:

- Run `GoblinFarmerSetup-<version>.exe` when an installer is provided.
- The installer places the app under `%LOCALAPPDATA%\Programs\GoblinFarmer`, creates a Start Menu shortcut, and can create a desktop shortcut.
- No source checkout or .NET SDK is required for the self-contained release.

If using the publish folder directly, run `GoblinFarmer.exe` from the published `GoblinFarmer` folder.

## First-Run Setup

On first launch, GoblinFarmer attempts to discover the Diablo III and Battle.net executables. If either path is missing, the first-run setup dialog opens.

Select:

- `Diablo III64.exe` or `Diablo III.exe`
- `Battle.net.exe`
- The `Images` template folder

Automation stays disabled until required paths and template folders validate successfully.

## Config Files

Runtime configuration lives beside the app:

```text
Config/AppSettings.json
ScanRegions.json
```

`AppSettings.json` is user-editable, but normal path changes can be made from the Settings panel. Image/template paths should stay relative to the installed app folder when possible, with the default `Images` folder copied into every release.

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

Logs and screenshots are written under the runtime app folder and are not required for installation.

## Release Build

Publish a release folder from the project root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\publish-release.ps1 -Version 1.0.0
```

The script runs a self-contained `win-x64` Release publish, verifies the executable, icon, config, and Images folder, then compiles `Installer/GoblinFarmer.iss` if Inno Setup is installed. If `ISCC.exe` is not available, the publish folder is still ready to distribute.

See [Docs/Release_Checklist.md](Docs/Release_Checklist.md) before publishing a final build.

## Project Structure

```text
Docs/
  Project_Status.md       Detailed developer source of truth
  TODO.md                 Current work items and backlog
  TEST_CHECKLIST.md       Manual validation checklist

Images/
  Combat/                 Combat templates and scan regions
  Current Location/       Location detection templates
  Start Game/             Battle.net and Start Game templates
  Teleport Function/      Map templates and waypoint coordinates
  Repair/                 Repair templates and coordinates
  Salvage/                Salvage templates and coordinates

Scripts/
  create-debug-package.ps1
  publish-release.ps1

Installer/
  GoblinFarmer.iss

frmMain.*.cs              Main WinForms feature areas
Form1.cs                  Form and shared app logic
GoblinFarmer.csproj       Project file
```

## Source Of Truth

- [Docs/Project_Status.md](Docs/Project_Status.md): authoritative route logic, stable systems, known issues, recent fixes, and next recommended task.
- [Docs/TODO.md](Docs/TODO.md): actionable development and validation checklist.
- [AGENTS.md](AGENTS.md): coding and documentation rules for contributors and agents.

README.md should stay concise and user-facing. Detailed implementation notes belong in `Docs/Project_Status.md`.

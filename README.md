# GoblinFarmer

GoblinFarmer is a Windows Forms C# assistant for Diablo III farming workflows. It combines image recognition, route-aware teleporting, Battle.net launch support, combat helpers, town automation, and diagnostic tooling for troubleshooting long farming sessions.

This README is intentionally high-level and user-facing. The detailed developer source of truth is [Docs/Project_Status.md](Docs/Project_Status.md).

## Current Status

The project is in an active reliability phase. Core route, combat, diagnostic, and launch systems are implemented, while current validation is focused on combat mouse suppression over no-click UI regions, Battle.net window-relative Play button accuracy, Start Game reliability, repair timing, and full-route testing.

The current recommended validation target is combat no-click suppression, including Demon Hunter right-hold behavior and combat keyboard filtering. See [Docs/Project_Status.md](Docs/Project_Status.md) for the exact current focus and latest known issues.

## Stable Systems

- Teleport Routing tracks the configured farming route and preserves raw, normalized, display, and blocking locations separately.
- Teleport Blocking prevents known bad route transitions, such as blocked Cathedral, City of Caldeum, Ancient Waterway, and Stinging Winds cases.
- Teleport Retry Logic preserves failed or interrupted manual and hotkey teleport state until arrival confirmation succeeds or the user explicitly changes course.
- Combat Automation includes current Monk, Demon Hunter, and Witch Doctor support.
- Combat no-click safety suppresses physical mouse clicks in known UI regions, including the extended lower-right hover menu area, without moving the cursor or stopping combat.
- Demon Hunter right-hold starts only from a safe world area and remains held through hover/no-click regions to better match the old Python app feel.
- Demon Hunter sustained combat reports active right-held no-click suppression instead of appearing stopped when shared left-click input is suppressed over UI.
- Combat keyboard filtering suppresses physical `1`/`2` during combat while allowing injected automation key events through.
- Battle.net Launch Flow can relaunch or focus Battle.net and uses window-relative tab/Play button image searches with full-screen fallback.
- Start Game Flow is implemented and has passed prior validation, though image-recognition reliability is still being improved.
- Repair and salvage workflows are implemented, with repair still using coordinate-based station clicks.
- Runtime input cleanup tracks held mouse/Shift state to avoid unsafe post-exit releases.

## Systems Under Active Improvement

- Combat no-click suppression validation.
- Battle.net Play button detection across fullscreen, windowed, moved-window, and multi-monitor setups.
- Battle.net Play button fallback comparison diagnostics for region accuracy.
- Start Game image recognition and possible cursor interference.
- Repair and salvage timing validation.
- Full route validation from Southern Highlands through Pandemonium Fortress Level 2.
- Release/publish validation to ensure runtime images are included.

## Diagnostics And Debugging

- Diagnostic Overlay: compact read-only live status panel for location, route, combat, retry, failure, log, and screenshot state.
- Route State Inspector: fuller read-only diagnostics tab for route decisions, blocking state, retry state, active workflow, Diablo focus/running status, and latest evidence paths.
- Screenshot-On-Failure: captures failure screenshots for major workflow failures, including teleport, Start Game, Battle.net, repair, cancellation, and unexpected exception cases.
- Debug Package Generator: packages logs, failure screenshots, normal debug screenshots, key docs, git status/log output, and a manifest for troubleshooting.

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

frmMain.*.cs              Main WinForms feature areas
Form1.cs                  Form and shared app logic
GoblinFarmer.csproj       Project file
```

## Source Of Truth

- [Docs/Project_Status.md](Docs/Project_Status.md): authoritative route logic, stable systems, known issues, recent fixes, and next recommended task.
- [Docs/TODO.md](Docs/TODO.md): actionable development and validation checklist.
- [AGENTS.md](AGENTS.md): coding and documentation rules for contributors and agents.

README.md should stay concise and user-facing. Detailed implementation notes belong in `Docs/Project_Status.md`.

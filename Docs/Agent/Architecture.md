# Agent Architecture Manual

Stable architecture notes for Codex. Current behavior, active issues, and route details live in `Docs/Project_Status.md`; that file wins on conflict.

## Application Shape

- GoblinFarmer is a Windows Forms automation app targeting Diablo III workflows.
- `Form1.cs`, `Form1.Designer.cs`, and `frmMain.*.cs` partials carry the UI, route controls, diagnostics, combat, town automation, Goblin Tracker, and hotkey behavior.
- `Program.cs` stays on the normal WinForms startup path. Debug/replay tooling must not be wired into normal app startup unless explicitly requested.
- `AppSettings.cs` and `Config\AppSettings.json` define defaults. Private local overrides belong in ignored `Config\AppSettings.local.json`.

## Route And Teleport Logic

- Route state, teleport button behavior, blocked areas, retry routing, and display/current/blocking area separation are source-of-truth topics in `Docs/Project_Status.md`.
- Keep raw detected location, normalized location, display location, and blocking location separate.
- Preserve interrupted teleport fail-safe behavior and button state after failed or interrupted teleports.
- Do not change route/blocking behavior without explicit user request and targeted validation.

## Combat Profiles

- Combat behavior is split into profile-specific partials and helpers, especially `frmMain.Combat.cs`, `CombatClickSafety.cs`, and related input logic.
- Combat changes are high risk. Do not change combat logic unless explicitly requested.
- Existing no-click region protections and recovery behavior should remain conservative.

## Goblin Tracker

- Goblin Tracker evidence recognition uses Journal and Minimap template evidence, area resolution, duplicate guards, stale-journal protection, and per-area count limits.
- Automatic counting is gated by Observation Mode and Auto Goblin Count settings.
- DecisionBundles, EncounterCaptures, ObservationDiagnostics, logs, and JSONL events are the primary debugging evidence.
- Goblin Replay is explicit/on-demand developer forensic tooling only.

## Town Automation

- Repair/salvage and Kadala/town flows live in `frmMain.Town.cs` and related workflow helpers.
- Do not change repair/salvage behavior unless explicitly requested.
- Salvage validation should focus on safe clicks, confirmation prompts, and the cached single-inventory-scan behavior.

## Debug And Replay Tooling

- Debug package generation is script-driven through `Scripts\Create Debug Package.bat` and `Scripts\create-debug-package.ps1`.
- Replay must stay harness-only and explicit. It must not run from normal startup, scanner execution, route workflows, package creation, or form close.
- Form close must remain a fast quiet shutdown path.

## Config And Assets

- Tracked defaults live in `Config\AppSettings.json`; local private settings belong in `Config\AppSettings.local.json`.
- Image templates live under `Images`. Use project-relative paths and do not hardcode absolute image paths.
- Generated outputs such as `bin`, `obj`, `DebugPackages`, `ProjectBrain`, `Reports`, logs, screenshots, installers, and artifacts are not source.

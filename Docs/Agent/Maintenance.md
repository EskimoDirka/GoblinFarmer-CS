# Agent Maintenance Manual

Stable maintenance guidance for Codex. Current script policy and latest validation live in `Docs/Project_Status.md`; that file wins on conflict.

## Scripts Folder Policy

- Normal package launcher is `Scripts\Create Debug Package.bat`.
- Required backing script is `Scripts\create-debug-package.ps1`.
- `Scripts\debug-analysis-tools.ps1` remains because the debug package script loads it.
- `Scripts\add-auto-count-review-notes.ps1` is the tracked notes-only post-review helper for updating the newest debug package from review notes.
- `Scripts\draft-auto-count-scenario.ps1` is the tracked AutoCount Matrix draft helper for ignored review-only scenario drafts.
- Cleanup scripts are maintenance-only exceptions: `Scripts\Cleanup Project.bat`, `Scripts\Cleanup Project Delete.bat`, and `Scripts\cleanup-project.ps1`.
- Local recorder scripts belong under ignored `Scripts\Local Tools\*` and remain outside tracked app startup/control code.
- Retired/manual scripts belong under `Docs\ScriptArchive` unless the user asks to restore them.

## Cleanup Script Policy

- `Cleanup Project.bat` must remain dry-run.
- `Cleanup Project Delete.bat` must remain confirmation-gated before passing `-Delete`.
- `cleanup-project.ps1` must never delete by default.
- Optional runtime artifacts require `-RuntimeArtifacts`.
- Older installer pruning requires `-PruneOldInstallers` and must keep the latest installer.
- The cleanup report is `Reports\Cleanup_Report.md`.

## Storage Cleanup Priorities

Clean generated artifacts before source:

1. `bin` and `obj`.
2. `Tests/**/bin` and `Tests/**/obj`.
3. Debug package retention, usually newest 10 during active testing.
4. Runtime artifacts only when logs/screenshots/evidence are disposable.
5. Old installers only when a current/latest installer is preserved.

Do not trim source/docs first. `Images`, `Docs`, `Scripts`, `Config`, `Installer`, `.git`, source files, project files, `README.md`, and `AGENTS.md` are protected paths.

## Retention Expectations

- App/debug package workflow defaults may retain 20 debug packages unless explicitly changed.
- Legacy Salvage Inventory Replay artifacts live under `Debug\InventoryReplay\Salvage` and use 7-day age cleanup, but current debug packages exclude legacy automatic replay image folders by default. Current salvage and gem-stash replay evidence is structured log output under `Debug\ReplayLogs` plus curated `ReviewEvidence` frames/crops when supplied.
- Cleanup script default retention can recommend 10 for local storage cleanup without changing app behavior.
- Stable storage cleanup may use 5 debug packages only when the user agrees the run evidence is no longer needed.
- Do not add new artifact producers without a retention and package-size policy.

## Do Not Commit Generated Artifacts

Do not commit:

- Generated executables.
- Installer output.
- Portable ZIPs.
- Debug packages.
- Reports generated for local review unless intentionally requested.
- Logs, screenshots, replay captures, DecisionBundles, EncounterCaptures, GoblinEvidence runtime output.
- Inventory Replay runtime crops and metadata.
- Source-upload folders or `GitHub Upload` copies.

Tracked `Config\AppSettings.json` should remain sanitized. Private paths and toggles belong in ignored `Config\AppSettings.local.json`.

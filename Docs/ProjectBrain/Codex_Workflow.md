# Codex Workflow

Source of truth: `AGENTS.md`.

## Before Code Changes

- Read `Docs\Project_Status.md`.
- Treat `Docs\Project_Status.md` as the source of truth for route logic, blocking rules, active issues, confirmed behavior, and next recommended task.
- Verify `.gitignore` excludes build artifacts, user-specific files, and other non-source output.
- Do not commit, push, branch, merge, or otherwise modify git state unless explicitly requested.

## Working Style

- Keep tasks small and focused.
- Prefer existing repo patterns and script conventions.
- Do not change combat logic, Battle.net launch flow, or repair/salvage logic unless explicitly requested.
- Preserve interrupted teleport fail-safe behavior and button state after failed/interrupted teleports.
- Keep raw detected location, normalized location, display location, and blocking location separate.
- Use project-relative image paths, not hardcoded absolute image paths.

## Evidence Priority

Use this priority order when analyzing issues:

1. Latest user-provided debug package and live-test notes.
2. `Docs\Project_Status.md`.
3. Current local source code.
4. GitHub repository for reference only.

Do not assume GitHub reflects the user's latest local state.

## After Changes

- Update `Docs\Project_Status.md`.
- Record what changed, what was tested, what still needs testing, and the next recommended task.
- Keep `README.md` synchronized when Project Status changes affect stable, current, or user-facing behavior.
- Include validation and next test steps in responses.
- Do not commit or push automatically.

## Project Brain Docs

- Use `Docs\ProjectBrain\*.md` directly as compact Codex context.
- Project Brain ZIP generation has been retired; there is no active Project Brain batch or PowerShell generator.
- Project Brain docs summarize and route context; they do not replace `Docs\Project_Status.md`.

## Scripts Policy

- Normal tracked user-facing package batch file under `Scripts` is limited to `Create Debug Package.bat`.
- Required backing script is `create-debug-package.ps1`.
- `debug-analysis-tools.ps1` remains because `create-debug-package.ps1` directly loads it.
- `Cleanup Project.bat`, `Cleanup Project Delete.bat`, and `cleanup-project.ps1` are documented maintenance-only exceptions for generated artifact cleanup. The dry-run launcher and PowerShell script must default to dry-run. The delete launcher must ask for `Y/N` before passing `-Delete`.
- Retired/manual helpers are archived under `Docs\ScriptArchive\2026-06-09` and should not be reintroduced as active scripts without explicit approval.

## Cleanup Workflow

- Run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Scripts\cleanup-project.ps1` first and review `Reports\Cleanup_Report.md`.
- Delete mode is explicit only: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Scripts\cleanup-project.ps1 -Delete`.
- `Scripts\Cleanup Project Delete.bat` is allowed as a convenience launcher for default delete mode, but it must remain confirmation-gated and must not include `-RuntimeArtifacts` or `-PruneOldInstallers`.
- Use `-RuntimeArtifacts` only when runtime logs, screenshots, GoblinEvidence, DecisionBundles, or replay/capture folders are intentionally disposable.
- Use `-PruneOldInstallers` only when older installer files should be pruned; the newest installer remains protected.
- Keep source/docs protected. Do not use cleanup to remove `Images`, `Docs`, `Scripts`, `Config`, `Installer`, `.git`, source files, project files, `README.md`, `AGENTS.md`, or Project Brain docs.

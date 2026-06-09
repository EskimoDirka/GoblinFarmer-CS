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

## Project Brain Workflow

- Use `Scripts\Create Project Brain.bat` or `Scripts\create-project-brain.ps1` for AI-assisted context packages.
- Project Brain ZIPs are generated output under `ProjectBrain` and must not be committed.
- Project Brain docs summarize and route context; they do not replace `Docs\Project_Status.md`.
- The same PowerShell script can generate `Reports\Storage_Breakdown.md` with `-StorageBreakdownOnly`; this is report-only and must not delete files.

## Scripts Policy

- Tracked user-facing batch files under `Scripts` are limited to `Create Debug Package.bat` and `Create Project Brain.bat`.
- Required backing scripts are `create-debug-package.ps1` and `create-project-brain.ps1`.
- `debug-analysis-tools.ps1` remains because `create-debug-package.ps1` directly loads it.
- Retired/manual helpers are archived under `Docs\ScriptArchive\2026-06-09` and should not be reintroduced as active scripts without explicit approval.

# Agent Workflows Manual

Stable workflow guidance for Codex. Current priorities and latest findings live in `Docs/Project_Status.md`; that file wins on conflict.

## Normal Development Task

1. Read `Docs/Project_Status.md`.
2. Read the relevant `Docs/Agent/*.md` manual for the area being touched.
3. Inspect current local source before assuming GitHub reflects the latest state.
4. Make the smallest scoped change that matches existing patterns.
5. Validate with the commands appropriate to the changed area.
6. Update `Docs/Project_Status.md` with what changed, what was tested, what still needs testing, and the next recommended task.
7. Do not commit or push unless explicitly requested.

## Debug Package Review

- Treat user live-test notes and the named/latest debug package as primary evidence.
- Use `Scripts\Create Debug Package.bat` as the supported package export workflow.
- Review package analysis files, logs, route summaries, Goblin Tracker summaries, GoblinEvidence, DecisionBundles, EncounterCaptures, ObservationDiagnostics, and JSONL events before changing code.
- Do not create a second debug package workflow.

## Project Brain Docs

- `Docs\ProjectBrain\*.md` remains available as compact Codex context.
- Project Brain ZIP generation has been retired; read the markdown docs directly when useful.
- `Docs\Project_Status.md` remains the source of truth when Project Brain summaries are stale.

## Cleanup Workflow

- Use `Scripts\Cleanup Project.bat` for dry-run cleanup review.
- Use `Scripts\Cleanup Project Delete.bat` only when generated artifact deletion is intended; it must remain confirmation-gated.
- Use direct PowerShell flags only when appropriate:
  - `-RuntimeArtifacts` for disposable logs, screenshots, evidence, and replay/capture folders.
  - `-PruneOldInstallers` for older installer files while keeping the latest installer.
  - `-DebugPackageRetention 10` for active testing or `5` only when stable.
- Review `Reports\Cleanup_Report.md` before destructive cleanup.

## Codex Prompt/Test Cycle

- For implementation prompts, carry the work through edit, validation, and documentation updates.
- For review/opinion/clarification prompts, do not change code unless the user asks to implement.
- If validation cannot be performed, state exactly what was skipped and why.
- Always provide the next validation step in the final response.

## Documentation Expectations

- `Docs/Project_Status.md` remains the detailed current source of truth.
- `Docs/Agent/*.md` should hold stable operating manuals and avoid copying large status sections.
- `README.md` should stay high-level and user-facing.
- `Docs/TODO.md` should hold open test priorities when relevant.

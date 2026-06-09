# GoblinFarmer Agent Instructions

## Source Of Truth

- Read `Docs/Project_Status.md` before code or workflow changes.
- Treat `Docs/Project_Status.md` as the current source of truth for route logic, blocking rules, active issues, confirmed behavior, and next recommended tasks.
- `Docs/Agent/*.md` contains stable operating manuals for architecture, workflows, testing, maintenance, debugging, and release work.
- If any doc conflicts with `Docs/Project_Status.md`, `Docs/Project_Status.md` wins.

## Before Touching Related Areas

- Architecture/app structure: read `Docs/Agent/Architecture.md`.
- Development workflow or prompt handoff: read `Docs/Agent/Workflows.md`.
- Build, tests, or live validation: read `Docs/Agent/Testing.md`.
- Scripts, cleanup, storage, or generated artifacts: read `Docs/Agent/Maintenance.md`.
- Debug packages, GoblinEvidence, DecisionBundles, replay, or stale evidence analysis: read `Docs/Agent/Debugging.md`.
- Publish, installer, version, tag, or release prep: read `Docs/Agent/Release.md`.

## Operating Rules

- Do not commit, push, create branches, merge, tag, or create GitHub releases unless the user explicitly asks.
- Do not change combat logic, Battle.net launch flow, repair/salvage logic, route logic, or Goblin Tracker count policy unless the user explicitly asks for that area.
- Preserve interrupted teleport fail-safe behavior and button state after failed or interrupted teleports.
- Keep raw detected location, normalized location, display location, and blocking location separate.
- Use project-relative image paths. Do not add hardcoded absolute image paths.
- Do not add new screenshot, package, capture, replay, cleanup, or export workflows without an explicit retention and package-size policy.

## After Changes

- Update `Docs/Project_Status.md` after code, script, workflow, or documentation changes.
- Record what changed, what was tested, what still needs testing, and the next recommended task.
- Keep `README.md` synchronized when `Docs/Project_Status.md` changes affect stable or user-facing behavior.
- Include the next validation steps in the response.
- Do not install Agent Zero or add a new agent framework for this project.

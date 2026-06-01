# GoblinFarmer Agent Instructions

## Required Before Any Code Changes

1. Read `Docs/Project_Status.md`.
2. Treat `Docs/Project_Status.md` as the source of truth for:
   - route logic
   - blocking rules
   - active issues
   - confirmed working behavior
   - next recommended task

## Required After Any Code Changes

1. Update `Docs/Project_Status.md`.
2. Record:
   - what changed
   - what was tested
   - what still needs testing
   - next recommended task

## Coding Rules

- Do not change combat logic unless explicitly requested.
- Do not change Battle.net launch flow unless explicitly requested.
- Do not change repair/salvage logic unless explicitly requested.
- Preserve interrupted teleport fail-safe behavior.
- Preserve button state after failed/interrupted teleports.
- Keep raw detected location, normalized location, display location, and blocking location separate.
- Do not use hardcoded absolute image paths.
- Use project-relative image paths.

## Build Requirement

Before finishing, run `dotnet build GoblinFarmer.csproj`.

The build must succeed.

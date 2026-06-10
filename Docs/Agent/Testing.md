# Agent Testing Manual

Stable validation guidance for Codex. Current required checks and priorities live in `Docs/Project_Status.md`; that file wins on conflict.

## Build And Test Commands

Before finishing code changes, run:

```powershell
dotnet build GoblinFarmer.csproj
```

For Goblin Tracker, Goblin Evidence, Goblin Replay, debug package, or script-policy changes, also run when practical:

```powershell
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
git diff --check
```

Documentation-only changes may not need app runtime testing, but still validate any changed scripts or generated packages.

## Live Test Session Pattern

- Treat user live-test runs as VS Debug runs unless the user explicitly says otherwise.
- Use user notes plus the latest/named debug package as evidence before changing behavior.
- Do not infer a runtime fix from a package that lacks relevant evidence.
- Report exact validation gaps when a live run does not cover a requested scenario.

## Sim Count Validation

- Use VS Debug `Sim Count` for deterministic count-policy validation, especially blocked areas, duplicate suppression, PF1/PF2/Stinging Winds area limits, and rare route areas.
- Sim Count validates count policy and UI wiring, not live scanner timing, real image recognition, or sound playback.

## Goblin Tracker Validation

- Route-specific Goblin Tracker next tests should follow the route order from `Docs/Project_Status.md`.
- Validate stale Journal suppression, Minimap confirmations, sustained Engaged handling, duplicate guards, area limits, Last Observation persistence, Reset Stats, and Make New Game behavior.
- Use DecisionBundles and EncounterCaptures for evidence. Use the VS Debug `Capture` button only when image-recognition evidence is visibly needed.

## Repair/Salvage Validation

- Confirm repair/salvage still avoids unsafe clicks and handles confirmation prompts.
- For salvage changes, confirm the cached single-inventory-scan behavior and per-slot click flow.
- Do not alter repair/salvage logic unless explicitly requested.

## What To Report

- Commands run and pass/fail status.
- Debug package paths created when relevant.
- Whether real runtime deletion or package retention cleanup occurred.
- What still needs live validation.
- The next recommended test step.

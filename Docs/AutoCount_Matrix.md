# AutoCount Matrix

The AutoCount Matrix is a small explicit replay suite for recurring Goblin Tracker policy shapes. It is tooling-only: it does not run from app startup, VS Debug startup, scanner execution, route flow, package creation, or form close.

## Command

```powershell
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --goblin-auto-count-matrix
```

The command reads committed scenarios from `Tests\Fixtures\GoblinReplayScenarios\AutoCountMatrix`, synthesizes Journal/Minimap crops from `Images\Goblin Evidence`, and checks `# MatrixStep=` expectations for decision, reason, source, goblin type, area, counted state, and freshness bucket.

## Coverage

| Shape | Unit policy tests | Template replay matrix | DecisionBundle replay | Live-only validation |
|---|---:|---:|---:|---:|
| Southern Highlands -> Cave Level 1 stale Journal | Yes | Yes | When captured | No |
| Ruined Cistern -> Ancient Waterway blocked current context | Yes | Yes | When captured | Overlay/UI state remains live-only |
| Cave Level 1 -> Cave Level 2 independent counts | Yes | Yes | When captured | No |
| Cathedral stale Journal row | Yes | Yes | When captured | No |
| Northern Highlands fresh Journal after old Cave empty bucket | Yes | Yes | When captured | No |
| PF1/PF2 first, second, and third goblin behavior | Yes | PF2 matrix seed | When captured | Real two-goblin spawn timing |
| Stinging Winds two-count behavior | Yes | Yes | When captured | Real two-goblin spawn timing |
| New Game/reset carryover | Yes | Yes | When captured | No |
| Minimap-to-Journal source variants | Yes | Yes | When captured | No |
| Same-type cross-area Minimap collision | Yes | No | When captured | Real frame/template collision shape |
| Notification latency and splash display | Partial | No | Logs only | Yes |
| Scanner timing, focus/input, missing frames, OBS/overlay UI | No | No | Logs/evidence only | Yes |

## Review Workflow

1. Review the OBS clip and debug package.
2. Open `goblin-auto-count-triage.md` first for likely review targets.
3. If the issue is replayable, draft a scenario with `Scripts\draft-auto-count-scenario.ps1`.
4. Promote the draft into the committed matrix only after the assertion is understood.
5. Run the AutoCount Matrix before changing count policy.

## Package Size Policy

Debug packages include only `goblin-auto-count-triage.md`, `goblin-auto-count-triage.json`, and this coverage doc for the accelerator. Full videos, bulk image folders, and automatic replay image folders remain excluded by default.

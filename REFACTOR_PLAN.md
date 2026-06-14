# GoblinFarmer Refactor Plan

Date: 2026-06-14

Scope: Prompt 4 from the health refactoring optimization sequence. This is a plan only. It does not change runtime behavior, thresholds, routing, combat, repair, salvage, gem-stash, Kadala, make-new-game, replay execution, OBS behavior, or package output.

## Refactor Principles

- Preserve current behavior first. Each extraction should be mechanical, covered by existing tests, and easy to revert.
- Keep public command lines, settings names, debug package paths, replay inputs, and log event names stable unless a separate compatibility plan is approved.
- Prefer small service/helper extractions over broad rewrites.
- Add or extend tests before changing stale/duplicate suppression, area resolution, salvage/gem classifiers, replay schemas, or package retention.
- Do not refactor salvage into best-sample promotion; that exclusion is intentional.

## Phase 1: Documentation And Script Boundary Cleanup

Goal: keep current manuals aligned before code movement.

- Keep `Docs\Agent\Maintenance.md`, `Docs\Agent\Debugging.md`, `Docs\ProjectBrain\Debug_And_Testing.md`, `Docs\Project_Status.md`, and `Docs\Test_Results.md` synchronized with current package/replay/OBS behavior.
- Keep script ownership explicit:
  - package creation: `Scripts\create-debug-package.ps1`
  - shared package analysis: `Scripts\debug-analysis-tools.ps1`
  - post-review notes updater: `Scripts\add-auto-count-review-notes.ps1`
  - review-only scenario draft helper: `Scripts\draft-auto-count-scenario.ps1`
  - local-only recorder tooling: ignored `Scripts\Local Tools\*`

Validation:

```powershell
git diff --check
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --goblin-auto-count-matrix
```

## Phase 2: Goblin Evidence Policy Extraction

Goal: reduce risk in `frmMain.GoblinEvidence.cs`, `GoblinEvidence.cs`, and `frmMain.SessionStats.AutoCount.cs` without loosening count acceptance.

Status:

- Started 2026-06-14.
- Extracted `GoblinManualCountBlockList`, `GoblinAreaDuplicateGuardResult`, and `GoblinAreaDuplicateGuard` into `GoblinCountAreaPolicies.cs`.
- Extracted `GoblinJournalFreshnessPolicy` into `GoblinJournalFreshnessPolicy.cs`.
- Extracted `GoblinAutoCountEncounterSuppressionPolicy` into `GoblinAutoCountEncounterSuppressionPolicy.cs`.
- No call sites, signatures, thresholds, freshness windows, duplicate guard limits, blocked-area list contents, or count acceptance behavior were changed.

Proposed extractions:

1. `GoblinEvidenceFreshnessTracker`
   - Owns stale Journal visible-line memory, reset carryover suppression, history input suppression, and first-seen/last-seen updates.
   - Keeps current log reasons stable: `JournalCandidateIgnoredStaleVisibleLine`, `JournalCandidateIgnoredResetCarryover`, `JournalKilledIgnoredStale`, and related fresh-area bypass reasons.

2. `GoblinAutoCountSuppressionService`
   - Wraps encounter suppression, source-variant duplicate checks, area duplicate guard interactions, and PF/Stinging Winds area-limit checks.
   - Keeps `GoblinAutoCountEncounterSuppressionPolicy`, `GoblinPandemoniumMultiCountDuplicatePolicy`, and `GoblinAreaDuplicateGuard` behavior unchanged initially.

3. `GoblinAutoCountRecorder`
   - Owns accepted/suppressed event logging, GoblinTrackerEvents JSONL payload shape, notification queue handoff, and Last Observation publication.
   - Leaves UI update order stable, especially overlay update before best-sample capture.

Tests before extraction:

- Same-area duplicate Journal refresh.
- Cross-area stale Journal rows.
- PF second-slot behavior.
- Stinging Winds two-count behavior.
- Blocked area suppression.
- Manual/debug simulation exclusion.
- Notification total-positive guard.

Validation:

```powershell
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --goblin-auto-count-matrix
```

## Phase 3: Replay Harness Split

Goal: make replay additions faster without changing explicit/on-demand replay boundaries.

Proposed extractions from `GoblinReplayFixtureRunner.cs` and test harness code:

1. Scenario parser.
2. Template/current-location frame synthesizer.
3. DecisionBundle and capture-folder input resolver.
4. Runner/result model.
5. Console/Markdown report formatter.

Compatibility rules:

- Keep existing harness commands working:
  - `--goblin-replay-captures`
  - `--goblin-replay-metadata`
  - `--goblin-replay-prefix`
  - `--goblin-replay-decision-bundle`
  - `--goblin-replay-scenario`
  - `--goblin-auto-count-matrix`
  - `--debug-package-replay`
  - `--inventory-replay`
- Do not write persistent debug files during replay.

Validation:

```powershell
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --goblin-auto-count-matrix
```

## Phase 4: Debug Package Script Modularization

Goal: reduce the size and risk of `Scripts\create-debug-package.ps1` and `Scripts\debug-analysis-tools.ps1`.

Suggested module boundaries:

- package staging and copy filters
- retention and size summaries
- review-video selection and ffmpeg extraction
- auto-count triage report generation
- package index/HTML/manifest writing
- script smoke-test helpers

Compatibility rules:

- Keep `Scripts\Create Debug Package.bat` and `Scripts\create-debug-package.ps1` parameters stable.
- Keep installed/output-folder behavior working without `.git`.
- Keep full videos excluded by default.
- Keep package size reporting.
- Keep replay explicit and never invoked by package creation.

Validation:

```powershell
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
git diff --check
```

Also run a manual package smoke test with and without review notes after script movement.

## Phase 5: Town Workflow Separation

Goal: reduce coupling in `frmMain.Town.cs` and `frmMain.ImageRecognition.cs` while keeping salvage and gem-stash safety intact.

Proposed extractions:

1. `RepairWorkflow`
   - repair button sampling
   - completion verification
   - status updates

2. `SalvageWorkflow`
   - bulk blue/yellow category pass
   - shared inventory scan
   - confirmation-aware individual salvage
   - bounded recovery

3. `GemAutoStashWorkflow`
   - gem asset/coordinate preflight
   - stash open/tab focus
   - gem scan/right-click/recovery
   - best-sample capture for successful stash actions

Rules:

- Do not change click order, timings, safety gates, or gem/salvage classifier thresholds during extraction.
- Keep shared inventory grid capture centralized.
- Keep salvage excluded from best-sample promotion.

Validation:

```powershell
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
```

Live validation remains required for repair/salvage/gem-stash click behavior.

## Phase 6: Settings Model Split

Goal: make `AppSettings.cs` easier to change without damaging VS Debug/release default separation.

Proposed extractions:

- settings model declarations
- schema migration/default seeding
- VS Debug profile overrides
- release-user persistence
- validation/clamping
- diagnostic summary formatting

Rules:

- Keep JSON property names stable.
- Preserve tracked `Config\AppSettings.json` safe defaults.
- Preserve VS Debug defaults that enable validation-only features and release defaults that keep risky behavior off.

Validation:

```powershell
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
```

## Recommended Order

1. Complete small documentation and script-boundary cleanup.
2. Add or expand tests around the most brittle auto-count stale/duplicate cases.
3. Extract Goblin Evidence freshness and suppression services.
4. Split replay harness after the auto-count matrix remains green.
5. Modularize debug package scripts.
6. Extract town workflow services only after salvage and gem-stash live validation are stable.
7. Split settings last, because it cuts across startup, VS Debug defaults, and release-user persistence.

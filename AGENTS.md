# GoblinFarmer Agent Instructions

## Required Before Any Code Changes

1. Read `Docs/Project_Status.md`.
2. Treat `Docs/Project_Status.md` as the source of truth for:

   * route logic
   * blocking rules
   * active issues
   * confirmed working behavior
   * next recommended task
3. Verify `.gitignore` is up to date and properly excludes build artifacts, user-specific files, and other non-source files.
4. Do not commit, push, create branches, merge, or otherwise modify git state unless the user explicitly requests it.

## Source Of Truth Priority

When analyzing issues or planning changes, use the following priority order:

1. Latest user-provided debug package and live-test notes.
2. `Docs/Project_Status.md`.
3. Current local source code.
4. GitHub repository (reference only).

Do not assume GitHub reflects the user's latest local state.

## Required After Any Code Changes

1. Update `Docs/Project_Status.md`.
2. Record:

   * what changed
   * what was tested
   * what still needs testing
   * next recommended task
3. Keep `README.md` synchronized with high-level stable/current behavior when `Project_Status.md` changes affect user-facing behavior.

## Required Response And GitHub Follow-Up

* After every user prompt unless otherwise specified, provide the next steps to test.
* If no code/runtime behavior changed, state that no app test is required and provide only the next relevant validation step.
* Do not commit or push automatically. The user handles commits and pushes unless they explicitly request GitHub updates.
* If the user submits a release prompt, update GitHub accordingly with a version tag and release notes after the release build artifacts are created and validated.

## Workflow Notes

* When a recent repeatable project workflow is added, changed, or used as the expected way to do work, add or update the concise workflow guidance in `AGENTS.md`.
* If the user asks for clarification, an opinion, recommendations, or review only, do not change code unless they explicitly ask to implement.
* Treat every user live-test run as a VS Debug run unless the user explicitly says otherwise. Goblin Tracker/counter workflows should automatically write the decision bundles, encounter captures, logs, and other runtime evidence needed for debugging. The VS Debug `Capture` button is only a supplemental image-recognition aid that creates fullscreen, minimap, journal, and metadata files when the user clicks it.
* For latest-debug-package review prompts, treat the named/latest debug package and live-test notes as evidence before changing code. Review the newest ZIP in `DebugPackages` when provided or requested. The batch launcher `Scripts\Create Debug Package.bat` delegates to `Scripts\create-debug-package.ps1` and is the intentional ZIP export workflow for both VS Debug and Release. Inspect `debug-package-analysis.txt`, `goblin-tracker-timeline.md`, `goblin-evidence-health.txt`, `debug-package-manifest.txt`, `goblin-tracker-summary.txt`, `goblin-tracker-review.html`, latest logs, route summaries, screenshots, GoblinEvidence, encounter captures, decision bundles, and observation diagnostics. The optional helper scripts under `Scripts\` may analyze an existing package directly, but they must not replace the batch-created ZIP workflow. Form close is a quiet shutdown path and must not be used as a package/export trigger. Do not run a separate evidence-reprocessing workflow unless the user explicitly introduces a new one.
* For AI-assisted development handoff context, use `Scripts\Create-ProjectBrain.bat`, which delegates to `Scripts\create-project-brain.ps1` and writes a docs-only timestamped ZIP under `ProjectBrain`. This package is intentionally limited to `AGENTS.md`, `README.md`, and selected `Docs\*.md` status/reference files when present; it must not include runtime artifacts, screenshots, images, replay evidence, binaries, or existing ZIPs.
* Keep Goblin Tracker test priorities in `Docs/TODO.md`; the retired VS Debug `Next Tests` tab and metadata workflow must not be reintroduced unless the user explicitly asks for it.
* When providing Goblin Tracker next test steps, list route-specific scenarios in the official route-logic order from `Docs/Project_Status.md` first, then list general reset, stale evidence, display, classification, package, and documentation checks.
* Do not commit generated executables, installer output, portable ZIPs, debug packages, Project Brain ZIPs, logs, screenshots, source-upload folders, or `GitHub Upload` copies. Keep tracked `Config\AppSettings.json` sanitized for release/defaults; private VS Debug paths and toggles belong in ignored `Config\AppSettings.local.json`.
* Do not add new screenshot, package, capture, replay, or export artifacts without an explicit retention and package-size policy.

## Goblin Replay Rules

* Goblin Replay must remain explicit/on-demand.
* For real-session Goblin Replay investigations, prefer the explicit test-harness inputs that target the smallest relevant evidence: `--goblin-replay-metadata` for one `*_Metadata.txt`, `--goblin-replay-prefix` for one capture prefix in a shared capture folder, `--goblin-replay-captures` for whole capture folders, and `--goblin-replay-decision-bundle` when starting from a DecisionBundle folder.
* For small policy simulations that can be modeled from existing `Images\Goblin Evidence` templates, use `--goblin-replay-scenario` with a text scenario file containing explicit `Scan`, `Wait`, `ResetStats`, or `NewGame` steps. Use `Location=...` with `Images\Current Location` title templates when the scenario should exercise current-location image resolution instead of a manual `Area=...` override. Scenario replay must stay harness-only and must create only temporary crop frames that are deleted after the run.
* Do not wire Goblin Replay into:

  * normal startup
  * VS Debug startup
  * scanner execution
  * Create Debug Package
  * form closing
  * route workflows
  * automated testing flows
* Replay may only execute when explicitly requested by the user or by explicit replay commands.
* Goblin Replay is a developer forensic tool and must remain isolated from live gameplay workflows.

## Debug Package Rules

* The batch-created debug package ZIP is the only supported debug package export workflow.
* Do not introduce duplicate debug package generators, automatic package creation, or alternate export paths without explicit user approval.
* Form close must remain a fast shutdown path and must not trigger debug package creation.

## Form Close Rules

* Form close must remain a fast shutdown path.
* Closing the application or VS Debug form must not:

  * create debug packages
  * run Goblin Replay
  * perform long analysis
  * export screenshots
  * generate reports
* Only perform minimal cleanup required for safe shutdown.

## Coding Rules

* Do not change combat logic unless explicitly requested.
* Do not change Battle.net launch flow unless explicitly requested.
* Do not change repair/salvage logic unless explicitly requested.
* Preserve interrupted teleport fail-safe behavior.
* Preserve button state after failed/interrupted teleports.
* Keep raw detected location, normalized location, display location, and blocking location separate.
* Do not use hardcoded absolute image paths.
* Use project-relative image paths.

## Build Requirement

Before finishing code changes, run:

```text
dotnet build GoblinFarmer.csproj
```

The build must succeed.

If build validation cannot be performed, clearly state that validation was not performed.

For Goblin Tracker, Goblin Evidence, Goblin Replay, or debug package changes, also run when practical:

```text
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
git diff --check
```

## Documentation Rules

After updating `Docs/Project_Status.md`:

* Keep `README.md` synchronized with:

  * Stable Systems
  * Current Focus
  * Current Improvements

`README.md` should remain high-level and user-facing.

`Project_Status.md` remains the detailed developer source of truth.

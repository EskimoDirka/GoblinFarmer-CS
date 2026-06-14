# Debug And Testing

Source of truth: `Docs\Project_Status.md`, `README.md`, `Docs\TODO.md`, and `AGENTS.md`.

## Debug Package Workflow

- Supported debug ZIP export path: `Scripts\Create Debug Package.bat`, which delegates to `Scripts\create-debug-package.ps1`.
- Use it for both VS Debug and Release review.
- The batch-created ZIP is the only supported debug package export workflow.
- Form close must stay quiet and must not trigger debug package creation.
- Debug packages write under `DebugPackages`.
- Debug package retention keeps the newest 20 matching `GoblinFarmer_Debug_*.zip` files by default. Startup cleanup and the batch package script both enforce the count.
- The package script auto-selects the newest NVIDIA/attached recording under `Video Clip Review` when possible, writes selected review-video frames under `ReviewEvidence`, and prints the selected video path, duration, size, modified time, and review notes URL in the final PowerShell summary.

## Useful Debug Package Contents

Useful review evidence includes:

- `debug-package-analysis.txt`
- `goblin-tracker-timeline.md`
- `goblin-evidence-health.txt`
- `debug-package-manifest.txt`
- `goblin-tracker-summary.txt`
- `goblin-tracker-review.html`
- latest logs
- route summaries
- session metadata
- Goblin Tracker JSONL events
- decision bundles
- encounter captures
- observation diagnostics
- bounded GoblinEvidence samples

Normal scanner events skip redundant root `GoblinEvidence_*` full/event images by default. DecisionBundles and EncounterCaptures should carry replay-ready Journal/Minimap crops, metadata, JSONL, and trace data.

## Retention Summary

- General debug artifact age retention is 7 days by default.
- Startup cleans `Logs`, `Screenshots`, `debug-screenshots`, `Sessions`, `DebugPackages`, and `Debug\GoblinEvidence` under the runtime root and project/config root when available.
- Session summaries keep the newest 50 `Session_*.md` files.
- GoblinEvidence loose files keep the newest 250 files recursively; ObservationDiagnostics keeps 24 local crop samples during scanner cleanup.
- DecisionBundles, EncounterCaptures, ManualCaptures, and replay-ready crops live under `Debug\GoblinEvidence`, so startup age/count cleanup covers them.
- Debug package ZIP contents are additionally bounded by package-script include/exclude limits; old full DecisionBundle evidence images and Encounter/ManualCapture fullscreen images are excluded by default.

## Replay Tooling Status

- Goblin Replay is explicit/on-demand only.
- It is not wired into startup, VS Debug startup, scanner execution, Create Debug Package, form closing, route workflows, or automated live testing flows.
- Supported harness inputs:
  - `--goblin-replay-captures`
  - `--goblin-replay-metadata`
  - `--goblin-replay-prefix`
  - `--goblin-replay-decision-bundle`
  - `--goblin-replay-scenario`
- Template scenarios can synthesize temporary crop frames from `Images\Goblin Evidence` and can use `Location=...` with `Images\Current Location` title templates.
- Scenario replay creates only temporary crop frames that are deleted after the run.

## Test Commands

Required build command before finishing code changes:

```powershell
dotnet build GoblinFarmer.csproj
```

For Goblin Tracker, Goblin Evidence, Goblin Replay, or debug package changes, run when practical:

```powershell
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
git diff --check
```

Project Brain ZIP generation has been retired. Use `Docs\ProjectBrain\*.md` directly when compact project context is useful.

## Project Cleanup Script

`Scripts\Cleanup Project.bat`, `Scripts\Cleanup Project Delete.bat`, and `Scripts\cleanup-project.ps1` are maintenance-only cleanup tools. They are tracked as an exception to the normal two package launchers.

Default dry-run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Scripts\cleanup-project.ps1
```

Delete mode requires an explicit flag:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Scripts\cleanup-project.ps1 -Delete
```

`Scripts\Cleanup Project Delete.bat` is a confirmation-gated launcher for the default delete mode. It asks for `Y/N` before calling `cleanup-project.ps1 -Delete`, and it does not pass `-RuntimeArtifacts` or `-PruneOldInstallers`.

Useful options:

- `-DebugPackageRetention 10` keeps the newest 10 debug packages for active testing; use `5` only when stable.
- `-RuntimeArtifacts` includes optional runtime folders such as `Debug`, logs, screenshots, `GoblinEvidence`, `DecisionBundles`, and replay/capture folders.
- `-PruneOldInstallers` deletes older installer files while keeping the newest installer.

The cleanup script writes `Reports\Cleanup_Report.md`, prints every path considered, estimates reclaimable bytes, skips missing paths, and refuses targets outside the project root. It must never delete by default.

Protected paths include `Images`, `Docs`, `Scripts`, `Config`, `Installer`, `.git`, source files, project/solution files, `README.md`, `AGENTS.md`, and Project Brain docs.

Recommended cleanup order: run dry-run first, prune `bin`/`obj` and test build outputs, reduce debug package retention if needed, then separately decide whether runtime evidence or older installers should be deleted.

## Live Test Workflow

- Treat every user live-test run as a VS Debug run unless the user explicitly says otherwise.
- Review the latest named/provided debug package and live-test notes before changing code.
- VS Debug has a passive OBS Status group that observes the local OBS process and monitor log only; it must not start, stop, launch, or configure recording. Live run videos are recorded separately by the local OBS monitor and reviewed alongside the package and shared Google Sheet notes.
- For Goblin Tracker next tests, list route-specific scenarios in official route order first, then general reset, stale evidence, display, classification, package, and documentation checks.
- Use VS Debug `Capture` only when image-recognition evidence is visibly needed.
- Use automatic decision bundles and encounter captures for normal Goblin Tracker debugging.

## What To Provide For AI Review

For a full runtime issue, provide the latest debug package ZIP plus live notes and any matching recording. For project context, use the markdown docs under `Docs\ProjectBrain` directly.

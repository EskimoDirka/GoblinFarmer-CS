# Script Archive - 2026-06-09

These scripts were moved out of the active `Scripts` folder during the repository utility cleanup. They are kept for reference only and are not active user-facing workflows.

## Audit Result

| Original script | Classification | Notes |
|---|---|---|
| `Scripts\Create Debug Package.bat` | Keep: required for Debug Package workflow | Remains active. Delegates to `create-debug-package.ps1`. |
| `Scripts\create-debug-package.ps1` | Keep: required for Debug Package workflow | Remains active. Creates the supported debug ZIP and handles retention. |
| `Scripts\debug-analysis-tools.ps1` | Keep: dependency/helper used by Debug Package workflow | Remains active because `create-debug-package.ps1` dot-sources it. |
| `Scripts\Create-ProjectBrain.bat` | Keep: required for Project Brain workflow | Renamed to `Scripts\Create Project Brain.bat` for consistent user-facing naming. |
| `Scripts\Create Project Brain.bat` | Keep: required for Project Brain workflow | Remains active. Delegates to `create-project-brain.ps1`. |
| `Scripts\create-project-brain.ps1` | Keep: required for Project Brain workflow | Remains active. Also supports `-StorageBreakdownOnly` for `Reports\Storage_Breakdown.md`. |
| `Scripts\analyze-latest-debug-package.ps1` | Candidate for removal | Archived. Optional manual analyzer; no longer part of the active Scripts surface. |
| `Scripts\build-goblin-tracker-timeline.ps1` | Candidate for removal | Archived. Optional manual analyzer; debug package creation now writes the timeline through the supported workflow. |
| `Scripts\check-goblin-evidence-health.ps1` | Candidate for removal | Archived. Optional manual analyzer; debug package creation now writes the health report through the supported workflow. |
| `Scripts\dev-verify.ps1` | Candidate for removal | Archived. Local verification wrapper, not required by either package workflow. |
| `Scripts\Git Commit Push.bat` | Candidate for removal | Archived. User indicated this is likely unused. It is not part of the current tracked workflow policy. |
| `Scripts\Git Commit Push.ps1` | Not present | No current file found in the repository. |
| `Scripts\GitHub Sync.bat` | Not present | No current file found in the repository. Historical docs say old GitHub Sync helpers were already retired. |
| `Scripts\GitHub Sync.ps1` | Not present | No current file found in the repository. Historical docs say old GitHub Sync helpers were already retired. |
| `Scripts\publish-release.ps1` | Candidate for removal/manual review | Archived. Release docs now prefer Visual Studio `GoblinFarmerRelease` or the README `dotnet publish` command. |
| `Scripts\Local Tools\*` | Unknown/manual review needed | Ignored local-only folder. Not tracked and not moved by this cleanup. |

## Current Policy

- Active tracked package batch files under `Scripts` are limited to `Create Debug Package.bat` and `Create Project Brain.bat`; `Cleanup Project.bat` is a later maintenance-only exception for generated artifact cleanup.
- Active tracked PowerShell scripts under `Scripts` are limited to `create-debug-package.ps1`, `create-project-brain.ps1`, and `debug-analysis-tools.ps1`.
- Do not delete archived scripts until a separate cleanup pass confirms they are no longer useful as references.

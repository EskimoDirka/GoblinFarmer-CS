# GoblinFarmer Agent Instructions

## Required Before Any Code Changes

1. Read `Docs/Project_Status.md`.
2. Treat `Docs/Project_Status.md` as the source of truth for:
   - route logic
   - blocking rules
   - active issues
   - confirmed working behavior
   - next recommended task
3. Verify .gitignore is up to date and properly excludes build artifacts, user-specific files, and other non-source files

## Required After Any Code Changes

1. Update `Docs/Project_Status.md`.
2. Record:
   - what changed
   - what was tested
   - what still needs testing
   - next recommended task
3. Keep README.md synchronized with high-level stable/current behavior when Project_Status changes affect user-facing behavior.

## Required Response And GitHub Follow-Up

- After every user prompt unless otherwise specified, provide the next steps to test.
- After every user prompt unless otherwise specified, commit and push resulting repository changes to GitHub. If no files changed, state that no commit was created.
- If the user submits a release prompt, update GitHub accordingly with a version tag and release notes after the release build artifacts are created and validated.

## Workflow Notes

- When a recent repeatable project workflow is added, changed, or used as the expected way to do work, add or update the concise workflow guidance in `AGENTS.md`.
- Before adding or keeping workflow guidance in `AGENTS.md`, check `Docs/Worflow blocklist.md`.
- Remove workflow guidance from `AGENTS.md` when it is listed in `Docs/Worflow blocklist.md`, and do not re-add blocked workflows unless the user explicitly removes them from that blocklist.
- Treat every user live-test run as a VS Debug run unless the user explicitly says otherwise. Goblin Tracker/counter workflows should automatically write the decision bundles, encounter captures, logs, and other runtime evidence needed for debugging. The VS Debug `Capture` button is only a supplemental image-recognition aid that creates fullscreen, minimap, journal, and metadata files when the user clicks it.
- For latest-debug-package review prompts, treat the named/latest debug package and live-test notes as evidence before changing code. Review the newest ZIP in `DebugPackages` when provided or requested. The batch launcher `Scripts\Create Debug Package.bat` delegates to `Scripts\create-debug-package.ps1` and is the intentional ZIP export workflow for both VS Debug and Release. Inspect `debug-package-analysis.txt`, `goblin-tracker-timeline.md`, `goblin-evidence-health.txt`, `debug-package-manifest.txt`, `goblin-tracker-summary.txt`, `goblin-tracker-review.html`, latest logs, route summaries, screenshots, GoblinEvidence, encounter captures, decision bundles, and observation diagnostics. The optional helper scripts under `Scripts\` may analyze an existing package directly, but they must not replace the batch-created ZIP workflow. Form close is a quiet shutdown path and must not be used as a package/export trigger. Do not run a separate evidence-reprocessing workflow unless the user explicitly introduces a new one.
- Keep Goblin Tracker test priorities in `Docs/TODO.md`; the retired VS Debug `Next Tests` tab and metadata workflow must not be reintroduced unless the user explicitly asks for it.
- When providing Goblin Tracker next test steps, list route-specific scenarios in the official route-logic order from `Docs/Project_Status.md` first, then list general reset, stale evidence, display, classification, package, and documentation checks.
- Do not commit generated executables, installer output, portable ZIPs, debug packages, logs, screenshots, source-upload folders, or `GitHub Upload` copies. Keep tracked `Config\AppSettings.json` sanitized for release/defaults; private VS Debug paths and toggles belong in ignored `Config\AppSettings.local.json`.

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

## Documentation Rules

After updating Docs/Project_Status.md:

- Keep README.md synchronized with:
  - Stable Systems
  - Current Focus
  - Current Improvements

README.md should remain high-level and user-facing.

Project_Status.md remains the detailed developer source of truth.

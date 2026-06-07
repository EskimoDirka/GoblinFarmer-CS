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
- For latest-debug-package review prompts, treat the named/latest debug package and live-test notes as evidence before changing code. Inspect package manifests, session summaries, logs, route summaries, Goblin Tracker metadata, and runtime artifacts first; use GitHub only as repository reference unless the user explicitly says otherwise.
- For Goblin Tracker evidence issues, ask the user to click the VS Debug `Review Files` button when fresh runtime evidence is needed; it self-discovers the active runtime evidence folder, runs Goblin replay against `Debug\GoblinEvidence`, records the `Next Tests` checkbox state automatically, and refreshes loose files under `Debug\GoblinReplayReview\Latest` without prompting or creating a ZIP. Review `Debug\GoblinReplayReview\Latest\goblin-tracker-review.html`, `goblin-tracker-summary.txt`, and `goblin-tracker-next-tests.txt`, then compare `Logs\GoblinReplay\GoblinReplay_*` log/HTML/summary/changed files and replay decision bundles with the live notes. ZIP debug package creation is reserved for release/export troubleshooting workflows.
- For terminal-only Goblin Tracker package review, use `Scripts\replay-goblin-evidence.ps1` to summarize the latest package artifacts before deciding whether code changes are needed.
- When Goblin Tracker live-validation priorities change, keep the VS Debug `Next Tests` diagnostics tab synchronized with the current in-game test scenarios so the app shows the next validation checklist during play.
- When providing Goblin Tracker next test steps, list route-specific scenarios in the official route-logic order from `Docs/Project_Status.md` first, then list general reset, stale evidence, display, classification, package, and documentation checks.

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

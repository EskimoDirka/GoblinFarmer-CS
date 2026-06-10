# Agent Invariants

Long-lived GoblinFarmer rules that should not change without deliberate review. This file is not current project status and does not replace `Docs/Project_Status.md`.

## Documentation

- `Docs/Project_Status.md` remains the source of truth for current state, confirmed behavior, active issues, and next tasks.
- Agent docs summarize stable behavior and procedures. They should stay concise and avoid copying large status sections.
- Project Brain contains project knowledge and documentation context, not source code or runtime artifacts.
- If current status and a stable manual conflict, `Docs/Project_Status.md` wins for the current implementation state.

## Build Modes

- Release builds default Debug Mode off.
- Visual Studio Debug builds default Debug Mode on.
- Debug-only tooling must remain unavailable in release builds unless explicitly intended and reviewed.

## Goblin Tracker

- Counting policy must remain deterministic.
- Duplicate suppression changes require explicit review.
- Area count limits must remain centrally defined.
- Blocked-area and area-limit behavior should be validated before changing count acceptance rules.

## Replay Tooling

- Replay tooling must never execute automatically.
- Replay should reuse production logic whenever practical.
- Replay is a diagnostic tool, not a production workflow.
- Replay output does not replace live validation for scanner timing, focus, UI rendering, or notification behavior.

## Maintenance

- Cleanup tooling must default to dry-run.
- Destructive cleanup must require an explicit delete flag or confirmation-gated launcher.
- Generated artifacts should be removable without affecting source.
- Project Brain should remain lightweight and documentation-focused.
- New artifact producers require a retention and package-size policy.

## Automation Safety

- Town automation failures should prefer skip-and-resume over hard failure when safe.
- Diagnostic tooling should not change production behavior.
- Debug captures, packages, replay, cleanup, and export workflows should remain explicit unless the user requests otherwise.

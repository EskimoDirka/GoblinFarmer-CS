# GoblinFarmer Agent Instructions

## Read Order

1. User request.
2. `AGENTS.md`.
3. `Docs/Project_Status.md`.
4. `Docs/Agent/Invariants.md`.
5. Relevant `Docs/Agent/*.md` manuals.

## Document Roles

- `Docs/Project_Status.md` is the current source of truth for active state, confirmed behavior, open issues, and next tasks.
- `Docs/Agent/Invariants.md` records long-lived project rules that should not change without deliberate review.
- Other `Docs/Agent/*.md` files are stable operating manuals for architecture, workflows, testing, maintenance, debugging, and release work.

## Conflict Rules

- The user request wins for the current task.
- `Docs/Project_Status.md` wins over agent manuals for current implementation state.
- `Docs/Agent/Invariants.md` governs long-term rules unless the user explicitly asks to revise an invariant.

## Operating Rules

- Read `Docs/Project_Status.md` before code or workflow changes, then read the relevant agent docs before touching related areas.
- If the user says `debug review`, follow `Docs/Agent/Debug_Review_Automation.md` after the standard read order.
- Update `Docs/Project_Status.md` after code, script, workflow, or documentation changes.
- Do not commit, push, create branches, merge, tag, or create GitHub releases unless explicitly requested.
- Do not install Agent Zero or add a new agent framework for this project.

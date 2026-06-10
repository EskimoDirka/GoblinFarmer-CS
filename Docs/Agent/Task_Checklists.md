# Agent Task Checklists

Concise checklists for Codex task hygiene. Current validation priorities and active project state live in `Docs/Project_Status.md`.

## Task Start Checklist

- Read `Docs/Project_Status.md`.
- Read `Docs/Agent/Invariants.md` when changing long-lived policy or workflow behavior.
- Read the relevant `Docs/Agent/*.md` manual for the area being touched.
- Verify the requested scope and avoid unrelated refactors.
- Identify validation requirements before editing.
- Check the working tree so unrelated user changes are not overwritten.

## Task Completion Checklist

- Validation completed or clearly documented as skipped with the reason.
- Relevant docs updated.
- `Docs/Project_Status.md` updated with what changed, what was tested, remaining test gaps, and the next recommended task.
- Next test steps documented for the user.
- No commit, push, branch, tag, merge, release, framework install, Skill, or MCP integration unless explicitly requested.

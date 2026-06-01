# GoblinFarmer Agent Instructions

Always read:

Docs/Project_Status.md

before making any changes.

Requirements:
- Follow route logic defined in Project_Status.md.
- Update Project_Status.md after every completed task.
- Preserve existing behavior unless task explicitly changes it.
- Build successfully before finishing.
- Add diagnostic logging for new workflows.
- If Project_Status.md and code behavior disagree, Project_Status.md wins.

## Debugging Workflow

During debugging tasks:
- Prefer logging first before changing behavior.
- Do not remove existing logs unless asked.
- Add enough logs to prove why a decision was made.
- Preserve working behavior unless the task explicitly changes it.
- If a fix affects route logic, update Docs/Project_Status.md.

## Build/Test Rule

After code changes:
- Run a build.
- If build fails, fix compile errors before stopping.
- Summarize changed files, build result, and next test.

## Git Rule

Do not commit automatically unless asked.
After changes, summarize what should be committed.

## Screenshot/Log Rule

When debugging image recognition or teleport failures:
- Save debug screenshots on failure.
- Log image name, scan region, confidence, threshold, and best/second-best matches.

## Safety Rule

Do not change:
- Battle.net launch flow
- Combat logic
- Repair/salvage logic
unless the current task specifically asks for it.

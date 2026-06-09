# Agent Release Manual

Stable release guidance for Codex. Current release readiness and version targets live in `Docs/Project_Status.md`; that file wins on conflict.

## Release Build Workflow

- Release version metadata starts in `GoblinFarmer.csproj`.
- Use Visual Studio with the `GoblinFarmerRelease` publish profile or the README `dotnet publish` command.
- Confirm publish output includes the executable, icon, config defaults, Images, and the supported debug package scripts.
- Do not treat a publish folder as source.

## Installer Workflow

- Compile `Installer\GoblinFarmer.iss` only after the publish folder exists.
- The installer reads version data from the published executable.
- Installer output belongs under generated artifact locations and must not be committed.
- Keep the latest installer when pruning older installer artifacts.

## Version, Tag, And GitHub Expectations

- Do not create tags, GitHub releases, branches, commits, or pushes unless the user explicitly requests them.
- If the user submits a release prompt, build and validate artifacts first, then update GitHub release notes/tags only as requested.
- Release notes should come from the matching `Docs\Release_v*.md` file or user-approved notes.

## Release Readiness Checklist

Before release, validate:

- `dotnet build GoblinFarmer.csproj`
- publish output and version metadata
- installer build and installer version
- first-run path discovery and settings behavior
- Debug Mode visibility and release defaults
- generated artifacts are ignored and not staged as source
- README and release notes match the shipped behavior

Use `Docs\Release_Checklist.md` for the active checklist.

## What Not To Change During Release Prep

- Do not change combat, route, repair/salvage, Battle.net launch, Goblin Tracker count policy, replay wiring, or debug package behavior unless explicitly requested.
- Do not introduce a new packaging/export workflow during release prep.
- Do not commit local config, debug packages, installer output, portable ZIPs, logs, screenshots, or runtime evidence.

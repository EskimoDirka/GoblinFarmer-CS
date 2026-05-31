# GoblinFarmer-CS

A WinForms C# Diablo III farming assistant.

## Features

- Teleport routing system
- Teleport Next hotkey
- Location verification
- Teleport blocking logic
- Combat automation
    - Monk
    - Demon Hunter
    - Witch Doctor
- Make New Game flow
- Leave Game flow
- Repair automation
- Salvage automation
- Image recognition with scan region support

## Project Status

Current Version: v1.1

### Completed

- Python to C# port
- Partial class architecture
- Teleport routing
- Combat profiles
- Game flow automation
- Town automation

### Planned

- Performance metrics
- Screenshot debug mode
- Combat profile editor
- UI improvements

## Structure

```text
frmMain.Combat.cs
frmMain.GameFlow.cs
frmMain.Hotkeys.cs
frmMain.TeleportRouting.cs
frmMain.TeleportState.cs
frmMain.Town.cs
frmMain.PortedAutomation.cs
```

## Release Workflow

Run the release script from the repository root:

```powershell
.\Scripts\release.ps1 -Version "v1.2" -Message "Add session stats and debug screenshots"
```

The script stages changes, commits with `<Version> <Message>`, pushes the branch, creates the git tag, and pushes the tag to `origin`. If GitHub CLI is installed, it also creates a GitHub release with `gh release create`.

`README.md` is manual and is not overwritten by the release script. Update `CHANGELOG.md` before releasing; it is used as the release notes file when GitHub CLI is available.

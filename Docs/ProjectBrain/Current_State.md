# Current State

Source of truth: `Docs\Project_Status.md`. This file is a short context-loading summary only.

## Project Summary

GoblinFarmer is a personal Windows Forms automation project for Diablo III farming workflows. It includes Battle.net/Diablo launch support, route-aware teleporting, combat helpers, town automation, Goblin Tracker evidence recognition, and diagnostic packaging.

Current release line: `v1.5.0`.

Current focus: v1.5 release readiness for salvage reliability, Inventory Replay, and gem auto-stashing, with ongoing Goblin Tracker validation.

## Stable Behavior

- Battle.net/Diablo launch, Start Game, Make New Game, route teleporting, interrupted teleport recovery, repair/salvage, gem stashing, Kadala timing, and Witch Doctor combat are stable enough for ongoing monitoring.
- Release and VS Debug Goblin Tracker layouts keep evidence and Last Observation fields readable.
- Debug packages are created only through `Scripts\Create Debug Package.bat`.
- Project Brain markdown docs remain available directly under `Docs\ProjectBrain`; ZIP generation has been retired.
- Form close is intentionally quiet and does not create debug packages, replay evidence, screenshots, or reports.
- Runtime/debug artifacts are bounded by retention policy and are not source files.
- Notification latency, stale journal suppression, Last Observation persistence, Sim Count expansion, salvage single-scan caching, Inventory Replay, and debug package retention are stable after the latest validation pass.
- VS Debug `Sim Count` keeps `Current Area` pinned first, then alphabetizes the centralized area list while preserving existing count/block/duplicate behavior.
- Tracked `Config\AppSettings.json` must stay sanitized; private VS Debug settings belong in ignored `Config\AppSettings.local.json`.

## Active Development Focus

- Continue validating automatic Goblin Tracker counts during normal VS Debug use.
- Live-validate gem auto-stashing after adding `Images\Gems` templates and gem stash coordinates.
- Use live testing for real detection misses, reset behavior, distinct alerts/sound, and cases where replay output does not match package evidence.
- Use explicit Goblin Replay for suspicious stale-location or count-policy cases that can be modeled from saved Journal/Minimap/current-location evidence.
- Use VS Debug `Sim Count` for deterministic duplicate, blocked-area, countable-area, and area-limit policy checks.

## Current Known Issues And Watch Items

- First-seen Journal Engaged-only evidence should remain pending; sustained same-area active-combat Engaged evidence can count after the confirmation window.
- Rainbow Goblin distinct alert and sound still need live validation.
- Reset Stats clearing and Make New Game clearing still need normal live validation.
- Demon Hunter blocked-cursor fallback needs focused live validation; unexpected combat stops should be investigated only when a package shows a stop without explicit hotkey, focus, teleport, or app-close cause.
- Western Channel Level 1, Leoric's Passage, and Black Canyon Mines misses should be reopened only with fresh package evidence.

## Recent Project Brain Utility State

The Project Brain ZIP utility was removed after dependency review. `Docs\ProjectBrain\*.md` remains as direct Codex context and should not include runtime artifacts, debug packages, screenshots, images, videos, binaries, replay captures, or existing ZIPs.

## Current Scripts Policy

Tracked package batch files under `Scripts` are limited to `Create Debug Package.bat`. Required backing scripts are `create-debug-package.ps1`; `debug-analysis-tools.ps1` remains as a direct debug package dependency. `Cleanup Project.bat`, `Cleanup Project Delete.bat`, and `cleanup-project.ps1` are maintenance-only generated-artifact cleanup exceptions. The dry-run launcher and PowerShell script default to dry-run; the delete launcher asks for `Y/N` before passing `-Delete`. Older manual helpers were archived under `Docs\ScriptArchive\2026-06-09`.

`Scripts\cleanup-project.ps1` writes `Reports\Cleanup_Report.md`; real deletion requires `-Delete`.

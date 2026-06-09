# Current State

Source of truth: `Docs\Project_Status.md`. This file is a short context-loading summary only.

## Project Summary

GoblinFarmer is a personal Windows Forms automation project for Diablo III farming workflows. It includes Battle.net/Diablo launch support, route-aware teleporting, combat helpers, town automation, Goblin Tracker evidence recognition, and diagnostic packaging.

Current release line: `v1.4.0`.

Current focus: Goblin Tracker full automatic counting readiness in VS Debug, with continued live validation around evidence detection, stale suppression, notification latency, and display/reset behavior.

## Stable Behavior

- Battle.net/Diablo launch, Start Game, Make New Game, route teleporting, interrupted teleport recovery, repair/salvage, Kadala timing, and Witch Doctor combat are stable enough for ongoing monitoring.
- Release and VS Debug Goblin Tracker layouts keep evidence and Last Observation fields readable.
- Debug packages are created only through `Scripts\Create Debug Package.bat`.
- Project Brain packages are created through `Scripts\Create-ProjectBrain.bat`.
- Form close is intentionally quiet and does not create debug packages, replay evidence, screenshots, or reports.
- Runtime/debug artifacts are bounded by retention policy and are not source files.
- Tracked `Config\AppSettings.json` must stay sanitized; private VS Debug settings belong in ignored `Config\AppSettings.local.json`.

## Active Development Focus

- Continue validating automatic Goblin Tracker counts during normal VS Debug use.
- Use live testing for real detection misses, notification timing, Last Observation display, reset behavior, and cases where replay output does not match package evidence.
- Use explicit Goblin Replay for suspicious stale-location or count-policy cases that can be modeled from saved Journal/Minimap/current-location evidence.
- Use VS Debug `Sim Count` for deterministic duplicate and area-limit policy checks.

## Current Known Issues And Watch Items

- Notification latency still needs post-fix live validation. Strong Minimap evidence should no longer be masked by pending Journal Engaged evidence, and logs should show `GoblinEvidenceCandidateSelection reason=JournalPendingMinimapConfirmed` when that path applies.
- First-seen Journal Engaged-only evidence should remain pending; sustained same-area active-combat Engaged evidence can count after the confirmation window.
- Rainbow Goblin distinct alert and sound still need live validation.
- Last Observation persistence, Reset Stats clearing, and Make New Game clearing still need normal live validation.
- Demon Hunter blocked-cursor fallback needs focused live validation; unexpected combat stops should be investigated only when a package shows a stop without explicit hotkey, focus, teleport, or app-close cause.
- Western Channel Level 1, Leoric's Passage, and Black Canyon Mines misses should be reopened only with fresh package evidence.

## Recent Project Brain Utility State

The Project Brain utility now creates a docs-only ZIP under `ProjectBrain` and includes `Docs\ProjectBrain\*.md` plus selected small stable docs. It must not include runtime artifacts, debug packages, screenshots, images, videos, binaries, replay captures, or existing ZIPs.

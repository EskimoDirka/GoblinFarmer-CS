# Combat Profiles

GoblinFarmer includes combat helpers for Monk, Demon Hunter, and Witch Doctor profiles.

## Scope

- Combat helpers are intended to support farming workflows while Diablo III is focused.
- Combat input is guarded by no-click regions so the app avoids clicking known UI areas.
- Physical hotkeys can be suppressed during combat when they would conflict with automation.

## Profiles

- Monk: uses the current Monk combat loop and safety checks.
- Demon Hunter: supports right-hold behavior from safe world areas and avoids sending new mouse clicks over protected UI.
- Witch Doctor: supports held/channel behavior from safe world areas and keeps keyboard timers running while UI clicks are suppressed.

## Debugging

Combat diagnostics are written to the normal app log and can be included in generated debug packages. Debug Mode can expose additional state for profile validation without changing the active combat profile.

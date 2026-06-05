# Combat Profiles

GoblinFarmer includes combat helpers for Monk, Demon Hunter, and Witch Doctor profiles.

## Scope

- Combat helpers are intended to support farming workflows while Diablo III is focused.
- Combat input is guarded by no-click regions so the app avoids clicking known UI areas.
- Physical hotkeys can be suppressed during combat when they would conflict with automation.
- The selected combat profile is saved in `Config\AppSettings.json` as `User.CombatProfile` and restored on startup.

## Profiles

- Monk: uses the current Monk combat loop and safety checks.
- Demon Hunter: supports right-hold behavior from safe world areas, keeps key rotation running while protected UI clicks are suppressed, and skips unsafe Shift+Left maintenance clicks instead of stopping combat.
- Witch Doctor: uses mouse wheel scrolling as the repeated mouse action, keeps the `2, 3, 1` key loop running, and does not use held-left/channel mouse mode.

## Combat Profile Skill Setup

GoblinFarmer expects these exact Diablo III skills and runes to be assigned to the listed keybinds for each combat profile. Incorrect skill placement may cause combat automation to behave incorrectly.

### Monk

| Keybind | Skill | Rune |
|---|---|---|
| 1 | Epiphany | Insight |
| 2 | Mystic Ally | Air Ally |
| 3 | Dashing Strike | Radiance |

### Demon Hunter

| Keybind | Skill | Rune |
|---|---|---|
| Left Mouse | Hungering Arrow | Devouring Arrow |
| Right Mouse | Strafe | Drifting Shadow |
| 1 | Preparation | Focused Mind |
| 2 | Companion | Bat Companion |
| 3 | Vengeance | Seethe |
| 4 | Smoke Screen | Displacement |

### Witch Doctor

| Keybind | Skill | Rune |
|---|---|---|
| 1 | Hex | Angry Chicken |
| 2 | Horrify | Stalker |
| 3 | Spirit Walk | Severance |

## Debugging

Combat diagnostics are written to the normal app log and can be included in generated debug packages. Debug Mode can expose additional state for profile validation without changing the active combat profile.

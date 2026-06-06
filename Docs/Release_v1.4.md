# GoblinFarmer v1.4

## Highlights

* Added Goblin Tracker Automation Observation Mode for Journal and Minimap evidence without enabling automatic goblin counting
* Hardened manual `X` goblin counting with fresh-evidence gating, stale journal protection, blocked-area suppression, and per-area duplicate limits
* Added Pandemonium Fortress Level 1, Pandemonium Fortress Level 2, and Stinging Winds two-count exceptions
* Improved Goblin Tracker Last Observation diagnostics and accepted-count display behavior
* Added Witch Doctor mouse-wheel plus cursor-change left-click combat input
* Improved startup path validation and Diablo III auto-discovery for custom install locations
* Improved debug package size controls for screenshots and Goblin Evidence diagnostics
* Improved salvage timing diagnostics and per-slot speed

## Quality Improvements

* Observation Mode now reports what would count while preserving manual `X` as the only real count action
* Journal evidence uses first-seen freshness signatures so old visible journal lines cannot be reused after moving areas
* Manual no-count locations such as WhimsyDale and blocked Caldeum/Waterway areas suppress before consuming count slots
* Goblin Tracker reset and new-game cleanup clear duplicate guard, evidence cooldowns, and Last Observation state
* Manual accepted counts keep `ManualHotkey` / `Counted` visible during the 5-second notification window
* Debug packages include bounded recent evidence samples and report included/excluded screenshot counts
* Release installer metadata is synchronized with the published application version

## Notes

Automatic Goblin Tracker counting is still not enabled in v1.4. Observation Mode is diagnostic-only and exists to validate evidence, area resolution, duplicate logic, and block-list behavior before automatic counting is designed.

After installing, use `Verify Paths` to confirm Diablo III, Battle.net, and Images paths. Continue validating image-recognition behavior on unusual display, DPI, or multi-monitor setups.

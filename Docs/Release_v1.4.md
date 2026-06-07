# GoblinFarmer v1.4

## Highlights

* Added Goblin Tracker Automation Observation Mode for Journal and Minimap evidence
* Added gated automatic Goblin Tracker counting behind Observation Mode and Auto Goblin Count
* Retired the physical `X` Goblin Tracker count hotkey and added a VS Debug `Capture` button for image-recognition troubleshooting snapshots
* Hardened automatic counting with fresh-evidence gating, stale journal protection, blocked-area suppression, and per-area duplicate limits
* Added Pandemonium Fortress Level 1, Pandemonium Fortress Level 2, and Stinging Winds two-count exceptions
* Improved Goblin Tracker Last Observation diagnostics and accepted-count display behavior
* Added Witch Doctor mouse-wheel plus cursor-change left-click combat input
* Improved startup path validation and Diablo III auto-discovery for custom install locations
* Improved debug package size controls for screenshots and Goblin Evidence diagnostics
* Improved salvage timing diagnostics and per-slot speed

## Quality Improvements

* Observation Mode can report candidates without counting, while automatic counting remains gated by a separate setting
* Journal evidence uses first-seen freshness signatures so old visible journal lines cannot be reused after moving areas
* Manual no-count locations such as WhimsyDale and blocked Caldeum/Waterway areas suppress before consuming count slots
* Goblin Tracker reset and new-game cleanup clear duplicate guard, evidence cooldowns, and Last Observation state
* Accepted automatic counts write decision bundles and encounter captures for review
* Debug packages include bounded recent evidence samples and report included/excluded screenshot counts
* Release installer metadata is synchronized with the published application version

## Notes

Automatic Goblin Tracker counting is available only when both Observation Mode and Auto Goblin Count are enabled. Keep Auto Goblin Count off when using Observation Mode for diagnostics only.

After installing, use `Verify Paths` to confirm Diablo III, Battle.net, and Images paths. Continue validating image-recognition behavior on unusual display, DPI, or multi-monitor setups.

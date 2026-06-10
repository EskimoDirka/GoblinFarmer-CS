# GoblinFarmer v1.5

## Highlights

* Added blue/yellow bulk salvage behind `Repair.EnableBulkCategorySalvage`
* Added confirmation-aware set/legendary leftover salvage with stale cached target verification
* Added normal-gem exclusion during salvage for Emerald, Ruby, Amethyst, Topaz, and Diamond
* Added gated normal-gem auto-stashing behind `Stash.EnableAutoGemStash`
* Added separated `Images\Gems` support for gem templates and gem stash coordinates
* Added explicit Inventory Replay support for salvage and gem-stash scans
* Added bounded debug-package inclusion for Inventory Replay artifacts

## Quality Improvements

* Salvage uses legal one- or two-row item footprints and rejects textured empty cells
* Salvage performs bounded recovery rescans when accepted non-gem leftovers remain
* Auto gem stashing safely skips when assets or coordinates are missing
* Auto gem stashing right-clicks only accepted production gem-template matches
* Debug logs include salvage category color metrics, gem-skip diagnostics, gem-stash candidate decisions, and replay artifact paths
* Release defaults keep bulk salvage and auto gem stashing disabled until release readiness approval, while VS Debug enables them for validation
* Release version metadata is synchronized with the published application version

## Notes

Auto gem stashing is configured separately from salvage assets. Add normal gem templates directly under `Images\Gems` and provide `Images\Gems\Gem Coordinates.txt` with `Stash Coordinates` and `Gem Stash Tab Coordinates`.

Inventory Replay remains explicit and on-demand. It does not run during startup, town automation, scanner workflows, or debug package creation.

# Teleport Logic

GoblinFarmer uses image recognition and route state to decide when Teleport Next is safe and what destination should be selected next.

## Route Behavior

- Route state tracks the current location, next target, queued retry target, and last requested target.
- Current-location detection keeps raw, normalized, display, and blocking locations separate so logs and user messages stay accurate.
- Manual teleport buttons are intentionally allowed to bypass hotkey route blocking.
- Teleport Next uses route-blocking rules to prevent known unsafe or incorrect transitions.

## v1.3 Reliability Notes

- Cathedral, Caldeum, Ancient Waterway/channel, Stinging Winds, Caverns of Frost, Cave Of The Moon Clan, and WhimsyDale edge cases are represented in the route-blocking model.
- Caldeum sublocations keep their specific detected names in logs and notifications, while route button state still groups them under the City Of Caldeum checkpoint.
- Cave Of The Moon Clan Level 1 blocks Teleport Next routing, while Cave Of The Moon Clan Level 2 is an allowed Southern Highlands route sublocation that can continue to Northern Highlands.
- Caverns of Frost Level 1 blocks Teleport Next routing, while Caverns of Frost Level 2 remains allowed for the Rakkis Crossing path.
- After combat stops, Teleport Next can be queued immediately once combat is marked stopped; tracked mouse and Shift inputs are still released before the teleport flow runs, Witch Doctor cleanup no longer waits multiple seconds for Hex confirmation, and app-generated number-key taps are guarded so only user `1` presses trigger Teleport Next.

## Diagnostics

Teleport decisions are logged with route state, detected location, requested target, source, blocking reason, and screenshot evidence when enabled.

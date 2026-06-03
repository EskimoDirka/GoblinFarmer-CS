# GoblinFarmer Test Checklist

## Make New Game
- Battle.net launches/focuses
- Diablo III tab selected
- Play clicked
- Paired success screenshots captured for Battle.net Play click accepted
- Diablo launches
- Paired success screenshots captured for Diablo process detected
- Start Game clicked
- Paired success screenshots captured for Start Game click accepted
- Southern Highlands loaded
- Paired success screenshots captured for Southern Highlands teleport confirmation

## Teleport Route
- Southern Highlands -> Northern Highlands
- Northern Highlands -> Weeping Hollow
- Weeping Hollow -> Festering Woods
- Festering Woods -> Cathedral
- Cathedral Level 1 blocks Royal Crypts
- Cathedral Level 3 allows Royal Crypts
- City of Caldeum only allows Ancient Waterway from Ruined Cistern
- Stinging Winds blocks Battlefields
- Black Canyon Mines allows Battlefields
- Caverns of Frost Level 1 blocks Teleport Next
- Caverns of Frost Level 2 allows Rakkis Crossing
- Each successful teleport confirmation captures paired Diablo/App success screenshots
- Route blocks/failures capture paired Diablo/App failure screenshots

## Failure Recovery
- Interrupted teleport preserves green/orange buttons
- Retry hotkey uses same intended target
- Failed teleport does not advance route
- Failure screenshots include both Diablo and GoblinFarmer app evidence

## Debug Package Evidence
- Debug package includes latest log
- Debug package includes `route-failure-summary.txt`
- Debug package includes `debug-screenshot-manifest.txt`
- Screenshot manifest pairs success/failure Diablo and App screenshots by timestamp, workflow, and action
- Screenshot manifest includes only current-session screenshots
- Success screenshots are included under `Screenshots\Success`
- Failure screenshots are included under `Screenshots\Failure`
- Recent debug screenshots are included under `Screenshots\Recent`
- Screenshots from previous sessions are excluded from the package
- Debug package manifest reports package size, session start, session duration, total screenshots, success screenshots, failure screenshots, and stale screenshot exclusions
- Screenshot retention cleanup still controls all runtime screenshots in the shared `Screenshots` folder

## Town And Exit
- Repair complete captures paired Diablo/App success screenshots
- Salvage complete or skipped captures paired Diablo/App success screenshots
- Leave Game main menu confirmation captures paired Diablo/App success screenshots
- Exit Game complete captures paired Diablo/App success screenshots

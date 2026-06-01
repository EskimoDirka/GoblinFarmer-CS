# GoblinFarmer Project Status

## Current Focus
Battle.net launch flow is now mostly working. Next focus is normal teleport route testing.

## Last Known Good
- Images moved into project and pushed to GitHub.
- Battle.net can relaunch/focus if process exists but no visible window exists.
- Diablo launch grace period prevents false cancellation.
- Start Game verified successfully.
- Make New Game flow created 1 game and completed first teleport to Southern Highlands.

## Active Issues
- Battle.net scan regions were captured fullscreen and may fail when Battle.net is windowed.
- Need to test full teleport route.
- Need to test interrupted teleport recovery.
- Need to test Exit Game workflow.
- Need to verify repair + salvage flow.
- Need to validate publish/release folder includes Images.

## Next Test
Run teleport-next repeatedly through the route and check:
- button colors
- next target
- blocked teleport behavior
- failed teleport recovery

## Backlog
- Make Battle.net scan regions relative to Battle.net window.
- Clean up nested GoblinFarmer folder structure later.
- Review right-click behavior after Battle.net Play.
- Improve Start Game diagnostics if it fails again.

## Important Paths
Project:
D:\D3\Projects\GoblinFarmer\GoblinFarmer\GoblinFarmer

Runtime Images:
bin\Debug\net10.0-windows\Images

Release Target:
D:\GoblinFarmer
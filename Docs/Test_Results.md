# GoblinFarmer Test Results

This file records recent validation runs that are useful for release readiness review. `Docs/Project_Status.md` remains the source of truth for current behavior and open state.

## 2026-06-10 Latest Live Review Fix Pass

- Review inputs: `DebugPackages\GoblinFarmer_Debug_20260610_150324.zip` and `Video Clip Review\2026-06-10 13-55-23.mkv`.
- Issues covered: missed Goblin Tracker Journal counts after same-template stale visible rows, Event Complete menu not clearing automatically, lowercase OBS status text, inactive repair-button click, salvage/gem confusion around set/legendary leftovers, and auto-stash travel when no valid gems were present.
- Fix coverage: stale Journal visible-line fresh-area bypass, Enter-before-Escape event menu clear, proper-case OBS display status, repair-button active-color sampling, stack-text-gated gem-stash fallback, strong non-gem salvage quality boundary preservation, and no-gem auto-stash preflight skip.

Validation commands:

```powershell
dotnet build .\GoblinFarmer.csproj
dotnet test .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
```

Results:

- `dotnet build .\GoblinFarmer.csproj`: passed.
- `dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet test .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed after the gem fallback was adjusted to keep diamond-like stack support while still requiring stack-count text.
- `git diff --check`: passed with line-ending normalization warnings only.

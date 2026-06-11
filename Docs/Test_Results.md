# GoblinFarmer Test Results

This file records recent validation runs that are useful for release readiness review. `Docs/Project_Status.md` remains the source of truth for current behavior and open state.

## 2026-06-11 111859 Live Review Fix Pass

- Review inputs: `DebugPackages\GoblinFarmer_Debug_20260611_111859.zip` and `Video Clip Review\2026-06-11 10-39-48.mkv`.
- Issues covered: wrong first notification for a Highlands Cave Odious Collector, delayed Western Channel Level 1 and Battlefields notifications from accepted counts with `total=0`, repair stop after a visually successful click, and OBS recording continuing until the VS Debug form closed when the Diablo process lingered without a visible game window.
- Fix coverage: Treasure/Odious minimap template/color disagreement is suppressed for Journal confirmation instead of reporting the wrong type, recent-minimap Journal reliability now continues through duplicate guard and count recording, repair can soft-confirm a click when focused active pixels collapse, and the local OBS monitor stops recording when the visible Diablo window closes.
- Replay note: `--debug-package-replay` loaded the attached package successfully; it contained no replay logs or review frames to replay.

Validation commands:

```powershell
dotnet build .\GoblinFarmer.csproj
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet test .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --debug-package-replay .\DebugPackages\GoblinFarmer_Debug_20260611_111859.zip
git diff --check
```

Results:

- `dotnet build .\GoblinFarmer.csproj`: passed.
- `dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet test .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed with new checks for Treasure/Odious minimap disagreement suppression, recent-minimap Journal count recording, repair soft confirmation, and visible-window OBS shutdown source coverage.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --debug-package-replay .\DebugPackages\GoblinFarmer_Debug_20260611_111859.zip`: passed package load; no image scenarios were present.

## 2026-06-11 Best-Sample Promotion Helper

- Scope covered: reusable debug-only image-recognition best-sample capture/promotion helper wired to Goblin Evidence and Gem Auto-Stash only; Salvage remains excluded.
- Fix coverage: top-N candidate capture with metadata, deterministic best-candidate selection, quality rejection for unsafe samples, unique promotion filenames with sidecar metadata, promoted Goblin/Gem template discovery through sidecars, accepted-count-only Goblin Evidence capture, success-only Gem Auto-Stash capture, per-domain retention cleanup, and bounded debug-package inclusion.
- Defaults: accepted-sample capture is release/default off and VS Debug on for validation; Goblin/Gem promotion is disabled by default.

Validation commands:

```powershell
dotnet build GoblinFarmer.csproj
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
git diff --check
```

Results:

- `dotnet build GoblinFarmer.csproj`: passed.
- `dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed with new checks for shared helper capture/selection/promotion/retention, Goblin Evidence accepted-count-only wiring, Gem Auto-Stash success-only wiring, promoted template sidecar loading, and bounded debug-package inclusion.
- `git diff --check`: passed.

## 2026-06-11 Follow-Up Live Review Fix Pass

- Review inputs: `DebugPackages\GoblinFarmer_Debug_20260611_082506.zip` and `Video Clip Review\2026-06-11 07-41-00.mkv`.
- Issues covered: suppressed/duplicate PF2 notification with invalid total, stale Journal Last Observation returning while idle/menu-like, repair active-button diagnostics, unidentified legendary/set salvage missed while gem stash later stashed gear, and debug package PNG/file growth visibility.
- Fix coverage: positive-total guard before automatic-count splash notifications, `LastObservationUpdateSkippedPublishGuard` for non-counting idle Journal rows, unidentified salvage template matches overriding gem rejection, gem-stash safety scan loading unidentified salvage templates and requiring stack-verified gem evidence, explicit `RepairButtonColorSample` diagnostics with broader gold active-state matching, and `debug-package-size-summary.txt` inside future debug ZIPs.
- Replay note: `--debug-package-replay` loaded the attached package successfully, but no image replay scenarios were available because replay crops are excluded by default; the package included structured replay logs only.

Validation commands:

```powershell
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet test .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --debug-package-replay .\DebugPackages\GoblinFarmer_Debug_20260611_082506.zip
```

Results:

- `dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet test .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed with new checks for notification total gating, idle Journal Last Observation suppression, unidentified salvage/gem-stash safety, repair diagnostics, and package size summary.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --debug-package-replay .\DebugPackages\GoblinFarmer_Debug_20260611_082506.zip`: passed package load; no image scenarios were present.

## 2026-06-11 Morning Live Review Fix Pass

- Review inputs: `DebugPackages\GoblinFarmer_Debug_20260611_071517.zip` and `Video Clip Review\2026-06-11 06-42-19.mkv`.
- Issues covered: delayed/wrong-area Goblin Tracker counts after area transitions, Battlefields Treasure Goblin blocked by stale visible journal state, bounty menu Enter close failure, repair active-button color miss, salvage false target on compact Marquise Topaz, missing unidentified set salvage support, and gem-stash color coverage.
- Fix coverage: pending Journal evidence first-seen area locking, original evidence-age-aware stale visible-line bypass, Escape-only bounty menu close diagnostics, broader repair active-button color matching, unidentified set salvage template support, compact Topaz/regular-gem salvage rejection, non-blocking expected-confirmation miss handoff to final leftover scan, and all-normal-gem color fallback coverage for gem stashing.

Validation commands:

```powershell
dotnet build .\GoblinFarmer.csproj
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet test .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
```

Results:

- `dotnet build .\GoblinFarmer.csproj`: passed.
- `dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet test .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed with new checks for Escape-only bounty close, journal area-lock diagnostics, unidentified set salvage, compact Topaz salvage rejection, and all normal gem color fallback.

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

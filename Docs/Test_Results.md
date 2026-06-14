# GoblinFarmer Test Results

This file records recent validation runs that are useful for release readiness review. `Docs/Project_Status.md` remains the source of truth for current behavior and open state.

## 2026-06-14 Goblin Evidence Policy Refactor

- Scope: Phase 2 refactor start. Mechanically extracted Goblin Evidence policy types into `GoblinCountAreaPolicies.cs`, `GoblinJournalFreshnessPolicy.cs`, and `GoblinAutoCountEncounterSuppressionPolicy.cs`.
- Behavior note: no production behavior, auto-count thresholds, scanner execution, overlay behavior, combat, routing, repair, salvage, gem-stash, Kadala, make-new-game, replay execution, package replay policy, package format, or OBS control behavior changed.

Validation commands:

```powershell
dotnet build GoblinFarmer.csproj
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --goblin-auto-count-matrix
git diff --check
```

Results:

- Passed in the source worktree before commit: both builds, full console harness, AutoCount Matrix with `scenarios=9`, `steps=21`, `failed=0`, and `git diff --check` with LF-to-CRLF normalization warnings only.

## 2026-06-14 Auto-Count Stabilization Tooling Pass

- Scope: tooling and coverage only. No runtime auto-count acceptance policy, scanner execution, overlay display, combat, route, town, repair, salvage, gem stash, Kadala, or make-new-game behavior was changed.
- Added coverage: explicit AutoCount Matrix scenarios for Southern Highlands -> Cave Level 1 stale Journal, Ruined Cistern -> Ancient Waterway blocked context, Cave Level 1 -> Level 2 independent counts, Cathedral stale Journal row, Northern Highlands fresh Journal after old empty-bucket Cave row, PF2 first/second/third count behavior, Stinging Winds two-count behavior, New Game carryover, and Minimap-to-Journal source variants.
- Added package analysis: `goblin-auto-count-triage.md` and `goblin-auto-count-triage.json` are generated in debug packages from available logs/events/DecisionBundles/ReviewEvidence/crops, and missing sources are reported instead of failing package creation.
- Added draft workflow: `Scripts\draft-auto-count-scenario.ps1` accepts a debug package path, timestamps, and notes, then writes review-only draft files under `Debug\AutoCountScenarioDrafts` or a caller-supplied output folder.
- Package-size note: new auto-count stabilization artifacts are text/JSON/docs only. A smoke package reported `goblin-auto-count-triage.md`, `goblin-auto-count-triage.json`, and `Docs\AutoCount_Matrix.md`; full videos and bulk image folders stayed excluded. Example triage generation against `DebugPackages\GoblinFarmer_Debug_20260614_071706.zip` produced `goblin-auto-count-triage.md` at `17,664` bytes and `goblin-auto-count-triage.json` at `444,112` bytes.
- Live-only notes: scanner timing, Diablo focus/input, notification rendering, OBS/overlay UI, missing-frame issues, and real same-type cross-area Minimap collision shapes still require live review or captured DecisionBundles.

Validation commands:

```powershell
dotnet build GoblinFarmer.csproj
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --goblin-auto-count-matrix
git diff --check
```

Results:

- `Scripts\draft-auto-count-scenario.ps1` smoke run: passed; created `draft-scenario.txt` and `draft-explanation.md` in a temp output folder only.
- `Scripts\create-debug-package.ps1` smoke run with a temp runtime root: passed; included auto-count triage reports and reported package size.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --goblin-auto-count-matrix`: passed with `scenarios=9`, `steps=21`, `failed=0`, `passed=True`.
- `dotnet build GoblinFarmer.csproj`: passed.
- `dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed, including AutoCount Matrix, debug package triage, draft-helper, and package-bound assertions.
- `git diff --check`: passed with LF-to-CRLF normalization warnings only.

## 2026-06-14 071706 Live Review Fix Pass

- Review inputs: `DebugPackages\GoblinFarmer_Debug_20260614_071706.zip` and `Video Clip Review\2026-06-14 07-05-08.mkv`.
- Issues covered: Highlands Cave Treasure Goblin at about `02:40` was not counted/notified even though the decision-bundle Journal crop showed both `engaged a Treasure Goblin` and `killed a Treasure Goblin`.
- Root cause: journal template selection picked the Engaged template as the best candidate and carried `Kind=JournalEngaged` into count policy, so the visible killed line in the same crop never became `JournalKilledConfirmed`.
- Fix coverage: Journal candidate selection now promotes an Engaged candidate to a same-crop same-type Killed companion only when the Killed match meets its normal threshold, appears in the active feed, is near/below the Engaged row, and passes the existing journal-name validation. Diagnostics log accepted promotions and rejected companion candidates.
- Live validation still needed: confirm the next Highlands Cave or similar same-crop Engaged/Killed journal event logs `GoblinEvidenceJournalEngagedPromotedToKilledCompanion`, counts as `JournalKilledConfirmed`, and does not create stale duplicate notifications.

Validation commands:

```powershell
dotnet build .\GoblinFarmer.csproj
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet test .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --debug-package-replay ".\DebugPackages\GoblinFarmer_Debug_20260614_071706.zip"
git diff --check
```

Results:

- `dotnet build .\GoblinFarmer.csproj`: passed.
- `dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet test .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed, including `Goblin journal engaged candidate promotes same-crop killed companion`.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --debug-package-replay ".\DebugPackages\GoblinFarmer_Debug_20260614_071706.zip"`: passed with one replay log loaded and no failures.
- `git diff --check`: passed with LF-to-CRLF normalization warnings only.

## 2026-06-14 064418 Live Review Fix Pass

- Review inputs: `DebugPackages\GoblinFarmer_Debug_20260614_064418.zip` and `Video Clip Review\2026-06-14 06-38-57.mkv`.
- Issues covered: Cave of the Moon Clan Level 1 overlay `Go Next` changed from `N` to `Y` after leaving Southern Highlands, Northern Highlands Treasure Goblin Journal evidence was suppressed as an expired empty-bucket Cave Level 1 duplicate, and repair flow clicked the salvage tab even though no inventory items were salvageable.
- Fix coverage: overlay `Go Next` now prefers a fresh title-resolved area that differs from both accepted area and accepted route context; cross-area Journal duplicate continuity only extends beyond the stale visible-row window when non-empty line buckets match; repair flow now preflights salvage targets with the production classifier and skips opening the salvage tab when zero actionable non-gem targets are found.
- Replay note: `--debug-package-replay` loaded the attached package successfully with one replay log and no failures.
- Live validation still needed: confirm Cave Level 1 stays `Go Next: N`, a fresh Northern Highlands Treasure Goblin can count after an expired empty-bucket prior Journal signature, and empty-inventory town prep logs `salvageTabClickSkipped=True` without selecting the salvage tab.

Validation commands:

```powershell
dotnet build .\GoblinFarmer.csproj
dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false
dotnet test .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --debug-package-replay ".\DebugPackages\GoblinFarmer_Debug_20260614_064418.zip"
git diff --check
```

Results:

- `dotnet build .\GoblinFarmer.csproj`: passed.
- `dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet test .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed, including the new empty-bucket cross-area Journal expiry and repair-flow salvage preflight coverage.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --debug-package-replay ".\DebugPackages\GoblinFarmer_Debug_20260614_064418.zip"`: passed with one replay log loaded and no failures.
- `git diff --check`: passed with LF-to-CRLF normalization warnings only.

## 2026-06-13 175405 Overlay Review Fix Pass

- Review inputs: `DebugPackages\GoblinFarmer_Debug_20260613_175405.zip` and `Video Clip Review\2026-06-13 17-34-00.mkv`.
- Issue covered: after a Ruined Cistern Gilded Baron count, the overlay kept `Go Next: Y` after teleporting to plain Ancient Waterway even though Ancient Waterway is a blocked no-count/no-next current area.
- Fix coverage: overlay `Go Next` now allows confirmed current location to override fresh detected context when the confirmed area differs from the accepted area and is either outside the accepted route context or explicitly blocked. This keeps Southern Highlands -> Cave title-area handling intact while forcing plain Ancient Waterway transitions back to `N`.
- Live validation still needed: confirm the next Ruined Cistern -> Ancient Waterway transition flips `Go Next` to `N` on the next status refresh after the teleport/title update.

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
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed, including the updated overlay source-contract coverage for blocked confirmed-area override.
- `git diff --check`: passed with LF-to-CRLF normalization warnings only.

## 2026-06-13 164257 Live Review Fix Pass

- Review inputs: `DebugPackages\GoblinFarmer_Debug_20260613_164257.zip` and `Video Clip Review\2026-06-13 16-23-12.mkv`.
- Issues covered: Cave Level 1 overlay `Go Next` falling back to `Y` after title-area freshness expired, Cathedral Level 2 stale Blood Thief killed-row false count, PF1 first-goblin overlay `Go Next` showing `Y`, PF1 second Treasure Goblin same-signature Journal Engaged duplicate suppression, and New Tristram strong green set cells skipped as regular gems after bulk salvage.
- Fix coverage: overlay current-area override now persists for 90 seconds and stores accepted area count/limit; PF1/PF2 overlay stays `Go Next: N` until the second count slot is filled; killed Journal visible-row carryover has a bounded 75-second suppression window; PF duplicate bypass accepts sustained high-confidence combat Journal Engaged support up to 60 seconds only inside PF1/PF2 while capacity remains; salvage rejects strong set-green/weak-gem-body cells from both regular-gem and live gem-stack fallback classification.
- Live validation still needed: confirm the next PF1/PF2 run shows `Go Next: N` after goblin one and counts/notifies goblin two, and confirm a town salvage run no longer leaves the reviewed strong green set-item shape behind.

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
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed, including the new stale killed-row, PF duplicate, overlay, and salvage set-cell coverage.
- `git diff --check`: passed with LF-to-CRLF normalization warnings only.

## 2026-06-13 VS Debug In-Game Goblin Overlay

- Scope covered: passive VS Debug-only click-through Goblin overlay centered over Diablo.
- Fix coverage: overlay initializes only for VS Debug, hides when Diablo is missing/minimized, formats `Count/GPH/Goblin/Area/Go Next`, updates only after successful automatic counts, excludes `Sim Count`/debug simulation paths, recomputes `Go Next` from current confirmed location, and resets on Reset Stats/New Game.
- Follow-up coverage: `Video Clip Review\2026-06-13 13-20-42.mkv` showed a Cave of the Moon Clan Level 1 Treasure Goblin count where the accepted area was the Cave sub-area but the route-confirmed context was still Southern Highlands, so `Go Next` stayed `N`. The overlay now stores the route context at acceptance and treats that as valid for `Go Next=Y` until the confirmed route context changes; the overlay text is also custom-painted with larger colored sections, explicit section gaps, and `GPH` immediately to the right of `Count`.
- Live validation still needed: confirm the overlay is positioned correctly over the real Diablo window on the current dual-monitor layout and remains click-through during route/combat input.

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
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed, including `VS Debug in-game goblin overlay is passive and live-count only`.
- `git diff --check`: passed with LF-to-CRLF normalization warnings only.

## 2026-06-11 111859 Live Review Fix Pass

- Review inputs: `DebugPackages\GoblinFarmer_Debug_20260611_111859.zip` and `Video Clip Review\2026-06-11 10-39-48.mkv`.
- Issues covered: wrong first notification for a Highlands Cave Odious Collector, delayed Western Channel Level 1 and Battlefields notifications from accepted counts with `total=0`, and repair stop after a visually successful click.
- Fix coverage: Treasure/Odious minimap template/color disagreement is suppressed for Journal confirmation instead of reporting the wrong type, recent-minimap Journal reliability now continues through duplicate guard and count recording, and repair can soft-confirm a click when focused active pixels collapse.
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
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed with new checks for Treasure/Odious minimap disagreement suppression, recent-minimap Journal count recording, and repair soft confirmation.
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
- Issues covered: missed Goblin Tracker Journal counts after same-template stale visible rows, Event Complete menu not clearing automatically, inactive repair-button click, salvage/gem confusion around set/legendary leftovers, and auto-stash travel when no valid gems were present.
- Fix coverage: stale Journal visible-line fresh-area bypass, Enter-before-Escape event menu clear, repair-button active-color sampling, stack-text-gated gem-stash fallback, strong non-gem salvage quality boundary preservation, and no-gem auto-stash preflight skip.

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

# GoblinFarmer Codebase Health Audit

Date: 2026-06-14

Scope: Prompt 1 from the health refactoring optimization sequence. This audit is read-only by design: it identifies cleanup, refactoring, dead-code, and performance opportunities without changing production behavior, thresholds, routing, salvage, gem-stash, repair, combat, overlay, OBS, or debug package behavior.

## Current Evidence

- Largest tracked C# files by size: `frmMain.GoblinEvidence.cs` (~142 KB), `frmMain.PortedAutomation.cs.cs` (~111 KB), `frmMain.SessionStats.cs` (~98 KB), `frmMain.Town.cs` (~96 KB), `GoblinReplayFixtureRunner.cs` (~95 KB), `Form1.cs` (~94 KB), `GoblinEvidence.cs` (~93 KB), `AppSettings.cs` (~73 KB), `frmMain.Combat.cs` (~64 KB), `frmMain.SessionStats.AutoCount.cs` (~64 KB), and `frmMain.ImageRecognition.cs` (~57 KB).
- Largest scripts: `Scripts\create-debug-package.ps1` (~159 KB), `Scripts\debug-analysis-tools.ps1` (~57 KB), `Scripts\cleanup-project.ps1` (~17 KB), `Scripts\add-auto-count-review-notes.ps1` (~13 KB), and `Scripts\draft-auto-count-scenario.ps1` (~8 KB).
- Runtime replay policy is currently log-first: salvage and gem-stash scans call `SalvageInventoryReplayArtifacts.RecordScanLog` and `GemStashInventoryReplayArtifacts.RecordScanLog`; tests assert that live image scan artifact writers are not called.
- Debug packages include curated `ReviewEvidence` when selected by notes/video alignment and intentionally exclude full videos and old automatic `Debug\InventoryReplay` screenshot folders by default.
- OBS recording is local/passive from the app perspective. The tracked VS Debug form reads OBS process/log state but must not start or stop recording.
- Existing tests include guard checks for retired `Next Tests`, in-app ZIP creation, `LocalToolStartup`, and old package include behavior.

## Safe Cleanup

1. Sync stale Project Brain OBS wording.
   - Files/classes/methods: `Docs\ProjectBrain\Debug_And_Testing.md`, Live Test Workflow section.
   - Problem: it still says "VS Debug has no external recorder status group" even though `Docs\Project_Status.md`, `frmMain.ObsStatus.cs`, and tests confirm a passive OBS Status group now exists.
   - Suggested fix: update the sentence to say VS Debug has a passive OBS Status group that observes local OBS state only.
   - Prompt 2 status: completed as documentation-only cleanup.
   - Risk: low; documentation-only.
   - Validation: `git diff --check`; no runtime validation needed.

2. Sync maintenance script policy with current debug automation scripts.
   - Files/classes/methods: `Docs\Agent\Maintenance.md`, Scripts Folder Policy.
   - Problem: the policy names `create-debug-package.ps1` and `debug-analysis-tools.ps1`, but does not mention the tracked auto-count review helper and scenario drafting helper now used by the supported debug-review workflow.
   - Suggested fix: list `Scripts\add-auto-count-review-notes.ps1` and `Scripts\draft-auto-count-scenario.ps1` as tracked debug-review tooling, while keeping `Scripts\Local Tools\*` local-only.
   - Prompt 2 status: completed as documentation-only cleanup.
   - Risk: low; documentation-only.
   - Validation: `git diff --check`.

3. Clarify legacy Inventory Replay wording in maintenance docs.
   - Files/classes/methods: `Docs\Agent\Maintenance.md`, Retention Expectations.
   - Problem: it says debug packages include newest bounded salvage Inventory Replay folders, but current package policy excludes legacy `Debug\InventoryReplay` image folders and uses `Debug\ReplayLogs` plus curated `ReviewEvidence`.
   - Suggested fix: document legacy age cleanup separately from current package inclusion.
   - Prompt 2 status: completed as documentation-only cleanup.
   - Risk: low; documentation-only.
   - Validation: `git diff --check`.

4. Keep old-retirement tests but make their intent clearer when touched.
   - Files/classes/methods: `Tests\GoblinFarmer.Tests\Program.cs` tests around retired `Next Tests`, `LocalToolStartup`, and in-app ZIP package creation.
   - Problem: broad searches make these look like stale code references, but they are intentional guard assertions.
   - Suggested fix: if these tests are edited later, group comments can explain they are regression guards for removed flows.
   - Risk: low if comment-only; otherwise leave unchanged.
   - Validation: test harness.

## Refactor Candidates

1. Split Goblin Evidence scanner and count policy responsibilities.
   - Files/classes/methods: `frmMain.GoblinEvidence.cs`, `GoblinEvidence.cs`, `frmMain.SessionStats.AutoCount.cs`.
   - Problem: capture, evidence freshness, stale/duplicate suppression, auto-count acceptance, diagnostics, and candidate promotion are spread across very large partials. This increases regression risk when fixing stale Journal rows or Minimap/Journal cross-evidence cases.
   - Suggested fix: extract small services for evidence freshness state, suppression decisions, accepted-count recording, and candidate ranking/promotion wiring. Keep public behavior and logs stable during extraction.
   - Risk: high; stale/duplicate suppression and acceptance logic are core behavior.
   - Validation: unit tests for stale/blocked/duplicate/manual-sim paths, AutoCount Matrix, DecisionBundle replay, full harness, and live review.

2. Split town automation orchestration from classifiers.
   - Files/classes/methods: `frmMain.Town.cs`, `frmMain.ImageRecognition.cs`, `SalvageInventorySlotClassifier.cs`, gem-stash classifier code.
   - Problem: repair, salvage, gem-stash, shared inventory grid capture, UI travel, and post-action verification remain tightly coupled. This slows changes to gem auto-stash because salvage safety concerns are nearby.
   - Suggested fix: create narrow orchestration helpers for repair, salvage, and gem-stash while keeping the shared inventory grid capture and safe-click helpers centralized.
   - Risk: medium to high; town automation touches live click behavior.
   - Validation: build, full harness, salvage/gem replay logs, and live VS Debug town workflow.

3. Split replay runner responsibilities.
   - Files/classes/methods: `GoblinReplayFixtureRunner.cs`, tests around `--goblin-replay-*` and `--goblin-auto-count-matrix`.
   - Problem: replay scenario parsing, template synthesis, runner execution, CLI output, and result formatting are concentrated in one large file.
   - Suggested fix: split into scenario parser, frame/template builder, runner, and report formatter classes.
   - Risk: medium; replay is explicit but is now central to regression speed.
   - Validation: all replay commands plus AutoCount Matrix.

4. Modularize debug package scripts.
   - Files/classes/methods: `Scripts\create-debug-package.ps1`, `Scripts\debug-analysis-tools.ps1`.
   - Problem: package creation, retention, review-video alignment, auto-count triage, HTML summaries, and zip writing are in very large scripts.
   - Suggested fix: extract helper modules or internal functions by concern: package staging, retention, review evidence, auto-count triage, and report writing. Keep the batch launcher and script parameters stable.
   - Risk: medium; script packaging must work from installed/output folders and may run without `.git`.
   - Validation: script smoke tests, debug package tests, package size report, and manual batch execution.

5. Split AppSettings defaults and migration logic.
   - Files/classes/methods: `AppSettings.cs`.
   - Problem: defaults, VS Debug overrides, schema migration, sanitize/write, and summary logging live in one large class.
   - Suggested fix: split settings models, default seeding/migration, debug-profile overrides, and validation/clamping into separate internal helpers.
   - Risk: medium; settings persistence and VS Debug/release default separation are user-visible.
   - Validation: settings tests, release default tests, VS Debug default tests, and manual startup.

## Performance Opportunities

1. Index debug package analysis inputs once per triage run.
   - Files/classes/methods: `Scripts\debug-analysis-tools.ps1`, package auto-count triage functions.
   - Problem: debug package analysis and review-note triage scan logs, event files, DecisionBundles, crops, and `ReviewEvidence` repeatedly across report sections.
   - Suggested fix: build a package evidence index once and pass it through grouping/report functions.
   - Risk: low to medium; report ordering and missing-source behavior must stay stable.
   - Validation: script smoke tests with and without logs, crops, videos, and notes.

2. Cache review-note/video selections during package creation.
   - Files/classes/methods: `Scripts\create-debug-package.ps1`, `Get-AutoReviewEvidenceSelection`, `Add-ReviewEvidenceToPackage`.
   - Problem: newest video/log discovery, event parsing, and ffmpeg frame extraction are bounded but live in a large flow that is easy to accidentally repeat.
   - Suggested fix: keep a single immutable review context object for selected video, notes, timestamps, and derived frames.
   - Risk: low if behavior remains identical.
   - Validation: debug package tests and package summary output.

3. Reduce high-frequency duplicate/stale log noise with summaries.
   - Files/classes/methods: Goblin Tracker stale Journal rows, duplicate suppression, pending evidence diagnostics.
   - Problem: verbose event logs are valuable, but repeated live scanner suppressions increase log size and review noise.
   - Suggested fix: add bounded/throttled summary events only after coverage proves no loss of diagnostics.
   - Risk: medium; logs are the default replay/debug substrate.
   - Validation: package triage reports must still explain every reviewed count/miss.

4. Keep scanner image work bounded per tick.
   - Files/classes/methods: `frmMain.GoblinEvidence.cs`, `GoblinEvidence.cs`, image recognition helpers.
   - Problem: auto-count changes can accidentally add repeated crop/template passes per status tick.
   - Suggested fix: preserve one capture context per scan phase and share detected area/template results within that scan.
   - Risk: medium; scanner timing is live-only.
   - Validation: live OBS review plus logs with scan duration/phase data.

5. Split long-running test suites into targeted smoke commands.
   - Files/classes/methods: `Tests\GoblinFarmer.Tests\Program.cs`.
   - Problem: the full harness is valuable but slower than focused matrix checks during tight live-debug loops.
   - Suggested fix: add more explicit harness filters only after the current full-run behavior remains available.
   - Risk: low if additive.
   - Validation: full harness and each filter command.

## Risky Changes Requiring Tests First

1. Stale Journal row and accepted evidence lifetime rules.
   - Files/classes/methods: Goblin Evidence freshness/suppression code and `frmMain.SessionStats.AutoCount.cs`.
   - Why risky: recent live issues involved stale Journal rows causing false counts and area changes clearing or preserving observations incorrectly.
   - Required tests first: same-type cross-area stale Journal, delayed Journal evidence after combat, area-change reset, blocked-area suppression, duplicate suppression, and manual/debug simulation exclusion.

2. PF1/PF2 multi-goblin behavior and Go Next policy.
   - Files/classes/methods: route/location helpers, overlay Go Next helpers, Goblin Tracker count policy.
   - Why risky: one-goblin PF areas should not immediately mark Go Next as ready when a second spawn is guaranteed.
   - Required tests first: PF one-of-two accepted, PF second accepted, location-change reset, overlay display-only checks.

3. Salvage and gem auto-stash classifier changes.
   - Files/classes/methods: `SalvageInventorySlotClassifier.cs`, `frmMain.ImageRecognition.cs`, gem-stash classification.
   - Why risky: false positives can right-click or salvage valuable items; false negatives leave inventory clutter and break town flow.
   - Required tests first: unidentified/set/legendary/gem grids, tooltip-covered grids, replay logs, and live stash/salvage review.

4. Debug package or replay schema changes.
   - Files/classes/methods: `Scripts\create-debug-package.ps1`, `Scripts\debug-analysis-tools.ps1`, replay harness code.
   - Why risky: packages are the primary issue reproduction artifact, and replay must remain explicit/on-demand only.
   - Required tests first: package without videos, package with review evidence, missing ffmpeg, package replay from ZIP/folder, size bounds.

5. OBS workflow integration.
   - Files/classes/methods: `frmMain.ObsStatus.cs`, ignored `Scripts\Local Tools\*`, scheduled task setup outside tracked repo.
   - Why risky: tracked app code must remain passive and local-only recording should not be started/stopped by the app.
   - Required tests first: source guard tests plus local manual OBS validation.

## Dead/Stale Code Candidates

1. Stale Project Brain recorder sentence.
   - Files/classes/methods: `Docs\ProjectBrain\Debug_And_Testing.md`.
   - Why candidate: current code/tests/docs have superseded it with a passive OBS Status group.
   - Suggested fix: update docs only.
   - Risk: low.
   - Validation: docs diff review.

2. Legacy Inventory Replay package wording.
   - Files/classes/methods: `Docs\Agent\Maintenance.md`, possibly older docs under `Docs\Project_Status.md` historical sections.
   - Why candidate: current package policy excludes legacy automatic screenshot replay folders while retaining cleanup for old folders.
   - Suggested fix: update active manuals; keep historical notes unless misleading in current guidance.
   - Risk: low.
   - Validation: docs diff review.

3. Legacy `Debug\InventoryReplay` cleanup support.
   - Files/classes/methods: `DebugManager.cs`, `SalvageInventoryReplayArtifacts.cs`, `GemStashInventoryReplayArtifacts.cs`.
   - Why candidate: runtime no longer creates automatic screenshot replay folders.
   - Suggested fix: do not remove yet. Cleanup and explicit harness compatibility are intentional until older artifacts age out and tests are revised.
   - Risk: medium if removed too early.
   - Validation: inventory replay tests and debug cleanup tests.

4. Retired UI flow references in tests.
   - Files/classes/methods: `Tests\GoblinFarmer.Tests\Program.cs` assertions for `Next Tests`, `Review Files`, `LocalToolStartup`, and duplicate in-app package creation.
   - Why candidate: search hits look stale, but these are guard tests.
   - Suggested fix: keep.
   - Risk: high if removed without replacement because old UI/package paths could regress.
   - Validation: full harness.

5. OBS local-tool startup leftovers.
   - Files/classes/methods: `Program.cs`, `LocalToolStartup.cs`, `frmMain.ObsStatus.cs`, tests around `LocalToolStartup`.
   - Why candidate: tests confirm tracked app startup no longer launches local recorder tooling and `LocalToolStartup.cs` should be absent.
   - Suggested fix: no tracked code removal currently identified. Keep passive status panel.
   - Risk: medium if app starts controlling OBS again.
   - Validation: source guard tests.

## Prompt 3 Dead-Code Review

Prompt 3 reviewed the requested removal areas after Prompt 2 cleanup:

- OBS/startup leftovers: no tracked `LocalToolStartup.cs` exists, and the remaining `LocalToolStartup`, `StartRecord`, and `StopRecord` references are guard tests that prove the app does not launch or control OBS.
- Duplicate debug-package writers: no in-app `PortCreateDebugPackage` or `ReviewDebugPackageComplete` implementation remains; references are guard tests and docs.
- Retired `Next Tests` and `Review Files` flows: remaining references are guard tests and history/docs, not production UI.
- Obsolete replay/debug paths: `Debug\InventoryReplay` remains in legacy cleanup and explicit harness compatibility. Runtime image scan paths use structured `Debug\ReplayLogs`; package tests verify legacy screenshot folders are excluded. Removal is deferred until compatibility is intentionally retired.
- Image-recognition best-sample helpers: `ImageRecognitionBestSamplePromoter` is active and wired only to Goblin Evidence and Gem Auto-Stash. Salvage has a guard test proving it is not wired into promotion.
- Duplicate goblin evidence/area suppression helpers: searches show heavily tested active suppression, stale-evidence, duplicate-guard, PF multi-count, and blocked-area policies. No duplicate helper was confirmed safe to remove.

No production code was removed for Prompt 3 because no candidate met the "confirmed dead with compiler/tests proving removal is safe" bar. This preserves package compatibility and avoids changing auto-count, replay, salvage, gem-stash, town, OBS, or route behavior.

## Coverage Gaps

- Scanner timing, Diablo focus/input, missing frames, notification rendering, OBS behavior, and overlay click-through positioning remain live-only unless a debug package contains replay-ready DecisionBundles or curated `ReviewEvidence`.
- AutoCount Matrix scenarios should continue to grow from real reviewed misses, but generated drafts under `Debug\AutoCountScenarioDrafts` should stay ignored until intentionally promoted.
- Gem auto-stash live validation still needs real Diablo confirmation that promoted/loaded gem templates do not classify gear as gems and that right-click transfer succeeds on the focused stash tab.
- Salvage live validation still needs confirmation that log-first replay diagnostics cover tooltip-covered inventory, pale items, gem stacks, and expected confirmation prompts without reintroducing screenshot artifact growth.

## Validation Results

Prompt 1 validation completed on 2026-06-14:

- `dotnet build GoblinFarmer.csproj`: passed, 0 warnings, 0 errors.
- `dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false`: passed, 0 warnings, 0 errors.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed, all tests passed.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --goblin-auto-count-matrix`: passed, 9 scenarios, 21 steps, 0 failed.
- `git diff --check`: passed with Git LF-to-CRLF working-copy warnings only.

Prompt 2 validation after safe documentation cleanup also completed on 2026-06-14:

- `dotnet build GoblinFarmer.csproj`: passed, 0 warnings, 0 errors.
- `dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false`: passed, 0 warnings, 0 errors.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed, all tests passed.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --goblin-auto-count-matrix`: passed, 9 scenarios, 21 steps, 0 failed.
- `git diff --check`: passed with Git LF-to-CRLF working-copy warnings only.

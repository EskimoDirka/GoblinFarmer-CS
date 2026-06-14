# GoblinFarmer Performance Audit

Date: 2026-06-14

Scope: Prompt 5 from the health refactoring optimization sequence. This audit identifies performance opportunities only. It does not change production behavior, thresholds, routing, combat, repair, salvage, gem-stash, Kadala, make-new-game, replay execution, OBS behavior, or debug package output.

## Summary

The highest-value performance improvements are in tooling, not live automation:

1. Build a reusable evidence index for debug package triage and review-note tooling.
2. Avoid repeated file scans in PowerShell package/report functions.
3. Keep replay/matrix commands fast and explicit.
4. Treat runtime scanner/log changes as higher risk because logs and timing are part of current debugging reliability.

## Low-Risk Opportunities

### 1. Single Evidence Index For Debug Analysis

- Files: `Scripts\debug-analysis-tools.ps1`, `Scripts\add-auto-count-review-notes.ps1`, `Scripts\create-debug-package.ps1`.
- Current shape: debug analysis functions discover logs, `GoblinTrackerEvents`, DecisionBundles, crops, latency traces, and `ReviewEvidence` independently across report sections.
- Opportunity: construct one immutable package evidence index per package root or extracted package folder, then pass it to grouping/report functions.
- Expected benefit: faster package triage and fewer repeated disk traversals, especially on large debug packages.
- Risk: low if report ordering and missing-source reporting remain unchanged.
- Validation:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Scripts\add-auto-count-review-notes.ps1 -ReviewTimestamp "00:00:01=performance smoke"
dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
```

### 2. Reuse Review Context During Package Creation

- Files: `Scripts\create-debug-package.ps1`.
- Current shape: video selection, log event selection, timestamp selection, notes handling, and frame extraction are bounded but spread across a long script flow.
- Opportunity: create a single review context object containing selected video, source notes, timestamps, derived log events, and frame extraction decisions.
- Expected benefit: less repeated parsing and easier package summary validation.
- Risk: low to medium; script parameter compatibility and missing-ffmpeg behavior must stay stable.
- Validation: debug package tests, smoke package creation with and without review video/notes.

### 3. Keep AutoCount Matrix Fast And Focused

- Files: `Tests\GoblinFarmer.Tests\Program.cs`, `Tests\Fixtures\GoblinReplayScenarios\AutoCountMatrix`.
- Current shape: matrix run is already fast and explicit; latest run passed 9 scenarios and 21 steps in seconds.
- Opportunity: continue adding high-value policy scenarios here before broad live scanner changes.
- Expected benefit: faster feedback for stale/duplicate policy regressions.
- Risk: low if matrix remains explicit and does not replace full harness validation.
- Validation: `--goblin-auto-count-matrix`.

### 4. Avoid Repeated Template Discovery In Tooling

- Files: replay harness and debug analysis code that resolves `Images\Goblin Evidence` and `Images\Current Location`.
- Current shape: replay commands resolve templates per run; this is acceptable but can become noisy as scenarios grow.
- Opportunity: cache template catalogs inside a single replay process.
- Expected benefit: faster multi-scenario replay.
- Risk: low if cache lifetime is per command and file changes are not expected during the run.
- Validation: replay harness and matrix output.

## Medium-Risk Opportunities

### 1. Runtime Scanner Work Sharing

- Files: `frmMain.GoblinEvidence.cs`, `GoblinEvidence.cs`.
- Current shape: live scanner logic balances Journal, Minimap, current-location, stale suppression, and evidence capture. Recent fixes intentionally add checks and diagnostics.
- Opportunity: ensure each scanner tick has one capture context and shared area/template results rather than repeating image work across candidate paths.
- Expected benefit: reduced CPU and less timing jitter during combat.
- Risk: medium; timing and frame availability are live-only failure modes.
- Validation: full harness, matrix, live OBS review, and package timing logs.

### 2. Throttled Duplicate/Stale Diagnostic Summaries

- Files: Goblin Tracker event logging and debug package triage.
- Current shape: repeated suppressions are useful for diagnosis but can grow logs.
- Opportunity: keep first/important events verbatim, then summarize repeated identical suppressions over short windows.
- Expected benefit: smaller packages and clearer reports.
- Risk: medium to high; logs are the default replay/debug substrate.
- Validation: triage report must still explain reviewed notes and stale/duplicate decisions.

### 3. Package Size Preflight

- Files: `Scripts\create-debug-package.ps1`, `Scripts\debug-analysis-tools.ps1`.
- Current shape: package creation reports size after building and applies individual include bounds.
- Opportunity: estimate package size before adding optional evidence groups and log skip decisions earlier.
- Expected benefit: fewer oversized packages and clearer package summaries.
- Risk: medium; must not hide requested review evidence.
- Validation: package size tests and manual smoke package.

### 4. Test Harness Filters

- Files: `Tests\GoblinFarmer.Tests\Program.cs`.
- Current shape: the full harness is a monolithic command, with specific commands for matrix and replay.
- Opportunity: add more explicit filters for package-only, town-only, salvage/gem-only, and settings-only smoke suites.
- Expected benefit: faster local iteration before running the full suite.
- Risk: medium; filtered tests must not become a substitute for full validation before delivery.
- Validation: full harness plus each filter.

## High-Risk / Defer Until Covered

### 1. Changing Auto-Count Freshness Windows

- Reason to defer: stale Journal, delayed Engaged/Killed rows, Minimap anchors, and PF area limits are currently the most sensitive production logic.
- Required first: matrix scenarios and DecisionBundle replay for each new live failure shape.

### 2. Changing Salvage/Gem Classifier Thresholds

- Reason to defer: false positives can salvage or stash the wrong item.
- Required first: curated replay crops, synthetic fixture coverage, and live validation.

### 3. Moving Town Click/Wait Timing

- Reason to defer: repair, salvage, and stash success depend on Diablo UI timing and focus.
- Required first: live VS Debug validation with OBS and logs.

### 4. Removing Legacy Replay Compatibility

- Reason to defer: explicit `--inventory-replay` and cleanup compatibility still have tests and value for older artifacts.
- Required first: a separate compatibility retirement decision and migration note.

## Recommended Prompt 6 Candidate

The best low-risk implementation candidate is a script-only helper that builds and reuses an evidence file index in `Scripts\debug-analysis-tools.ps1`. It should be additive and internal to debug package analysis:

- no runtime app changes
- no replay behavior changes
- no package format changes
- no threshold or scanner changes
- bounded to local/report tooling

Validation should include full harness and a smoke run of `Scripts\add-auto-count-review-notes.ps1` if a current package/video exists.

## Prompt 6 Implementation

Implemented on 2026-06-14:

- Added `New-DgaEvidenceIndex` to `Scripts\debug-analysis-tools.ps1`.
- Added `Test-DgaPathUnder` for safe internal package-root path grouping.
- Reused the evidence index in `New-DgaAutoCountTriageData`.
- Reused the evidence index in `New-DgaAutoCountReviewNotesTriageData`.
- Updated `Get-DgaEvidenceReferencesForReviewNote` to accept an optional index while preserving its existing call shape.

Behavior boundaries:

- Runtime app behavior is unchanged.
- Debug package ZIP format is unchanged.
- Replay remains explicit/on-demand only.
- Full videos and bulk image folders remain excluded by package policy.
- Missing-source reporting remains present in review-note triage.

Initial smoke results:

- Dot-sourced `Scripts\debug-analysis-tools.ps1` and created an evidence index for the workspace successfully.
- `Scripts\add-auto-count-review-notes.ps1 -ReviewTimestamp "00:00:01=performance index validation smoke"` completed successfully against the newest standard debug package/video, wrote `AutoCountReviewNotes` Markdown/JSON, and created an ignored draft scenario folder.

Final validation:

- `dotnet build GoblinFarmer.csproj`: passed.
- `dotnet build .\GoblinFarmer.csproj -p:UseSharedCompilation=false`: passed.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false`: passed, all tests passed.
- `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --goblin-auto-count-matrix`: passed, 9 scenarios, 21 steps, 0 failed.
- `git diff --check`: passed with Git LF-to-CRLF working-copy warnings only.

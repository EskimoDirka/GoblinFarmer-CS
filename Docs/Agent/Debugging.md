# Agent Debugging Manual

Stable debugging guidance for Codex. Latest package findings and active issues live in `Docs/Project_Status.md`; that file wins on conflict.

## Debug Package Contents

Use `Scripts\Create Debug Package.bat` to create the supported review ZIP. Useful package roots include:

- `debug-package-analysis.txt`
- `goblin-tracker-timeline.md`
- `goblin-evidence-health.txt`
- `debug-package-manifest.txt`
- `goblin-tracker-summary.txt`
- `goblin-tracker-review.html`
- `goblin-auto-count-triage.md` and `goblin-auto-count-triage.json`
- latest logs, route summaries, session metadata, screenshots, and structured JSONL events
- `Debug\ReplayLogs\*.jsonl` when inventory replay metadata was recorded
- `ReviewEvidence\manifest.json`, `ReviewEvidence\issue.md`, and selected reviewed video frames/crops when review evidence parameters were supplied

Do not create alternate debug package generators without explicit approval.

Reviewed video evidence should be folded into the normal debug package, not a separate package format. By default, `Scripts\Create Debug Package.bat` attempts to find the newest OBS/attached recording under `Video Clip Review`, align high-value log events to video offsets, and extract selected frames into `ReviewEvidence`. Pass selected timestamps and notes to `Scripts\create-debug-package.ps1` using `-ReviewVideoPath`, `-ReviewTimestamp`, `-ReviewNotesPath`, `-ReviewNotesUrl`, and optional `-ReviewEvidenceFolder` when the automatic alignment misses something. Full video files remain outside ZIPs by default. The default live-test notes source is the shared Google Sheet: `https://docs.google.com/spreadsheets/d/1y4qj2gwqmtMxiG3t-epvyjaPoW7TM74bOcbeFyGn9kw/edit?gid=0#gid=0`.

After a video review, use `Scripts\add-auto-count-review-notes.ps1` with only the review notes/timestamps during normal work. If `-DebugPackagePath` or `-ReviewVideoPath` is omitted, the helper searches the standard repo-local `DebugPackages\` and `Video Clip Review\` folders and chooses the newest matching ZIP/video. Example:

```powershell
.\Scripts\add-auto-count-review-notes.ps1 -ReviewTimestamp "00:07:11=Cathedral Level 2 stale Blood Thief false count" -ReviewTimestamp "00:15:32=PF1 first goblin Go Next should stay N"
```

The helper writes a new `*_AutoCountNotes.zip` by default and adds `AutoCountReviewNotes\auto-count-review-notes.md`, `AutoCountReviewNotes\auto-count-review-notes.json`, and `AutoCountReviewNotes\validation.txt`. It also automatically creates a review-only AutoCount Matrix draft under `Debug\AutoCountScenarioDrafts` from the same package and notes; use `-SkipScenarioDraft` only for unusual cases where no draft is wanted. It groups accepted counts, suppressed candidates, pending evidence, stale Journal rows, suppression reasons, DecisionBundle/GoblinTrackerEvents references, Journal/Minimap crop references, notification traces, and likely review targets. It references existing package evidence only; full videos and bulk image folders remain excluded.

For the `debug review` shortcut phrase, follow `Docs\Agent\Debug_Review_Automation.md`. That workflow reads the Google Doc `Video Review`, parses `Area` / `Timestamp` / `Note` blocks, and runs the helper with only parsed notes so the newest debug package and video are selected automatically.

The VS Debug OBS Status group is passive. It may show the latest local OBS monitor state plus `Start` and `Stop` times from `Video Clip Review\Auto Record Diablo.log` and the `obs64` process, but it must not launch OBS or start/stop recording.

The VS Debug in-game Goblin overlay is also passive. It is a click-through, no-activate topmost window over Diablo that displays accepted automatic-count state only; it must not send input, change routing, alter count acceptance, or update from `Sim Count`/debug simulation paths.

## Useful Logs And Evidence

- Human-readable logs show route, timing, hotkey, combat, town, and Goblin Tracker decisions.
- `Debug\GoblinEvidence\GoblinTrackerEvents.jsonl` records structured scanner/count events when enabled.
- Route context fields help distinguish raw/current/display/blocking area confusion.
- Latency diagnostics use `GoblinLatencyTrace` stages such as evidence detected, count accepted, notification queued, and notification displayed.

## GoblinEvidence Captures

- EncounterCaptures and DecisionBundles should carry replay-ready Journal/Minimap crops and metadata.
- Normal scanner events should avoid redundant root fullscreen/event images by default.
- The VS Debug `Capture` button is supplemental and should be used only when visible image-recognition evidence is needed.

## DecisionBundles

- DecisionBundles are the preferred evidence for count-policy review.
- New bundles should be replay-ready when Journal/Minimap crops and metadata are available.
- Older bundles with only fullscreen evidence may be manual-review evidence but not crop-replayable.

## Replay Tooling

- Goblin Replay is explicit/on-demand only.
- Use focused harness inputs: metadata file, capture prefix, capture folder, DecisionBundle folder, or a small scenario file.
- Package replay is explicit/on-demand only through the test harness `--debug-package-replay` command. It loads structured replay logs and curated `ReviewEvidence` images from a debug package ZIP/folder without clicking or pressing keys.
- Inventory Replay classifier loaders remain available for curated image evidence, but live Debug Mode inventory scans should record structured JSONL under `Debug\ReplayLogs` instead of writing automatic replay screenshot folders.
- Replay must not run during startup, VS Debug startup, scanner execution, route workflows, package creation, form close, or automated live testing.
- Log-only replay validates recorded policy/workflow decisions. Image-recognition replay requires curated frames/crops from `ReviewEvidence`; logs alone cannot validate pixel classifier changes. Replay still does not validate real-time scanner timing, sound playback, mouse click-through, or Diablo focus behavior.
- Auto-count triage reports are generated during debug package analysis. They group accepted counts, suppressions, pending evidence, stale Journal rows, duplicate/area-limit/blocked-area suppressions, area-resolution changes, and latency traces from whichever package sources exist.
- Note-scoped auto-count triage reports and review-only matrix drafts are added explicitly after review with `Scripts\add-auto-count-review-notes.ps1`; this helper must not run from startup, scanner workflows, route automation, package creation, form close, or live testing.
- Use `Scripts\draft-auto-count-scenario.ps1 -DebugPackagePath <zip-or-folder> -Timestamp <hh:mm:ss> -ReviewNotes <text>` to create a review-only scenario draft under `Debug\AutoCountScenarioDrafts`. The draft helper must not modify production policy or committed test fixtures; promote scenarios manually only after the assertion is understood.
- Use the explicit test-harness command `dotnet run --project .\Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false -- --goblin-auto-count-matrix` to run the curated AutoCount Matrix. It is policy replay only and must not be invoked by startup, scanners, route workflows, package creation, or VS Debug form startup.

## Salvage Diagnostics

- Salvage inventory targets must be one column wide and one or two rows tall. Any `footprintRows` value above 2 is a classifier bug.
- Weak upper cells attached to lower green/set pixels should remain one two-row target unless the upper cell has its own convincing item boundary. If Inventory Replay shows lower-row set targets after the upper cell was clicked successfully, suspect stale cache splitting before changing confirmation timing.
- Normal gems must stay `RegularGemNonSalvageable`, including stack-count gem cells, tooltip-covered live gem-stack clusters, and amber topaz/ruby-like bodies with no item top boundary. Stack-count evidence alone is not enough for lower-half gear; keep the top-boundary and cluster-support checks so lower halves of set/legendary items do not become gem skips. If Diablo shows `You cannot salvage this item` or final salvage fails with only gems visible, replay the saved crop and inspect `regularGemPixels`, `stackCountTextPixels`, amber/orange metrics, supported gem-stack clustering, and whether a gem-like strong cell anchored an adjacent weak footprint before loosening item anchors.
- Gem stashing accepts exact template matches first, then `GemColorMatched` fallback for strong normal-gem color cells when valid gem templates are loaded. If stash opens but no gems move, compare `bestTemplate`, `confidence`, threshold, and whether candidates are below threshold despite obvious gem color in the replay crop.
- Salvage scans should move the cursor away from the inventory before capture. If a replay crop contains tooltip text/stat lines over the grid, treat false left-column targets as hover contamination before loosening classifier thresholds.
- Missing confirmation for a set/legendary target should verify the cached target with a fresh classifier scan before failing. `StaleCachedTargetSkippedAfterRescan` means the target was already gone and the final leftover/recovery scan should decide completion.
- `PostSalvageLeftoverWarning` is still diagnostic and non-clicking. Accepted non-gem targets found by the final scan can trigger bounded recovery rescans from fresh classifier output; rejected or unknown warning candidates must not be clicked.
- In Debug Mode, salvage and gem-stash scans should record structured replay metadata under `Debug\ReplayLogs` with `SalvageInventoryReplayLogRecorded` or `GemStashInventoryReplayLogRecorded`. Automatic `inventory-grid.png` replay folders should not be created by normal runtime scans.

## Notification Latency

- Separate detection-to-count, count-to-notification, and detection-to-display timing.
- A delayed notification may actually be delayed candidate acceptance, especially when Journal Engaged waits for Killed/Minimap/sustained confirmation.
- Compare Minimap confidence, Journal state, candidate selection logs, and `GoblinLatencyTrace` stages before changing UI notification code.

## Stale Journal Suppression

- Old visible Journal rows can follow route transitions and should not recount in new areas.
- Reason about stale suppression using row bucket, template family, goblin type, area resolution, current route context, and reset/new-game timing.
- First-seen Journal Engaged evidence is diagnostic/pending unless policy confirms it through Minimap, Killed evidence, or sustained active-combat confirmation.
- Do not loosen stale policy from a single ambiguous package; prefer replayable evidence or a repeated live package shape.

## PF Multi-Goblin Duplicate Review

- PF1/PF2 allow two counts per level, but exact evidence signatures can repeat for separate real same-type goblins.
- When reviewing PF duplicate suppressions, compare area count/limit, elapsed time since the last accepted PF count, source, confidence, and whether the current evidence is fresh Minimap or sustained current-area Journal Engaged.
- `PfMultiCountDuplicateBypass` means the scoped PF duplicate exception allowed a delayed second same-signature count while a PF slot remained.
- `PfMultiCountDuplicateBypassSkipped` should explain why a PF-shaped duplicate stayed suppressed, such as elapsed-too-short, area-limit reached, previous accepted area mismatch, or missing fresh supporting evidence.
- Do not generalize this bypass to normal one-count areas, stale previous-area Journal rows, immediate same-goblin duplicates, or third PF counts.

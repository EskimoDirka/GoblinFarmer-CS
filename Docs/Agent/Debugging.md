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
- latest logs, route summaries, session metadata, screenshots, and structured JSONL events

Do not create alternate debug package generators without explicit approval.

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
- Inventory Replay for salvage is explicit/on-demand only through the test harness `--inventory-replay` command. It loads saved `Debug\InventoryReplay\Salvage\SalvageInventoryReplay_*\metadata.json` artifacts and reruns the production salvage classifier without clicking or pressing keys.
- Replay must not run during startup, VS Debug startup, scanner execution, route workflows, package creation, form close, or automated live testing.
- Replay validates policy and saved-frame recognition, not real-time scanner timing, sound playback, mouse click-through, or Diablo focus behavior.

## Salvage Diagnostics

- Salvage inventory targets must be one column wide and one or two rows tall. Any `footprintRows` value above 2 is a classifier bug.
- Weak upper cells attached to lower green/set pixels should remain one two-row target unless the upper cell has its own convincing item boundary. If Inventory Replay shows lower-row set targets after the upper cell was clicked successfully, suspect stale cache splitting before changing confirmation timing.
- Normal gems must stay `RegularGemNonSalvageable`, including stack-count gem cells and amber topaz/ruby-like bodies with no item top boundary. If Diablo shows `You cannot salvage this item` and replay points at left-column gem stacks, inspect `regularGemPixels`, `stackCountTextPixels`, amber/orange metrics, and whether a gem-like strong cell anchored an adjacent weak footprint before loosening item anchors.
- Salvage scans should move the cursor away from the inventory before capture. If a replay crop contains tooltip text/stat lines over the grid, treat false left-column targets as hover contamination before loosening classifier thresholds.
- Missing confirmation for a set/legendary target should verify the cached target with a fresh classifier scan before failing. `StaleCachedTargetSkippedAfterRescan` means the target was already gone and the final leftover/recovery scan should decide completion.
- `PostSalvageLeftoverWarning` is still diagnostic and non-clicking. Accepted non-gem targets found by the final scan can trigger bounded recovery rescans from fresh classifier output; rejected or unknown warning candidates must not be clicked.
- In Debug Mode, salvage scans should save inventory replay crops and metadata under `Debug\InventoryReplay\Salvage` with `SalvageInventoryReplayArtifactSaved`.

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

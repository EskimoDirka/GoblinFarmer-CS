# Debug Review Automation

Stable procedure for the user shortcut phrase `debug review`.

## Trigger

When the user says `debug review`, treat that as a request to perform the full GoblinFarmer video/debug-package review workflow without asking for package or video paths.

## Inputs

- Review notes Google Doc: `https://docs.google.com/document/d/1rLdwXr1UTlVwfBxcAlEs1rGzsTE8pDCxpuMh_evEzbo/edit?tab=t.0`
- Debug packages folder: `D:\D3\Projects\GoblinFarmer\GoblinFarmer\GoblinFarmer\DebugPackages`
- Video clips folder: `D:\D3\Projects\GoblinFarmer\GoblinFarmer\GoblinFarmer\Video Clip Review`
- Repo helper: `Scripts\add-auto-count-review-notes.ps1`

## Required Flow

1. Read `AGENTS.md`, `Docs\Project_Status.md`, `Docs\Agent\Invariants.md`, `Docs\Agent\Debugging.md`, and this file.
2. Use the Google Drive connector to read the review notes Google Doc.
3. Parse notes as repeated blocks:

```text
Area: <area>
Timestamp: <video timestamp>
Note: <review note>
```

4. Ignore blank blocks. Preserve area text by appending it to the note text passed to the helper.
5. Run `Scripts\add-auto-count-review-notes.ps1` with notes only. Do not pass package or video paths during normal use; let the helper select the newest matching debug package and video clip.
6. Confirm the helper created:
   - `*_AutoCountNotes.zip`
   - `AutoCountReviewNotes\auto-count-review-notes.md`
   - `AutoCountReviewNotes\auto-count-review-notes.json`
   - `AutoCountReviewNotes\validation.txt`
   - a review-only draft under `Debug\AutoCountScenarioDrafts`
7. Review the generated Markdown/JSON and matrix draft, then summarize likely fixes, likely tests, and any missing evidence.

## Command Shape

Use one `-ReviewTimestamp` argument per parsed note:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Scripts\add-auto-count-review-notes.ps1 `
  -ReviewTimestamp "00:07:11=Area: Cathedral Level 2 | Note: stale Blood Thief false count" `
  -ReviewTimestamp "00:15:32=Area: PF1 | Note: first goblin Go Next should stay N"
```

## Boundaries

- Do not edit live runtime behavior as part of the automatic review step.
- Do not promote draft matrix files into committed fixtures unless the user explicitly asks.
- Do not include full videos in debug packages.
- Do not require the user to attach package ZIPs or video clips if the standard folders contain current files.
- If the Google Doc has no filled notes, stop and say the review notes document is empty.
- If the helper reports missing sources, summarize the missing evidence and continue with whatever report data exists.

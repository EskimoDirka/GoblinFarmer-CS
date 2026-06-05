# Goblin Evidence Templates

Automation Observation Mode discovers calibrated PNG templates from this folder.

Supported names:

- `<Goblin Type> Engaged Journal.png`
- `<Goblin Type> Killed Journal.png`
- `<Goblin Type> Engaged & Killed Journal.png`
- `<Goblin Type> Minimap.png`

Examples:

- `Menagerist Goblin Engaged Journal.png`
- `Blood Thief Engaged & Killed Journal.png`
- `Gilded Baron Minimap.png`

Journal templates use the calibrated `64,736,645,417` reference region. Minimap templates use the calibrated `2108,66,421,423` reference region. These regions are for Goblin Evidence Observation Mode only and do not change route or location-title detection.

Use Ctrl+Shift+G calibration captures or `Debug\GoblinEvidence\ObservationDiagnostics` crops as reference material only. Do not auto-create templates from arbitrary crops; manually crop and validate them before relying on observation candidate detection.

Observation Mode is diagnostic only. It reports what would count, including goblin type when a template matches, but it does not increment `GoblinCount`, change GPH, or consume area-count slots.

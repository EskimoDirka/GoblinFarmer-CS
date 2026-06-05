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

Journal templates use the calibrated `64,736,645,417` reference region. Minimap templates should be tight crops of only the goblin minimap icon/dot; the scanner searches those small templates inside the calibrated `2108,66,421,423` minimap reference region. These regions are for Goblin Evidence Observation Mode only and do not change route or location-title detection.

When a template is evaluated, the logs include the template name, goblin type, source (`Journal` or `Minimap`), scan region, best confidence, threshold, match point, screen match point, and template size. Journal evidence is primary when both Journal and Minimap match in the same scan; Minimap evidence is supporting goblin-presence evidence unless no Journal match is available.

Use Ctrl+Shift+G calibration captures or `Debug\GoblinEvidence\ObservationDiagnostics` crops as reference material only. Do not auto-create templates from arbitrary crops; manually crop and validate them before relying on observation candidate detection.

Observation Mode is diagnostic only. It reports what would count, including goblin type when a template matches, but it does not increment `GoblinCount`, change GPH, or consume area-count slots.

If the tight minimap icon templates prove unreliable under live conditions after one or two targeted tuning passes, the next planned fallback is color matching for the goblin minimap marker within the calibrated minimap region.

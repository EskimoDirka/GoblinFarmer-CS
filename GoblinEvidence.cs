namespace GoblinFarmer
{
    internal enum GoblinEvidenceType
    {
        Unknown,
        JournalEncounter,
        JournalKill,
        MinimapIcon,
    }

    internal sealed record GoblinEvidenceEvent(
        DateTime Timestamp,
        GoblinEvidenceType Type,
        double Confidence,
        string Source,
        string ScreenshotPath,
        string Notes);

    internal sealed record GoblinEvidenceCandidate(
        GoblinEvidenceType Type,
        double Confidence,
        string Source,
        string Notes);
}

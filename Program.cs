namespace GoblinFarmer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            AppSettings.Load();
            AppLogger.CleanupOldLogs(AppSettings.RetentionDays);
            DebugManager.CleanupDebugArtifactsByAge(AppSettings.DebugArtifactRetentionAge);
            DebugManager.CleanupOldSessionSummaries(AppSettings.Debug.SessionSummaryRetentionCount);
            DebugManager.CleanupOldDebugPackages(AppSettings.Debug.DebugPackageRetentionCount);
            DebugManager.CleanupOldGoblinEvidence(AppSettings.Debug.GoblinEvidenceRetentionCount);
            DebugManager.CleanupOldImageRecognitionBestSampleSets(
                DebugManager.GoblinEvidenceAcceptedCandidatesDirectory,
                AppSettings.ImageRecognition.TopCandidateRetentionCount,
                "GoblinEvidence");
            DebugManager.CleanupOldImageRecognitionBestSampleSets(
                DebugManager.GemAutoStashAcceptedCandidatesDirectory,
                AppSettings.ImageRecognition.TopCandidateRetentionCount,
                "GemAutoStash");
            LocalToolStartup.StartVsDebugDiabloAutoRecordMonitor();

            Application.Run(new frmMain());
        }
    }
}

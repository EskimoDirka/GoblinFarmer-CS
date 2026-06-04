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
            DebugManager.CleanupOldSessionSummaries(AppSettings.Debug.SessionSummaryRetentionCount);
            DebugManager.CleanupOldDebugPackages(AppSettings.Debug.DebugPackageRetentionCount);
            Application.Run(new frmMain());
        }
    }
}

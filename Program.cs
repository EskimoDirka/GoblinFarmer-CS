namespace GoblinFarmer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            AppSettings.Load();
            if (TryHandleCommandLine(args, out int exitCode))
            {
                return exitCode;
            }

            AppLogger.CleanupOldLogs(AppSettings.RetentionDays);
            DebugManager.CleanupDebugArtifactsByAge(AppSettings.DebugArtifactRetentionAge);
            DebugManager.CleanupOldSessionSummaries(AppSettings.Debug.SessionSummaryRetentionCount);
            DebugManager.CleanupOldDebugPackages(AppSettings.Debug.DebugPackageRetentionCount);
            DebugManager.CleanupOldGoblinEvidence(AppSettings.Debug.GoblinEvidenceRetentionCount);

            Application.Run(new frmMain());
            return 0;
        }

        private static bool TryHandleCommandLine(string[] args, out int exitCode)
        {
            exitCode = 0;
            if (args.Length == 0)
            {
                return false;
            }

            bool runGoblinReplay = args.Any(arg =>
                arg.Equals("--goblin-replay", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("--run-goblin-replay", StringComparison.OrdinalIgnoreCase));
            bool showHelp = args.Any(arg =>
                arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("/?", StringComparison.OrdinalIgnoreCase));

            if (!runGoblinReplay)
            {
                if (showHelp)
                {
                    Console.WriteLine("GoblinFarmer command line:");
                    Console.WriteLine("  --goblin-replay [--input <folder-or-zip>]");
                    exitCode = 0;
                    return true;
                }

                return false;
            }

            string inputPath = ReadArgumentValue(args, "--input") ??
                ReadArgumentValue(args, "--goblin-replay-input") ??
                DebugManager.GoblinEvidenceDirectory;
            try
            {
                Console.WriteLine($"GoblinReplayCliStarted: inputPath={inputPath}");
                using frmMain replayHost = new();
                bool success = replayHost.PortRunGoblinReplayForCommandLine(inputPath);
                exitCode = success ? 0 : 2;
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Goblin replay CLI failed: inputPath={inputPath}", ex);
                Console.Error.WriteLine($"GoblinReplayCliFailed: inputPath={inputPath}; error={ex.Message}");
                exitCode = 2;
                return true;
            }
        }

        private static string? ReadArgumentValue(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string prefix = name + "=";
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return arg[prefix.Length..].Trim('"');
                }

                if (arg.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    i + 1 < args.Length)
                {
                    return args[i + 1].Trim('"');
                }
            }

            return null;
        }
    }
}

using System.Diagnostics;

namespace GoblinFarmer
{
    internal static class LocalToolStartup
    {
        private const string AutoRecordScriptRelativePath = @"Scripts\Local Tools\Auto Record Diablo.ps1";

        public static void StartVsDebugDiabloAutoRecordMonitor()
        {
            if (!AppSettings.IsVsDebugProfile)
            {
                AppLogger.Info("AutoRecordDiabloMonitorSkipped: reason=NotVsDebugProfile");
                return;
            }

            string projectRoot = ResolveProjectRoot();
            string scriptPath = Path.Combine(projectRoot, AutoRecordScriptRelativePath);
            if (!File.Exists(scriptPath))
            {
                AppLogger.Info($"AutoRecordDiabloMonitorSkipped: reason=ScriptMissing; scriptPath={scriptPath}; projectRoot={projectRoot}");
                return;
            }

            for (int attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = "powershell.exe",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = projectRoot,
                    };
                    startInfo.ArgumentList.Add("-NoProfile");
                    startInfo.ArgumentList.Add("-ExecutionPolicy");
                    startInfo.ArgumentList.Add("Bypass");
                    startInfo.ArgumentList.Add("-File");
                    startInfo.ArgumentList.Add(scriptPath);
                    startInfo.ArgumentList.Add("-ProjectRoot");
                    startInfo.ArgumentList.Add(projectRoot);

                    Process? process = Process.Start(startInfo);
                    AppLogger.Info($"AutoRecordDiabloMonitorLaunchRequested: scriptPath={scriptPath}; projectRoot={projectRoot}; processId={process?.Id.ToString() ?? "unknown"}; attempt={attempt}");
                    if (process == null)
                    {
                        AppLogger.Info($"AutoRecordDiabloMonitorLaunchRetry: reason=ProcessStartReturnedNull; scriptPath={scriptPath}; attempt={attempt}");
                        continue;
                    }

                    if (!process.WaitForExit(750))
                    {
                        AppLogger.Info($"AutoRecordDiabloMonitorReady: scriptPath={scriptPath}; processId={process.Id}; attempt={attempt}; state=Running");
                        return;
                    }

                    AppLogger.Info($"AutoRecordDiabloMonitorExitedEarly: scriptPath={scriptPath}; processId={process.Id}; exitCode={process.ExitCode}; attempt={attempt}");
                    if (process.ExitCode == 0)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"AutoRecordDiabloMonitorLaunchFailed: scriptPath={scriptPath}; projectRoot={projectRoot}; attempt={attempt}", ex);
                }
            }
        }

        private static string ResolveProjectRoot()
        {
            string? configDirectory = Path.GetDirectoryName(AppSettings.ConfigPath);
            if (!string.IsNullOrWhiteSpace(configDirectory) &&
                string.Equals(Path.GetFileName(configDirectory), "Config", StringComparison.OrdinalIgnoreCase))
            {
                DirectoryInfo? root = Directory.GetParent(configDirectory);
                if (root != null)
                {
                    return root.FullName;
                }
            }

            DirectoryInfo? directory = new(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "GoblinFarmer.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}

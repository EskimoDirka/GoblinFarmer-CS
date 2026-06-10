using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace GoblinFarmer
{
    internal static class DebugManager
    {
        private const int DebugScreenshotRunCap = 50;
        private static readonly TimeSpan DebugScreenshotThrottle = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan ExpensiveDiagnosticThrottle = TimeSpan.FromSeconds(2);
        private static readonly object SyncRoot = new();
        private static readonly Dictionary<string, DateTime> LastDebugScreenshotByKey = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, DateTime> LastExpensiveDiagnosticByKey = new(StringComparer.OrdinalIgnoreCase);
        private static int debugScreenshotsThisSession;

        public static DiagnosticsSessionState Session { get; } = new();

        public static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;
        public static string LogsDirectory => Path.Combine(BaseDirectory, "Logs");
        public static string ScreenshotsDirectory => Path.Combine(BaseDirectory, "Screenshots");
        public static string DebugScreenshotsDirectory => Path.Combine(BaseDirectory, "debug-screenshots");
        public static string GoblinEvidenceDirectory => Path.Combine(BaseDirectory, "Debug", "GoblinEvidence");
        public static string SessionsDirectory => Path.Combine(BaseDirectory, "Sessions");
        public static string SessionInfoPath => Path.Combine(BaseDirectory, "session-info.txt");

        public static bool IsVisualStudioDebugSession => AppSettings.IsVsDebugProfile;
        public static bool IsReleaseDebugModeEnabled => !AppSettings.IsVsDebugProfile && AppSettings.Debug.DebugMode;
        public static bool IsNormalReleaseUserMode => !AppSettings.IsVsDebugProfile && !AppSettings.Debug.DebugMode;
        public static bool UsesInMemoryForcedVsDebugEvidenceSettings => AppSettings.IsVsDebugProfile;
        public static bool UsesUserSavedReleaseDebugModePreferences => !AppSettings.IsVsDebugProfile;
        public static bool DiagnosticLoggingEnabled => AppSettings.IsVsDebugProfile || (AppSettings.Debug.DebugMode && AppSettings.Debug.EnableVerboseLogging);

        public static AppSettings.DebugDefaultsProfile ResolveDebugDefaultsProfile(string? explicitProfile, bool debuggerAttached, bool debugBuild)
        {
            if (Enum.TryParse(explicitProfile, ignoreCase: true, out AppSettings.DebugDefaultsProfile parsedProfile))
            {
                return parsedProfile;
            }

            return IsVsDebugLaunchSurface(debuggerAttached, debugBuild)
                ? AppSettings.DebugDefaultsProfile.VsDebug
                : AppSettings.DebugDefaultsProfile.ReleaseUser;
        }

        public static bool IsVsDebugLaunchSurface(bool debuggerAttached, bool debugBuild)
        {
            return debuggerAttached && debugBuild;
        }

        public static bool ShouldSuppressFirstRunSetup(AppSettings.DebugDefaultsProfile profile)
        {
            return profile == AppSettings.DebugDefaultsProfile.VsDebug;
        }

        public static bool ShouldShowDynamicDebugControls(AppSettings.DebugDefaultsProfile profile)
        {
            return profile != AppSettings.DebugDefaultsProfile.VsDebug;
        }

        public static bool ShouldRequireFirstRunSetup(AppSettings.DebugDefaultsProfile profile, bool requiredRuntimeConfigurationIsValid)
        {
            return !requiredRuntimeConfigurationIsValid;
        }

        public static void ApplyVisualStudioDebugDefaults(AppSettings.DebugSettings debug)
        {
            debug.DebugMode = true;
            debug.ShowDiagnosticOverlay = true;
            debug.ShowRouteInspector = true;
            debug.EnableDebugScreenshots = true;
            debug.EnableMissingAssetPrompts = true;
            debug.EnableVerboseLogging = true;
        }

        public static void ApplyReleaseUserDefaultsIfPreferenceUnsaved(AppSettings.DebugSettings debug)
        {
            if (debug.DebugModePreferenceSaved)
            {
                return;
            }

            debug.DebugMode = false;
            debug.ShowDiagnosticOverlay = false;
            debug.ShowRouteInspector = false;
            debug.EnableDebugScreenshots = false;
            debug.EnableMissingAssetPrompts = false;
            debug.EnableVerboseLogging = false;
        }

        public static void ApplyReleaseDebugModePreference(bool enabled, AppSettings.DebugSettings debug)
        {
            debug.DebugModePreferenceSaved = true;
            debug.DebugMode = enabled;
            debug.EnableMissingAssetPrompts = enabled;
            debug.ShowDiagnosticOverlay = enabled;
            debug.ShowRouteInspector = enabled;
            if (enabled)
            {
                debug.EnableDebugScreenshots = true;
            }
        }

        public static bool ShouldShowDiagnosticOverlay()
        {
            return AppSettings.IsVsDebugProfile || (AppSettings.Debug.DebugMode && AppSettings.Debug.ShowDiagnosticOverlay);
        }

        public static bool ShouldShowRouteInspector()
        {
            return AppSettings.IsVsDebugProfile || (AppSettings.Debug.DebugMode && AppSettings.Debug.ShowRouteInspector);
        }

        public static bool ShouldCaptureDebugEvidence(bool keepDebugScreenshotsChecked)
        {
            return AppSettings.Debug.EnableDebugScreenshots && keepDebugScreenshotsChecked;
        }

        public static bool TryReserveDebugScreenshot(string actionName, string reason, out string skipReason)
        {
            skipReason = "";
            string key = $"{actionName}|{reason}";
            DateTime now = DateTime.Now;

            lock (SyncRoot)
            {
                if (debugScreenshotsThisSession >= DebugScreenshotRunCap)
                {
                    skipReason = "run cap reached";
                    return false;
                }

                if (LastDebugScreenshotByKey.TryGetValue(key, out DateTime lastCaptured) &&
                    now - lastCaptured < DebugScreenshotThrottle)
                {
                    skipReason = "throttled";
                    return false;
                }

                LastDebugScreenshotByKey[key] = now;
                debugScreenshotsThisSession++;
                return true;
            }
        }

        public static void ReleaseDebugScreenshotReservation()
        {
            lock (SyncRoot)
            {
                debugScreenshotsThisSession = Math.Max(0, debugScreenshotsThisSession - 1);
            }
        }

        public static bool ShouldLogExpensiveDiagnostic(string key)
        {
            if (!DiagnosticLoggingEnabled)
            {
                return false;
            }

            DateTime now = DateTime.Now;
            lock (SyncRoot)
            {
                if (LastExpensiveDiagnosticByKey.TryGetValue(key, out DateTime lastLogged) &&
                    now - lastLogged < ExpensiveDiagnosticThrottle)
                {
                    return false;
                }

                LastExpensiveDiagnosticByKey[key] = now;
                return true;
            }
        }

        public static void RecordDebugScreenshotPath(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                Session.SetLatestScreenshot(path);
            }
        }

        public static void RecordImageRecognition(ImageRecognitionDiagnostic diagnostic)
        {
            string key = $"{diagnostic.CallingFlow}|{diagnostic.TemplateName}|{diagnostic.ScanRegion}";
            if (!ShouldLogExpensiveDiagnostic(key))
            {
                return;
            }

            AppLogger.Info(
                "ImageRecognitionDiagnostic: " +
                $"flow={LogField(diagnostic.CallingFlow)}; " +
                $"template={LogField(diagnostic.TemplateName)}; " +
                $"confidence={diagnostic.Confidence:0.000}; " +
                $"runnerUp={LogField(diagnostic.RunnerUpName)}; " +
                $"runnerUpConfidence={diagnostic.RunnerUpConfidence:0.000}; " +
                $"scanRegion={LogField(diagnostic.ScanRegion)}; " +
                $"threshold={diagnostic.Threshold:0.000}; " +
                $"bestPoint={LogField(diagnostic.BestMatchPoint)}; " +
                $"detected={LogField(diagnostic.DetectedName)}; " +
                $"templateCount={diagnostic.TemplateCount}");
        }

        public static string FindLatestDebugPackagePath()
        {
            DirectoryInfo? directory = new(BaseDirectory);
            for (int depth = 0; directory != null && depth < 8; depth++, directory = directory.Parent)
            {
                string packageDirectory = Path.Combine(directory.FullName, "DebugPackages");
                if (!Directory.Exists(packageDirectory))
                {
                    continue;
                }

                FileInfo? latestPackage = new DirectoryInfo(packageDirectory)
                    .GetFiles("GoblinFarmer_Debug_*.zip")
                    .OrderByDescending(file => file.LastWriteTime)
                    .FirstOrDefault();

                if (latestPackage != null)
                {
                    return latestPackage.FullName;
                }
            }

            return "";
        }

        public static CleanupResult CleanupOldSessionSummaries(int retentionCount)
        {
            CleanupResult result = CleanupOldSessionSummaries(SessionsDirectory, retentionCount);
            AppLogger.Info($"Session summary retention cleanup complete: scanned={result.Scanned}; deleted={result.Deleted}; kept={result.Kept}; retentionCount={retentionCount}; folder={SessionsDirectory}");
            return result;
        }

        internal static CleanupResult CleanupOldSessionSummaries(string sessionsDirectory, int retentionCount)
        {
            return CleanupOldFilesByCount(sessionsDirectory, "Session_*.md", retentionCount, "session summary");
        }

        public static CleanupResult CleanupOldDebugPackages(int retentionCount)
        {
            CleanupResult total = new(0, 0, 0);
            foreach (string directory in FindDebugPackageDirectories())
            {
                CleanupResult result = CleanupOldDebugPackages(directory, retentionCount);
                total = total.Add(result);
            }

            if (total.Scanned == 0)
            {
                string defaultPackageDirectory = Path.Combine(BaseDirectory, "DebugPackages");
                AppLogger.Info($"Debug package retention cleanup complete: scanned=0; deleted=0; kept=0; retentionCount={retentionCount}; folder={defaultPackageDirectory}");
            }
            else
            {
                AppLogger.Info($"Debug package retention cleanup complete: scanned={total.Scanned}; deleted={total.Deleted}; kept={total.Kept}; retentionCount={retentionCount}");
            }

            return total;
        }

        internal static CleanupResult CleanupOldDebugPackages(string packageDirectory, int retentionCount)
        {
            return CleanupOldFilesByCount(packageDirectory, "GoblinFarmer_Debug_*.zip", retentionCount, "debug package");
        }

        public static CleanupResult CleanupOldGoblinEvidence(int retentionCount)
        {
            CleanupResult result = CleanupOldGoblinEvidence(GoblinEvidenceDirectory, retentionCount);
            AppLogger.Info($"GoblinEvidence retention cleanup complete: scanned={result.Scanned}; deleted={result.Deleted}; skipped={result.Skipped}; kept={result.Kept}; retentionCount={retentionCount}; folder={GoblinEvidenceDirectory}");
            return result;
        }

        internal static CleanupResult CleanupOldGoblinEvidence(string goblinEvidenceDirectory, int retentionCount)
        {
            if (retentionCount <= 0)
            {
                AppLogger.Info($"GoblinEvidence retention cleanup disabled: retentionCount={retentionCount}; folder={goblinEvidenceDirectory}");
                return new CleanupResult(0, 0, 0);
            }

            if (!Directory.Exists(goblinEvidenceDirectory))
            {
                return new CleanupResult(0, 0, 0);
            }

            string root;
            try
            {
                root = Path.GetFullPath(goblinEvidenceDirectory);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"GoblinEvidence retention cleanup skipped: invalid folder={goblinEvidenceDirectory}", ex);
                return new CleanupResult(0, 0, 0, 1);
            }

            int skipped = 0;
            FileInfo[] files;
            try
            {
                files = EnumerateFilesInsideDirectory(root, ref skipped)
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .ThenBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"GoblinEvidence retention cleanup scan failed: folder={root}", ex);
                return new CleanupResult(0, 0, 0, 1);
            }

            int deleted = 0;
            foreach (FileInfo file in files.Skip(retentionCount))
            {
                if (!IsPathInsideDirectory(root, file.FullName))
                {
                    skipped++;
                    AppLogger.Info($"GoblinEvidence retention cleanup skipped outside folder: path={file.FullName}; folder={root}");
                    continue;
                }

                try
                {
                    file.Delete();
                    deleted++;
                }
                catch (Exception ex)
                {
                    skipped++;
                    AppLogger.Error($"GoblinEvidence retention cleanup delete skipped: path={file.FullName}", ex);
                }
            }

            return new CleanupResult(files.Length, deleted, files.Length - deleted, skipped);
        }

        private static List<FileInfo> EnumerateFilesInsideDirectory(string root, ref int skipped)
        {
            return EnumerateFilesInsideDirectory(root, ref skipped, "GoblinEvidence");
        }

        private static List<FileInfo> EnumerateFilesInsideDirectory(string root, ref int skipped, string artifactName)
        {
            List<FileInfo> discoveredFiles = [];
            Stack<DirectoryInfo> directories = new();
            directories.Push(new DirectoryInfo(root));

            while (directories.Count > 0)
            {
                DirectoryInfo directory = directories.Pop();
                FileInfo[] files;
                try
                {
                    files = directory.GetFiles("*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex)
                {
                    skipped++;
                    AppLogger.Error($"{artifactName} retention cleanup scan skipped folder: folder={directory.FullName}", ex);
                    continue;
                }

                foreach (FileInfo file in files)
                {
                    if (IsPathInsideDirectory(root, file.FullName))
                    {
                        discoveredFiles.Add(file);
                    }
                    else
                    {
                        skipped++;
                        AppLogger.Info($"{artifactName} retention cleanup skipped outside folder: path={file.FullName}; folder={root}");
                    }
                }

                DirectoryInfo[] childDirectories;
                try
                {
                    childDirectories = directory.GetDirectories("*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex)
                {
                    skipped++;
                    AppLogger.Error($"{artifactName} retention cleanup scan skipped child folders: folder={directory.FullName}", ex);
                    continue;
                }

                foreach (DirectoryInfo childDirectory in childDirectories)
                {
                    if ((childDirectory.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    {
                        skipped++;
                        AppLogger.Info($"{artifactName} retention cleanup skipped linked folder: folder={childDirectory.FullName}");
                        continue;
                    }

                    if (IsPathInsideDirectory(root, childDirectory.FullName))
                    {
                        directories.Push(childDirectory);
                    }
                    else
                    {
                        skipped++;
                        AppLogger.Info($"{artifactName} retention cleanup skipped outside folder: folder={childDirectory.FullName}; root={root}");
                    }
                }
            }

            return discoveredFiles;
        }

        public static CleanupResult CleanupDebugArtifactsByAge(TimeSpan retentionAge)
        {
            if (retentionAge <= TimeSpan.Zero)
            {
                AppLogger.Info($"Debug artifact age retention cleanup disabled: retentionAge={retentionAge}");
                return new CleanupResult(0, 0, 0);
            }

            CleanupResult total = new(0, 0, 0);
            foreach ((string Directory, string Name) target in FindDebugArtifactRetentionTargets())
            {
                CleanupResult result = CleanupOldFilesByAge(target.Directory, retentionAge, target.Name);
                total = total.Add(result);
            }

            AppLogger.Info($"Debug artifact age retention cleanup complete: scanned={total.Scanned}; deleted={total.Deleted}; skipped={total.Skipped}; kept={total.Kept}; retentionAgeHours={retentionAge.TotalHours:0.##}; mode={(IsVisualStudioDebugSession ? "VsDebug" : "Release")}");
            return total;
        }

        internal static CleanupResult CleanupOldFilesByAge(string directory, TimeSpan retentionAge, string artifactName)
        {
            if (retentionAge <= TimeSpan.Zero)
            {
                AppLogger.Info($"{artifactName} age retention cleanup disabled: retentionAge={retentionAge}; folder={directory}");
                return new CleanupResult(0, 0, 0);
            }

            if (!Directory.Exists(directory))
            {
                return new CleanupResult(0, 0, 0);
            }

            string root;
            try
            {
                root = Path.GetFullPath(directory);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"{artifactName} age retention cleanup skipped: invalid folder={directory}", ex);
                return new CleanupResult(0, 0, 0, 1);
            }

            int skipped = 0;
            FileInfo[] files;
            try
            {
                files = EnumerateFilesInsideDirectory(root, ref skipped, artifactName).ToArray();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"{artifactName} age retention cleanup scan failed: folder={root}", ex);
                return new CleanupResult(0, 0, 0, 1);
            }

            DateTime cutoffUtc = DateTime.UtcNow - retentionAge;
            int deleted = 0;
            foreach (FileInfo file in files.Where(file => file.LastWriteTimeUtc < cutoffUtc))
            {
                if (!IsPathInsideDirectory(root, file.FullName))
                {
                    skipped++;
                    AppLogger.Info($"{artifactName} age retention cleanup skipped outside folder: path={file.FullName}; folder={root}");
                    continue;
                }

                try
                {
                    file.Delete();
                    deleted++;
                }
                catch (Exception ex)
                {
                    skipped++;
                    AppLogger.Error($"{artifactName} age retention cleanup delete skipped: path={file.FullName}", ex);
                }
            }

            CleanupEmptyDirectories(root, artifactName, ref skipped);
            return new CleanupResult(files.Length, deleted, files.Length - deleted, skipped);
        }

        private static void CleanupEmptyDirectories(string root, string artifactName, ref int skipped)
        {
            if (!Directory.Exists(root))
            {
                return;
            }

            List<DirectoryInfo> directories;
            try
            {
                directories = new DirectoryInfo(root)
                    .EnumerateDirectories("*", SearchOption.AllDirectories)
                    .OrderByDescending(directory => directory.FullName.Length)
                    .ToList();
            }
            catch (Exception ex)
            {
                skipped++;
                AppLogger.Error($"{artifactName} age retention cleanup empty-folder scan skipped: folder={root}", ex);
                return;
            }

            foreach (DirectoryInfo directory in directories)
            {
                if ((directory.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                {
                    skipped++;
                    AppLogger.Info($"{artifactName} age retention cleanup skipped linked empty-folder check: folder={directory.FullName}");
                    continue;
                }

                if (!IsPathInsideDirectory(root, directory.FullName))
                {
                    skipped++;
                    AppLogger.Info($"{artifactName} age retention cleanup skipped outside empty-folder check: folder={directory.FullName}; root={root}");
                    continue;
                }

                try
                {
                    if (!directory.EnumerateFileSystemInfos().Any())
                    {
                        directory.Delete();
                    }
                }
                catch (Exception ex)
                {
                    skipped++;
                    AppLogger.Error($"{artifactName} age retention cleanup empty-folder delete skipped: folder={directory.FullName}", ex);
                }
            }
        }

        private static CleanupResult CleanupOldFilesByCount(string directory, string searchPattern, int retentionCount, string artifactName)
        {
            if (retentionCount <= 0)
            {
                AppLogger.Info($"{artifactName} retention cleanup disabled: retentionCount={retentionCount}; folder={directory}");
                return new CleanupResult(0, 0, 0);
            }

            if (!Directory.Exists(directory))
            {
                return new CleanupResult(0, 0, 0);
            }

            FileInfo[] files;
            try
            {
                files = new DirectoryInfo(directory)
                    .GetFiles(searchPattern, SearchOption.TopDirectoryOnly)
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .ThenByDescending(file => file.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"{artifactName} retention cleanup scan failed: folder={directory}; pattern={searchPattern}", ex);
                return new CleanupResult(0, 0, 0);
            }

            int deleted = 0;
            int skipped = 0;
            foreach (FileInfo file in files.Skip(retentionCount))
            {
                try
                {
                    file.Delete();
                    deleted++;
                }
                catch (Exception ex)
                {
                    skipped++;
                    AppLogger.Error($"{artifactName} retention cleanup delete failed: path={file.FullName}", ex);
                }
            }

            return new CleanupResult(files.Length, deleted, files.Length - deleted, skipped);
        }

        private static bool IsPathInsideDirectory(string directory, string path)
        {
            string root = Path.GetFullPath(directory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(path);
            return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<(string Directory, string Name)> FindDebugArtifactRetentionTargets()
        {
            HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);
            List<(string Directory, string Name)> targets = [];
            void AddTarget(string path, string name)
            {
                if (string.IsNullOrWhiteSpace(path) || !directories.Add(path))
                {
                    return;
                }

                targets.Add((path, name));
            }

            HashSet<string> roots = new(StringComparer.OrdinalIgnoreCase)
            {
                BaseDirectory,
            };
            if (TryResolveConfigRoot(out string configRoot))
            {
                roots.Add(configRoot);
            }

            foreach (string root in roots)
            {
                AddTarget(Path.Combine(root, "Logs"), "Logs");
                AddTarget(Path.Combine(root, "Screenshots"), "Screenshots");
                AddTarget(Path.Combine(root, "debug-screenshots"), "DebugScreenshots");
                AddTarget(Path.Combine(root, "Sessions"), "Sessions");
                AddTarget(Path.Combine(root, "DebugPackages"), "DebugPackages");
                AddTarget(Path.Combine(root, "Debug", "GoblinEvidence"), "GoblinEvidence");
                AddTarget(Path.Combine(root, "Debug", "InventoryReplay"), "InventoryReplay");
            }

            return targets;
        }

        private static bool TryResolveConfigRoot(out string configRoot)
        {
            string? configDirectory = Path.GetDirectoryName(AppSettings.ConfigPath);
            if (!string.IsNullOrWhiteSpace(configDirectory) &&
                string.Equals(Path.GetFileName(configDirectory), "Config", StringComparison.OrdinalIgnoreCase))
            {
                DirectoryInfo? root = Directory.GetParent(configDirectory);
                if (root != null)
                {
                    configRoot = root.FullName;
                    return true;
                }
            }

            configRoot = "";
            return false;
        }

        private static IEnumerable<string> FindDebugPackageDirectories()
        {
            HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);
            DirectoryInfo? directory = new(BaseDirectory);
            for (int depth = 0; directory != null && depth < 8; depth++, directory = directory.Parent)
            {
                string packageDirectory = Path.Combine(directory.FullName, "DebugPackages");
                if (Directory.Exists(packageDirectory))
                {
                    directories.Add(Path.GetFullPath(packageDirectory));
                }
            }

            return directories;
        }

        public static string FindLatestScreenshotPath()
        {
            if (!string.IsNullOrWhiteSpace(Session.LatestScreenshotPath))
            {
                return Session.LatestScreenshotPath;
            }

            if (!Directory.Exists(ScreenshotsDirectory))
            {
                return "";
            }

            return new DirectoryInfo(ScreenshotsDirectory)
                .GetFiles("*.*")
                .Where(file => IsScreenshotExtension(file.Extension))
                .OrderByDescending(file => file.LastWriteTime)
                .FirstOrDefault()
                ?.FullName ?? "";
        }

        public static string ExportSessionSummary(SessionSummaryContext context)
        {
            try
            {
                Directory.CreateDirectory(SessionsDirectory);

                DateTime endedAt = DateTime.Now;
                DiagnosticsSessionSnapshot snapshot = Session.Snapshot(endedAt);
                string path = Path.Combine(SessionsDirectory, $"Session_{snapshot.StartedAtLocal:yyyyMMdd_HHmmss}.md");
                string latestLogPath = string.IsNullOrWhiteSpace(context.LatestLogPath) ? AppLogger.CurrentLogFilePath : context.LatestLogPath;
                string latestPackagePath = string.IsNullOrWhiteSpace(context.LatestDebugPackagePath) ? FindLatestDebugPackagePath() : context.LatestDebugPackagePath;
                string latestScreenshotPath = string.IsNullOrWhiteSpace(context.LatestScreenshotPath) ? FindLatestScreenshotPath() : context.LatestScreenshotPath;
                string latestFailurePath = string.IsNullOrWhiteSpace(context.LatestFailureScreenshotPath) ? latestScreenshotPath : context.LatestFailureScreenshotPath;
                string lastKnownIssue = string.IsNullOrWhiteSpace(context.LastKnownIssue) ? snapshot.LastKnownIssue : context.LastKnownIssue;

                StringBuilder builder = new();
                builder.AppendLine("# GoblinFarmer Session Summary");
                builder.AppendLine();
                builder.AppendLine($"- App version: {AppDisplayVersion}");
                builder.AppendLine($"- Build mode: {AppSettings.BuildConfiguration}");
                builder.AppendLine($"- Debug profile: {AppSettings.CurrentDebugDefaultsProfile}");
                builder.AppendLine($"- Debug mode: {AppSettings.Debug.DebugMode}");
                builder.AppendLine($"- Session start: {snapshot.StartedAtLocal:O}");
                builder.AppendLine($"- Session end: {snapshot.EndedAtLocal:O}");
                builder.AppendLine($"- Duration: {snapshot.Duration:hh\\:mm\\:ss}");
                builder.AppendLine($"- Games created: {snapshot.GamesCreated}");
                builder.AppendLine($"- Teleports attempted: {snapshot.TeleportsAttempted}");
                builder.AppendLine($"- Teleports confirmed: {snapshot.TeleportsConfirmed}");
                builder.AppendLine($"- Teleport blocks: {snapshot.TeleportBlocks}");
                builder.AppendLine($"- Teleport failures/timeouts: {snapshot.TeleportFailuresOrTimeouts}");
                builder.AppendLine($"- Start Game failures: {snapshot.StartGameFailures}");
                builder.AppendLine($"- Battle.net launch failures: {snapshot.BattleNetLaunchFailures}");
                builder.AppendLine($"- Repair failures: {snapshot.RepairFailures}");
                builder.AppendLine($"- Salvage failures: {snapshot.SalvageFailures}");
                builder.AppendLine($"- Stash failures: {snapshot.StashFailures}");
                builder.AppendLine($"- Workflow cancellations: {snapshot.WorkflowCancellations}");
                builder.AppendLine($"- Unexpected exceptions: {snapshot.UnexpectedExceptions}");
                builder.AppendLine($"- Combat/farming active time: {snapshot.CombatActiveTime:hh\\:mm\\:ss}");
                builder.AppendLine();
                builder.AppendLine("## Goblin Tracker");
                builder.AppendLine();
                builder.AppendLine($"Goblins Found: {snapshot.GoblinCount}");
                builder.AppendLine($"Goblin Found Records: {snapshot.GoblinFoundRecordCount}");
                builder.AppendLine($"Counted Area Keys: {snapshot.CountedGoblinAreaCount}");
                builder.AppendLine($"Last Counted Area: {DisplayPath(snapshot.LastCountedGoblinAreaKey)}");
                builder.AppendLine($"Active Combat Time: {snapshot.GoblinActiveCombatTime:hh\\:mm\\:ss}");
                builder.AppendLine($"GPH: {snapshot.GoblinsPerHour:0.00}");
                builder.AppendLine();
                builder.AppendLine("## Goblin Observations");
                builder.AppendLine();
                builder.AppendLine($"Goblin Observations: {snapshot.GoblinObservationCount}");
                builder.AppendLine($"Journal Observations: {snapshot.JournalObservationCount}");
                builder.AppendLine($"Minimap Observations: {snapshot.MinimapObservationCount}");
                builder.AppendLine($"Eligible Observations: {snapshot.EligibleObservationCount}");
                builder.AppendLine($"Blocked Observations: {snapshot.BlockedObservationCount}");
                builder.AppendLine($"Duplicate Observations: {snapshot.DuplicateObservationCount}");
                builder.AppendLine($"Last Observation: {DisplayPath(snapshot.LastGoblinObservation?.GoblinType ?? "")}");
                builder.AppendLine($"Last Observation Area: {DisplayPath(snapshot.LastGoblinObservation?.AreaKey ?? "")}");
                builder.AppendLine($"Last Observation Source: {DisplayPath(snapshot.LastGoblinObservation?.Source ?? "")}");
                builder.AppendLine($"Last Observation Reason: {DisplayPath(snapshot.LastGoblinObservation?.Reason ?? "")}");
                builder.AppendLine();
                builder.AppendLine("## Goblin Evidence");
                builder.AppendLine();
                builder.AppendLine($"Events Detected: {snapshot.GoblinEvidenceEventCount}");
                builder.AppendLine($"Last Evidence: {snapshot.LastGoblinEvidenceType}");
                builder.AppendLine($"Last Confidence: {snapshot.LastGoblinEvidenceConfidence:0.00}");
                builder.AppendLine($"Last Evidence Time: {(snapshot.LastGoblinEvidenceTime.HasValue ? snapshot.LastGoblinEvidenceTime.Value.ToString("HH:mm:ss") : "--")}");
                builder.AppendLine($"Evidence Screenshot Folder: {DisplayPath(snapshot.GoblinEvidenceScreenshotFolder)}");
                builder.AppendLine($"Last Evidence Screenshot: {DisplayPath(snapshot.LastGoblinEvidenceScreenshotPath)}");
                builder.AppendLine();
                builder.AppendLine($"- Latest log path: {DisplayPath(latestLogPath)}");
                builder.AppendLine($"- Latest debug package path: {DisplayPath(latestPackagePath)}");
                builder.AppendLine($"- Latest screenshot path: {DisplayPath(latestScreenshotPath)}");
                builder.AppendLine($"- Latest failure screenshot path: {DisplayPath(latestFailurePath)}");
                builder.AppendLine($"- Latest failure type: {DisplayPath(context.LatestFailureType)}");
                builder.AppendLine($"- Active workflow at exit: {DisplayPath(context.ActiveWorkflow)}");
                builder.AppendLine($"- Last known issue/recent failure summary: {DisplayPath(lastKnownIssue)}");

                File.WriteAllText(path, builder.ToString());
                Session.SetLatestSessionSummary(path);
                AppLogger.Info($"Session summary exported: {path}");
                return path;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Session summary export failed.", ex);
                return "";
            }
        }

        public static string AppDisplayVersion
        {
            get
            {
                string? informationalVersion = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion;

                if (!string.IsNullOrWhiteSpace(informationalVersion))
                {
                    int metadataIndex = informationalVersion.IndexOf('+');
                    return metadataIndex >= 0
                        ? informationalVersion[..metadataIndex]
                        : informationalVersion;
                }

                Version? version = Assembly.GetExecutingAssembly().GetName().Version;
                return version is null
                    ? "unknown"
                    : $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }

        private static bool IsScreenshotExtension(string extension)
        {
            return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
        }

        private static string DisplayPath(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "none" : value;
        }

        private static string LogField(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "Unknown"
                : value.Replace(";", ",").Replace(Environment.NewLine, " ");
        }
    }

    internal sealed class DiagnosticsSessionState
    {
        private readonly object syncRoot = new();
        private DateTime? combatActiveStartedAt;
        private TimeSpan combatActiveTime;
        private DateTime? goblinCombatStartedAt;
        private TimeSpan goblinActiveCombatTime;
        private int goblinCount;
        private readonly List<GoblinEvidenceEvent> goblinEvidenceEvents = [];
        private readonly List<GoblinFoundRecord> goblinFoundRecords = [];
        private readonly List<GoblinObservationRecord> goblinObservationRecords = [];
        private int journalObservationCount;
        private int minimapObservationCount;
        private int eligibleObservationCount;
        private int blockedObservationCount;
        private int duplicateObservationCount;
        private int gamesCreated;
        private int teleportsAttempted;
        private int teleportsConfirmed;
        private int teleportBlocks;
        private int teleportFailuresOrTimeouts;
        private int startGameFailures;
        private int battleNetLaunchFailures;
        private int repairFailures;
        private int salvageFailures;
        private int stashFailures;
        private int workflowCancellations;
        private int unexpectedExceptions;
        private string lastKnownIssue = "";
        private string latestScreenshotPath = "";
        private string latestSessionSummaryPath = "";

        public DateTime StartedAtLocal { get; } = DateTime.Now;
        public DateTime StartedAtUtc => StartedAtLocal.ToUniversalTime();
        public string LatestScreenshotPath => latestScreenshotPath;

        public int RecordGoblinFound(GoblinFoundRecord record)
        {
            lock (syncRoot)
            {
                goblinFoundRecords.Add(record);
                return Interlocked.Increment(ref goblinCount);
            }
        }

        public void RecordGoblinFoundRecord(GoblinFoundRecord record)
        {
            lock (syncRoot)
            {
                goblinFoundRecords.Add(record);
            }
        }

        public void RecordGameCreated() => Interlocked.Increment(ref gamesCreated);
        public void RecordTeleportAttempted() => Interlocked.Increment(ref teleportsAttempted);
        public void RecordTeleportConfirmed() => Interlocked.Increment(ref teleportsConfirmed);
        public void RecordTeleportBlocked(string issue) => RecordIssueAndIncrement(ref teleportBlocks, issue);
        public void RecordTeleportFailureOrTimeout(string issue) => RecordIssueAndIncrement(ref teleportFailuresOrTimeouts, issue);
        public void RecordStartGameFailure(string issue) => RecordIssueAndIncrement(ref startGameFailures, issue);
        public void RecordBattleNetLaunchFailure(string issue) => RecordIssueAndIncrement(ref battleNetLaunchFailures, issue);
        public void RecordRepairFailure(string issue) => RecordIssueAndIncrement(ref repairFailures, issue);
        public void RecordSalvageFailure(string issue) => RecordIssueAndIncrement(ref salvageFailures, issue);
        public void RecordStashFailure(string issue) => RecordIssueAndIncrement(ref stashFailures, issue);
        public void RecordWorkflowCancellation(string issue) => RecordIssueAndIncrement(ref workflowCancellations, issue);
        public void RecordUnexpectedException(string issue) => RecordIssueAndIncrement(ref unexpectedExceptions, issue);

        public void RecordGoblinEvidence(GoblinEvidenceEvent evidenceEvent)
        {
            lock (syncRoot)
            {
                goblinEvidenceEvents.Add(evidenceEvent);
                if (!string.IsNullOrWhiteSpace(evidenceEvent.ScreenshotPath))
                {
                    latestScreenshotPath = evidenceEvent.ScreenshotPath;
                }
            }
        }

        public void RecordGoblinObservation(GoblinObservationRecord record)
        {
            lock (syncRoot)
            {
                goblinObservationRecords.Add(record);
                if (record.Source.Equals("Journal", StringComparison.OrdinalIgnoreCase))
                {
                    journalObservationCount++;
                }
                else if (record.Source.Equals("Minimap", StringComparison.OrdinalIgnoreCase))
                {
                    minimapObservationCount++;
                }

                if (record.Reason.Equals("Eligible", StringComparison.OrdinalIgnoreCase))
                {
                    eligibleObservationCount++;
                }
                else if (record.Reason.Equals("BlockedArea", StringComparison.OrdinalIgnoreCase))
                {
                    blockedObservationCount++;
                }
                else if (record.Reason.Equals("AreaAlreadyCounted", StringComparison.OrdinalIgnoreCase) ||
                    record.Reason.Equals("AreaLimitReached", StringComparison.OrdinalIgnoreCase))
                {
                    duplicateObservationCount++;
                }
            }
        }

        public void SetLatestScreenshot(string path)
        {
            lock (syncRoot)
            {
                latestScreenshotPath = path;
            }
        }

        public void SetLatestSessionSummary(string path)
        {
            lock (syncRoot)
            {
                latestSessionSummaryPath = path;
            }
        }

        public void SetLastKnownIssue(string issue)
        {
            if (string.IsNullOrWhiteSpace(issue))
            {
                return;
            }

            lock (syncRoot)
            {
                lastKnownIssue = issue.Trim();
            }
        }

        public void BeginCombatActive()
        {
            lock (syncRoot)
            {
                DateTime now = DateTime.Now;
                combatActiveStartedAt ??= now;
                goblinCombatStartedAt ??= now;
            }
        }

        public void EndCombatActive()
        {
            lock (syncRoot)
            {
                if (combatActiveStartedAt.HasValue)
                {
                    combatActiveTime += DateTime.Now - combatActiveStartedAt.Value;
                    combatActiveStartedAt = null;
                }

                if (goblinCombatStartedAt.HasValue)
                {
                    goblinActiveCombatTime += DateTime.Now - goblinCombatStartedAt.Value;
                    goblinCombatStartedAt = null;
                }
            }
        }

        public void ResetGoblinTrackerStats()
        {
            lock (syncRoot)
            {
                Interlocked.Exchange(ref goblinCount, 0);
                goblinFoundRecords.Clear();
                goblinActiveCombatTime = TimeSpan.Zero;
                if (goblinCombatStartedAt.HasValue)
                {
                    goblinCombatStartedAt = DateTime.Now;
                }
            }
        }

        public DiagnosticsSessionSnapshot Snapshot(DateTime endedAtLocal)
        {
            lock (syncRoot)
            {
                TimeSpan activeTime = combatActiveTime;
                if (combatActiveStartedAt.HasValue)
                {
                    activeTime += endedAtLocal - combatActiveStartedAt.Value;
                }

                TimeSpan goblinActiveTime = goblinActiveCombatTime;
                if (goblinCombatStartedAt.HasValue)
                {
                    goblinActiveTime += endedAtLocal - goblinCombatStartedAt.Value;
                }

                int currentGoblinCount = Volatile.Read(ref goblinCount);
                double goblinsPerHour = CalculateGoblinsPerHour(currentGoblinCount, goblinActiveTime);
                GoblinEvidenceEvent? lastGoblinEvidence = goblinEvidenceEvents.Count > 0
                    ? goblinEvidenceEvents[^1]
                    : null;
                GoblinObservationRecord? lastGoblinObservation = goblinObservationRecords.Count > 0
                    ? goblinObservationRecords[^1]
                    : null;
                GoblinFoundRecord? lastCountedGoblin = goblinFoundRecords.LastOrDefault(record => record.Counted);
                int countedAreaCount = goblinFoundRecords
                    .Where(record => record.Counted && !string.IsNullOrWhiteSpace(record.AreaKey))
                    .Select(record => record.AreaKey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
                return new DiagnosticsSessionSnapshot(
                    StartedAtLocal,
                    endedAtLocal,
                    endedAtLocal - StartedAtLocal,
                    currentGoblinCount,
                    goblinFoundRecords.Count,
                    countedAreaCount,
                    lastCountedGoblin?.AreaKey ?? "",
                    Volatile.Read(ref gamesCreated),
                    Volatile.Read(ref teleportsAttempted),
                    Volatile.Read(ref teleportsConfirmed),
                    Volatile.Read(ref teleportBlocks),
                    Volatile.Read(ref teleportFailuresOrTimeouts),
                    Volatile.Read(ref startGameFailures),
                    Volatile.Read(ref battleNetLaunchFailures),
                    Volatile.Read(ref repairFailures),
                    Volatile.Read(ref salvageFailures),
                    Volatile.Read(ref stashFailures),
                    Volatile.Read(ref workflowCancellations),
                    Volatile.Read(ref unexpectedExceptions),
                    activeTime,
                    combatActiveStartedAt,
                    goblinActiveTime,
                    goblinCombatStartedAt,
                    goblinsPerHour,
                    latestScreenshotPath,
                    latestSessionSummaryPath,
                    lastKnownIssue,
                    goblinObservationRecords.Count,
                    journalObservationCount,
                    minimapObservationCount,
                    eligibleObservationCount,
                    blockedObservationCount,
                    duplicateObservationCount,
                    lastGoblinObservation,
                    goblinEvidenceEvents.Count,
                    lastGoblinEvidence?.Type ?? GoblinEvidenceType.Unknown,
                    lastGoblinEvidence?.Confidence ?? 0,
                    lastGoblinEvidence?.Timestamp,
                    lastGoblinEvidence?.ScreenshotPath ?? "",
                    DebugManager.GoblinEvidenceDirectory);
            }
        }

        private static double CalculateGoblinsPerHour(int count, TimeSpan activeTime)
        {
            if (count <= 0 || activeTime.TotalSeconds <= 0 || activeTime.TotalHours <= 0)
            {
                return 0;
            }

            return count / activeTime.TotalHours;
        }

        private void RecordIssueAndIncrement(ref int counter, string issue)
        {
            Interlocked.Increment(ref counter);
            SetLastKnownIssue(issue);
        }
    }

    internal readonly record struct DiagnosticsSessionSnapshot(
        DateTime StartedAtLocal,
        DateTime EndedAtLocal,
        TimeSpan Duration,
        int GoblinCount,
        int GoblinFoundRecordCount,
        int CountedGoblinAreaCount,
        string LastCountedGoblinAreaKey,
        int GamesCreated,
        int TeleportsAttempted,
        int TeleportsConfirmed,
        int TeleportBlocks,
        int TeleportFailuresOrTimeouts,
        int StartGameFailures,
        int BattleNetLaunchFailures,
        int RepairFailures,
        int SalvageFailures,
        int StashFailures,
        int WorkflowCancellations,
        int UnexpectedExceptions,
        TimeSpan CombatActiveTime,
        DateTime? CombatStartTime,
        TimeSpan GoblinActiveCombatTime,
        DateTime? GoblinCombatStartTime,
        double GoblinsPerHour,
        string LatestScreenshotPath,
        string LatestSessionSummaryPath,
        string LastKnownIssue,
        int GoblinObservationCount,
        int JournalObservationCount,
        int MinimapObservationCount,
        int EligibleObservationCount,
        int BlockedObservationCount,
        int DuplicateObservationCount,
        GoblinObservationRecord? LastGoblinObservation,
        int GoblinEvidenceEventCount,
        GoblinEvidenceType LastGoblinEvidenceType,
        double LastGoblinEvidenceConfidence,
        DateTime? LastGoblinEvidenceTime,
        string LastGoblinEvidenceScreenshotPath,
        string GoblinEvidenceScreenshotFolder);

    internal readonly record struct SessionSummaryContext(
        string LatestLogPath,
        string LatestDebugPackagePath,
        string LatestScreenshotPath,
        string LatestFailureScreenshotPath,
        string LatestFailureType,
        string ActiveWorkflow,
        string LastKnownIssue);

    internal readonly record struct ImageRecognitionDiagnostic(
        string TemplateName,
        double Confidence,
        string RunnerUpName,
        double RunnerUpConfidence,
        string ScanRegion,
        double Threshold,
        string BestMatchPoint,
        string CallingFlow,
        string DetectedName,
        int TemplateCount);

    internal readonly record struct CleanupResult(int Scanned, int Deleted, int Kept, int Skipped = 0)
    {
        public CleanupResult Add(CleanupResult other)
        {
            return new CleanupResult(
                Scanned + other.Scanned,
                Deleted + other.Deleted,
                Kept + other.Kept,
                Skipped + other.Skipped);
        }
    }
}

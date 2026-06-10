using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoblinFarmer
{
    internal static class SalvageInventoryReplayArtifacts
    {
        public const string ClassifierVersion = "SalvageGeometryV2";
        public const int RetentionDays = 7;
        public const string RelativeRoot = @"Debug\InventoryReplay\Salvage";
        public const string LogRelativeRoot = @"Debug\ReplayLogs";
        private const string FolderPrefix = "SalvageInventoryReplay_";
        private static readonly object LogFileLock = new();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static string RootDirectory => Path.Combine(DebugManager.BaseDirectory, "Debug", "InventoryReplay", "Salvage");
        public static string LogDirectory => Path.Combine(DebugManager.BaseDirectory, "Debug", "ReplayLogs");
        public static string EventLogPath => Path.Combine(LogDirectory, "inventory-replay-events.jsonl");

        public static void RecordScanLog(Rectangle screenGrid, string phase, SalvageInventorySlotScanResult scan)
        {
            if (!AppSettings.Debug.DebugMode)
            {
                return;
            }

            try
            {
                DateTime nowUtc = DateTime.UtcNow;
                SalvageInventoryReplayLogEvent logEvent = new(
                    ArtifactType: "SalvageInventoryReplayLog",
                    SavedAtUtc: nowUtc,
                    Phase: phase,
                    ClassifierVersion: ClassifierVersion,
                    Columns: SalvageInventorySlotClassifier.Columns,
                    Rows: SalvageInventorySlotClassifier.Rows,
                    ScreenGrid: new SalvageInventoryReplayRectangle(screenGrid.Left, screenGrid.Top, screenGrid.Width, screenGrid.Height),
                    TargetCount: scan.Targets.Count,
                    CandidateCount: scan.Candidates.Count,
                    Targets: scan.Targets.Select(SalvageInventoryReplayTarget.FromTarget).ToArray(),
                    Candidates: scan.Candidates.Select(SalvageInventoryReplayCandidate.FromCandidate).ToArray());

                AppendJsonLine(EventLogPath, logEvent);
                AppLogger.Info($"SalvageInventoryReplayLogRecorded phase={phase}; path={EventLogPath}; targetCount={scan.Targets.Count}; candidateCount={scan.Candidates.Count}; classifierVersion={ClassifierVersion}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"SalvageInventoryReplayLogRecordFailed phase={phase}", ex);
            }
        }

        public static CleanupResult CleanupOldArtifacts(string rootDirectory, TimeSpan retentionAge, string folderPrefix = FolderPrefix)
        {
            if (retentionAge <= TimeSpan.Zero)
            {
                AppLogger.Info($"InventoryReplayRetentionCleanup disabled: retentionDays={retentionAge.TotalDays:0.##}; folder={rootDirectory}");
                return new CleanupResult(0, 0, 0);
            }

            if (!Directory.Exists(rootDirectory))
            {
                return new CleanupResult(0, 0, 0);
            }

            string root = Path.GetFullPath(rootDirectory);
            DateTime cutoffUtc = DateTime.UtcNow - retentionAge;
            DirectoryInfo[] artifactDirectories;
            try
            {
                artifactDirectories = new DirectoryInfo(root)
                    .GetDirectories($"{folderPrefix}*", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(directory => directory.LastWriteTimeUtc)
                    .ToArray();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"InventoryReplayRetentionCleanup scan failed: folder={root}", ex);
                return new CleanupResult(0, 0, 0, 1);
            }

            int deleted = 0;
            int skipped = 0;
            foreach (DirectoryInfo directory in artifactDirectories.Where(directory => directory.LastWriteTimeUtc < cutoffUtc))
            {
                string fullPath = Path.GetFullPath(directory.FullName);
                if (!IsPathInsideDirectory(root, fullPath))
                {
                    skipped++;
                    AppLogger.Info($"InventoryReplayRetentionCleanup skipped outside folder: path={fullPath}; folder={root}");
                    continue;
                }

                try
                {
                    directory.Delete(recursive: true);
                    deleted++;
                    AppLogger.Info($"InventoryReplayRetentionCleanup deleted: {fullPath}");
                }
                catch (Exception ex)
                {
                    skipped++;
                    AppLogger.Error($"InventoryReplayRetentionCleanup delete failed: path={fullPath}", ex);
                }
            }

            int retained = artifactDirectories.Length - deleted;
            AppLogger.Info($"InventoryReplayRetentionCleanup deleted={deleted}; retained={retained}; scanned={artifactDirectories.Length}; skipped={skipped}; retentionDays={retentionAge.TotalDays:0.##}; folder={root}");
            return new CleanupResult(artifactDirectories.Length, deleted, retained, skipped);
        }

        internal static SalvageInventoryReplayRunResult RunReplayForHarness(string artifactPath)
        {
            string fullPath = Path.GetFullPath(artifactPath);
            string metadataPath;
            string artifactDirectory;
            if (Directory.Exists(fullPath))
            {
                artifactDirectory = fullPath;
                metadataPath = Path.Combine(artifactDirectory, "metadata.json");
            }
            else
            {
                metadataPath = fullPath;
                artifactDirectory = Path.GetDirectoryName(metadataPath) ?? Directory.GetCurrentDirectory();
            }

            if (!File.Exists(metadataPath))
            {
                return new SalvageInventoryReplayRunResult(false, "MetadataMissing", fullPath, 0, 0, []);
            }

            SalvageInventoryReplayMetadata? metadata = JsonSerializer.Deserialize<SalvageInventoryReplayMetadata>(File.ReadAllText(metadataPath), JsonOptions);
            if (metadata == null)
            {
                return new SalvageInventoryReplayRunResult(false, "MetadataInvalid", fullPath, 0, 0, []);
            }

            string imagePath = Path.Combine(artifactDirectory, metadata.ImageFile);
            if (!File.Exists(imagePath))
            {
                return new SalvageInventoryReplayRunResult(false, "ImageMissing", imagePath, 0, 0, []);
            }

            using Bitmap bitmap = new(imagePath);
            Rectangle screenGrid = new(metadata.ScreenGrid.Left, metadata.ScreenGrid.Top, metadata.ScreenGrid.Width, metadata.ScreenGrid.Height);
            SalvageInventorySlotScanResult scan = SalvageInventorySlotClassifier.Scan(bitmap, screenGrid);
            return new SalvageInventoryReplayRunResult(
                true,
                "Loaded",
                imagePath,
                scan.Targets.Count,
                scan.Candidates.Count,
                scan.Targets.Select(SalvageInventoryReplayTarget.FromTarget).ToArray());
        }

        private static bool IsPathInsideDirectory(string directory, string path)
        {
            string root = Path.GetFullPath(directory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(path);
            return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        private static string MakeSafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Scan";
            }

            char[] chars = value
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
                .ToArray();
            string safe = new(chars);
            return string.IsNullOrWhiteSpace(safe) ? "Scan" : safe;
        }

        private static void AppendJsonLine<T>(string path, T value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? DebugManager.BaseDirectory);
            string line = JsonSerializer.Serialize(value, JsonOptions);
            lock (LogFileLock)
            {
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
    }

    internal sealed record SalvageInventoryReplayRunResult(
        bool Loaded,
        string Reason,
        string ImagePath,
        int TargetCount,
        int CandidateCount,
        IReadOnlyList<SalvageInventoryReplayTarget> Targets);

    internal sealed record SalvageInventoryReplayMetadata(
        DateTime SavedAtUtc,
        string Phase,
        string ClassifierVersion,
        int Columns,
        int Rows,
        SalvageInventoryReplayRectangle ScreenGrid,
        string ImageFile,
        int TargetCount,
        int CandidateCount,
        IReadOnlyList<SalvageInventoryReplayTarget> Targets,
        IReadOnlyList<SalvageInventoryReplayCandidate> Candidates);

    internal sealed record SalvageInventoryReplayLogEvent(
        string ArtifactType,
        DateTime SavedAtUtc,
        string Phase,
        string ClassifierVersion,
        int Columns,
        int Rows,
        SalvageInventoryReplayRectangle ScreenGrid,
        int TargetCount,
        int CandidateCount,
        IReadOnlyList<SalvageInventoryReplayTarget> Targets,
        IReadOnlyList<SalvageInventoryReplayCandidate> Candidates);

    internal sealed record SalvageInventoryReplayRectangle(int Left, int Top, int Width, int Height);

    internal sealed record SalvageInventoryReplayPoint(int X, int Y);

    internal sealed record SalvageInventoryReplayTarget(
        int Row,
        int Column,
        SalvageInventoryReplayPoint ScreenPoint,
        int FootprintRows,
        string Quality,
        bool ConfirmationExpected,
        SalvageInventorySlotMetrics Metrics)
    {
        public static SalvageInventoryReplayTarget FromTarget(SalvageInventorySlotTarget target)
        {
            return new SalvageInventoryReplayTarget(
                target.Row,
                target.Column,
                new SalvageInventoryReplayPoint(target.ScreenPoint.X, target.ScreenPoint.Y),
                target.FootprintRows,
                target.Quality,
                target.ConfirmationExpected,
                target.Metrics);
        }
    }

    internal sealed record SalvageInventoryReplayCandidate(
        int Row,
        int Column,
        SalvageInventoryReplayPoint ScreenPoint,
        bool Accepted,
        string Reason,
        int FootprintRows,
        string Quality,
        bool ConfirmationExpected,
        SalvageInventorySlotMetrics Metrics)
    {
        public static SalvageInventoryReplayCandidate FromCandidate(SalvageInventorySlotCandidateDiagnostic candidate)
        {
            return new SalvageInventoryReplayCandidate(
                candidate.Row,
                candidate.Column,
                new SalvageInventoryReplayPoint(candidate.ScreenPoint.X, candidate.ScreenPoint.Y),
                candidate.Accepted,
                candidate.Reason,
                candidate.FootprintRows,
                candidate.Quality,
                candidate.ConfirmationExpected,
                candidate.Metrics);
        }
    }
}

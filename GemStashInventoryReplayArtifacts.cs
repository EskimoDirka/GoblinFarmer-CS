using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoblinFarmer
{
    internal static class GemStashInventoryReplayArtifacts
    {
        public const int RetentionDays = 7;
        public const string RelativeRoot = @"Debug\InventoryReplay\Stash";
        public const string LogRelativeRoot = @"Debug\ReplayLogs";
        private const string FolderPrefix = "GemStashInventoryReplay_";
        private static readonly object LogFileLock = new();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static string RootDirectory => Path.Combine(DebugManager.BaseDirectory, "Debug", "InventoryReplay", "Stash");
        public static string LogDirectory => Path.Combine(DebugManager.BaseDirectory, "Debug", "ReplayLogs");
        public static string EventLogPath => Path.Combine(LogDirectory, "inventory-replay-events.jsonl");

        public static void RecordScanLog(Rectangle screenGrid, string phase, GemStashInventoryScanResult scan, double threshold)
        {
            if (!AppSettings.Debug.DebugMode)
            {
                return;
            }

            try
            {
                DateTime nowUtc = DateTime.UtcNow;
                GemStashInventoryReplayLogEvent logEvent = new(
                    ArtifactType: "GemStashInventoryReplayLog",
                    SavedAtUtc: nowUtc,
                    Phase: phase,
                    ClassifierVersion: GemStashInventoryClassifier.ClassifierVersion,
                    Columns: InventoryGridLayout.Columns,
                    Rows: InventoryGridLayout.Rows,
                    ScreenGrid: new InventoryReplayRectangle(screenGrid.Left, screenGrid.Top, screenGrid.Width, screenGrid.Height),
                    Threshold: threshold,
                    TemplateNames: scan.TemplateNames.ToArray(),
                    InvalidTemplates: scan.InvalidTemplates.ToArray(),
                    TargetCount: scan.Targets.Count,
                    CandidateCount: scan.Candidates.Count,
                    Targets: scan.Targets.Select(GemStashInventoryReplayTarget.FromTarget).ToArray(),
                    Candidates: scan.Candidates.Select(GemStashInventoryReplayCandidate.FromCandidate).ToArray());

                AppendJsonLine(EventLogPath, logEvent);
                AppLogger.Info($"GemStashInventoryReplayLogRecorded phase={phase}; path={EventLogPath}; targetCount={scan.Targets.Count}; candidateCount={scan.Candidates.Count}; templateCount={scan.TemplateNames.Count}; invalidTemplateCount={scan.InvalidTemplates.Count}; classifierVersion={GemStashInventoryClassifier.ClassifierVersion}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"GemStashInventoryReplayLogRecordFailed phase={phase}", ex);
            }
        }

        internal static bool IsGemStashReplayArtifact(string artifactPath)
        {
            string metadataPath = ResolveMetadataPath(artifactPath, out _);
            if (!File.Exists(metadataPath))
            {
                return false;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(metadataPath));
                JsonElement root = document.RootElement;
                return (root.TryGetProperty("ArtifactType", out JsonElement artifactType) &&
                        artifactType.GetString()?.Equals("GemStash", StringComparison.OrdinalIgnoreCase) == true) ||
                    root.TryGetProperty("TemplateNames", out _);
            }
            catch
            {
                return false;
            }
        }

        internal static GemStashInventoryReplayRunResult RunReplayForHarness(string artifactPath)
        {
            string metadataPath = ResolveMetadataPath(artifactPath, out string artifactDirectory);
            if (!File.Exists(metadataPath))
            {
                return new GemStashInventoryReplayRunResult(false, "MetadataMissing", artifactPath, 0, 0, []);
            }

            GemStashInventoryReplayMetadata? metadata = JsonSerializer.Deserialize<GemStashInventoryReplayMetadata>(File.ReadAllText(metadataPath), JsonOptions);
            if (metadata == null)
            {
                return new GemStashInventoryReplayRunResult(false, "MetadataInvalid", metadataPath, 0, 0, []);
            }

            string imagePath = Path.Combine(artifactDirectory, metadata.ImageFile);
            if (!File.Exists(imagePath))
            {
                return new GemStashInventoryReplayRunResult(false, "ImageMissing", imagePath, 0, 0, []);
            }

            List<GemStashTemplate> templates = [];
            foreach (string templateName in metadata.TemplateNames)
            {
                string templatePath = Path.Combine(artifactDirectory, "templates", $"{MakeSafeName(templateName)}.png");
                if (File.Exists(templatePath))
                {
                    templates.Add(new GemStashTemplate(templateName, templatePath));
                }
            }

            using Bitmap bitmap = new(imagePath);
            Rectangle screenGrid = new(metadata.ScreenGrid.Left, metadata.ScreenGrid.Top, metadata.ScreenGrid.Width, metadata.ScreenGrid.Height);
            GemStashInventoryScanResult scan = GemStashInventoryClassifier.Scan(bitmap, screenGrid, templates, metadata.Threshold);
            return new GemStashInventoryReplayRunResult(
                true,
                "Loaded",
                imagePath,
                scan.Targets.Count,
                scan.Candidates.Count,
                scan.Targets.Select(GemStashInventoryReplayTarget.FromTarget).ToArray());
        }

        private static string ResolveMetadataPath(string artifactPath, out string artifactDirectory)
        {
            string fullPath = Path.GetFullPath(artifactPath);
            if (Directory.Exists(fullPath))
            {
                artifactDirectory = fullPath;
                return Path.Combine(artifactDirectory, "metadata.json");
            }

            artifactDirectory = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
            return fullPath;
        }

        private static void SaveTemplates(string artifactDirectory, GemStashInventoryScanResult scan)
        {
            string templatesDirectory = Path.Combine(artifactDirectory, "templates");
            Directory.CreateDirectory(templatesDirectory);
            for (int i = 0; i < scan.TemplateNames.Count && i < scan.TemplatePaths.Count; i++)
            {
                string source = scan.TemplatePaths[i];
                if (!File.Exists(source))
                {
                    continue;
                }

                string safeName = MakeSafeName(scan.TemplateNames[i]);
                File.Copy(source, Path.Combine(templatesDirectory, $"{safeName}.png"), overwrite: true);
            }
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

    internal sealed record GemStashInventoryReplayRunResult(
        bool Loaded,
        string Reason,
        string ImagePath,
        int TargetCount,
        int CandidateCount,
        IReadOnlyList<GemStashInventoryReplayTarget> Targets);

    internal sealed record GemStashInventoryReplayMetadata(
        string ArtifactType,
        DateTime SavedAtUtc,
        string Phase,
        string ClassifierVersion,
        int Columns,
        int Rows,
        InventoryReplayRectangle ScreenGrid,
        string ImageFile,
        double Threshold,
        IReadOnlyList<string> TemplateNames,
        IReadOnlyList<string> InvalidTemplates,
        int TargetCount,
        int CandidateCount,
        IReadOnlyList<GemStashInventoryReplayTarget> Targets,
        IReadOnlyList<GemStashInventoryReplayCandidate> Candidates);

    internal sealed record GemStashInventoryReplayLogEvent(
        string ArtifactType,
        DateTime SavedAtUtc,
        string Phase,
        string ClassifierVersion,
        int Columns,
        int Rows,
        InventoryReplayRectangle ScreenGrid,
        double Threshold,
        IReadOnlyList<string> TemplateNames,
        IReadOnlyList<string> InvalidTemplates,
        int TargetCount,
        int CandidateCount,
        IReadOnlyList<GemStashInventoryReplayTarget> Targets,
        IReadOnlyList<GemStashInventoryReplayCandidate> Candidates);

    internal sealed record InventoryReplayRectangle(int Left, int Top, int Width, int Height);

    internal sealed record InventoryReplayPoint(int X, int Y);

    internal sealed record GemStashInventoryReplayTarget(
        int Row,
        int Column,
        InventoryReplayPoint ScreenPoint,
        string Template,
        double Confidence)
    {
        public static GemStashInventoryReplayTarget FromTarget(GemStashInventorySlotTarget target)
        {
            return new GemStashInventoryReplayTarget(
                target.Row,
                target.Column,
                new InventoryReplayPoint(target.ScreenPoint.X, target.ScreenPoint.Y),
                target.Template,
                target.Confidence);
        }
    }

    internal sealed record GemStashInventoryReplayCandidate(
        int Row,
        int Column,
        InventoryReplayPoint ScreenPoint,
        bool Accepted,
        string Reason,
        string BestTemplate,
        double Confidence)
    {
        public static GemStashInventoryReplayCandidate FromCandidate(GemStashInventorySlotCandidate candidate)
        {
            return new GemStashInventoryReplayCandidate(
                candidate.Row,
                candidate.Column,
                new InventoryReplayPoint(candidate.ScreenPoint.X, candidate.ScreenPoint.Y),
                candidate.Accepted,
                candidate.Reason,
                candidate.BestTemplate,
                candidate.Confidence);
        }
    }
}

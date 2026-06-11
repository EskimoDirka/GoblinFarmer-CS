using System.Drawing.Imaging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoblinFarmer
{
    internal sealed record ImageRecognitionSampleCandidate(
        int Rank,
        string TargetLabel,
        string Source,
        double Confidence,
        string Reason,
        string TemplateName,
        string TemplatePath,
        Rectangle CropRegion,
        Rectangle ScreenRegion,
        byte[]? CropPng,
        bool DomainPromotable = true,
        string DomainSkipReason = "",
        IReadOnlyDictionary<string, string>? Metadata = null);

    internal sealed record ImageRecognitionSamplePromotionRequest(
        string Domain,
        string AcceptedActionId,
        string SessionId,
        string CaptureRootDirectory,
        string PromotionRootDirectory,
        bool CaptureEnabled,
        bool PromotionEnabled,
        int TopCandidateCount,
        int RetentionCount,
        IReadOnlyList<ImageRecognitionSampleCandidate> Candidates,
        IReadOnlyDictionary<string, string>? Metadata = null);

    internal sealed record ImageRecognitionSampleQuality(
        bool Promotable,
        string SkipReason,
        int Width,
        int Height,
        double MeanBrightness,
        double BrightnessStdDev,
        double EdgeScore);

    internal sealed record ImageRecognitionSampleCandidateResult(
        int Rank,
        string TargetLabel,
        string Source,
        double Confidence,
        string Reason,
        string TemplateName,
        string TemplatePath,
        string CapturePath,
        Rectangle CropRegion,
        Rectangle ScreenRegion,
        ImageRecognitionSampleQuality Quality,
        IReadOnlyDictionary<string, string>? Metadata);

    internal sealed record ImageRecognitionSamplePromotionResult(
        bool Captured,
        string CaptureDirectory,
        string MetadataPath,
        ImageRecognitionSampleCandidateResult? Selected,
        string SelectionReason,
        bool Promoted,
        string PromotionPath,
        string PromotionMetadataPath,
        string PromotionSkipReason,
        IReadOnlyList<ImageRecognitionSampleCandidateResult> Candidates);

    internal static class ImageRecognitionBestSamplePromoter
    {
        public const string MetadataFileName = "metadata.json";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static ImageRecognitionSamplePromotionResult CaptureSelectAndPromote(ImageRecognitionSamplePromotionRequest request)
        {
            string domain = NormalizeToken(request.Domain, "UnknownDomain");
            string actionId = NormalizeToken(request.AcceptedActionId, $"accepted-{DateTime.UtcNow:yyyyMMddHHmmssfff}");
            string sessionId = NormalizeToken(request.SessionId, DateTime.UtcNow.ToString("yyyyMMdd"));
            int topCount = Math.Clamp(request.TopCandidateCount <= 0 ? 3 : request.TopCandidateCount, 1, 10);
            IReadOnlyList<ImageRecognitionSampleCandidate> candidates = request.Candidates
                .OrderBy(candidate => candidate.Rank)
                .ThenByDescending(candidate => candidate.Confidence)
                .Take(topCount)
                .ToArray();

            if (!request.CaptureEnabled)
            {
                AppLogger.Info(
                    "BestImageRecognitionCandidatePromotionSkipped: " +
                    $"domain={Log(domain)}; " +
                    $"acceptedActionId={Log(actionId)}; " +
                    "reason=CaptureDisabled");
                return new ImageRecognitionSamplePromotionResult(false, "", "", null, "", false, "", "", "CaptureDisabled", []);
            }

            if (candidates.Count == 0)
            {
                AppLogger.Info(
                    "BestImageRecognitionCandidatePromotionSkipped: " +
                    $"domain={Log(domain)}; " +
                    $"acceptedActionId={Log(actionId)}; " +
                    "reason=NoCandidates");
                return new ImageRecognitionSamplePromotionResult(false, "", "", null, "", false, "", "", "NoCandidates", []);
            }

            string captureDirectory = Path.Combine(request.CaptureRootDirectory, sessionId, actionId);
            Directory.CreateDirectory(captureDirectory);

            List<ImageRecognitionSampleCandidateResult> savedCandidates = [];
            foreach (ImageRecognitionSampleCandidate candidate in candidates)
            {
                ImageRecognitionSampleQuality quality = AnalyzeQuality(candidate);
                string capturePath = "";
                if (candidate.CropPng is { Length: > 0 })
                {
                    capturePath = Path.Combine(
                        captureDirectory,
                        $"{candidate.Rank:00}_{NormalizeToken(candidate.TargetLabel, "Target")}_{NormalizeToken(candidate.Source, "Source")}_{candidate.Confidence:0.000}.png");
                    File.WriteAllBytes(capturePath, candidate.CropPng);
                }

                savedCandidates.Add(new ImageRecognitionSampleCandidateResult(
                    candidate.Rank,
                    candidate.TargetLabel,
                    candidate.Source,
                    candidate.Confidence,
                    candidate.Reason,
                    candidate.TemplateName,
                    candidate.TemplatePath,
                    capturePath,
                    candidate.CropRegion,
                    candidate.ScreenRegion,
                    quality,
                    candidate.Metadata));
            }

            ImageRecognitionSampleCandidateResult? selected = SelectBest(savedCandidates, out string selectionReason);
            string promotionSkipReason = "";
            string promotionPath = "";
            string promotionMetadataPath = "";
            bool promoted = false;

            AppLogger.Info(
                "TopImageRecognitionCandidatesCaptured: " +
                $"domain={Log(domain)}; " +
                $"acceptedActionId={Log(actionId)}; " +
                $"sessionId={Log(sessionId)}; " +
                $"candidateCount={savedCandidates.Count}; " +
                $"captureDirectory={Log(captureDirectory)}");

            if (selected != null)
            {
                AppLogger.Info(
                    "BestImageRecognitionCandidateSelected: " +
                    $"domain={Log(domain)}; " +
                    $"acceptedActionId={Log(actionId)}; " +
                    $"targetLabel={Log(selected.TargetLabel)}; " +
                    $"source={Log(selected.Source)}; " +
                    $"rank={selected.Rank}; " +
                    $"confidence={selected.Confidence:0.000}; " +
                    $"qualityScore={selected.Quality.EdgeScore + selected.Quality.BrightnessStdDev:0.000}; " +
                    $"selectionReason={Log(selectionReason)}");
            }

            if (selected == null)
            {
                promotionSkipReason = "NoPromotableCandidate";
            }
            else if (!request.PromotionEnabled)
            {
                promotionSkipReason = "PromotionDisabled";
            }
            else if (string.IsNullOrWhiteSpace(selected.CapturePath) || !File.Exists(selected.CapturePath))
            {
                promotionSkipReason = "SourceCropUnavailable";
            }
            else
            {
                string targetDirectory = Path.Combine(
                    request.PromotionRootDirectory,
                    NormalizeToken(selected.TargetLabel, "Target"),
                    NormalizeToken(selected.Source, "Source"));
                Directory.CreateDirectory(targetDirectory);
                string baseName = $"{NormalizeToken(selected.TargetLabel, "Target")}_{NormalizeToken(selected.Source, "Source")}_{DateTime.Now:yyyyMMdd_HHmmss_fff}_score{selected.Confidence:0.000}";
                promotionPath = UniqueFilePath(targetDirectory, baseName, ".png");
                File.Copy(selected.CapturePath, promotionPath, overwrite: false);
                promotionMetadataPath = Path.ChangeExtension(promotionPath, ".json");
                File.WriteAllText(promotionMetadataPath, JsonSerializer.Serialize(new
                {
                    domain,
                    acceptedActionId = actionId,
                    sessionId,
                    promotedUtc = DateTime.UtcNow,
                    selected,
                    request.Metadata,
                }, JsonOptions));
                promoted = true;
                AppLogger.Info(
                    "BestImageRecognitionCandidatePromoted: " +
                    $"domain={Log(domain)}; " +
                    $"acceptedActionId={Log(actionId)}; " +
                    $"targetLabel={Log(selected.TargetLabel)}; " +
                    $"source={Log(selected.Source)}; " +
                    $"rank={selected.Rank}; " +
                    $"confidence={selected.Confidence:0.000}; " +
                    $"path={Log(promotionPath)}; " +
                    $"metadataPath={Log(promotionMetadataPath)}");
            }

            if (!promoted)
            {
                AppLogger.Info(
                    "BestImageRecognitionCandidatePromotionSkipped: " +
                    $"domain={Log(domain)}; " +
                    $"acceptedActionId={Log(actionId)}; " +
                    $"targetLabel={Log(selected?.TargetLabel ?? "")}; " +
                    $"source={Log(selected?.Source ?? "")}; " +
                    $"rank={(selected == null ? 0 : selected.Rank)}; " +
                    $"confidence={(selected == null ? 0 : selected.Confidence):0.000}; " +
                    $"reason={Log(promotionSkipReason)}");
            }

            string metadataPath = Path.Combine(captureDirectory, MetadataFileName);
            ImageRecognitionSamplePromotionResult result = new(
                true,
                captureDirectory,
                metadataPath,
                selected,
                selectionReason,
                promoted,
                promotionPath,
                promotionMetadataPath,
                promotionSkipReason,
                savedCandidates);
            File.WriteAllText(metadataPath, JsonSerializer.Serialize(new
            {
                domain,
                acceptedActionId = actionId,
                sessionId,
                capturedUtc = DateTime.UtcNow,
                request.Metadata,
                result,
            }, JsonOptions));
            CleanupOldCaptureSets(request.CaptureRootDirectory, request.RetentionCount);
            return result;
        }

        public static ImageRecognitionSampleQuality AnalyzeQuality(ImageRecognitionSampleCandidate candidate)
        {
            if (!candidate.DomainPromotable)
            {
                return new ImageRecognitionSampleQuality(false, string.IsNullOrWhiteSpace(candidate.DomainSkipReason) ? "DomainRejected" : candidate.DomainSkipReason, 0, 0, 0, 0, 0);
            }

            if (candidate.CropPng == null || candidate.CropPng.Length == 0)
            {
                return new ImageRecognitionSampleQuality(false, "FailedCrop", 0, 0, 0, 0, 0);
            }

            try
            {
                using MemoryStream stream = new(candidate.CropPng);
                using Bitmap bitmap = new(stream);
                if (bitmap.Width < 8 || bitmap.Height < 8)
                {
                    return new ImageRecognitionSampleQuality(false, "TooSmall", bitmap.Width, bitmap.Height, 0, 0, 0);
                }

                (double mean, double stdDev, double edgeScore) = MeasureQuality(bitmap);
                if (stdDev < 2.0)
                {
                    return new ImageRecognitionSampleQuality(false, "BlankOrLowVariance", bitmap.Width, bitmap.Height, mean, stdDev, edgeScore);
                }

                if (stdDev < 7.0 && edgeScore < 0.50)
                {
                    return new ImageRecognitionSampleQuality(false, "Blurry", bitmap.Width, bitmap.Height, mean, stdDev, edgeScore);
                }

                return new ImageRecognitionSampleQuality(true, "", bitmap.Width, bitmap.Height, mean, stdDev, edgeScore);
            }
            catch
            {
                return new ImageRecognitionSampleQuality(false, "FailedCrop", 0, 0, 0, 0, 0);
            }
        }

        public static CleanupResult CleanupOldCaptureSets(string captureRootDirectory, int retentionCount)
        {
            if (retentionCount <= 0)
            {
                return new CleanupResult(0, 0, 0);
            }

            if (string.IsNullOrWhiteSpace(captureRootDirectory) || !Directory.Exists(captureRootDirectory))
            {
                return new CleanupResult(0, 0, 0);
            }

            DirectoryInfo root = new(captureRootDirectory);
            DirectoryInfo[] setDirectories = root
                .EnumerateDirectories("*", SearchOption.AllDirectories)
                .Where(directory => File.Exists(Path.Combine(directory.FullName, MetadataFileName)))
                .OrderByDescending(directory => directory.LastWriteTimeUtc)
                .ThenBy(directory => directory.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            int deleted = 0;
            int skipped = 0;
            foreach (DirectoryInfo directory in setDirectories.Skip(retentionCount))
            {
                try
                {
                    directory.Delete(recursive: true);
                    deleted++;
                }
                catch (Exception ex)
                {
                    skipped++;
                    AppLogger.Error($"Image recognition candidate retention delete skipped: path={directory.FullName}", ex);
                }
            }

            CleanupEmptyDirectories(root);
            return new CleanupResult(setDirectories.Length, deleted, setDirectories.Length - deleted, skipped);
        }

        public static byte[] EncodePng(Bitmap bitmap, Rectangle cropRegion)
        {
            Rectangle bounds = new(Point.Empty, bitmap.Size);
            Rectangle crop = Rectangle.Intersect(bounds, cropRegion);
            if (crop.Width <= 0 || crop.Height <= 0)
            {
                return [];
            }

            using Bitmap cropped = bitmap.Clone(crop, PixelFormat.Format32bppArgb);
            using MemoryStream stream = new();
            cropped.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        private static ImageRecognitionSampleCandidateResult? SelectBest(
            IReadOnlyList<ImageRecognitionSampleCandidateResult> candidates,
            out string selectionReason)
        {
            ImageRecognitionSampleCandidateResult? selected = candidates
                .Where(candidate => candidate.Quality.Promotable)
                .OrderByDescending(candidate => candidate.Confidence)
                .ThenByDescending(candidate => candidate.Quality.BrightnessStdDev + candidate.Quality.EdgeScore)
                .ThenBy(candidate => candidate.Rank)
                .ThenBy(candidate => candidate.Source, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.TemplateName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.CapturePath, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            selectionReason = selected == null
                ? "NoPromotableCandidate"
                : "HighestConfidenceThenQualityThenStableTieBreakers";
            return selected;
        }

        private static (double Mean, double StdDev, double EdgeScore) MeasureQuality(Bitmap bitmap)
        {
            long count = 0;
            double sum = 0;
            double sumSquares = 0;
            double edge = 0;
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    double gray = (color.R + color.G + color.B) / 3.0;
                    sum += gray;
                    sumSquares += gray * gray;
                    count++;
                    if (x > 0)
                    {
                        Color left = bitmap.GetPixel(x - 1, y);
                        edge += Math.Abs(gray - ((left.R + left.G + left.B) / 3.0));
                    }

                    if (y > 0)
                    {
                        Color up = bitmap.GetPixel(x, y - 1);
                        edge += Math.Abs(gray - ((up.R + up.G + up.B) / 3.0));
                    }
                }
            }

            double mean = count == 0 ? 0 : sum / count;
            double variance = count == 0 ? 0 : Math.Max(0, (sumSquares / count) - (mean * mean));
            double edgeDenominator = Math.Max(1, (bitmap.Width - 1) * bitmap.Height + bitmap.Width * (bitmap.Height - 1));
            return (mean, Math.Sqrt(variance), edge / edgeDenominator);
        }

        private static string UniqueFilePath(string directory, string baseName, string extension)
        {
            string candidate = Path.Combine(directory, $"{baseName}{extension}");
            for (int i = 2; File.Exists(candidate) || File.Exists(Path.ChangeExtension(candidate, ".json")); i++)
            {
                candidate = Path.Combine(directory, $"{baseName}_{i}{extension}");
            }

            return candidate;
        }

        private static void CleanupEmptyDirectories(DirectoryInfo root)
        {
            foreach (DirectoryInfo directory in root.EnumerateDirectories("*", SearchOption.AllDirectories).OrderByDescending(directory => directory.FullName.Length))
            {
                try
                {
                    if (!directory.EnumerateFileSystemInfos().Any())
                    {
                        directory.Delete();
                    }
                }
                catch
                {
                }
            }
        }

        private static string NormalizeToken(string value, string fallback)
        {
            string token = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                token = token.Replace(invalid, '_');
            }

            token = token
                .Replace(' ', '_')
                .Replace(';', '_')
                .Replace(':', '_')
                .Replace('=', '_');
            return string.IsNullOrWhiteSpace(token) ? fallback : token;
        }

        private static string Log(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "None"
                : value.Replace(";", ",").Replace(Environment.NewLine, " ").Trim();
        }
    }
}

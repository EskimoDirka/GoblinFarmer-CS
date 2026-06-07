using System.Drawing;

namespace GoblinFarmer
{
    internal interface IGoblinEvidenceFrameSource
    {
        GoblinEvidenceScanContext? TryCreateScanContext(string source, Rectangle referenceRegion, string reason);
    }

    internal sealed class LiveGoblinEvidenceFrameSource(Func<Rectangle, Rectangle?> resolveScreenRegion) : IGoblinEvidenceFrameSource
    {
        public GoblinEvidenceScanContext? TryCreateScanContext(string source, Rectangle referenceRegion, string reason)
        {
            Rectangle? resolvedScreenRegion = resolveScreenRegion(referenceRegion);
            if (resolvedScreenRegion == null)
            {
                return null;
            }

            Rectangle screenRegion = resolvedScreenRegion.Value;
            if (screenRegion.Width <= 0 || screenRegion.Height <= 0)
            {
                return null;
            }

            Bitmap screenshot = new(screenRegion.Width, screenRegion.Height);
            try
            {
                using (Graphics graphics = Graphics.FromImage(screenshot))
                {
                    graphics.CopyFromScreen(screenRegion.Left, screenRegion.Top, 0, 0, screenshot.Size);
                }

                return GoblinEvidenceScanContext.FromBitmap(referenceRegion, screenRegion, screenshot, reason);
            }
            catch
            {
                screenshot.Dispose();
                throw;
            }
        }
    }

    internal sealed class FixtureGoblinEvidenceFrameSource : IGoblinEvidenceFrameSource
    {
        private readonly Dictionary<string, string> imagePathBySource;

        public FixtureGoblinEvidenceFrameSource(IReadOnlyDictionary<string, string> imagePathBySource)
        {
            this.imagePathBySource = imagePathBySource
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                .ToDictionary(
                    pair => NormalizeSource(pair.Key),
                    pair => pair.Value,
                    StringComparer.OrdinalIgnoreCase);
        }

        public static FixtureGoblinEvidenceFrameSource FromJournalAndMinimap(string? journalPath, string? minimapPath)
        {
            Dictionary<string, string> paths = new(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(journalPath))
            {
                paths["JournalCandidate"] = journalPath;
            }

            if (!string.IsNullOrWhiteSpace(minimapPath))
            {
                paths["MinimapCandidate"] = minimapPath;
            }

            return new FixtureGoblinEvidenceFrameSource(paths);
        }

        public GoblinEvidenceScanContext? TryCreateScanContext(string source, Rectangle referenceRegion, string reason)
        {
            string normalizedSource = NormalizeSource(source);
            if (!imagePathBySource.TryGetValue(normalizedSource, out string? imagePath) ||
                string.IsNullOrWhiteSpace(imagePath) ||
                !File.Exists(imagePath))
            {
                return null;
            }

            using Bitmap loaded = new(imagePath);
            Bitmap screenshot = new(loaded);
            Rectangle screenRegion = new(0, 0, screenshot.Width, screenshot.Height);
            try
            {
                return GoblinEvidenceScanContext.FromBitmap(referenceRegion, screenRegion, screenshot, reason);
            }
            catch
            {
                screenshot.Dispose();
                throw;
            }
        }

        private static string NormalizeSource(string source)
        {
            return (source ?? "").Contains("Minimap", StringComparison.OrdinalIgnoreCase)
                ? "MinimapCandidate"
                : "JournalCandidate";
        }
    }

    internal sealed class GoblinEvidenceScanContext(
        Rectangle referenceRegion,
        Rectangle screenRegion,
        Bitmap screenshot,
        OpenCvSharp.Mat screenMat,
        string reason) : IDisposable
    {
        public Rectangle ReferenceRegion { get; } = referenceRegion;
        public Rectangle ScreenRegion { get; } = screenRegion;
        public Bitmap Screenshot { get; } = screenshot;
        public OpenCvSharp.Mat ScreenMat { get; } = screenMat;
        public string Reason { get; } = reason;

        public static GoblinEvidenceScanContext FromBitmap(
            Rectangle referenceRegion,
            Rectangle screenRegion,
            Bitmap screenshot,
            string reason)
        {
            OpenCvSharp.Mat? rawScreenMat = null;
            OpenCvSharp.Mat? screenMat = null;
            try
            {
                rawScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
                if (rawScreenMat.Channels() == 4)
                {
                    screenMat = new OpenCvSharp.Mat();
                    OpenCvSharp.Cv2.CvtColor(rawScreenMat, screenMat, OpenCvSharp.ColorConversionCodes.BGRA2BGR);
                }
                else if (rawScreenMat.Channels() == 3)
                {
                    screenMat = rawScreenMat.Clone();
                }
                else
                {
                    screenMat = new OpenCvSharp.Mat();
                    OpenCvSharp.Cv2.CvtColor(rawScreenMat, screenMat, OpenCvSharp.ColorConversionCodes.GRAY2BGR);
                }

                return new GoblinEvidenceScanContext(referenceRegion, screenRegion, screenshot, screenMat, reason);
            }
            catch
            {
                screenMat?.Dispose();
                screenshot.Dispose();
                throw;
            }
            finally
            {
                rawScreenMat?.Dispose();
            }
        }

        public void Dispose()
        {
            ScreenMat.Dispose();
            Screenshot.Dispose();
        }
    }

    internal static class GoblinEvidenceFrameTemplateMatcher
    {
        public static GoblinEvidenceReplayCandidate? DetectBestCandidate(
            IGoblinEvidenceFrameSource frameSource,
            IReadOnlyList<GoblinEvidenceTemplateRequirement> templates,
            Func<GoblinEvidenceTemplateRequirement, string> templatePathResolver,
            Rectangle referenceRegion,
            string reason)
        {
            if (templates.Count == 0)
            {
                return null;
            }

            string source = templates[0].Source;
            using GoblinEvidenceScanContext? scanContext = frameSource.TryCreateScanContext(
                source,
                referenceRegion,
                reason);
            if (scanContext == null)
            {
                return null;
            }

            GoblinEvidenceTemplateRequirement? bestTemplate = null;
            string bestTemplatePath = "";
            GoblinEvidenceTemplateMatch bestMatch = new(0, Point.Empty, Point.Empty, Size.Empty);
            foreach (GoblinEvidenceTemplateRequirement template in templates)
            {
                string templatePath = templatePathResolver(template);
                if (!File.Exists(templatePath))
                {
                    continue;
                }

                using OpenCvSharp.Mat templateMat = OpenCvSharp.Cv2.ImRead(templatePath, OpenCvSharp.ImreadModes.Color);
                GoblinEvidenceTemplateMatch match = MatchTemplate(scanContext, templateMat);
                if (bestTemplate == null || match.Confidence > bestMatch.Confidence)
                {
                    bestTemplate = template;
                    bestTemplatePath = templatePath;
                    bestMatch = match;
                }
            }

            if (bestTemplate == null)
            {
                return null;
            }

            return new GoblinEvidenceReplayCandidate(
                bestTemplate,
                bestTemplatePath,
                bestMatch,
                bestMatch.Confidence >= bestTemplate.Threshold);
        }

        public static GoblinEvidenceTemplateMatch MatchTemplate(
            GoblinEvidenceScanContext scanContext,
            OpenCvSharp.Mat templateMat,
            Func<Bitmap, Point, Size, GoblinMinimapColorClassification>? classifyMinimapColor = null)
        {
            if (templateMat.Empty() ||
                templateMat.Width > scanContext.ScreenMat.Width ||
                templateMat.Height > scanContext.ScreenMat.Height)
            {
                return new GoblinEvidenceTemplateMatch(0, Point.Empty, Point.Empty, new Size(templateMat.Width, templateMat.Height));
            }

            using OpenCvSharp.Mat result = new();
            OpenCvSharp.Cv2.MatchTemplate(scanContext.ScreenMat, templateMat, result, OpenCvSharp.TemplateMatchModes.CCoeffNormed);
            OpenCvSharp.Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);
            Point matchPoint = new(maxLoc.X, maxLoc.Y);
            Point screenMatchPoint = new(scanContext.ScreenRegion.Left + maxLoc.X, scanContext.ScreenRegion.Top + maxLoc.Y);
            Size templateSize = new(templateMat.Width, templateMat.Height);
            GoblinMinimapColorClassification minimapColor = classifyMinimapColor == null
                ? GoblinMinimapColorClassification.Empty
                : classifyMinimapColor(scanContext.Screenshot, matchPoint, templateSize);
            return new GoblinEvidenceTemplateMatch(maxVal, matchPoint, screenMatchPoint, templateSize, minimapColor);
        }

        public static GoblinEvidenceCandidate? TryDetectSingleTemplateCandidate(
            IGoblinEvidenceFrameSource frameSource,
            GoblinEvidenceTemplateRequirement template,
            string templatePath,
            Rectangle referenceRegion,
            out GoblinEvidenceTemplateMatch match)
        {
            match = new GoblinEvidenceTemplateMatch(0, Point.Empty, Point.Empty, Size.Empty);
            if (!File.Exists(templatePath))
            {
                return null;
            }

            using GoblinEvidenceScanContext? scanContext = frameSource.TryCreateScanContext(
                template.Source,
                referenceRegion,
                "FixtureSingleTemplateCandidate");
            if (scanContext == null)
            {
                return null;
            }

            using OpenCvSharp.Mat templateMat = OpenCvSharp.Cv2.ImRead(templatePath, OpenCvSharp.ImreadModes.Color);
            match = MatchTemplate(scanContext, templateMat);
            if (match.Confidence < template.Threshold)
            {
                return null;
            }

            return new GoblinEvidenceCandidate(
                template.Type,
                match.Confidence,
                template.Source,
                $"Template={template.FileName}; Kind={template.Kind}; Threshold={template.Threshold:0.000}; MatchPoint={match.MatchPoint.X},{match.MatchPoint.Y}; ScreenMatchPoint={match.ScreenMatchPoint.X},{match.ScreenMatchPoint.Y}",
                template.GoblinType);
        }
    }

    internal sealed record GoblinEvidenceReplayCandidate(
        GoblinEvidenceTemplateRequirement Template,
        string TemplatePath,
        GoblinEvidenceTemplateMatch Match,
        bool PassedThreshold);
}

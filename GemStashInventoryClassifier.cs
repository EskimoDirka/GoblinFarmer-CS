using System.Drawing;
using OpenCvSharp;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    internal sealed record GemStashTemplate(string Name, string Path);

    internal sealed record GemStashInventorySlotCandidate(
        int Row,
        int Column,
        DrawingPoint ScreenPoint,
        bool Accepted,
        string Reason,
        string BestTemplate,
        double Confidence);

    internal sealed record GemStashInventorySlotTarget(
        int Row,
        int Column,
        DrawingPoint ScreenPoint,
        string Template,
        double Confidence);

    internal sealed record GemStashInventoryScanResult(
        IReadOnlyList<GemStashInventorySlotTarget> Targets,
        IReadOnlyList<GemStashInventorySlotCandidate> Candidates,
        IReadOnlyList<string> TemplateNames,
        IReadOnlyList<string> TemplatePaths,
        IReadOnlyList<string> InvalidTemplates);

    internal static class GemStashTemplateCatalog
    {
        public static IReadOnlyList<GemStashTemplate> Load(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return [];
            }

            return Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly)
                .Where(IsGemTemplateFile)
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                .Select(path => new GemStashTemplate(Path.GetFileNameWithoutExtension(path), path))
                .ToArray();
        }

        private static bool IsGemTemplateFile(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            return !name.Contains("scan region", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("coordinate", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("stash", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("tab", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("placement", StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static class GemStashInventoryClassifier
    {
        public const string ClassifierVersion = "GemStashTemplateV1";

        private sealed record LoadedTemplate(string Name, Mat Mat) : IDisposable
        {
            public void Dispose()
            {
                Mat.Dispose();
            }
        }

        public static GemStashInventoryScanResult Scan(
            Bitmap inventoryGrid,
            Rectangle screenGrid,
            IReadOnlyList<GemStashTemplate> templates,
            double threshold)
        {
            if (inventoryGrid.Width <= 0 || inventoryGrid.Height <= 0)
            {
                return new GemStashInventoryScanResult([], [], templates.Select(template => template.Name).ToArray(), templates.Select(template => template.Path).ToArray(), []);
            }

            int slotWidth = inventoryGrid.Width / InventoryGridLayout.Columns;
            int slotHeight = inventoryGrid.Height / InventoryGridLayout.Rows;
            if (slotWidth <= 0 || slotHeight <= 0)
            {
                return new GemStashInventoryScanResult([], [], templates.Select(template => template.Name).ToArray(), templates.Select(template => template.Path).ToArray(), []);
            }

            List<string> invalidTemplates = [];
            List<LoadedTemplate> loadedTemplates = [];
            try
            {
                foreach (GemStashTemplate template in templates)
                {
                    Mat mat = Cv2.ImRead(template.Path, ImreadModes.Color);
                    if (mat.Empty() || mat.Width <= 0 || mat.Height <= 0 || mat.Width > slotWidth || mat.Height > slotHeight)
                    {
                        invalidTemplates.Add(template.Name);
                        mat.Dispose();
                        continue;
                    }

                    loadedTemplates.Add(new LoadedTemplate(template.Name, mat));
                }

                return ScanLoaded(
                    inventoryGrid,
                    screenGrid,
                    loadedTemplates,
                    templates.Select(template => template.Name).ToArray(),
                    templates.Select(template => template.Path).ToArray(),
                    invalidTemplates,
                    threshold);
            }
            finally
            {
                foreach (LoadedTemplate template in loadedTemplates)
                {
                    template.Dispose();
                }
            }
        }

        private static GemStashInventoryScanResult ScanLoaded(
            Bitmap inventoryGrid,
            Rectangle screenGrid,
            IReadOnlyList<LoadedTemplate> loadedTemplates,
            IReadOnlyList<string> templateNames,
            IReadOnlyList<string> templatePaths,
            IReadOnlyList<string> invalidTemplates,
            double threshold)
        {
            List<GemStashInventorySlotTarget> targets = [];
            List<GemStashInventorySlotCandidate> candidates = [];

            using Mat rawGrid = OpenCvSharp.Extensions.BitmapConverter.ToMat(inventoryGrid);
            using Mat grid = new();
            Cv2.CvtColor(rawGrid, grid, ColorConversionCodes.BGRA2BGR);

            for (int row = 0; row < InventoryGridLayout.Rows; row++)
            {
                for (int column = 0; column < InventoryGridLayout.Columns; column++)
                {
                    Rectangle local = InventoryGridLayout.SlotRectangle(inventoryGrid, row, column);
                    DrawingPoint screenPoint = InventoryGridLayout.SlotScreenPoint(screenGrid, local);
                    string bestTemplate = "";
                    double bestConfidence = 0;

                    if (loadedTemplates.Count > 0)
                    {
                        using Mat slot = new(grid, new OpenCvSharp.Rect(local.Left, local.Top, local.Width, local.Height));
                        foreach (LoadedTemplate template in loadedTemplates)
                        {
                            double confidence = TemplateConfidence(slot, template.Mat);
                            if (confidence > bestConfidence)
                            {
                                bestConfidence = confidence;
                                bestTemplate = template.Name;
                            }
                        }
                    }

                    bool accepted = bestConfidence >= threshold;
                    string reason = loadedTemplates.Count == 0
                        ? "NoValidGemTemplates"
                        : accepted
                            ? "GemTemplateMatched"
                            : "BelowThreshold";
                    candidates.Add(new GemStashInventorySlotCandidate(
                        row + 1,
                        column + 1,
                        screenPoint,
                        accepted,
                        reason,
                        bestTemplate,
                        bestConfidence));

                    if (accepted)
                    {
                        targets.Add(new GemStashInventorySlotTarget(
                            row + 1,
                            column + 1,
                            screenPoint,
                            bestTemplate,
                            bestConfidence));
                    }
                }
            }

            return new GemStashInventoryScanResult(targets, candidates, templateNames, templatePaths, invalidTemplates);
        }

        private static double TemplateConfidence(Mat slot, Mat template)
        {
            using Mat result = new();
            Cv2.MatchTemplate(slot, template, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
            return maxVal;
        }
    }
}

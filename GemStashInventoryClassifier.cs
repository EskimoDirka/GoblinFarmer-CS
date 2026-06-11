using System.Drawing;
using OpenCvSharp;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    internal sealed record GemStashTemplate(string Name, string Path, string Source = "Template");

    internal sealed record GemStashInventorySlotCandidate(
        int Row,
        int Column,
        DrawingPoint ScreenPoint,
        bool Accepted,
        string Reason,
        string BestTemplate,
        double Confidence,
        string BestTemplatePath = "",
        Rectangle LocalRegion = default,
        Rectangle ScreenRegion = default,
        byte[]? CropPng = null);

    internal sealed record GemStashInventorySlotTarget(
        int Row,
        int Column,
        DrawingPoint ScreenPoint,
        string Template,
        double Confidence);

    internal sealed record GemStashInventorySlotMetrics(
        int RegularGemPixels,
        int StackCountTextPixels,
        int InnerSaturatedPixels);

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

            List<GemStashTemplate> templates = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly)
                .Where(IsGemTemplateFile)
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                .Select(path => new GemStashTemplate(Path.GetFileNameWithoutExtension(path), path))
                .ToList();
            string promoted = Path.Combine(folder, "Promoted");
            if (Directory.Exists(promoted))
            {
                templates.AddRange(Directory.GetFiles(promoted, "*.png", SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .Select(TryLoadPromotedTemplate)
                    .OfType<GemStashTemplate>());
            }

            return templates;
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

        private static GemStashTemplate? TryLoadPromotedTemplate(string path)
        {
            string metadataPath = Path.ChangeExtension(path, ".json");
            if (!File.Exists(metadataPath))
            {
                return null;
            }

            try
            {
                using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(metadataPath));
                System.Text.Json.JsonElement selected = document.RootElement.TryGetProperty("selected", out System.Text.Json.JsonElement selectedElement)
                    ? selectedElement
                    : document.RootElement;
                string target = selected.TryGetProperty("targetLabel", out System.Text.Json.JsonElement targetElement) && targetElement.ValueKind == System.Text.Json.JsonValueKind.String
                    ? targetElement.GetString() ?? ""
                    : "";
                string source = selected.TryGetProperty("source", out System.Text.Json.JsonElement sourceElement) && sourceElement.ValueKind == System.Text.Json.JsonValueKind.String
                    ? sourceElement.GetString() ?? "Promoted"
                    : "Promoted";
                string gemType = NormalizeKnownGemType(target);
                return string.IsNullOrWhiteSpace(gemType)
                    ? null
                    : new GemStashTemplate(gemType, path, source);
            }
            catch
            {
                return null;
            }
        }

        internal static string NormalizeKnownGemType(string value)
        {
            if (value.Contains("Emerald", StringComparison.OrdinalIgnoreCase))
            {
                return "Emerald";
            }

            if (value.Contains("Ruby", StringComparison.OrdinalIgnoreCase))
            {
                return "Ruby";
            }

            if (value.Contains("Topaz", StringComparison.OrdinalIgnoreCase))
            {
                return "Topaz";
            }

            if (value.Contains("Amethyst", StringComparison.OrdinalIgnoreCase))
            {
                return "Amethyst";
            }

            if (value.Contains("Diamond", StringComparison.OrdinalIgnoreCase))
            {
                return "Diamond";
            }

            return "";
        }
    }

    internal static class GemStashInventoryClassifier
    {
        public const string ClassifierVersion = "GemStashTemplateColorV3";

        private sealed record LoadedTemplate(string Name, string Path, Mat Mat) : IDisposable
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
            return Scan(
                inventoryGrid,
                screenGrid,
                templates,
                threshold,
                unidentifiedLegendaryTemplatePath: "",
                unidentifiedSetTemplatePath: "");
        }

        public static GemStashInventoryScanResult Scan(
            Bitmap inventoryGrid,
            Rectangle screenGrid,
            IReadOnlyList<GemStashTemplate> templates,
            double threshold,
            string unidentifiedLegendaryTemplatePath,
            string unidentifiedSetTemplatePath)
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

                    loadedTemplates.Add(new LoadedTemplate(template.Name, template.Path, mat));
                }

                return ScanLoaded(
                    inventoryGrid,
                    screenGrid,
                    loadedTemplates,
                    templates.Select(template => template.Name).ToArray(),
                    templates.Select(template => template.Path).ToArray(),
                    invalidTemplates,
                    threshold,
                    unidentifiedLegendaryTemplatePath,
                    unidentifiedSetTemplatePath);
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
            double threshold,
            string unidentifiedLegendaryTemplatePath,
            string unidentifiedSetTemplatePath)
        {
            List<GemStashInventorySlotTarget> targets = [];
            List<GemStashInventorySlotCandidate> candidates = [];
            Dictionary<(int Row, int Column), SalvageInventorySlotCandidateDiagnostic> salvageDiagnostics =
                SalvageInventorySlotClassifier.Scan(
                    inventoryGrid,
                    screenGrid,
                    unidentifiedLegendaryTemplatePath,
                    unidentifiedSetTemplatePath)
                    .Candidates
                    .ToDictionary(candidate => (candidate.Row, candidate.Column));

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
                    string bestTemplatePath = "";
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
                                bestTemplatePath = template.Path;
                            }
                        }
                    }

                    GemStashInventorySlotMetrics metrics = MeasureSlot(inventoryGrid, local);
                    bool templateAccepted = bestConfidence >= threshold;
                    bool nearThresholdTemplateAccepted = bestConfidence >= Math.Max(0.70, threshold - 0.06);
                    bool colorAccepted = loadedTemplates.Count > 0 && IsGemColorFallback(metrics, bestConfidence);
                    bool salvageAllowsGem = salvageDiagnostics.TryGetValue((row + 1, column + 1), out SalvageInventorySlotCandidateDiagnostic? salvageDiagnostic) &&
                        salvageDiagnostic.Reason.Equals("RegularGemNonSalvageable", StringComparison.OrdinalIgnoreCase);
                    bool stackVerifiedGem = metrics.StackCountTextPixels >= 12 || metrics.RegularGemPixels >= 500;
                    bool acceptedBeforeSalvageGuard = templateAccepted || colorAccepted || nearThresholdTemplateAccepted;
                    bool accepted = acceptedBeforeSalvageGuard && salvageAllowsGem && stackVerifiedGem;
                    string reason = loadedTemplates.Count == 0
                        ? "NoValidGemTemplates"
                        : templateAccepted
                            ? salvageAllowsGem
                                ? stackVerifiedGem ? "GemTemplateMatched" : "RejectedGemStackVerification"
                                : "RejectedNonGemFootprint"
                            : nearThresholdTemplateAccepted
                                ? salvageAllowsGem
                                    ? stackVerifiedGem ? "GemTemplateNearThresholdVerifiedGem" : "RejectedGemStackVerification"
                                    : "RejectedNonGemFootprint"
                                : colorAccepted
                                ? salvageAllowsGem
                                    ? stackVerifiedGem ? "GemColorMatched" : "RejectedGemStackVerification"
                                    : "RejectedNonGemFootprint"
                                : "BelowThreshold";
                    candidates.Add(new GemStashInventorySlotCandidate(
                        row + 1,
                        column + 1,
                        screenPoint,
                        accepted,
                        reason,
                        bestTemplate,
                        bestConfidence,
                        bestTemplatePath,
                        local,
                        new Rectangle(screenGrid.Left + local.Left, screenGrid.Top + local.Top, local.Width, local.Height),
                        ImageRecognitionBestSamplePromoter.EncodePng(inventoryGrid, local)));

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

        private static bool IsGemColorFallback(GemStashInventorySlotMetrics metrics, double bestConfidence)
        {
            if (bestConfidence < 0.25)
            {
                return false;
            }

            return metrics.RegularGemPixels >= 120 &&
                metrics.StackCountTextPixels >= 18 &&
                (metrics.InnerSaturatedPixels >= 450 ||
                    metrics.RegularGemPixels >= 500);
        }

        private static GemStashInventorySlotMetrics MeasureSlot(Bitmap bitmap, Rectangle local)
        {
            int innerLeft = local.Left + Math.Max(1, (int)Math.Round(local.Width * 0.18));
            int innerTop = local.Top + Math.Max(1, (int)Math.Round(local.Height * 0.18));
            int innerRight = local.Left + Math.Min(local.Width - 1, (int)Math.Round(local.Width * 0.82));
            int innerBottom = local.Top + Math.Min(local.Height - 1, (int)Math.Round(local.Height * 0.82));
            int regularGemPixels = 0;
            int stackCountTextPixels = 0;
            int innerSaturatedPixels = 0;

            for (int y = local.Top; y < local.Bottom && y < bitmap.Height; y++)
            {
                for (int x = local.Left; x < local.Right && x < bitmap.Width; x++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    int brightness = Math.Max(color.R, Math.Max(color.G, color.B));
                    int darkness = Math.Min(color.R, Math.Min(color.G, color.B));
                    int saturation = brightness - darkness;
                    int gray = (color.R + color.G + color.B) / 3;
                    bool inner = x >= innerLeft && x < innerRight && y >= innerTop && y < innerBottom;
                    bool stackTextBand =
                        y >= local.Top + (int)Math.Round(local.Height * 0.48) &&
                        y < local.Bottom - 3 &&
                        x >= local.Left + 4 &&
                        x < local.Right - 4;

                    if (inner)
                    {
                        if (brightness > 30 && saturation > 45)
                        {
                            innerSaturatedPixels++;
                        }

                        bool emerald = color.G >= 125 && color.R <= 105 && color.B <= 120 && color.G >= color.R + 30 && color.G >= color.B + 25;
                        bool ruby = color.R >= 140 && color.G <= 95 && color.B <= 105 && color.R >= color.G + 45 && color.R >= color.B + 45;
                        bool amethyst = color.R >= 95 &&
                            color.B >= 125 &&
                            color.G <= 95 &&
                            color.R >= color.G + 25 &&
                            color.B >= color.G + 35 &&
                            Math.Abs(color.R - color.B) <= 70;
                        bool topaz = color.R >= 150 &&
                            color.G >= 120 &&
                            color.B <= 95 &&
                            Math.Abs(color.R - color.G) <= 90;
                        bool diamond = brightness >= 150 && saturation <= 45;
                        if (emerald || ruby || amethyst || topaz || diamond)
                        {
                            regularGemPixels++;
                        }
                    }

                    if (stackTextBand && brightness >= 145 && saturation <= 90 && gray >= 120)
                    {
                        stackCountTextPixels++;
                    }
                }
            }

            return new GemStashInventorySlotMetrics(regularGemPixels, stackCountTextPixels, innerSaturatedPixels);
        }
    }
}

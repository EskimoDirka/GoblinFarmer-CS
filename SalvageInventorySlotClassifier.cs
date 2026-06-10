using System.Drawing;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    internal sealed record SalvageInventorySlotMetrics(
        double MeanBrightness,
        double BrightnessStdDev,
        double InnerMeanBrightness,
        int ColoredFramePixels,
        int TopFramePixels,
        int InnerBrightPixels,
        int InnerSaturatedPixels,
        int GreenQualityPixels,
        int OrangeQualityPixels,
        int RegularGemPixels,
        int StackCountTextPixels,
        double Confidence);

    internal sealed record SalvageInventorySlotCandidateDiagnostic(
        int Row,
        int Column,
        DrawingPoint ScreenPoint,
        bool Accepted,
        string Reason,
        int FootprintRows,
        string Quality,
        bool ConfirmationExpected,
        SalvageInventorySlotMetrics Metrics);

    internal sealed record SalvageInventorySlotTarget(
        int Row,
        int Column,
        DrawingPoint ScreenPoint,
        int FootprintRows,
        string Quality,
        bool ConfirmationExpected,
        SalvageInventorySlotMetrics Metrics);

    internal sealed record SalvageInventorySlotScanResult(
        IReadOnlyList<SalvageInventorySlotTarget> Targets,
        IReadOnlyList<SalvageInventorySlotCandidateDiagnostic> Candidates);

    internal static class SalvageInventorySlotClassifier
    {
        public const int Columns = InventoryGridLayout.Columns;
        public const int Rows = InventoryGridLayout.Rows;
        private const int MinimumColoredFramePixels = 450;
        private const int MinimumInnerBrightPixels = 250;
        private const int MinimumInnerSaturatedPixels = 220;
        private const int MinimumPaleItemColoredFramePixels = 800;
        private const int MinimumPaleItemInnerBrightPixels = 800;
        private const int MinimumPaleItemInnerSaturatedPixels = 100;
        private const int WeakFootprintInnerBrightPixels = 250;
        private const int WeakFootprintColoredFramePixels = 100;
        private const int WeakFootprintInnerSaturatedPixels = 45;
        private const int SetQualityPixels = 180;
        private const int SetQualityAnchorPixels = 120;
        private const int SetQualityAnchorColoredFramePixels = 180;
        private const int SetQualityAnchorContentPixels = 80;
        private const int SetQualityContinuationGreenPixels = 800;
        private const int SetQualityContinuationSaturatedPixels = 900;
        private const int LegendaryQualityPixels = 320;
        private const int ItemTopBoundaryPixels = 350;
        private const int MaximumItemFootprintRows = 2;

        private sealed record ItemFootprint(
            int StartRow,
            int Column,
            int Rows,
            string Quality,
            bool ConfirmationExpected);

        public static SalvageInventorySlotScanResult Scan(Bitmap inventoryGrid, Rectangle screenGrid)
        {
            if (inventoryGrid.Width <= 0 || inventoryGrid.Height <= 0)
            {
                return new SalvageInventorySlotScanResult([], []);
            }

            int slotWidth = inventoryGrid.Width / Columns;
            int slotHeight = inventoryGrid.Height / Rows;
            if (slotWidth <= 0 || slotHeight <= 0)
            {
                return new SalvageInventorySlotScanResult([], []);
            }

            bool[,] strongItemLike = new bool[Rows, Columns];
            bool[,] weakFootprintLike = new bool[Rows, Columns];
            bool[,] regularGemLike = new bool[Rows, Columns];
            bool[,] occupied = new bool[Rows, Columns];
            SalvageInventorySlotMetrics[,] metrics = new SalvageInventorySlotMetrics[Rows, Columns];
            DrawingPoint[,] points = new DrawingPoint[Rows, Columns];

            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    Rectangle local = InventoryGridLayout.SlotRectangle(inventoryGrid, row, column);
                    SalvageInventorySlotMetrics slotMetrics = MeasureSlot(inventoryGrid, local);
                    metrics[row, column] = slotMetrics;
                    points[row, column] = InventoryGridLayout.SlotScreenPoint(screenGrid, local);
                    strongItemLike[row, column] =
                        (slotMetrics.ColoredFramePixels >= MinimumColoredFramePixels &&
                            slotMetrics.InnerBrightPixels >= MinimumInnerBrightPixels &&
                            slotMetrics.InnerSaturatedPixels >= MinimumInnerSaturatedPixels) ||
                        IsPaleItemAnchor(slotMetrics) ||
                        IsSetQualityAnchor(slotMetrics);
                    weakFootprintLike[row, column] =
                        (slotMetrics.InnerBrightPixels >= WeakFootprintInnerBrightPixels &&
                            (slotMetrics.InnerSaturatedPixels >= WeakFootprintInnerSaturatedPixels ||
                                slotMetrics.ColoredFramePixels >= WeakFootprintColoredFramePixels)) ||
                        IsSetQualityFootprint(slotMetrics);
                    regularGemLike[row, column] = IsRegularGem(slotMetrics);
                }
            }

            bool[,] liveGemStackLike = BuildLiveGemStackClusterMap(metrics, regularGemLike);
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    regularGemLike[row, column] = regularGemLike[row, column] || liveGemStackLike[row, column];
                }
            }

            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    bool adjacentStrong =
                        (row > 0 && strongItemLike[row - 1, column] && !regularGemLike[row - 1, column]) ||
                        (row + 1 < Rows && strongItemLike[row + 1, column] && !regularGemLike[row + 1, column]);
                    occupied[row, column] =
                        !regularGemLike[row, column] &&
                        (strongItemLike[row, column] || (weakFootprintLike[row, column] && adjacentStrong));
                }
            }

            Dictionary<(int Row, int Column), ItemFootprint> footprintsByCell = [];
            List<ItemFootprint> footprints = BuildFootprints(occupied, metrics);
            foreach (ItemFootprint footprint in footprints)
            {
                for (int row = footprint.StartRow; row < footprint.StartRow + footprint.Rows; row++)
                {
                    footprintsByCell[(row, footprint.Column)] = footprint;
                }
            }

            List<SalvageInventorySlotTarget> targets = [];
            List<SalvageInventorySlotCandidateDiagnostic> diagnostics = [];
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    SalvageInventorySlotMetrics slotMetrics = metrics[row, column];
                    footprintsByCell.TryGetValue((row, column), out ItemFootprint? footprint);
                    bool accepted = false;
                    string reason;
                    int footprintRows = 0;
                    string quality = "None";
                    bool confirmationExpected = false;
                    if (regularGemLike[row, column])
                    {
                        reason = "RegularGemNonSalvageable";
                        quality = "RegularGem";
                    }
                    else if (footprint == null)
                    {
                        reason = RejectionReason(slotMetrics);
                    }
                    else if (row != footprint.StartRow)
                    {
                        reason = "DuplicateMultiSlotFootprint";
                        footprintRows = footprint.Rows;
                        quality = footprint.Quality;
                        confirmationExpected = footprint.ConfirmationExpected;
                    }
                    else
                    {
                        accepted = true;
                        reason = "ItemAnchor";
                        footprintRows = footprint.Rows;
                        quality = footprint.Quality;
                        confirmationExpected = footprint.ConfirmationExpected;
                        targets.Add(new SalvageInventorySlotTarget(
                            row + 1,
                            column + 1,
                            points[row, column],
                            footprintRows,
                            quality,
                            confirmationExpected,
                            slotMetrics));
                    }

                    diagnostics.Add(new SalvageInventorySlotCandidateDiagnostic(
                        row + 1,
                        column + 1,
                        points[row, column],
                        accepted,
                        reason,
                        footprintRows,
                        quality,
                        confirmationExpected,
                        slotMetrics));
                }
            }

            return new SalvageInventorySlotScanResult(targets, diagnostics);
        }

        private static List<ItemFootprint> BuildFootprints(bool[,] occupied, SalvageInventorySlotMetrics[,] metrics)
        {
            List<ItemFootprint> footprints = [];
            for (int column = 0; column < Columns; column++)
            {
                int row = 0;
                while (row < Rows)
                {
                    if (!occupied[row, column])
                    {
                        row++;
                        continue;
                    }

                    int startRow = row;
                    while (row < Rows && occupied[row, column])
                    {
                        row++;
                    }

                    int endRowExclusive = row;
                    AddLegalFootprints(footprints, metrics, startRow, endRowExclusive, column);
                }
            }

            return footprints;
        }

        private static void AddLegalFootprints(
            List<ItemFootprint> footprints,
            SalvageInventorySlotMetrics[,] metrics,
            int startRow,
            int endRowExclusive,
            int column)
        {
            int segmentStart = startRow;
            for (int row = startRow + 1; row < endRowExclusive; row++)
            {
                if (!ShouldStartNewFootprint(metrics, segmentStart, row, column))
                {
                    continue;
                }

                AddLegalFootprintSegment(footprints, metrics, segmentStart, row, column);
                segmentStart = row;
            }

            AddLegalFootprintSegment(footprints, metrics, segmentStart, endRowExclusive, column);
        }

        private static void AddLegalFootprintSegment(
            List<ItemFootprint> footprints,
            SalvageInventorySlotMetrics[,] metrics,
            int startRow,
            int endRowExclusive,
            int column)
        {
            int row = startRow;
            while (row < endRowExclusive)
            {
                int rows = Math.Min(MaximumItemFootprintRows, endRowExclusive - row);
                string quality = ResolveQuality(metrics, row, column, rows);
                footprints.Add(new ItemFootprint(
                    row,
                    column,
                    rows,
                    quality,
                    QualityRequiresConfirmation(quality)));
                row += rows;
            }
        }

        private static bool HasItemTopBoundary(SalvageInventorySlotMetrics metrics)
        {
            return metrics.TopFramePixels >= ItemTopBoundaryPixels;
        }

        private static bool ShouldStartNewFootprint(
            SalvageInventorySlotMetrics[,] metrics,
            int segmentStart,
            int row,
            int column)
        {
            if (!HasItemTopBoundary(metrics[row, column]))
            {
                return false;
            }

            int precedingRows = row - segmentStart;
            if (precedingRows >= MaximumItemFootprintRows)
            {
                return true;
            }

            if (precedingRows == 1 &&
                IsSetQualityContinuation(metrics[segmentStart, column], metrics[row, column]))
            {
                return false;
            }

            return precedingRows == 1 && HasItemTopBoundary(metrics[segmentStart, column]);
        }

        private static bool IsSetQualityContinuation(
            SalvageInventorySlotMetrics previous,
            SalvageInventorySlotMetrics current)
        {
            return previous.GreenQualityPixels >= SetQualityPixels &&
                current.GreenQualityPixels >= SetQualityContinuationGreenPixels &&
                current.InnerSaturatedPixels >= SetQualityContinuationSaturatedPixels;
        }

        private static string RejectionReason(SalvageInventorySlotMetrics metrics)
        {
            if (metrics.InnerBrightPixels >= WeakFootprintInnerBrightPixels &&
                (metrics.InnerSaturatedPixels >= WeakFootprintInnerSaturatedPixels ||
                    metrics.ColoredFramePixels >= WeakFootprintColoredFramePixels))
            {
                return "DetachedItemFootprint";
            }

            if (metrics.ColoredFramePixels < MinimumColoredFramePixels)
            {
                return "NoItemFrameAnchor";
            }

            if (metrics.InnerBrightPixels < MinimumInnerBrightPixels)
            {
                return "NoItemIconAnchor";
            }

            return "InsufficientItemColor";
        }

        private static string ResolveQuality(SalvageInventorySlotMetrics[,] metrics, int topRow, int column, int footprintRows)
        {
            int greenPixels = 0;
            int orangePixels = 0;
            for (int row = topRow; row < Rows && row < topRow + footprintRows; row++)
            {
                greenPixels += metrics[row, column].GreenQualityPixels;
                orangePixels += metrics[row, column].OrangeQualityPixels;
            }

            if (greenPixels >= SetQualityPixels)
            {
                return "Set";
            }

            if (orangePixels >= LegendaryQualityPixels)
            {
                return "Legendary";
            }

            for (int row = topRow; row < Rows && row < topRow + footprintRows; row++)
            {
                if (IsConservativeConfirmationFootprint(metrics[row, column]))
                {
                    return "Legendary";
                }
            }

            return "Normal";
        }

        private static bool IsConservativeConfirmationFootprint(SalvageInventorySlotMetrics metrics)
        {
            return metrics.StackCountTextPixels >= 18 &&
                metrics.RegularGemPixels < 120 &&
                metrics.GreenQualityPixels < 250 &&
                metrics.OrangeQualityPixels < 250 &&
                metrics.ColoredFramePixels >= 900 &&
                metrics.InnerBrightPixels >= 600 &&
                metrics.InnerSaturatedPixels >= 450;
        }

        private static bool QualityRequiresConfirmation(string quality)
        {
            return quality.Equals("Set", StringComparison.OrdinalIgnoreCase) ||
                quality.Equals("Legendary", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSetQualityAnchor(SalvageInventorySlotMetrics metrics)
        {
            if (metrics.GreenQualityPixels < SetQualityAnchorPixels)
            {
                return false;
            }

            if (metrics.ColoredFramePixels < SetQualityAnchorColoredFramePixels)
            {
                return false;
            }

            return metrics.InnerBrightPixels >= SetQualityAnchorContentPixels ||
                metrics.InnerSaturatedPixels >= SetQualityAnchorContentPixels ||
                metrics.TopFramePixels >= SetQualityAnchorContentPixels;
        }

        private static bool IsPaleItemAnchor(SalvageInventorySlotMetrics metrics)
        {
            return metrics.ColoredFramePixels >= MinimumPaleItemColoredFramePixels &&
                metrics.InnerBrightPixels >= MinimumPaleItemInnerBrightPixels &&
                metrics.InnerSaturatedPixels >= MinimumPaleItemInnerSaturatedPixels &&
                !IsRegularGem(metrics);
        }

        private static bool IsSetQualityFootprint(SalvageInventorySlotMetrics metrics)
        {
            return metrics.GreenQualityPixels >= SetQualityAnchorPixels &&
                (metrics.InnerBrightPixels >= SetQualityAnchorContentPixels ||
                    metrics.InnerSaturatedPixels >= SetQualityAnchorContentPixels ||
                    metrics.ColoredFramePixels >= WeakFootprintColoredFramePixels);
        }

        private static bool IsRegularGem(SalvageInventorySlotMetrics metrics)
        {
            if (metrics.RegularGemPixels >= 300 ||
                (metrics.RegularGemPixels >= 220 && metrics.InnerSaturatedPixels >= 500))
            {
                return true;
            }

            bool amberGemBody =
                metrics.TopFramePixels <= 100 &&
                metrics.OrangeQualityPixels >= 250 &&
                metrics.ColoredFramePixels >= 1200 &&
                metrics.InnerBrightPixels >= 750 &&
                metrics.InnerSaturatedPixels >= 750;
            if (amberGemBody)
            {
                return true;
            }

            return metrics.StackCountTextPixels >= 18 &&
                metrics.ColoredFramePixels >= 900 &&
                metrics.InnerSaturatedPixels >= 700 &&
                (metrics.OrangeQualityPixels >= 250 ||
                    metrics.GreenQualityPixels >= 250 ||
                    metrics.RegularGemPixels >= 120);
        }

        private static bool[,] BuildLiveGemStackClusterMap(SalvageInventorySlotMetrics[,] metrics, bool[,] knownGemLike)
        {
            bool[,] candidates = new bool[Rows, Columns];
            bool[,] supported = new bool[Rows, Columns];

            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    candidates[row, column] = knownGemLike[row, column] || IsLiveGemStackCandidate(metrics[row, column]);
                }
            }

            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    supported[row, column] = candidates[row, column] && HasGemStackClusterSupport(candidates, row, column);
                }
            }

            return supported;
        }

        private static bool HasGemStackClusterSupport(bool[,] candidates, int row, int column)
        {
            return
                (column > 0 && candidates[row, column - 1]) ||
                (column + 1 < Columns && candidates[row, column + 1]) ||
                (row > 0 && candidates[row - 1, column]) ||
                (row + 1 < Rows && candidates[row + 1, column]);
        }

        private static bool IsLiveGemStackCandidate(SalvageInventorySlotMetrics metrics)
        {
            bool stackTextGem =
                metrics.StackCountTextPixels >= 16 &&
                metrics.TopFramePixels <= 100 &&
                metrics.ColoredFramePixels >= 500 &&
                metrics.ColoredFramePixels <= 1400 &&
                metrics.InnerBrightPixels >= 350 &&
                metrics.InnerSaturatedPixels >= 500;

            bool tooltipCoveredTopGem =
                metrics.StackCountTextPixels == 0 &&
                metrics.TopFramePixels <= 20 &&
                metrics.ColoredFramePixels >= 500 &&
                metrics.ColoredFramePixels <= 900 &&
                metrics.InnerBrightPixels >= 500 &&
                metrics.InnerSaturatedPixels >= 550 &&
                metrics.GreenQualityPixels < 120 &&
                metrics.OrangeQualityPixels < 120;

            bool emeraldStackGem =
                metrics.GreenQualityPixels >= 700 &&
                metrics.StackCountTextPixels >= 18 &&
                metrics.TopFramePixels <= 80 &&
                metrics.ColoredFramePixels >= 550 &&
                metrics.InnerSaturatedPixels >= 650;

            return stackTextGem || tooltipCoveredTopGem || emeraldStackGem;
        }

        private static SalvageInventorySlotMetrics MeasureSlot(Bitmap bitmap, Rectangle local)
        {
            int innerLeft = local.Left + Math.Max(1, (int)Math.Round(local.Width * 0.18));
            int innerTop = local.Top + Math.Max(1, (int)Math.Round(local.Height * 0.18));
            int innerRight = local.Left + Math.Min(local.Width - 1, (int)Math.Round(local.Width * 0.82));
            int innerBottom = local.Top + Math.Min(local.Height - 1, (int)Math.Round(local.Height * 0.82));
            int topBandBottom = local.Top + Math.Max(4, local.Height / 5);

            long brightnessSum = 0;
            long brightnessSquares = 0;
            long innerBrightnessSum = 0;
            int pixelCount = 0;
            int innerCount = 0;
            int coloredFramePixels = 0;
            int topFramePixels = 0;
            int innerBrightPixels = 0;
            int innerSaturatedPixels = 0;
            int greenQualityPixels = 0;
            int orangeQualityPixels = 0;
            int regularGemPixels = 0;
            int stackCountTextPixels = 0;

            for (int y = local.Top; y < local.Bottom && y < bitmap.Height; y++)
            {
                for (int x = local.Left; x < local.Right && x < bitmap.Width; x++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    int brightness = Math.Max(color.R, Math.Max(color.G, color.B));
                    int darkness = Math.Min(color.R, Math.Min(color.G, color.B));
                    int saturation = brightness - darkness;
                    int gray = (color.R + color.G + color.B) / 3;
                    bool colored = brightness >= 70 && saturation >= 45;
                    bool inner = x >= innerLeft && x < innerRight && y >= innerTop && y < innerBottom;
                    bool stackTextBand =
                        y >= local.Top + (int)Math.Round(local.Height * 0.48) &&
                        y < local.Bottom - 3 &&
                        x >= local.Left + 4 &&
                        x < local.Right - 4;
                    bool lightStackText = brightness >= 145 && saturation <= 90 && gray >= 120;

                    brightnessSum += gray;
                    brightnessSquares += (long)gray * gray;
                    pixelCount++;

                    if (colored)
                    {
                        coloredFramePixels++;
                        if (y < topBandBottom)
                        {
                            topFramePixels++;
                        }
                    }

                    if (inner)
                    {
                        innerBrightnessSum += gray;
                        innerCount++;
                        if (gray > 45)
                        {
                            innerBrightPixels++;
                        }

                        if (brightness > 30 && saturation > 45)
                        {
                            innerSaturatedPixels++;
                        }

                        if (color.G >= 55 &&
                            color.G >= color.R + 12 &&
                            color.G >= color.B + 8 &&
                            saturation >= 18)
                        {
                            greenQualityPixels++;
                        }

                        if (color.R >= 90 &&
                            color.G >= 35 &&
                            color.G <= 115 &&
                            color.B <= 80 &&
                            color.R >= color.G + 20)
                        {
                            orangeQualityPixels++;
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

                    if (stackTextBand && lightStackText)
                    {
                        stackCountTextPixels++;
                    }
                }
            }

            double mean = pixelCount == 0 ? 0 : brightnessSum / (double)pixelCount;
            double variance = pixelCount == 0
                ? 0
                : Math.Max(0, (brightnessSquares / (double)pixelCount) - (mean * mean));
            double innerMean = innerCount == 0 ? 0 : innerBrightnessSum / (double)innerCount;
            double confidence = Math.Min(
                1,
                (Math.Min(1, coloredFramePixels / (double)MinimumColoredFramePixels) * 0.45) +
                (Math.Min(1, innerBrightPixels / (double)MinimumInnerBrightPixels) * 0.35) +
                (Math.Min(1, innerSaturatedPixels / (double)MinimumInnerSaturatedPixels) * 0.20));

            return new SalvageInventorySlotMetrics(
                mean,
                Math.Sqrt(variance),
                innerMean,
                coloredFramePixels,
                topFramePixels,
                innerBrightPixels,
                innerSaturatedPixels,
                greenQualityPixels,
                orangeQualityPixels,
                regularGemPixels,
                stackCountTextPixels,
                confidence);
        }
    }
}

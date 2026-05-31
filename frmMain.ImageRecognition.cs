using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using OpenCvSharp;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private bool PortWaitForImageInDiablo(string imagePath, CancellationToken token, int timeoutMs, double confidence)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }
                if (PortImageVisibleInDiablo(imagePath, confidence))
                {
                    return true;
                }

                PortSleep(token, 100);
            }

            return false;
        }

        private bool PortImageVisibleInDiablo(string imagePath, double confidence, ImageMatchMode matchMode = ImageMatchMode.Default)
        {
            return FindImageInDiabloWindow(imagePath, out _, confidence, matchMode);
        }

        private bool PortImageVisibleInDiabloRegion(string imagePath, Rectangle referenceRegion, double confidence)
        {
            return PortBestTemplateConfidenceInDiabloRegion(imagePath, referenceRegion) >= confidence;
        }

        private bool PortStartGameButtonVisible(bool logPerf = false)
        {
            Stopwatch perf = Stopwatch.StartNew();
            bool visible = PortImageVisibleInDiablo(Img("Start Game", "Start Game Button.png"), PortStartGameButtonConfidence, ImageMatchMode.Color);
            if (logPerf || visible || perf.ElapsedMilliseconds >= 1000)
            {
                AppLogger.Info($"PERF PortStartGameButtonVisible: {visible} in {perf.ElapsedMilliseconds}ms");
            }
            return visible;
        }

        private bool PortCharacterLoadConfirmationVisible()
        {
            string imagePath = Img("Start Game", "Character Load Confirmation.png");
            return File.Exists(imagePath) && PortImageVisibleInDiabloRegion(imagePath, PortScanRegion("CharacterLoad", imagePath), PortCharacterLoadConfidence);
        }

        private bool PortPlayerIsInGame()
        {
            return PortCharacterLoadConfirmationVisible() ||
                PortGameMenuVisible() ||
                !string.IsNullOrWhiteSpace(PortDetectSpecificLocation("New Tristram")) ||
                !string.IsNullOrWhiteSpace(PortDetectSpecificLocation("Southern Highlands"));
        }

        private bool PortGameMenuVisible()
        {
            return PortImageVisibleInDiablo(Img("Leave Game", "Leave Game Button.png"), PortGameMenuConfidence);
        }

        private bool PortCloseGameMenuIfOpen(CancellationToken token)
        {
            if (!PortGameMenuVisible())
            {
                return true;
            }

            PortSetAppStatus("Closing Game Menu");
            PortPressEscapeForAutomation();

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 3000)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (!PortGameMenuVisible())
                {
                    PortSleep(token, 250);
                    return true;
                }

                PortSleep(token, 100);
            }

            return false;
        }

        private bool PortWaitForMapReady(CancellationToken token, int timeoutMs)
        {
            Stopwatch perf = Stopwatch.StartNew();
            int scans = 0;
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    AppLogger.Info($"PERF PortWaitForMapReady: cancelled after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return false;
                }
                scans++;
                if (!string.IsNullOrWhiteSpace(PortDetectMapActHeader()))
                {
                    AppLogger.Info($"PERF PortWaitForMapReady: ready after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return true;
                }

                PortSleep(token, 100);
            }

            AppLogger.Info($"PERF PortWaitForMapReady: timeout after {scans} scans in {perf.ElapsedMilliseconds}ms");
            return false;
        }

        private bool PortWaitForWorldMapReady(CancellationToken token, int timeoutMs)
        {
            Stopwatch perf = Stopwatch.StartNew();
            int scans = 0;
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    AppLogger.Info($"PERF PortWaitForWorldMapReady: cancelled after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return false;
                }
                scans++;
                if (PortImageVisibleInDiabloRegion(Img("Teleport Function", "World Map.png"), PortMapHeaderRegion(), PortWorldMapConfidence))
                {
                    AppLogger.Info($"PERF PortWaitForWorldMapReady: matched after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return true;
                }

                PortSleep(token, 100);
            }

            AppLogger.Info($"PERF PortWaitForWorldMapReady: timeout after {scans} scans in {perf.ElapsedMilliseconds}ms");
            return false;
        }

        private bool PortWaitForMapActHeader(string actName, CancellationToken token, int timeoutMs)
        {
            Stopwatch perf = Stopwatch.StartNew();
            int scans = 0;
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    AppLogger.Info($"PERF PortWaitForMapActHeader {actName}: cancelled after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return false;
                }
                scans++;
                string detectedAct = PortDetectMapActHeader();
                if (string.Equals(detectedAct, actName, StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Info($"PERF PortWaitForMapActHeader {actName}: matched after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return true;
                }

                PortSleep(token, 100);
            }

            string finalAct = PortDetectMapActHeader(logPerf: true);
            AppLogger.Info($"PERF PortWaitForMapActHeader {actName}: timeout after {scans} scans in {perf.ElapsedMilliseconds}ms, final={PortDisplayLocation(finalAct)}");
            return false;
        }

        private string PortDetectMapActHeader(bool logPerf = false)
        {
            Stopwatch perf = Stopwatch.StartNew();
            (string Act, string File)[] templates =
            [
                ("Act 1", "Act 1 Map Header.png"),
                ("Act 2", "Act 2.png"),
                ("Act 3", "Act 3.png"),
                ("Act 4", "Act 4.png"),
                ("Act 5", "Act 5.png"),
            ];

            string bestAct = "";
            double bestConfidence = 0;

            foreach ((string act, string file) in templates)
            {
                double confidence = PortBestTemplateConfidenceInDiabloRegion(Img("Teleport Function", file), PortMapHeaderRegion());
                if (confidence > bestConfidence)
                {
                    bestAct = act;
                    bestConfidence = confidence;
                }
            }

            string detected = bestConfidence >= PortMapActHeaderConfidence ? bestAct : "";
            if (logPerf)
            {
                AppLogger.Info($"PERF PortDetectMapActHeader: scanned {templates.Length} templates in {perf.ElapsedMilliseconds}ms, best={PortDisplayLocation(bestAct)} confidence={bestConfidence:0.000}, detected={PortDisplayLocation(detected)}");
            }
            return detected;
        }

        private bool PortIsInGame()
        {
            return !string.IsNullOrWhiteSpace(PortDetectCurrentLocation());
        }

        private bool PortGameLoadedLocationTitleVisible()
        {
            if (!PortTryGetDiabloRect(out RECT rect))
            {
                return false;
            }

            Rectangle titleRegion = PortScaleReferenceRectangle(new Rectangle(
                (int)Math.Round(PortReferenceWidth * 0.82),
                0,
                (int)Math.Round(PortReferenceWidth * 0.18),
                (int)Math.Round(PortReferenceHeight * 0.04)),
                rect);

            if (titleRegion.Width <= 0 || titleRegion.Height <= 0)
            {
                return false;
            }

            using Bitmap screenshot = new(titleRegion.Width, titleRegion.Height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(titleRegion.Left, titleRegion.Top, 0, 0, screenshot.Size);
            }

            using Mat raw = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
            using Mat bgr = new();
            using Mat mask = new();
            Cv2.CvtColor(raw, bgr, ColorConversionCodes.BGRA2BGR);
            Cv2.InRange(bgr, new Scalar(185, 185, 185), new Scalar(255, 255, 255), mask);
            return Cv2.CountNonZero(mask) > 8;
        }

        private string PortDetectCurrentLocation()
        {
            return PortDetectCurrentLocationFromTemplates(portCurrentLocationTemplates, "full current-location detection", logPerf: true);
        }

        /// <summary>
        /// Checks the current-location title area for templates associated with a specific destination.
        /// </summary>
        private string PortDetectSpecificLocation(string locationName)
        {
            Stopwatch perf = Stopwatch.StartNew();
            Dictionary<string, string> targetTemplates = PortCurrentLocationTemplatesForTarget(locationName, fallbackToAll: false);
            double threshold = PortLocationConfidenceForTarget(locationName);
            PortLocationDetectionResult result = PortDetectCurrentLocationFromTemplatesDetailed(targetTemplates, $"specific location: {locationName}", logPerf: false, threshold);
            string detected = result.Detected;
            bool matched = PortLocationMatches(detected, locationName);
            AppLogger.Info($"PERF PortDetectSpecificLocation {locationName}: {(matched ? "matched" : "not matched")} {PortDisplayLocation(detected)} with {targetTemplates.Count} templates in {perf.ElapsedMilliseconds}ms; best={PortDisplayLocation(result.BestName)} confidence={result.BestConfidence:0.000}, second={PortDisplayLocation(result.SecondName)} confidence={result.SecondConfidence:0.000}, threshold={threshold:0.000}");
            return matched ? detected : "";
        }

        private bool PortWaitForCurrentLocation(string targetLocation, CancellationToken token, int timeoutMs)
        {
            return PortWaitForSpecificLocation(targetLocation, token, timeoutMs);
        }

        private bool PortWaitForSpecificLocation(string targetLocation, CancellationToken token, int timeoutMs)
        {
            Stopwatch perf = Stopwatch.StartNew();
            int scans = 0;
            Dictionary<string, string> targetTemplates = PortCurrentLocationTemplatesForTarget(targetLocation);
            double threshold = PortLocationConfidenceForTarget(targetLocation);
            PortLocationDetectionResult lastResult = new("", "", 0, "", 0, targetTemplates.Count, 0);
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    AppLogger.Info($"PERF PortWaitForSpecificLocation {targetLocation}: cancelled after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return false;
                }

                scans++;
                lastResult = PortDetectCurrentLocationFromTemplatesDetailed(targetTemplates, $"specific wait: {targetLocation}", logPerf: false, threshold);
                string detectedLocation = lastResult.Detected;
                if (PortLocationMatches(detectedLocation, targetLocation))
                {
                    AppLogger.Info($"PERF PortWaitForSpecificLocation {targetLocation}: matched {detectedLocation} with {targetTemplates.Count} templates after {scans} scans in {perf.ElapsedMilliseconds}ms; best={PortDisplayLocation(lastResult.BestName)} confidence={lastResult.BestConfidence:0.000}, second={PortDisplayLocation(lastResult.SecondName)} confidence={lastResult.SecondConfidence:0.000}, threshold={threshold:0.000}");
                    return true;
                }

                PortSleep(token, 250);
            }

            AppLogger.Info($"PERF PortWaitForSpecificLocation {targetLocation}: timeout with {targetTemplates.Count} templates after {scans} scans in {perf.ElapsedMilliseconds}ms; best={PortDisplayLocation(lastResult.BestName)} confidence={lastResult.BestConfidence:0.000}, second={PortDisplayLocation(lastResult.SecondName)} confidence={lastResult.SecondConfidence:0.000}, threshold={threshold:0.000}");
            return false;
        }

        private string PortDetectBlockedTeleportLocation()
        {
            Dictionary<string, string> blockedTemplates = PortCurrentLocationTemplatesForNames(portTeleportBlockedLocations);
            return PortDetectCurrentLocationFromTemplatesDetailed(blockedTemplates, "blocked teleport location detection", logPerf: true, PortBlockedLocationConfidence).Detected;
        }

        private Dictionary<string, string> PortCurrentLocationTemplatesForTarget(string targetLocation, bool fallbackToAll = true)
        {
            Dictionary<string, string> templates = PortCurrentLocationTemplatesForNames(PortLocationNamesForTarget(targetLocation));

            return templates.Count > 0 || !fallbackToAll ? templates : portCurrentLocationTemplates;
        }

        private Dictionary<string, string> PortCurrentLocationTemplatesForNames(IEnumerable<string> locationNames)
        {
            Dictionary<string, string> templates = new(StringComparer.OrdinalIgnoreCase);
            foreach (string locationName in locationNames)
            {
                string key = PortNormalizeLocation(locationName);
                if (portCurrentLocationTemplates.TryGetValue(key, out string? imagePath))
                {
                    templates[key] = imagePath;
                }
            }

            return templates;
        }

        private IEnumerable<string> PortLocationNamesForTarget(string targetLocation)
        {
            yield return targetLocation;

            string key = PortLocationKey(targetLocation);
            if (portArrivalAliases.TryGetValue(key, out string[]? aliases))
            {
                foreach (string alias in aliases)
                {
                    yield return alias;
                }
            }
        }

        private string PortDetectCurrentLocationFromTemplates(IReadOnlyDictionary<string, string> templates, string label, bool logPerf)
        {
            return PortDetectCurrentLocationFromTemplatesDetailed(templates, label, logPerf, PortCurrentLocationConfidence).Detected;
        }

        private PortLocationDetectionResult PortDetectCurrentLocationFromTemplatesDetailed(IReadOnlyDictionary<string, string> templates, string label, bool logPerf, double threshold)
        {
            Stopwatch perf = Stopwatch.StartNew();
            string bestName = "";
            double bestConfidence = 0;
            string secondName = "";
            double secondConfidence = 0;

            if (templates.Count == 0)
            {
                if (logPerf)
                {
                    AppLogger.Info($"PERF PortDetectCurrentLocation ({label}): 0 templates scanned in 0ms");
                }
                return new("", "", 0, "", 0, 0, perf.ElapsedMilliseconds);
            }

            if (!PortTryGetDiabloRect(out _))
            {
                if (logPerf)
                {
                    AppLogger.Info($"PERF PortDetectCurrentLocation ({label}): window missing, {templates.Count} templates available in {perf.ElapsedMilliseconds}ms");
                }
                return new("", "", 0, "", 0, templates.Count, perf.ElapsedMilliseconds);
            }

            using Bitmap screenshot = PortCaptureDiabloRegion(PortCurrentLocationTitleRegion());
            if (screenshot.Width <= 1 || screenshot.Height <= 1)
            {
                if (logPerf)
                {
                    AppLogger.Info($"PERF PortDetectCurrentLocation ({label}): title-region capture failed, {templates.Count} templates available in {perf.ElapsedMilliseconds}ms");
                }
                return new("", "", 0, "", 0, templates.Count, perf.ElapsedMilliseconds);
            }

            using Mat rawScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
            using Mat screenMat = new();
            Cv2.CvtColor(rawScreenMat, screenMat, ColorConversionCodes.BGRA2BGR);

            foreach ((string name, string imagePath) in templates)
            {
                double confidence = PortLocationKey(name).StartsWith("pandemonium fortress level", StringComparison.Ordinal)
                    ? PortBestPandemoniumTemplateConfidenceInMat(screenMat, imagePath)
                    : PortBestTemplateConfidenceInMat(screenMat, imagePath);
                if (confidence > bestConfidence)
                {
                    secondName = bestName;
                    secondConfidence = bestConfidence;
                    bestName = name;
                    bestConfidence = confidence;
                }
                else if (confidence > secondConfidence)
                {
                    secondName = name;
                    secondConfidence = confidence;
                }
            }

            string detected = PortResolveDetectedLocation(bestName, bestConfidence, secondName, secondConfidence, threshold);
            if (logPerf)
            {
                AppLogger.Info($"PERF PortDetectCurrentLocation ({label}): scanned {templates.Count} templates in title region in {perf.ElapsedMilliseconds}ms, best={PortDisplayLocation(bestName)} confidence={bestConfidence:0.000}, second={PortDisplayLocation(secondName)} confidence={secondConfidence:0.000}, threshold={threshold:0.000}, detected={PortDisplayLocation(detected)}");
            }
            return new(detected, bestName, bestConfidence, secondName, secondConfidence, templates.Count, perf.ElapsedMilliseconds);
        }

        private string PortResolveDetectedLocation(string bestName, double bestConfidence, string secondName, double secondConfidence, double threshold)
        {
            if (bestConfidence < threshold)
            {
                return "";
            }

            string bestKey = PortLocationKey(bestName);
            string secondKey = PortLocationKey(secondName);
            bool bestIsPandemonium = PortIsPandemoniumLocation(bestName);
            bool secondIsPandemonium = PortIsPandemoniumLocation(secondName);

            if (bestIsPandemonium && secondIsPandemonium && bestKey != secondKey && bestConfidence - secondConfidence < 0.025)
            {
                AppLogger.Info($"Pandemonium location detection ambiguous: best={bestName} {bestConfidence:0.000}, second={secondName} {secondConfidence:0.000}");
                return "";
            }

            return PortNormalizeLocation(bestName);
        }

        private double PortLocationConfidenceForTarget(string targetLocation)
        {
            string key = PortLocationKey(targetLocation);
            if (key == PortLocationKey("City Of Caldeum") || key == PortLocationKey("Ancient Waterway"))
            {
                return 0.68;
            }

            if (key == PortLocationKey("Pandemonium Fortress Level 1") || key == PortLocationKey("Pandemonium Fortress Level 2"))
            {
                return 0.72;
            }

            return PortCurrentLocationConfidence;
        }

        private bool PortIsPandemoniumLocation(string location)
        {
            string key = PortLocationKey(location);
            return key == PortLocationKey("Pandemonium Fortress Level 1") ||
                key == PortLocationKey("Pandemonium Fortress Level 2");
        }

        private static double PortBestTemplateConfidenceInMat(Mat screenMat, string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                return 0;
            }

            using Mat templateMat = Cv2.ImRead(imagePath, ImreadModes.Color);
            if (templateMat.Empty() || templateMat.Width > screenMat.Width || templateMat.Height > screenMat.Height)
            {
                return 0;
            }

            using Mat result = new();
            Cv2.MatchTemplate(screenMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
            return maxVal;
        }

        private static double PortBestPandemoniumTemplateConfidenceInMat(Mat screenMat, string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                return 0;
            }

            using Mat templateMat = Cv2.ImRead(imagePath, ImreadModes.Color);
            if (templateMat.Empty() || templateMat.Width > screenMat.Width || templateMat.Height > screenMat.Height)
            {
                return 0;
            }

            using Mat result = new();
            Cv2.MatchTemplate(screenMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double fullScore, out _, out OpenCvSharp.Point maxLoc);

            int suffixWidth = Math.Min(44, templateMat.Width);
            int suffixX = templateMat.Width - suffixWidth;
            int screenX = maxLoc.X + suffixX;
            if (screenX < 0 || screenX + suffixWidth > screenMat.Width || maxLoc.Y < 0 || maxLoc.Y + templateMat.Height > screenMat.Height)
            {
                return fullScore;
            }

            using Mat templateSuffix = new(templateMat, new OpenCvSharp.Rect(suffixX, 0, suffixWidth, templateMat.Height));
            using Mat screenSuffix = new(screenMat, new OpenCvSharp.Rect(screenX, maxLoc.Y, suffixWidth, templateMat.Height));
            using Mat suffixResult = new();
            Cv2.MatchTemplate(screenSuffix, templateSuffix, suffixResult, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(suffixResult, out _, out double suffixScore, out _, out _);
            return suffixScore;
        }

        private double PortBestTemplateConfidenceInDiablo(string imagePath)
        {
            Stopwatch perf = Stopwatch.StartNew();
            IntPtr diabloWindow = FindDiabloWindow();
            if (diabloWindow == IntPtr.Zero || !File.Exists(imagePath))
            {
                AppLogger.Info($"PERF PortBestTemplateConfidenceInDiablo {Path.GetFileName(imagePath)}: unavailable in {perf.ElapsedMilliseconds}ms");
                return 0;
            }

            Rectangle? referenceRegion = PortScanRegionForImage(imagePath);
            Bitmap? screenshot = referenceRegion.HasValue
                ? PortCaptureDiabloRegion(referenceRegion.Value)
                : CaptureWindow(diabloWindow);
            if (screenshot == null)
            {
                AppLogger.Info($"PERF PortBestTemplateConfidenceInDiablo {Path.GetFileName(imagePath)}: capture failed in {perf.ElapsedMilliseconds}ms");
                return 0;
            }

            using (screenshot)
            {
                using Mat rawScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
                using Mat screenMat = new();
                Cv2.CvtColor(rawScreenMat, screenMat, ColorConversionCodes.BGRA2BGR);

                using Mat templateMat = Cv2.ImRead(imagePath, ImreadModes.Color);
                if (templateMat.Empty() || templateMat.Width > screenMat.Width || templateMat.Height > screenMat.Height)
                {
                    return 0;
                }

                using Mat result = new();
                Cv2.MatchTemplate(screenMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
                AppLogger.Info($"PERF PortBestTemplateConfidenceInDiablo {Path.GetFileName(imagePath)}: confidence={maxVal:0.000} in {perf.ElapsedMilliseconds}ms");
                return maxVal;
            }
        }

        private Bitmap PortCaptureDiabloRegion(Rectangle referenceRegion)
        {
            if (!PortTryGetDiabloRect(out RECT rect))
            {
                return new Bitmap(1, 1);
            }

            Rectangle screenRegion = PortScaleReferenceRectangle(referenceRegion, rect);
            screenRegion = Rectangle.Intersect(SystemInformation.VirtualScreen, screenRegion);

            if (screenRegion.Width <= 0 || screenRegion.Height <= 0)
            {
                return new Bitmap(1, 1);
            }

            Bitmap screenshot = new(screenRegion.Width, screenRegion.Height);

            using Graphics graphics = Graphics.FromImage(screenshot);
            graphics.CopyFromScreen(screenRegion.Left, screenRegion.Top, 0, 0, screenshot.Size);

            return screenshot;
        }

        private double PortBestTemplateConfidenceInDiabloRegion(string imagePath, Rectangle referenceRegion)
        {
            if (!PortTryGetDiabloRect(out RECT rect) || !File.Exists(imagePath))
            {
                return 0;
            }

            Rectangle screenRegion = PortScaleReferenceRectangle(referenceRegion, rect);
            screenRegion = Rectangle.Intersect(SystemInformation.VirtualScreen, screenRegion);
            if (screenRegion.Width <= 0 || screenRegion.Height <= 0)
            {
                return 0;
            }

            using Bitmap screenshot = new(screenRegion.Width, screenRegion.Height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(screenRegion.Left, screenRegion.Top, 0, 0, screenshot.Size);
            }

            using Mat rawScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
            using Mat screenMat = new();
            Cv2.CvtColor(rawScreenMat, screenMat, ColorConversionCodes.BGRA2BGR);

            using Mat templateMat = Cv2.ImRead(imagePath, ImreadModes.Color);
            if (templateMat.Empty() || templateMat.Width > screenMat.Width || templateMat.Height > screenMat.Height)
            {
                return 0;
            }

            using Mat result = new();
            Cv2.MatchTemplate(screenMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
            return maxVal;
        }

        private DrawingPoint? PortFirstFilledInventorySlot()
        {
            if (!PortTryGetDiabloRect(out RECT rect))
            {
                return null;
            }

            Rectangle grid = PortScaleReferenceRectangle(new Rectangle(1864, 725, 687, 423), rect);
            int columns = 10;
            int rows = 6;
            int slotWidth = grid.Width / columns;
            int slotHeight = grid.Height / rows;
            string blankPath = Img("Salvage", "Blank Inventory Tile.png");

            using Bitmap screenshot = new(grid.Width, grid.Height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(grid.Left, grid.Top, 0, 0, screenshot.Size);
            }

            using Mat rawMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
            using Mat gray = new();
            Cv2.CvtColor(rawMat, gray, ColorConversionCodes.BGRA2GRAY);
            using Mat? blank = File.Exists(blankPath) ? Cv2.ImRead(blankPath, ImreadModes.Grayscale) : null;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    Rectangle local = new(col * slotWidth, row * slotHeight, slotWidth, slotHeight);
                    using Mat slot = new(gray, new OpenCvSharp.Rect(local.Left, local.Top, local.Width, local.Height));
                    Cv2.MeanStdDev(slot, out Scalar mean, out Scalar stdDev);

                    bool blankLike = mean.Val0 < 18.0 && stdDev.Val0 < 10.0;
                    if (!blankLike && blank != null)
                    {
                        using Mat resized = new();
                        using Mat result = new();
                        Cv2.Resize(blank, resized, new OpenCvSharp.Size(slot.Width, slot.Height));
                        Cv2.MatchTemplate(slot, resized, result, TemplateMatchModes.CCoeffNormed);
                        Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
                        blankLike = maxVal >= PortBlankInventoryTileConfidence;
                    }

                    if (!blankLike)
                    {
                        return new DrawingPoint(grid.Left + local.Left + slotWidth / 2, grid.Top + local.Top + slotHeight / 2);
                    }
                }
            }

            return null;
        }

        private PortScanRegionManager PortScanRegions => portScanRegionManager ??= PortCreateScanRegionManager();

        private PortScanRegionManager PortCreateScanRegionManager()
        {
            Dictionary<string, Rectangle> hardCodedRegions = new(StringComparer.OrdinalIgnoreCase)
            {
                ["CurrentLocation"] = PortReferenceRegion(2050, 0, 500, 42),
                ["MapHeader"] = PortReferenceRegion(970, 55, 620, 110),
                ["CharacterLoad"] = PortReferenceRegion(600, 1200, 1200, 220),
                ["WitchDoctorHex"] = PortReferenceRegion(842, 1336, 73, 73),
                ["MomentumStack"] = PortReferenceRegion(
                    (int)(PortReferenceWidth * 0.325),
                    (int)(PortReferenceHeight * 0.835),
                    (int)(PortReferenceWidth * 0.354),
                    (int)(PortReferenceHeight * 0.072)),
            };

            return new PortScanRegionManager(
                ImagesPath,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ScanRegions.json"),
                hardCodedRegions);
        }

        private Rectangle PortScanRegion(string key, string? imagePath = null)
        {
            return PortScanRegions.GetRegion(key, imagePath) ?? PortReferenceRegion(0, 0, PortReferenceWidth, PortReferenceHeight);
        }

        private Rectangle? PortScanRegionForImage(string imagePath)
        {
            return PortScanRegions.GetRegion(PortScanRegionManager.KeyFromImagePath(imagePath), imagePath);
        }

        private Rectangle PortCurrentLocationTitleRegion()
        {
            return PortScanRegion("CurrentLocation", Img("Current Location", "Current Location Scan Region.png"));
        }

        private Rectangle PortMapHeaderRegion()
        {
            return PortScanRegion("MapHeader", Img("Teleport Function", "Map Scan Region.jpg"));
        }

        private sealed class PortScanRegionManager
        {
            private readonly string imagesRoot;
            private readonly string cachePath;
            private readonly Dictionary<string, Rectangle> hardCodedRegions;
            private readonly Dictionary<string, PortScanRegionEntry> discoveredRegions = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> missingRegions = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> loggedRegions = new(StringComparer.OrdinalIgnoreCase);
            private bool cacheLoaded;

            public PortScanRegionManager(string imagesRoot, string cachePath, Dictionary<string, Rectangle> hardCodedRegions)
            {
                this.imagesRoot = imagesRoot;
                this.cachePath = cachePath;
                this.hardCodedRegions = new(hardCodedRegions, StringComparer.OrdinalIgnoreCase);
                AppLogger.Info($"ScanRegion manager initialized: root={imagesRoot}; cache={cachePath}");
            }

            public Rectangle? GetRegion(string key, string? imagePath = null)
            {
                key = NormalizeKey(key);
                LoadCache();

                if (hardCodedRegions.TryGetValue(key, out Rectangle hardCoded))
                {
                    LogOnce($"hardcoded:{key}", $"ScanRegion loaded hard-coded: {key}={FormatRegion(hardCoded)}");
                    return hardCoded;
                }

                if (discoveredRegions.TryGetValue(key, out PortScanRegionEntry cached))
                {
                    LogOnce($"cache:{key}", $"ScanRegion loaded from cache: {key} image={cached.Image} region={cached.Region}");
                    return cached.ToRectangle();
                }

                if (missingRegions.Contains(key))
                {
                    return null;
                }

                if (TryDiscoverRegion(key, imagePath, out PortScanRegionEntry discovered))
                {
                    discoveredRegions[key] = discovered;
                    AppLogger.Info($"ScanRegion discovered: {key} image={discovered.Image} region={discovered.Region}");
                    SaveCache();
                    AppLogger.Info($"ScanRegion saved: {key}");
                    return discovered.ToRectangle();
                }

                missingRegions.Add(key);
                AppLogger.Info($"ScanRegion missing: {key}; falling back to full-window scan");
                return null;
            }

            public static string KeyFromImagePath(string imagePath)
            {
                return NormalizeKey(Path.GetFileNameWithoutExtension(imagePath));
            }

            private bool TryDiscoverRegion(string key, string? imagePath, out PortScanRegionEntry entry)
            {
                entry = default;
                if (!Directory.Exists(imagesRoot))
                {
                    AppLogger.Info($"ScanRegion missing image root: {imagesRoot}");
                    return false;
                }

                string? scanImage = Directory.EnumerateFiles(imagesRoot, "*.*", SearchOption.AllDirectories)
                    .Where(IsScanRegionImage)
                    .Select(path => new { Path = path, Score = MatchScore(key, path) })
                    .Where(item => item.Score > 0)
                    .OrderByDescending(item => item.Score)
                    .ThenBy(item => item.Path.Length)
                    .Select(item => item.Path)
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(scanImage))
                {
                    return false;
                }

                if (!TryDeriveRegion(scanImage, imagePath, out Rectangle region))
                {
                    AppLogger.Info($"ScanRegion derive failed: {key} image={scanImage}");
                    return false;
                }

                entry = new PortScanRegionEntry(Path.GetFileName(scanImage), FormatRegion(region));
                return true;
            }

            private static bool IsScanRegionImage(string path)
            {
                string file = Path.GetFileName(path);
                string key = NormalizeKey(file);
                return key.Contains("scanregion", StringComparison.OrdinalIgnoreCase) &&
                    (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                     file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                     file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));
            }

            private static int MatchScore(string key, string path)
            {
                string scanKey = NormalizeKey(Path.GetFileNameWithoutExtension(path)).Replace("scanregion", "", StringComparison.OrdinalIgnoreCase);
                if (string.IsNullOrWhiteSpace(scanKey))
                {
                    return 0;
                }

                if (scanKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return 1000;
                }

                if (scanKey.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    return 700;
                }

                if (key.Contains(scanKey, StringComparison.OrdinalIgnoreCase))
                {
                    return 500;
                }

                string[] keyParts = SplitKey(key);
                int overlap = keyParts.Count(part => scanKey.Contains(part, StringComparison.OrdinalIgnoreCase));
                return overlap >= Math.Min(2, keyParts.Length) ? overlap * 100 : 0;
            }

            private static string[] SplitKey(string key)
            {
                return Regex.Matches(key, "[A-Z]?[a-z]+|[0-9]+")
                    .Select(match => NormalizeKey(match.Value))
                    .Where(part => part.Length > 2)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            private static bool TryDeriveRegion(string scanImagePath, string? templateImagePath, out Rectangle region)
            {
                region = Rectangle.Empty;
                using Mat image = Cv2.ImRead(scanImagePath, ImreadModes.Color);
                if (image.Empty())
                {
                    return false;
                }

                using Mat hsv = new();
                Cv2.CvtColor(image, hsv, ColorConversionCodes.BGR2HSV);

                using Mat red1 = new();
                using Mat red2 = new();
                using Mat green = new();
                using Mat blue = new();
                using Mat mask = new();
                Cv2.InRange(hsv, new Scalar(0, 80, 120), new Scalar(10, 255, 255), red1);
                Cv2.InRange(hsv, new Scalar(170, 80, 120), new Scalar(179, 255, 255), red2);
                Cv2.InRange(hsv, new Scalar(35, 80, 120), new Scalar(95, 255, 255), green);
                Cv2.InRange(hsv, new Scalar(95, 80, 120), new Scalar(135, 255, 255), blue);
                Cv2.BitwiseOr(red1, red2, mask);
                Cv2.BitwiseOr(mask, green, mask);
                Cv2.BitwiseOr(mask, blue, mask);

                Cv2.FindContours(mask, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                List<Rectangle> candidates = [];
                foreach (OpenCvSharp.Point[] contour in contours)
                {
                    OpenCvSharp.Rect rect = Cv2.BoundingRect(contour);
                    if (rect.Width < 20 || rect.Height < 10)
                    {
                        continue;
                    }

                    using Mat roi = new(mask, rect);
                    double fillRatio = Cv2.CountNonZero(roi) / (double)(rect.Width * rect.Height);
                    if (fillRatio > 0.45)
                    {
                        continue;
                    }

                    candidates.Add(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height));
                }

                if (candidates.Count == 0)
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(templateImagePath) && File.Exists(templateImagePath))
                {
                    using Mat template = Cv2.ImRead(templateImagePath, ImreadModes.Color);
                    if (!template.Empty())
                    {
                        Rectangle? best = null;
                        double bestScore = 0;
                        foreach (Rectangle candidate in candidates)
                        {
                            Rectangle inflated = InflateWithin(candidate, image.Width, image.Height, 4);
                            if (template.Width > inflated.Width || template.Height > inflated.Height)
                            {
                                continue;
                            }

                            using Mat crop = new(image, new OpenCvSharp.Rect(inflated.X, inflated.Y, inflated.Width, inflated.Height));
                            using Mat result = new();
                            Cv2.MatchTemplate(crop, template, result, TemplateMatchModes.CCoeffNormed);
                            Cv2.MinMaxLoc(result, out _, out double score, out _, out _);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                best = inflated;
                            }
                        }

                        if (best.HasValue && bestScore >= 0.45)
                        {
                            region = best.Value;
                            return true;
                        }
                    }
                }

                region = candidates
                    .OrderByDescending(candidate => candidate.Width * candidate.Height)
                    .First();
                return true;
            }

            private static Rectangle InflateWithin(Rectangle rectangle, int maxWidth, int maxHeight, int padding)
            {
                int left = Math.Max(0, rectangle.Left - padding);
                int top = Math.Max(0, rectangle.Top - padding);
                int right = Math.Min(maxWidth, rectangle.Right + padding);
                int bottom = Math.Min(maxHeight, rectangle.Bottom + padding);
                return Rectangle.FromLTRB(left, top, right, bottom);
            }

            private void LoadCache()
            {
                if (cacheLoaded)
                {
                    return;
                }

                cacheLoaded = true;
                if (!File.Exists(cachePath))
                {
                    return;
                }

                try
                {
                    Dictionary<string, PortScanRegionEntry>? loaded = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, PortScanRegionEntry>>(File.ReadAllText(cachePath));
                    if (loaded == null)
                    {
                        return;
                    }

                    foreach ((string key, PortScanRegionEntry entry) in loaded)
                    {
                        if (entry.ToRectangle().Width > 0 && entry.ToRectangle().Height > 0)
                        {
                            discoveredRegions[NormalizeKey(key)] = entry;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error("ScanRegion cache load failed.", ex);
                }
            }

            private void SaveCache()
            {
                try
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(discoveredRegions, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(cachePath, json);
                }
                catch (Exception ex)
                {
                    AppLogger.Error("ScanRegion cache save failed.", ex);
                }
            }

            private void LogOnce(string key, string message)
            {
                if (loggedRegions.Add(key))
                {
                    AppLogger.Info(message);
                }
            }

            private static string FormatRegion(Rectangle region)
            {
                return $"{region.X},{region.Y},{region.Width},{region.Height}";
            }

            private static string NormalizeKey(string value)
            {
                return Regex.Replace(value ?? "", "[^a-zA-Z0-9]+", "");
            }

            private readonly record struct PortScanRegionEntry(string Image, string Region)
            {
                public Rectangle ToRectangle()
                {
                    string[] parts = Region.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 4 ||
                        !int.TryParse(parts[0], out int x) ||
                        !int.TryParse(parts[1], out int y) ||
                        !int.TryParse(parts[2], out int width) ||
                        !int.TryParse(parts[3], out int height))
                    {
                        return Rectangle.Empty;
                    }

                    return new Rectangle(x, y, width, height);
                }
            }
        }
    }
}

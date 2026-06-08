using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using OpenCvSharp;

namespace GoblinFarmer
{
    internal sealed record CurrentLocationImageResolverResult(
        string Detected,
        string BestName,
        double BestConfidence,
        string SecondName,
        double SecondConfidence,
        int TemplateCount,
        long ElapsedMilliseconds);

    internal static class CurrentLocationImageResolver
    {
        public static CurrentLocationImageResolverResult DetectFromBitmap(
            Bitmap screenshot,
            IReadOnlyDictionary<string, string> templates,
            double threshold,
            Action<string>? missingTemplate = null)
        {
            ArgumentNullException.ThrowIfNull(screenshot);
            ArgumentNullException.ThrowIfNull(templates);

            Stopwatch perf = Stopwatch.StartNew();
            if (templates.Count == 0)
            {
                return new("", "", 0, "", 0, 0, perf.ElapsedMilliseconds);
            }

            string bestName = "";
            double bestConfidence = 0;
            string secondName = "";
            double secondConfidence = 0;
            using Mat rawScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
            using Mat screenMat = ToColorMat(rawScreenMat);
            foreach ((string name, string imagePath) in templates)
            {
                double confidence = LocationKey(name).StartsWith("pandemonium fortress level", StringComparison.Ordinal)
                    ? BestPandemoniumTemplateConfidence(screenMat, imagePath, missingTemplate)
                    : BestTemplateConfidence(screenMat, imagePath, missingTemplate);
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

            string detected = ResolveDetectedLocation(bestName, bestConfidence, secondName, secondConfidence, threshold);
            return new(
                detected,
                bestName,
                bestConfidence,
                secondName,
                secondConfidence,
                templates.Count,
                perf.ElapsedMilliseconds);
        }

        public static Dictionary<string, string> DiscoverTemplatePaths(string currentLocationTemplateDirectory)
        {
            Dictionary<string, string> templates = new(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(currentLocationTemplateDirectory) ||
                !Directory.Exists(currentLocationTemplateDirectory))
            {
                return templates;
            }

            foreach (string imagePath in Directory.EnumerateFiles(currentLocationTemplateDirectory, "*.png", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileNameWithoutExtension(imagePath);
                if (fileName.Contains("Scan Region", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                templates[NormalizeLocation(fileName)] = imagePath;
            }

            return templates;
        }

        public static string NormalizeLocation(string name)
        {
            name = Regex.Replace((name ?? "").Replace("\ufeff", "").Trim(), @"\s+", " ");
            Dictionary<string, string> aliases = new(StringComparer.OrdinalIgnoreCase)
            {
                ["battelfields"] = "Battlefields",
                ["city of caldeum"] = "City Of Caldeum",
                ["the battlefields"] = "Battlefields",
                ["the royal crypts"] = "Royal Crypts",
                ["whimsydale"] = "WhimsyDale",
            };

            return aliases.TryGetValue(name, out string? alias) ? alias : name;
        }

        public static string LocationKey(string name)
        {
            return Regex.Replace(NormalizeLocation(name).ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
        }

        private static string ResolveDetectedLocation(
            string bestName,
            double bestConfidence,
            string secondName,
            double secondConfidence,
            double threshold)
        {
            if (bestConfidence < threshold)
            {
                return "";
            }

            string bestKey = LocationKey(bestName);
            string secondKey = LocationKey(secondName);
            bool bestIsPandemonium = IsPandemoniumLocation(bestName);
            bool secondIsPandemonium = IsPandemoniumLocation(secondName);
            if (bestIsPandemonium && secondIsPandemonium && bestKey != secondKey && bestConfidence - secondConfidence < 0.025)
            {
                return "";
            }

            if (IsCityOfCaldeumTitleAlias(bestName) &&
                IsCityOfCaldeumTitleAlias(secondName) &&
                bestKey != secondKey &&
                bestConfidence < 0.80 &&
                bestConfidence - secondConfidence < 0.025)
            {
                return "City Of Caldeum";
            }

            return NormalizeLocation(bestName);
        }

        private static bool IsPandemoniumLocation(string location)
        {
            string key = LocationKey(location);
            return key == LocationKey("Pandemonium Fortress Level 1") ||
                key == LocationKey("Pandemonium Fortress Level 2");
        }

        private static bool IsCityOfCaldeumTitleAlias(string location)
        {
            string key = LocationKey(location);
            return key == LocationKey("Gates of Caldeum") ||
                key == LocationKey("Caldeum Bazaar") ||
                key == LocationKey("Sewers of Caldeum") ||
                key == LocationKey("Flooded Causeway");
        }

        private static Mat ToColorMat(Mat rawScreenMat)
        {
            Mat screenMat = new();
            if (rawScreenMat.Channels() == 4)
            {
                Cv2.CvtColor(rawScreenMat, screenMat, ColorConversionCodes.BGRA2BGR);
            }
            else if (rawScreenMat.Channels() == 3)
            {
                screenMat = rawScreenMat.Clone();
            }
            else
            {
                Cv2.CvtColor(rawScreenMat, screenMat, ColorConversionCodes.GRAY2BGR);
            }

            return screenMat;
        }

        private static double BestTemplateConfidence(Mat screenMat, string imagePath, Action<string>? missingTemplate)
        {
            if (!File.Exists(imagePath))
            {
                missingTemplate?.Invoke(imagePath);
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

        private static double BestPandemoniumTemplateConfidence(Mat screenMat, string imagePath, Action<string>? missingTemplate)
        {
            if (!File.Exists(imagePath))
            {
                missingTemplate?.Invoke(imagePath);
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
    }
}

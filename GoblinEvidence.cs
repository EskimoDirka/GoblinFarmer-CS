using System.Globalization;
using System.Text.Json;

namespace GoblinFarmer
{
    internal enum GoblinJournalEventKind
    {
        Unknown,
        Killed,
        Escaped,
    }

    internal sealed record GoblinJournalEvent(
        GoblinJournalEventKind Kind,
        string GoblinType,
        string SourceLine);

    internal enum GoblinEvidenceType
    {
        Unknown,
        JournalEncounter,
        JournalKill,
        MinimapIcon,
    }

    internal sealed record GoblinEvidenceEvent(
        DateTime Timestamp,
        GoblinEvidenceType Type,
        double Confidence,
        string Source,
        string ScreenshotPath,
        string Notes);

    internal sealed record GoblinEvidenceCandidate(
        GoblinEvidenceType Type,
        double Confidence,
        string Source,
        string Notes,
        string GoblinType = "Unknown",
        IReadOnlyList<ImageRecognitionSampleCandidate>? RankedSamples = null);

    internal readonly record struct GoblinEvidenceTemplateMatch(
        double Confidence,
        System.Drawing.Point MatchPoint,
        System.Drawing.Point ScreenMatchPoint,
        System.Drawing.Size TemplateSize,
        GoblinMinimapColorClassification MinimapColor)
    {
        public GoblinEvidenceTemplateMatch(
            double confidence,
            System.Drawing.Point matchPoint,
            System.Drawing.Point screenMatchPoint,
            System.Drawing.Size templateSize)
            : this(confidence, matchPoint, screenMatchPoint, templateSize, GoblinMinimapColorClassification.Empty)
        {
        }
    }

    internal readonly record struct GoblinMinimapColorClassification(
        string ClassifiedGoblinType,
        int YellowPixels,
        int OrangePixels,
        int GreenPixels,
        int PurplePixels,
        int ColoredPixels)
    {
        public static GoblinMinimapColorClassification Empty { get; } = new("", 0, 0, 0, 0, 0);
    }

    internal static class GoblinEvidenceScanRegions
    {
        public static readonly System.Drawing.Rectangle JournalReferenceRegion = new(64, 736, 645, 417);
        public static readonly System.Drawing.Rectangle MinimapReferenceRegion = new(2108, 66, 421, 423);
    }

    internal enum GoblinEvidenceTemplateKind
    {
        Unknown,
        JournalEngaged,
        JournalKilled,
        JournalEngagedAndKilled,
        Minimap,
    }

    internal sealed record GoblinEvidenceTemplateRequirement(
        GoblinEvidenceType Type,
        string Source,
        string FileName,
        double Threshold,
        string GoblinType,
        GoblinEvidenceTemplateKind Kind);

    internal sealed record GoblinEvidenceInvalidTemplate(
        string FileName,
        string Reason);

    internal sealed record GoblinEvidenceTemplateCatalog(
        IReadOnlyList<GoblinEvidenceTemplateRequirement> Templates,
        IReadOnlyList<GoblinEvidenceInvalidTemplate> InvalidTemplates)
    {
        public bool HasJournalTemplates => Templates.Any(template => template.Source.Equals("JournalCandidate", StringComparison.OrdinalIgnoreCase));

        public bool HasMinimapTemplates => Templates.Any(template => template.Source.Equals("MinimapCandidate", StringComparison.OrdinalIgnoreCase));

        public bool HasUsableTemplates => Templates.Count > 0;
    }

    internal static class GoblinEvidenceTemplateRequirements
    {
        public const double JournalThreshold = 0.90;
        public const double GelatinousSireJournalThreshold = 0.78;
        public const double MinimapThreshold = 0.65;

        public static GoblinEvidenceTemplateCatalog DiscoverTemplates(string templateDirectory)
        {
            if (string.IsNullOrWhiteSpace(templateDirectory))
            {
                return new GoblinEvidenceTemplateCatalog(
                    [],
                    [new GoblinEvidenceInvalidTemplate("", "TemplateDirectoryMissing")]);
            }

            if (!Directory.Exists(templateDirectory))
            {
                return new GoblinEvidenceTemplateCatalog(
                    [],
                    [new GoblinEvidenceInvalidTemplate(templateDirectory, "TemplateDirectoryMissing")]);
            }

            List<GoblinEvidenceTemplateRequirement> templates = [];
            List<GoblinEvidenceInvalidTemplate> invalidTemplates = [];
            foreach (FileInfo file in new DirectoryInfo(templateDirectory).EnumerateFiles("*.png", SearchOption.TopDirectoryOnly).OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (TryParseTemplate(file.Name, out GoblinEvidenceTemplateRequirement? template, out string invalidReason))
                {
                    templates.Add(template!);
                    continue;
                }

                invalidTemplates.Add(new GoblinEvidenceInvalidTemplate(file.Name, invalidReason));
            }

            string promotedDirectory = Path.Combine(templateDirectory, "Promoted");
            if (Directory.Exists(promotedDirectory))
            {
                foreach (FileInfo file in new DirectoryInfo(promotedDirectory).EnumerateFiles("*.png", SearchOption.AllDirectories).OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase))
                {
                    if (TryParsePromotedTemplate(templateDirectory, file, out GoblinEvidenceTemplateRequirement? template, out string invalidReason))
                    {
                        templates.Add(template!);
                        continue;
                    }

                    invalidTemplates.Add(new GoblinEvidenceInvalidTemplate(Path.GetRelativePath(templateDirectory, file.FullName), invalidReason));
                }
            }

            return new GoblinEvidenceTemplateCatalog(templates, invalidTemplates);
        }

        public static string DisplayPath(GoblinEvidenceTemplateRequirement requirement)
        {
            return Path.Combine("Images", "Goblin Evidence", requirement.FileName);
        }

        public static string NamingGuidance()
        {
            return "<Goblin Type> Engaged Journal.png | <Goblin Type> Killed Journal.png | <Goblin Type> Engaged & Killed Journal.png | <Goblin Type> Engaged.png | <Goblin Type> Killed.png | Engaged <Goblin Type> Journal.png | Killed <Goblin Type> Journal.png | <Goblin Type> Minimap.png";
        }

        private static bool TryParseTemplate(
            string fileName,
            out GoblinEvidenceTemplateRequirement? template,
            out string invalidReason)
        {
            template = null;
            invalidReason = "";
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                invalidReason = "EmptyFileName";
                return false;
            }

            if (TryParseTemplateSuffix(
                baseName,
                " Engaged & Killed Journal",
                GoblinEvidenceTemplateKind.JournalEngagedAndKilled,
                GoblinEvidenceType.JournalEncounter,
                "JournalCandidate",
                JournalThreshold,
                out template,
                out invalidReason))
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(invalidReason))
            {
                return false;
            }

            if (TryParseTemplateSuffix(
                baseName,
                " Engaged & Killed",
                GoblinEvidenceTemplateKind.JournalEngagedAndKilled,
                GoblinEvidenceType.JournalEncounter,
                "JournalCandidate",
                JournalThreshold,
                out template,
                out invalidReason))
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(invalidReason))
            {
                return false;
            }

            if (TryParseTemplatePrefix(
                baseName,
                "Engaged & Killed ",
                " Journal",
                GoblinEvidenceTemplateKind.JournalEngagedAndKilled,
                GoblinEvidenceType.JournalEncounter,
                "JournalCandidate",
                JournalThreshold,
                out template,
                out invalidReason))
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(invalidReason))
            {
                return false;
            }

            if (TryParseTemplatePrefix(
                baseName,
                "Engaged & Killed ",
                "",
                GoblinEvidenceTemplateKind.JournalEngagedAndKilled,
                GoblinEvidenceType.JournalEncounter,
                "JournalCandidate",
                JournalThreshold,
                out template,
                out invalidReason))
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(invalidReason))
            {
                return false;
            }

            if (TryParseTemplateSuffix(
                baseName,
                " Engaged Journal",
                GoblinEvidenceTemplateKind.JournalEngaged,
                GoblinEvidenceType.JournalEncounter,
                "JournalCandidate",
                JournalThreshold,
                out template,
                out invalidReason))
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(invalidReason))
            {
                return false;
            }

            if (TryParseTemplateSuffix(
                baseName,
                " Engaged",
                GoblinEvidenceTemplateKind.JournalEngaged,
                GoblinEvidenceType.JournalEncounter,
                "JournalCandidate",
                JournalThreshold,
                out template,
                out invalidReason))
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(invalidReason))
            {
                return false;
            }

            if (TryParseTemplatePrefix(
                baseName,
                "Engaged ",
                " Journal",
                GoblinEvidenceTemplateKind.JournalEngaged,
                GoblinEvidenceType.JournalEncounter,
                "JournalCandidate",
                JournalThreshold,
                out template,
                out invalidReason))
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(invalidReason))
            {
                return false;
            }

            if (TryParseTemplatePrefix(
                baseName,
                "Engaged ",
                "",
                GoblinEvidenceTemplateKind.JournalEngaged,
                GoblinEvidenceType.JournalEncounter,
                "JournalCandidate",
                JournalThreshold,
                out template,
                out invalidReason))
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(invalidReason))
            {
                return false;
            }

            if (TryParseTemplateSuffix(
                baseName,
                " Killed Journal",
                GoblinEvidenceTemplateKind.JournalKilled,
                GoblinEvidenceType.JournalKill,
                "JournalCandidate",
                JournalThreshold,
                out template,
                out invalidReason))
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(invalidReason))
            {
                return false;
            }

            if (TryParseTemplateSuffix(
                baseName,
                " Killed",
                GoblinEvidenceTemplateKind.JournalKilled,
                GoblinEvidenceType.JournalKill,
                "JournalCandidate",
                JournalThreshold,
                out template,
                out invalidReason))
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(invalidReason))
            {
                return false;
            }

            if (TryParseTemplatePrefix(
                baseName,
                "Killed ",
                " Journal",
                GoblinEvidenceTemplateKind.JournalKilled,
                GoblinEvidenceType.JournalKill,
                "JournalCandidate",
                JournalThreshold,
                out template,
                out invalidReason))
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(invalidReason))
            {
                return false;
            }

            if (TryParseTemplatePrefix(
                baseName,
                "Killed ",
                "",
                GoblinEvidenceTemplateKind.JournalKilled,
                GoblinEvidenceType.JournalKill,
                "JournalCandidate",
                JournalThreshold,
                out template,
                out invalidReason))
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(invalidReason))
            {
                return false;
            }

            if (TryParseTemplateSuffix(
                baseName,
                " Minimap",
                GoblinEvidenceTemplateKind.Minimap,
                GoblinEvidenceType.MinimapIcon,
                "MinimapCandidate",
                MinimapThreshold,
                out template,
                out invalidReason))
            {
                return true;
            }
            else if (!string.IsNullOrWhiteSpace(invalidReason))
            {
                return false;
            }

            invalidReason = "UnsupportedNamePattern";
            return false;
        }

        private static bool TryParsePromotedTemplate(
            string templateDirectory,
            FileInfo file,
            out GoblinEvidenceTemplateRequirement? template,
            out string invalidReason)
        {
            template = null;
            invalidReason = "";
            string sidecarPath = Path.ChangeExtension(file.FullName, ".json");
            if (!File.Exists(sidecarPath))
            {
                invalidReason = "PromotedMetadataMissing";
                return false;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(sidecarPath));
                JsonElement selected = document.RootElement.TryGetProperty("selected", out JsonElement selectedElement)
                    ? selectedElement
                    : document.RootElement;
                string goblinType = ReadString(selected, "targetLabel");
                string source = ReadString(selected, "source");
                string kind = "";
                if (selected.TryGetProperty("metadata", out JsonElement metadata) &&
                    metadata.ValueKind == JsonValueKind.Object)
                {
                    kind = ReadString(metadata, "EvidenceKind");
                }

                if (string.IsNullOrWhiteSpace(goblinType) || !GoblinTypeNormalizer.IsKnownGoblinType(goblinType))
                {
                    invalidReason = $"PromotedUnknownGoblinType:{goblinType}";
                    return false;
                }

                if (!Enum.TryParse(kind, ignoreCase: true, out GoblinEvidenceTemplateKind evidenceKind) ||
                    evidenceKind == GoblinEvidenceTemplateKind.Unknown)
                {
                    invalidReason = $"PromotedUnknownEvidenceKind:{kind}";
                    return false;
                }

                string normalizedSource = source.Contains("Minimap", StringComparison.OrdinalIgnoreCase)
                    ? "MinimapCandidate"
                    : source.Contains("Journal", StringComparison.OrdinalIgnoreCase)
                        ? "JournalCandidate"
                        : "";
                if (string.IsNullOrWhiteSpace(normalizedSource))
                {
                    invalidReason = $"PromotedUnknownSource:{source}";
                    return false;
                }

                GoblinEvidenceType evidenceType = evidenceKind == GoblinEvidenceTemplateKind.Minimap
                    ? GoblinEvidenceType.MinimapIcon
                    : evidenceKind == GoblinEvidenceTemplateKind.JournalKilled
                        ? GoblinEvidenceType.JournalKill
                        : GoblinEvidenceType.JournalEncounter;
                double threshold = ResolveTemplateThreshold(GoblinTypeNormalizer.Normalize(goblinType), evidenceKind);
                template = new GoblinEvidenceTemplateRequirement(
                    evidenceType,
                    normalizedSource,
                    Path.GetRelativePath(templateDirectory, file.FullName),
                    threshold,
                    GoblinTypeNormalizer.Normalize(goblinType),
                    evidenceKind);
                return true;
            }
            catch (Exception ex)
            {
                invalidReason = $"PromotedMetadataInvalid:{ex.GetType().Name}";
                return false;
            }
        }

        private static string ReadString(JsonElement element, string propertyName)
        {
            return element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out JsonElement value) &&
                value.ValueKind == JsonValueKind.String
                    ? value.GetString() ?? ""
                    : "";
        }

        private static bool TryParseTemplatePrefix(
            string baseName,
            string prefix,
            string optionalSuffix,
            GoblinEvidenceTemplateKind kind,
            GoblinEvidenceType evidenceType,
            string source,
            double threshold,
            out GoblinEvidenceTemplateRequirement? template,
            out string invalidReason)
        {
            template = null;
            invalidReason = "";
            if (!baseName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string goblinType = baseName[prefix.Length..].Trim();
            if (!string.IsNullOrWhiteSpace(optionalSuffix))
            {
                if (!goblinType.EndsWith(optionalSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                goblinType = goblinType[..^optionalSuffix.Length].Trim();
            }

            if (string.IsNullOrWhiteSpace(goblinType))
            {
                invalidReason = "MissingGoblinType";
                return false;
            }

            if (!GoblinTypeNormalizer.IsKnownGoblinType(goblinType))
            {
                invalidReason = $"UnknownGoblinType:{goblinType}";
                return false;
            }

            template = new GoblinEvidenceTemplateRequirement(
                evidenceType,
                source,
                $"{baseName}.png",
                ResolveTemplateThreshold(GoblinTypeNormalizer.Normalize(goblinType), kind, threshold),
                GoblinTypeNormalizer.Normalize(goblinType),
                kind);
            return true;
        }

        private static bool TryParseTemplateSuffix(
            string baseName,
            string suffix,
            GoblinEvidenceTemplateKind kind,
            GoblinEvidenceType evidenceType,
            string source,
            double threshold,
            out GoblinEvidenceTemplateRequirement? template,
            out string invalidReason)
        {
            template = null;
            invalidReason = "";
            if (!baseName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string goblinType = baseName[..^suffix.Length].Trim();
            if (string.IsNullOrWhiteSpace(goblinType))
            {
                invalidReason = "MissingGoblinType";
                return false;
            }

            if (!GoblinTypeNormalizer.IsKnownGoblinType(goblinType))
            {
                invalidReason = $"UnknownGoblinType:{goblinType}";
                return false;
            }

            template = new GoblinEvidenceTemplateRequirement(
                evidenceType,
                source,
                $"{baseName}.png",
                ResolveTemplateThreshold(GoblinTypeNormalizer.Normalize(goblinType), kind, threshold),
                GoblinTypeNormalizer.Normalize(goblinType),
                kind);
            return true;
        }

        private static double ResolveTemplateThreshold(
            string goblinType,
            GoblinEvidenceTemplateKind kind,
            double defaultThreshold = JournalThreshold)
        {
            if (!kind.Equals(GoblinEvidenceTemplateKind.Minimap) &&
                GoblinTypeNormalizer.Normalize(goblinType).Equals("Gelatinous Sire", StringComparison.OrdinalIgnoreCase))
            {
                return GelatinousSireJournalThreshold;
            }

            return kind == GoblinEvidenceTemplateKind.Minimap
                ? MinimapThreshold
                : defaultThreshold;
        }
    }

    internal sealed record GoblinFoundRecord(
        string AreaKey,
        string DisplayLocation,
        string GoblinType,
        string Source,
        DateTime FirstSeenUtc,
        bool Counted,
        string SuppressionReason);

    internal sealed record GoblinObservationRecord(
        DateTime TimestampUtc,
        string Source,
        string GoblinType,
        string AreaKey,
        string DisplayLocation,
        bool WouldCount,
        string Reason,
        string DuplicateState,
        int AreaLimit,
        int CurrentAreaCount,
        double EvidenceConfidence = 0);

    internal sealed record GoblinMinimapAreaAnchorState(
        string GoblinType,
        string AreaKey,
        string DisplayLocation,
        DateTime SeenUtc,
        double EvidenceConfidence,
        string SuppressionReason,
        string CurrentAreaAtDetection,
        string EvidenceHash);

    internal sealed record GoblinDecisionTraceRecord(
        DateTime TimestampUtc,
        string CorrelationId,
        string Mode,
        string Source,
        string ImageFile,
        string ImagePath,
        string AreaRaw,
        string AreaKey,
        string GoblinType,
        string EvidenceSignature,
        double EvidenceAgeSeconds,
        double EvidenceFirstSeenAgeSeconds,
        bool AutoCountEnabled,
        bool ObservationModeEnabled,
        bool EvidencePredatesAutoCount,
        bool Fresh,
        bool AllowedArea,
        string BlockedReason,
        string DuplicateKey,
        int AreaCountBefore,
        int AreaLimit,
        int TotalGoblinCountBefore,
        string Decision,
        string Reason,
        bool NotificationShown);

    internal sealed record GoblinJournalEngagedState(
        string GoblinType,
        string AreaKey,
        DateTime SeenUtc);

    internal sealed record GoblinJournalKilledState(
        string GoblinType,
        string AreaKey,
        DateTime FirstSeenUtc,
        DateTime LastSeenUtc);

    internal readonly record struct GoblinAreaResolution(
        string RawLocation,
        string AreaKey,
        string DisplayLocation)
    {
        public bool Resolved => !string.IsNullOrWhiteSpace(AreaKey);
    }

    internal readonly record struct GoblinAreaDetectionDisambiguationResult(
        bool Ambiguous,
        bool Blocked,
        string SelectedLocation,
        string AmbiguityGroup,
        string Reason,
        double Delta);

    internal readonly record struct GoblinJournalAreaOverrideDecision(
        bool Overridden,
        GoblinAreaResolution Area,
        string Reason,
        double RecentObservationAgeSeconds);

    internal static class GoblinAreaResolver
    {
        public static IReadOnlyList<string> KnownAreas { get; } =
        [
            "Ancient Waterway",
            "Battlefields",
            "Black Canyon Mines",
            "Caldeum Bazaar",
            "Cathedral",
            "Cathedral Level 1",
            "Cathedral Level 2",
            "Cathedral Level 3",
            "Cave Of The Moon Clan Level 1",
            "Cave Of The Moon Clan Level 2",
            "Caverns of Frost Level 1",
            "Caverns of Frost Level 2",
            "City of Caldeum",
            "Eastern Channel Level 1",
            "Eastern Channel Level 2",
            "Fields of Slaughter",
            "Flooded Causeway",
            "Gates of Caldeum",
            "Hidden Camp",
            "Highlands Cave",
            "Leoric's Hunting Grounds",
            "Leoric's Passage",
            "New Tristram",
            "Northern Highlands",
            "Pandemonium Fortress Level 1",
            "Pandemonium Fortress Level 2",
            "Rakkis Crossing",
            "Royal Crypts",
            "Ruined Cistern",
            "Sewers of Caldeum",
            "Southern Highlands",
            "Stinging Winds",
            "The Bridge Of Korsikk",
            "The Festering Woods",
            "The Weeping Hollow",
            "Western Channel Level 1",
            "Western Channel Level 2",
            "WhimsyDale",
        ];
        private static readonly Dictionary<string, string> CanonicalAreas = BuildCanonicalAreas();

        public static GoblinAreaResolution Resolve(string rawLocation)
        {
            string cleaned = Clean(rawLocation);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return new GoblinAreaResolution("", "", "");
            }

            string lookupKey = NormalizedKey(cleaned);
            string areaKey = CanonicalAreas.TryGetValue(lookupKey, out string? canonical)
                ? canonical
                : TitleCase(lookupKey);

            return new GoblinAreaResolution(cleaned, areaKey, areaKey);
        }

        public static string NormalizedKey(string value)
        {
            string cleaned = Clean(value).ToLowerInvariant();
            return System.Text.RegularExpressions.Regex.Replace(cleaned, @"[^a-z0-9]+", " ").Trim();
        }

        private static Dictionary<string, string> BuildCanonicalAreas()
        {
            Dictionary<string, string> areas = new(StringComparer.OrdinalIgnoreCase);
            foreach (string name in KnownAreas)
            {
                areas[NormalizedKey(name)] = name;
            }

            AddAlias(areas, "battelfields", "Battlefields");
            AddAlias(areas, "The Battlefields", "Battlefields");
            AddAlias(areas, "City Of Caldeum", "City of Caldeum");
            AddAlias(areas, "The Royal Crypts", "Royal Crypts");
            AddAlias(areas, "Whimsydale", "WhimsyDale");

            return areas;
        }

        private static void AddAlias(Dictionary<string, string> areas, string alias, string canonical)
        {
            areas[NormalizedKey(alias)] = canonical;
        }

        private static string Clean(string value)
        {
            return System.Text.RegularExpressions.Regex.Replace((value ?? "").Replace("\ufeff", "").Trim(), @"\s+", " ");
        }

        private static string TitleCase(string normalizedKey)
        {
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                return "";
            }

            return string.Join(" ", normalizedKey.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
        }
    }

    internal static class GoblinAreaDetectionDisambiguator
    {
        private const double AmbiguousDeltaThreshold = 0.025;
        private const double AmbiguousCandidateMinimumConfidence = 0.90;
        private const double RouteContextCandidateMinimumConfidence = 0.80;
        private const double RouteContextDeltaThreshold = 0.15;

        public static GoblinAreaDetectionDisambiguationResult Disambiguate(
            string bestName,
            double bestConfidence,
            string secondName,
            double secondConfidence,
            string routeContext)
        {
            double delta = bestConfidence - secondConfidence;
            string ambiguityGroup = AmbiguityGroup(bestName, secondName);
            if (string.IsNullOrWhiteSpace(ambiguityGroup) ||
                bestConfidence < AmbiguousCandidateMinimumConfidence ||
                delta < 0)
            {
                return new GoblinAreaDetectionDisambiguationResult(false, false, bestName, "", "NotAmbiguous", delta);
            }

            string selected = SelectFromRouteContext(bestName, secondName, routeContext);
            if (!string.IsNullOrWhiteSpace(selected))
            {
                double selectedConfidence = CandidateConfidence(selected, bestName, bestConfidence, secondName, secondConfidence);
                if (selectedConfidence >= RouteContextCandidateMinimumConfidence &&
                    (string.Equals(GoblinAreaResolver.NormalizedKey(selected), GoblinAreaResolver.NormalizedKey(bestName), StringComparison.OrdinalIgnoreCase) ||
                        delta <= RouteContextDeltaThreshold))
                {
                    return new GoblinAreaDetectionDisambiguationResult(true, false, selected, ambiguityGroup, "RouteContext", delta);
                }
            }

            if (secondConfidence < AmbiguousCandidateMinimumConfidence ||
                delta > AmbiguousDeltaThreshold)
            {
                return new GoblinAreaDetectionDisambiguationResult(false, false, bestName, "", "NotAmbiguous", delta);
            }

            if (!IsPandemonium(bestName) && IsPandemonium(secondName))
            {
                return new GoblinAreaDetectionDisambiguationResult(true, false, bestName, ambiguityGroup, "BestNonPandemonium", delta);
            }

            return new GoblinAreaDetectionDisambiguationResult(true, true, "", ambiguityGroup, "AmbiguousAreaDetection", delta);
        }

        private static string SelectFromRouteContext(string bestName, string secondName, string routeContext)
        {
            string contextKey = GoblinAreaResolver.NormalizedKey(routeContext);
            if (string.IsNullOrWhiteSpace(contextKey))
            {
                return "";
            }

            if (contextKey == GoblinAreaResolver.NormalizedKey(bestName))
            {
                return bestName;
            }

            if (contextKey == GoblinAreaResolver.NormalizedKey(secondName))
            {
                return secondName;
            }

            if (IsAncientWaterwayContext(routeContext))
            {
                if (IsChannel(bestName))
                {
                    return bestName;
                }

                if (IsChannel(secondName))
                {
                    return secondName;
                }
            }

            if (IsBattlefieldsContext(routeContext))
            {
                if (IsCavernsOfFrost(bestName))
                {
                    return bestName;
                }

                if (IsCavernsOfFrost(secondName))
                {
                    return secondName;
                }
            }

            if (IsSouthernHighlandsContext(routeContext))
            {
                if (IsMoonClanCave(bestName))
                {
                    return bestName;
                }

                if (IsMoonClanCave(secondName))
                {
                    return secondName;
                }
            }

            if (IsCathedralContext(routeContext))
            {
                if (IsCathedral(bestName))
                {
                    return bestName;
                }

                if (IsCathedral(secondName))
                {
                    return secondName;
                }
            }

            return "";
        }

        private static double CandidateConfidence(
            string selected,
            string bestName,
            double bestConfidence,
            string secondName,
            double secondConfidence)
        {
            string selectedKey = GoblinAreaResolver.NormalizedKey(selected);
            if (selectedKey == GoblinAreaResolver.NormalizedKey(bestName))
            {
                return bestConfidence;
            }

            if (selectedKey == GoblinAreaResolver.NormalizedKey(secondName))
            {
                return secondConfidence;
            }

            return 0;
        }

        private static string AmbiguityGroup(string first, string second)
        {
            if (SameLevel(first, second, 1) && HasPandemoniumAndChannel(first, second))
            {
                return "ChannelVsPandemonium";
            }

            if (SameLevel(first, second, 2) && HasPandemoniumAndChannel(first, second))
            {
                return "ChannelVsPandemonium";
            }

            if (SameLevel(first, second, 1) && HasPandemoniumAndCaverns(first, second))
            {
                return "CavernsVsPandemonium";
            }

            if (SameLevel(first, second, 2) && HasPandemoniumAndCaverns(first, second))
            {
                return "CavernsVsPandemonium";
            }

            if (SameLevel(first, second, 1) && HasPandemoniumAndMoonClanCave(first, second))
            {
                return "MoonClanVsPandemonium";
            }

            if (SameLevel(first, second, 2) && HasPandemoniumAndMoonClanCave(first, second))
            {
                return "MoonClanVsPandemonium";
            }

            if (SameLevel(first, second, 1) && HasPandemoniumAndCathedral(first, second))
            {
                return "CathedralVsPandemonium";
            }

            if (SameLevel(first, second, 2) && HasPandemoniumAndCathedral(first, second))
            {
                return "CathedralVsPandemonium";
            }

            return "";
        }

        private static bool SameLevel(string first, string second, int level)
        {
            return GoblinAreaResolver.NormalizedKey(first).EndsWith($"level {level}", StringComparison.OrdinalIgnoreCase) &&
                GoblinAreaResolver.NormalizedKey(second).EndsWith($"level {level}", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasPandemoniumAndChannel(string first, string second)
        {
            return (IsPandemonium(first) && IsChannel(second)) ||
                (IsPandemonium(second) && IsChannel(first));
        }

        private static bool HasPandemoniumAndCaverns(string first, string second)
        {
            return (IsPandemonium(first) && IsCavernsOfFrost(second)) ||
                (IsPandemonium(second) && IsCavernsOfFrost(first));
        }

        private static bool HasPandemoniumAndMoonClanCave(string first, string second)
        {
            return (IsPandemonium(first) && IsMoonClanCave(second)) ||
                (IsPandemonium(second) && IsMoonClanCave(first));
        }

        private static bool HasPandemoniumAndCathedral(string first, string second)
        {
            return (IsPandemonium(first) && IsCathedral(second)) ||
                (IsPandemonium(second) && IsCathedral(first));
        }

        private static bool IsPandemonium(string location)
        {
            string key = GoblinAreaResolver.NormalizedKey(location);
            return key == GoblinAreaResolver.NormalizedKey("Pandemonium Fortress Level 1") ||
                key == GoblinAreaResolver.NormalizedKey("Pandemonium Fortress Level 2");
        }

        private static bool IsChannel(string location)
        {
            string key = GoblinAreaResolver.NormalizedKey(location);
            return key == GoblinAreaResolver.NormalizedKey("Western Channel Level 1") ||
                key == GoblinAreaResolver.NormalizedKey("Western Channel Level 2") ||
                key == GoblinAreaResolver.NormalizedKey("Eastern Channel Level 1") ||
                key == GoblinAreaResolver.NormalizedKey("Eastern Channel Level 2");
        }

        private static bool IsCavernsOfFrost(string location)
        {
            string key = GoblinAreaResolver.NormalizedKey(location);
            return key == GoblinAreaResolver.NormalizedKey("Caverns of Frost Level 1") ||
                key == GoblinAreaResolver.NormalizedKey("Caverns of Frost Level 2");
        }

        private static bool IsMoonClanCave(string location)
        {
            string key = GoblinAreaResolver.NormalizedKey(location);
            return key == GoblinAreaResolver.NormalizedKey("Cave Of The Moon Clan Level 1") ||
                key == GoblinAreaResolver.NormalizedKey("Cave Of The Moon Clan Level 2");
        }

        private static bool IsCathedral(string location)
        {
            string key = GoblinAreaResolver.NormalizedKey(location);
            return key == GoblinAreaResolver.NormalizedKey("Cathedral") ||
                key == GoblinAreaResolver.NormalizedKey("Cathedral Level 1") ||
                key == GoblinAreaResolver.NormalizedKey("Cathedral Level 2") ||
                key == GoblinAreaResolver.NormalizedKey("Cathedral Level 3");
        }

        private static bool IsAncientWaterwayContext(string location)
        {
            string key = GoblinAreaResolver.NormalizedKey(location);
            return key == GoblinAreaResolver.NormalizedKey("Ancient Waterway") ||
                IsChannel(location);
        }

        private static bool IsBattlefieldsContext(string location)
        {
            string key = GoblinAreaResolver.NormalizedKey(location);
            return key == GoblinAreaResolver.NormalizedKey("Battlefields") ||
                key == GoblinAreaResolver.NormalizedKey("The Battlefields") ||
                key == GoblinAreaResolver.NormalizedKey("Fields of Slaughter") ||
                IsCavernsOfFrost(location);
        }

        private static bool IsSouthernHighlandsContext(string location)
        {
            string key = GoblinAreaResolver.NormalizedKey(location);
            return key == GoblinAreaResolver.NormalizedKey("Southern Highlands") ||
                IsMoonClanCave(location);
        }

        private static bool IsCathedralContext(string location)
        {
            return IsCathedral(location);
        }
    }

    internal static class GoblinManualCountBlockList
    {
        private static readonly HashSet<string> BlockedAreaKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            GoblinAreaResolver.NormalizedKey("Ancient Waterway"),
            GoblinAreaResolver.NormalizedKey("Caldeum Bazaar"),
            GoblinAreaResolver.NormalizedKey("City of Caldeum"),
            GoblinAreaResolver.NormalizedKey("Flooded Causeway"),
            GoblinAreaResolver.NormalizedKey("Gates of Caldeum"),
            GoblinAreaResolver.NormalizedKey("New Tristram"),
            GoblinAreaResolver.NormalizedKey("The Bridge Of Korsikk"),
            GoblinAreaResolver.NormalizedKey("WhimsyDale"),
        };

        public static bool IsBlocked(string areaKey)
        {
            return !string.IsNullOrWhiteSpace(areaKey) && BlockedAreaKeys.Contains(GoblinAreaResolver.NormalizedKey(areaKey));
        }
    }

    internal static class GoblinTrackerDebugSimulationAreas
    {
        public static IReadOnlyList<string> DropdownItems()
        {
            List<string> items = ["Current Area"];
            SortedSet<string> sortedAreas = new(StringComparer.OrdinalIgnoreCase);
            foreach (string area in GoblinAreaResolver.KnownAreas)
            {
                AddIfKnown(sortedAreas, area);
            }

            items.AddRange(sortedAreas);
            return items;
        }

        private static void AddIfKnown(SortedSet<string> items, string area)
        {
            GoblinAreaResolution resolved = GoblinAreaResolver.Resolve(area);
            if (!resolved.Resolved)
            {
                return;
            }

            items.Add(resolved.AreaKey);
        }
    }

    internal static class GoblinAutoCountEvidenceReliabilityPolicy
    {
        public const string JournalPendingKilledOrMinimapConfirmation = "JournalPendingKilledOrMinimapConfirmation";
        public const string JournalEngagedSustainedActiveCombat = "JournalEngagedSustainedActiveCombat";
        public const string JournalEngagedHighConfidenceFreshCombat = "JournalEngagedHighConfidenceFreshCombat";
        public const string JournalEvidenceKindUnknown = "JournalEvidenceKindUnknown";
        public static readonly TimeSpan JournalEngagedSustainedActiveCombatMinimumAge = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan JournalEngagedSustainedActiveCombatMaximumAge = TimeSpan.FromSeconds(8);
        public static readonly TimeSpan JournalEngagedHighConfidenceFreshCombatMinimumAge = TimeSpan.FromMilliseconds(25);
        public const double JournalEngagedHighConfidenceFreshCombatMinimumConfidence = 0.95;

        public static bool AllowsAutomaticCount(
            string source,
            string evidenceSignature,
            double evidenceFirstSeenAgeSeconds,
            bool combatActive,
            double evidenceConfidence,
            out string reason,
            out string reliability)
        {
            reason = "";
            reliability = "Unknown";
            string normalizedSource = NormalizeObservationSource(source);
            if (normalizedSource.Equals("Minimap", StringComparison.OrdinalIgnoreCase))
            {
                reliability = "MinimapConfirmed";
                return true;
            }

            if (!normalizedSource.Equals("Journal", StringComparison.OrdinalIgnoreCase))
            {
                reliability = "NonJournalEvidence";
                return true;
            }

            string evidenceKind = ExtractEvidenceSignatureValue(evidenceSignature, "Kind");
            if (evidenceKind.Equals(nameof(GoblinEvidenceTemplateKind.JournalKilled), StringComparison.OrdinalIgnoreCase))
            {
                reliability = "JournalKilledConfirmed";
                return true;
            }

            if (evidenceKind.Equals(nameof(GoblinEvidenceTemplateKind.JournalEngaged), StringComparison.OrdinalIgnoreCase) ||
                evidenceKind.Equals(nameof(GoblinEvidenceTemplateKind.JournalEngagedAndKilled), StringComparison.OrdinalIgnoreCase))
            {
                if (combatActive &&
                    evidenceConfidence >= JournalEngagedHighConfidenceFreshCombatMinimumConfidence &&
                    evidenceFirstSeenAgeSeconds >= JournalEngagedHighConfidenceFreshCombatMinimumAge.TotalSeconds &&
                    evidenceFirstSeenAgeSeconds < JournalEngagedSustainedActiveCombatMinimumAge.TotalSeconds)
                {
                    reliability = JournalEngagedHighConfidenceFreshCombat;
                    return true;
                }

                if (combatActive &&
                    evidenceFirstSeenAgeSeconds >= JournalEngagedSustainedActiveCombatMinimumAge.TotalSeconds &&
                    evidenceFirstSeenAgeSeconds <= JournalEngagedSustainedActiveCombatMaximumAge.TotalSeconds)
                {
                    reliability = JournalEngagedSustainedActiveCombat;
                    return true;
                }

                reason = JournalPendingKilledOrMinimapConfirmation;
                reliability = "JournalEngagedOnly";
                return false;
            }

            reason = JournalEvidenceKindUnknown;
            reliability = string.IsNullOrWhiteSpace(evidenceKind)
                ? "JournalKindMissing"
                : $"JournalKind:{evidenceKind}";
            return false;
        }

        public static bool AllowsAutomaticCount(
            string source,
            string evidenceSignature,
            out string reason,
            out string reliability)
        {
            return AllowsAutomaticCount(
                source,
                evidenceSignature,
                evidenceFirstSeenAgeSeconds: 0,
                combatActive: false,
                evidenceConfidence: 0,
                out reason,
                out reliability);
        }

        private static string NormalizeObservationSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return "";
            }

            if (source.Contains("Journal", StringComparison.OrdinalIgnoreCase))
            {
                return "Journal";
            }

            if (source.Contains("Minimap", StringComparison.OrdinalIgnoreCase))
            {
                return "Minimap";
            }

            return source.Trim();
        }

        internal static string ExtractEvidenceSignatureValue(string evidenceSignature, string key)
        {
            if (string.IsNullOrWhiteSpace(evidenceSignature) || string.IsNullOrWhiteSpace(key))
            {
                return "";
            }

            string[] parts = evidenceSignature.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string part in parts)
            {
                int separator = part.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                string partKey = part[..separator].Trim();
                if (partKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return part[(separator + 1)..].Trim();
                }
            }

            if (key.Equals("Kind", StringComparison.OrdinalIgnoreCase) && parts.Length > 0)
            {
                string first = parts[0].Trim();
                if (first.Equals(nameof(GoblinEvidenceTemplateKind.JournalKilled), StringComparison.OrdinalIgnoreCase) ||
                    first.Equals(nameof(GoblinEvidenceTemplateKind.JournalEngaged), StringComparison.OrdinalIgnoreCase) ||
                    first.Equals(nameof(GoblinEvidenceTemplateKind.JournalEngagedAndKilled), StringComparison.OrdinalIgnoreCase))
                {
                    return first;
                }
            }

            return "";
        }
    }

    internal static class GoblinAutoCountEncounterSuppressionPolicy
    {
        private static readonly TimeSpan CrossAreaJournalVisibleRowSuppressWindow = TimeSpan.FromSeconds(45);

        public static bool ShouldSuppress(
            string source,
            string goblinType,
            string areaKey,
            string globalEvidenceKey,
            string countedGoblinType,
            string countedAreaKey,
            string countedSource,
            string countedEvidenceKey,
            DateTime countedUtc,
            DateTime lastSeenUtc,
            DateTime nowUtc,
            TimeSpan encounterSuppressWindow,
            TimeSpan sourceVariantWindow,
            out string matchReason)
        {
            matchReason = "";
            if (string.IsNullOrWhiteSpace(areaKey) ||
                string.IsNullOrWhiteSpace(countedAreaKey) ||
                !GoblinTypeNormalizer.Normalize(countedGoblinType).Equals(GoblinTypeNormalizer.Normalize(goblinType), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            TimeSpan encounterAge = nowUtc - countedUtc;
            TimeSpan lastSeenAge = nowUtc - lastSeenUtc;
            if (encounterAge > encounterSuppressWindow &&
                lastSeenAge > sourceVariantWindow)
            {
                return false;
            }

            string normalizedSource = NormalizeObservationSource(source);
            string normalizedCountedSource = NormalizeObservationSource(countedSource);
            if (!IsGoblinObservationEvidenceSource(normalizedSource) ||
                !IsGoblinObservationEvidenceSource(normalizedCountedSource))
            {
                return false;
            }

            bool sameArea = GoblinAreaResolver.NormalizedKey(areaKey).Equals(
                GoblinAreaResolver.NormalizedKey(countedAreaKey),
                StringComparison.OrdinalIgnoreCase);
            bool currentJournal = normalizedSource.Equals("Journal", StringComparison.OrdinalIgnoreCase);
            bool countedJournal = normalizedCountedSource.Equals("Journal", StringComparison.OrdinalIgnoreCase);
            bool currentMinimap = normalizedSource.Equals("Minimap", StringComparison.OrdinalIgnoreCase);
            bool countedMinimap = normalizedCountedSource.Equals("Minimap", StringComparison.OrdinalIgnoreCase);
            bool bothMinimap = normalizedSource.Equals("Minimap", StringComparison.OrdinalIgnoreCase) &&
                normalizedCountedSource.Equals("Minimap", StringComparison.OrdinalIgnoreCase);
            bool recentOrContinuouslySeen = encounterAge <= sourceVariantWindow || lastSeenAge <= sourceVariantWindow;
            bool recentCrossAreaJournalRow = sameArea ||
                !currentJournal ||
                !countedJournal ||
                encounterAge <= CrossAreaJournalVisibleRowSuppressWindow;
            bool continuingCrossAreaJournalRow = !sameArea &&
                currentJournal &&
                countedJournal &&
                lastSeenAge <= sourceVariantWindow;
            bool sameVisibleJournalRowCanSuppress = recentCrossAreaJournalRow || continuingCrossAreaJournalRow;

            if (encounterAge <= encounterSuppressWindow &&
                !string.IsNullOrWhiteSpace(globalEvidenceKey) &&
                !string.IsNullOrWhiteSpace(countedEvidenceKey) &&
                string.Equals(countedEvidenceKey, globalEvidenceKey, StringComparison.OrdinalIgnoreCase))
            {
                if (bothMinimap)
                {
                    if (sameArea)
                    {
                        matchReason = "SameEvidenceKey";
                        return true;
                    }
                }
                else if (sameArea ||
                    (!currentMinimap || !countedJournal) &&
                    (!currentJournal && !countedJournal || recentOrContinuouslySeen) &&
                    sameVisibleJournalRowCanSuppress)
                {
                    matchReason = "SameEvidenceKey";
                    return true;
                }
            }

            if (encounterAge <= encounterSuppressWindow &&
                currentJournal &&
                countedJournal &&
                sameVisibleJournalRowCanSuppress &&
                JournalEvidenceBucketsMatch(globalEvidenceKey, countedEvidenceKey, out int currentBucket, out int countedBucket))
            {
                matchReason = $"JournalLineBucket:{currentBucket}->{countedBucket}";
                return true;
            }

            if (recentOrContinuouslySeen &&
                recentCrossAreaJournalRow &&
                (sameArea || !currentMinimap || !countedJournal) &&
                (currentJournal ||
                countedJournal ||
                !normalizedSource.Equals(normalizedCountedSource, StringComparison.OrdinalIgnoreCase)))
            {
                matchReason = encounterAge <= sourceVariantWindow
                    ? $"RecentSourceVariant:{normalizedCountedSource}->{normalizedSource}"
                    : $"RecentSourceVariantLastSeen:{normalizedCountedSource}->{normalizedSource}";
                return true;
            }

            return false;
        }

        public static bool ShouldRefreshEncounterLastSeenAfterSuppression(
            string source,
            string areaKey,
            string countedAreaKey)
        {
            if (string.IsNullOrWhiteSpace(areaKey) ||
                string.IsNullOrWhiteSpace(countedAreaKey))
            {
                return false;
            }

            bool sameArea = GoblinAreaResolver.NormalizedKey(areaKey).Equals(
                GoblinAreaResolver.NormalizedKey(countedAreaKey),
                StringComparison.OrdinalIgnoreCase);
            if (sameArea)
            {
                return IsGoblinObservationEvidenceSource(source);
            }

            string normalizedSource = NormalizeObservationSource(source);
            return IsGoblinObservationEvidenceSource(normalizedSource) &&
                !normalizedSource.Equals("Journal", StringComparison.OrdinalIgnoreCase);
        }

        public static bool ShouldRefreshEncounterLastSeenAfterAreaAlreadyCounted(
            string source,
            string areaKey,
            string countedAreaKey)
        {
            if (string.IsNullOrWhiteSpace(areaKey) ||
                string.IsNullOrWhiteSpace(countedAreaKey))
            {
                return false;
            }

            bool sameArea = GoblinAreaResolver.NormalizedKey(areaKey).Equals(
                GoblinAreaResolver.NormalizedKey(countedAreaKey),
                StringComparison.OrdinalIgnoreCase);
            return sameArea && IsGoblinObservationEvidenceSource(source);
        }

        public static bool JournalEvidenceBucketsMatch(string currentEvidenceKey, string countedEvidenceKey, out int currentBucket, out int countedBucket)
        {
            currentBucket = -1;
            countedBucket = -1;
            if (!TryParseJournalEvidenceLineBucket(currentEvidenceKey, out currentBucket) ||
                !TryParseJournalEvidenceLineBucket(countedEvidenceKey, out countedBucket))
            {
                return false;
            }

            if (!JournalEvidenceLineFamily(currentEvidenceKey).Equals(
                JournalEvidenceLineFamily(countedEvidenceKey),
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return Math.Abs(currentBucket - countedBucket) <= GoblinJournalEvidencePolicy.NearbyLineBucketMaximumDelta;
        }

        private static string JournalEvidenceLineFamily(string evidenceKey)
        {
            if (string.IsNullOrWhiteSpace(evidenceKey))
            {
                return "";
            }

            List<string> familyParts = [];
            foreach (string part in evidenceKey.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part.StartsWith("LineBucket=", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (part.StartsWith("Template=", StringComparison.OrdinalIgnoreCase) ||
                    part.StartsWith("Kind=", StringComparison.OrdinalIgnoreCase))
                {
                    familyParts.Add(part);
                }
            }

            return string.Join("|", familyParts);
        }

        private static bool TryParseJournalEvidenceLineBucket(string evidenceKey, out int lineBucket)
        {
            lineBucket = -1;
            if (string.IsNullOrWhiteSpace(evidenceKey))
            {
                return false;
            }

            foreach (string part in evidenceKey.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                const string prefix = "LineBucket=";
                if (part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(part[prefix.Length..], out int parsed))
                {
                    lineBucket = parsed;
                    return true;
                }
            }

            return false;
        }

        private static bool IsGoblinObservationEvidenceSource(string source)
        {
            string normalizedSource = NormalizeObservationSource(source);
            return normalizedSource.Equals("Journal", StringComparison.OrdinalIgnoreCase) ||
                normalizedSource.Equals("Minimap", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeObservationSource(string source)
        {
            if (string.Equals(source, "JournalCandidate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source, "Journal", StringComparison.OrdinalIgnoreCase))
            {
                return "Journal";
            }

            if (string.Equals(source, "MinimapCandidate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source, "Minimap", StringComparison.OrdinalIgnoreCase))
            {
                return "Minimap";
            }

            return string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
        }
    }

    internal static class GoblinPandemoniumMultiCountDuplicatePolicy
    {
        public static readonly TimeSpan MinimumElapsedSinceLastAccepted = TimeSpan.FromSeconds(8);

        public static bool ShouldBypass(
            string source,
            string areaKey,
            int currentAreaCount,
            int areaLimit,
            string countedAreaKey,
            DateTime countedUtc,
            DateTime nowUtc,
            string evidenceKey,
            double evidenceConfidence,
            double minimapMinimumConfidence,
            double evidenceFirstSeenAgeSeconds,
            bool combatActive,
            out string reason,
            out double elapsedSinceLastAcceptedSeconds)
        {
            reason = "";
            elapsedSinceLastAcceptedSeconds = Math.Max(0, (nowUtc - countedUtc).TotalSeconds);

            if (!IsPandemoniumFortressTwoCountArea(areaKey))
            {
                reason = "NotPandemoniumFortressTwoCountArea";
                return false;
            }

            if (areaLimit != 2)
            {
                reason = "AreaLimitNotTwo";
                return false;
            }

            if (currentAreaCount >= areaLimit)
            {
                reason = "AreaLimitReached";
                return false;
            }

            if (string.IsNullOrWhiteSpace(countedAreaKey) ||
                !GoblinAreaResolver.NormalizedKey(areaKey).Equals(GoblinAreaResolver.NormalizedKey(countedAreaKey), StringComparison.OrdinalIgnoreCase))
            {
                reason = "PreviousAcceptedDifferentArea";
                return false;
            }

            if (nowUtc - countedUtc < MinimumElapsedSinceLastAccepted)
            {
                reason = "ElapsedTooShort";
                return false;
            }

            if (!HasFreshSupportingEvidence(
                source,
                evidenceKey,
                evidenceConfidence,
                minimapMinimumConfidence,
                evidenceFirstSeenAgeSeconds,
                combatActive))
            {
                reason = "NoFreshSupportingEvidence";
                return false;
            }

            reason = "Allowed";
            return true;
        }

        public static bool IsPandemoniumFortressTwoCountArea(string areaKey)
        {
            string normalized = GoblinAreaResolver.NormalizedKey(areaKey);
            return normalized == GoblinAreaResolver.NormalizedKey("Pandemonium Fortress Level 1") ||
                normalized == GoblinAreaResolver.NormalizedKey("Pandemonium Fortress Level 2");
        }

        private static bool HasFreshSupportingEvidence(
            string source,
            string evidenceKey,
            double evidenceConfidence,
            double minimapMinimumConfidence,
            double evidenceFirstSeenAgeSeconds,
            bool combatActive)
        {
            string normalizedSource = NormalizeObservationSource(source);
            if (normalizedSource.Equals("Minimap", StringComparison.OrdinalIgnoreCase))
            {
                return evidenceConfidence >= minimapMinimumConfidence;
            }

            if (!normalizedSource.Equals("Journal", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool engagedEvidence = evidenceKey.Contains("Kind=JournalEngaged", StringComparison.OrdinalIgnoreCase) ||
                evidenceKey.Contains("Kind=JournalEngagedAndKilled", StringComparison.OrdinalIgnoreCase);
            return engagedEvidence &&
                combatActive &&
                ((evidenceConfidence >= GoblinAutoCountEvidenceReliabilityPolicy.JournalEngagedHighConfidenceFreshCombatMinimumConfidence &&
                    evidenceFirstSeenAgeSeconds >= GoblinAutoCountEvidenceReliabilityPolicy.JournalEngagedHighConfidenceFreshCombatMinimumAge.TotalSeconds &&
                    evidenceFirstSeenAgeSeconds < GoblinAutoCountEvidenceReliabilityPolicy.JournalEngagedSustainedActiveCombatMinimumAge.TotalSeconds) ||
                (evidenceFirstSeenAgeSeconds >= GoblinAutoCountEvidenceReliabilityPolicy.JournalEngagedSustainedActiveCombatMinimumAge.TotalSeconds &&
                    evidenceFirstSeenAgeSeconds <= GoblinAutoCountEvidenceReliabilityPolicy.JournalEngagedSustainedActiveCombatMaximumAge.TotalSeconds));
        }

        private static string NormalizeObservationSource(string source)
        {
            if (string.Equals(source, "JournalCandidate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source, "Journal", StringComparison.OrdinalIgnoreCase))
            {
                return "Journal";
            }

            if (string.Equals(source, "MinimapCandidate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source, "Minimap", StringComparison.OrdinalIgnoreCase))
            {
                return "Minimap";
            }

            return string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
        }
    }

    internal static class GoblinPandemoniumSecondSlotJournalReliabilityPolicy
    {
        public const string Reliability = "PandemoniumSecondSlotJournalEngaged";
        public const double MinimumConfidence = 0.90;
        public static readonly TimeSpan MaximumFirstSeenAge = TimeSpan.FromSeconds(8);

        public static bool AllowsAutomaticCount(
            string source,
            string evidenceKey,
            string areaKey,
            int currentAreaCount,
            int areaLimit,
            string currentAreaAtAcceptance,
            string firstSeenArea,
            double evidenceFirstSeenAgeSeconds,
            double evidenceConfidence,
            out string reason,
            out string reliability)
        {
            reason = "";
            reliability = "";

            if (!GoblinPandemoniumMultiCountDuplicatePolicy.IsPandemoniumFortressTwoCountArea(areaKey))
            {
                reason = "NotPandemoniumFortressTwoCountArea";
                return false;
            }

            if (areaLimit != 2)
            {
                reason = "AreaLimitNotTwo";
                return false;
            }

            if (currentAreaCount <= 0)
            {
                reason = "NoPriorPandemoniumCount";
                return false;
            }

            if (currentAreaCount >= areaLimit)
            {
                reason = "AreaLimitReached";
                return false;
            }

            if (!string.Equals(NormalizeObservationSource(source), "Journal", StringComparison.OrdinalIgnoreCase))
            {
                reason = "SourceNotJournal";
                return false;
            }

            bool engagedEvidence = evidenceKey.Contains("Kind=JournalEngaged", StringComparison.OrdinalIgnoreCase) ||
                evidenceKey.Contains("Kind=JournalEngagedAndKilled", StringComparison.OrdinalIgnoreCase);
            if (!engagedEvidence)
            {
                reason = "EvidenceNotJournalEngaged";
                return false;
            }

            if (evidenceConfidence < MinimumConfidence)
            {
                reason = "ConfidenceTooLow";
                return false;
            }

            if (evidenceFirstSeenAgeSeconds < 0 ||
                evidenceFirstSeenAgeSeconds > MaximumFirstSeenAge.TotalSeconds)
            {
                reason = "EvidenceFirstSeenAgeOutOfRange";
                return false;
            }

            if (!AreaMatches(areaKey, currentAreaAtAcceptance))
            {
                reason = "CurrentAreaMismatch";
                return false;
            }

            if (!AreaMatches(areaKey, firstSeenArea))
            {
                reason = "FirstSeenAreaMismatch";
                return false;
            }

            reliability = Reliability;
            return true;
        }

        private static bool AreaMatches(string expectedAreaKey, string actualAreaKey)
        {
            if (string.IsNullOrWhiteSpace(expectedAreaKey) || string.IsNullOrWhiteSpace(actualAreaKey))
            {
                return false;
            }

            return GoblinAreaResolver.NormalizedKey(expectedAreaKey)
                .Equals(GoblinAreaResolver.NormalizedKey(actualAreaKey), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeObservationSource(string source)
        {
            if (string.Equals(source, "JournalCandidate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source, "Journal", StringComparison.OrdinalIgnoreCase))
            {
                return "Journal";
            }

            if (string.Equals(source, "MinimapCandidate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source, "Minimap", StringComparison.OrdinalIgnoreCase))
            {
                return "Minimap";
            }

            return string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
        }
    }

    internal static class GoblinTypeNormalizer
    {
        private static readonly Dictionary<string, string> CanonicalTypes = BuildCanonicalTypes();

        public static string Normalize(string goblinType)
        {
            string key = GoblinAreaResolver.NormalizedKey(StripMarkup(goblinType));
            if (string.IsNullOrWhiteSpace(key))
            {
                return "Unknown";
            }

            return CanonicalTypes.TryGetValue(key, out string? canonical)
                ? canonical
                : TitleCase(key);
        }

        public static bool IsKnownGoblinType(string value)
        {
            string key = GoblinAreaResolver.NormalizedKey(StripMarkup(value));
            return !string.IsNullOrWhiteSpace(key) && CanonicalTypes.ContainsKey(key);
        }

        private static Dictionary<string, string> BuildCanonicalTypes()
        {
            string[] canonicalNames =
            [
                "Blood Thief",
                "Gem Hoarder",
                "Gelatinous Sire",
                "Gilded Baron",
                "Insufferable Miscreant",
                "Malevolent Tormentor",
                "Menagerist",
                "Odious Collector",
                "Rainbow Goblin",
                "Treasure Goblin",
            ];

            Dictionary<string, string> types = new(StringComparer.OrdinalIgnoreCase);
            foreach (string name in canonicalNames)
            {
                types[GoblinAreaResolver.NormalizedKey(name)] = name;
            }

            AddAlias(types, "Gelatinous Spawn", "Gelatinous Sire");
            AddAlias(types, "Menagerist Goblin", "Menagerist");
            AddAlias(types, "Oddius Collector", "Odious Collector");
            return types;
        }

        private static void AddAlias(Dictionary<string, string> types, string alias, string canonical)
        {
            types[GoblinAreaResolver.NormalizedKey(alias)] = canonical;
        }

        private static string StripMarkup(string value)
        {
            return (value ?? "")
                .Replace("<", "")
                .Replace(">", "")
                .Trim()
                .Trim('.', '!', '?', ':', ';', ',', '"', '\'');
        }

        private static string TitleCase(string normalizedKey)
        {
            return string.Join(" ", normalizedKey
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
        }
    }

    internal static class GoblinJournalParser
    {
        public static IReadOnlyList<GoblinJournalEvent> ParseEvents(string text)
        {
            List<GoblinJournalEvent> events = [];
            if (string.IsNullOrWhiteSpace(text))
            {
                return events;
            }

            foreach (string rawLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                GoblinJournalEvent? parsed = ParseLine(rawLine);
                if (parsed != null)
                {
                    events.Add(parsed);
                }
            }

            return events;
        }

        public static GoblinJournalEvent? ParseLine(string line)
        {
            string cleaned = CleanLine(line);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return null;
            }

            System.Text.RegularExpressions.Match killed = System.Text.RegularExpressions.Regex.Match(
                cleaned,
                @"\bhas\s+killed\s+(?:an?\s+)?(?<name><[^>]+>|[A-Za-z][A-Za-z\s']*?)\s*[.!]?\s*$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (killed.Success)
            {
                string goblinType = GoblinTypeNormalizer.Normalize(killed.Groups["name"].Value);
                if (IsGoblinEvidence(killed.Groups["name"].Value, goblinType))
                {
                    return new GoblinJournalEvent(GoblinJournalEventKind.Killed, goblinType, cleaned);
                }
            }

            System.Text.RegularExpressions.Match escaped = System.Text.RegularExpressions.Regex.Match(
                cleaned,
                @"^(?:an?\s+)?(?<name><[^>]+>|[A-Za-z][A-Za-z\s']*?)\s+has\s+escaped\s*[.!]?\s*$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (escaped.Success)
            {
                string goblinType = GoblinTypeNormalizer.Normalize(escaped.Groups["name"].Value);
                if (IsGoblinEvidence(escaped.Groups["name"].Value, goblinType))
                {
                    return new GoblinJournalEvent(GoblinJournalEventKind.Escaped, goblinType, cleaned);
                }
            }

            return null;
        }

        private static bool IsGoblinEvidence(string rawName, string normalizedName)
        {
            return GoblinAreaResolver.NormalizedKey(rawName).Contains("goblin", StringComparison.OrdinalIgnoreCase) ||
                GoblinTypeNormalizer.IsKnownGoblinType(rawName) ||
                GoblinTypeNormalizer.IsKnownGoblinType(normalizedName);
        }

        private static string CleanLine(string line)
        {
            return System.Text.RegularExpressions.Regex.Replace((line ?? "").Replace("\ufeff", "").Trim(), @"\s+", " ");
        }
    }

    internal readonly record struct GoblinAreaDuplicateGuardResult(
        bool Accepted,
        int AreaCount,
        int AreaLimit);

    internal static class GoblinDecisionTracePolicy
    {
        public static string DecisionForReason(string reason, bool counted)
        {
            if (counted)
            {
                return "Count";
            }

            return reason switch
            {
                "EvidenceSeenBeforeAutoCountEnabled" or "StaleEvidence" => "Stale",
                "BlockedArea" or "AreaUnresolved" or "AmbiguousAreaDetection" => "Block",
                "AreaAlreadyCounted" or "AreaLimitReached" or "EvidenceAlreadyAutoCounted" or "EncounterAlreadyAutoCounted" => "Duplicate",
                "AutomaticCountingDisabled" => "ObserveOnly",
                _ => "Suppress",
            };
        }

        public static GoblinDecisionTraceRecord Create(
            DateTime timestampUtc,
            string mode,
            string source,
            string imageFile,
            string imagePath,
            string areaRaw,
            string areaKey,
            string goblinType,
            string evidenceSignature,
            double evidenceAgeSeconds,
            double evidenceFirstSeenAgeSeconds,
            bool autoCountEnabled,
            bool observationModeEnabled,
        string suppressionReason,
        bool counted,
        int areaCountBefore,
        int areaLimit,
        int totalGoblinCountBefore,
        string correlationId = "")
        {
            correlationId = string.IsNullOrWhiteSpace(correlationId)
                ? CreateCorrelationId(timestampUtc, mode, source, evidenceSignature, imageFile, imagePath)
                : correlationId.Trim();
            string reason = counted
                ? "Eligible"
                : string.IsNullOrWhiteSpace(suppressionReason) ? "Suppressed" : suppressionReason;
            bool evidencePredatesAutoCount = reason.Equals("EvidenceSeenBeforeAutoCountEnabled", StringComparison.OrdinalIgnoreCase);
            bool fresh = !reason.Equals("StaleEvidence", StringComparison.OrdinalIgnoreCase) && !evidencePredatesAutoCount;
            bool allowedArea = !reason.Equals("BlockedArea", StringComparison.OrdinalIgnoreCase) &&
                !reason.Equals("AreaUnresolved", StringComparison.OrdinalIgnoreCase) &&
                !reason.Equals("AmbiguousAreaDetection", StringComparison.OrdinalIgnoreCase);
            string blockedReason = allowedArea ? "" : reason;

            return new GoblinDecisionTraceRecord(
                timestampUtc,
                correlationId,
                string.IsNullOrWhiteSpace(mode) ? "Live" : mode.Trim(),
                string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim(),
                Path.GetFileName(imageFile ?? ""),
                imagePath ?? "",
                areaRaw ?? "",
                areaKey ?? "",
                GoblinTypeNormalizer.Normalize(goblinType),
                evidenceSignature ?? "",
                Math.Max(0, evidenceAgeSeconds),
                Math.Max(0, evidenceFirstSeenAgeSeconds),
                autoCountEnabled,
                observationModeEnabled,
                evidencePredatesAutoCount,
                fresh,
                allowedArea,
                blockedReason,
                string.IsNullOrWhiteSpace(areaKey) ? "Unknown" : GoblinAreaResolver.NormalizedKey(areaKey),
                Math.Max(0, areaCountBefore),
                Math.Max(0, areaLimit),
                Math.Max(0, totalGoblinCountBefore),
                DecisionForReason(reason, counted),
                reason,
                counted);
        }

        public static string ToLogLine(GoblinDecisionTraceRecord trace)
        {
            return "GoblinDecisionTrace: " +
                $"correlationId={LogValue(trace.CorrelationId)}; " +
                $"mode={LogValue(trace.Mode)}; " +
                $"source={LogValue(trace.Source)}; " +
                $"imageFile={LogValue(trace.ImageFile)}; " +
                $"imagePath={LogValue(trace.ImagePath)}; " +
                $"areaRaw={LogValue(trace.AreaRaw)}; " +
                $"areaKey={LogValue(trace.AreaKey)}; " +
                $"goblinType={LogValue(trace.GoblinType)}; " +
                $"evidenceSignature={LogValue(trace.EvidenceSignature)}; " +
                $"evidenceAgeSeconds={trace.EvidenceAgeSeconds:0.0}; " +
                $"evidenceFirstSeenAgeSeconds={trace.EvidenceFirstSeenAgeSeconds:0.0}; " +
                $"autoCountEnabled={trace.AutoCountEnabled}; " +
                $"observationModeEnabled={trace.ObservationModeEnabled}; " +
                $"evidencePredatesAutoCount={trace.EvidencePredatesAutoCount}; " +
                $"fresh={trace.Fresh}; " +
                $"allowedArea={trace.AllowedArea}; " +
                $"blockedReason={LogValue(trace.BlockedReason)}; " +
                $"duplicateKey={LogValue(trace.DuplicateKey)}; " +
                $"areaCountBefore={trace.AreaCountBefore}; " +
                $"areaLimit={trace.AreaLimit}; " +
                $"totalGoblinCountBefore={trace.TotalGoblinCountBefore}; " +
                $"decision={trace.Decision}; " +
                $"reason={LogValue(trace.Reason)}; " +
                $"notificationShown={trace.NotificationShown}";
        }

        public static string CreateCorrelationId(
            DateTime timestampUtc,
            string mode,
            string source,
            string evidenceSignature,
            string imageFile,
            string imagePath)
        {
            string seed = string.Join("|",
                timestampUtc.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture),
                mode ?? "",
                source ?? "",
                evidenceSignature ?? "",
                imageFile ?? "",
                imagePath ?? "");
            int hash = seed.GetHashCode(StringComparison.Ordinal);
            return $"gdt-{timestampUtc:yyyyMMddHHmmssfff}-{Math.Abs(hash):x8}";
        }

        private static string LogValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "Unknown"
                : value.Replace(";", ",").Replace(Environment.NewLine, " ").Trim();
        }
    }

    internal static class GoblinJournalAreaOverridePolicy
    {
        public static GoblinJournalAreaOverrideDecision TryUseRecentMinimapChannelArea(
            GoblinAreaResolution journalArea,
            string goblinType,
            GoblinObservationRecord? recentMinimapObservation,
            string routeContext,
            DateTime nowUtc,
            TimeSpan maxAge)
        {
            if (!journalArea.Resolved ||
                !IsPandemonium(journalArea.AreaKey) ||
                recentMinimapObservation == null ||
                !recentMinimapObservation.Source.Equals("Minimap", StringComparison.OrdinalIgnoreCase) ||
                !GoblinTypeNormalizer.Normalize(recentMinimapObservation.GoblinType).Equals(GoblinTypeNormalizer.Normalize(goblinType), StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(recentMinimapObservation.AreaKey) ||
                !IsChannel(recentMinimapObservation.AreaKey) ||
                !SameLevel(journalArea.AreaKey, recentMinimapObservation.AreaKey) ||
                !IsAncientWaterwayOrChannelContext(routeContext))
            {
                return new GoblinJournalAreaOverrideDecision(false, journalArea, "NotApplicable", -1);
            }

            double ageSeconds = Math.Max(0, (nowUtc - recentMinimapObservation.TimestampUtc).TotalSeconds);
            if (ageSeconds > maxAge.TotalSeconds)
            {
                return new GoblinJournalAreaOverrideDecision(false, journalArea, "RecentMinimapExpired", ageSeconds);
            }

            GoblinAreaResolution minimapArea = GoblinAreaResolver.Resolve(recentMinimapObservation.AreaKey);
            if (!minimapArea.Resolved)
            {
                return new GoblinJournalAreaOverrideDecision(false, journalArea, "RecentMinimapAreaUnresolved", ageSeconds);
            }

            return new GoblinJournalAreaOverrideDecision(true, minimapArea, "RecentMinimapChannelContext", ageSeconds);
        }

        private static bool SameLevel(string first, string second)
        {
            int firstLevel = ExtractLevel(first);
            return firstLevel > 0 && firstLevel == ExtractLevel(second);
        }

        private static int ExtractLevel(string location)
        {
            string key = GoblinAreaResolver.NormalizedKey(location);
            if (key.EndsWith("level 1", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (key.EndsWith("level 2", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            return 0;
        }

        private static bool IsPandemonium(string location)
        {
            string key = GoblinAreaResolver.NormalizedKey(location);
            return key == GoblinAreaResolver.NormalizedKey("Pandemonium Fortress Level 1") ||
                key == GoblinAreaResolver.NormalizedKey("Pandemonium Fortress Level 2");
        }

        private static bool IsChannel(string location)
        {
            string key = GoblinAreaResolver.NormalizedKey(location);
            return key == GoblinAreaResolver.NormalizedKey("Western Channel Level 1") ||
                key == GoblinAreaResolver.NormalizedKey("Western Channel Level 2") ||
                key == GoblinAreaResolver.NormalizedKey("Eastern Channel Level 1") ||
                key == GoblinAreaResolver.NormalizedKey("Eastern Channel Level 2");
        }

        private static bool IsAncientWaterwayOrChannelContext(string location)
        {
            string key = GoblinAreaResolver.NormalizedKey(location);
            return key == GoblinAreaResolver.NormalizedKey("Ancient Waterway") ||
                IsChannel(location);
        }
    }

    internal sealed class GoblinAreaDuplicateGuard
    {
        private readonly Dictionary<string, int> countedAreaKeys = new(StringComparer.OrdinalIgnoreCase);

        public int Count => countedAreaKeys.Count;

        public bool TryAccept(string areaKey)
        {
            return TryAccept(areaKey, out _);
        }

        public bool TryAccept(string areaKey, out GoblinAreaDuplicateGuardResult result)
        {
            if (string.IsNullOrWhiteSpace(areaKey))
            {
                result = new GoblinAreaDuplicateGuardResult(true, 0, 0);
                return true;
            }

            int limit = AreaLimit(areaKey);
            countedAreaKeys.TryGetValue(areaKey, out int currentCount);
            if (currentCount >= limit)
            {
                result = new GoblinAreaDuplicateGuardResult(false, currentCount, limit);
                return false;
            }

            int updatedCount = currentCount + 1;
            countedAreaKeys[areaKey] = updatedCount;
            result = new GoblinAreaDuplicateGuardResult(true, updatedCount, limit);
            return true;
        }

        public GoblinAreaDuplicateGuardResult Peek(string areaKey)
        {
            if (string.IsNullOrWhiteSpace(areaKey))
            {
                return new GoblinAreaDuplicateGuardResult(true, 0, 0);
            }

            int limit = AreaLimit(areaKey);
            countedAreaKeys.TryGetValue(areaKey, out int currentCount);
            return new GoblinAreaDuplicateGuardResult(currentCount < limit, currentCount, limit);
        }

        public bool TryReleaseLastAccepted(string areaKey, int expectedAreaCount)
        {
            if (string.IsNullOrWhiteSpace(areaKey) ||
                expectedAreaCount <= 0 ||
                !countedAreaKeys.TryGetValue(areaKey, out int currentCount) ||
                currentCount != expectedAreaCount)
            {
                return false;
            }

            if (currentCount <= 1)
            {
                countedAreaKeys.Remove(areaKey);
            }
            else
            {
                countedAreaKeys[areaKey] = currentCount - 1;
            }

            return true;
        }

        public int Reset()
        {
            int cleared = countedAreaKeys.Count;
            countedAreaKeys.Clear();
            return cleared;
        }

        private static int AreaLimit(string areaKey)
        {
            return areaKey.Equals("Pandemonium Fortress Level 1", StringComparison.OrdinalIgnoreCase) ||
                areaKey.Equals("Pandemonium Fortress Level 2", StringComparison.OrdinalIgnoreCase) ||
                areaKey.Equals("Stinging Winds", StringComparison.OrdinalIgnoreCase)
                    ? 2
                    : 1;
        }
    }

    internal static class GoblinObservationTypeReuse
    {
        public static string ResolveForManualCount(
            string requestedGoblinType,
            string manualAreaKey,
            GoblinObservationRecord? observation,
            DateTime nowUtc,
            TimeSpan maxAge)
        {
            string normalizedRequested = GoblinTypeNormalizer.Normalize(requestedGoblinType);
            if (!string.Equals(normalizedRequested, "Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedRequested;
            }

            if (observation == null ||
                string.IsNullOrWhiteSpace(manualAreaKey) ||
                string.IsNullOrWhiteSpace(observation.AreaKey))
            {
                return "Unknown";
            }

            if (nowUtc - observation.TimestampUtc > maxAge)
            {
                return "Unknown";
            }

            if (!string.Equals(
                GoblinAreaResolver.NormalizedKey(manualAreaKey),
                GoblinAreaResolver.NormalizedKey(observation.AreaKey),
                StringComparison.OrdinalIgnoreCase))
            {
                return "Unknown";
            }

            string observedType = GoblinTypeNormalizer.Normalize(observation.GoblinType);
            return string.Equals(observedType, "Unknown", StringComparison.OrdinalIgnoreCase)
                ? "Unknown"
                : observedType;
        }
    }

    internal static class GoblinManualCountPolicy
    {
        public static bool RequiresFreshObservationForUnknownManualCount(
            string source,
            string goblinType,
            bool areaResolved,
            bool allowUnknownManualCount,
            bool hasFreshObservation)
        {
            if (!string.Equals(source, "ManualHotkey", StringComparison.OrdinalIgnoreCase) ||
                !areaResolved ||
                allowUnknownManualCount ||
                hasFreshObservation)
            {
                return false;
            }

            return string.Equals(GoblinTypeNormalizer.Normalize(goblinType), "Unknown", StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static class GoblinJournalFreshnessPolicy
    {
        public static readonly TimeSpan AreaChangedKilledRecentEngagedWindow = TimeSpan.FromSeconds(12);

        public static bool IsFresh(DateTime firstSeenUtc, DateTime nowUtc, TimeSpan freshnessWindow)
        {
            return nowUtc - firstSeenUtc <= freshnessWindow;
        }

        public static bool EngagedIsFresh(DateTime firstSeenUtc, string firstSeenAreaKey, string areaKey, DateTime nowUtc, TimeSpan freshnessWindow)
        {
            bool areaMatches = string.IsNullOrWhiteSpace(firstSeenAreaKey) ||
                string.IsNullOrWhiteSpace(areaKey) ||
                string.Equals(firstSeenAreaKey, areaKey, StringComparison.OrdinalIgnoreCase);
            return areaMatches && IsFresh(firstSeenUtc, nowUtc, freshnessWindow);
        }

        public static bool StaleSuppressionActive(DateTime lastSeenUtc, DateTime nowUtc, TimeSpan freshnessWindow)
        {
            return nowUtc - lastSeenUtc <= freshnessWindow;
        }

        public static bool KilledHasRecentEngaged(GoblinJournalEngagedState? recentEngaged, string areaKey, DateTime nowUtc, TimeSpan freshnessWindow)
        {
            return recentEngaged != null &&
                string.Equals(recentEngaged.AreaKey, areaKey, StringComparison.OrdinalIgnoreCase) &&
                nowUtc - recentEngaged.SeenUtc <= freshnessWindow;
        }

        public static bool KilledHasRecentEngagedForAreaChangedLock(GoblinJournalEngagedState? recentEngaged, string goblinType, DateTime nowUtc)
        {
            return recentEngaged != null &&
                !string.IsNullOrWhiteSpace(recentEngaged.AreaKey) &&
                !GoblinManualCountBlockList.IsBlocked(recentEngaged.AreaKey) &&
                string.Equals(recentEngaged.GoblinType, goblinType, StringComparison.OrdinalIgnoreCase) &&
                nowUtc - recentEngaged.SeenUtc <= AreaChangedKilledRecentEngagedWindow;
        }

        public static bool KilledIsFresh(GoblinJournalKilledState? killedState, string areaKey, DateTime nowUtc, TimeSpan freshnessWindow)
        {
            return killedState != null &&
                !string.IsNullOrWhiteSpace(areaKey) &&
                !string.IsNullOrWhiteSpace(killedState.AreaKey) &&
                string.Equals(killedState.AreaKey, areaKey, StringComparison.OrdinalIgnoreCase) &&
                IsFresh(killedState.FirstSeenUtc, nowUtc, freshnessWindow);
        }
    }

    internal static class GoblinJournalEvidencePolicy
    {
        public const int ActiveFeedMinimumY = 256;
        public const int NearbyLineBucketMaximumDelta = 4;

        public static string LineSignature(
            GoblinEvidenceTemplateRequirement template,
            string goblinType,
            GoblinEvidenceTemplateMatch match)
        {
            return string.Join("|",
                template.Kind,
                GoblinTypeNormalizer.Normalize(goblinType),
                template.FileName,
                $"LineBucket={LineBucket(match.MatchPoint)}");
        }

        public static int LineBucket(System.Drawing.Point matchPoint)
        {
            return Math.Max(0, matchPoint.Y) / 32;
        }

        public static bool SameVisibleLineFamily(string currentSignature, string previousSignature, out int currentBucket, out int previousBucket)
        {
            currentBucket = -1;
            previousBucket = -1;
            string currentFamily = LineFamily(currentSignature, out currentBucket);
            string previousFamily = LineFamily(previousSignature, out previousBucket);
            return !string.IsNullOrWhiteSpace(currentFamily) &&
                currentFamily.Equals(previousFamily, StringComparison.OrdinalIgnoreCase) &&
                currentBucket >= 0 &&
                previousBucket >= 0 &&
                Math.Abs(currentBucket - previousBucket) <= NearbyLineBucketMaximumDelta;
        }

        public static bool SameVisibleGoblinLine(string currentSignature, string previousSignature, out int currentBucket, out int previousBucket)
        {
            currentBucket = -1;
            previousBucket = -1;
            string currentGoblinType = LineGoblinType(currentSignature, out currentBucket);
            string previousGoblinType = LineGoblinType(previousSignature, out previousBucket);
            return !string.IsNullOrWhiteSpace(currentGoblinType) &&
                currentGoblinType.Equals(previousGoblinType, StringComparison.OrdinalIgnoreCase) &&
                currentBucket >= 0 &&
                previousBucket >= 0 &&
                Math.Abs(currentBucket - previousBucket) <= NearbyLineBucketMaximumDelta;
        }

        private static string LineFamily(string signature, out int lineBucket)
        {
            lineBucket = -1;
            if (string.IsNullOrWhiteSpace(signature))
            {
                return "";
            }

            List<string> familyParts = [];
            foreach (string part in signature.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                const string prefix = "LineBucket=";
                if (part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    _ = int.TryParse(part[prefix.Length..], out lineBucket);
                    continue;
                }

                familyParts.Add(part);
            }

            return string.Join("|", familyParts);
        }

        private static string LineGoblinType(string signature, out int lineBucket)
        {
            lineBucket = -1;
            if (string.IsNullOrWhiteSpace(signature))
            {
                return "";
            }

            string[] parts = signature.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string part in parts)
            {
                const string prefix = "LineBucket=";
                if (part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    _ = int.TryParse(part[prefix.Length..], out lineBucket);
                    break;
                }
            }

            return parts.Length >= 2 ? GoblinTypeNormalizer.Normalize(parts[1]) : "";
        }

        public static bool AppearsInActiveFeed(GoblinEvidenceTemplateMatch match)
        {
            return match.MatchPoint.Y >= ActiveFeedMinimumY;
        }
    }
}

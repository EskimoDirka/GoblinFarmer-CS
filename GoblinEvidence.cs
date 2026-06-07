using System.Globalization;

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
        string GoblinType = "Unknown");

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
                threshold,
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
                threshold,
                GoblinTypeNormalizer.Normalize(goblinType),
                kind);
            return true;
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

        public static bool KilledIsFresh(GoblinJournalKilledState? killedState, string areaKey, DateTime nowUtc, TimeSpan freshnessWindow)
        {
            return killedState != null &&
                !string.IsNullOrWhiteSpace(areaKey) &&
                !string.IsNullOrWhiteSpace(killedState.AreaKey) &&
                string.Equals(killedState.AreaKey, areaKey, StringComparison.OrdinalIgnoreCase) &&
                IsFresh(killedState.FirstSeenUtc, nowUtc, freshnessWindow);
        }
    }
}

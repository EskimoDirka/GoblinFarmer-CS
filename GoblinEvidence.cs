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
        int CurrentAreaCount);

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
            string[] canonicalNames =
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

            Dictionary<string, string> areas = new(StringComparer.OrdinalIgnoreCase);
            foreach (string name in canonicalNames)
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
                secondConfidence < AmbiguousCandidateMinimumConfidence ||
                delta < 0 ||
                delta > AmbiguousDeltaThreshold)
            {
                return new GoblinAreaDetectionDisambiguationResult(false, false, bestName, "", "NotAmbiguous", delta);
            }

            string selected = SelectFromRouteContext(bestName, secondName, routeContext);
            if (!string.IsNullOrWhiteSpace(selected))
            {
                return new GoblinAreaDetectionDisambiguationResult(true, false, selected, ambiguityGroup, "RouteContext", delta);
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
                areaKey.Equals("Pandemonium Fortress Level 2", StringComparison.OrdinalIgnoreCase)
                    ? 2
                    : 1;
        }
    }
}

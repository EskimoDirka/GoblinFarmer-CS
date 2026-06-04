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

    internal readonly record struct GoblinAreaResolution(
        string RawLocation,
        string AreaKey,
        string DisplayLocation)
    {
        public bool Resolved => !string.IsNullOrWhiteSpace(AreaKey);
    }

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

    internal sealed class GoblinAreaDuplicateGuard
    {
        private readonly HashSet<string> countedAreaKeys = new(StringComparer.OrdinalIgnoreCase);

        public int Count => countedAreaKeys.Count;

        public bool TryAccept(string areaKey)
        {
            if (string.IsNullOrWhiteSpace(areaKey))
            {
                return true;
            }

            return countedAreaKeys.Add(areaKey);
        }

        public int Reset()
        {
            int cleared = countedAreaKeys.Count;
            countedAreaKeys.Clear();
            return cleared;
        }
    }
}

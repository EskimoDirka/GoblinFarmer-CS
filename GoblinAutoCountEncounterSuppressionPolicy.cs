namespace GoblinFarmer
{
    internal static class GoblinAutoCountEncounterSuppressionPolicy
    {
        private static readonly TimeSpan CrossAreaJournalVisibleRowSuppressWindow = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan CrossAreaJournalKilledVisibleRowSuppressWindow = TimeSpan.FromSeconds(75);
        private static readonly TimeSpan CrossAreaJournalContinuitySuppressWindow = TimeSpan.FromSeconds(30);

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
            TimeSpan visibleRowSuppressWindow = IsJournalKilledEvidence(globalEvidenceKey) || IsJournalKilledEvidence(countedEvidenceKey)
                ? CrossAreaJournalKilledVisibleRowSuppressWindow
                : CrossAreaJournalVisibleRowSuppressWindow;
            bool recentOrContinuouslySeen = encounterAge <= sourceVariantWindow || lastSeenAge <= sourceVariantWindow;
            bool recentCrossAreaJournalRow = sameArea ||
                !currentJournal ||
                !countedJournal ||
                encounterAge <= visibleRowSuppressWindow;
            bool continuingCrossAreaJournalRow = !sameArea &&
                currentJournal &&
                countedJournal &&
                lastSeenAge <= CrossAreaJournalContinuitySuppressWindow;
            bool crossAreaJournalBucketsMatch = currentJournal &&
                countedJournal &&
                JournalEvidenceBucketsMatch(globalEvidenceKey, countedEvidenceKey, out _, out _);
            bool sameVisibleJournalRowCanSuppress = recentCrossAreaJournalRow ||
                (continuingCrossAreaJournalRow && crossAreaJournalBucketsMatch);

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

        private static bool IsJournalKilledEvidence(string evidenceKey)
        {
            return !string.IsNullOrWhiteSpace(evidenceKey) &&
                evidenceKey.Contains("Kind=JournalKilled", StringComparison.OrdinalIgnoreCase);
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
}

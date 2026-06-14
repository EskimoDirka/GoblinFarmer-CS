namespace GoblinFarmer
{
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
}

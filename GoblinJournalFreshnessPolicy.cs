namespace GoblinFarmer
{
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
}

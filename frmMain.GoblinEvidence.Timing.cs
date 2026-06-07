using System.Diagnostics;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private static readonly TimeSpan GoblinEvidenceTimingSummaryCooldown = TimeSpan.FromSeconds(30);
        private readonly Dictionary<string, PortGoblinEvidenceTimingStats> portGoblinEvidenceTimingByStage = new(StringComparer.OrdinalIgnoreCase);
        private long portLastGoblinEvidenceTimingSummaryTicks;

        private void PortRecordGoblinEvidenceTiming(string stage, TimeSpan elapsed)
        {
            if (string.IsNullOrWhiteSpace(stage))
            {
                return;
            }

            Dictionary<string, PortGoblinEvidenceTimingStats> snapshot;
            lock (portGoblinEvidenceLock)
            {
                if (!portGoblinEvidenceTimingByStage.TryGetValue(stage, out PortGoblinEvidenceTimingStats? stats))
                {
                    stats = new PortGoblinEvidenceTimingStats();
                    portGoblinEvidenceTimingByStage[stage] = stats;
                }

                stats.Record(elapsed.TotalMilliseconds);
                long nowTicks = DateTime.UtcNow.Ticks;
                long lastTicks = Interlocked.Read(ref portLastGoblinEvidenceTimingSummaryTicks);
                if (nowTicks - lastTicks < GoblinEvidenceTimingSummaryCooldown.Ticks)
                {
                    return;
                }

                Interlocked.Exchange(ref portLastGoblinEvidenceTimingSummaryTicks, nowTicks);
                snapshot = portGoblinEvidenceTimingByStage.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.Clone(),
                    StringComparer.OrdinalIgnoreCase);
            }

            string summary = string.Join("|", snapshot
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}:count={pair.Value.Count},avgMs={pair.Value.AverageMs:0.0},lastMs={pair.Value.LastMs:0.0},maxMs={pair.Value.MaxMs:0.0}"));
            AppLogger.Info(
                "GoblinEvidenceTimingSummary: " +
                $"summary={PortLogField(summary)}; " +
                $"observationModeEnabled={PortGoblinObservationScannerEnabled()}; " +
                $"automaticCountingEnabled={PortGoblinAutomaticCountingEnabled()}; " +
                $"diabloActive={PortDiabloIsActive()}; " +
                $"currentArea={PortLogField(PortDisplayLocation(portLastConfirmedLocation))}");
            PortWriteGoblinTrackerJsonEvent(
                "GoblinEvidenceTimingSummary",
                new Dictionary<string, object?>
                {
                    ["summary"] = summary,
                    ["stageCount"] = snapshot.Count,
                    ["currentArea"] = PortDisplayLocation(portLastConfirmedLocation),
                    ["observationModeEnabled"] = PortGoblinObservationScannerEnabled(),
                    ["automaticCountingEnabled"] = PortGoblinAutomaticCountingEnabled(),
                });
        }

        private sealed class PortGoblinEvidenceTimingStats
        {
            private double totalMs;

            public int Count { get; private set; }
            public double LastMs { get; private set; }
            public double MaxMs { get; private set; }
            public double AverageMs => Count == 0 ? 0 : totalMs / Count;

            public void Record(double elapsedMs)
            {
                Count++;
                LastMs = elapsedMs;
                totalMs += elapsedMs;
                MaxMs = Math.Max(MaxMs, elapsedMs);
            }

            public PortGoblinEvidenceTimingStats Clone()
            {
                return new PortGoblinEvidenceTimingStats
                {
                    Count = Count,
                    LastMs = LastMs,
                    MaxMs = MaxMs,
                    totalMs = totalMs,
                };
            }
        }
    }
}

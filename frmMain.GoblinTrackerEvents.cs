using System.Text.Json;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private static readonly JsonSerializerOptions GoblinTrackerJsonEventOptions = new()
        {
            WriteIndented = false,
        };

        private readonly object portGoblinTrackerJsonEventLock = new();

        private void PortWriteGoblinTrackerJsonEvent(string eventName, Dictionary<string, object?> fields)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            if (!DebugManager.IsVisualStudioDebugSession &&
                !AppSettings.Debug.DebugMode &&
                !AppSettings.GoblinTracker.EnableDecisionTrace)
            {
                return;
            }

            try
            {
                string directory = DebugManager.GoblinEvidenceDirectory;
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "GoblinTrackerEvents.jsonl");
                Dictionary<string, object?> payload = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["timestampUtc"] = DateTime.UtcNow.ToString("O"),
                    ["eventName"] = eventName,
                    ["buildConfiguration"] = AppSettings.BuildConfiguration,
                    ["isVsDebug"] = AppSettings.IsVsDebugProfile,
                };

                foreach (KeyValuePair<string, object?> field in fields)
                {
                    payload[field.Key] = field.Value;
                }

                string line = JsonSerializer.Serialize(payload, GoblinTrackerJsonEventOptions);
                lock (portGoblinTrackerJsonEventLock)
                {
                    File.AppendAllText(path, line + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Goblin Tracker JSON event write failed: eventName={eventName}", ex);
            }
        }
    }
}

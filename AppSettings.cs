using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoblinFarmer
{
    internal static class AppSettings
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        private static SettingsModel settings = SettingsModel.Default();

        public static DebugSettings Debug => settings.Debug;
        public static UiSettings UI => settings.UI;
        public static RepairSettings Repair => settings.Repair;
        public static TeleportSettings Teleport => settings.Teleport;
        public static BountySettings Bounty => settings.Bounty;
        public static int RetentionDays => 1;

        public static void Load()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "AppSettings.json");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                if (!File.Exists(configPath))
                {
                    settings = SettingsModel.Default();
                    File.WriteAllText(configPath, JsonSerializer.Serialize(settings, JsonOptions));
                    AppLogger.Info($"AppSettings created with safe defaults: {configPath}");
                }
                else
                {
                    SettingsModel? loaded = JsonSerializer.Deserialize<SettingsModel>(File.ReadAllText(configPath), JsonOptions);
                    settings = loaded ?? SettingsModel.Default();
                    settings.Normalize();
                }
            }
            catch (Exception ex)
            {
                settings = SettingsModel.Default();
                AppLogger.Error($"AppSettings load failed; using safe defaults from {configPath}.", ex);
            }

            LogLoadedValues(configPath);
        }

        private static void LogLoadedValues(string configPath)
        {
            AppLogger.Info(
                "AppSettings loaded: " +
                $"path={configPath}; " +
                $"Debug.ShowDiagnosticOverlay={Debug.ShowDiagnosticOverlay}; " +
                $"Debug.ShowRouteInspector={Debug.ShowRouteInspector}; " +
                $"Debug.EnableDebugScreenshots={Debug.EnableDebugScreenshots}; " +
                $"Debug.EnableMissingAssetPrompts={Debug.EnableMissingAssetPrompts}; " +
                $"Debug.EnableVerboseLogging={Debug.EnableVerboseLogging}; " +
                $"UI.NotificationDurationMs={UI.NotificationDurationMs}; " +
                $"UI.NotificationOpacity={UI.NotificationOpacity:0.00}; " +
                $"UI.NotificationPosition={UI.NotificationPosition}; " +
                $"Repair.PostArrivalSettleDelayMs={Repair.PostArrivalSettleDelayMs}; " +
                $"Repair.RepairMenuPollingIntervalMs={Repair.RepairMenuPollingIntervalMs}; " +
                $"Teleport.TeleportConfirmationTimeoutMs={Teleport.TeleportConfirmationTimeoutMs}; " +
                $"Teleport.TeleportRetryCount={Teleport.TeleportRetryCount}; " +
                $"Bounty.PollIntervalMs={Bounty.PollIntervalMs}; " +
                $"Bounty.EscapeCooldownMs={Bounty.EscapeCooldownMs}");
        }

        internal sealed class SettingsModel
        {
            public DebugSettings Debug { get; set; } = new();
            public UiSettings UI { get; set; } = new();
            public RepairSettings Repair { get; set; } = new();
            public TeleportSettings Teleport { get; set; } = new();
            public BountySettings Bounty { get; set; } = new();

            public static SettingsModel Default()
            {
                SettingsModel model = new()
                {
                    Debug = new DebugSettings(),
                    UI = new UiSettings(),
                    Repair = new RepairSettings(),
                    Teleport = new TeleportSettings(),
                    Bounty = new BountySettings(),
                };
                model.Normalize();
                return model;
            }

            public void Normalize()
            {
                Debug ??= new DebugSettings();
                UI ??= new UiSettings();
                Repair ??= new RepairSettings();
                Teleport ??= new TeleportSettings();
                Bounty ??= new BountySettings();
                UI.Normalize();
                Repair.Normalize();
                Teleport.Normalize();
                Bounty.Normalize();
            }
        }

        internal sealed class DebugSettings
        {
            public bool ShowDiagnosticOverlay { get; set; }
            public bool ShowRouteInspector { get; set; }
            public bool EnableDebugScreenshots { get; set; } = true;
            public bool EnableMissingAssetPrompts { get; set; } = true;
            public bool EnableVerboseLogging { get; set; }
        }

        internal sealed class UiSettings
        {
            public int NotificationDurationMs { get; set; }
            public double NotificationOpacity { get; set; } = 0.90;
            public string NotificationPosition { get; set; } = "Center";

            public void Normalize()
            {
                if (NotificationDurationMs < 0)
                {
                    NotificationDurationMs = 0;
                }

                if (NotificationOpacity <= 0 || NotificationOpacity > 1)
                {
                    NotificationOpacity = 0.90;
                }

                if (string.IsNullOrWhiteSpace(NotificationPosition))
                {
                    NotificationPosition = "Center";
                }
            }
        }

        internal sealed class RepairSettings
        {
            public int PostArrivalSettleDelayMs { get; set; } = 50;
            public int RepairMenuPollingIntervalMs { get; set; } = 75;

            public void Normalize()
            {
                PostArrivalSettleDelayMs = Math.Clamp(PostArrivalSettleDelayMs, 0, 1000);
                RepairMenuPollingIntervalMs = Math.Clamp(RepairMenuPollingIntervalMs, 25, 500);
            }
        }

        internal sealed class TeleportSettings
        {
            public int TeleportConfirmationTimeoutMs { get; set; } = 18000;
            public int TeleportRetryCount { get; set; } = 0;

            public void Normalize()
            {
                TeleportConfirmationTimeoutMs = Math.Clamp(TeleportConfirmationTimeoutMs, 5000, 60000);
                TeleportRetryCount = Math.Clamp(TeleportRetryCount, 0, 10);
            }
        }

        internal sealed class BountySettings
        {
            public int PollIntervalMs { get; set; } = 100;
            public int EscapeCooldownMs { get; set; } = 1000;

            public void Normalize()
            {
                PollIntervalMs = Math.Clamp(PollIntervalMs, 50, 1000);
                EscapeCooldownMs = Math.Clamp(EscapeCooldownMs, 500, 10000);
            }
        }
    }
}

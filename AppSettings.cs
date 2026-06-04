using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using Microsoft.Win32;

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
        private static string configPath = ResolveAppSettingsConfigPath();
        private static DebugSettings persistedDebugSettings = new();
        private static bool firstRunSetupSuppressed;

        public enum DebugDefaultsProfile
        {
            VsDebug,
            ReleaseUser,
        }

        public static string ConfigPath => configPath;
        public static DebugDefaultsProfile CurrentDebugDefaultsProfile { get; private set; } = ResolveDebugDefaultsProfile();
        public static bool IsVsDebugProfile => CurrentDebugDefaultsProfile == DebugDefaultsProfile.VsDebug;
        public static bool FirstRunSetupSuppressed => firstRunSetupSuppressed;
        public static string BuildConfiguration =>
#if DEBUG
            "Debug";
#else
            "Release";
#endif
        public static bool IsDebugBuild =>
#if DEBUG
            true;
#else
            false;
#endif
        public static RuntimeSettings Runtime => settings.Runtime;
        public static LaunchSettings Launch => settings.Launch;
        public static DebugSettings Debug => settings.Debug;
        public static UiSettings UI => settings.UI;
        public static RepairSettings Repair => settings.Repair;
        public static TeleportSettings Teleport => settings.Teleport;
        public static BountySettings Bounty => settings.Bounty;
        public static ImageRecognitionSettings ImageRecognition => settings.ImageRecognition;
        public static UserSettings User => settings.User;
        public static int RetentionDays => 1;

        public static void Load()
        {
            configPath = ResolveAppSettingsConfigPath();
            CurrentDebugDefaultsProfile = ResolveDebugDefaultsProfile();
            firstRunSetupSuppressed = ShouldSuppressFirstRunSetup(CurrentDebugDefaultsProfile);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                if (!File.Exists(configPath))
                {
                    settings = SettingsModel.Default();
                    settings.Normalize();
                    ApplyReleaseDebugPersistenceDefaults();
                    persistedDebugSettings = settings.Debug.Clone();
                    File.WriteAllText(configPath, JsonSerializer.Serialize(settings, JsonOptions));
                    AppLogger.Info($"AppSettings created with safe defaults: {configPath}");
                    ApplyDebugDefaultsProfile();
                    ApplyVsDebugDevDefaults();
                }
                else
                {
                    string json = File.ReadAllText(configPath);
                    bool hasSavedDebugScreenshotsPreference = HasSavedDebugScreenshotsPreference(json);
                    bool hasSavedUserPreferences = HasSavedUserPreferences(json);
                    SettingsModel? loaded = JsonSerializer.Deserialize<SettingsModel>(json, JsonOptions);
                    settings = loaded ?? SettingsModel.Default();
                    bool shouldSaveLoadedSettings = loaded == null;
                    if (!hasSavedDebugScreenshotsPreference)
                    {
                        settings.Debug.EnableDebugScreenshots = DebugSettings.DefaultEnableDebugScreenshots;
                        shouldSaveLoadedSettings = true;
                        AppLogger.Info($"AppSettings missing Debug.EnableDebugScreenshots; using persisted default {settings.Debug.EnableDebugScreenshots}.");
                    }

                    if (!hasSavedUserPreferences)
                    {
                        shouldSaveLoadedSettings = true;
                        AppLogger.Info("AppSettings missing one or more User preferences; using defaults for missing values.");
                    }

                    settings.Normalize();
                    ApplyReleaseDebugPersistenceDefaults();
                    persistedDebugSettings = settings.Debug.Clone();
                    if (shouldSaveLoadedSettings)
                    {
                        Save();
                    }

                    ApplyDebugDefaultsProfile();
                    ApplyVsDebugDevDefaults();
                }
            }
            catch (Exception ex)
            {
                settings = SettingsModel.Default();
                settings.Normalize();
                ApplyReleaseDebugPersistenceDefaults();
                persistedDebugSettings = settings.Debug.Clone();
                ApplyDebugDefaultsProfile();
                ApplyVsDebugDevDefaults();
                AppLogger.Error($"AppSettings load failed; using safe defaults from {configPath}.", ex);
            }

            LogLoadedValues(configPath);
        }

        private static string ResolveAppSettingsConfigPath()
        {
            return ResolveAppSettingsConfigPath(
                Environment.GetEnvironmentVariable("GOBLINFARMER_APPSETTINGS_PATH"),
                AppDomain.CurrentDomain.BaseDirectory);
        }

        internal static string ResolveAppSettingsConfigPath(string? explicitPath, string baseDirectory)
        {
            if (!string.IsNullOrWhiteSpace(explicitPath))
            {
                return Path.GetFullPath(Environment.ExpandEnvironmentVariables(explicitPath.Trim()));
            }

            return Path.Combine(baseDirectory, "Config", "AppSettings.json");
        }

        private static string? TryResolveProjectAppSettingsPath()
        {
            DirectoryInfo? directory = new(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                string projectFilePath = Path.Combine(directory.FullName, "GoblinFarmer.csproj");
                string projectConfigPath = Path.Combine(directory.FullName, "Config", "AppSettings.json");
                if (File.Exists(projectFilePath) && File.Exists(projectConfigPath))
                {
                    return projectConfigPath;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static string? TryResolveProjectRoot()
        {
            DirectoryInfo? directory = new(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                string projectFilePath = Path.Combine(directory.FullName, "GoblinFarmer.csproj");
                if (File.Exists(projectFilePath))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return null;
        }

        public static void Save()
        {
            try
            {
                settings.Normalize();
                ApplyReleaseDebugPersistenceDefaults();
                SettingsModel modelToSave = settings;
                if (IsVsDebugProfile)
                {
                    modelToSave = settings.WithDebugSettings(persistedDebugSettings);
                }
                else
                {
                    persistedDebugSettings = settings.Debug.Clone();
                }

                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                File.WriteAllText(configPath, JsonSerializer.Serialize(modelToSave, JsonOptions));
                AppLogger.Info($"AppSettings saved: {configPath}");
                ApplyDebugDefaultsProfile();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"AppSettings save failed: {configPath}", ex);
            }
        }

        private static bool HasSavedDebugScreenshotsPreference(string json)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });

                return document.RootElement.TryGetProperty("Debug", out JsonElement debugElement) &&
                    debugElement.ValueKind == JsonValueKind.Object &&
                    debugElement.TryGetProperty("EnableDebugScreenshots", out _);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to inspect AppSettings Debug.EnableDebugScreenshots preference.", ex);
                return false;
            }
        }

        private static bool HasSavedUserPreferences(string json)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });

                return document.RootElement.TryGetProperty("User", out JsonElement userElement) &&
                    userElement.ValueKind == JsonValueKind.Object &&
                    userElement.TryGetProperty("CombatProfile", out _) &&
                    userElement.TryGetProperty("CombatHotkeyEnabled", out _) &&
                    userElement.TryGetProperty("TeleportNextHotkeyEnabled", out _) &&
                    userElement.TryGetProperty("ExitGameHotkeyEnabled", out _) &&
                    userElement.TryGetProperty("KadalaHotkeyEnabled", out _) &&
                    userElement.TryGetProperty("LootHotkeyEnabled", out _);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to inspect AppSettings User preferences.", ex);
                return false;
            }
        }

        private static DebugDefaultsProfile ResolveDebugDefaultsProfile()
        {
            return ResolveDebugDefaultsProfile(
                Environment.GetEnvironmentVariable("GOBLINFARMER_DEBUG_DEFAULTS_PROFILE"),
                Debugger.IsAttached,
                IsDebugBuild);
        }

        internal static DebugDefaultsProfile ResolveDebugDefaultsProfile(string? explicitProfile, bool debuggerAttached, bool debugBuild)
        {
            if (Enum.TryParse(explicitProfile, ignoreCase: true, out DebugDefaultsProfile parsedProfile))
            {
                return parsedProfile;
            }

            return IsVsDebugLaunchSurface(debuggerAttached, debugBuild)
                ? DebugDefaultsProfile.VsDebug
                : DebugDefaultsProfile.ReleaseUser;
        }

        internal static bool IsVsDebugLaunchSurface(bool debuggerAttached, bool debugBuild)
        {
            return debuggerAttached && debugBuild;
        }

        internal static bool ShouldSuppressFirstRunSetup(DebugDefaultsProfile profile)
        {
            return profile == DebugDefaultsProfile.VsDebug;
        }

        internal static bool ShouldShowDynamicDebugControls(DebugDefaultsProfile profile)
        {
            return profile != DebugDefaultsProfile.VsDebug;
        }

        internal static bool ShouldRequireFirstRunSetup(DebugDefaultsProfile profile, bool requiredRuntimeConfigurationIsValid)
        {
            return !ShouldSuppressFirstRunSetup(profile) && !requiredRuntimeConfigurationIsValid;
        }

        public static void ApplyDebugDefaultsProfile()
        {
            CurrentDebugDefaultsProfile = ResolveDebugDefaultsProfile();
            firstRunSetupSuppressed = ShouldSuppressFirstRunSetup(CurrentDebugDefaultsProfile);
            if (CurrentDebugDefaultsProfile != DebugDefaultsProfile.VsDebug)
            {
                return;
            }

            settings.Debug.DebugMode = true;
            settings.Debug.ShowDiagnosticOverlay = true;
            settings.Debug.ShowRouteInspector = true;
            settings.Debug.EnableDebugScreenshots = true;
            settings.Debug.EnableMissingAssetPrompts = true;
            settings.Debug.EnableVerboseLogging = true;
        }

        public static void ApplyVsDebugDevDefaults()
        {
            CurrentDebugDefaultsProfile = ResolveDebugDefaultsProfile();
            firstRunSetupSuppressed = ShouldSuppressFirstRunSetup(CurrentDebugDefaultsProfile);
            if (!firstRunSetupSuppressed)
            {
                return;
            }

            ApplyDebugDefaultsProfile();

            string? projectRoot = TryResolveProjectRoot();
            if (!string.IsNullOrWhiteSpace(projectRoot))
            {
                ApplyProjectRuntimeDefaults(projectRoot);

                string projectImages = Path.Combine(projectRoot, "Images");
                if (Directory.Exists(projectImages))
                {
                    settings.Runtime.ImagesRoot = projectImages;
                }
            }

            if (!ExecutableExists(settings.Runtime.DiabloExecutablePath))
            {
                string discoveredDiablo = DiscoverDiabloExecutable();
                if (!string.IsNullOrWhiteSpace(discoveredDiablo))
                {
                    settings.Runtime.DiabloExecutablePath = discoveredDiablo;
                }
            }

            if (!ExecutableExists(settings.Runtime.BattleNetExecutablePath))
            {
                string discoveredBattleNet = DiscoverBattleNetExecutable();
                if (!string.IsNullOrWhiteSpace(discoveredBattleNet))
                {
                    settings.Runtime.BattleNetExecutablePath = discoveredBattleNet;
                }
            }

            settings.Normalize();
        }

        private static void ApplyProjectRuntimeDefaults(string projectRoot)
        {
            string projectConfigPath = Path.Combine(projectRoot, "Config", "AppSettings.json");
            if (!File.Exists(projectConfigPath))
            {
                return;
            }

            try
            {
                SettingsModel? projectSettings = JsonSerializer.Deserialize<SettingsModel>(File.ReadAllText(projectConfigPath), JsonOptions);
                RuntimeSettings? projectRuntime = projectSettings?.Runtime;
                if (projectRuntime == null)
                {
                    return;
                }

                if (!ExecutableExists(settings.Runtime.DiabloExecutablePath) && ExecutableExists(projectRuntime.DiabloExecutablePath))
                {
                    settings.Runtime.DiabloExecutablePath = projectRuntime.DiabloExecutablePath;
                }

                if (!ExecutableExists(settings.Runtime.BattleNetExecutablePath) && ExecutableExists(projectRuntime.BattleNetExecutablePath))
                {
                    settings.Runtime.BattleNetExecutablePath = projectRuntime.BattleNetExecutablePath;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to inspect VS/dev runtime defaults from {projectConfigPath}.", ex);
            }
        }

        private static void ApplyReleaseDebugPersistenceDefaults()
        {
            CurrentDebugDefaultsProfile = ResolveDebugDefaultsProfile();
            firstRunSetupSuppressed = ShouldSuppressFirstRunSetup(CurrentDebugDefaultsProfile);
            if (CurrentDebugDefaultsProfile != DebugDefaultsProfile.ReleaseUser)
            {
                return;
            }

            if (settings.Debug.DebugModePreferenceSaved)
            {
                return;
            }

            settings.Debug.DebugMode = false;
            settings.Debug.ShowDiagnosticOverlay = false;
            settings.Debug.ShowRouteInspector = false;
            settings.Debug.EnableDebugScreenshots = false;
            settings.Debug.EnableMissingAssetPrompts = false;
            settings.Debug.EnableVerboseLogging = false;
        }

        internal static LaunchProfileSnapshot ResolveLaunchProfileForTests(string? explicitProfile, bool debuggerAttached, bool debugBuild, string? explicitConfigPath, string baseDirectory)
        {
            DebugDefaultsProfile profile = ResolveDebugDefaultsProfile(explicitProfile, debuggerAttached, debugBuild);
            bool setupSuppressed = ShouldSuppressFirstRunSetup(profile);
            DebugSettings debug = new();
            if (profile == DebugDefaultsProfile.VsDebug)
            {
                debug.DebugMode = true;
                debug.ShowDiagnosticOverlay = true;
                debug.ShowRouteInspector = true;
                debug.EnableDebugScreenshots = true;
                debug.EnableMissingAssetPrompts = true;
                debug.EnableVerboseLogging = true;
            }

            return new LaunchProfileSnapshot(
                profile,
                setupSuppressed,
                debug.DebugMode,
                debug.EnableDebugScreenshots,
                ShouldShowDynamicDebugControls(profile),
                ResolveAppSettingsConfigPath(explicitConfigPath, baseDirectory));
        }

        public static string ResolveRuntimePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "";
            }

            string expanded = Environment.ExpandEnvironmentVariables(path.Trim());
            if (Path.IsPathRooted(expanded))
            {
                return Path.GetFullPath(expanded);
            }

            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, expanded));
        }

        public static string ImagesRootPath => ResolveRuntimePath(Runtime.ImagesRoot);
        public static string ScanRegionCachePath => ResolveRuntimePath(Runtime.ScanRegionCachePath);

        public static string ResolveBattleNetExecutablePathForLaunch(out string source, out string configuredPath, out string detectedPath)
        {
            configuredPath = string.IsNullOrWhiteSpace(Runtime.BattleNetExecutablePath)
                ? ""
                : ResolveRuntimePath(Runtime.BattleNetExecutablePath);
            detectedPath = "";

            if (ExecutableExists(Runtime.BattleNetExecutablePath))
            {
                source = "configured";
                return configuredPath;
            }

            detectedPath = DiscoverBattleNetExecutable();
            if (!string.IsNullOrWhiteSpace(detectedPath) && ExecutableExists(detectedPath))
            {
                source = "detected";
                return detectedPath;
            }

            source = "missing";
            return configuredPath;
        }

        public static bool DiscoverMissingRuntimePaths()
        {
            bool changed = false;

            if (!ExecutableExists(Runtime.DiabloExecutablePath))
            {
                string discoveredDiablo = DiscoverDiabloExecutable();
                if (!string.IsNullOrWhiteSpace(discoveredDiablo))
                {
                    Runtime.DiabloExecutablePath = discoveredDiablo;
                    AppLogger.Info($"Discovered Diablo III executable: {discoveredDiablo}");
                    changed = true;
                }
                else
                {
                    AppLogger.Info("Diablo III executable was not discovered automatically.");
                }
            }

            if (!ExecutableExists(Runtime.BattleNetExecutablePath))
            {
                string discoveredBattleNet = DiscoverBattleNetExecutable();
                if (!string.IsNullOrWhiteSpace(discoveredBattleNet))
                {
                    Runtime.BattleNetExecutablePath = discoveredBattleNet;
                    AppLogger.Info($"Discovered Battle.net executable: {discoveredBattleNet}");
                    changed = true;
                }
                else
                {
                    AppLogger.Info("Battle.net executable was not discovered automatically.");
                }
            }

            if (changed)
            {
                Save();
            }

            return changed;
        }

        public static bool RequiredRuntimeConfigurationIsValid(out string message)
        {
            List<string> errors = [];

            if (!ExecutableExists(Runtime.DiabloExecutablePath))
            {
                errors.Add("Diablo III executable is missing or invalid.");
            }

            if (!ExecutableExists(Runtime.BattleNetExecutablePath))
            {
                errors.Add("Battle.net executable is missing or invalid.");
            }

            if (!Directory.Exists(ImagesRootPath))
            {
                errors.Add($"Images folder is missing: {ImagesRootPath}");
            }

            string[] requiredImageFolders = ["Combat", "Current Location", "Leave Game", "Repair", "Salvage", "Start Game", "Teleport Function"];
            foreach (string folder in requiredImageFolders)
            {
                string path = Path.Combine(ImagesRootPath, folder);
                if (!Directory.Exists(path))
                {
                    errors.Add($"Required template folder is missing: {path}");
                }
            }

            message = string.Join(Environment.NewLine, errors);
            return errors.Count == 0;
        }

        public static bool ExecutableExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                string resolved = ResolveRuntimePath(path);
                return File.Exists(resolved) && resolved.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string DiscoverDiabloExecutable()
        {
            string[] candidates =
            [
                RegistryString(@"HKEY_LOCAL_MACHINE\SOFTWARE\Blizzard Entertainment\Diablo III", "InstallPath"),
                RegistryString(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Blizzard Entertainment\Diablo III", "InstallPath"),
                RegistryString(@"HKEY_CURRENT_USER\SOFTWARE\Blizzard Entertainment\Diablo III", "InstallPath"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Diablo III"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Diablo III"),
                @"C:\Program Files\Diablo III",
                @"C:\Program Files (x86)\Diablo III",
            ];

            return FindFirstExecutableCandidate(candidates, ["Diablo III64.exe", "Diablo III.exe"]);
        }

        private static string DiscoverBattleNetExecutable()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string programFiles64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] candidates =
            [
                RegistryString(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Battle.net", "InstallLocation"),
                RegistryString(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Battle.net", "InstallLocation"),
                RegistryString(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Battle.net", "InstallLocation"),
                Path.Combine(programFiles, "Battle.net"),
                Path.Combine(programFiles64, "Battle.net"),
                Path.Combine(localAppData, "Programs", "Battle.net"),
                @"C:\Program Files (x86)\Battle.net",
                @"C:\Program Files\Battle.net",
            ];

            return FindFirstExecutableCandidate(candidates, ["Battle.net.exe"]);
        }

        private static string RegistryString(string keyName, string valueName)
        {
            try
            {
                return Registry.GetValue(keyName, valueName, "") as string ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static string FindFirstExecutableCandidate(IEnumerable<string> rootsOrFiles, string[] executableNames)
        {
            foreach (string rootOrFile in rootsOrFiles.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                string expanded = Environment.ExpandEnvironmentVariables(rootOrFile);
                if (File.Exists(expanded) && expanded.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFullPath(expanded);
                }

                foreach (string executableName in executableNames)
                {
                    string direct = Path.Combine(expanded, executableName);
                    if (File.Exists(direct))
                    {
                        return Path.GetFullPath(direct);
                    }
                }
            }

            return "";
        }

        private static void LogLoadedValues(string configPath)
        {
            AppLogger.Info(
                "AppSettings loaded: " +
                $"AppSettingsPath={configPath}; " +
                $"path={configPath}; " +
                $"ExecutablePath={Environment.ProcessPath ?? AppDomain.CurrentDomain.BaseDirectory}; " +
                $"LaunchKind={(Debugger.IsAttached ? "VS/debugger" : "installed exe/standalone")}; " +
                $"DebuggerAttached={Debugger.IsAttached}; " +
                $"BuildConfiguration={BuildConfiguration}; " +
                $"VsDevProfileActive={IsVsDebugProfile}; " +
                $"FirstRunSetupSuppressed={FirstRunSetupSuppressed}; " +
                $"DebugDefaultsProfile={CurrentDebugDefaultsProfile}; " +
                $"Runtime.DiabloExecutablePath={Runtime.DiabloExecutablePath}; " +
                $"Runtime.BattleNetExecutablePath={Runtime.BattleNetExecutablePath}; " +
                $"Runtime.ImagesRoot={Runtime.ImagesRoot}; " +
                $"Runtime.ScanRegionCachePath={Runtime.ScanRegionCachePath}; " +
                $"Launch.BattleNetWindowFocusTimeoutMs={Launch.BattleNetWindowFocusTimeoutMs}; " +
                $"Launch.BattleNetPlayButtonTimeoutMs={Launch.BattleNetPlayButtonTimeoutMs}; " +
                $"Launch.DiabloStartTimeoutMs={Launch.DiabloStartTimeoutMs}; " +
                $"Debug.DebugMode={Debug.DebugMode}; " +
                $"DebugMode={Debug.DebugMode}; " +
                $"KeepDebugScreenshots={Debug.EnableDebugScreenshots}; " +
                $"Debug.ShowDiagnosticOverlay={Debug.ShowDiagnosticOverlay}; " +
                $"ShowDiagnosticOverlay={Debug.ShowDiagnosticOverlay}; " +
                $"Debug.ShowRouteInspector={Debug.ShowRouteInspector}; " +
                $"ShowRouteInspector={Debug.ShowRouteInspector}; " +
                $"Debug.EnableDebugScreenshots={Debug.EnableDebugScreenshots}; " +
                $"EnableDebugScreenshots={Debug.EnableDebugScreenshots}; " +
                $"Debug.EnableMissingAssetPrompts={Debug.EnableMissingAssetPrompts}; " +
                $"Debug.EnableVerboseLogging={Debug.EnableVerboseLogging}; " +
                $"VerboseLogging={Debug.EnableVerboseLogging}; " +
                $"UI.NotificationDurationMs={UI.NotificationDurationMs}; " +
                $"UI.NotificationOpacity={UI.NotificationOpacity:0.00}; " +
                $"UI.NotificationPosition={UI.NotificationPosition}; " +
                $"Repair.PostArrivalSettleDelayMs={Repair.PostArrivalSettleDelayMs}; " +
                $"Repair.RepairMenuPollingIntervalMs={Repair.RepairMenuPollingIntervalMs}; " +
                $"Teleport.TeleportConfirmationTimeoutMs={Teleport.TeleportConfirmationTimeoutMs}; " +
                $"Teleport.TeleportRetryCount={Teleport.TeleportRetryCount}; " +
                $"Bounty.PollIntervalMs={Bounty.PollIntervalMs}; " +
                $"Bounty.EscapeCooldownMs={Bounty.EscapeCooldownMs}; " +
                $"ImageRecognition.StartGameButtonConfidence={ImageRecognition.StartGameButtonConfidence:0.000}; " +
                $"ImageRecognition.BattleNetPlayButtonConfidence={ImageRecognition.BattleNetPlayButtonConfidence:0.000}; " +
                $"User.CombatProfile={User.CombatProfile}; " +
                $"SelectedCombatProfile={User.CombatProfile}; " +
                $"SelectedCombatClass={User.CombatProfile}; " +
                $"User.CombatHotkeyEnabled={User.CombatHotkeyEnabled}; " +
                $"User.TeleportNextHotkeyEnabled={User.TeleportNextHotkeyEnabled}; " +
                $"User.ExitGameHotkeyEnabled={User.ExitGameHotkeyEnabled}; " +
                $"User.KadalaHotkeyEnabled={User.KadalaHotkeyEnabled}; " +
                $"User.LootHotkeyEnabled={User.LootHotkeyEnabled}");
        }

        internal readonly record struct LaunchProfileSnapshot(
            DebugDefaultsProfile Profile,
            bool FirstRunSetupSuppressed,
            bool DebugMode,
            bool KeepDebugScreenshots,
            bool DynamicDebugControlsVisible,
            string ConfigPath);

        internal sealed class SettingsModel
        {
            public RuntimeSettings Runtime { get; set; } = new();
            public LaunchSettings Launch { get; set; } = new();
            public DebugSettings Debug { get; set; } = new();
            public UiSettings UI { get; set; } = new();
            public RepairSettings Repair { get; set; } = new();
            public TeleportSettings Teleport { get; set; } = new();
            public BountySettings Bounty { get; set; } = new();
            public ImageRecognitionSettings ImageRecognition { get; set; } = new();
            public UserSettings User { get; set; } = new();

            public static SettingsModel Default()
            {
                SettingsModel model = new()
                {
                    Runtime = new RuntimeSettings(),
                    Launch = new LaunchSettings(),
                    Debug = new DebugSettings(),
                    UI = new UiSettings(),
                    Repair = new RepairSettings(),
                    Teleport = new TeleportSettings(),
                    Bounty = new BountySettings(),
                    ImageRecognition = new ImageRecognitionSettings(),
                    User = new UserSettings(),
                };
                model.Normalize();
                return model;
            }

            public SettingsModel WithDebugSettings(DebugSettings debugSettings)
            {
                return new SettingsModel
                {
                    Runtime = Runtime,
                    Launch = Launch,
                    Debug = debugSettings.Clone(),
                    UI = UI,
                    Repair = Repair,
                    Teleport = Teleport,
                    Bounty = Bounty,
                    ImageRecognition = ImageRecognition,
                    User = User,
                };
            }

            public void Normalize()
            {
                Runtime ??= new RuntimeSettings();
                Launch ??= new LaunchSettings();
                Debug ??= new DebugSettings();
                UI ??= new UiSettings();
                Repair ??= new RepairSettings();
                Teleport ??= new TeleportSettings();
                Bounty ??= new BountySettings();
                ImageRecognition ??= new ImageRecognitionSettings();
                User ??= new UserSettings();
                Runtime.Normalize();
                Launch.Normalize();
                UI.Normalize();
                Repair.Normalize();
                Teleport.Normalize();
                Bounty.Normalize();
                ImageRecognition.Normalize();
                User.Normalize();
            }
        }

        internal sealed class UserSettings
        {
            private static readonly string[] ValidCombatProfiles = ["monk", "demon_hunter", "witch_doctor"];

            public string CombatProfile { get; set; } = "monk";
            public bool CombatHotkeyEnabled { get; set; } = true;
            public bool TeleportNextHotkeyEnabled { get; set; } = true;
            public bool ExitGameHotkeyEnabled { get; set; } = true;
            public bool KadalaHotkeyEnabled { get; set; } = true;
            public bool LootHotkeyEnabled { get; set; } = true;

            public void Normalize()
            {
                CombatProfile = string.IsNullOrWhiteSpace(CombatProfile)
                    ? "monk"
                    : CombatProfile.Trim().ToLowerInvariant();

                if (!ValidCombatProfiles.Contains(CombatProfile, StringComparer.OrdinalIgnoreCase))
                {
                    CombatProfile = "monk";
                }
            }
        }

        internal sealed class RuntimeSettings
        {
            public string DiabloExecutablePath { get; set; } = "";
            public string BattleNetExecutablePath { get; set; } = "";
            public string ImagesRoot { get; set; } = "Images";
            public string ScanRegionCachePath { get; set; } = "ScanRegions.json";

            public void Normalize()
            {
                DiabloExecutablePath = DiabloExecutablePath?.Trim() ?? "";
                BattleNetExecutablePath = BattleNetExecutablePath?.Trim() ?? "";
                ImagesRoot = string.IsNullOrWhiteSpace(ImagesRoot) ? "Images" : ImagesRoot.Trim();
                ScanRegionCachePath = string.IsNullOrWhiteSpace(ScanRegionCachePath) ? "ScanRegions.json" : ScanRegionCachePath.Trim();
            }
        }

        internal sealed class LaunchSettings
        {
            public int BattleNetWindowFocusTimeoutMs { get; set; } = 30000;
            public int BattleNetPlayPrecheckMs { get; set; } = 3000;
            public int BattleNetPostTabSettleMs { get; set; } = 1200;
            public int BattleNetPlayButtonTimeoutMs { get; set; } = 60000;
            public int BattleNetPlayClickAcceptedTimeoutMs { get; set; } = 10000;
            public int BattleNetPostPlayAcceptedDelayMs { get; set; } = 2000;
            public int DiabloStartTimeoutMs { get; set; } = 120000;
            public int DiabloStartPollIntervalMs { get; set; } = 1000;
            public int LaunchGracePeriodMs { get; set; } = 45000;
            public int BattleNetCloseTimeoutMs { get; set; } = 5000;
            public int BattleNetClosePollIntervalMs { get; set; } = 250;

            public void Normalize()
            {
                BattleNetWindowFocusTimeoutMs = Math.Clamp(BattleNetWindowFocusTimeoutMs, 1000, 120000);
                BattleNetPlayPrecheckMs = Math.Clamp(BattleNetPlayPrecheckMs, 0, 30000);
                BattleNetPostTabSettleMs = Math.Clamp(BattleNetPostTabSettleMs, 0, 10000);
                BattleNetPlayButtonTimeoutMs = Math.Clamp(BattleNetPlayButtonTimeoutMs, 5000, 180000);
                BattleNetPlayClickAcceptedTimeoutMs = Math.Clamp(BattleNetPlayClickAcceptedTimeoutMs, 1000, 60000);
                BattleNetPostPlayAcceptedDelayMs = Math.Clamp(BattleNetPostPlayAcceptedDelayMs, 0, 10000);
                DiabloStartTimeoutMs = Math.Clamp(DiabloStartTimeoutMs, 10000, 240000);
                DiabloStartPollIntervalMs = Math.Clamp(DiabloStartPollIntervalMs, 250, 5000);
                LaunchGracePeriodMs = Math.Clamp(LaunchGracePeriodMs, 5000, 120000);
                BattleNetCloseTimeoutMs = Math.Clamp(BattleNetCloseTimeoutMs, 1000, 30000);
                BattleNetClosePollIntervalMs = Math.Clamp(BattleNetClosePollIntervalMs, 100, 2000);
            }
        }

        internal sealed class DebugSettings
        {
            public bool DebugModePreferenceSaved { get; set; }
            public bool DebugMode { get; set; }
            public bool ShowDiagnosticOverlay { get; set; }
            public bool ShowRouteInspector { get; set; }
            public bool EnableDebugScreenshots { get; set; } = DefaultEnableDebugScreenshots;
            public bool EnableMissingAssetPrompts { get; set; }
            public bool EnableVerboseLogging { get; set; }

            public DebugSettings Clone()
            {
                return new DebugSettings
                {
                    DebugModePreferenceSaved = DebugModePreferenceSaved,
                    DebugMode = DebugMode,
                    ShowDiagnosticOverlay = ShowDiagnosticOverlay,
                    ShowRouteInspector = ShowRouteInspector,
                    EnableDebugScreenshots = EnableDebugScreenshots,
                    EnableMissingAssetPrompts = EnableMissingAssetPrompts,
                    EnableVerboseLogging = EnableVerboseLogging,
                };
            }

            public const bool DefaultEnableDebugScreenshots = false;
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

        internal sealed class ImageRecognitionSettings
        {
            public double StartGameButtonConfidence { get; set; } = 0.85;
            public double BattleNetPlayButtonConfidence { get; set; } = 0.85;
            public double BattleNetDiabloTabConfidence { get; set; } = 0.80;
            public double CharacterLoadConfidence { get; set; } = 0.82;
            public double GameMenuConfidence { get; set; } = 0.80;
            public double VendorUiConfidence { get; set; } = 0.80;
            public double BlankInventoryTileConfidence { get; set; } = 0.78;
            public double BountyMenuConfidence { get; set; } = 0.74;
            public double CurrentLocationConfidence { get; set; } = 0.82;
            public double BlockedLocationConfidence { get; set; } = 0.68;
            public double MapActHeaderConfidence { get; set; } = 0.92;
            public double WorldMapConfidence { get; set; } = 0.80;

            public void Normalize()
            {
                StartGameButtonConfidence = ClampConfidence(StartGameButtonConfidence, 0.85);
                BattleNetPlayButtonConfidence = ClampConfidence(BattleNetPlayButtonConfidence, 0.85);
                BattleNetDiabloTabConfidence = ClampConfidence(BattleNetDiabloTabConfidence, 0.80);
                CharacterLoadConfidence = ClampConfidence(CharacterLoadConfidence, 0.82);
                GameMenuConfidence = ClampConfidence(GameMenuConfidence, 0.80);
                VendorUiConfidence = ClampConfidence(VendorUiConfidence, 0.80);
                BlankInventoryTileConfidence = ClampConfidence(BlankInventoryTileConfidence, 0.78);
                BountyMenuConfidence = ClampConfidence(BountyMenuConfidence, 0.74);
                CurrentLocationConfidence = ClampConfidence(CurrentLocationConfidence, 0.82);
                BlockedLocationConfidence = ClampConfidence(BlockedLocationConfidence, 0.68);
                MapActHeaderConfidence = ClampConfidence(MapActHeaderConfidence, 0.92);
                WorldMapConfidence = ClampConfidence(WorldMapConfidence, 0.80);
            }

            private static double ClampConfidence(double value, double defaultValue)
            {
                if (double.IsNaN(value) || value < 0.10 || value > 1.0)
                {
                    AppLogger.Info($"Invalid image confidence value {value}; using default {defaultValue:0.000}");
                    return defaultValue;
                }

                return value;
            }
        }
    }
}

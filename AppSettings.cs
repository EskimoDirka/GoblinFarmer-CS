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
        private static bool vsDebugProjectRootConfigUsed;
        private static string appSettingsPathResolution = "startup";
        private static RuntimePathPreservationResult lastRuntimePathPreservation;
        private static RuntimeDiscoveryResult lastRuntimeDiscoveryResult;
        private static string rawDiabloExecutablePathLoaded = "";
        private static bool rawDiabloExecutablePathWasBlank = true;

        public enum DebugDefaultsProfile
        {
            VsDebug,
            ReleaseUser,
        }

        public static string ConfigPath => configPath;
        public static bool VsDebugProjectRootConfigUsed => vsDebugProjectRootConfigUsed;
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
        public static GoblinTrackerSettings GoblinTracker => settings.GoblinTracker;
        public static UserSettings User => settings.User;
        public static TimeSpan DebugArtifactRetentionAge => TimeSpan.FromDays(7);
        public static int RetentionDays => 7;

        public static void Load()
        {
            CurrentDebugDefaultsProfile = ResolveDebugDefaultsProfile();
            ConfigPathResolution pathResolution = ResolveAppSettingsConfigPathWithMetadata(
                Environment.GetEnvironmentVariable("GOBLINFARMER_APPSETTINGS_PATH"),
                AppDomain.CurrentDomain.BaseDirectory,
                CurrentDebugDefaultsProfile);
            configPath = pathResolution.Path;
            vsDebugProjectRootConfigUsed = pathResolution.UsedVsDebugProjectRootConfig;
            appSettingsPathResolution = pathResolution.Reason;
            firstRunSetupSuppressed = ShouldSuppressFirstRunSetup(CurrentDebugDefaultsProfile);
            lastRuntimePathPreservation = default;
            lastRuntimeDiscoveryResult = default;
            rawDiabloExecutablePathLoaded = "";
            rawDiabloExecutablePathWasBlank = true;
            AppLogger.Info(
                "AppSettings path resolved: " +
                $"path={configPath}; " +
                $"resolution={appSettingsPathResolution}; " +
                $"vsDebugProjectRootConfigUsed={vsDebugProjectRootConfigUsed}; " +
                $"projectRootConfigPath={pathResolution.ProjectRootConfigPath}; " +
                $"baseDirectory={AppDomain.CurrentDomain.BaseDirectory}");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                if (!File.Exists(configPath))
                {
                    settings = SettingsModel.Default();
                    settings.Normalize();
                    RecordRawLoadedDiabloPath(settings.Runtime.DiabloExecutablePath);
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
                    bool hasSavedSuccessScreenshotsPreference = HasSavedSuccessScreenshotsPreference(json);
                    bool hasSavedUserPreferences = HasSavedUserPreferences(json);
                    bool hasSavedGoblinTrackerPreferences = HasSavedGoblinTrackerPreferences(json);
                    SettingsModel? loaded = JsonSerializer.Deserialize<SettingsModel>(json, JsonOptions);
                    settings = loaded ?? SettingsModel.Default();
                    settings.Normalize();
                    RecordRawLoadedDiabloPath(settings.Runtime.DiabloExecutablePath);
                    bool shouldSaveLoadedSettings = loaded == null;
                    if (!hasSavedDebugScreenshotsPreference)
                    {
                        settings.Debug.EnableDebugScreenshots = DebugSettings.DefaultEnableDebugScreenshots;
                        shouldSaveLoadedSettings = true;
                        AppLogger.Info($"AppSettings missing Debug.EnableDebugScreenshots; using persisted default {settings.Debug.EnableDebugScreenshots}.");
                    }

                    if (!hasSavedSuccessScreenshotsPreference)
                    {
                        settings.Debug.EnableSuccessScreenshots = DebugSettings.DefaultEnableSuccessScreenshots;
                        shouldSaveLoadedSettings = true;
                        AppLogger.Info($"AppSettings missing Debug.EnableSuccessScreenshots; using persisted default {settings.Debug.EnableSuccessScreenshots}.");
                    }

                    if (!hasSavedUserPreferences)
                    {
                        shouldSaveLoadedSettings = true;
                        AppLogger.Info("AppSettings missing one or more User preferences; using defaults for missing values.");
                    }

                    if (!hasSavedGoblinTrackerPreferences)
                    {
                        shouldSaveLoadedSettings = true;
                        AppLogger.Info("AppSettings missing one or more GoblinTracker preferences; using defaults for missing values.");
                    }

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
                RecordRawLoadedDiabloPath(settings.Runtime.DiabloExecutablePath);
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
            DebugDefaultsProfile profile = ResolveDebugDefaultsProfile();
            return ResolveAppSettingsConfigPath(explicitPath, baseDirectory, profile);
        }

        internal static string ResolveAppSettingsConfigPath(string? explicitPath, string baseDirectory, DebugDefaultsProfile profile)
        {
            return ResolveAppSettingsConfigPathWithMetadata(explicitPath, baseDirectory, profile).Path;
        }

        private static ConfigPathResolution ResolveAppSettingsConfigPathWithMetadata(string? explicitPath, string baseDirectory, DebugDefaultsProfile profile)
        {
            if (!string.IsNullOrWhiteSpace(explicitPath))
            {
                return new ConfigPathResolution(
                    Path.GetFullPath(Environment.ExpandEnvironmentVariables(explicitPath.Trim())),
                    false,
                    "",
                    "explicit override");
            }

            string projectConfigPath = TryResolveProjectAppSettingsPath(baseDirectory) ?? "";
            if (profile == DebugDefaultsProfile.VsDebug && !string.IsNullOrWhiteSpace(projectConfigPath))
            {
                return new ConfigPathResolution(
                    projectConfigPath,
                    true,
                    projectConfigPath,
                    "VS Debug project-root config");
            }

            return new ConfigPathResolution(
                Path.Combine(baseDirectory, "Config", "AppSettings.json"),
                false,
                projectConfigPath,
                profile == DebugDefaultsProfile.VsDebug
                    ? "VS Debug app-local config fallback"
                    : "app-local config");
        }

        private static string? TryResolveProjectAppSettingsPath()
        {
            return TryResolveProjectAppSettingsPath(AppDomain.CurrentDomain.BaseDirectory);
        }

        private static string? TryResolveProjectAppSettingsPath(string baseDirectory)
        {
            DirectoryInfo? directory = new(baseDirectory);
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
            return TryResolveProjectRoot(AppDomain.CurrentDomain.BaseDirectory);
        }

        private static string? TryResolveProjectRoot(string baseDirectory)
        {
            DirectoryInfo? directory = new(baseDirectory);
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
                lastRuntimePathPreservation = PreserveRuntimePathValues(settings, configPath);
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
                AppLogger.Info(
                    "AppSettings saved: " +
                    $"path={configPath}; " +
                    $"preservedDiabloPath={lastRuntimePathPreservation.DiabloPathPreserved}; " +
                    $"preservedBattleNetPath={lastRuntimePathPreservation.BattleNetPathPreserved}; " +
                    $"preservedImagesRoot={lastRuntimePathPreservation.ImagesRootPreserved}");
                ApplyDebugDefaultsProfile();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"AppSettings save failed: {configPath}", ex);
            }
        }

        internal static RuntimePathPreservationResult PreserveRuntimePathValuesForTests(SettingsModel target, string existingConfigPath)
        {
            target.Normalize();
            return PreserveRuntimePathValues(target, existingConfigPath);
        }

        private static RuntimePathPreservationResult PreserveRuntimePathValues(SettingsModel target, string existingConfigPath)
        {
            if (!File.Exists(existingConfigPath))
            {
                return default;
            }

            try
            {
                SettingsModel? existing = JsonSerializer.Deserialize<SettingsModel>(File.ReadAllText(existingConfigPath), JsonOptions);
                RuntimeSettings? existingRuntime = existing?.Runtime;
                if (existingRuntime == null)
                {
                    return default;
                }

                bool diabloPreserved = false;
                bool battleNetPreserved = false;
                bool imagesPreserved = false;

                if (ShouldPreserveExistingExecutablePath(target.Runtime.DiabloExecutablePath, existingRuntime.DiabloExecutablePath))
                {
                    target.Runtime.DiabloExecutablePath = existingRuntime.DiabloExecutablePath.Trim();
                    diabloPreserved = true;
                }

                if (ShouldPreserveExistingExecutablePath(target.Runtime.BattleNetExecutablePath, existingRuntime.BattleNetExecutablePath))
                {
                    target.Runtime.BattleNetExecutablePath = existingRuntime.BattleNetExecutablePath.Trim();
                    battleNetPreserved = true;
                }

                if (ShouldPreserveExistingImagesRoot(target.Runtime.ImagesRoot, existingRuntime.ImagesRoot))
                {
                    target.Runtime.ImagesRoot = existingRuntime.ImagesRoot.Trim();
                    imagesPreserved = true;
                }

                if (diabloPreserved || battleNetPreserved || imagesPreserved)
                {
                    AppLogger.Info(
                        "AppSettings runtime path values preserved from existing config: " +
                        $"path={existingConfigPath}; " +
                        $"preservedDiabloPath={diabloPreserved}; " +
                        $"preservedBattleNetPath={battleNetPreserved}; " +
                        $"preservedImagesRoot={imagesPreserved}");
                }

                return new RuntimePathPreservationResult(diabloPreserved, battleNetPreserved, imagesPreserved);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to inspect existing AppSettings runtime path values for preservation: {existingConfigPath}", ex);
                return default;
            }
        }

        private static bool ShouldPreserveExistingExecutablePath(string? currentPath, string? existingPath)
        {
            return string.IsNullOrWhiteSpace(currentPath) &&
                !string.IsNullOrWhiteSpace(existingPath);
        }

        private static bool ShouldPreserveExistingImagesRoot(string? currentPath, string? existingPath)
        {
            if (string.IsNullOrWhiteSpace(existingPath))
            {
                return false;
            }

            string current = currentPath?.Trim() ?? "";
            string existing = existingPath.Trim();
            if (string.Equals(current, existing, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(current) ||
                string.Equals(current, "Images", StringComparison.OrdinalIgnoreCase);
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

        private static bool HasSavedSuccessScreenshotsPreference(string json)
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
                    debugElement.TryGetProperty("EnableSuccessScreenshots", out _);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to inspect AppSettings Debug.EnableSuccessScreenshots preference.", ex);
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

        private static bool HasSavedGoblinTrackerPreferences(string json)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });

                return document.RootElement.TryGetProperty("GoblinTracker", out JsonElement goblinTrackerElement) &&
                    goblinTrackerElement.ValueKind == JsonValueKind.Object &&
                    goblinTrackerElement.TryGetProperty("AllowUnknownManualCount", out _) &&
                    goblinTrackerElement.TryGetProperty("EnableObservationMode", out _) &&
                    goblinTrackerElement.TryGetProperty("EnableAutomaticCounting", out _) &&
                    goblinTrackerElement.TryGetProperty("EnableDecisionTrace", out _);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to inspect AppSettings GoblinTracker preferences.", ex);
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
            return DebugManager.ResolveDebugDefaultsProfile(explicitProfile, debuggerAttached, debugBuild);
        }

        internal static bool IsVsDebugLaunchSurface(bool debuggerAttached, bool debugBuild)
        {
            return DebugManager.IsVsDebugLaunchSurface(debuggerAttached, debugBuild);
        }

        internal static bool ShouldSuppressFirstRunSetup(DebugDefaultsProfile profile)
        {
            return DebugManager.ShouldSuppressFirstRunSetup(profile);
        }

        internal static bool ShouldShowDynamicDebugControls(DebugDefaultsProfile profile)
        {
            return DebugManager.ShouldShowDynamicDebugControls(profile);
        }

        internal static bool ShouldRequireFirstRunSetup(DebugDefaultsProfile profile, bool requiredRuntimeConfigurationIsValid)
        {
            return DebugManager.ShouldRequireFirstRunSetup(profile, requiredRuntimeConfigurationIsValid);
        }

        public static void ApplyDebugDefaultsProfile()
        {
            CurrentDebugDefaultsProfile = ResolveDebugDefaultsProfile();
            firstRunSetupSuppressed = ShouldSuppressFirstRunSetup(CurrentDebugDefaultsProfile);
            if (CurrentDebugDefaultsProfile != DebugDefaultsProfile.VsDebug)
            {
                return;
            }

            DebugManager.ApplyVisualStudioDebugDefaults(settings.Debug);
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
            settings.GoblinTracker.EnableDecisionTrace = true;

            string? projectRoot = TryResolveProjectRoot();
            if (!string.IsNullOrWhiteSpace(projectRoot))
            {
                ApplyProjectRuntimeDefaults(projectRoot);

                string projectImages = Path.Combine(projectRoot, "Images");
                if (Directory.Exists(projectImages) && !Directory.Exists(ImagesRootPath))
                {
                    settings.Runtime.ImagesRoot = Path.GetRelativePath(RuntimeRootPath, projectImages);
                    AppLogger.Info($"VS/dev Images root defaulted from project folder: {settings.Runtime.ImagesRoot}");
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

            DebugManager.ApplyReleaseUserDefaultsIfPreferenceUnsaved(settings.Debug);
        }

        private static void RecordRawLoadedDiabloPath(string path)
        {
            rawDiabloExecutablePathLoaded = path ?? "";
            rawDiabloExecutablePathWasBlank = string.IsNullOrWhiteSpace(rawDiabloExecutablePathLoaded);
            AppLogger.Info(
                "AppSettings raw Diablo path loaded: " +
                $"configPath={configPath}; " +
                $"rawDiabloPath={rawDiabloExecutablePathLoaded}; " +
                $"diabloPathBlank={rawDiabloExecutablePathWasBlank}");
        }

        internal static LaunchProfileSnapshot ResolveLaunchProfileForTests(string? explicitProfile, bool debuggerAttached, bool debugBuild, string? explicitConfigPath, string baseDirectory)
        {
            DebugDefaultsProfile profile = ResolveDebugDefaultsProfile(explicitProfile, debuggerAttached, debugBuild);
            bool setupSuppressed = ShouldSuppressFirstRunSetup(profile);
            DebugSettings debug = new();
            if (profile == DebugDefaultsProfile.VsDebug)
            {
                DebugManager.ApplyVisualStudioDebugDefaults(debug);
            }

            return new LaunchProfileSnapshot(
                profile,
                setupSuppressed,
                debug.DebugMode,
                debug.EnableDebugScreenshots,
                ShouldShowDynamicDebugControls(profile),
                ResolveAppSettingsConfigPath(explicitConfigPath, baseDirectory, profile),
                ResolveAppSettingsConfigPathWithMetadata(explicitConfigPath, baseDirectory, profile).UsedVsDebugProjectRootConfig);
        }

        public static string ResolveRuntimePath(string path)
        {
            return ResolveRuntimePath(path, RuntimeRootPath);
        }

        private static string RuntimeRootPath
        {
            get
            {
                string? configDirectory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrWhiteSpace(configDirectory) &&
                    string.Equals(Path.GetFileName(configDirectory), "Config", StringComparison.OrdinalIgnoreCase))
                {
                    DirectoryInfo? root = Directory.GetParent(configDirectory);
                    if (root != null)
                    {
                        return root.FullName;
                    }
                }

                return AppDomain.CurrentDomain.BaseDirectory;
            }
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
            lastRuntimeDiscoveryResult = DiscoverMissingRuntimePathsCore(
                settings,
                RuntimeRootPath,
                DiscoverDiabloExecutable,
                DiscoverBattleNetExecutable);

            if (lastRuntimeDiscoveryResult.Changed)
            {
                Save();
            }

            return lastRuntimeDiscoveryResult.Changed;
        }

        internal static RuntimeDiscoveryResult DiscoverMissingRuntimePathsForTests(
            SettingsModel target,
            string runtimeRootPath,
            Func<string> diabloDiscoverer,
            Func<string> battleNetDiscoverer)
        {
            return DiscoverMissingRuntimePathsCore(target, runtimeRootPath, diabloDiscoverer, battleNetDiscoverer);
        }

        private static RuntimeDiscoveryResult DiscoverMissingRuntimePathsCore(
            SettingsModel target,
            string runtimeRootPath,
            Func<string> diabloDiscoverer,
            Func<string> battleNetDiscoverer)
        {
            bool changed = false;
            bool diabloDiscoveryRan = false;
            bool diabloDiscoveryFound = false;
            bool battleNetDiscoveryRan = false;
            bool battleNetDiscoveryFound = false;

            target.Normalize();

            if (!ExecutableExists(target.Runtime.DiabloExecutablePath, runtimeRootPath))
            {
                diabloDiscoveryRan = true;
                AppLogger.Info(
                    "Diablo III executable discovery started: " +
                    $"configPath={configPath}; " +
                    $"rawDiabloPathLoaded={rawDiabloExecutablePathLoaded}; " +
                    $"diabloPathBlank={rawDiabloExecutablePathWasBlank}");

                string discoveredDiablo = diabloDiscoverer();
                if (!string.IsNullOrWhiteSpace(discoveredDiablo))
                {
                    target.Runtime.DiabloExecutablePath = discoveredDiablo;
                    diabloDiscoveryFound = true;
                    AppLogger.Info($"Discovered Diablo III executable: {discoveredDiablo}");
                    changed = true;
                }
                else
                {
                    AppLogger.Info("Diablo III executable was not discovered automatically.");
                }
            }

            if (!ExecutableExists(target.Runtime.BattleNetExecutablePath, runtimeRootPath))
            {
                battleNetDiscoveryRan = true;
                string discoveredBattleNet = battleNetDiscoverer();
                if (!string.IsNullOrWhiteSpace(discoveredBattleNet))
                {
                    target.Runtime.BattleNetExecutablePath = discoveredBattleNet;
                    battleNetDiscoveryFound = true;
                    AppLogger.Info($"Discovered Battle.net executable: {discoveredBattleNet}");
                    changed = true;
                }
                else
                {
                    AppLogger.Info("Battle.net executable was not discovered automatically.");
                }
            }

            AppLogger.Info(
                "Runtime path auto-discovery summary: " +
                $"changed={changed}; " +
                $"diabloAutoDiscoveryRan={diabloDiscoveryRan}; " +
                $"diabloAutoDiscoveryFound={diabloDiscoveryFound}; " +
                $"battleNetAutoDiscoveryRan={battleNetDiscoveryRan}; " +
                $"battleNetAutoDiscoveryFound={battleNetDiscoveryFound}; " +
                $"finalDiabloPath={target.Runtime.DiabloExecutablePath}");

            return new RuntimeDiscoveryResult(
                changed,
                diabloDiscoveryRan,
                diabloDiscoveryFound,
                battleNetDiscoveryRan,
                battleNetDiscoveryFound);
        }

        public static bool RequiredRuntimeConfigurationIsValid(out string message)
        {
            RuntimeConfigurationValidationResult result = ValidateRuntimeConfiguration(settings, RuntimeRootPath);
            message = result.Message;
            AppLogger.Info(
                "Runtime configuration check: " +
                $"valid={result.Valid}; " +
                $"setupBlockedByMissingPath={!result.Valid}; " +
                $"diabloMissing={result.DiabloMissing}; " +
                $"battleNetMissing={result.BattleNetMissing}; " +
                $"imagesMissing={result.ImagesMissing}; " +
                $"missingTemplateFolderCount={result.MissingTemplateFolderCount}; " +
                $"configPath={configPath}; " +
                $"rawDiabloPathLoaded={rawDiabloExecutablePathLoaded}; " +
                $"diabloPathBlank={rawDiabloExecutablePathWasBlank}; " +
                $"diabloAutoDiscoveryRan={lastRuntimeDiscoveryResult.DiabloDiscoveryRan}; " +
                $"diabloAutoDiscoveryFound={lastRuntimeDiscoveryResult.DiabloDiscoveryFound}; " +
                $"vsDebugProjectRootConfigUsed={vsDebugProjectRootConfigUsed}; " +
                $"message={message.Replace(Environment.NewLine, " | ")}");
            return result.Valid;
        }

        internal static RuntimeConfigurationValidationResult ValidateRuntimeConfigurationForTests(SettingsModel target, string runtimeRootPath)
        {
            return ValidateRuntimeConfiguration(target, runtimeRootPath);
        }

        private static RuntimeConfigurationValidationResult ValidateRuntimeConfiguration(SettingsModel target, string runtimeRootPath)
        {
            List<string> errors = [];
            target.Normalize();
            bool diabloMissing = !ExecutableExists(target.Runtime.DiabloExecutablePath, runtimeRootPath);
            bool battleNetMissing = !ExecutableExists(target.Runtime.BattleNetExecutablePath, runtimeRootPath);
            string imagesRootPath = ResolveRuntimePath(target.Runtime.ImagesRoot, runtimeRootPath);
            bool imagesMissing = !Directory.Exists(imagesRootPath);

            if (diabloMissing)
            {
                errors.Add("Diablo III executable is missing or invalid.");
            }

            if (battleNetMissing)
            {
                errors.Add("Battle.net executable is missing or invalid.");
            }

            if (imagesMissing)
            {
                errors.Add($"Images folder is missing: {imagesRootPath}");
            }

            int missingTemplateFolderCount = 0;
            string[] requiredImageFolders = ["Combat", "Current Location", "Goblin Evidence", "Leave Game", "Repair", "Salvage", "Start Game", "Teleport Function"];
            foreach (string folder in requiredImageFolders)
            {
                string path = Path.Combine(imagesRootPath, folder);
                if (!Directory.Exists(path))
                {
                    missingTemplateFolderCount++;
                    errors.Add($"Required template folder is missing: {path}");
                }
            }

            return new RuntimeConfigurationValidationResult(
                errors.Count == 0,
                string.Join(Environment.NewLine, errors),
                diabloMissing,
                battleNetMissing,
                imagesMissing,
                missingTemplateFolderCount);
        }

        internal static string ResolveStartupAppStatusForTests(bool requiredRuntimeConfigurationIsValid)
        {
            return requiredRuntimeConfigurationIsValid ? "Idle" : "Setup Required";
        }

        public static bool ExecutableExists(string path)
        {
            return ExecutableExists(path, RuntimeRootPath);
        }

        private static bool ExecutableExists(string path, string runtimeRootPath)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                string resolved = ResolveRuntimePath(path, runtimeRootPath);
                return File.Exists(resolved) && resolved.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string ResolveRuntimePath(string path, string runtimeRootPath)
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

            return Path.GetFullPath(Path.Combine(runtimeRootPath, expanded));
        }

        private static string DiscoverDiabloExecutable()
        {
            List<string> configuredRoots =
            [
                RegistryString(@"HKEY_LOCAL_MACHINE\SOFTWARE\Blizzard Entertainment\Diablo III", "InstallPath"),
                RegistryString(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Blizzard Entertainment\Diablo III", "InstallPath"),
                RegistryString(@"HKEY_CURRENT_USER\SOFTWARE\Blizzard Entertainment\Diablo III", "InstallPath"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Diablo III"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Diablo III"),
                @"C:\Program Files\Diablo III",
                @"C:\Program Files (x86)\Diablo III",
            ];

            IReadOnlyList<string> candidateRoots = BuildDiabloCandidateRoots(configuredRoots, EnumerateFixedDriveRootPaths());
            return FindFirstDiabloExecutableCandidate(candidateRoots);
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

        internal static IReadOnlyList<string> BuildDiabloCandidateRootsForTests(IEnumerable<string> configuredRoots, IEnumerable<string> fixedDriveRoots)
        {
            return BuildDiabloCandidateRoots(configuredRoots, fixedDriveRoots);
        }

        internal static string FindFirstDiabloExecutableCandidateForTests(IEnumerable<string> candidateRoots)
        {
            return FindFirstDiabloExecutableCandidate(candidateRoots);
        }

        private static IReadOnlyList<string> BuildDiabloCandidateRoots(IEnumerable<string> configuredRoots, IEnumerable<string> fixedDriveRoots)
        {
            List<string> roots = [];
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            AddConfiguredRoots(configuredRoots);
            foreach (string driveRoot in fixedDriveRoots)
            {
                if (string.IsNullOrWhiteSpace(driveRoot))
                {
                    continue;
                }

                string root = Path.GetFullPath(Environment.ExpandEnvironmentVariables(driveRoot.Trim()));
                AddRoot(Path.Combine(root, "Diablo III"));
                AddRoot(Path.Combine(root, "Games", "Diablo III"));
                AddRoot(Path.Combine(root, "Battle.net", "Diablo III"));
                AddRoot(Path.Combine(root, "Program Files", "Diablo III"));
                AddRoot(Path.Combine(root, "Program Files (x86)", "Diablo III"));
            }

            return roots;

            void AddConfiguredRoots(IEnumerable<string> values)
            {
                foreach (string value in values)
                {
                    AddRoot(value);
                }
            }

            void AddRoot(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                try
                {
                    string expanded = Environment.ExpandEnvironmentVariables(value.Trim());
                    string fullPath = Path.GetFullPath(expanded);
                    if (seen.Add(fullPath))
                    {
                        roots.Add(fullPath);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Invalid Diablo III discovery candidate root skipped: {value}", ex);
                }
            }
        }

        private static IReadOnlyList<string> EnumerateFixedDriveRootPaths()
        {
            List<string> roots = [];
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType != DriveType.Fixed)
                    {
                        continue;
                    }

                    try
                    {
                        if (!drive.IsReady)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        continue;
                    }

                    roots.Add(drive.RootDirectory.FullName);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to enumerate fixed drives for Diablo III discovery.", ex);
            }

            return roots;
        }

        private static string FindFirstDiabloExecutableCandidate(IEnumerable<string> candidateRoots)
        {
            string[] preferredRelativePaths =
            [
                Path.Combine("x64", "Diablo III64.exe"),
                "Diablo III64.exe",
                "Diablo III.exe",
            ];

            List<string> roots = candidateRoots
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Path.GetFullPath(Environment.ExpandEnvironmentVariables(value.Trim())))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (string rootOrFile in roots)
            {
                bool directFile = File.Exists(rootOrFile);
                bool directoryExists = Directory.Exists(rootOrFile);
                AppLogger.Info(
                    "Diablo III discovery candidate root checked: " +
                    $"root={rootOrFile}; " +
                    $"directoryExists={directoryExists}; " +
                    $"directFile={directFile}");
            }

            foreach (string relativePath in preferredRelativePaths)
            {
                foreach (string rootOrFile in roots)
                {
                    string candidate = ResolveDiabloExecutableCandidate(rootOrFile, relativePath);
                    if (string.IsNullOrWhiteSpace(candidate))
                    {
                        continue;
                    }

                    if (IsAllowedDiabloGameExecutable(candidate) && File.Exists(candidate))
                    {
                        string selected = Path.GetFullPath(candidate);
                        AppLogger.Info($"Diablo III discovery selected executable: path={selected}; relativeCandidate={relativePath}");
                        return selected;
                    }
                }
            }

            AppLogger.Info("Diablo III discovery selected executable: path=; result=not_found");
            return "";
        }

        private static string ResolveDiabloExecutableCandidate(string rootOrFile, string relativePath)
        {
            if (File.Exists(rootOrFile))
            {
                return Path.GetFileName(rootOrFile).Equals(Path.GetFileName(relativePath), StringComparison.OrdinalIgnoreCase)
                    ? rootOrFile
                    : "";
            }

            return Path.Combine(rootOrFile, relativePath);
        }

        private static bool IsAllowedDiabloGameExecutable(string path)
        {
            string fileName = Path.GetFileName(path);
            return fileName.Equals("Diablo III64.exe", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("Diablo III.exe", StringComparison.OrdinalIgnoreCase);
        }

        private static void LogLoadedValues(string configPath)
        {
            AppLogger.Info(
                "AppSettings loaded: " +
                $"AppSettingsPath={configPath}; " +
                $"path={configPath}; " +
                $"ConfigPathResolution={appSettingsPathResolution}; " +
                $"VsDebugProjectRootConfigUsed={vsDebugProjectRootConfigUsed}; " +
                $"RuntimeRootPath={RuntimeRootPath}; " +
                $"RawDiabloPathLoaded={rawDiabloExecutablePathLoaded}; " +
                $"DiabloPathBlank={rawDiabloExecutablePathWasBlank}; " +
                $"DiabloAutoDiscoveryRan={lastRuntimeDiscoveryResult.DiabloDiscoveryRan}; " +
                $"DiabloAutoDiscoveryFound={lastRuntimeDiscoveryResult.DiabloDiscoveryFound}; " +
                $"PreservedDiabloPath={lastRuntimePathPreservation.DiabloPathPreserved}; " +
                $"PreservedBattleNetPath={lastRuntimePathPreservation.BattleNetPathPreserved}; " +
                $"PreservedImagesRoot={lastRuntimePathPreservation.ImagesRootPreserved}; " +
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
                $"Debug.EnableSuccessScreenshots={Debug.EnableSuccessScreenshots}; " +
                $"Debug.EnableMissingAssetPrompts={Debug.EnableMissingAssetPrompts}; " +
                $"Debug.EnableVerboseLogging={Debug.EnableVerboseLogging}; " +
                $"VerboseLogging={Debug.EnableVerboseLogging}; " +
                $"Debug.SessionSummaryRetentionCount={Debug.SessionSummaryRetentionCount}; " +
                $"Debug.DebugPackageRetentionCount={Debug.DebugPackageRetentionCount}; " +
                $"Debug.GoblinEvidenceRetentionCount={Debug.GoblinEvidenceRetentionCount}; " +
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
                $"GoblinTracker.AllowUnknownManualCount={GoblinTracker.AllowUnknownManualCount}; " +
                $"GoblinTracker.EnableObservationMode={GoblinTracker.EnableObservationMode}; " +
                $"GoblinTracker.EnableAutomaticCounting={GoblinTracker.EnableAutomaticCounting}; " +
                $"GoblinTracker.EnableDecisionTrace={GoblinTracker.EnableDecisionTrace}; " +
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
            string ConfigPath,
            bool VsDebugProjectRootConfigUsed);

        private readonly record struct ConfigPathResolution(
            string Path,
            bool UsedVsDebugProjectRootConfig,
            string ProjectRootConfigPath,
            string Reason);

        internal readonly record struct RuntimePathPreservationResult(
            bool DiabloPathPreserved,
            bool BattleNetPathPreserved,
            bool ImagesRootPreserved);

        internal readonly record struct RuntimeDiscoveryResult(
            bool Changed,
            bool DiabloDiscoveryRan,
            bool DiabloDiscoveryFound,
            bool BattleNetDiscoveryRan,
            bool BattleNetDiscoveryFound);

        internal readonly record struct RuntimeConfigurationValidationResult(
            bool Valid,
            string Message,
            bool DiabloMissing,
            bool BattleNetMissing,
            bool ImagesMissing,
            int MissingTemplateFolderCount);

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
            public GoblinTrackerSettings GoblinTracker { get; set; } = new();
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
                    GoblinTracker = new GoblinTrackerSettings(),
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
                    GoblinTracker = GoblinTracker,
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
                GoblinTracker ??= new GoblinTrackerSettings();
                User ??= new UserSettings();
                Runtime.Normalize();
                Launch.Normalize();
                UI.Normalize();
                Debug.Normalize();
                Repair.Normalize();
                Teleport.Normalize();
                Bounty.Normalize();
                ImageRecognition.Normalize();
                GoblinTracker.Normalize();
                User.Normalize();
            }
        }

        internal sealed class GoblinTrackerSettings
        {
            public bool AllowUnknownManualCount { get; set; } = false;
            public bool EnableObservationMode { get; set; } = true;
            public bool EnableAutomaticCounting { get; set; } = false;
            public bool EnableDecisionTrace { get; set; } = false;

            public void Normalize()
            {
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
            // Debug-only success milestone screenshots; disabled by default to keep normal runs and packages compact.
            public bool EnableSuccessScreenshots { get; set; } = DefaultEnableSuccessScreenshots;
            public bool EnableMissingAssetPrompts { get; set; }
            public bool EnableVerboseLogging { get; set; }
            public int SessionSummaryRetentionCount { get; set; } = DefaultSessionSummaryRetentionCount;
            public int DebugPackageRetentionCount { get; set; } = DefaultDebugPackageRetentionCount;
            public int GoblinEvidenceRetentionCount { get; set; } = DefaultGoblinEvidenceRetentionCount;

            public DebugSettings Clone()
            {
                return new DebugSettings
                {
                    DebugModePreferenceSaved = DebugModePreferenceSaved,
                    DebugMode = DebugMode,
                    ShowDiagnosticOverlay = ShowDiagnosticOverlay,
                    ShowRouteInspector = ShowRouteInspector,
                    EnableDebugScreenshots = EnableDebugScreenshots,
                    EnableSuccessScreenshots = EnableSuccessScreenshots,
                    EnableMissingAssetPrompts = EnableMissingAssetPrompts,
                    EnableVerboseLogging = EnableVerboseLogging,
                    SessionSummaryRetentionCount = SessionSummaryRetentionCount,
                    DebugPackageRetentionCount = DebugPackageRetentionCount,
                    GoblinEvidenceRetentionCount = GoblinEvidenceRetentionCount,
                };
            }

            public void Normalize()
            {
                SessionSummaryRetentionCount = Math.Clamp(SessionSummaryRetentionCount, 0, 1000);
                DebugPackageRetentionCount = Math.Clamp(DebugPackageRetentionCount, 0, 1000);
                GoblinEvidenceRetentionCount = Math.Clamp(GoblinEvidenceRetentionCount, 0, 1000);
            }

            public const bool DefaultEnableDebugScreenshots = false;
            public const bool DefaultEnableSuccessScreenshots = false;
            public const int DefaultSessionSummaryRetentionCount = 50;
            public const int DefaultDebugPackageRetentionCount = 20;
            public const int DefaultGoblinEvidenceRetentionCount = 250;
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

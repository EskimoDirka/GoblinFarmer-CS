using System.Drawing;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using GoblinFarmer;

int failures = 0;

Run("Python no-click rectangles use blacklist safety and Python boundaries", TestPythonNoClickRectangles);
Run("Unsafe cursor suppresses left clicks without stopping Demon Hunter key loop", TestUnsafeCursorDoesNotStopKeyLoop);
Run("Demon Hunter right mouse remains held after initial safe start", TestRightMouseRemainsHeldAfterSafeStart);
Run("Initial safe-wait timeout stops only when Python would stop", TestInitialSafeWaitTimeoutPolicy);
Run("Default AppSettings path/debug profile are launch-surface neutral", TestAppSettingsLaunchParityDefaults);
Run("VS Debug/dev profile suppresses first-run setup and forces internal debug defaults", TestVsDebugDevProfileDefaults);
Run("VS Debug/dev profile prefers project-root AppSettings", TestVsDebugProjectRootConfigPreferred);
Run("Missing Diablo path keeps startup in setup required", TestMissingDiabloPathKeepsStartupInSetupRequired);
Run("VS Debug blank project-root Diablo path attempts discovery", TestVsDebugBlankProjectRootDiabloPathAttemptsDiscovery);
Run("Diablo discovery finds custom drive root install", TestDiabloDiscoveryFindsCustomDriveRootInstall);
Run("Diablo discovery ignores launcher executable", TestDiabloDiscoveryIgnoresLauncherExecutable);
Run("Diablo discovery prefers 64-bit executable", TestDiabloDiscoveryPrefers64BitExecutable);
Run("Diablo discovery stays bounded to known roots", TestDiabloDiscoveryStaysBoundedToKnownRoots);
Run("Configured valid Diablo path wins over discovery", TestConfiguredValidDiabloPathWinsOverDiscovery);
Run("DebugManager profile helpers separate VS, release debug, and normal release", TestDebugManagerProfileHelpers);
Run("DebugManager retention cleanup only deletes matching artifacts", TestDebugManagerRetentionCleanupFilters);
Run("DebugManager age retention deletes old debug artifacts", TestDebugManagerAgeRetentionDeletesOldArtifacts);
Run("GoblinEvidence retention keeps newest 250 files", TestGoblinEvidenceRetentionKeepsNewest250Files);
Run("GoblinEvidence retention breaks timestamp ties by filename", TestGoblinEvidenceRetentionBreaksTimestampTiesByFilename);
Run("GoblinEvidence retention ignores missing folder", TestGoblinEvidenceRetentionMissingFolderDoesNotThrow);
Run("GoblinEvidence retention count less than one disables cleanup", TestGoblinEvidenceRetentionCountLessThanOneDisablesCleanup);
Run("GoblinEvidence retention deletes only inside GoblinEvidence", TestGoblinEvidenceRetentionDeletesOnlyInsideFolder);
Run("GoblinEvidence template discovery accepts per-goblin evidence files", TestGoblinEvidenceTemplateDiscoveryAcceptsPerGoblinEvidenceFiles);
Run("GoblinEvidence template discovery finds source image set", TestGoblinEvidenceTemplateDiscoveryFindsSourceImageSet);
Run("GoblinEvidence observation scan regions match calibration", TestGoblinEvidenceObservationScanRegionsMatchCalibration);
Run("Installed/release profile with missing paths still requires first-run setup", TestReleaseProfileRequiresSetupWhenMissingPaths);
Run("Release Goblin Tracker layout keeps observation fields separated", TestReleaseGoblinTrackerLayoutKeepsObservationFieldsSeparated);
Run("VS Debug diagnostics include next test steps tab", TestVsDebugDiagnosticsIncludeNextTestStepsTab);
Run("Battle.net successful launch diagnostics avoid failure screenshots", TestBattleNetSuccessfulLaunchDiagnosticsAvoidFailureScreenshots);
Run("Explicit AppSettings path override wins", TestExplicitAppSettingsPathOverrideWins);
Run("AppSettings migration preserves existing runtime paths", TestAppSettingsMigrationPreservesRuntimePaths);
Run("Demon Hunter no-click suppression diagnostic is not named as failure or stall", TestDemonHunterNoClickSuppressionDiagnosticName);
Run("Witch Doctor combat uses mouse wheel and not held-left mode", TestWitchDoctorCombatUsesMouseWheelNotHeldLeftMode);
Run("Start Game click policy blocks Leave Game and in-game signals", TestStartGameClickPolicyBlocksInGameSignals);
Run("Goblin journal parser counts escaped goblin encounters", TestGoblinJournalParserCountsEscapedEncounters);
Run("Goblin type normalization maps Gelatinous Spawn to Gelatinous Sire", TestGelatinousSpawnNormalizesToSire);
Run("Goblin minimap color disambiguates Treasure and Odious", TestGoblinMinimapColorDisambiguatesTreasureAndOdious);
Run("Goblin minimap color disambiguates Gilded and Malevolent", TestGoblinMinimapColorDisambiguatesGildedAndMalevolent);
Run("Goblin automatic minimap counts require strong confidence", TestGoblinAutomaticMinimapCountsRequireStrongConfidence);
Run("Goblin journal freshness stays area strict across Caverns levels", TestGoblinJournalFreshnessStaysAreaStrictAcrossCavernsLevels);
Run("Debug package excludes success screenshots by default", TestDebugPackageExcludesSuccessScreenshotsByDefault);
Run("Debug package limits failure and debug screenshots by default", TestDebugPackageLimitsFailureAndDebugScreenshotsByDefault);
Run("Debug package limits observation diagnostic crops", TestDebugPackageLimitsObservationDiagnosticCrops);
Run("Debug package limits goblin evidence event screenshots", TestDebugPackageLimitsGoblinEvidenceEventScreenshots);
Run("Debug package includes success screenshots only with opt-in", TestDebugPackageIncludesSuccessScreenshotsWithOptIn);
Run("Goblin area resolver keeps true areas separate", TestGoblinAreaResolverKeepsTrueAreasSeparate);
Run("Goblin area duplicate guard suppresses same area and resets", TestGoblinAreaDuplicateGuardSuppressesSameAreaAndResets);
Run("Goblin area duplicate guard allows PF exceptions twice only", TestGoblinAreaDuplicateGuardAllowsPandemoniumFortressTwiceOnly);
Run("Goblin area duplicate guard allows Stinging Winds twice only", TestGoblinAreaDuplicateGuardAllowsStingingWindsTwiceOnly);
Run("Goblin area duplicate guard keeps default one-count areas", TestGoblinAreaDuplicateGuardKeepsDefaultOneCountAreas);
Run("Goblin area duplicate guard peek does not consume count slots", TestGoblinAreaDuplicateGuardPeekDoesNotConsumeCountSlots);
Run("Goblin manual count block list blocks explicit no-count areas", TestGoblinManualCountBlockListBlocksExplicitNoCountAreas);
Run("Goblin observation counters are diagnostic only", TestGoblinObservationCountersAreDiagnosticOnly);
Run("Goblin observation type reuse requires recent matching area", TestGoblinObservationTypeReuseRequiresRecentMatchingArea);
Run("Goblin manual unknown count requires fresh observation by default", TestGoblinManualUnknownCountRequiresFreshObservationByDefault);
Run("Goblin observation UI state logs update and clear", TestGoblinObservationUiStateLogsUpdateAndClear);
Run("Goblin observation mode is enabled by default in Release", TestGoblinObservationModeEnabledByDefaultInRelease);
Run("Goblin automatic counting gate defaults disabled", TestGoblinAutomaticCountingGateDefaultsDisabled);
Run("Goblin VS Debug automatic-count settings are form-toggleable", TestGoblinVsDebugAutomaticCountSettingsAreFormToggleable);
Run("Goblin VS Debug manual test count override is safety-scoped", TestGoblinVsDebugManualTestCountOverrideIsSafetyScoped);
Run("Goblin decision trace logs count stale block and duplicate", TestGoblinDecisionTraceLogsCountStaleBlockAndDuplicate);
Run("Goblin replay tool is dry-run and VS Debug review files are loose", TestGoblinReplayToolIsDryRunAndPackaged);
Run("Goblin automatic counting requires fresh armed evidence", TestGoblinAutomaticCountingRequiresFreshArmedEvidence);
Run("Goblin accepted manual count updates Last Observation display", TestGoblinAcceptedManualCountUpdatesLastObservationDisplay);
Run("Goblin stale journal freshness policy suppresses old visible lines", TestGoblinStaleJournalFreshnessPolicySuppressesOldVisibleLines);
Run("Goblin fresh killed journal can satisfy evidence gate", TestGoblinFreshKilledJournalCanSatisfyEvidenceGate);
Run("Goblin refresh logs fresh and stale killed journal decisions", TestGoblinRefreshLogsFreshAndStaleKilledJournalDecisions);
Run("Goblin reset clears stale observation state", TestGoblinResetClearsStaleObservationState);
Run("Goblin manual no-fresh gate preserves blocked area priority", TestGoblinManualNoFreshGatePreservesBlockedAreaPriority);
Run("Salvage loop uses bounded confirmation wait and timing logs", TestSalvageLoopUsesBoundedConfirmationWaitAndTimingLogs);
Run("Kadala hotkey uses faster cadence and timing logs", TestKadalaHotkeyUsesFasterCadenceAndTimingLogs);
Run("Teleport Next no-route state notifies user", TestTeleportNextNoRouteStateNotifiesUser);
Run("Goblin area detection disambiguates PF false positives from route context", TestGoblinAreaDetectionDisambiguatesPandemoniumFalsePositivesFromRouteContext);
Run("Goblin area detection uses strong route-context runner-up", TestGoblinAreaDetectionUsesStrongRouteContextRunnerUp);
Run("Goblin area detection blocks unresolved PF ambiguity", TestGoblinAreaDetectionBlocksUnresolvedPandemoniumAmbiguity);
Run("Goblin area detection false PF matches do not consume PF area slots", TestGoblinAreaDetectionFalsePandemoniumMatchesDoNotConsumePandemoniumSlots);
Run("Goblin tracker records allow unknown manual fallback and reset cleanly", TestGoblinTrackerRecordsAllowUnknownManualFallbackAndReset);

if (failures > 0)
{
    Console.Error.WriteLine($"{failures} test(s) failed.");
    return 1;
}

Console.WriteLine("All tests passed.");
return 0;

void Run(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

static void TestPythonNoClickRectangles()
{
    Rectangle diabloRect = new(0, 0, CombatClickSafety.ReferenceWidth, CombatClickSafety.ReferenceHeight);

    foreach (CombatNoClickRegion region in CombatClickSafety.PythonCombatNoClickRegions)
    {
        Rectangle scaled = region.ScaleTo(diabloRect);

        AssertUnsafe(new Point(scaled.Left, scaled.Top), diabloRect, $"{region.Name} left/top boundary");
        AssertUnsafe(new Point(scaled.Right - 1, scaled.Bottom - 1), diabloRect, $"{region.Name} right/bottom interior");
        AssertAnySafe(OutsideCandidates(scaled), diabloRect, $"{region.Name} has a safe just-outside point");

        AssertFalse(CombatClickSafety.ContainsPythonBoundary(scaled, new Point(scaled.Right, scaled.Top)), $"{region.Name} right boundary exclusive");
        AssertFalse(CombatClickSafety.ContainsPythonBoundary(scaled, new Point(scaled.Left, scaled.Bottom)), $"{region.Name} bottom boundary exclusive");
        AssertTrue(CombatClickSafety.ContainsPythonBoundary(scaled, new Point(scaled.Left, scaled.Top)), $"{region.Name} left/top boundary inclusive");
    }
}

static void TestUnsafeCursorDoesNotStopKeyLoop()
{
    Rectangle diabloRect = new(0, 0, CombatClickSafety.ReferenceWidth, CombatClickSafety.ReferenceHeight);
    Point unsafeCursor = new(CombatClickSafety.PythonCombatNoClickRegions[0].Left, CombatClickSafety.PythonCombatNoClickRegions[0].Top);

    AssertFalse(CombatClickSafety.CombatMouseClickIsSafe(unsafeCursor, diabloRect), "cursor should be unsafe inside no-click region");
    AssertTrue(DemonHunterCombatPolicy.KeyLoopContinuesWhenCursorUnsafe(combatRunning: true, combatClass: "demon_hunter", diabloActive: true), "key loop should continue independently of click suppression");
}

static void TestRightMouseRemainsHeldAfterSafeStart()
{
    AssertTrue(DemonHunterCombatPolicy.RightMouseRemainsHeldAfterSafeStart(startedFromSafeCursor: true, combatRunning: true, combatClass: "demon_hunter", diabloActive: true), "right mouse should remain held after safe start");
    AssertFalse(DemonHunterCombatPolicy.RightMouseRemainsHeldAfterSafeStart(startedFromSafeCursor: false, combatRunning: true, combatClass: "demon_hunter", diabloActive: true), "right mouse should not hold without initial safe start");
}

static void TestInitialSafeWaitTimeoutPolicy()
{
    AssertTrue(DemonHunterCombatPolicy.SafeWaitTimeoutStopsCombat(safeFoundWithinTimeout: false), "timeout should stop Demon Hunter combat");
    AssertFalse(DemonHunterCombatPolicy.SafeWaitTimeoutStopsCombat(safeFoundWithinTimeout: true), "safe cursor before timeout should not stop combat");
}

static void TestAppSettingsLaunchParityDefaults()
{
    AssertTrue(AppSettings.ConfigPath.EndsWith(Path.Combine("Config", "AppSettings.json"), StringComparison.OrdinalIgnoreCase), "AppSettings should use app-local Config/AppSettings.json by default");
    AssertFalse(AppSettings.ConfigPath.Contains("GoblinFarmer.csproj", StringComparison.OrdinalIgnoreCase), "AppSettings path should not be resolved from the project file");
    AssertEqual(AppSettings.DebugDefaultsProfile.ReleaseUser, AppSettings.CurrentDebugDefaultsProfile, "default debug profile should be ReleaseUser unless explicitly overridden");
    AssertTrue(AppSettings.BuildConfiguration is "Debug" or "Release", "build configuration should be reported");
}

static void TestVsDebugDevProfileDefaults()
{
    AppSettings.LaunchProfileSnapshot snapshot = AppSettings.ResolveLaunchProfileForTests(
        explicitProfile: null,
        debuggerAttached: true,
        debugBuild: true,
        explicitConfigPath: null,
        baseDirectory: @"C:\dev\GoblinFarmer\bin\Debug\net10.0-windows");

    AssertEqual(AppSettings.DebugDefaultsProfile.VsDebug, snapshot.Profile, "VS Debug launch surface should activate VS/dev profile");
    AssertTrue(snapshot.FirstRunSetupSuppressed, "VS/dev profile should suppress first-run setup");
    AssertTrue(snapshot.DebugMode, "VS/dev profile should force Debug Mode on internally");
    AssertTrue(snapshot.KeepDebugScreenshots, "VS/dev profile should force Keep Debug Screenshots on internally");
    AssertFalse(snapshot.DynamicDebugControlsVisible, "VS/dev profile should not show dynamic debug checkboxes");
}

static void TestVsDebugProjectRootConfigPreferred()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.ConfigTests", Guid.NewGuid().ToString("N"));
    string projectConfigDirectory = Path.Combine(root, "Config");
    string binDirectory = Path.Combine(root, "bin", "Debug", "net10.0-windows");
    string binConfigDirectory = Path.Combine(binDirectory, "Config");
    string diabloPath = Path.Combine(root, "Diablo III", "Diablo III64.exe");
    string battleNetPath = Path.Combine(root, "Battle.net", "Battle.net.exe");
    Directory.CreateDirectory(projectConfigDirectory);
    Directory.CreateDirectory(binConfigDirectory);
    Directory.CreateDirectory(Path.GetDirectoryName(diabloPath)!);
    Directory.CreateDirectory(Path.GetDirectoryName(battleNetPath)!);
    File.WriteAllText(diabloPath, "fake exe for config test");
    File.WriteAllText(battleNetPath, "fake exe for config test");
    CreateRequiredImagesFolders(Path.Combine(root, "Images"));

    try
    {
        string projectConfigPath = Path.Combine(projectConfigDirectory, "AppSettings.json");
        string binConfigPath = Path.Combine(binConfigDirectory, "AppSettings.json");
        File.WriteAllText(Path.Combine(root, "GoblinFarmer.csproj"), "<Project />");
        File.WriteAllText(projectConfigPath, """
            {
              "Runtime": {
                "DiabloExecutablePath": "%DIABLO_PATH%",
                "BattleNetExecutablePath": "%BATTLENET_PATH%",
                "ImagesRoot": "Images"
              }
            }
            """
            .Replace("%DIABLO_PATH%", EscapeJsonPath(diabloPath))
            .Replace("%BATTLENET_PATH%", EscapeJsonPath(battleNetPath)));
        File.WriteAllText(binConfigPath, """
            {
              "Runtime": {
                "DiabloExecutablePath": "",
                "BattleNetExecutablePath": "",
                "ImagesRoot": "Images"
              }
            }
            """);

        AppSettings.LaunchProfileSnapshot snapshot = AppSettings.ResolveLaunchProfileForTests(
            explicitProfile: null,
            debuggerAttached: true,
            debugBuild: true,
            explicitConfigPath: null,
            baseDirectory: binDirectory);

        AssertEqual(Path.GetFullPath(projectConfigPath), snapshot.ConfigPath, "VS Debug should prefer project-root AppSettings over stale bin config");
        AssertTrue(snapshot.VsDebugProjectRootConfigUsed, "snapshot should record that project-root config was used");

        AppSettings.SettingsModel? loaded = JsonSerializer.Deserialize<AppSettings.SettingsModel>(File.ReadAllText(snapshot.ConfigPath));
        AssertTrue(loaded != null, "project-root AppSettings should deserialize");
        loaded!.Normalize();
        AssertEqual(diabloPath, loaded.Runtime.DiabloExecutablePath, "VS Debug should load Diablo path from project-root AppSettings");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void TestMissingDiabloPathKeepsStartupInSetupRequired()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.ConfigTests", Guid.NewGuid().ToString("N"));
    string battleNetPath = Path.Combine(root, "Battle.net", "Battle.net.exe");
    Directory.CreateDirectory(Path.GetDirectoryName(battleNetPath)!);
    File.WriteAllText(battleNetPath, "fake exe for config test");
    CreateRequiredImagesFolders(Path.Combine(root, "Images"));

    try
    {
        AppSettings.SettingsModel model = AppSettings.SettingsModel.Default();
        model.Runtime.DiabloExecutablePath = "";
        model.Runtime.BattleNetExecutablePath = battleNetPath;
        model.Runtime.ImagesRoot = "Images";

        AppSettings.RuntimeConfigurationValidationResult validation = AppSettings.ValidateRuntimeConfigurationForTests(model, root);

        AssertFalse(validation.Valid, "runtime validation should fail when Diablo is missing even if Battle.net and Images are valid");
        AssertTrue(validation.DiabloMissing, "validation should report Diablo missing");
        AssertFalse(validation.BattleNetMissing, "Battle.net should be valid in this scenario");
        AssertFalse(validation.ImagesMissing, "Images should be valid in this scenario");
        AssertEqual("Setup Required", AppSettings.ResolveStartupAppStatusForTests(validation.Valid), "missing Diablo should produce Setup Required status");
        AssertFalse(AppSettings.ResolveStartupAppStatusForTests(validation.Valid) == "Idle", "missing Diablo should not allow Idle status");
        AssertTrue(AppSettings.ShouldRequireFirstRunSetup(AppSettings.DebugDefaultsProfile.VsDebug, validation.Valid), "VS Debug should still require setup when Diablo is missing");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void TestVsDebugBlankProjectRootDiabloPathAttemptsDiscovery()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.ConfigTests", Guid.NewGuid().ToString("N"));
    string discoveredDiabloPath = Path.Combine(root, "Diablo III", "Diablo III64.exe");
    Directory.CreateDirectory(Path.GetDirectoryName(discoveredDiabloPath)!);
    File.WriteAllText(discoveredDiabloPath, "fake exe for discovery test");

    try
    {
        AppSettings.SettingsModel model = AppSettings.SettingsModel.Default();
        model.Runtime.DiabloExecutablePath = "";
        model.Runtime.BattleNetExecutablePath = "";
        model.Runtime.ImagesRoot = "Images";

        AppSettings.RuntimeDiscoveryResult result = AppSettings.DiscoverMissingRuntimePathsForTests(
            model,
            root,
            () => discoveredDiabloPath,
            () => "");

        AssertTrue(result.DiabloDiscoveryRan, "blank Diablo path should run Diablo auto-discovery");
        AssertTrue(result.DiabloDiscoveryFound, "fake discovery should report Diablo found");
        AssertEqual(discoveredDiabloPath, model.Runtime.DiabloExecutablePath, "discovered Diablo path should be applied to active settings");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void TestDiabloDiscoveryFindsCustomDriveRootInstall()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.DriveTests", Guid.NewGuid().ToString("N"));
    string diabloPath = Path.Combine(root, "Diablo III", "Diablo III.exe");
    Directory.CreateDirectory(Path.GetDirectoryName(diabloPath)!);
    File.WriteAllText(diabloPath, "fake exe for discovery test");

    try
    {
        IReadOnlyList<string> candidateRoots = AppSettings.BuildDiabloCandidateRootsForTests([], [root]);
        string selected = AppSettings.FindFirstDiabloExecutableCandidateForTests(candidateRoots);

        AssertEqual(Path.GetFullPath(diabloPath), selected, "custom drive root Diablo III install should be discovered");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void TestDiabloDiscoveryIgnoresLauncherExecutable()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.DriveTests", Guid.NewGuid().ToString("N"));
    string launcherPath = Path.Combine(root, "Diablo III", "Diablo III Launcher.exe");
    Directory.CreateDirectory(Path.GetDirectoryName(launcherPath)!);
    File.WriteAllText(launcherPath, "launcher should be ignored");

    try
    {
        IReadOnlyList<string> candidateRoots = AppSettings.BuildDiabloCandidateRootsForTests([], [root]);
        string selected = AppSettings.FindFirstDiabloExecutableCandidateForTests(candidateRoots);

        AssertEqual("", selected, "Diablo III Launcher.exe should never be selected as the game executable");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void TestDiabloDiscoveryPrefers64BitExecutable()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.DriveTests", Guid.NewGuid().ToString("N"));
    string x86Path = Path.Combine(root, "Diablo III", "Diablo III.exe");
    string x64Path = Path.Combine(root, "Diablo III", "x64", "Diablo III64.exe");
    Directory.CreateDirectory(Path.GetDirectoryName(x86Path)!);
    Directory.CreateDirectory(Path.GetDirectoryName(x64Path)!);
    File.WriteAllText(x86Path, "fake 32-bit exe for discovery test");
    File.WriteAllText(x64Path, "fake 64-bit exe for discovery test");

    try
    {
        IReadOnlyList<string> candidateRoots = AppSettings.BuildDiabloCandidateRootsForTests([], [root]);
        string selected = AppSettings.FindFirstDiabloExecutableCandidateForTests(candidateRoots);

        AssertEqual(Path.GetFullPath(x64Path), selected, "Diablo III64.exe should be preferred when both game executables exist");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void TestDiabloDiscoveryStaysBoundedToKnownRoots()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.DriveTests", Guid.NewGuid().ToString("N"));
    string nestedPath = Path.Combine(root, "Unlisted", "Deep", "Diablo III64.exe");
    Directory.CreateDirectory(Path.GetDirectoryName(nestedPath)!);
    File.WriteAllText(nestedPath, "fake exe outside bounded roots");

    try
    {
        IReadOnlyList<string> candidateRoots = AppSettings.BuildDiabloCandidateRootsForTests([], [root]);
        string selected = AppSettings.FindFirstDiabloExecutableCandidateForTests(candidateRoots);

        AssertEqual("", selected, "discovery should not recursively scan arbitrary drive folders");
        AssertFalse(candidateRoots.Any(path => path.Contains(Path.Combine("Unlisted", "Deep"), StringComparison.OrdinalIgnoreCase)), "candidate roots should stay bounded to known locations");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void TestConfiguredValidDiabloPathWinsOverDiscovery()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.DriveTests", Guid.NewGuid().ToString("N"));
    string configuredPath = Path.Combine(root, "Configured", "Diablo III.exe");
    Directory.CreateDirectory(Path.GetDirectoryName(configuredPath)!);
    File.WriteAllText(configuredPath, "configured fake exe");

    try
    {
        AppSettings.SettingsModel model = AppSettings.SettingsModel.Default();
        model.Runtime.DiabloExecutablePath = configuredPath;
        model.Runtime.BattleNetExecutablePath = "";

        AppSettings.RuntimeDiscoveryResult result = AppSettings.DiscoverMissingRuntimePathsForTests(
            model,
            root,
            () => throw new InvalidOperationException("Diablo discovery should not run when configured path is valid."),
            () => "");

        AssertFalse(result.DiabloDiscoveryRan, "valid configured Diablo path should win without running discovery");
        AssertEqual(configuredPath, model.Runtime.DiabloExecutablePath, "configured Diablo path should remain unchanged");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void TestDebugManagerProfileHelpers()
{
    AppSettings.DebugSettings debug = new();
    AssertFalse(debug.EnableSuccessScreenshots, "default config should disable success screenshots");

    DebugManager.ApplyVisualStudioDebugDefaults(debug);

    AssertTrue(debug.DebugMode, "VS defaults should force debug mode in memory");
    AssertTrue(debug.ShowDiagnosticOverlay, "VS defaults should show diagnostics in memory");
    AssertTrue(debug.ShowRouteInspector, "VS defaults should show route inspector in memory");
    AssertTrue(debug.EnableDebugScreenshots, "VS defaults should enable screenshots in memory");
    AssertFalse(debug.EnableSuccessScreenshots, "success screenshots should remain disabled by default even in VS defaults");
    AssertTrue(DebugManager.ShouldSuppressFirstRunSetup(AppSettings.DebugDefaultsProfile.VsDebug), "VS defaults should suppress first-run setup");
    AssertTrue(AppSettings.ShouldRequireFirstRunSetup(AppSettings.DebugDefaultsProfile.VsDebug, requiredRuntimeConfigurationIsValid: false), "VS Debug should still require setup when required paths are invalid");
    AssertFalse(DebugManager.ShouldShowDynamicDebugControls(AppSettings.DebugDefaultsProfile.VsDebug), "VS defaults should hide dynamic debug controls");

    debug = new AppSettings.DebugSettings { EnableSuccessScreenshots = true };
    DebugManager.ApplyVisualStudioDebugDefaults(debug);
    AssertTrue(debug.EnableSuccessScreenshots, "VS defaults should preserve an explicit success screenshot config value");

    debug = new AppSettings.DebugSettings();
    DebugManager.ApplyReleaseUserDefaultsIfPreferenceUnsaved(debug);
    AssertFalse(debug.DebugMode, "normal release should stay quiet by default");
    AssertFalse(debug.EnableDebugScreenshots, "normal release should not capture screenshots by default");
    AssertFalse(debug.EnableSuccessScreenshots, "normal release should not capture success screenshots by default");

    debug = new AppSettings.DebugSettings { EnableSuccessScreenshots = true };
    DebugManager.ApplyReleaseUserDefaultsIfPreferenceUnsaved(debug);
    AssertTrue(debug.EnableSuccessScreenshots, "normal release defaults should preserve an explicit success screenshot config value");

    debug = new AppSettings.DebugSettings();
    DebugManager.ApplyReleaseDebugModePreference(true, debug);
    AssertTrue(debug.DebugModePreferenceSaved, "release debug mode should mark user preference saved");
    AssertTrue(debug.DebugMode, "release debug mode should enable debug mode");
    AssertTrue(debug.EnableDebugScreenshots, "release debug mode should seed screenshots on");
    AssertFalse(debug.EnableSuccessScreenshots, "release debug mode should not seed success screenshots on");
}

static void TestDebugManagerRetentionCleanupFilters()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.Tests", Guid.NewGuid().ToString("N"));
    string sessions = Path.Combine(root, "Sessions");
    string packages = Path.Combine(root, "DebugPackages");
    Directory.CreateDirectory(sessions);
    Directory.CreateDirectory(packages);

    try
    {
        string oldSession = Touch(Path.Combine(sessions, "Session_20260101_000000.md"), -4);
        string keptSession1 = Touch(Path.Combine(sessions, "Session_20260102_000000.md"), -3);
        string keptSession2 = Touch(Path.Combine(sessions, "Session_20260103_000000.md"), -2);
        string unrelatedSession = Touch(Path.Combine(sessions, "Notes_20260101.md"), -5);

        CleanupResult sessionResult = DebugManager.CleanupOldSessionSummaries(sessions, 2);
        AssertEqual(3, sessionResult.Scanned, "session cleanup should scan only Session_*.md");
        AssertEqual(1, sessionResult.Deleted, "session cleanup should delete one old matching file");
        AssertEqual(2, sessionResult.Kept, "session cleanup should keep newest matching files");
        AssertFalse(File.Exists(oldSession), "old matching session summary should be deleted");
        AssertTrue(File.Exists(keptSession1), "newer session summary should be kept");
        AssertTrue(File.Exists(keptSession2), "newest session summary should be kept");
        AssertTrue(File.Exists(unrelatedSession), "unrelated session file should not be deleted");

        string oldPackage = Touch(Path.Combine(packages, "GoblinFarmer_Debug_20260101_000000.zip"), -4);
        string keptPackage = Touch(Path.Combine(packages, "GoblinFarmer_Debug_20260102_000000.zip"), -3);
        string unrelatedZip = Touch(Path.Combine(packages, "Other_Debug_20260101_000000.zip"), -5);

        CleanupResult packageResult = DebugManager.CleanupOldDebugPackages(packages, 1);
        AssertEqual(2, packageResult.Scanned, "package cleanup should scan only GoblinFarmer_Debug_*.zip");
        AssertEqual(1, packageResult.Deleted, "package cleanup should delete one old matching file");
        AssertEqual(1, packageResult.Kept, "package cleanup should keep newest matching package");
        AssertFalse(File.Exists(oldPackage), "old matching debug package should be deleted");
        AssertTrue(File.Exists(keptPackage), "newest matching debug package should be kept");
        AssertTrue(File.Exists(unrelatedZip), "unrelated zip should not be deleted");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    static string Touch(string path, int ageMinutes)
    {
        File.WriteAllText(path, Path.GetFileName(path));
        DateTime timestamp = DateTime.UtcNow.AddMinutes(ageMinutes);
        File.SetCreationTimeUtc(path, timestamp);
        File.SetLastWriteTimeUtc(path, timestamp);
        return path;
    }
}

static void TestDebugManagerAgeRetentionDeletesOldArtifacts()
{
    string repoRoot = FindRepositoryRootForTests();
    string appSettingsSource = File.ReadAllText(Path.Combine(repoRoot, "AppSettings.cs"));
    string programSource = File.ReadAllText(Path.Combine(repoRoot, "Program.cs"));
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.AgeRetentionTests", Guid.NewGuid().ToString("N"));
    string logs = Path.Combine(root, "Logs");
    string review = Path.Combine(root, "Debug", "GoblinReplayReview", "Latest");
    Directory.CreateDirectory(logs);
    Directory.CreateDirectory(review);

    try
    {
        AssertFalse(appSettingsSource.Contains("TimeSpan.FromHours(24)", StringComparison.Ordinal), "VS Debug artifact retention should no longer be limited to 24 hours");
        AssertTrue(appSettingsSource.Contains("DebugArtifactRetentionAge => TimeSpan.FromDays(7)", StringComparison.Ordinal), "VS Debug and Release artifact retention should be 7 days");
        AssertTrue(appSettingsSource.Contains("RetentionDays => 7", StringComparison.Ordinal), "log and screenshot retention should also be 7 days");
        AssertTrue(programSource.Contains("DebugManager.CleanupDebugArtifactsByAge(AppSettings.DebugArtifactRetentionAge)", StringComparison.Ordinal), "startup should apply age-based debug artifact cleanup");

        string oldLog = Touch(Path.Combine(logs, "old.log"), TimeSpan.FromDays(-8));
        string newLog = Touch(Path.Combine(logs, "new.log"), TimeSpan.FromDays(-6));
        string oldReview = Touch(Path.Combine(review, "goblin-tracker-review.html"), TimeSpan.FromDays(-8));
        string newReview = Touch(Path.Combine(review, "goblin-tracker-summary.txt"), TimeSpan.FromMinutes(-10));

        CleanupResult result = DebugManager.CleanupOldFilesByAge(root, TimeSpan.FromDays(7), "test debug artifacts");

        AssertEqual(4, result.Scanned, "age cleanup should scan all debug files recursively");
        AssertEqual(2, result.Deleted, "age cleanup should delete files older than the retention window");
        AssertFalse(File.Exists(oldLog), "old log should be deleted after 7 days");
        AssertTrue(File.Exists(newLog), "new log should be kept inside the 7-day VS Debug window");
        AssertFalse(File.Exists(oldReview), "old loose review file should be deleted after 7 days");
        AssertTrue(File.Exists(newReview), "new loose review file should be kept");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    static string Touch(string path, TimeSpan age)
    {
        File.WriteAllText(path, Path.GetFileName(path));
        DateTime timestamp = DateTime.UtcNow + age;
        File.SetCreationTimeUtc(path, timestamp);
        File.SetLastWriteTimeUtc(path, timestamp);
        return path;
    }
}

static void TestGoblinEvidenceRetentionKeepsNewest250Files()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.GoblinEvidenceTests", Guid.NewGuid().ToString("N"));
    string evidence = Path.Combine(root, "Debug", "GoblinEvidence");
    string calibration = Path.Combine(evidence, "Calibration");
    Directory.CreateDirectory(calibration);

    try
    {
        DateTime baseTime = DateTime.UtcNow.AddHours(-1);
        List<string> paths = [];
        for (int index = 0; index < 255; index++)
        {
            string folder = index % 2 == 0 ? evidence : calibration;
            paths.Add(TouchGoblinEvidenceFile(Path.Combine(folder, $"GoblinEvidence_{index:000}.png"), baseTime.AddMinutes(index)));
        }

        CleanupResult result = DebugManager.CleanupOldGoblinEvidence(evidence, 250);
        AssertEqual(255, result.Scanned, "GoblinEvidence cleanup should scan all files under the evidence folder");
        AssertEqual(5, result.Deleted, "GoblinEvidence cleanup should delete files beyond the retention count");
        AssertEqual(250, Directory.GetFiles(evidence, "*", SearchOption.AllDirectories).Length, "GoblinEvidence cleanup should keep exactly the retention count");

        foreach (string deletedPath in paths.Take(5))
        {
            AssertFalse(File.Exists(deletedPath), "oldest GoblinEvidence files should be deleted");
        }

        foreach (string keptPath in paths.Skip(5))
        {
            AssertTrue(File.Exists(keptPath), "newest GoblinEvidence files should be kept");
        }
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void TestGoblinEvidenceRetentionBreaksTimestampTiesByFilename()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.GoblinEvidenceTests", Guid.NewGuid().ToString("N"));
    string evidence = Path.Combine(root, "Debug", "GoblinEvidence");
    Directory.CreateDirectory(evidence);

    try
    {
        DateTime timestamp = DateTime.UtcNow;
        string alpha = TouchGoblinEvidenceFile(Path.Combine(evidence, "GoblinEvidence_Alpha.png"), timestamp);
        string bravo = TouchGoblinEvidenceFile(Path.Combine(evidence, "GoblinEvidence_Bravo.png"), timestamp);
        string charlie = TouchGoblinEvidenceFile(Path.Combine(evidence, "GoblinEvidence_Charlie.png"), timestamp);

        CleanupResult result = DebugManager.CleanupOldGoblinEvidence(evidence, 2);
        AssertEqual(3, result.Scanned, "GoblinEvidence tie cleanup should scan all tied files");
        AssertEqual(1, result.Deleted, "GoblinEvidence tie cleanup should delete one file beyond retention");
        AssertTrue(File.Exists(alpha), "alphabetically first tied file should be kept");
        AssertTrue(File.Exists(bravo), "alphabetically second tied file should be kept");
        AssertFalse(File.Exists(charlie), "alphabetically later tied file should be deleted beyond retention");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void TestGoblinEvidenceRetentionMissingFolderDoesNotThrow()
{
    string missing = Path.Combine(Path.GetTempPath(), "GoblinFarmer.GoblinEvidenceTests", Guid.NewGuid().ToString("N"), "Debug", "GoblinEvidence");

    CleanupResult result = DebugManager.CleanupOldGoblinEvidence(missing, 250);
    AssertEqual(0, result.Scanned, "missing GoblinEvidence folder should scan zero files");
    AssertEqual(0, result.Deleted, "missing GoblinEvidence folder should delete zero files");
}

static void TestGoblinEvidenceRetentionCountLessThanOneDisablesCleanup()
{
    foreach (int retentionCount in new[] { 0, -1 })
    {
        string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.GoblinEvidenceTests", Guid.NewGuid().ToString("N"));
        string evidence = Path.Combine(root, "Debug", "GoblinEvidence");
        Directory.CreateDirectory(evidence);

        try
        {
            string oldFile = TouchGoblinEvidenceFile(Path.Combine(evidence, "GoblinEvidence_Old.png"), DateTime.UtcNow.AddMinutes(-10));
            string newFile = TouchGoblinEvidenceFile(Path.Combine(evidence, "GoblinEvidence_New.png"), DateTime.UtcNow);

            CleanupResult result = DebugManager.CleanupOldGoblinEvidence(evidence, retentionCount);
            AssertEqual(0, result.Scanned, "disabled GoblinEvidence cleanup should not scan files");
            AssertEqual(0, result.Deleted, "disabled GoblinEvidence cleanup should not delete files");
            AssertTrue(File.Exists(oldFile), "disabled GoblinEvidence cleanup should keep old files");
            AssertTrue(File.Exists(newFile), "disabled GoblinEvidence cleanup should keep new files");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}

static void TestGoblinEvidenceRetentionDeletesOnlyInsideFolder()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.GoblinEvidenceTests", Guid.NewGuid().ToString("N"));
    string evidence = Path.Combine(root, "Debug", "GoblinEvidence");
    string screenshotsFailure = Path.Combine(root, "Screenshots", "Failure");
    string screenshotsSuccess = Path.Combine(root, "Screenshots", "Success");
    string debugScreenshots = Path.Combine(root, "debug-screenshots");
    string logs = Path.Combine(root, "Logs");
    string packages = Path.Combine(root, "DebugPackages");
    Directory.CreateDirectory(evidence);
    Directory.CreateDirectory(screenshotsFailure);
    Directory.CreateDirectory(screenshotsSuccess);
    Directory.CreateDirectory(debugScreenshots);
    Directory.CreateDirectory(logs);
    Directory.CreateDirectory(packages);

    try
    {
        string oldEvidence = TouchGoblinEvidenceFile(Path.Combine(evidence, "GoblinEvidence_Old.png"), DateTime.UtcNow.AddMinutes(-2));
        string keptEvidence = TouchGoblinEvidenceFile(Path.Combine(evidence, "GoblinEvidence_New.png"), DateTime.UtcNow);
        string failure = TouchGoblinEvidenceFile(Path.Combine(screenshotsFailure, "Failure.png"), DateTime.UtcNow.AddMinutes(-10));
        string success = TouchGoblinEvidenceFile(Path.Combine(screenshotsSuccess, "Success.png"), DateTime.UtcNow.AddMinutes(-10));
        string debugShot = TouchGoblinEvidenceFile(Path.Combine(debugScreenshots, "Debug.png"), DateTime.UtcNow.AddMinutes(-10));
        string log = TouchGoblinEvidenceFile(Path.Combine(logs, "GoblinFarmer.log"), DateTime.UtcNow.AddMinutes(-10));
        string package = TouchGoblinEvidenceFile(Path.Combine(packages, "GoblinFarmer_Debug_20260604_120000.zip"), DateTime.UtcNow.AddMinutes(-10));

        CleanupResult result = DebugManager.CleanupOldGoblinEvidence(evidence, 1);
        AssertEqual(2, result.Scanned, "GoblinEvidence cleanup should scan only files inside GoblinEvidence");
        AssertEqual(1, result.Deleted, "GoblinEvidence cleanup should delete only the old evidence file");
        AssertFalse(File.Exists(oldEvidence), "old evidence file should be deleted");
        AssertTrue(File.Exists(keptEvidence), "new evidence file should be kept");
        AssertTrue(File.Exists(failure), "failure screenshots should not be deleted by GoblinEvidence cleanup");
        AssertTrue(File.Exists(success), "success screenshots should not be deleted by GoblinEvidence cleanup");
        AssertTrue(File.Exists(debugShot), "debug-screenshots should not be deleted by GoblinEvidence cleanup");
        AssertTrue(File.Exists(log), "logs should not be deleted by GoblinEvidence cleanup");
        AssertTrue(File.Exists(package), "debug packages should not be deleted by GoblinEvidence cleanup");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void TestGoblinEvidenceTemplateDiscoveryAcceptsPerGoblinEvidenceFiles()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.GoblinEvidenceTemplateTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);

    try
    {
        File.WriteAllText(Path.Combine(root, "Menagerist Goblin Engaged Journal.png"), "fake template for discovery test");
        File.WriteAllText(Path.Combine(root, "Blood Thief Engaged & Killed Journal.png"), "fake template for discovery test");
        File.WriteAllText(Path.Combine(root, "Blood Thief Engaged.png"), "fake template for discovery test");
        File.WriteAllText(Path.Combine(root, "Killed Treasure Goblin Journal.png"), "fake template for discovery test");
        File.WriteAllText(Path.Combine(root, "Gilded Baron Minimap.png"), "fake template for discovery test");
        File.WriteAllText(Path.Combine(root, "Oddius Collector Killed Journal.png"), "fake template for discovery test");
        File.WriteAllText(Path.Combine(root, "Unsupported Evidence.png"), "fake invalid template for discovery test");

        GoblinEvidenceTemplateCatalog catalog = GoblinEvidenceTemplateRequirements.DiscoverTemplates(root);

        AssertEqual(6, catalog.Templates.Count, "valid per-goblin evidence templates should be discovered");
        AssertEqual(1, catalog.InvalidTemplates.Count, "unsupported template names should be reported invalid");
        AssertTrue(catalog.HasJournalTemplates, "journal templates should be detected");
        AssertTrue(catalog.HasMinimapTemplates, "minimap templates should be detected");
        AssertTrue(catalog.Templates.Any(template => template.GoblinType == "Menagerist" && template.Kind == GoblinEvidenceTemplateKind.JournalEngaged), "Menagerist Goblin alias should normalize to Menagerist");
        AssertTrue(catalog.Templates.Any(template => template.GoblinType == "Blood Thief" && template.Kind == GoblinEvidenceTemplateKind.JournalEngagedAndKilled), "combined engaged/killed journal templates should be accepted");
        AssertTrue(catalog.Templates.Any(template => template.GoblinType == "Blood Thief" && template.Kind == GoblinEvidenceTemplateKind.JournalEngaged), "journal engaged templates without the Journal suffix should be accepted");
        AssertTrue(catalog.Templates.Any(template => template.GoblinType == "Treasure Goblin" && template.Kind == GoblinEvidenceTemplateKind.JournalKilled), "prefix-form killed journal templates should be accepted");
        AssertTrue(catalog.Templates.Any(template => template.GoblinType == "Gilded Baron" && template.Kind == GoblinEvidenceTemplateKind.Minimap), "minimap templates should be accepted");
        AssertTrue(catalog.Templates.Any(template => template.GoblinType == "Odious Collector" && template.Kind == GoblinEvidenceTemplateKind.JournalKilled), "Oddius typo should normalize to Odious Collector");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void TestGoblinEvidenceTemplateDiscoveryFindsSourceImageSet()
{
    string scriptPath = FindDebugPackageScript();
    string repoRoot = Directory.GetParent(Path.GetDirectoryName(scriptPath)!)!.FullName;
    string evidenceDirectory = Path.Combine(repoRoot, "Images", "Goblin Evidence");

    GoblinEvidenceTemplateCatalog catalog = GoblinEvidenceTemplateRequirements.DiscoverTemplates(evidenceDirectory);

    AssertTrue(catalog.Templates.Count >= 20, "source Goblin Evidence folder should contain the new per-goblin template set");
    AssertEqual(0, catalog.InvalidTemplates.Count, "source Goblin Evidence template names should all parse cleanly");
    AssertTrue(catalog.HasJournalTemplates, "source Goblin Evidence folder should include journal templates");
    AssertTrue(catalog.HasMinimapTemplates, "source Goblin Evidence folder should include minimap templates");
    AssertEqual(10, catalog.Templates.Count(template => template.Kind == GoblinEvidenceTemplateKind.Minimap), "all 10 updated minimap icon templates should be discovered");
    AssertTrue(catalog.Templates.Any(template => template.GoblinType == "Blood Thief" && template.Kind == GoblinEvidenceTemplateKind.JournalEngaged), "source templates should accept Blood Thief engaged journal evidence");
    AssertTrue(catalog.Templates.Any(template => template.GoblinType == "Blood Thief" && template.Kind == GoblinEvidenceTemplateKind.JournalKilled), "source templates should accept Blood Thief killed journal evidence");
    AssertTrue(catalog.Templates.Any(template => template.GoblinType == "Gilded Baron" && template.Kind == GoblinEvidenceTemplateKind.Minimap), "source templates should include Gilded Baron minimap evidence");
    AssertTrue(catalog.Templates.Any(template => template.GoblinType == "Menagerist" && template.Kind == GoblinEvidenceTemplateKind.Minimap), "source templates should include Menagerist minimap evidence");
}

static void TestGoblinMinimapColorDisambiguatesTreasureAndOdious()
{
    string repoRoot = FindRepositoryRootForTests();
    string evidenceSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs"));
    string evidenceModelSource = File.ReadAllText(Path.Combine(repoRoot, "GoblinEvidence.cs"));

    AssertTrue(evidenceModelSource.Contains("GoblinMinimapColorClassification", StringComparison.Ordinal), "minimap template matches should carry color classification data");
    AssertTrue(evidenceSource.Contains("PortApplyMinimapColorDisambiguation(bestTemplate, bestMatch)", StringComparison.Ordinal), "minimap candidates should apply color disambiguation before creating the observation candidate");
    AssertTrue(evidenceSource.Contains("GoblinEvidenceMinimapColorOverride", StringComparison.Ordinal), "Treasure/Odious minimap color overrides should be logged");
    AssertTrue(evidenceSource.Contains("PortGoblinTypeUsesTreasureOdiousMinimapColor", StringComparison.Ordinal), "color override should be scoped to the Treasure/Odious pair");
    AssertTrue(evidenceSource.Contains("return \"Treasure Goblin\"", StringComparison.Ordinal), "yellow minimap matches should classify as Treasure Goblin");
    AssertTrue(evidenceSource.Contains("return \"Odious Collector\"", StringComparison.Ordinal), "green minimap matches should classify as Odious Collector");
    AssertTrue(evidenceSource.Contains("MinimapYellowPixels", StringComparison.Ordinal), "candidate notes should include minimap yellow pixel diagnostics");
    AssertTrue(evidenceSource.Contains("MinimapGreenPixels", StringComparison.Ordinal), "candidate notes should include minimap green pixel diagnostics");
}

static void TestGoblinMinimapColorDisambiguatesGildedAndMalevolent()
{
    string repoRoot = FindRepositoryRootForTests();
    string evidenceSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs"));
    string evidenceModelSource = File.ReadAllText(Path.Combine(repoRoot, "GoblinEvidence.cs"));

    AssertTrue(evidenceModelSource.Contains("OrangePixels", StringComparison.Ordinal), "minimap color diagnostics should track orange pixels for Malevolent Tormentor");
    AssertTrue(evidenceSource.Contains("PortGoblinTypeUsesGildedMalevolentMinimapColor", StringComparison.Ordinal), "color override should be scoped to the Gilded/Malevolent pair");
    AssertTrue(evidenceSource.Contains("return \"Gilded Baron\"", StringComparison.Ordinal), "yellow-dominant Gilded/Malevolent patches should classify as Gilded Baron");
    AssertTrue(evidenceSource.Contains("return \"Malevolent Tormentor\"", StringComparison.Ordinal), "orange-dominant Gilded/Malevolent patches should classify as Malevolent Tormentor");
    AssertTrue(evidenceSource.Contains("MinimapOrangePixels", StringComparison.Ordinal), "candidate notes should include minimap orange pixel diagnostics");
}

static void TestGoblinAutomaticMinimapCountsRequireStrongConfidence()
{
    string repoRoot = FindRepositoryRootForTests();
    string sessionSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.cs"));
    string evidenceModelSource = File.ReadAllText(Path.Combine(repoRoot, "GoblinEvidence.cs"));

    AssertTrue(evidenceModelSource.Contains("EvidenceConfidence", StringComparison.Ordinal), "observation records should carry evidence confidence into auto-count decisions");
    AssertTrue(sessionSource.Contains("PortAutomaticGoblinMinimapCountMinimumConfidence = 0.85", StringComparison.Ordinal), "normal automatic minimap counts should accept strong evidence below the ambiguity-pair gate");
    AssertTrue(sessionSource.Contains("PortAutomaticGoblinAmbiguousMinimapCountMinimumConfidence = 0.90", StringComparison.Ordinal), "Gilded/Malevolent automatic minimap counts should keep the stricter ambiguity-pair gate");
    AssertTrue(sessionSource.Contains("PortAutomaticGoblinMinimapCountMinimumConfidenceFor", StringComparison.Ordinal), "automatic minimap counts should use a goblin-type-specific confidence gate");
    AssertTrue(sessionSource.Contains("MinimapConfidencePendingJournal", StringComparison.Ordinal), "low-confidence minimap auto-count attempts should suppress and wait for stronger evidence");
    AssertTrue(sessionSource.Contains("minimapAutoCountMinConfidence", StringComparison.Ordinal), "auto-count diagnostics should report the minimap confidence gate");
}

static void TestGoblinJournalFreshnessStaysAreaStrictAcrossCavernsLevels()
{
    DateTime now = DateTime.UtcNow;
    GoblinJournalKilledState killedState = new(
        "Treasure Goblin",
        "Caverns of Frost Level 1",
        now - TimeSpan.FromSeconds(30),
        now);

    AssertFalse(
        GoblinJournalFreshnessPolicy.KilledIsFresh(
            killedState,
            "Caverns of Frost Level 2",
            now,
            TimeSpan.FromSeconds(45)),
        "killed journal evidence first seen in Caverns Level 1 should not become fresh Level 2 evidence after a level transition");

    AssertFalse(
        GoblinJournalFreshnessPolicy.EngagedIsFresh(
            now - TimeSpan.FromSeconds(12),
            "Cave Of The Moon Clan Level 1",
            "Cave Of The Moon Clan Level 2",
            now,
            TimeSpan.FromSeconds(45)),
        "fresh-looking Engaged journal evidence first seen on Cave Level 1 should not become fresh Level 2 evidence after a level transition");

    AssertTrue(
        GoblinJournalFreshnessPolicy.EngagedIsFresh(
            now - TimeSpan.FromSeconds(12),
            "Cave Of The Moon Clan Level 2",
            "Cave Of The Moon Clan Level 2",
            now,
            TimeSpan.FromSeconds(45)),
        "same-area Engaged journal evidence inside the freshness window should remain eligible");
}

static void TestGoblinEvidenceObservationScanRegionsMatchCalibration()
{
    AssertEqual(new Rectangle(64, 736, 645, 417), GoblinEvidenceScanRegions.JournalReferenceRegion, "journal observation scan region should match the calibrated GoblinEvidence journal region");
    AssertEqual(new Rectangle(2108, 66, 421, 423), GoblinEvidenceScanRegions.MinimapReferenceRegion, "minimap observation scan region should match the calibrated GoblinEvidence minimap region");
}

static string TouchGoblinEvidenceFile(string path, DateTime lastWriteTimeUtc)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, Path.GetFileName(path));
    File.SetCreationTimeUtc(path, lastWriteTimeUtc);
    File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);
    return path;
}

static void CreateRequiredImagesFolders(string imagesRoot)
{
    foreach (string folder in new[] { "Combat", "Current Location", "Goblin Evidence", "Leave Game", "Repair", "Salvage", "Start Game", "Teleport Function" })
    {
        Directory.CreateDirectory(Path.Combine(imagesRoot, folder));
    }
}

static string EscapeJsonPath(string path)
{
    return path.Replace(@"\", @"\\");
}

static void TestReleaseProfileRequiresSetupWhenMissingPaths()
{
    AppSettings.LaunchProfileSnapshot snapshot = AppSettings.ResolveLaunchProfileForTests(
        explicitProfile: null,
        debuggerAttached: false,
        debugBuild: false,
        explicitConfigPath: null,
        baseDirectory: @"C:\Program Files\GoblinFarmer");

    AssertEqual(AppSettings.DebugDefaultsProfile.ReleaseUser, snapshot.Profile, "installed launch should use release-user profile");
    AssertFalse(snapshot.FirstRunSetupSuppressed, "installed launch should not suppress first-run setup");
    AssertTrue(snapshot.DynamicDebugControlsVisible, "installed launch can show dynamic debug controls");
    AssertTrue(AppSettings.ShouldRequireFirstRunSetup(snapshot.Profile, requiredRuntimeConfigurationIsValid: false), "installed launch with missing paths should require setup");
}

static void TestReleaseGoblinTrackerLayoutKeepsObservationFieldsSeparated()
{
    string repoRoot = FindRepositoryRootForTests();
    string designerSource = File.ReadAllText(Path.Combine(repoRoot, "Form1.Designer.cs"));
    string releaseSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.Release.cs"));

    AssertTrue(designerSource.Contains("grpGoblinTracker.Size = new Size(311, 304)", StringComparison.Ordinal), "base Release layout should give Goblin Tracker enough height for all labels");
    AssertTrue(designerSource.Contains("lblGoblinActiveTime.Location = new Point(12, 72)", StringComparison.Ordinal), "Active Time should be below GPH without overlapping evidence labels");
    AssertTrue(designerSource.Contains("lblGoblinEvidenceLast.Location = new Point(12, 108)", StringComparison.Ordinal), "Last Evidence should be separated below core tracker stats");
    AssertTrue(designerSource.Contains("lblGoblinObservation.Location = new Point(12, 212)", StringComparison.Ordinal), "Last Observation should start below evidence fields");
    AssertTrue(designerSource.Contains("lblGoblinObservation.Size = new Size(287, 84)", StringComparison.Ordinal), "Last Observation should have enough height for five lines");
    AssertTrue(designerSource.Contains("ClientSize = new Size(918, 836)", StringComparison.Ordinal), "base Release form should be tall enough for the expanded Goblin Tracker group");
    AssertTrue(releaseSource.Contains("ClientSize = new Size(918, 836)", StringComparison.Ordinal), "Release Debug Mode off reset should not shrink back to the overlapping layout");
}

static void TestBattleNetSuccessfulLaunchDiagnosticsAvoidFailureScreenshots()
{
    string repoRoot = FindRepositoryRootForTests();
    string formSource = File.ReadAllText(Path.Combine(repoRoot, "Form1.cs"));
    string packageScript = File.ReadAllText(Path.Combine(repoRoot, "Scripts", "create-debug-package.ps1"));
    string recordMethod = ExtractMethodBody(formSource, "private void PortRecordDiabloLaunchAfterBattleNet");
    string stillOpenMethod = ExtractMethodBody(formSource, "private void PortLogBattleNetStillOpenAfterDiabloLaunch");
    int failureTypesStart = packageScript.IndexOf("$failureTypes = @(", StringComparison.Ordinal);
    int failureTypesEnd = packageScript.IndexOf("foreach ($failureType in $failureTypes)", StringComparison.Ordinal);
    AssertTrue(failureTypesStart >= 0 && failureTypesEnd > failureTypesStart, "debug package failure type list should be present");
    string failureTypesSource = packageScript[failureTypesStart..failureTypesEnd];

    AssertTrue(recordMethod.Contains("PortRecordBattleNetPlayClickAccepted(\"Diablo process detected after app Play click\"", StringComparison.Ordinal), "Diablo appearing after an app Play click should be reconciled as app-click acceptance before launch outcome logging");
    AssertTrue(recordMethod.Contains("CaptureDebugScreenshot(\"BattleNetLaunch\", \"BattleNetManualPlaySuspected\")", StringComparison.Ordinal), "manual-play suspicion after Diablo launches should be diagnostic-only");
    AssertFalse(recordMethod.Contains("PortCaptureFailureScreenshot(\"BattleNetManualPlaySuspected\"", StringComparison.Ordinal), "manual-play suspicion during a successful launch should not create failure screenshot pairs");
    AssertTrue(stillOpenMethod.Contains("bool successfulAppClickLaunch = battleNetPlayClickAcceptedByBattleNet && diabloLaunchedAfterAppPlayClick", StringComparison.Ordinal), "Battle.net still-open handling should distinguish successful app-click launches");
    AssertTrue(stillOpenMethod.Contains("? \"None\"", StringComparison.Ordinal), "successful app-click launches should not capture still-open screenshots");
    AssertTrue(stillOpenMethod.Contains(": CaptureDebugScreenshot(\"BattleNetLaunch\", \"BattleNetStillOpenAfterDiabloLaunch\")", StringComparison.Ordinal), "non-app-click/manual launch diagnostics may still capture still-open debug evidence");
    AssertTrue(stillOpenMethod.Contains("screenshotCaptured={screenshotCaptured}", StringComparison.Ordinal), "still-open logs should state whether screenshot evidence was captured");
    AssertFalse(stillOpenMethod.Contains("PortCaptureFailureScreenshot(\"BattleNetStillOpenAfterDiabloLaunch\"", StringComparison.Ordinal), "Battle.net still-open-after-launch should not be packaged as a failure when close handling can succeed");
    AssertFalse(failureTypesSource.Contains("BattleNetPlayClickAccepted", StringComparison.Ordinal), "successful Battle.net Play acceptance should not be classified as a failure screenshot");
    AssertFalse(failureTypesSource.Contains("BattleNetManualPlaySuspected", StringComparison.Ordinal), "manual-play diagnostics should not be classified as failure screenshots by package defaults");
    AssertFalse(failureTypesSource.Contains("BattleNetStillOpenAfterDiabloLaunch", StringComparison.Ordinal), "still-open diagnostics should not be classified as failure screenshots by package defaults");
    AssertTrue(failureTypesSource.Contains("BattleNetPlayButtonNotClickedByApp", StringComparison.Ordinal), "real Battle.net app-click failures should remain classified as failure screenshots");
}

static void TestExplicitAppSettingsPathOverrideWins()
{
    string explicitPath = Path.Combine(Path.GetTempPath(), "GoblinFarmer.Tests", "CustomAppSettings.json");
    AppSettings.LaunchProfileSnapshot snapshot = AppSettings.ResolveLaunchProfileForTests(
        explicitProfile: null,
        debuggerAttached: true,
        debugBuild: true,
        explicitConfigPath: explicitPath,
        baseDirectory: @"C:\dev\GoblinFarmer\bin\Debug\net10.0-windows");

    AssertEqual(Path.GetFullPath(explicitPath), snapshot.ConfigPath, "explicit AppSettings path should win even for VS/dev profile");
    AssertFalse(snapshot.VsDebugProjectRootConfigUsed, "explicit AppSettings path should not be reported as project-root config");
}

static void TestAppSettingsMigrationPreservesRuntimePaths()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.ConfigTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);

    try
    {
        string existingConfigPath = Path.Combine(root, "AppSettings.json");
        File.WriteAllText(existingConfigPath, """
            {
              "Runtime": {
                "DiabloExecutablePath": "D:\\Games\\Diablo III\\Diablo III64.exe",
                "BattleNetExecutablePath": "D:\\Games\\Battle.net\\Battle.net.exe",
                "ImagesRoot": "D:\\D3\\Projects\\GoblinFarmer\\Images"
              },
              "Debug": {
                "EnableDebugScreenshots": false
              }
            }
            """);

        AppSettings.SettingsModel migrated = AppSettings.SettingsModel.Default();
        migrated.Runtime.DiabloExecutablePath = "";
        migrated.Runtime.BattleNetExecutablePath = "";
        migrated.Runtime.ImagesRoot = "Images";
        migrated.Debug.EnableSuccessScreenshots = AppSettings.DebugSettings.DefaultEnableSuccessScreenshots;

        AppSettings.RuntimePathPreservationResult result = AppSettings.PreserveRuntimePathValuesForTests(migrated, existingConfigPath);

        AssertTrue(result.DiabloPathPreserved, "migration should preserve existing Diablo path");
        AssertTrue(result.BattleNetPathPreserved, "migration should preserve existing Battle.net path");
        AssertTrue(result.ImagesRootPreserved, "migration should preserve existing custom Images root");
        AssertEqual(@"D:\Games\Diablo III\Diablo III64.exe", migrated.Runtime.DiabloExecutablePath, "Diablo path should survive default merge");
        AssertEqual(@"D:\Games\Battle.net\Battle.net.exe", migrated.Runtime.BattleNetExecutablePath, "Battle.net path should survive default merge");
        AssertEqual(@"D:\D3\Projects\GoblinFarmer\Images", migrated.Runtime.ImagesRoot, "Images root should survive default merge");
    }
    finally
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

static void TestDemonHunterNoClickSuppressionDiagnosticName()
{
    AssertEqual("Diagnostic", CombatDiagnosticNames.DemonHunterNoClickSuppressionOutcome, "diagnostic outcome should not be Failure");
    AssertEqual("Combat", CombatDiagnosticNames.DemonHunterNoClickSuppressionWorkflow, "diagnostic workflow should stay Combat");
    AssertEqual("DemonHunterNoClickSuppressionActive", CombatDiagnosticNames.DemonHunterNoClickSuppressionAction, "diagnostic action should describe active suppression");
    AssertEqual("Diagnostic_Combat_DemonHunterNoClickSuppressionActive", CombatDiagnosticNames.DemonHunterNoClickSuppressionEvent, "diagnostic event should use the new package/file prefix");
    AssertEqual("Diagnostic_Combat_DemonHunterNoClickSuppressionActive", CombatDiagnosticNames.DemonHunterNoClickSuppressionScreenshotPrefix, "diagnostic screenshot prefix should use the new package/file prefix");
    AssertFalse(CombatDiagnosticNames.DemonHunterNoClickSuppressionEvent.Contains("Failure", StringComparison.OrdinalIgnoreCase), "diagnostic event should not be labeled failure");
    AssertFalse(CombatDiagnosticNames.DemonHunterNoClickSuppressionEvent.Contains("Stall", StringComparison.OrdinalIgnoreCase), "diagnostic event should not be labeled stall");
}

static void TestWitchDoctorCombatUsesMouseWheelNotHeldLeftMode()
{
    string repoRoot = FindRepositoryRootForTests();
    string combatSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.Combat.cs"));
    string stateSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.PortedAutomation.cs.cs"));
    int witchDoctorLoopStart = combatSource.IndexOf("private void PortWitchDoctorCursorLeftClickLoop", StringComparison.Ordinal);
    int witchDoctorLoopEnd = combatSource.IndexOf("private bool PortCombatShouldContinue", StringComparison.Ordinal);
    AssertTrue(witchDoctorLoopStart >= 0 && witchDoctorLoopEnd > witchDoctorLoopStart, "Witch Doctor cursor-change left-click loop should be present");
    string witchDoctorCursorLoopSource = combatSource[witchDoctorLoopStart..witchDoctorLoopEnd];
    int witchDoctorStartBranchStart = combatSource.IndexOf("if (portCombatClass == \"witch_doctor\")", StringComparison.Ordinal);
    int witchDoctorStartBranchEnd = combatSource.IndexOf("else if (portCombatClass == \"demon_hunter\")", StringComparison.Ordinal);
    AssertTrue(witchDoctorStartBranchStart >= 0 && witchDoctorStartBranchEnd > witchDoctorStartBranchStart, "Witch Doctor combat startup branch should be present");
    string witchDoctorStartBranchSource = combatSource[witchDoctorStartBranchStart..witchDoctorStartBranchEnd];

    AssertTrue(combatSource.Contains("PortWitchDoctorMouseWheelLoop", StringComparison.Ordinal), "Witch Doctor should run a dedicated mouse wheel loop");
    AssertTrue(combatSource.Contains("PortWitchDoctorCursorLeftClickLoop", StringComparison.Ordinal), "Witch Doctor should run a dedicated cursor-change left-click loop");
    AssertTrue(witchDoctorStartBranchSource.Contains("PortRunCombatTask(\"Witch Doctor loop\"", StringComparison.Ordinal), "Witch Doctor startup should launch the key loop");
    AssertTrue(witchDoctorStartBranchSource.Contains("PortRunCombatTask(\"Witch Doctor mouse wheel loop\"", StringComparison.Ordinal), "Witch Doctor startup should launch the mouse wheel loop");
    AssertTrue(witchDoctorStartBranchSource.Contains("PortRunCombatTask(\"Witch Doctor cursor left click loop\"", StringComparison.Ordinal), "Witch Doctor startup should launch the cursor-change left-click loop");
    AssertTrue(combatSource.Contains("PortRuntimeMouseWheel(-120)", StringComparison.Ordinal), "Witch Doctor should repeatedly send mouse wheel input");
    AssertTrue(witchDoctorCursorLoopSource.Contains("PortRuntimeMouseDown(MOUSEEVENTF_LEFTDOWN)", StringComparison.Ordinal), "Witch Doctor cursor-change input should send a left-click down pulse");
    AssertTrue(witchDoctorCursorLoopSource.Contains("PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP)", StringComparison.Ordinal), "Witch Doctor cursor-change input should send a left-click up pulse");
    AssertTrue(witchDoctorCursorLoopSource.Contains("PortCombatCursorShouldSendClick", StringComparison.Ordinal), "Witch Doctor cursor-change input should use the shared cursor-change gate");
    AssertTrue(combatSource.Contains("combatInputMode=MouseWheelScroll", StringComparison.Ordinal), "Witch Doctor logs should report mouse wheel input mode");
    AssertTrue(combatSource.Contains("WitchDoctorCursorChangeLeftClickLoopStarted", StringComparison.Ordinal), "Witch Doctor should log cursor-change left-click loop startup");
    AssertTrue(combatSource.Contains("WitchDoctorCursorChangeLeftClickCheck", StringComparison.Ordinal), "Witch Doctor should log cursor-change checks");
    AssertTrue(combatSource.Contains("WitchDoctorCursorChangeLeftClickSent", StringComparison.Ordinal), "Witch Doctor should log sent cursor-change left-click pulses");
    AssertTrue(combatSource.Contains("WitchDoctorCursorChangeLeftClickSkipped", StringComparison.Ordinal), "Witch Doctor should log skipped cursor-change left-click pulses");
    AssertTrue(combatSource.Contains("keyOrder=2,3,1", StringComparison.Ordinal), "Witch Doctor key loop order should remain 2, 3, 1");
    AssertTrue(combatSource.Contains("heldLeftMode=false", StringComparison.Ordinal), "Witch Doctor logs should explicitly report no held-left mode");
    AssertTrue(combatSource.Contains("heldRightMode=false", StringComparison.Ordinal), "Witch Doctor logs should explicitly report no held-right mode");
    AssertTrue(combatSource.Contains("cursorChanged=", StringComparison.Ordinal), "Witch Doctor left-click logs should include cursor change state");
    AssertTrue(combatSource.Contains("LEFTDOWN,LEFTUP", StringComparison.Ordinal), "Witch Doctor sent logs should identify the discrete left-click pulse");
    AssertTrue(stateSource.Contains("PortRuntimeMouseWheel", StringComparison.Ordinal), "runtime input helpers should include mouse wheel support");

    AssertFalse(combatSource.Contains("PortHandleWitchDoctorCursorInput", StringComparison.Ordinal), "Witch Doctor should not use the old cursor-held input handler");
    AssertFalse(combatSource.Contains("WitchDoctorHeldInput", StringComparison.Ordinal), "Witch Doctor should not log held-left input state");
    AssertFalse(combatSource.Contains("WitchDoctorScrollHeldFromSafeRegion", StringComparison.Ordinal), "Witch Doctor should not enter held-from-safe-region mode");
    AssertFalse(combatSource.Contains("WitchDoctorCursorChangeRightClick", StringComparison.Ordinal), "Witch Doctor should not log cursor-change right-click pulses");
    AssertFalse(combatSource.Contains("PortWitchDoctorCursorRightClickLoop", StringComparison.Ordinal), "Witch Doctor should not run a cursor-change right-click loop");
    AssertFalse(witchDoctorCursorLoopSource.Contains("PortRuntimeMouseDown(MOUSEEVENTF_RIGHTDOWN)", StringComparison.Ordinal), "Witch Doctor cursor-change input should not send right-click down");
    AssertFalse(witchDoctorCursorLoopSource.Contains("PortRuntimeMouseUp(MOUSEEVENTF_RIGHTUP)", StringComparison.Ordinal), "Witch Doctor cursor-change input should not send right-click up");
    AssertFalse(stateSource.Contains("portWitchDoctorHeldInputFromSafeRegion", StringComparison.Ordinal), "Witch Doctor should not track a held-left safe-region state");
}

static string FindRepositoryRootForTests()
{
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory != null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "frmMain.Combat.cs")) &&
            File.Exists(Path.Combine(directory.FullName, "GoblinFarmer.csproj")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate GoblinFarmer source root from test output directory.");
}

static string ExtractMethodBody(string source, string methodName)
{
    int methodIndex = source.IndexOf(methodName, StringComparison.Ordinal);
    if (methodIndex < 0)
    {
        throw new InvalidOperationException($"Could not find method {methodName}.");
    }

    int openBrace = source.IndexOf('{', methodIndex);
    if (openBrace < 0)
    {
        throw new InvalidOperationException($"Could not find body for method {methodName}.");
    }

    int depth = 0;
    for (int i = openBrace; i < source.Length; i++)
    {
        if (source[i] == '{')
        {
            depth++;
        }
        else if (source[i] == '}')
        {
            depth--;
            if (depth == 0)
            {
                return source.Substring(openBrace, i - openBrace + 1);
            }
        }
    }

    throw new InvalidOperationException($"Could not extract body for method {methodName}.");
}

static void TestStartGameClickPolicyBlocksInGameSignals()
{
    StartGameClickReadiness leaveVisible = StartGameClickPolicy.Evaluate(new StartGameClickState(
        StableStartGameVisible: true,
        LeaveGameVisible: true,
        CharacterLoadVisible: false,
        LoadedLocationVisible: false,
        PlayerInGameVisible: false,
        CurrentLocationVisible: false,
        StableElapsedMs: StartGameClickPolicy.RequiredStableDurationMs));
    AssertEqual(StartGameClickDecision.BlockAlreadyInGame, leaveVisible.Decision, "Leave Game visible should block Start Game clicks");
    AssertFalse(leaveVisible.MainMenuConfirmed, "Leave Game visible should not confirm the main menu");

    StartGameClickReadiness locationVisible = StartGameClickPolicy.Evaluate(new StartGameClickState(
        StableStartGameVisible: true,
        LeaveGameVisible: false,
        CharacterLoadVisible: false,
        LoadedLocationVisible: true,
        PlayerInGameVisible: false,
        CurrentLocationVisible: true,
        StableElapsedMs: StartGameClickPolicy.RequiredStableDurationMs));
    AssertEqual(StartGameClickDecision.BlockAlreadyInGame, locationVisible.Decision, "location/player-state signals should block Start Game clicks");

    StartGameClickReadiness tooFast = StartGameClickPolicy.Evaluate(new StartGameClickState(
        StableStartGameVisible: true,
        LeaveGameVisible: false,
        CharacterLoadVisible: false,
        LoadedLocationVisible: false,
        PlayerInGameVisible: false,
        CurrentLocationVisible: false,
        StableElapsedMs: StartGameClickPolicy.RequiredStableDurationMs - 1));
    AssertEqual(StartGameClickDecision.BlockMainMenuNotConfirmed, tooFast.Decision, "short-lived Start Game matches should not be clickable");

    StartGameClickReadiness allowed = StartGameClickPolicy.Evaluate(new StartGameClickState(
        StableStartGameVisible: true,
        LeaveGameVisible: false,
        CharacterLoadVisible: false,
        LoadedLocationVisible: false,
        PlayerInGameVisible: false,
        CurrentLocationVisible: false,
        StableElapsedMs: StartGameClickPolicy.RequiredStableDurationMs));
    AssertEqual(StartGameClickDecision.Allow, allowed.Decision, "stable Start Game with no in-game signals should be allowed");
    AssertTrue(allowed.MainMenuConfirmed, "allowed Start Game click should confirm main menu");
}

static void TestGoblinJournalParserCountsEscapedEncounters()
{
    IReadOnlyList<GoblinJournalEvent> events = GoblinJournalParser.ParseEvents("""
        Someone has killed a <Goblin>
        A <Goblin> has escaped!
        A Rainbow Goblin has escaped!
        """);

    AssertEqual(3, events.Count, "killed and escaped goblin journal lines should count as encounters");
    AssertEqual(GoblinJournalEventKind.Killed, events[0].Kind, "killed line should parse as kill");
    AssertEqual("Goblin", events[0].GoblinType, "generic goblin kill should normalize");
    AssertEqual(GoblinJournalEventKind.Escaped, events[1].Kind, "escaped generic goblin should parse as escaped");
    AssertEqual("Goblin", events[1].GoblinType, "generic escaped goblin should normalize");
    AssertEqual("Rainbow Goblin", events[2].GoblinType, "specific escaped goblin should normalize");
}

static void TestGelatinousSpawnNormalizesToSire()
{
    AssertEqual("Gelatinous Sire", GoblinTypeNormalizer.Normalize("Gelatinous Spawn"), "spawn alias should normalize to sire");
    GoblinJournalEvent? parsed = GoblinJournalParser.ParseLine("Player has killed a Gelatinous Spawn");
    AssertTrue(parsed != null, "Gelatinous Spawn kill should parse");
    AssertEqual("Gelatinous Sire", parsed!.GoblinType, "Gelatinous Spawn journal kill should count as Gelatinous Sire");
}

static void TestDebugPackageExcludesSuccessScreenshotsByDefault()
{
    string testRoot = Path.Combine(Path.GetTempPath(), "GoblinFarmer.PackageTests", Guid.NewGuid().ToString("N"));
    string screenshots = Path.Combine(testRoot, "Screenshots");
    string logs = Path.Combine(testRoot, "Logs");
    string config = Path.Combine(testRoot, "Config");
    string goblinEvidence = Path.Combine(testRoot, "Debug", "GoblinEvidence", "Calibration");
    Directory.CreateDirectory(screenshots);
    Directory.CreateDirectory(logs);
    Directory.CreateDirectory(config);
    Directory.CreateDirectory(goblinEvidence);

    try
    {
        DateTime sessionStart = DateTime.Now.AddMinutes(-5);
        File.WriteAllLines(Path.Combine(testRoot, "session-info.txt"),
        [
            $"SessionStartLocal={sessionStart:O}",
            $"SessionStartUtc={sessionStart.ToUniversalTime():O}",
            "GoblinCount=0",
            "GoblinObservationCount=3",
            "JournalObservationCount=2",
            "MinimapObservationCount=1",
            "EligibleObservationCount=1",
            "BlockedObservationCount=1",
            "DuplicateObservationCount=1",
            "LastGoblinObservationSource=Journal",
            "LastGoblinObservationType=Rainbow Goblin",
            "LastGoblinObservationAreaKey=The Weeping Hollow",
            "LastGoblinObservationWouldCount=True",
            "LastGoblinObservationReason=Eligible",
        ]);
        File.WriteAllText(Path.Combine(logs, "GoblinFarmer.log"), "test log");
        File.WriteAllText(Path.Combine(config, "AppSettings.json"), """
            {
              "Debug": {
                "EnableSuccessScreenshots": true
              }
            }
            """);

        Touch(Path.Combine(screenshots, "2026-06-04_120000_000_Success_StartGame_StartGameClicked_Diablo.png"));
        Touch(Path.Combine(screenshots, "2026-06-04_120000_000_Success_StartGame_StartGameClicked_App.png"));
        Touch(Path.Combine(screenshots, "2026-06-04_120001_000_Failure_StartGame_StartGameVerificationFailed_Diablo.png"));
        Touch(Path.Combine(screenshots, "2026-06-04_120001_000_Failure_StartGame_StartGameVerificationFailed_App.png"));
        Touch(Path.Combine(goblinEvidence, "GoblinCalibration_20260604_120000_Full.png"));
        Touch(Path.Combine(goblinEvidence, "GoblinCalibration_20260604_120000_Minimap.png"));
        Touch(Path.Combine(goblinEvidence, "GoblinCalibration_20260604_120000_Metadata.txt"));

        string scriptPath = FindDebugPackageScript();
        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -RuntimeRoot \"{testRoot}\" -MaxFailureScreenshots 5 -MaxDiagnosticScreenshots 1",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException("Could not start debug package script");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit(60000);
        AssertEqual(0, process.ExitCode, $"debug package script should succeed. stdout={output}; stderr={error}");

        string packagePath = Directory.GetFiles(Path.Combine(testRoot, "DebugPackages"), "GoblinFarmer_Debug_*.zip")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault() ?? "";
        AssertTrue(File.Exists(packagePath), "debug package zip should be created");

        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        AssertFalse(archive.Entries.Any(entry => entry.FullName.Contains("Screenshots/Success", StringComparison.OrdinalIgnoreCase) || entry.FullName.Contains("Screenshots\\Success", StringComparison.OrdinalIgnoreCase)), "success screenshots should be excluded by default");
        AssertFalse(archive.Entries.Any(entry => entry.FullName.Contains("_Success_", StringComparison.OrdinalIgnoreCase)), "success-category screenshots should not be included in any package folder by default");
        AssertTrue(archive.Entries.Any(entry => entry.FullName.Contains("Screenshots/Failure", StringComparison.OrdinalIgnoreCase) || entry.FullName.Contains("Screenshots\\Failure", StringComparison.OrdinalIgnoreCase)), "failure screenshots should remain included");
        AssertFalse(archive.Entries.Any(entry => entry.FullName.EndsWith("_Full.png", StringComparison.OrdinalIgnoreCase)), "GoblinEvidence full images should remain excluded by default");
        AssertTrue(archive.Entries.Any(entry =>
            (entry.FullName.Contains("Debug/GoblinEvidence", StringComparison.OrdinalIgnoreCase) ||
                entry.FullName.Contains("Debug\\GoblinEvidence", StringComparison.OrdinalIgnoreCase)) &&
            entry.FullName.EndsWith("_Minimap.png", StringComparison.OrdinalIgnoreCase)), "GoblinEvidence cropped images should remain included");

        ZipArchiveEntry manifest = archive.GetEntry("debug-package-manifest.txt") ?? throw new InvalidOperationException("manifest missing from debug package");
        using StreamReader reader = new(manifest.Open());
        string manifestText = reader.ReadToEnd();
        AssertTrue(manifestText.Contains("Success screenshot package policy: Skipped by default", StringComparison.OrdinalIgnoreCase), "manifest should document success screenshot exclusion policy");
        AssertTrue(manifestText.Contains("Debug.EnableSuccessScreenshots: True", StringComparison.OrdinalIgnoreCase), "manifest should report enabled success screenshot setting");
        AssertTrue(manifestText.Contains("Success screenshots available but excluded by default: count=2", StringComparison.OrdinalIgnoreCase), "manifest should report available success screenshot count without including them");
        AssertTrue(manifestText.Contains("Current GoblinEvidence source files: count=3", StringComparison.OrdinalIgnoreCase), "manifest should report current GoblinEvidence source count");
        AssertTrue(manifestText.Contains("- Debug/GoblinEvidence: 2 files", StringComparison.OrdinalIgnoreCase), "manifest package folder totals should report included GoblinEvidence files");
        AssertTrue(manifestText.Contains("- Debug/GoblinEvidence current source: 3 files", StringComparison.OrdinalIgnoreCase), "manifest folder totals should report current GoblinEvidence source totals");
        AssertTrue(manifestText.Contains("- Goblin evidence full images excluded: 1", StringComparison.OrdinalIgnoreCase), "manifest should report excluded GoblinEvidence full images");
        AssertTrue(manifestText.Contains("Goblin observations: 3", StringComparison.OrdinalIgnoreCase), "manifest should report Goblin observation count");
        AssertTrue(manifestText.Contains("Goblin journal observations: 2", StringComparison.OrdinalIgnoreCase), "manifest should report Journal observation count");
        AssertTrue(manifestText.Contains("Goblin minimap observations: 1", StringComparison.OrdinalIgnoreCase), "manifest should report Minimap observation count");
        AssertTrue(manifestText.Contains("Goblin duplicate observations: 1", StringComparison.OrdinalIgnoreCase), "manifest should report duplicate observation count");
        AssertTrue(manifestText.Contains("Goblin last observation: source=Journal; type=Rainbow Goblin; area=The Weeping Hollow; wouldCount=True; reason=Eligible", StringComparison.OrdinalIgnoreCase), "manifest should summarize the last Goblin observation");
        AssertTrue(manifestText.Contains("Package folder totals:", StringComparison.OrdinalIgnoreCase), "manifest should include package folder totals");
    }
    finally
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    static void Touch(string path)
    {
        File.WriteAllText(path, "not a real image; package test only");
        File.SetCreationTime(path, DateTime.Now);
        File.SetLastWriteTime(path, DateTime.Now);
    }
}

static void TestDebugPackageLimitsFailureAndDebugScreenshotsByDefault()
{
    string testRoot = Path.Combine(Path.GetTempPath(), "GoblinFarmer.PackageTests", Guid.NewGuid().ToString("N"));
    string screenshots = Path.Combine(testRoot, "Screenshots");
    string debugScreenshots = Path.Combine(testRoot, "debug-screenshots");
    string logs = Path.Combine(testRoot, "Logs");
    Directory.CreateDirectory(screenshots);
    Directory.CreateDirectory(debugScreenshots);
    Directory.CreateDirectory(logs);

    try
    {
        DateTime sessionStart = DateTime.Now.AddMinutes(-10);
        File.WriteAllLines(Path.Combine(testRoot, "session-info.txt"),
        [
            $"SessionStartLocal={sessionStart:O}",
            $"SessionStartUtc={sessionStart.ToUniversalTime():O}",
            "GoblinCount=0",
        ]);
        File.WriteAllText(Path.Combine(logs, "GoblinFarmer.log"), "test log");

        for (int index = 0; index < 5; index++)
        {
            DateTime timestamp = sessionStart.AddSeconds(index + 1);
            Touch(Path.Combine(screenshots, $"2026-06-05_140{index:000}_000_Failure_Teleport_TeleportInterrupted_Diablo.png"), timestamp);
            Touch(Path.Combine(screenshots, $"2026-06-05_140{index:000}_000_Failure_Teleport_TeleportInterrupted_App.png"), timestamp);
        }

        Touch(Path.Combine(screenshots, "20260605_140600_000_BattleNetManualPlaySuspected.png"), sessionStart.AddSeconds(6));
        Touch(Path.Combine(screenshots, "20260605_140601_000_BattleNetStillOpenAfterDiabloLaunch.png"), sessionStart.AddSeconds(7));
        Touch(Path.Combine(screenshots, "20260605_140602_000_BattleNetPlayClickAccepted.png"), sessionStart.AddSeconds(8));

        for (int index = 0; index < 7; index++)
        {
            DateTime timestamp = sessionStart.AddSeconds(index + 1);
            Touch(Path.Combine(debugScreenshots, $"20260605_140{index:000}_000_Teleport_TeleportInterrupted.png"), timestamp);
        }

        string scriptPath = FindDebugPackageScript();
        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -RuntimeRoot \"{testRoot}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException("Could not start debug package script");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit(60000);
        AssertEqual(0, process.ExitCode, $"debug package script should succeed. stdout={output}; stderr={error}");

        string packagePath = Directory.GetFiles(Path.Combine(testRoot, "DebugPackages"), "GoblinFarmer_Debug_*.zip")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault() ?? "";
        AssertTrue(File.Exists(packagePath), "debug package zip should be created");

        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        int includedFailureScreenshots = archive.Entries.Count(entry =>
            entry.FullName.Replace('\\', '/').Contains("Screenshots/Failure", StringComparison.OrdinalIgnoreCase) &&
            entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
        int includedDebugScreenshots = archive.Entries.Count(entry =>
            entry.FullName.Replace('\\', '/').StartsWith("debug-screenshots/", StringComparison.OrdinalIgnoreCase) &&
            entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
        bool falseBattleNetFailureIncluded = archive.Entries.Any(entry =>
            entry.FullName.Replace('\\', '/').Contains("Screenshots/Failure", StringComparison.OrdinalIgnoreCase) &&
            (entry.FullName.Contains("BattleNetManualPlaySuspected", StringComparison.OrdinalIgnoreCase) ||
                entry.FullName.Contains("BattleNetStillOpenAfterDiabloLaunch", StringComparison.OrdinalIgnoreCase) ||
                entry.FullName.Contains("BattleNetPlayClickAccepted", StringComparison.OrdinalIgnoreCase)));

        AssertEqual(6, includedFailureScreenshots, "default debug package should include only the latest three failure screenshot groups");
        AssertEqual(4, includedDebugScreenshots, "default debug package should include only the capped current-session debug screenshot sample");
        AssertFalse(falseBattleNetFailureIncluded, "successful/diagnostic Battle.net launch screenshots should not be packaged as failure screenshots");

        ZipArchiveEntry manifest = archive.GetEntry("debug-package-manifest.txt") ?? throw new InvalidOperationException("manifest missing from debug package");
        using StreamReader reader = new(manifest.Open());
        string manifestText = reader.ReadToEnd();
        AssertTrue(manifestText.Contains("Failure screenshot package policy: most recent 3 groups included; 4 files excluded", StringComparison.OrdinalIgnoreCase), "manifest should document default failure screenshot cap and exclusions");
        AssertTrue(manifestText.Contains("- Failure screenshots included: 6", StringComparison.OrdinalIgnoreCase), "manifest should report included failure screenshots");
        AssertTrue(manifestText.Contains("- Failure screenshots excluded: 4", StringComparison.OrdinalIgnoreCase), "manifest should report excluded failure screenshots");
        AssertTrue(manifestText.Contains("Debug screenshot package policy: most recent 4 files included from current session", StringComparison.OrdinalIgnoreCase), "manifest should document debug screenshot cap");
        AssertTrue(manifestText.Contains("- Debug screenshots included: 4", StringComparison.OrdinalIgnoreCase), "manifest should report included debug screenshots");
    }
    finally
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    static void Touch(string path, DateTime timestamp)
    {
        File.WriteAllText(path, "not a real image; package test only");
        File.SetCreationTime(path, timestamp);
        File.SetLastWriteTime(path, timestamp);
    }
}

static void TestDebugPackageLimitsObservationDiagnosticCrops()
{
    string testRoot = Path.Combine(Path.GetTempPath(), "GoblinFarmer.PackageTests", Guid.NewGuid().ToString("N"));
    string observationDiagnostics = Path.Combine(testRoot, "Debug", "GoblinEvidence", "ObservationDiagnostics");
    string logs = Path.Combine(testRoot, "Logs");
    Directory.CreateDirectory(observationDiagnostics);
    Directory.CreateDirectory(logs);

    try
    {
        DateTime sessionStart = DateTime.Now.AddMinutes(-5);
        File.WriteAllLines(Path.Combine(testRoot, "session-info.txt"),
        [
            $"SessionStartLocal={sessionStart:O}",
            $"SessionStartUtc={sessionStart.ToUniversalTime():O}",
            "GoblinCount=0",
        ]);
        File.WriteAllText(
            Path.Combine(logs, "GoblinFarmer.log"),
            "GoblinEvidenceTemplateSetupWarning: templateCount=3; invalidTemplateCount=1; invalidTemplates=Unsupported Evidence.png:UnsupportedNamePattern\r\n" +
            "GoblinEvidenceScanResult: reason=MissingTemplate; missingCount=3");

        for (int index = 0; index < 20; index++)
        {
            string label = index % 2 == 0 ? "Journal" : "Minimap";
            string path = Path.Combine(observationDiagnostics, $"GoblinEvidenceScan_20260605_0909{index:00}_000_{label}.png");
            File.WriteAllText(path, "not a real image; package test only");
            DateTime timestamp = sessionStart.AddSeconds(index + 1);
            File.SetCreationTime(path, timestamp);
            File.SetLastWriteTime(path, timestamp);
        }

        string scriptPath = FindDebugPackageScript();
        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -RuntimeRoot \"{testRoot}\" -MaxGoblinObservationDiagnosticCrops 6",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException("Could not start debug package script");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit(60000);
        AssertEqual(0, process.ExitCode, $"debug package script should succeed. stdout={output}; stderr={error}");

        string packagePath = Directory.GetFiles(Path.Combine(testRoot, "DebugPackages"), "GoblinFarmer_Debug_*.zip")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault() ?? "";
        AssertTrue(File.Exists(packagePath), "debug package zip should be created");

        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        int includedObservationCrops = archive.Entries.Count(entry =>
            entry.FullName.Replace('\\', '/').Contains("Debug/GoblinEvidence/ObservationDiagnostics", StringComparison.OrdinalIgnoreCase) &&
            entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
        AssertEqual(6, includedObservationCrops, "debug package should include only the configured recent ObservationDiagnostics crop sample");

        ZipArchiveEntry manifest = archive.GetEntry("debug-package-manifest.txt") ?? throw new InvalidOperationException("manifest missing from debug package");
        using StreamReader reader = new(manifest.Open());
        string manifestText = reader.ReadToEnd();
        AssertTrue(manifestText.Contains("Goblin observation diagnostic crop package policy: most recent 6 included; 14 excluded", StringComparison.OrdinalIgnoreCase), "manifest should document observation crop package limits");
        AssertTrue(manifestText.Contains("- Goblin observation diagnostic crops included: 6", StringComparison.OrdinalIgnoreCase), "manifest should report included observation crop count");
        AssertTrue(manifestText.Contains("- Goblin observation diagnostic crops excluded: 14", StringComparison.OrdinalIgnoreCase), "manifest should report excluded observation crop count");
        AssertTrue(manifestText.Contains("Goblin evidence missing template state: detected=True; logEntries=2", StringComparison.OrdinalIgnoreCase), "manifest should report missing-template state from the latest log");
        AssertTrue(manifestText.Contains("Unsupported Evidence.png:UnsupportedNamePattern", StringComparison.OrdinalIgnoreCase), "manifest should list invalid per-goblin evidence template issues");
        AssertTrue(manifestText.Contains("<Goblin Type> Engaged & Killed Journal.png", StringComparison.OrdinalIgnoreCase), "manifest should list per-goblin evidence naming guidance");
    }
    finally
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }
}

static void TestDebugPackageLimitsGoblinEvidenceEventScreenshots()
{
    string testRoot = Path.Combine(Path.GetTempPath(), "GoblinFarmer.PackageTests", Guid.NewGuid().ToString("N"));
    string goblinEvidence = Path.Combine(testRoot, "Debug", "GoblinEvidence");
    string logs = Path.Combine(testRoot, "Logs");
    string config = Path.Combine(testRoot, "Config");
    Directory.CreateDirectory(goblinEvidence);
    Directory.CreateDirectory(logs);
    Directory.CreateDirectory(config);

    try
    {
        DateTime sessionStart = DateTime.Now.AddMinutes(-10);
        File.WriteAllLines(Path.Combine(testRoot, "session-info.txt"),
        [
            $"SessionStartLocal={sessionStart:O}",
            $"SessionStartUtc={sessionStart.ToUniversalTime():O}",
            "GoblinCount=0",
        ]);
        File.WriteAllText(Path.Combine(logs, "GoblinFarmer.log"), "test log");
        File.WriteAllText(Path.Combine(config, "AppSettings.json"), """
            {
              "Debug": {
                "EnableSuccessScreenshots": false
              }
            }
            """);

        for (int index = 0; index < 8; index++)
        {
            string path = Path.Combine(goblinEvidence, $"GoblinEvidence_20260605_1212{index:00}_MinimapIcon.png");
            File.WriteAllText(path, "not a real image; package test only");
            DateTime timestamp = sessionStart.AddSeconds(index + 1);
            File.SetCreationTime(path, timestamp);
            File.SetLastWriteTime(path, timestamp);
        }

        string scriptPath = FindDebugPackageScript();
        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -RuntimeRoot \"{testRoot}\" -MaxGoblinEvidenceEventScreenshots 2",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException("Could not start debug package script");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit(60000);
        AssertEqual(0, process.ExitCode, $"debug package script should succeed. stdout={output}; stderr={error}");

        string packagePath = Directory.GetFiles(Path.Combine(testRoot, "DebugPackages"), "GoblinFarmer_Debug_*.zip")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault() ?? "";
        AssertTrue(File.Exists(packagePath), "debug package zip should be created");

        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        int includedEventScreenshots = archive.Entries.Count(entry =>
            entry.FullName.Replace('\\', '/').Contains("Debug/GoblinEvidence/GoblinEvidence_", StringComparison.OrdinalIgnoreCase) &&
            entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
        AssertEqual(2, includedEventScreenshots, "debug package should include only the configured recent goblin evidence event screenshot sample");

        ZipArchiveEntry manifest = archive.GetEntry("debug-package-manifest.txt") ?? throw new InvalidOperationException("manifest missing from debug package");
        using StreamReader reader = new(manifest.Open());
        string manifestText = reader.ReadToEnd();
        AssertTrue(manifestText.Contains("Goblin evidence event screenshot package policy: most recent 2 included when <= 1048576 bytes; 6 excluded; 0 oversized", StringComparison.OrdinalIgnoreCase), "manifest should document event screenshot package limits");
        AssertTrue(manifestText.Contains("- Goblin evidence event screenshots included: 2", StringComparison.OrdinalIgnoreCase), "manifest should report included event screenshot count");
        AssertTrue(manifestText.Contains("- Goblin evidence event screenshots excluded: 6", StringComparison.OrdinalIgnoreCase), "manifest should report excluded event screenshot count");
        AssertTrue(manifestText.Contains("- Goblin evidence event screenshots oversized: 0", StringComparison.OrdinalIgnoreCase), "manifest should report oversized event screenshot count");
    }
    finally
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }
}

static void TestDebugPackageIncludesSuccessScreenshotsWithOptIn()
{
    string testRoot = Path.Combine(Path.GetTempPath(), "GoblinFarmer.PackageTests", Guid.NewGuid().ToString("N"));
    string screenshots = Path.Combine(testRoot, "Screenshots");
    string logs = Path.Combine(testRoot, "Logs");
    string config = Path.Combine(testRoot, "Config");
    Directory.CreateDirectory(screenshots);
    Directory.CreateDirectory(logs);
    Directory.CreateDirectory(config);

    try
    {
        DateTime sessionStart = DateTime.Now.AddMinutes(-5);
        File.WriteAllLines(Path.Combine(testRoot, "session-info.txt"),
        [
            $"SessionStartLocal={sessionStart:O}",
            $"SessionStartUtc={sessionStart.ToUniversalTime():O}",
            "GoblinCount=0",
        ]);
        File.WriteAllText(Path.Combine(logs, "GoblinFarmer.log"), "test log");
        File.WriteAllText(Path.Combine(config, "AppSettings.json"), """
            {
              "Debug": {
                "EnableSuccessScreenshots": true
              }
            }
            """);

        Touch(Path.Combine(screenshots, "2026-06-04_120000_000_Success_StartGame_StartGameClicked_Diablo.png"));
        Touch(Path.Combine(screenshots, "2026-06-04_120000_000_Success_StartGame_StartGameClicked_App.png"));
        Touch(Path.Combine(screenshots, "2026-06-04_120001_000_Failure_StartGame_StartGameVerificationFailed_Diablo.png"));
        Touch(Path.Combine(screenshots, "2026-06-04_120001_000_Failure_StartGame_StartGameVerificationFailed_App.png"));

        string scriptPath = FindDebugPackageScript();
        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -RuntimeRoot \"{testRoot}\" -IncludeSuccessScreenshots -MaxFailureScreenshots 5 -MaxDiagnosticScreenshots 1",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException("Could not start debug package script");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit(60000);
        AssertEqual(0, process.ExitCode, $"debug package script should succeed. stdout={output}; stderr={error}");

        string packagePath = Directory.GetFiles(Path.Combine(testRoot, "DebugPackages"), "GoblinFarmer_Debug_*.zip")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault() ?? "";
        AssertTrue(File.Exists(packagePath), "debug package zip should be created");

        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        AssertTrue(archive.Entries.Any(entry => entry.FullName.Contains("Screenshots/Success", StringComparison.OrdinalIgnoreCase) || entry.FullName.Contains("Screenshots\\Success", StringComparison.OrdinalIgnoreCase)), "success screenshots should be included only when explicitly requested");
        AssertTrue(archive.Entries.Any(entry => entry.FullName.Contains("_Success_", StringComparison.OrdinalIgnoreCase)), "success-category screenshot files should be present after opt-in");
        AssertTrue(archive.Entries.Any(entry => entry.FullName.Contains("Screenshots/Failure", StringComparison.OrdinalIgnoreCase) || entry.FullName.Contains("Screenshots\\Failure", StringComparison.OrdinalIgnoreCase)), "failure screenshots should remain included with success opt-in");

        ZipArchiveEntry manifest = archive.GetEntry("debug-package-manifest.txt") ?? throw new InvalidOperationException("manifest missing from debug package");
        using StreamReader reader = new(manifest.Open());
        string manifestText = reader.ReadToEnd();
        AssertTrue(manifestText.Contains("Success screenshot package policy: Included when selected", StringComparison.OrdinalIgnoreCase), "manifest should document success screenshot opt-in policy");
        AssertTrue(manifestText.Contains("Success screenshots included by opt-in: count=2", StringComparison.OrdinalIgnoreCase), "manifest should report included success screenshots after opt-in");
        AssertTrue(manifestText.Contains("- Success screenshots included: 2", StringComparison.OrdinalIgnoreCase), "manifest included-file counts should report success screenshots");
    }
    finally
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    static void Touch(string path)
    {
        File.WriteAllText(path, "not a real image; package test only");
        File.SetCreationTime(path, DateTime.Now);
        File.SetLastWriteTime(path, DateTime.Now);
    }
}

static string FindDebugPackageScript()
{
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory != null)
    {
        string candidate = Path.Combine(directory.FullName, "Scripts", "create-debug-package.ps1");
        if (File.Exists(candidate))
        {
            return candidate;
        }

        directory = directory.Parent;
    }

    throw new FileNotFoundException("Could not locate Scripts/create-debug-package.ps1 from test output directory.");
}

static void TestGoblinAreaResolverKeepsTrueAreasSeparate()
{
    AssertEqual("Cathedral Level 1", GoblinAreaResolver.Resolve(" cathedral-level_1 ").AreaKey, "Cathedral Level 1 should normalize punctuation and casing");
    AssertEqual("Cathedral Level 2", GoblinAreaResolver.Resolve("Cathedral Level 2").AreaKey, "Cathedral Level 2 should remain distinct");
    AssertEqual("Cathedral Level 3", GoblinAreaResolver.Resolve("Cathedral Level 3").AreaKey, "Cathedral Level 3 should remain distinct");
    AssertFalse(
        GoblinAreaResolver.Resolve("Cathedral Level 1").AreaKey == GoblinAreaResolver.Resolve("Cathedral Level 2").AreaKey,
        "Cathedral levels should not collapse together");

    AssertEqual("City of Caldeum", GoblinAreaResolver.Resolve("City Of Caldeum").AreaKey, "City Of Caldeum casing alias should normalize");
    AssertEqual("Caldeum Bazaar", GoblinAreaResolver.Resolve("Caldeum Bazaar").AreaKey, "Caldeum Bazaar should remain distinct");
    AssertEqual("Ruined Cistern", GoblinAreaResolver.Resolve("Ruined Cistern").AreaKey, "Ruined Cistern should remain distinct");
    AssertFalse(
        GoblinAreaResolver.Resolve("City Of Caldeum").AreaKey == GoblinAreaResolver.Resolve("Sewers of Caldeum").AreaKey,
        "Caldeum subregions should not collapse when detected separately");

    AssertEqual("Ancient Waterway", GoblinAreaResolver.Resolve("Ancient Waterway").AreaKey, "Ancient Waterway should resolve");
    AssertEqual("Eastern Channel Level 2", GoblinAreaResolver.Resolve("Eastern Channel Level 2").AreaKey, "Waterway channels should remain distinct");
    AssertEqual("Battlefields", GoblinAreaResolver.Resolve("The Battlefields").AreaKey, "The Battlefields display alias should normalize");
    AssertEqual("Rakkis Crossing", GoblinAreaResolver.Resolve("Rakkis Crossing").AreaKey, "Rakkis Crossing should not collapse into Battlefields");
    AssertFalse(GoblinAreaResolver.Resolve("").Resolved, "empty location should be unresolved");
}

static void TestGoblinAreaDuplicateGuardSuppressesSameAreaAndResets()
{
    GoblinAreaDuplicateGuard guard = new();
    string cathedralLevel1 = GoblinAreaResolver.Resolve("Cathedral Level 1").AreaKey;
    string cathedralLevel2 = GoblinAreaResolver.Resolve("Cathedral Level 2").AreaKey;

    AssertTrue(guard.TryAccept(cathedralLevel1), "first count in an area should be accepted");
    AssertFalse(guard.TryAccept(cathedralLevel1), "same resolved area should be suppressed");
    AssertTrue(guard.TryAccept(cathedralLevel2), "different resolved area should be accepted");
    AssertEqual(2, guard.Reset(), "reset should report cleared counted areas");
    AssertTrue(guard.TryAccept(cathedralLevel1), "reset should allow the same area again");
}

static void TestGoblinAreaDuplicateGuardAllowsPandemoniumFortressTwiceOnly()
{
    GoblinAreaDuplicateGuard guard = new();

    AssertPandemoniumAreaLimit(guard, "Pandemonium Fortress Level 1");
    AssertPandemoniumAreaLimit(guard, "Pandemonium Fortress Level 2");
    AssertEqual(2, guard.Reset(), "reset should report both PF area keys");

    string pf1 = GoblinAreaResolver.Resolve("Pandemonium Fortress Level 1").AreaKey;
    AssertTrue(guard.TryAccept(pf1), "reset should clear PF1 area count state");
    AssertTrue(guard.TryAccept(pf1), "reset should allow PF1 second count again");
    AssertFalse(guard.TryAccept(pf1), "reset should still suppress PF1 third count");
}

static void TestGoblinAreaDuplicateGuardAllowsStingingWindsTwiceOnly()
{
    GoblinAreaDuplicateGuard guard = new();
    string stingingWinds = GoblinAreaResolver.Resolve("Stinging Winds").AreaKey;

    AssertTrue(guard.TryAccept(stingingWinds, out GoblinAreaDuplicateGuardResult first), "Stinging Winds first count should be accepted");
    AssertEqual(1, first.AreaCount, "Stinging Winds first count should report areaCount=1");
    AssertEqual(2, first.AreaLimit, "Stinging Winds should report areaLimit=2");
    AssertTrue(guard.TryAccept(stingingWinds, out GoblinAreaDuplicateGuardResult second), "Stinging Winds second count should be accepted");
    AssertEqual(2, second.AreaCount, "Stinging Winds second count should report areaCount=2");
    AssertEqual(2, second.AreaLimit, "Stinging Winds should keep areaLimit=2");
    AssertFalse(guard.TryAccept(stingingWinds, out GoblinAreaDuplicateGuardResult third), "Stinging Winds third count should be suppressed");
    AssertEqual(2, third.AreaCount, "Stinging Winds third count should report capped areaCount=2");
    AssertEqual(2, third.AreaLimit, "Stinging Winds third count should report areaLimit=2");
}

static void TestGoblinAreaDuplicateGuardKeepsDefaultOneCountAreas()
{
    string[] defaultOneCountAreas =
    [
        "Cathedral Level 1",
        "Cathedral Level 3",
        "City Of Caldeum",
        "Ruined Cistern",
        "Ancient Waterway",
        "Western Channel Level 1",
        "Western Channel Level 2",
        "Eastern Channel Level 1",
        "Eastern Channel Level 2",
        "Cave Of The Moon Clan Level 2",
        "Caverns of Frost Level 1",
        "Caverns of Frost Level 2",
        "Black Canyon Mines",
        "Battlefields",
        "Rakkis Crossing",
    ];

    foreach (string areaName in defaultOneCountAreas)
    {
        GoblinAreaDuplicateGuard guard = new();
        string areaKey = GoblinAreaResolver.Resolve(areaName).AreaKey;

        AssertTrue(guard.TryAccept(areaKey, out GoblinAreaDuplicateGuardResult first), $"{areaName} first count should be accepted");
        AssertEqual(1, first.AreaCount, $"{areaName} first count should report areaCount=1");
        AssertEqual(1, first.AreaLimit, $"{areaName} should keep default areaLimit=1");
        AssertFalse(guard.TryAccept(areaKey, out GoblinAreaDuplicateGuardResult second), $"{areaName} second count should be suppressed");
        AssertEqual(1, second.AreaCount, $"{areaName} suppressed count should report the current areaCount=1");
        AssertEqual(1, second.AreaLimit, $"{areaName} suppressed count should report areaLimit=1");
    }
}

static void TestGoblinAreaDuplicateGuardPeekDoesNotConsumeCountSlots()
{
    GoblinAreaDuplicateGuard guard = new();
    string pf1 = GoblinAreaResolver.Resolve("Pandemonium Fortress Level 1").AreaKey;
    string weeping = GoblinAreaResolver.Resolve("The Weeping Hollow").AreaKey;

    GoblinAreaDuplicateGuardResult firstPfPeek = guard.Peek(pf1);
    AssertTrue(firstPfPeek.Accepted, "PF1 first observation should be eligible");
    AssertEqual(0, firstPfPeek.AreaCount, "PF1 first observation should report currentAreaCount=0");
    AssertEqual(2, firstPfPeek.AreaLimit, "PF1 observation should preserve the two-count limit");
    AssertTrue(guard.TryAccept(pf1, out GoblinAreaDuplicateGuardResult firstPfCount), "PF1 first real count should still be accepted after observation peek");
    AssertEqual(1, firstPfCount.AreaCount, "PF1 first real count should consume only one slot");

    GoblinAreaDuplicateGuardResult secondPfPeek = guard.Peek(pf1);
    AssertTrue(secondPfPeek.Accepted, "PF1 second observation should still be eligible");
    AssertEqual(1, secondPfPeek.AreaCount, "PF1 second observation should report the current count before consuming");
    AssertTrue(guard.TryAccept(pf1), "PF1 second real count should still be accepted after observation peek");

    GoblinAreaDuplicateGuardResult saturatedPfPeek = guard.Peek(pf1);
    AssertFalse(saturatedPfPeek.Accepted, "PF1 observation should suppress after the real two-count limit is reached");
    AssertEqual(2, saturatedPfPeek.AreaCount, "PF1 saturated observation should report currentAreaCount=2");
    AssertEqual(2, saturatedPfPeek.AreaLimit, "PF1 saturated observation should report areaLimit=2");

    GoblinAreaDuplicateGuardResult weepingPeek = guard.Peek(weeping);
    AssertTrue(weepingPeek.Accepted, "default area observation should be eligible before a real count");
    AssertEqual(0, weepingPeek.AreaCount, "default area observation should not consume a slot");
    AssertEqual(1, weepingPeek.AreaLimit, "default area observation should report areaLimit=1");
    AssertTrue(guard.TryAccept(weeping), "default area real count should still be accepted after observation peek");
    AssertFalse(guard.Peek(weeping).Accepted, "default area observation should suppress after one real count");
}

static void AssertPandemoniumAreaLimit(GoblinAreaDuplicateGuard guard, string areaName)
{
    string areaKey = GoblinAreaResolver.Resolve(areaName).AreaKey;

    AssertTrue(guard.TryAccept(areaKey, out GoblinAreaDuplicateGuardResult first), $"{areaName} first count should be accepted");
    AssertEqual(1, first.AreaCount, $"{areaName} first count should report areaCount=1");
    AssertEqual(2, first.AreaLimit, $"{areaName} should report areaLimit=2");
    AssertTrue(guard.TryAccept(areaKey, out GoblinAreaDuplicateGuardResult second), $"{areaName} second count should be accepted");
    AssertEqual(2, second.AreaCount, $"{areaName} second count should report areaCount=2");
    AssertEqual(2, second.AreaLimit, $"{areaName} should keep areaLimit=2");
    AssertFalse(guard.TryAccept(areaKey, out GoblinAreaDuplicateGuardResult third), $"{areaName} third count should be suppressed");
    AssertEqual(2, third.AreaCount, $"{areaName} third count should report the capped areaCount=2");
    AssertEqual(2, third.AreaLimit, $"{areaName} third count should report areaLimit=2");
}

static void TestGoblinManualCountBlockListBlocksExplicitNoCountAreas()
{
    string[] blockedAreas =
    [
        "Whimsydale",
        "City Of Caldeum",
        "City of Caldeum",
        "Gates of Caldeum",
        "Caldeum Bazaar",
        "Flooded Causeway",
        "Ancient Waterway",
        "The Bridge Of Korsikk",
        "New Tristram",
    ];

    foreach (string areaName in blockedAreas)
    {
        string areaKey = GoblinAreaResolver.Resolve(areaName).AreaKey;
        AssertTrue(GoblinManualCountBlockList.IsBlocked(areaKey), $"{areaName} should be blocked from manual goblin counts");
        AssertTrue(GoblinManualCountBlockList.IsBlocked(areaName.ToLowerInvariant()), $"{areaName} block list check should be case-insensitive");
    }

    string[] countableAreas =
    [
        "Sewers of Caldeum",
        "Ruined Cistern",
        "Western Channel Level 1",
        "Western Channel Level 2",
        "Eastern Channel Level 1",
        "Eastern Channel Level 2",
        "Cave Of The Moon Clan Level 1",
        "Cave Of The Moon Clan Level 2",
        "Caverns of Frost Level 1",
        "Caverns of Frost Level 2",
        "Pandemonium Fortress Level 1",
        "Pandemonium Fortress Level 2",
        "Cathedral Level 1",
        "Cathedral Level 2",
    ];

    foreach (string areaName in countableAreas)
    {
        string areaKey = GoblinAreaResolver.Resolve(areaName).AreaKey;
        AssertFalse(GoblinManualCountBlockList.IsBlocked(areaKey), $"{areaName} should not be in the manual count block list");
    }
}

static void TestGoblinObservationCountersAreDiagnosticOnly()
{
    DiagnosticsSessionState session = new();
    session.RecordGoblinObservation(new GoblinObservationRecord(
        DateTime.UtcNow,
        "Journal",
        "Rainbow Goblin",
        "The Weeping Hollow",
        "The Weeping Hollow",
        true,
        "Eligible",
        "Available",
        1,
        0));
    session.RecordGoblinObservation(new GoblinObservationRecord(
        DateTime.UtcNow,
        "Minimap",
        "Blood Thief",
        "WhimsyDale",
        "WhimsyDale",
        false,
        "BlockedArea",
        "BlockedArea",
        1,
        0));
    session.RecordGoblinObservation(new GoblinObservationRecord(
        DateTime.UtcNow,
        "Journal",
        "Treasure Goblin",
        "Cathedral Level 1",
        "Cathedral Level 1",
        false,
        "AreaAlreadyCounted",
        "AreaAlreadyCounted",
        1,
        1));

    DiagnosticsSessionSnapshot snapshot = session.Snapshot(DateTime.Now);
    AssertEqual(0, snapshot.GoblinCount, "observations should not increment GoblinCount");
    AssertEqual(0, snapshot.GoblinFoundRecordCount, "observations should not create GoblinFound records");
    AssertEqual(0.0, snapshot.GoblinsPerHour, "observations should not affect GPH");
    AssertEqual(3, snapshot.GoblinObservationCount, "all observations should be tracked diagnostically");
    AssertEqual(2, snapshot.JournalObservationCount, "journal observations should be counted separately");
    AssertEqual(1, snapshot.MinimapObservationCount, "minimap observations should be counted separately");
    AssertEqual(1, snapshot.EligibleObservationCount, "eligible observations should be counted separately");
    AssertEqual(1, snapshot.BlockedObservationCount, "blocked observations should be counted separately");
    AssertEqual(1, snapshot.DuplicateObservationCount, "duplicate observations should be counted separately");
    AssertEqual("Cathedral Level 1", snapshot.LastGoblinObservation?.AreaKey ?? "", "last observation should be retained for diagnostics");
}

static void TestGoblinObservationTypeReuseRequiresRecentMatchingArea()
{
    DateTime now = DateTime.UtcNow;
    TimeSpan window = TimeSpan.FromSeconds(20);
    GoblinObservationRecord matchingObservation = new(
        now.AddSeconds(-3),
        "Minimap",
        "Treasure Goblin",
        "Southern Highlands",
        "Southern Highlands",
        true,
        "Eligible",
        "Available",
        1,
        0);

    AssertEqual(
        "Treasure Goblin",
        GoblinObservationTypeReuse.ResolveForManualCount("Unknown", "Southern Highlands", matchingObservation, now, window),
        "recent same-area observation should provide the manual notification goblin type");

    AssertEqual(
        "Unknown",
        GoblinObservationTypeReuse.ResolveForManualCount("Unknown", "The Weeping Hollow", matchingObservation, now, window),
        "observation from another area should not be reused");

    GoblinObservationRecord staleObservation = matchingObservation with { TimestampUtc = now.AddSeconds(-30) };
    AssertEqual(
        "Unknown",
        GoblinObservationTypeReuse.ResolveForManualCount("Unknown", "Southern Highlands", staleObservation, now, window),
        "stale observation should not be reused");

    AssertEqual(
        "Rainbow Goblin",
        GoblinObservationTypeReuse.ResolveForManualCount("Rainbow Goblin", "Southern Highlands", matchingObservation, now, window),
        "known manual goblin type should win over observation reuse");
}

static void TestGoblinManualUnknownCountRequiresFreshObservationByDefault()
{
    AssertTrue(
        GoblinManualCountPolicy.RequiresFreshObservationForUnknownManualCount(
            "ManualHotkey",
            "Unknown",
            areaResolved: true,
            allowUnknownManualCount: false,
            hasFreshObservation: false),
        "manual Unknown in a resolved area should suppress when no fresh observation exists");

    AssertFalse(
        GoblinManualCountPolicy.RequiresFreshObservationForUnknownManualCount(
            "ManualHotkey",
            "Unknown",
            areaResolved: true,
            allowUnknownManualCount: false,
            hasFreshObservation: true),
        "manual Unknown should be allowed when a fresh observation/candidate exists");

    AssertFalse(
        GoblinManualCountPolicy.RequiresFreshObservationForUnknownManualCount(
            "ManualHotkey",
            "Unknown",
            areaResolved: true,
            allowUnknownManualCount: true,
            hasFreshObservation: false),
        "explicit opt-in should preserve legacy Unknown manual counting");

    AssertFalse(
        GoblinManualCountPolicy.RequiresFreshObservationForUnknownManualCount(
            "ManualHotkey",
            "Treasure Goblin",
            areaResolved: true,
            allowUnknownManualCount: false,
            hasFreshObservation: false),
        "known goblin types should not be blocked by the Unknown manual-count gate");

    AssertFalse(
        GoblinManualCountPolicy.RequiresFreshObservationForUnknownManualCount(
            "ManualHotkey",
            "Unknown",
            areaResolved: false,
            allowUnknownManualCount: false,
            hasFreshObservation: false),
        "unresolved legacy fallback remains outside the resolved-area no-fresh gate");
}

static void TestGoblinObservationUiStateLogsUpdateAndClear()
{
    string repoRoot = FindRepositoryRootForTests();
    string sessionStatsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.cs"));
    string evidenceSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs"));

    AssertTrue(sessionStatsSource.Contains("PortStartGoblinObservationScanner(\"Startup\")", StringComparison.Ordinal), "observation scanner should start outside combat at session initialization");
    AssertTrue(sessionStatsSource.Contains("LastObservationUpdated", StringComparison.Ordinal), "valid observations should log LastObservationUpdated when the UI state changes");
    AssertTrue(sessionStatsSource.Contains("PortAutomaticGoblinObservationDisplayHold", StringComparison.Ordinal), "automatic observations should stay readable briefly after the first no-candidate scan");
    AssertTrue(sessionStatsSource.Contains("ObservationDisplayHold", StringComparison.Ordinal), "no-candidate clears should report when they preserve a recent automatic observation");
    AssertTrue(sessionStatsSource.Contains("LastObservationPersistent", StringComparison.Ordinal), "no-candidate scans should preserve the latest real observation after the short hold expires");
    AssertTrue(sessionStatsSource.Contains("AreaChanged", StringComparison.Ordinal), "no-candidate clears should drop stale persisted observations when the current area changes");
    AssertTrue(sessionStatsSource.Contains("currentAreaKey", StringComparison.Ordinal), "Last Observation clear logs should report the current area used for stale-area decisions");
    AssertTrue(sessionStatsSource.Contains("displayHoldSeconds={PortAutomaticGoblinObservationDisplayHold.TotalSeconds:0}", StringComparison.Ordinal), "automatic observation update logs should include the display hold duration");
    AssertTrue(sessionStatsSource.Contains("LastObservationCleared", StringComparison.Ordinal), "no-candidate/stale scans should log LastObservationCleared when the UI state changes");
    AssertTrue(evidenceSource.Contains("PortMarkGoblinObservationNoCurrent(\"No current observation\")", StringComparison.Ordinal), "no-candidate scans should route through the Last Observation state helper");
    AssertTrue(evidenceSource.Contains("private const int GoblinEvidenceScanIntervalMs = 750", StringComparison.Ordinal), "observation scan interval should be responsive enough for live diagnostic feedback without loosening evidence thresholds");
}

static void TestGoblinObservationModeEnabledByDefaultInRelease()
{
    string repoRoot = FindRepositoryRootForTests();
    string appSettingsSource = File.ReadAllText(Path.Combine(repoRoot, "AppSettings.cs"));
    string evidenceSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs"));
    string configSource = File.ReadAllText(Path.Combine(repoRoot, "Config", "AppSettings.json"));
    string scannerEnabledMethod = ExtractMethodBody(evidenceSource, "private static bool PortGoblinObservationScannerEnabled");

    AssertTrue(appSettingsSource.Contains("public bool EnableObservationMode { get; set; } = true", StringComparison.Ordinal), "Observation Mode should default on for v1.4 Release diagnostics");
    AssertTrue(appSettingsSource.Contains("GoblinTracker.EnableObservationMode={GoblinTracker.EnableObservationMode}", StringComparison.Ordinal), "AppSettings startup log should expose the Observation Mode setting");
    AssertTrue(appSettingsSource.Contains("TryGetProperty(\"EnableObservationMode\"", StringComparison.Ordinal), "existing installed configs should be migrated when the Observation Mode setting is absent");
    AssertTrue(configSource.Contains("\"EnableObservationMode\"", StringComparison.Ordinal), "tracked config should make Observation Mode visible");
    AssertTrue(scannerEnabledMethod.Contains("AppSettings.GoblinTracker.EnableObservationMode", StringComparison.Ordinal), "scanner enablement should be controlled by the GoblinTracker Observation Mode setting");
    AssertFalse(scannerEnabledMethod.Contains("DebugManager.DiagnosticLoggingEnabled", StringComparison.Ordinal), "normal Release observation scanning must not require verbose diagnostic logging");
    AssertFalse(scannerEnabledMethod.Contains("AppSettings.Debug.DebugMode", StringComparison.Ordinal), "normal Release observation scanning must not require Debug Mode");
    AssertTrue(evidenceSource.Contains("ObservationModeConfiguration", StringComparison.Ordinal), "startup should log why Observation Mode is enabled or disabled");
    AssertTrue(evidenceSource.Contains("enableObservationMode={AppSettings.GoblinTracker.EnableObservationMode}", StringComparison.Ordinal), "Observation Mode diagnostics should report the scanner setting separately");
    AssertTrue(evidenceSource.Contains("enableAutomaticCounting={AppSettings.GoblinTracker.EnableAutomaticCounting}", StringComparison.Ordinal), "Observation Mode diagnostics should report the automatic-count setting separately");
    AssertTrue(evidenceSource.Contains("automaticCountingEnabled={PortGoblinAutomaticCountingEnabled()}", StringComparison.Ordinal), "Observation Mode diagnostics should report the effective automatic-count gate");
    AssertTrue(evidenceSource.Contains("observationModeEnabled={PortGoblinObservationScannerEnabled()}", StringComparison.Ordinal), "scan skipped diagnostics should expose the current Observation Mode setting");
}

static void TestGoblinAutomaticCountingGateDefaultsDisabled()
{
    string repoRoot = FindRepositoryRootForTests();
    string appSettingsSource = File.ReadAllText(Path.Combine(repoRoot, "AppSettings.cs"));
    string evidenceSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs"));
    string sessionStatsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.cs"));
    string configSource = File.ReadAllText(Path.Combine(repoRoot, "Config", "AppSettings.json"));
    string automaticEnabledMethod = ExtractMethodBody(evidenceSource, "private static bool PortGoblinAutomaticCountingEnabled");
    string observeMethod = ExtractMethodBody(sessionStatsSource, "private bool PortObserveGoblinCandidate");
    string autoCountMethod = ExtractMethodBody(sessionStatsSource, "private bool PortTryRecordAutomaticGoblinCount");

    AssertTrue(appSettingsSource.Contains("public bool EnableAutomaticCounting { get; set; } = false", StringComparison.Ordinal), "automatic goblin counting should default off");
    AssertTrue(configSource.Contains("\"EnableAutomaticCounting\"", StringComparison.Ordinal), "config should expose the automatic counting setting even when a local VS Debug run toggles it");
    AssertTrue(appSettingsSource.Contains("TryGetProperty(\"EnableAutomaticCounting\"", StringComparison.Ordinal), "installed configs missing the automatic-count setting should migrate to the disabled default");
    AssertTrue(appSettingsSource.Contains("GoblinTracker.EnableAutomaticCounting={GoblinTracker.EnableAutomaticCounting}", StringComparison.Ordinal), "startup AppSettings logs should expose the automatic-count setting");
    AssertTrue(automaticEnabledMethod.Contains("AppSettings.GoblinTracker.EnableObservationMode", StringComparison.Ordinal), "automatic counting should require Observation Mode");
    AssertTrue(automaticEnabledMethod.Contains("AppSettings.GoblinTracker.EnableAutomaticCounting", StringComparison.Ordinal), "automatic counting should require the explicit automatic-count setting");
    AssertTrue(observeMethod.Contains("PortTryRecordAutomaticGoblinCount(observation, area, evidenceSignature, evidenceImagePath)", StringComparison.Ordinal), "observation candidates should pass through the gated automatic-count helper with evidence identity and image path");
    AssertTrue(autoCountMethod.Contains("bool autoCountingEnabled = PortGoblinAutomaticCountingEnabled()", StringComparison.Ordinal), "automatic count helper should snapshot the effective gate");
    AssertTrue(autoCountMethod.Contains("AutomaticCountingDisabled", StringComparison.Ordinal), "automatic count helper should skip incrementing when the gate is disabled");
    AssertTrue(autoCountMethod.Contains("GoblinAutoCountSkippedDisabled", StringComparison.Ordinal), "disabled automatic counts should log that evidence was tracked but no count was attempted");
    AssertTrue(autoCountMethod.Contains("GoblinAutoCountAccepted", StringComparison.Ordinal), "enabled automatic counts should log accepted decisions");
    AssertTrue(autoCountMethod.Contains("GoblinAutoCountSuppressed", StringComparison.Ordinal), "enabled automatic counts should log suppressed decisions");
    AssertTrue(autoCountMethod.Contains("portGoblinAreaDuplicateGuard.TryAccept", StringComparison.Ordinal), "enabled automatic counts should consume the existing duplicate guard");
    AssertTrue(autoCountMethod.Contains("GoblinManualCountBlockList.IsBlocked", StringComparison.Ordinal), "enabled automatic counts should preserve the block list");
}

static void TestGoblinVsDebugAutomaticCountSettingsAreFormToggleable()
{
    string repoRoot = FindRepositoryRootForTests();
    string automationSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.PortedAutomation.cs.cs"));
    string releaseSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.Release.cs"));

    AssertTrue(automationSource.Contains("chkGoblinObservationMode", StringComparison.Ordinal), "VS Debug form should expose an Observation Mode checkbox");
    AssertTrue(automationSource.Contains("chkGoblinAutomaticCounting", StringComparison.Ordinal), "VS Debug form should expose an Automatic Counting checkbox");
    AssertTrue(automationSource.Contains("chkGoblinManualTestCountOverride", StringComparison.Ordinal), "VS Debug form should expose a manual test count override checkbox");
    AssertTrue(automationSource.Contains("chkGoblinDecisionTrace", StringComparison.Ordinal), "VS Debug form should expose a Decision Trace checkbox");
    AssertFalse(automationSource.Contains("btnCreateGoblinReviewFiles", StringComparison.Ordinal), "VS Debug form should not require a manual review files button");
    AssertTrue(automationSource.Contains("PortCreateGoblinReplayReviewFilesOnVsDebugClose();", StringComparison.Ordinal), "VS Debug form close should create loose review files automatically");
    AssertTrue(releaseSource.Contains("PortInitializeGoblinTrackerDebugPreferenceControls();", StringComparison.Ordinal), "VS Debug Goblin Tracker checkboxes should initialize before runtime validation can stop startup");
    AssertTrue(automationSource.Contains("portSettingsGroup.Controls.Add(chkGoblinObservationMode)", StringComparison.Ordinal), "Observation Mode checkbox should be placed inside the visible Settings group");
    AssertTrue(automationSource.Contains("portSettingsGroup.Controls.Add(chkGoblinAutomaticCounting)", StringComparison.Ordinal), "Automatic Counting checkbox should be placed inside the visible Settings group");
    AssertTrue(automationSource.Contains("portSettingsGroup.Controls.Add(chkGoblinManualTestCountOverride)", StringComparison.Ordinal), "manual test count override checkbox should be placed inside the visible Settings group");
    AssertTrue(automationSource.Contains("portSettingsGroup.Controls.Add(chkGoblinDecisionTrace)", StringComparison.Ordinal), "Decision Trace checkbox should be placed inside the visible Settings group");
    AssertFalse(automationSource.Contains("Controls.Add(grpGoblinTrackerDebugSettings)", StringComparison.Ordinal), "VS Debug Goblin Tracker checkboxes should not be added as a layered top-level overlay");
    AssertTrue(automationSource.Contains("AppSettings.GoblinTracker.EnableObservationMode = chkGoblinObservationMode.Checked", StringComparison.Ordinal), "Observation Mode checkbox changes should persist to AppSettings");
    AssertTrue(automationSource.Contains("AppSettings.GoblinTracker.EnableAutomaticCounting = chkGoblinAutomaticCounting.Checked", StringComparison.Ordinal), "Automatic Counting checkbox changes should persist to AppSettings");
    AssertTrue(automationSource.Contains("AppSettings.GoblinTracker.EnableManualTestCountOverride = chkGoblinManualTestCountOverride.Checked", StringComparison.Ordinal), "manual test count override checkbox changes should persist to AppSettings");
    AssertTrue(automationSource.Contains("AppSettings.GoblinTracker.EnableDecisionTrace = chkGoblinDecisionTrace.Checked", StringComparison.Ordinal), "Decision Trace checkbox changes should persist to AppSettings");
    AssertTrue(automationSource.Contains("PortSetGoblinAutomaticCountingArmedState(source)", StringComparison.Ordinal), "toggling automatic counting should re-arm the freshness gate");
    AssertTrue(automationSource.Contains("PortStartGoblinObservationScanner(source)", StringComparison.Ordinal), "enabling Observation Mode from the form should ensure the scanner is running");
    AssertTrue(automationSource.Contains("portSettingsGroup.Height = Math.Max(portSettingsGroup.Height, 214)", StringComparison.Ordinal), "VS Debug Settings group should still expand for the added Goblin Tracker test controls");
}

static void TestVsDebugDiagnosticsIncludeNextTestStepsTab()
{
    string repoRoot = FindRepositoryRootForTests();
    string diagnosticsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.Diagnostics.cs"));

    AssertTrue(diagnosticsSource.Contains("bool showNextTests = AppSettings.IsVsDebugProfile", StringComparison.Ordinal), "Next Tests tab should be scoped to VS Debug/dev profile");
    AssertTrue(diagnosticsSource.Contains("Name = \"tabNextTestSteps\"", StringComparison.Ordinal), "diagnostics should include a dedicated Next Tests tab");
    AssertTrue(diagnosticsSource.Contains("Text = \"Next Tests\"", StringComparison.Ordinal), "Next Tests tab should have a readable tab title");
    AssertTrue(diagnosticsSource.Contains("PortCreateNextTestStepsPanel", StringComparison.Ordinal), "Next Tests tab should use a dedicated checklist panel");
    AssertTrue(diagnosticsSource.Contains("CheckBox checkBox = new()", StringComparison.Ordinal), "Next Tests should render testable checkbox rows");
    AssertTrue(diagnosticsSource.Contains("Checked = false", StringComparison.Ordinal), "Next Tests checkboxes should start unchecked so checked means tested");
    AssertTrue(diagnosticsSource.Contains("portNextTestStepCheckboxes", StringComparison.Ordinal), "Next Tests checkbox state should be retained for automatic debug-package metadata");
    AssertTrue(diagnosticsSource.Contains("PortNextTestStepMetadataLines", StringComparison.Ordinal), "Next Tests should export checked/unchecked state without a prompt");
    AssertTrue(diagnosticsSource.Contains("CheckedCount", StringComparison.Ordinal), "Next Tests metadata should summarize checked items");
    AssertTrue(diagnosticsSource.Contains("UncheckedCount", StringComparison.Ordinal), "Next Tests metadata should summarize unchecked items");
    AssertTrue(diagnosticsSource.Contains("Goblin Tracker Auto-Count Next Pass", StringComparison.Ordinal), "Next Tests should be framed around the current automatic-count pass");
    AssertTrue(diagnosticsSource.Contains("Auto Goblin Count", StringComparison.Ordinal), "Next Tests should remind the tester to enable automatic counting for real validation");
    AssertTrue(diagnosticsSource.Contains("Test Count Override is off", StringComparison.Ordinal), "Next Tests should require the synthetic override off during real validation");
    AssertTrue(diagnosticsSource.Contains("Baseline already validated", StringComparison.Ordinal), "Next Tests should define setup rows as already-validated baseline reminders");
    AssertTrue(diagnosticsSource.Contains("Must-test route blockers", StringComparison.Ordinal), "Next Tests should call out the current must-test route blockers");
    AssertTrue(diagnosticsSource.Contains("If encountered regressions", StringComparison.Ordinal), "Next Tests should separate opportunistic regression checks from must-test blockers");
    AssertTrue(diagnosticsSource.Contains("Cave Of The Moon Clan Level 2", StringComparison.Ordinal), "Next Tests should list Cave Level 2 validation");
    AssertTrue(diagnosticsSource.Contains("Eastern Channel Level 2", StringComparison.Ordinal), "Next Tests should list Eastern Channel Level 2 validation");
    AssertTrue(diagnosticsSource.Contains("Battlefields", StringComparison.Ordinal), "Next Tests should list Battlefields validation");
    AssertTrue(diagnosticsSource.Contains("Notification latency", StringComparison.Ordinal), "Next Tests should include post-tuning notification latency validation");
    AssertFalse(diagnosticsSource.Contains("Stinging Winds: old journal evidence must not count", StringComparison.Ordinal), "Stinging Winds should not remain a must-test blocker after the latest live verification");
    AssertTrue(diagnosticsSource.Contains("New Game cleanup", StringComparison.Ordinal), "Next Tests should keep New Game cleanup validation after Reset Stats was live-confirmed");
    AssertTrue(diagnosticsSource.Contains("BlockedArea", StringComparison.Ordinal), "Next Tests should include blocked-area validation");
    AssertTrue(diagnosticsSource.Contains("Gilded Baron and Malevolent Tormentor", StringComparison.Ordinal), "Next Tests should include classification validation");
    AssertTrue(diagnosticsSource.Contains("Combat hotkey during Waiting For Location Confirmation", StringComparison.Ordinal), "Next Tests should include the combat-hotkey arrival wait override validation");
    AssertTrue(diagnosticsSource.Contains("Close the VS Debug form after the run", StringComparison.Ordinal), "Next Tests should explain automatic close-time loose review generation");
    AssertFalse(diagnosticsSource.Contains("Review rule", StringComparison.Ordinal), "Next Tests should not include a manual review rule section now that review files are automatic");
    AssertFalse(diagnosticsSource.Contains("Check this only after you clicked Review Files", StringComparison.Ordinal), "Next Tests should not require manual Review Files interaction");
    int caveIndex = diagnosticsSource.IndexOf("Cave Of The Moon Clan Level 2", StringComparison.Ordinal);
    int easternChannelIndex = diagnosticsSource.IndexOf("Eastern Channel Level 2", StringComparison.Ordinal);
    AssertTrue(caveIndex > 0 && easternChannelIndex > caveIndex, "route-specific Next Tests should list Cave Of The Moon Clan Level 2 before Eastern Channel Level 2");
}

static void TestGoblinVsDebugManualTestCountOverrideIsSafetyScoped()
{
    string repoRoot = FindRepositoryRootForTests();
    string appSettingsSource = File.ReadAllText(Path.Combine(repoRoot, "AppSettings.cs"));
    string configSource = File.ReadAllText(Path.Combine(repoRoot, "Config", "AppSettings.json"));
    string sessionStatsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.cs"));
    string recordMethod = ExtractMethodBody(sessionStatsSource, "private bool PortTryRecordGoblinFound");
    string overrideMethod = ExtractMethodBody(sessionStatsSource, "private static bool PortGoblinManualTestCountOverrideEnabled");

    AssertTrue(appSettingsSource.Contains("public bool EnableManualTestCountOverride { get; set; } = false", StringComparison.Ordinal), "manual test count override should default off");
    AssertTrue(appSettingsSource.Contains("TryGetProperty(\"EnableManualTestCountOverride\"", StringComparison.Ordinal), "installed configs missing the manual test count override setting should migrate to the disabled default");
    AssertTrue(appSettingsSource.Contains("GoblinTracker.EnableManualTestCountOverride={GoblinTracker.EnableManualTestCountOverride}", StringComparison.Ordinal), "startup AppSettings logs should expose the manual test count override setting");
    AssertTrue(configSource.Contains("\"EnableManualTestCountOverride\": false", StringComparison.Ordinal), "project config should expose the manual test count override disabled by default");
    AssertTrue(overrideMethod.Contains("AppSettings.IsVsDebugProfile", StringComparison.Ordinal), "manual test count override should only be effective in VS Debug/dev profile");
    AssertTrue(overrideMethod.Contains("AppSettings.GoblinTracker.EnableManualTestCountOverride", StringComparison.Ordinal), "manual test count override should require the explicit setting");
    AssertTrue(sessionStatsSource.Contains("ManualTestCountOverrideFreshObservationBypass", StringComparison.Ordinal), "manual test count override should log when it bypasses the fresh-evidence gate");
    AssertTrue(sessionStatsSource.Contains("respectsBlockListAndAreaLimits=True", StringComparison.Ordinal), "manual test count override log should make clear that normal protections still apply");
    AssertTrue(recordMethod.IndexOf("GoblinManualCountBlockList.IsBlocked(area.AreaKey)", StringComparison.Ordinal) < recordMethod.IndexOf("ManualTestCountOverrideFreshObservationBypass", StringComparison.Ordinal), "blocked areas should still be evaluated before the manual test count override can count");
    AssertTrue(recordMethod.IndexOf("portGoblinAreaDuplicateGuard.Peek(area.AreaKey)", StringComparison.Ordinal) < recordMethod.IndexOf("ManualTestCountOverrideFreshObservationBypass", StringComparison.Ordinal), "area-limit state should still be read before the manual test count override can count");
}

static void TestGoblinDecisionTraceLogsCountStaleBlockAndDuplicate()
{
    GoblinDecisionTraceRecord count = GoblinDecisionTracePolicy.Create(
        DateTime.UtcNow,
        "Live",
        "Journal",
        "",
        "",
        "Pandemonium Fortress Level 1",
        "Pandemonium Fortress Level 1",
        "Treasure Goblin",
        "sig",
        1,
        1,
        true,
        true,
        "",
        true,
        0,
        2,
        0);
    AssertEqual("Count", count.Decision, "fresh eligible evidence should trace Count");
    AssertEqual(2, count.AreaLimit, "PF1 traces should report areaLimit=2");
    AssertTrue(count.CorrelationId.StartsWith("gdt-", StringComparison.Ordinal), "decision traces should include a stable correlation id");
    AssertTrue(GoblinDecisionTracePolicy.ToLogLine(count).Contains("decision=Count", StringComparison.Ordinal), "trace log should include the Count decision");
    AssertTrue(GoblinDecisionTracePolicy.ToLogLine(count).Contains("correlationId=", StringComparison.Ordinal), "trace log should include the correlation id");

    GoblinDecisionTraceRecord stale = GoblinDecisionTracePolicy.Create(
        DateTime.UtcNow,
        "Live",
        "Journal",
        "",
        "",
        "Caverns of Frost Level 2",
        "Caverns of Frost Level 2",
        "Gem Hoarder",
        "sig",
        50,
        50,
        true,
        true,
        "EvidenceSeenBeforeAutoCountEnabled",
        false,
        0,
        1,
        0);
    AssertEqual("Stale", stale.Decision, "evidence predating auto-count should trace Stale");
    AssertTrue(stale.EvidencePredatesAutoCount, "trace should flag evidencePredatesAutoCount");
    AssertEqual("EvidenceSeenBeforeAutoCountEnabled", stale.Reason, "stale pre-arm trace should keep the exact reason");

    GoblinDecisionTraceRecord block = GoblinDecisionTracePolicy.Create(
        DateTime.UtcNow,
        "Replay",
        "Minimap",
        "New Tristram Minimap.png",
        "Debug\\GoblinEvidence\\New Tristram Minimap.png",
        "New Tristram",
        "New Tristram",
        "Treasure Goblin",
        "sig",
        1,
        1,
        true,
        true,
        "BlockedArea",
        false,
        0,
        0,
        0);
    AssertEqual("Block", block.Decision, "blocked areas should trace Block");
    AssertFalse(block.AllowedArea, "blocked areas should not trace as allowed");
    AssertEqual("BlockedArea", block.BlockedReason, "blocked trace should keep the block reason");

    GoblinDecisionTraceRecord duplicate = GoblinDecisionTracePolicy.Create(
        DateTime.UtcNow,
        "Live",
        "Journal",
        "",
        "",
        "Pandemonium Fortress Level 2",
        "Pandemonium Fortress Level 2",
        "Blood Thief",
        "sig",
        1,
        1,
        true,
        true,
        "AreaLimitReached",
        false,
        2,
        2,
        2);
    AssertEqual("Duplicate", duplicate.Decision, "area limit suppressions should trace Duplicate");
    AssertEqual(2, duplicate.AreaLimit, "PF2 duplicate trace should report areaLimit=2");
}

static void TestGoblinReplayToolIsDryRunAndPackaged()
{
    string repoRoot = FindRepositoryRootForTests();
    string evidenceSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs"));
    string packageScript = File.ReadAllText(Path.Combine(repoRoot, "Scripts", "create-debug-package.ps1"));
    string configSource = File.ReadAllText(Path.Combine(repoRoot, "Config", "AppSettings.json"));
    string appSettingsSource = File.ReadAllText(Path.Combine(repoRoot, "AppSettings.cs"));
    string sessionStatsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.cs"));
    string automationSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.PortedAutomation.cs.cs"));
    string releaseSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.Release.cs"));
    string replayCliScript = File.ReadAllText(Path.Combine(repoRoot, "Scripts", "replay-goblin-evidence.ps1"));
    string createReviewCloseMethod = ExtractMethodBody(evidenceSource, "private void PortCreateGoblinReplayReviewFilesOnVsDebugClose");
    string createReviewFilesMethod = ExtractMethodBody(evidenceSource, "private GoblinReplayReviewFilesResult PortCreateGoblinReplayReviewFilesForReview");
    string writeReviewFilesMethod = ExtractMethodBody(evidenceSource, "private GoblinReplayReviewFilesResult PortWriteGoblinReplayReviewFiles");
    string runReplayForReviewMethod = ExtractMethodBody(evidenceSource, "private GoblinReplaySummary? PortRunGoblinReplayForReview");

    AssertTrue(appSettingsSource.Contains("public bool EnableDecisionTrace { get; set; } = false", StringComparison.Ordinal), "Release decision trace should default off unless Debug Mode enables it");
    AssertTrue(appSettingsSource.Contains("settings.GoblinTracker.EnableDecisionTrace = true", StringComparison.Ordinal), "VS Debug/dev defaults should enable decision trace");
    AssertTrue(configSource.Contains("\"EnableDecisionTrace\": true", StringComparison.Ordinal), "project VS Debug config should explicitly expose decision trace");
    AssertTrue(evidenceSource.Contains("PortReplayGoblinEvidenceFolder", StringComparison.Ordinal), "replay folder runner should exist");
    AssertFalse(automationSource.Contains("Text = \"Review Files\"", StringComparison.Ordinal), "VS Debug troubleshooting should no longer expose a manual Review Files button");
    AssertTrue(automationSource.Contains("PortCreateGoblinReplayReviewFilesOnVsDebugClose();", StringComparison.Ordinal), "VS Debug close should create loose review files without prompting for replay input");
    AssertFalse(automationSource.Contains("txtGoblinScenarioArea", StringComparison.Ordinal), "VS Debug package creation should not require scenario area text input");
    AssertFalse(automationSource.Contains("txtGoblinScenarioGoblin", StringComparison.Ordinal), "VS Debug package creation should not require expected goblin text input");
    AssertFalse(automationSource.Contains("txtGoblinScenarioExpected", StringComparison.Ordinal), "VS Debug package creation should not require expected outcome text input");
    AssertFalse(releaseSource.Contains("PortCreateGoblinReplayReviewFilesFromButton", StringComparison.Ordinal), "Release form should not wire the removed VS Debug loose review button");
    AssertFalse(releaseSource.Contains("Create Package", StringComparison.Ordinal), "Release form package UI should remain unchanged unless explicitly requested");
    AssertFalse(createReviewCloseMethod.Contains("OpenFileDialog", StringComparison.Ordinal), "close-time review should not ask for a debug package ZIP");
    AssertFalse(createReviewCloseMethod.Contains("FolderBrowserDialog", StringComparison.Ordinal), "close-time review should not ask for a folder");
    AssertFalse(createReviewCloseMethod.Contains("InputBox", StringComparison.Ordinal), "close-time review should not ask for freeform debug metadata");
    AssertFalse(createReviewCloseMethod.Contains("PortCreateDebugPackage", StringComparison.Ordinal), "VS Debug close-time review should not invoke ZIP package creation");
    AssertTrue(createReviewCloseMethod.Contains("PortCreateGoblinReplayReviewFilesForReview(nextTestsPath, \"FormClosing\")", StringComparison.Ordinal), "close-time review should publish loose review files immediately");
    AssertTrue(createReviewCloseMethod.Contains("PortWriteGoblinTrackerNextTestMetadata", StringComparison.Ordinal), "close-time review should persist Next Tests checkbox state before replay");
    AssertTrue(evidenceSource.Contains("GoblinTrackerNextTests.txt", StringComparison.Ordinal), "VS Debug review file creation should write automatic Next Tests metadata");
    AssertTrue(evidenceSource.Contains("PortNextTestStepMetadataLines()", StringComparison.Ordinal), "Next Tests metadata should be generated from the in-app checklist");
    AssertFalse(evidenceSource.Contains("PortWriteGoblinTrackerReviewScenarioMetadata", StringComparison.Ordinal), "legacy scenario metadata writer should be removed");
    AssertTrue(createReviewFilesMethod.Contains("PortRunGoblinReplayForReview(source)", StringComparison.Ordinal), "review file creation should run Goblin replay before publishing loose files");
    AssertTrue(createReviewFilesMethod.Contains("PortWriteGoblinReplayReviewFiles(replaySummary, nextTestsPath)", StringComparison.Ordinal), "review file creation should publish the fresh replay outputs into the loose review folder");
    AssertTrue(evidenceSource.Contains("\"Debug\", \"GoblinReplayReview\"", StringComparison.Ordinal), "loose review files should be written under a stable Debug/GoblinReplayReview folder");
    AssertTrue(writeReviewFilesMethod.Contains("Latest", StringComparison.Ordinal), "loose review files should refresh a stable Latest folder");
    AssertTrue(writeReviewFilesMethod.Contains("goblin-tracker-review.html", StringComparison.Ordinal), "loose review files should include a root review index");
    AssertTrue(writeReviewFilesMethod.Contains("goblin-tracker-summary.txt", StringComparison.Ordinal), "loose review files should include a root summary");
    AssertTrue(writeReviewFilesMethod.Contains("goblin-tracker-next-tests.txt", StringComparison.Ordinal), "loose review files should include Next Tests metadata at the root");
    AssertTrue(writeReviewFilesMethod.Contains("PortCopyGoblinEncounterReviewCrops", StringComparison.Ordinal), "loose review files should include bounded encounter journal/minimap crops");
    AssertTrue(evidenceSource.Contains("EncounterCaptureFullscreenExcluded", StringComparison.Ordinal), "loose review metadata should report fullscreen encounter captures excluded from review");
    AssertTrue(evidenceSource.Contains("_Fullscreen", StringComparison.Ordinal), "VS Debug encounter capture should save fullscreen evidence locally");
    AssertTrue(evidenceSource.Contains("_Minimap", StringComparison.Ordinal), "VS Debug encounter capture should save minimap evidence");
    AssertTrue(evidenceSource.Contains("_Journal", StringComparison.Ordinal), "VS Debug encounter capture should save journal evidence");
    AssertTrue(evidenceSource.Contains("ZipCreated=False", StringComparison.Ordinal), "loose review metadata should make clear that no ZIP was created");
    AssertTrue(runReplayForReviewMethod.Contains("PortReplayGoblinEvidenceFolder(replayInputPath)", StringComparison.Ordinal), "review files button should invoke the replay engine");
    AssertTrue(runReplayForReviewMethod.Contains("ReviewGoblinReplayStarted", StringComparison.Ordinal), "review replay should log startup");
    AssertTrue(runReplayForReviewMethod.Contains("ReviewGoblinReplayComplete", StringComparison.Ordinal), "review replay should log completion");
    AssertTrue(runReplayForReviewMethod.Contains("ReviewGoblinReplaySkipped", StringComparison.Ordinal), "review replay should clearly log skipped states");
    AssertTrue(evidenceSource.Contains("return DebugManager.GoblinEvidenceDirectory", StringComparison.Ordinal), "review replay should self-discover current runtime GoblinEvidence input");
    AssertTrue(evidenceSource.Contains("PortResolveDebugPackageRuntimeRoot", StringComparison.Ordinal), "debug package creation should resolve the correct runtime root");
    AssertTrue(evidenceSource.Contains("AppSettings.IsVsDebugProfile && PortTryResolveConfigRoot", StringComparison.Ordinal), "VS Debug packaging should use the project-root config parent");
    AssertTrue(evidenceSource.Contains("Path.Combine(packageRuntimeRoot, \"DebugPackages\")", StringComparison.Ordinal), "debug package creation should log/discover the package folder from the resolved runtime root");
    AssertTrue(evidenceSource.Contains("SearchOption.AllDirectories", StringComparison.Ordinal), "replay should recursively scan debug package folders");
    AssertTrue(evidenceSource.Contains("PortExtractGoblinReplayZip", StringComparison.Ordinal), "replay should scan ZIP debug packages");
    AssertTrue(evidenceSource.Contains("ZipFile.ExtractToDirectory", StringComparison.Ordinal), "replay ZIP support should extract packages into a bounded temporary workspace");
    AssertTrue(evidenceSource.Contains("PortDetectBestGoblinEvidenceTemplateInImageFile", StringComparison.Ordinal), "replay should run template detection against saved images without Diablo");
    AssertTrue(evidenceSource.Contains("dryRun=True", StringComparison.Ordinal), "replay should be logged as a dry run");
    AssertTrue(evidenceSource.Contains("GoblinReplay_", StringComparison.Ordinal), "replay should write deterministic GoblinReplay logs");
    AssertTrue(evidenceSource.Contains("PortWriteGoblinReplayHtmlReport", StringComparison.Ordinal), "replay should write an HTML report");
    AssertTrue(evidenceSource.Contains("PortWriteGoblinReplayDecisionArtifacts", StringComparison.Ordinal), "replay should write package-friendly summaries and decision bundles");
    AssertTrue(evidenceSource.Contains("GoblinReplay_{timestamp}_summary.txt", StringComparison.Ordinal), "replay should write a grouped decision summary file");
    AssertTrue(evidenceSource.Contains("GoblinReplay_{timestamp}_changed.txt", StringComparison.Ordinal), "replay should write a changed-decision summary file");
    AssertTrue(evidenceSource.Contains("GoblinReplay_{timestamp}_bundles", StringComparison.Ordinal), "replay should write per-observation bundle folders");
    AssertTrue(evidenceSource.Contains("ChangedCount=", StringComparison.Ordinal), "replay changed summaries should identify changed decisions");
    AssertTrue(evidenceSource.Contains("GoblinReplayCandidateRanking", StringComparison.Ordinal), "replay should log ranked candidate matches");
    AssertTrue(evidenceSource.Contains("GoblinReplayAreaInference", StringComparison.Ordinal), "replay should log area inference decisions");
    AssertTrue(evidenceSource.Contains("replayComparison", StringComparison.Ordinal), "replay should compare decisions to the previous replay log");
    AssertTrue(evidenceSource.Contains("create-debug-package.ps1", StringComparison.Ordinal), "ZIP package creation should remain available for release/export workflows");
    AssertTrue(evidenceSource.Contains("process.StartInfo.ArgumentList.Add(\"-RuntimeRoot\")", StringComparison.Ordinal), "ZIP package creation should still pass the active runtime root");
    AssertTrue(evidenceSource.Contains("ReviewDebugPackageComplete", StringComparison.Ordinal), "ZIP package creation should still log the package result when explicitly used");
    AssertTrue(evidenceSource.Contains("PortExtractDebugPackagePathFromOutput", StringComparison.Ordinal), "ZIP package creation should still parse the generated package path when explicitly used");
    AssertTrue(evidenceSource.Contains("GoblinDecisionTracePolicy.ToLogLine(trace)", StringComparison.Ordinal), "replay should write structured decision trace lines");
    AssertFalse(ExtractMethodBody(evidenceSource, "private GoblinReplaySummary PortReplayGoblinEvidenceFolder").Contains("RecordGoblinFound(", StringComparison.Ordinal), "replay dry-run should not increment live GoblinCount");
    AssertTrue(sessionStatsSource.Contains("PortWriteGoblinDecisionBundle(trace)", StringComparison.Ordinal), "live decision traces should write evidence bundles");
    AssertTrue(sessionStatsSource.Contains("GoblinDecisionBundleSaved", StringComparison.Ordinal), "live decision bundles should log their saved folder");
    AssertTrue(packageScript.Contains("GoblinReplay_*.log", StringComparison.Ordinal), "debug packages should include replay logs");
    AssertTrue(packageScript.Contains("GoblinReplay_*.html", StringComparison.Ordinal), "debug packages should include replay HTML reports");
    AssertTrue(packageScript.Contains("GoblinReplay_*_summary.txt", StringComparison.Ordinal), "debug packages should include replay summary files");
    AssertTrue(packageScript.Contains("GoblinReplay_*_changed.txt", StringComparison.Ordinal), "debug packages should include changed-decision summary files");
    AssertTrue(packageScript.Contains("GoblinReplay_*_bundles", StringComparison.Ordinal), "debug packages should include replay decision bundle folders");
    AssertTrue(packageScript.Contains("GoblinTrackerNextTests.txt", StringComparison.Ordinal), "debug packages should include VS Debug Next Tests metadata when available");
    AssertTrue(packageScript.Contains("goblin-tracker-next-tests.txt", StringComparison.Ordinal), "debug packages should include root Next Tests metadata for package review");
    AssertFalse(packageScript.Contains("goblin-tracker-scenario.txt", StringComparison.Ordinal), "debug packages should not depend on legacy scenario input metadata");
    AssertTrue(packageScript.Contains("goblin-tracker-summary.txt", StringComparison.Ordinal), "debug packages should include a root Goblin Tracker review summary");
    AssertTrue(packageScript.Contains("goblin-tracker-review.html", StringComparison.Ordinal), "debug packages should include a root review index");
    AssertTrue(packageScript.Contains("$($replayReport.BaseName)_files", StringComparison.Ordinal), "debug packages should include replay report thumbnail assets");
    AssertTrue(packageScript.Contains("Logs\\GoblinReplay", StringComparison.Ordinal), "replay logs should have a stable package destination");
    AssertTrue(replayCliScript.Contains("Goblin replay CLI summary", StringComparison.Ordinal), "terminal replay runner should write a concise CLI summary");
    AssertTrue(replayCliScript.Contains("GoblinFarmer_Debug_*.zip", StringComparison.Ordinal), "terminal replay runner should self-discover the latest debug package");
    AssertTrue(replayCliScript.Contains("ChangedDecisionSummaries", StringComparison.Ordinal), "terminal replay runner should surface changed-decision summaries");
    AssertTrue(replayCliScript.Contains("DecisionBundles", StringComparison.Ordinal), "terminal replay runner should surface per-observation bundles");
    AssertTrue(File.Exists(Path.Combine(repoRoot, "Scripts", "replay-goblin-evidence.ps1")), "terminal Goblin replay runner should be tracked in Scripts");
}

static void TestGoblinAutomaticCountingRequiresFreshArmedEvidence()
{
    string repoRoot = FindRepositoryRootForTests();
    string sessionStatsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.cs"));
    string evidenceSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs"));
    string automationSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.PortedAutomation.cs.cs"));
    string observeMethod = ExtractMethodBody(sessionStatsSource, "private bool PortObserveGoblinCandidate");
    string autoCountMethod = ExtractMethodBody(sessionStatsSource, "private bool PortTryRecordAutomaticGoblinCount");

    AssertTrue(automationSource.Contains("portGoblinAutoCountEvidenceBySignature", StringComparison.Ordinal), "automatic counting should remember evidence signatures");
    AssertTrue(evidenceSource.Contains("PortGoblinEvidenceSignature(candidate)", StringComparison.Ordinal), "journal/minimap candidates should carry a stable evidence signature");
    AssertTrue(evidenceSource.Contains("PortGoblinEvidenceNoteValue(candidate.Notes, \"Template\")", StringComparison.Ordinal), "evidence signatures should include the template name");
    AssertTrue(evidenceSource.Contains("PortGoblinEvidenceNoteValue(candidate.Notes, \"Kind\")", StringComparison.Ordinal), "evidence signatures should include the evidence kind");
    AssertTrue(evidenceSource.Contains("PortGoblinEvidenceJournalLineBucket(candidate.Notes)", StringComparison.Ordinal), "journal evidence signatures should include a stable journal-row bucket");
    AssertTrue(sessionStatsSource.Contains("GoblinAreaResolver.NormalizedKey(observation.AreaKey)", StringComparison.Ordinal), "automatic evidence signatures should be scoped by resolved area key");
    AssertTrue(sessionStatsSource.Contains("PortGoblinAutoCountGlobalEvidenceKey", StringComparison.Ordinal), "automatic counting should also track the underlying journal row independent of current area");
    AssertFalse(ExtractMethodBody(evidenceSource, "private static string PortGoblinEvidenceSignature").Contains("MatchPoint", StringComparison.Ordinal), "evidence signatures should not include volatile match points");
    AssertFalse(ExtractMethodBody(evidenceSource, "private static string PortGoblinEvidenceSignature").Contains("candidate.Notes.Trim()", StringComparison.Ordinal), "evidence signatures should not include the whole diagnostic note string");
    AssertTrue(autoCountMethod.IndexOf("portGoblinAutoCountEvidenceBySignature[autoEvidenceKey] = evidenceState", StringComparison.Ordinal) < autoCountMethod.IndexOf("AutomaticCountingDisabled", StringComparison.Ordinal), "auto-count evidence should be remembered before the disabled gate returns");
    AssertTrue(autoCountMethod.Contains("EvidenceSeenBeforeAutoCountEnabled", StringComparison.Ordinal), "automatic counting should suppress evidence seen before the auto-count gate was armed");
    AssertTrue(autoCountMethod.Contains("EvidenceAlreadyAutoCounted", StringComparison.Ordinal), "automatic counting should suppress the same evidence signature after it counts once");
    AssertFalse(autoCountMethod.Contains("PortAllowsLinkedJournalAreaRepeat", StringComparison.Ordinal), "automatic counting should not bypass exact evidence suppression for linked Caverns levels");
    AssertTrue(autoCountMethod.Contains("EncounterAlreadyAutoCounted", StringComparison.Ordinal), "automatic counting should suppress the same counted journal row when it appears again in another area");
    AssertTrue(autoCountMethod.Contains("PortShouldSuppressJournalEncounterAlreadyAutoCounted(observation, area, globalEvidenceKey", StringComparison.Ordinal), "automatic counting should use evidence-row protection in addition to area-scoped exact signatures");
    AssertTrue(observeMethod.Contains("EncounterAlreadyAutoCounted", StringComparison.Ordinal), "observation summaries should report cross-area journal repeats as not countable before auto-count attempts run");
    AssertTrue(observeMethod.Contains("PortShouldPreserveDisplayedObservationAgainstIncoming", StringComparison.Ordinal), "suppressed old journal repeats should not replace the Last Observation display");
    AssertTrue(automationSource.Contains("portGoblinAutoCountEncounterByGoblinType", StringComparison.Ordinal), "automatic counting should remember recently counted goblin types across source/template variants");
    AssertTrue(automationSource.Contains("PortAutomaticGoblinJournalEncounterSuppressWindow", StringComparison.Ordinal), "cross-area journal suppression should use an explicit bounded window");
    AssertTrue(sessionStatsSource.Contains("encounterState.EvidenceKey", StringComparison.Ordinal), "journal repeat suppression should require the same underlying evidence key, not just the same goblin type");
    AssertTrue(sessionStatsSource.Contains("!string.Equals(encounterState.EvidenceKey, globalEvidenceKey", StringComparison.Ordinal), "journal repeat suppression should compare against the area-independent evidence key");
    AssertTrue(autoCountMethod.Contains("StaleEvidence", StringComparison.Ordinal), "automatic counting should suppress stale evidence signatures");
    AssertTrue(autoCountMethod.Contains("PortShowSplash($\"Goblin auto-counted", StringComparison.Ordinal), "automatic counting should show a visible notification when it increments");
    AssertTrue(sessionStatsSource.Contains("PortResetGoblinAutoCountEvidenceState(\"TrackerStatsReset\")", StringComparison.Ordinal), "Reset Stats should clear auto-count evidence signatures");
    AssertTrue(sessionStatsSource.Contains("PortResetGoblinAutoCountEvidenceState(\"NewGameCreated\")", StringComparison.Ordinal), "New Game should clear auto-count evidence signatures");
    AssertTrue(evidenceSource.Contains("clearedAutoCountEvidence", StringComparison.Ordinal), "evidence-state reset logs should report auto-count signature clearing");
    AssertTrue(evidenceSource.Contains("clearedAutoCountEncounters", StringComparison.Ordinal), "evidence-state reset logs should report auto-count encounter clearing");
}

static void TestGoblinAcceptedManualCountUpdatesLastObservationDisplay()
{
    string repoRoot = FindRepositoryRootForTests();
    string sessionStatsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.cs"));
    string publishMethod = ExtractMethodBody(sessionStatsSource, "private void PortPublishManualGoblinCountObservation");
    string clearMethod = ExtractMethodBody(sessionStatsSource, "private void PortMarkGoblinObservationNoCurrent");

    AssertTrue(sessionStatsSource.Contains("PortPublishManualGoblinCountObservation(area, goblinType, source, guardResult)", StringComparison.Ordinal), "accepted manual counts should publish the Last Observation display immediately");
    AssertTrue(publishMethod.Contains("ManualCountAccepted", StringComparison.Ordinal), "manual count display should identify the accepted-count state");
    AssertTrue(publishMethod.Contains("\"Counted\"", StringComparison.Ordinal), "manual count display should show Counted as the Last Observation reason");
    AssertTrue(publishMethod.Contains("PortManualGoblinCountDisplayHold", StringComparison.Ordinal), "manual count display should be held long enough to be visible");
    AssertTrue(publishMethod.Contains("LastObservationUiRefreshRequested", StringComparison.Ordinal), "manual count display should log an immediate UI refresh request");
    AssertFalse(publishMethod.Contains("RecordGoblinObservation", StringComparison.Ordinal), "manual count display updates should not increment observation-only counters");
    AssertTrue(clearMethod.Contains("LastObservationClearSkipped", StringComparison.Ordinal), "no-candidate scanner clears should be skipped during the manual-count display hold");
    AssertTrue(clearMethod.Contains("PortShouldPreserveDisplayedGoblinObservation", StringComparison.Ordinal), "Last Observation clearing should preserve recent accepted manual counts briefly");
    AssertTrue(sessionStatsSource.Contains("PortShouldPreserveDisplayedManualCountObservation", StringComparison.Ordinal), "shared Last Observation clearing should still preserve recent accepted manual counts briefly");
    AssertTrue(sessionStatsSource.Contains("LastObservationPersistent", StringComparison.Ordinal), "shared Last Observation clearing should keep the last real goblin visible until another goblin or reset updates it");
    AssertTrue(sessionStatsSource.Contains("LastObservationUpdateSkippedPreserved", StringComparison.Ordinal), "suppressed stale cross-area observations should not overwrite the displayed Last Observation");
    AssertTrue(sessionStatsSource.Contains("AreaAlreadyCounted", StringComparison.Ordinal), "duplicate same-area observations should not overwrite the displayed Last Observation");
    AssertTrue(sessionStatsSource.Contains("EvidenceAlreadyAutoCounted", StringComparison.Ordinal), "already-counted evidence should not overwrite the displayed Last Observation");
    AssertTrue(sessionStatsSource.Contains("StaleEvidence", StringComparison.Ordinal), "stale evidence should not overwrite the displayed Last Observation");
    AssertTrue(sessionStatsSource.Contains("LastObservationUpdateSkippedDuringManualHold", StringComparison.Ordinal), "scanner observation updates should not overwrite accepted manual counts during the display hold");
    AssertTrue(sessionStatsSource.Contains("PortManualCountDisplayHoldActive", StringComparison.Ordinal), "manual count display hold priority should be shared by clear and update paths");
    AssertTrue(sessionStatsSource.Contains("PortClearDisplayedGoblinObservationAfterConfirmedAreaChange", StringComparison.Ordinal), "confirmed area changes should clear stale previous-area Last Observation displays");
    AssertTrue(sessionStatsSource.Contains("ConfirmedAreaChanged", StringComparison.Ordinal), "confirmed area changes should log Last Observation area synchronization");
}

static void TestGoblinStaleJournalFreshnessPolicySuppressesOldVisibleLines()
{
    DateTime now = DateTime.UtcNow;
    TimeSpan window = TimeSpan.FromSeconds(45);
    DateTime firstSeen = now - TimeSpan.FromSeconds(46);
    DateTime recentSeen = now - TimeSpan.FromSeconds(10);

    AssertFalse(GoblinJournalFreshnessPolicy.IsFresh(firstSeen, now, window), "journal Engaged lines older than the freshness window should be stale");
    AssertTrue(GoblinJournalFreshnessPolicy.StaleSuppressionActive(recentSeen, now, window), "recently stale-suppressed signatures should stay suppressed while still visible");
    AssertFalse(GoblinJournalFreshnessPolicy.StaleSuppressionActive(now - TimeSpan.FromSeconds(90), now, window), "stale suppression should expire after the line disappears long enough");

    GoblinJournalEngagedState matchingEngaged = new("Treasure Goblin", "Cathedral Level 1", recentSeen);
    GoblinJournalEngagedState wrongAreaEngaged = new("Treasure Goblin", "Highlands Cave", recentSeen);
    AssertTrue(GoblinJournalFreshnessPolicy.KilledHasRecentEngaged(matchingEngaged, "Cathedral Level 1", now, window), "Killed journal evidence should be accepted after a recent same-area Engaged line");
    AssertFalse(GoblinJournalFreshnessPolicy.KilledHasRecentEngaged(wrongAreaEngaged, "Cathedral Level 1", now, window), "Killed journal evidence should ignore stale/wrong-area Engaged lines");
    AssertFalse(GoblinJournalFreshnessPolicy.KilledHasRecentEngaged(null, "Cathedral Level 1", now, window), "Killed journal evidence should be ignored without a recent Engaged anchor");

    string repoRoot = FindRepositoryRootForTests();
    string evidenceSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs"));
    string signatureMethod = ExtractMethodBody(evidenceSource, "private string PortJournalEvidenceLineSignature");
    AssertTrue(evidenceSource.Contains("PortJournalEvidenceLineSignature", StringComparison.Ordinal), "journal freshness should use a line signature, not current area as the freshness key");
    AssertTrue(evidenceSource.Contains("JournalEngagedIgnoredAreaChanged", StringComparison.Ordinal), "Engaged journal lines first seen in another area should log an area-change suppression");
    AssertTrue(evidenceSource.Contains("GoblinJournalFreshnessPolicy.EngagedIsFresh", StringComparison.Ordinal), "Engaged journal evidence should use area-strict freshness before being accepted");
    AssertFalse(signatureMethod.Contains("PortDisplayLocation", StringComparison.Ordinal), "journal line freshness signatures must not include current area, or old visible lines can become fresh after moving");
    AssertTrue(signatureMethod.Contains("LineBucket", StringComparison.Ordinal), "journal line freshness signatures should include a coarse row bucket so later legitimate same-template lines can be fresh");
    AssertTrue(signatureMethod.Contains("PortJournalEvidenceLineBucket(match.MatchPoint)", StringComparison.Ordinal), "journal line freshness should bucket the match row instead of using an exact volatile point");
    AssertFalse(signatureMethod.Contains("ScreenMatchPoint", StringComparison.Ordinal), "journal line freshness signatures should not use absolute screen coordinates");
    AssertFalse(evidenceSource.Contains("nowUtc - state.LastSeenUtc > GoblinJournalEvidenceFreshWindow", StringComparison.Ordinal), "Killed journal first-seen state should not reset just because the same visible line matched again later");
}

static void TestGoblinFreshKilledJournalCanSatisfyEvidenceGate()
{
    DateTime now = DateTime.UtcNow;
    TimeSpan window = TimeSpan.FromSeconds(45);
    GoblinJournalKilledState freshKilled = new("Rainbow Goblin", "Cave Of The Moon Clan Level 1", now.AddSeconds(-3), now);
    GoblinJournalKilledState staleKilled = freshKilled with { FirstSeenUtc = now.AddSeconds(-60), LastSeenUtc = now };
    GoblinJournalKilledState wrongAreaKilled = freshKilled with { AreaKey = "WhimsyDale" };

    AssertTrue(
        GoblinJournalFreshnessPolicy.KilledIsFresh(freshKilled, "Cave Of The Moon Clan Level 1", now, window),
        "fresh same-area Killed journal evidence should satisfy the evidence gate");
    AssertFalse(
        GoblinJournalFreshnessPolicy.KilledIsFresh(staleKilled, "Cave Of The Moon Clan Level 1", now, window),
        "stale Killed journal evidence should not satisfy the evidence gate");
    AssertFalse(
        GoblinJournalFreshnessPolicy.KilledIsFresh(wrongAreaKilled, "Cave Of The Moon Clan Level 1", now, window),
        "Killed journal evidence from another area should not satisfy the manual evidence gate");
    AssertFalse(
        GoblinJournalFreshnessPolicy.KilledIsFresh(freshKilled, "", now, window),
        "Killed journal evidence should require a resolved current area");
}

static void TestGoblinRefreshLogsFreshAndStaleKilledJournalDecisions()
{
    string repoRoot = FindRepositoryRootForTests();
    string evidenceSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs"));

    AssertTrue(evidenceSource.Contains("JournalKilledAcceptedFreshObservation", StringComparison.Ordinal), "continuous observation scans should log fresh Killed journal acceptance");
    AssertTrue(evidenceSource.Contains("JournalKilledAcceptedFreshManual", StringComparison.Ordinal), "manual refresh should log fresh Killed journal acceptance");
    AssertTrue(evidenceSource.Contains("JournalKilledIgnoredStale", StringComparison.Ordinal), "stale Killed journal lines should log a stale suppression reason");
    AssertTrue(evidenceSource.Contains("freshKilledWithoutEngagedReason: \"Observation\"", StringComparison.Ordinal), "continuous observation scans should opt into fresh Killed journal acceptance");
    AssertTrue(evidenceSource.Contains("freshKilledWithoutEngagedReason: \"Manual\"", StringComparison.Ordinal), "manual refresh should opt into fresh Killed journal acceptance");
    AssertTrue(evidenceSource.Contains("clearedJournalKilled", StringComparison.Ordinal), "Reset Stats/New Game should clear Killed journal freshness state");
}

static void TestGoblinResetClearsStaleObservationState()
{
    string repoRoot = FindRepositoryRootForTests();
    string evidenceSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs"));
    string resetMethod = ExtractMethodBody(evidenceSource, "private void PortResetGoblinEvidenceObservationState");

    AssertTrue(resetMethod.Contains("portJournalEvidenceSeenByKey.Clear()", StringComparison.Ordinal), "Reset Stats/New Game should clear journal first-seen state");
    AssertTrue(resetMethod.Contains("portStaleSuppressedJournalEvidenceByKey.Clear()", StringComparison.Ordinal), "Reset Stats/New Game should clear stale journal suppression state");
    AssertTrue(resetMethod.Contains("portJournalKilledEvidenceSeenBySignature.Clear()", StringComparison.Ordinal), "Reset Stats/New Game should clear Killed journal signatures");
    AssertTrue(resetMethod.Contains("portLastGoblinObservationForManualCount = null", StringComparison.Ordinal), "Reset Stats/New Game should clear manual observation reuse state");
    AssertTrue(resetMethod.Contains("portDisplayedGoblinObservation = null", StringComparison.Ordinal), "Reset Stats/New Game should clear Last Observation display state");
    AssertTrue(resetMethod.Contains("portDisplayedGoblinObservationStickyUntilUtc = DateTime.MinValue", StringComparison.Ordinal), "Reset Stats/New Game should clear manual-count display hold state");
}

static void TestGoblinManualNoFreshGatePreservesBlockedAreaPriority()
{
    string repoRoot = FindRepositoryRootForTests();
    string sessionStatsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.cs"));
    int blockedIndex = sessionStatsSource.IndexOf("suppressionReason = \"BlockedArea\"", StringComparison.Ordinal);
    int noFreshIndex = sessionStatsSource.IndexOf("suppressionReason = \"NoFreshObservation\"", StringComparison.Ordinal);

    AssertTrue(blockedIndex >= 0, "manual block-list suppression should still exist");
    AssertTrue(noFreshIndex >= 0, "manual no-fresh suppression should exist");
    AssertTrue(blockedIndex < noFreshIndex, "blocked areas should suppress before the no-fresh Unknown gate, even if evidence exists");
}

static void TestSalvageLoopUsesBoundedConfirmationWaitAndTimingLogs()
{
    string repoRoot = FindRepositoryRootForTests();
    string townSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.Town.cs"));

    AssertTrue(townSource.Contains("PortSalvageConfirmationTimeoutMs = 100", StringComparison.Ordinal), "salvage confirmation wait should be bounded below the previous 160ms fast probe");
    AssertTrue(townSource.Contains("PortSalvageConfirmationFastAttempts = 3", StringComparison.Ordinal), "salvage should use fewer fast confirmation scans in the common no-dialog case");
    AssertTrue(townSource.Contains("PortSalvageConfirmationFastDelayMs = 30", StringComparison.Ordinal), "salvage confirmation scan spacing should stay short but explicit");
    AssertTrue(townSource.Contains("PortWaitForSalvageConfirmationFast", StringComparison.Ordinal), "salvage should use a fast confirmation probe instead of the repair polling wait helper");
    AssertTrue(townSource.Contains("confirmationScans=", StringComparison.Ordinal), "salvage should log confirmation scan counts for timing diagnosis");
    AssertTrue(townSource.Contains("PortSalvagePostSlotDelayMs = 35", StringComparison.Ordinal), "salvage post-slot delay should be short but explicit");
    AssertTrue(townSource.Contains("PortSafeSalvageSlotClick", StringComparison.Ordinal), "salvage slot clicks should use the faster slot-only safe click helper");
    AssertTrue(townSource.Contains("slotClickSent=", StringComparison.Ordinal), "salvage should log whether the slot click was sent");
    AssertTrue(townSource.Contains("Salvage timing: slotIndex=", StringComparison.Ordinal), "salvage should log per-slot timing");
    AssertTrue(townSource.Contains("Salvage timing summary:", StringComparison.Ordinal), "salvage should log a summary timing line");
}

static void TestKadalaHotkeyUsesFasterCadenceAndTimingLogs()
{
    string repoRoot = FindRepositoryRootForTests();
    string hotkeysSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.Hotkeys.cs"));

    AssertTrue(hotkeysSource.Contains("const int kadalaClickIntervalMs = 60", StringComparison.Ordinal), "Kadala hotkey should click faster than the previous 100ms cadence");
    AssertTrue(hotkeysSource.Contains("const int kadalaClickHoldMs = 15", StringComparison.Ordinal), "Kadala hotkey should use a short explicit right-click hold");
    AssertTrue(hotkeysSource.Contains("Kadala timing: started", StringComparison.Ordinal), "Kadala hotkey should log timing at start");
    AssertTrue(hotkeysSource.Contains("Kadala timing: stopped", StringComparison.Ordinal), "Kadala hotkey should log timing at stop");
    AssertTrue(hotkeysSource.Contains("Kadala timing: active", StringComparison.Ordinal), "Kadala hotkey should log throttled active timing");
}

static void TestTeleportNextNoRouteStateNotifiesUser()
{
    string repoRoot = FindRepositoryRootForTests();
    string hotkeysSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.Hotkeys.cs"));
    string routingSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.TeleportRouting.cs"));
    string buttonSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.ButtonColors.cs"));

    AssertTrue(hotkeysSource.Contains("Teleport Next hotkey ignored: no queued/next teleport", StringComparison.Ordinal), "no-route Teleport Next state should still log the ignored hotkey");
    AssertTrue(hotkeysSource.Contains("Teleport Next skipped", StringComparison.Ordinal), "no-route Teleport Next state should show a player-visible notification");
    AssertTrue(hotkeysSource.Contains("No queued route target for current location.", StringComparison.Ordinal), "no-route Teleport Next notification should explain the route state");
    AssertTrue(File.ReadAllText(Path.Combine(repoRoot, "frmMain.Combat.cs")).Contains("CombatHotkeyStartAfterArrivalConfirmationCancel", StringComparison.Ordinal), "combat hotkey should start combat after cancelling an active arrival-confirmation wait");
    AssertTrue(File.ReadAllText(Path.Combine(repoRoot, "frmMain.Combat.cs")).Contains("PortStartCombatFromHotkey(\"hotkey-after-arrival-confirmation-cancel\", true)", StringComparison.Ordinal), "combat startup should be retried from the original hotkey after the cancelled teleport workflow unwinds");
    AssertTrue(routingSource.Contains("Ancient Waterway main area blocks hotkey teleportation to Stinging Winds", StringComparison.Ordinal), "plain Ancient Waterway should block the queued Stinging Winds Teleport Next hop");
    AssertTrue(routingSource.Contains("Eastern Channel Level 2 allows hotkey teleportation to Stinging Winds", StringComparison.Ordinal), "Eastern Channel Level 2 should continue to Stinging Winds");
    AssertTrue(routingSource.Contains("Western Channel Level 2 should return to Ancient Waterway, not Stinging Winds", StringComparison.Ordinal), "Western Channel Level 2 should not skip back to Stinging Winds");
    AssertTrue(routingSource.Contains("Already inside Ancient Waterway; Ancient Waterway button is blocked", StringComparison.Ordinal), "the Ancient Waterway waypoint button should still block when already inside Ancient Waterway");
    AssertTrue(buttonSource.Contains("ButtonClickReceived", StringComparison.Ordinal), "route button clicks should log receipt before any workflow gate can make them look silent");
    AssertTrue(buttonSource.Contains("ButtonClickQueued", StringComparison.Ordinal), "route button clicks should log when they are queued into the workflow runner");
    AssertTrue(buttonSource.Contains("ButtonClickExecuting", StringComparison.Ordinal), "route button clicks should log when the workflow body starts executing");
    AssertFalse(buttonSource.Contains("PortShowSplash($\"Teleport queued", StringComparison.Ordinal), "accepted route button clicks should not show the intrusive Teleport queued overlay during Goblin Tracker validation");
    AssertTrue(buttonSource.Contains("ButtonClickQueuedFeedbackSuppressed", StringComparison.Ordinal), "suppressed route button queued feedback should be logged for package diagnosis");
    AssertTrue(File.ReadAllText(Path.Combine(repoRoot, "frmMain.PortedAutomation.cs.cs")).Contains("ButtonClickAlreadyAtTargetFeedbackShown", StringComparison.Ordinal), "button clicks that short-circuit as already at target should show/log visible feedback");
}

static void TestGoblinAreaDetectionDisambiguatesPandemoniumFalsePositivesFromRouteContext()
{
    AssertDisambiguates(
        "Pandemonium Fortress Level 1",
        0.999,
        "Western Channel Level 1",
        0.996,
        "Ancient Waterway",
        "Western Channel Level 1",
        "ChannelVsPandemonium");

    AssertDisambiguates(
        "Pandemonium Fortress Level 2",
        0.999,
        "Western Channel Level 2",
        0.991,
        "Ancient Waterway",
        "Western Channel Level 2",
        "ChannelVsPandemonium");

    AssertDisambiguates(
        "Pandemonium Fortress Level 1",
        1.000,
        "Caverns of Frost Level 1",
        0.995,
        "Battlefields",
        "Caverns of Frost Level 1",
        "CavernsVsPandemonium");

    AssertDisambiguates(
        "Pandemonium Fortress Level 2",
        0.999,
        "Caverns of Frost Level 2",
        0.995,
        "Battlefields",
        "Caverns of Frost Level 2",
        "CavernsVsPandemonium");

    AssertDisambiguates(
        "Pandemonium Fortress Level 1",
        0.997,
        "Western Channel Level 1",
        0.994,
        "Pandemonium Fortress Level 1",
        "Pandemonium Fortress Level 1",
        "ChannelVsPandemonium");

    AssertDisambiguates(
        "Pandemonium Fortress Level 1",
        1.000,
        "Cave Of The Moon Clan Level 1",
        0.998,
        "Southern Highlands",
        "Cave Of The Moon Clan Level 1",
        "MoonClanVsPandemonium");

    AssertDisambiguates(
        "Pandemonium Fortress Level 1",
        0.999,
        "Cathedral Level 1",
        0.988,
        "Cathedral Level 1",
        "Cathedral Level 1",
        "CathedralVsPandemonium");

    AssertDisambiguates(
        "Pandemonium Fortress Level 2",
        0.999,
        "Cathedral Level 2",
        0.987,
        "Cathedral Level 2",
        "Cathedral Level 2",
        "CathedralVsPandemonium");
}

static void TestGoblinAreaDetectionUsesStrongRouteContextRunnerUp()
{
    GoblinAreaDetectionDisambiguationResult result = GoblinAreaDetectionDisambiguator.Disambiguate(
        "Pandemonium Fortress Level 1",
        0.960,
        "Western Channel Level 1",
        0.887,
        "Ancient Waterway");

    AssertTrue(result.Ambiguous, "strong channel runner-up should be treated as route-context resolvable");
    AssertFalse(result.Blocked, "Ancient Waterway route context should resolve the PF/channel false positive");
    AssertEqual("Western Channel Level 1", result.SelectedLocation, "route context should select Western Channel Level 1");
    AssertEqual("RouteContext", result.Reason, "strong runner-up correction should be explicit");

    result = GoblinAreaDetectionDisambiguator.Disambiguate(
        "Pandemonium Fortress Level 1",
        0.960,
        "Western Channel Level 1",
        0.650,
        "Ancient Waterway");

    AssertFalse(result.Ambiguous, "weak runner-up should not be corrected by route context alone");
    AssertEqual("Pandemonium Fortress Level 1", result.SelectedLocation, "weak runner-up case should leave the best match unchanged");
}

static void TestGoblinAreaDetectionBlocksUnresolvedPandemoniumAmbiguity()
{
    GoblinAreaDetectionDisambiguationResult result = GoblinAreaDetectionDisambiguator.Disambiguate(
        "Pandemonium Fortress Level 1",
        0.999,
        "Western Channel Level 1",
        0.996,
        "");

    AssertTrue(result.Ambiguous, "known close PF/channel pair should be marked ambiguous");
    AssertTrue(result.Blocked, "unresolved PF-leading ambiguity should block rather than count");
    AssertEqual("", result.SelectedLocation, "blocked ambiguity should not select an area");
    AssertEqual("AmbiguousAreaDetection", result.Reason, "blocked ambiguity should explain suppression");

    result = GoblinAreaDetectionDisambiguator.Disambiguate(
        "Western Channel Level 2",
        0.999,
        "Pandemonium Fortress Level 2",
        0.991,
        "");

    AssertTrue(result.Ambiguous, "known close channel/PF pair should be marked ambiguous");
    AssertFalse(result.Blocked, "non-PF best match should remain countable");
    AssertEqual("Western Channel Level 2", result.SelectedLocation, "non-PF best match should be preserved");
    AssertEqual("BestNonPandemonium", result.Reason, "non-PF best disambiguation should be explicit");

    result = GoblinAreaDetectionDisambiguator.Disambiguate(
        "Pandemonium Fortress Level 1",
        1.000,
        "Cave Of The Moon Clan Level 1",
        0.998,
        "");

    AssertTrue(result.Ambiguous, "known close Moon Clan/PF pair should be marked ambiguous");
    AssertTrue(result.Blocked, "unresolved PF-leading Moon Clan ambiguity should block rather than count");
    AssertEqual("AmbiguousAreaDetection", result.Reason, "blocked Moon Clan ambiguity should explain suppression");
}

static void TestGoblinAreaDetectionFalsePandemoniumMatchesDoNotConsumePandemoniumSlots()
{
    GoblinAreaDuplicateGuard guard = new();

    AcceptDisambiguatedArea(
        guard,
        "Pandemonium Fortress Level 1",
        0.999,
        "Western Channel Level 1",
        0.996,
        "Ancient Waterway",
        "Western Channel Level 1");
    AssertFalse(guard.TryAccept("Western Channel Level 1"), "disambiguated Western Channel Level 1 should suppress second same-area press");

    AcceptDisambiguatedArea(
        guard,
        "Pandemonium Fortress Level 1",
        1.000,
        "Caverns of Frost Level 1",
        0.995,
        "Battlefields",
        "Caverns of Frost Level 1");
    AssertFalse(guard.TryAccept("Caverns of Frost Level 1"), "disambiguated Caverns Level 1 should suppress second same-area press");

    AcceptDisambiguatedArea(
        guard,
        "Pandemonium Fortress Level 1",
        1.000,
        "Cave Of The Moon Clan Level 1",
        0.998,
        "Southern Highlands",
        "Cave Of The Moon Clan Level 1");
    AssertFalse(guard.TryAccept("Cave Of The Moon Clan Level 1"), "disambiguated Moon Clan Level 1 should suppress second same-area press");

    AcceptDisambiguatedArea(
        guard,
        "Pandemonium Fortress Level 1",
        0.999,
        "Cathedral Level 1",
        0.988,
        "Cathedral Level 1",
        "Cathedral Level 1");
    AssertFalse(guard.TryAccept("Cathedral Level 1"), "disambiguated Cathedral Level 1 should suppress second same-area press");

    AcceptDisambiguatedArea(
        guard,
        "Pandemonium Fortress Level 2",
        0.999,
        "Cathedral Level 2",
        0.987,
        "Cathedral Level 2",
        "Cathedral Level 2");
    AssertFalse(guard.TryAccept("Cathedral Level 2"), "disambiguated Cathedral Level 2 should suppress second same-area press");

    AssertTrue(guard.TryAccept("Pandemonium Fortress Level 1"), "false PF1 matches should not consume PF1 first slot");
    AssertTrue(guard.TryAccept("Pandemonium Fortress Level 1"), "false PF1 matches should not consume PF1 second slot");
    AssertFalse(guard.TryAccept("Pandemonium Fortress Level 1"), "PF1 third count should still be suppressed");
    AssertTrue(guard.TryAccept("Pandemonium Fortress Level 2"), "false PF2 matches should not consume PF2 first slot");
    AssertTrue(guard.TryAccept("Pandemonium Fortress Level 2"), "false PF2 matches should not consume PF2 second slot");
    AssertFalse(guard.TryAccept("Pandemonium Fortress Level 2"), "PF2 third count should still be suppressed");
}

static void AssertDisambiguates(
    string bestName,
    double bestConfidence,
    string secondName,
    double secondConfidence,
    string routeContext,
    string expectedSelected,
    string expectedGroup)
{
    GoblinAreaDetectionDisambiguationResult result = GoblinAreaDetectionDisambiguator.Disambiguate(
        bestName,
        bestConfidence,
        secondName,
        secondConfidence,
        routeContext);

    AssertTrue(result.Ambiguous, "known close PF ambiguity should be marked ambiguous");
    AssertFalse(result.Blocked, "route context should resolve the ambiguity");
    AssertEqual(expectedSelected, result.SelectedLocation, "route context should select the expected real area");
    AssertEqual(expectedGroup, result.AmbiguityGroup, "ambiguity group should identify the confusing title family");
    AssertEqual("RouteContext", result.Reason, "route context should be the disambiguation reason");
}

static void AcceptDisambiguatedArea(
    GoblinAreaDuplicateGuard guard,
    string bestName,
    double bestConfidence,
    string secondName,
    double secondConfidence,
    string routeContext,
    string expectedSelected)
{
    GoblinAreaDetectionDisambiguationResult result = GoblinAreaDetectionDisambiguator.Disambiguate(
        bestName,
        bestConfidence,
        secondName,
        secondConfidence,
        routeContext);

    AssertFalse(result.Blocked, "test setup should disambiguate instead of block");
    AssertEqual(expectedSelected, result.SelectedLocation, "test setup selected area mismatch");
    AssertTrue(guard.TryAccept(result.SelectedLocation), $"{expectedSelected} first count should be accepted");
}

static void TestGoblinTrackerRecordsAllowUnknownManualFallbackAndReset()
{
    DiagnosticsSessionState state = new();
    int count = state.RecordGoblinFound(new GoblinFoundRecord(
        "",
        "",
        "Unknown",
        "ManualHotkey",
        DateTime.UtcNow,
        true,
        ""));

    AssertEqual(1, count, "unknown manual fallback should still increment");
    DiagnosticsSessionSnapshot snapshot = state.Snapshot(DateTime.Now);
    AssertEqual(1, snapshot.GoblinCount, "snapshot should include manual fallback count");
    AssertEqual(1, snapshot.GoblinFoundRecordCount, "snapshot should include manual fallback record");
    AssertEqual(0, snapshot.CountedGoblinAreaCount, "unknown fallback should not create a counted area key");
    AssertEqual("", snapshot.LastCountedGoblinAreaKey, "unknown fallback should not report a counted area key");

    state.RecordGoblinFoundRecord(new GoblinFoundRecord(
        "Cathedral Level 1",
        "Cathedral Level 1",
        "Unknown",
        "MinimapCandidate",
        DateTime.UtcNow,
        false,
        "AreaAlreadyCounted"));
    snapshot = state.Snapshot(DateTime.Now);
    AssertEqual(1, snapshot.GoblinCount, "suppressed duplicate should not increment count");
    AssertEqual(2, snapshot.GoblinFoundRecordCount, "suppressed duplicate should stay in memory for troubleshooting");

    state.ResetGoblinTrackerStats();
    snapshot = state.Snapshot(DateTime.Now);
    AssertEqual(0, snapshot.GoblinCount, "tracker reset should clear count");
    AssertEqual(0, snapshot.GoblinFoundRecordCount, "tracker reset should clear in-memory found records");
}

static void AssertUnsafe(Point point, Rectangle diabloRect, string message)
{
    AssertFalse(CombatClickSafety.CombatMouseClickIsSafe(point, diabloRect), message);
}

static void AssertAnySafe(IEnumerable<Point> points, Rectangle diabloRect, string message)
{
    if (!points.Any(point => CombatClickSafety.CombatMouseClickIsSafe(point, diabloRect)))
    {
        throw new InvalidOperationException(message);
    }
}

static IEnumerable<Point> OutsideCandidates(Rectangle rectangle)
{
    int middleX = rectangle.Left + rectangle.Width / 2;
    int middleY = rectangle.Top + rectangle.Height / 2;

    yield return new Point(rectangle.Right, middleY);
    yield return new Point(rectangle.Left - 1, middleY);
    yield return new Point(middleX, rectangle.Bottom);
    yield return new Point(middleX, rectangle.Top - 1);
    yield return new Point(rectangle.Right, rectangle.Bottom);
    yield return new Point(rectangle.Left - 1, rectangle.Top - 1);
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertFalse(bool condition, string message)
{
    AssertTrue(!condition, message);
}

static void AssertEqual<T>(T expected, T actual, string message)
    where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message}: expected={expected}; actual={actual}");
    }
}

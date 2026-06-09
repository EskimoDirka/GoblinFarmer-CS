using System.Drawing;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using GoblinFarmer;

if (TryRunGoblinReplayCaptureCommand(args, out int replayCommandExitCode))
{
    return replayCommandExitCode;
}

int failures = 0;

Run("Python no-click rectangles use blacklist safety and Python boundaries", TestPythonNoClickRectangles);
Run("Unsafe cursor suppresses left clicks without stopping Demon Hunter key loop", TestUnsafeCursorDoesNotStopKeyLoop);
Run("Demon Hunter blocked-cursor fallback uses safe playfield point", TestDemonHunterBlockedCursorFallbackUsesSafePlayfieldPoint);
Run("Demon Hunter right mouse remains held after initial safe start", TestRightMouseRemainsHeldAfterSafeStart);
Run("Initial safe-wait timeout stops only when Python would stop", TestInitialSafeWaitTimeoutPolicy);
Run("Default AppSettings path/debug profile are launch-surface neutral", TestAppSettingsLaunchParityDefaults);
Run("VS Debug/dev profile suppresses first-run setup and forces internal debug defaults", TestVsDebugDevProfileDefaults);
Run("VS Debug/dev profile prefers project-root AppSettings", TestVsDebugProjectRootConfigPreferred);
Run("VS Debug/dev profile prefers ignored local AppSettings", TestVsDebugProjectLocalConfigPreferred);
Run("Missing Diablo path keeps startup in setup required", TestMissingDiabloPathKeepsStartupInSetupRequired);
Run("VS Debug blank project-root Diablo path attempts discovery", TestVsDebugBlankProjectRootDiabloPathAttemptsDiscovery);
Run("Diablo discovery finds custom drive root install", TestDiabloDiscoveryFindsCustomDriveRootInstall);
Run("Diablo discovery ignores launcher executable", TestDiabloDiscoveryIgnoresLauncherExecutable);
Run("Diablo discovery prefers 64-bit executable", TestDiabloDiscoveryPrefers64BitExecutable);
Run("Diablo discovery stays bounded to known roots", TestDiabloDiscoveryStaysBoundedToKnownRoots);
Run("Configured valid Diablo path wins over discovery", TestConfiguredValidDiabloPathWinsOverDiscovery);
Run("DebugManager profile helpers separate VS, release debug, and normal release", TestDebugManagerProfileHelpers);
Run("DebugManager retention cleanup only deletes matching artifacts", TestDebugManagerRetentionCleanupFilters);
Run("Debug package script applies 20 package retention", TestDebugPackageScriptAppliesPackageRetention);
Run("DebugManager age retention deletes old debug artifacts", TestDebugManagerAgeRetentionDeletesOldArtifacts);
Run("GoblinEvidence retention keeps newest 250 files", TestGoblinEvidenceRetentionKeepsNewest250Files);
Run("GoblinEvidence retention breaks timestamp ties by filename", TestGoblinEvidenceRetentionBreaksTimestampTiesByFilename);
Run("GoblinEvidence retention ignores missing folder", TestGoblinEvidenceRetentionMissingFolderDoesNotThrow);
Run("GoblinEvidence retention count less than one disables cleanup", TestGoblinEvidenceRetentionCountLessThanOneDisablesCleanup);
Run("GoblinEvidence retention deletes only inside GoblinEvidence", TestGoblinEvidenceRetentionDeletesOnlyInsideFolder);
Run("GoblinEvidence template discovery accepts per-goblin evidence files", TestGoblinEvidenceTemplateDiscoveryAcceptsPerGoblinEvidenceFiles);
Run("GoblinEvidence template discovery finds source image set", TestGoblinEvidenceTemplateDiscoveryFindsSourceImageSet);
Run("GoblinEvidence observation scan regions match calibration", TestGoblinEvidenceObservationScanRegionsMatchCalibration);
Run("Current location image resolver detects saved title templates", TestCurrentLocationImageResolverDetectsSavedTitleTemplates);
Run("Goblin replay fixture frame source reaches candidate detection", TestGoblinReplayFixtureFrameSourceReachesCandidateDetection);
Run("Goblin replay explicit fixture runner detects saved encounter frames", TestGoblinReplayExplicitFixtureRunnerDetectsSavedEncounterFrames);
Run("Goblin replay suppresses delayed journal after minimap count", TestGoblinReplaySuppressesDelayedJournalAfterMinimapCount);
Run("Goblin replay suppresses Moon Clan Level 1 evidence after Level 2 transition", TestGoblinReplaySuppressesMoonClanLevelOneEvidenceAfterLevelTwoTransition);
Run("Goblin replay suppresses Battlefields journal history evidence", TestGoblinReplaySuppressesBattlefieldsJournalHistoryEvidence);
Run("Goblin replay capture folder loader suppresses old area evidence after transition", TestGoblinReplayCaptureFolderLoaderSuppressesOldAreaEvidenceAfterTransition);
Run("Goblin replay capture folder loader suppresses journal history rows", TestGoblinReplayCaptureFolderLoaderSuppressesJournalHistoryRows);
Run("Goblin replay capture folder loader reports missing folders clearly", TestGoblinReplayCaptureFolderLoaderReportsMissingFoldersClearly);
Run("Goblin replay metadata-file loader replays a specific capture", TestGoblinReplayMetadataFileLoaderReplaysSpecificCapture);
Run("Goblin replay capture-prefix loader replays a specific capture", TestGoblinReplayCapturePrefixLoaderReplaysSpecificCapture);
Run("Goblin replay prefix loader reports missing metadata clearly", TestGoblinReplayCapturePrefixLoaderReportsMissingMetadataClearly);
Run("Goblin replay decision-bundle loader reports available evidence", TestGoblinReplayDecisionBundleLoaderReportsAvailableEvidence);
Run("Goblin replay decision-bundle loader can resolve replay-ready prefix", TestGoblinReplayDecisionBundleLoaderCanResolveReplayReadyPrefix);
Run("Goblin replay decision-bundle loader reads local replay crops", TestGoblinReplayDecisionBundleLoaderReadsLocalReplayCrops);
Run("Goblin replay template scenario suppresses reset carryover journal evidence", TestGoblinReplayTemplateScenarioSuppressesResetCarryoverJournalEvidence);
Run("Goblin replay template scenario suppresses stale journal row variants", TestGoblinReplayTemplateScenarioSuppressesStaleJournalRowVariants);
Run("Goblin replay template scenario suppresses late New Game journal carryover", TestGoblinReplayTemplateScenarioSuppressesLateNewGameJournalCarryover);
Run("Goblin replay template scenario uses current-location resolver", TestGoblinReplayTemplateScenarioUsesCurrentLocationResolver);
Run("Goblin replay template scenario allows fresh minimap after stale cross-area journal", TestGoblinReplayTemplateScenarioAllowsFreshMinimapAfterStaleCrossAreaJournal);
Run("Goblin replay template scenario waits for killed confirmation after engaged journal", TestGoblinReplayTemplateScenarioWaitsForKilledConfirmationAfterEngagedJournal);
Run("Goblin replay template scenario lets strong minimap override pending engaged journal", TestGoblinReplayTemplateScenarioLetsStrongMinimapOverridePendingEngagedJournal);
Run("Goblin replay template scenario counts sustained active engaged journal", TestGoblinReplayTemplateScenarioCountsSustainedActiveEngagedJournal);
Run("Goblin replay template scenario allows PF same-signature second minimap", TestGoblinReplayTemplateScenarioAllowsPandemoniumSameSignatureSecondMinimap);
Run("Goblin replay capture folder command remains harness-only", TestGoblinReplayCaptureFolderCommandRemainsHarnessOnly);
Run("Installed/release profile with missing paths still requires first-run setup", TestReleaseProfileRequiresSetupWhenMissingPaths);
Run("Release Goblin Tracker layout keeps observation fields separated", TestReleaseGoblinTrackerLayoutKeepsObservationFieldsSeparated);
Run("VS Debug diagnostics omit next test steps tab", TestVsDebugDiagnosticsOmitNextTestStepsTab);
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
Run("Debug package includes built-in analysis reports", TestDebugPackageIncludesBuiltInAnalysisReports);
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
Run("Goblin tracker stats UI refreshes after count changes", TestGoblinTrackerStatsUiRefreshesAfterCountChanges);
Run("Goblin observation mode is enabled by default in Release", TestGoblinObservationModeEnabledByDefaultInRelease);
Run("Goblin automatic counting gate defaults disabled", TestGoblinAutomaticCountingGateDefaultsDisabled);
Run("Goblin VS Debug automatic-count settings are form-toggleable", TestGoblinVsDebugAutomaticCountSettingsAreFormToggleable);
Run("Goblin VS Debug recognition Capture button is manual-only", TestGoblinVsDebugRecognitionCaptureButtonIsManualOnly);
Run("Goblin VS Debug simulation controls use count guards", TestGoblinVsDebugSimulationControlsUseCountGuards);
Run("Goblin VS Debug simulation area list covers route and blocked areas", TestGoblinVsDebugSimulationAreaListCoversRouteAndBlockedAreas);
Run("Goblin decision trace logs count stale block and duplicate", TestGoblinDecisionTraceLogsCountStaleBlockAndDuplicate);
Run("Debug package batch uses live evidence only", TestDebugPackageBatchUsesLiveEvidenceOnly);
Run("Goblin automatic counting requires fresh armed evidence", TestGoblinAutomaticCountingRequiresFreshArmedEvidence);
Run("Goblin automatic count reliability requires killed or minimap confirmation", TestGoblinAutomaticCountReliabilityRequiresKilledOrMinimapConfirmation);
Run("Goblin auto-count source variant suppression uses recent last-seen state", TestGoblinAutoCountSourceVariantSuppressionUsesRecentLastSeenState);
Run("Goblin PF multi-count duplicate bypass stays bounded", TestGoblinPandemoniumMultiCountDuplicateBypassStaysBounded);
Run("Goblin auto-count minimap collision allows new areas", TestGoblinAutoCountMinimapCollisionAllowsNewAreas);
Run("Goblin auto-count delayed journal after minimap suppresses", TestGoblinAutoCountDelayedJournalAfterMinimapSuppresses);
Run("Goblin auto-count stale journal does not block fresh cross-area minimap", TestGoblinAutoCountStaleJournalDoesNotBlockFreshCrossAreaMinimap);
Run("Goblin auto-count same-area duplicate journal refreshes encounter state", TestGoblinAutoCountSameAreaDuplicateJournalRefreshesEncounterState);
Run("Goblin auto-count suppresses shifted journal row after pause", TestGoblinAutoCountSuppressesShiftedJournalRowAfterPause);
Run("Goblin auto-count treats different journal templates as separate lines", TestGoblinAutoCountTreatsDifferentJournalTemplatesAsSeparateLines);
Run("Goblin accepted manual count updates Last Observation display", TestGoblinAcceptedManualCountUpdatesLastObservationDisplay);
Run("Goblin accepted counts persist Last Observation until reset", TestGoblinAcceptedCountsPersistLastObservationUntilReset);
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
Run("Goblin journal PF area uses recent channel minimap context", TestGoblinJournalPandemoniumAreaUsesRecentChannelMinimapContext);
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

static void TestDemonHunterBlockedCursorFallbackUsesSafePlayfieldPoint()
{
    Rectangle diabloRect = new(0, 0, CombatClickSafety.ReferenceWidth, CombatClickSafety.ReferenceHeight);

    AssertTrue(CombatClickSafety.TryGetDemonHunterFallbackClickPoint(diabloRect, out Point fallbackPoint), "Demon Hunter should have a safe fallback click point");
    AssertTrue(diabloRect.Contains(fallbackPoint), "fallback point should remain inside Diablo window");
    AssertTrue(CombatClickSafety.CombatMouseClickIsSafe(fallbackPoint, diabloRect), "fallback point should avoid no-click UI regions");
    AssertTrue(DemonHunterCombatPolicy.ShouldUseFallbackClickWhileRightHeld(combatRunning: true, combatClass: "demon_hunter", diabloActive: true, rightMouseHeld: true, rightHeldFromSafeRegion: true), "fallback should be allowed for active Demon Hunter right-hold combat");
    AssertFalse(DemonHunterCombatPolicy.ShouldUseFallbackClickWhileRightHeld(combatRunning: true, combatClass: "monk", diabloActive: true, rightMouseHeld: true, rightHeldFromSafeRegion: true), "fallback should stay Demon Hunter-specific");
    AssertFalse(DemonHunterCombatPolicy.ShouldUseFallbackClickWhileRightHeld(combatRunning: true, combatClass: "demon_hunter", diabloActive: true, rightMouseHeld: true, rightHeldFromSafeRegion: false), "fallback should require right hold that started safely");
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

static void TestVsDebugProjectLocalConfigPreferred()
{
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.ConfigTests", Guid.NewGuid().ToString("N"));
    string projectConfigDirectory = Path.Combine(root, "Config");
    string binDirectory = Path.Combine(root, "bin", "Debug", "net10.0-windows");
    string localDiabloPath = Path.Combine(root, "Local Diablo", "Diablo III64.exe");
    string trackedDiabloPath = Path.Combine(root, "Tracked Diablo", "Diablo III64.exe");
    Directory.CreateDirectory(projectConfigDirectory);
    Directory.CreateDirectory(Path.GetDirectoryName(localDiabloPath)!);
    Directory.CreateDirectory(Path.GetDirectoryName(trackedDiabloPath)!);
    File.WriteAllText(localDiabloPath, "fake local exe for config test");
    File.WriteAllText(trackedDiabloPath, "fake tracked exe for config test");

    try
    {
        string projectFilePath = Path.Combine(root, "GoblinFarmer.csproj");
        string trackedConfigPath = Path.Combine(projectConfigDirectory, "AppSettings.json");
        string localConfigPath = Path.Combine(projectConfigDirectory, "AppSettings.local.json");
        File.WriteAllText(projectFilePath, "<Project />");
        File.WriteAllText(trackedConfigPath, """
            {
              "Runtime": {
                "DiabloExecutablePath": "%TRACKED_DIABLO_PATH%",
                "BattleNetExecutablePath": "",
                "ImagesRoot": "Images"
              }
            }
            """
            .Replace("%TRACKED_DIABLO_PATH%", EscapeJsonPath(trackedDiabloPath)));
        File.WriteAllText(localConfigPath, """
            {
              "Runtime": {
                "DiabloExecutablePath": "%LOCAL_DIABLO_PATH%",
                "BattleNetExecutablePath": "",
                "ImagesRoot": "Images"
              }
            }
            """
            .Replace("%LOCAL_DIABLO_PATH%", EscapeJsonPath(localDiabloPath)));

        AppSettings.LaunchProfileSnapshot snapshot = AppSettings.ResolveLaunchProfileForTests(
            explicitProfile: null,
            debuggerAttached: true,
            debugBuild: true,
            explicitConfigPath: null,
            baseDirectory: binDirectory);

        AssertEqual(Path.GetFullPath(localConfigPath), snapshot.ConfigPath, "VS Debug should prefer ignored AppSettings.local.json over tracked AppSettings.json when present");
        AssertTrue(snapshot.VsDebugProjectRootConfigUsed, "snapshot should still report project-root config resolution for the local override");

        AppSettings.SettingsModel? loaded = JsonSerializer.Deserialize<AppSettings.SettingsModel>(File.ReadAllText(snapshot.ConfigPath));
        AssertTrue(loaded != null, "local AppSettings should deserialize");
        loaded!.Normalize();
        AssertEqual(localDiabloPath, loaded.Runtime.DiabloExecutablePath, "VS Debug should load Diablo path from local AppSettings override");
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

static void TestDebugPackageScriptAppliesPackageRetention()
{
    string repoRoot = FindRepositoryRootForTests();
    string scriptSource = File.ReadAllText(Path.Combine(repoRoot, "Scripts", "create-debug-package.ps1"));

    AssertTrue(scriptSource.Contains("[int]$DebugPackageRetentionCount = 20", StringComparison.Ordinal), "debug package export script should default to retaining 20 packages");
    AssertTrue(scriptSource.Contains("Invoke-DebugPackageRetentionCleanup", StringComparison.Ordinal), "debug package export script should invoke retention cleanup after creating a package");
    AssertTrue(scriptSource.Contains("GoblinFarmer_Debug_*.zip", StringComparison.Ordinal), "debug package retention should target only GoblinFarmer debug ZIPs");
    AssertTrue(scriptSource.Contains("Debug package retention cleanup deleted:", StringComparison.Ordinal), "debug package retention should log deleted packages");
    AssertTrue(scriptSource.Contains("Debug package retention cleanup complete:", StringComparison.Ordinal), "debug package retention should log a cleanup summary");
}

static void TestDebugManagerAgeRetentionDeletesOldArtifacts()
{
    string repoRoot = FindRepositoryRootForTests();
    string appSettingsSource = File.ReadAllText(Path.Combine(repoRoot, "AppSettings.cs"));
    string programSource = File.ReadAllText(Path.Combine(repoRoot, "Program.cs"));
    string root = Path.Combine(Path.GetTempPath(), "GoblinFarmer.AgeRetentionTests", Guid.NewGuid().ToString("N"));
    string logs = Path.Combine(root, "Logs");
    string evidence = Path.Combine(root, "Debug", "GoblinEvidence", "DecisionBundles", "sample");
    Directory.CreateDirectory(logs);
    Directory.CreateDirectory(evidence);

    try
    {
        AssertFalse(appSettingsSource.Contains("TimeSpan.FromHours(24)", StringComparison.Ordinal), "VS Debug artifact retention should no longer be limited to 24 hours");
        AssertTrue(appSettingsSource.Contains("DebugArtifactRetentionAge => TimeSpan.FromDays(7)", StringComparison.Ordinal), "VS Debug and Release artifact retention should be 7 days");
        AssertTrue(appSettingsSource.Contains("RetentionDays => 7", StringComparison.Ordinal), "log and screenshot retention should also be 7 days");
        AssertTrue(programSource.Contains("DebugManager.CleanupDebugArtifactsByAge(AppSettings.DebugArtifactRetentionAge)", StringComparison.Ordinal), "startup should apply age-based debug artifact cleanup");

        string oldLog = Touch(Path.Combine(logs, "old.log"), TimeSpan.FromDays(-8));
        string newLog = Touch(Path.Combine(logs, "new.log"), TimeSpan.FromDays(-6));
        string oldEvidence = Touch(Path.Combine(evidence, "old-decision-trace.txt"), TimeSpan.FromDays(-8));
        string newEvidence = Touch(Path.Combine(evidence, "decision-trace.txt"), TimeSpan.FromMinutes(-10));

        CleanupResult result = DebugManager.CleanupOldFilesByAge(root, TimeSpan.FromDays(7), "test debug artifacts");

        AssertEqual(4, result.Scanned, "age cleanup should scan all debug files recursively");
        AssertEqual(2, result.Deleted, "age cleanup should delete files older than the retention window");
        AssertFalse(File.Exists(oldLog), "old log should be deleted after 7 days");
        AssertTrue(File.Exists(newLog), "new log should be kept inside the 7-day VS Debug window");
        AssertFalse(File.Exists(oldEvidence), "old GoblinEvidence diagnostic should be deleted after 7 days");
        AssertTrue(File.Exists(newEvidence), "new GoblinEvidence diagnostic should be kept");
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
    string autoCountSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.AutoCount.cs"));
    string evidenceModelSource = File.ReadAllText(Path.Combine(repoRoot, "GoblinEvidence.cs"));
    string autoCountCombinedSource = sessionSource + autoCountSource;

    AssertTrue(evidenceModelSource.Contains("EvidenceConfidence", StringComparison.Ordinal), "observation records should carry evidence confidence into auto-count decisions");
    AssertTrue(sessionSource.Contains("PortAutomaticGoblinMinimapCountMinimumConfidence = 0.85", StringComparison.Ordinal), "normal automatic minimap counts should accept strong evidence below the ambiguity-pair gate");
    AssertTrue(sessionSource.Contains("PortAutomaticGoblinAmbiguousMinimapCountMinimumConfidence = 0.90", StringComparison.Ordinal), "Gilded/Malevolent automatic minimap counts should keep the stricter ambiguity-pair gate");
    AssertTrue(autoCountCombinedSource.Contains("PortAutomaticGoblinMinimapCountMinimumConfidenceFor", StringComparison.Ordinal), "automatic minimap counts should use a goblin-type-specific confidence gate");
    AssertTrue(autoCountSource.Contains("MinimapConfidencePendingJournal", StringComparison.Ordinal), "low-confidence minimap auto-count attempts should suppress and wait for stronger evidence");
    AssertTrue(autoCountSource.Contains("minimapAutoCountMinConfidence", StringComparison.Ordinal), "auto-count diagnostics should report the minimap confidence gate");
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

static void TestCurrentLocationImageResolverDetectsSavedTitleTemplates()
{
    string repoRoot = FindRepositoryRootForTests();
    string locationRoot = Path.Combine(repoRoot, "Images", "Current Location");
    Dictionary<string, string> templates = CurrentLocationImageResolver.DiscoverTemplatePaths(locationRoot);
    string easternChannelPath = Path.Combine(locationRoot, "Eastern Channel Level 1.png");
    AssertTrue(File.Exists(easternChannelPath), "Eastern Channel Level 1 current-location template should exist");
    AssertTrue(templates.Count > 20, "current-location resolver should discover the source title templates");

    using Bitmap frame = new(easternChannelPath);
    CurrentLocationImageResolverResult result = CurrentLocationImageResolver.DetectFromBitmap(
        frame,
        templates,
        0.82);

    AssertEqual("Eastern Channel Level 1", result.Detected, "current-location resolver should detect the saved Eastern Channel Level 1 title template");
    AssertEqual("Eastern Channel Level 1", result.BestName, "current-location resolver should report the matching title template as best");
    AssertTrue(result.BestConfidence >= 0.99, $"current-location resolver should match the exact template with high confidence, actual={result.BestConfidence:0.000}");
}

static void TestGoblinReplayFixtureFrameSourceReachesCandidateDetection()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerFixture_{Guid.NewGuid():N}");
    Directory.CreateDirectory(fixtureRoot);
    try
    {
        string templatePath = Path.Combine(fixtureRoot, "Treasure Goblin Minimap.png");
        string framePath = Path.Combine(fixtureRoot, "fixture-minimap.png");
        using Bitmap template = CreateFixturePatternBitmap(9, 9);
        template.Save(templatePath);

        using (Bitmap frame = new(40, 40))
        using (Graphics graphics = Graphics.FromImage(frame))
        {
            graphics.Clear(Color.Black);
            graphics.DrawImageUnscaled(template, 14, 17);
            frame.Save(framePath);
        }

        FixtureGoblinEvidenceFrameSource frameSource = FixtureGoblinEvidenceFrameSource.FromJournalAndMinimap(null, framePath);
        GoblinEvidenceTemplateRequirement requirement = new(
            GoblinEvidenceType.MinimapIcon,
            "MinimapCandidate",
            Path.GetFileName(templatePath),
            0.95,
            "Treasure Goblin",
            GoblinEvidenceTemplateKind.Minimap);

        GoblinEvidenceCandidate? candidate = GoblinEvidenceFrameTemplateMatcher.TryDetectSingleTemplateCandidate(
            frameSource,
            requirement,
            templatePath,
            GoblinEvidenceScanRegions.MinimapReferenceRegion,
            out GoblinEvidenceTemplateMatch match);

        AssertTrue(candidate != null, "fixture minimap PNG should produce a candidate through the shared frame/template detection path");
        AssertEqual("Treasure Goblin", candidate!.GoblinType, "fixture candidate should preserve the template goblin type");
        AssertEqual("MinimapCandidate", candidate.Source, "fixture candidate should preserve the evidence source");
        AssertTrue(match.Confidence >= 0.99, $"fixture template should match the saved frame with high confidence, actual={match.Confidence:0.000}");
        AssertEqual(new Point(14, 17), match.MatchPoint, "fixture match point should be relative to the saved fixture frame");
        AssertEqual(match.MatchPoint, match.ScreenMatchPoint, "fixture screen match point should use fixture-local coordinates");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayExplicitFixtureRunnerDetectsSavedEncounterFrames()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayFixture_{Guid.NewGuid():N}");
    string templateRoot = Path.Combine(fixtureRoot, "templates");
    Directory.CreateDirectory(templateRoot);
    try
    {
        string templatePath = Path.Combine(templateRoot, "Gem Hoarder Minimap.png");
        string framePath = Path.Combine(fixtureRoot, "encounter-minimap.png");
        using Bitmap template = CreateFixturePatternBitmap(11, 11);
        template.Save(templatePath);

        using (Bitmap frame = new(48, 48))
        using (Graphics graphics = Graphics.FromImage(frame))
        {
            graphics.Clear(Color.Black);
            graphics.DrawImageUnscaled(template, 21, 13);
            frame.Save(framePath);
        }

        List<string> replayLogs = [];
        IGoblinEvidenceFrameSource? scopedFrameSource = null;
        bool fixtureFrameSourceInjected = false;
        GoblinReplayFixtureRunResult result = GoblinReplayFixtureRunner.RunExplicitFixtureForHarness(
            new GoblinReplayFixture("single saved minimap frame", null, framePath),
            templateRoot,
            replayLogs.Add,
            frameSource =>
            {
                scopedFrameSource = frameSource;
                fixtureFrameSourceInjected |= frameSource is FixtureGoblinEvidenceFrameSource;
            });

        AssertTrue(result.CandidateFound, "explicit replay runner should detect the saved minimap encounter frame");
        AssertEqual(1, result.Candidates.Count, "explicit replay runner should report the best candidate for the fixture source");
        GoblinReplayFixtureCandidate candidate = result.Candidates[0];
        AssertEqual("Gem Hoarder", candidate.GoblinType, "explicit replay candidate should preserve goblin type from template discovery");
        AssertEqual("MinimapCandidate", candidate.Source, "explicit replay candidate should preserve the minimap source");
        AssertTrue(candidate.Confidence >= 0.99, $"explicit replay candidate should have high confidence, actual={candidate.Confidence:0.000}");
        AssertEqual(new Point(21, 13), candidate.MatchPoint, "explicit replay candidate should report fixture-local match point");
        AssertTrue(fixtureFrameSourceInjected, "explicit replay runner should scope fixture frame source injection to the replay run");
        AssertTrue(scopedFrameSource == null, "explicit replay runner should restore the live/default frame source after replay");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayFixtureRunStarted", StringComparison.Ordinal) && line.Contains("mode=ExplicitOnDemand", StringComparison.Ordinal)), "explicit replay runner should log obvious start guard");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayFixtureFrameSourceRestored", StringComparison.Ordinal) && line.Contains("target=LiveDefault", StringComparison.Ordinal)), "explicit replay runner should log frame-source restoration");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplaySuppressesDelayedJournalAfterMinimapCount()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayMinimapJournal_{Guid.NewGuid():N}");
    string templateRoot = Path.Combine(fixtureRoot, "templates");
    Directory.CreateDirectory(templateRoot);
    try
    {
        string minimapTemplatePath = Path.Combine(templateRoot, "Treasure Goblin Minimap.png");
        string journalTemplatePath = Path.Combine(templateRoot, "Treasure Goblin Engaged Journal.png");
        string minimapFramePath = Path.Combine(fixtureRoot, "southern-minimap.png");
        string southernJournalFramePath = Path.Combine(fixtureRoot, "southern-journal.png");
        string caveJournalFramePath = Path.Combine(fixtureRoot, "cave-old-journal-shifted.png");
        using Bitmap minimapTemplate = CreateFixturePatternBitmap(11, 11);
        using Bitmap journalTemplate = CreateFixturePatternBitmap(13, 11);
        minimapTemplate.Save(minimapTemplatePath);
        journalTemplate.Save(journalTemplatePath);
        SaveFixtureFrame(minimapFramePath, 80, 80, minimapTemplate, new Point(21, 13));
        SaveFixtureFrame(southernJournalFramePath, 180, 380, journalTemplate, new Point(30, 335));
        SaveFixtureFrame(caveJournalFramePath, 180, 380, journalTemplate, new Point(30, 312));

        DateTime startUtc = new(2026, 6, 8, 2, 17, 7, DateTimeKind.Utc);
        List<string> replayLogs = [];
        GoblinReplayFixtureScenarioResult result = GoblinReplayFixtureRunner.RunExplicitScenarioForHarness(
            "Southern Highlands minimap then stale Cave journal",
            [
                new GoblinReplayFixtureStep(
                    "Southern Highlands fresh minimap",
                    new GoblinReplayFixture("southern minimap", null, minimapFramePath),
                    "Southern Highlands",
                    startUtc),
                new GoblinReplayFixtureStep(
                    "Southern Highlands matching journal",
                    new GoblinReplayFixture("southern journal", southernJournalFramePath, null),
                    "Southern Highlands",
                    startUtc.AddSeconds(1)),
                new GoblinReplayFixtureStep(
                    "Cave Level 1 old shifted journal",
                    new GoblinReplayFixture("cave old journal", caveJournalFramePath, null),
                    "Cave Of The Moon Clan Level 1",
                    startUtc.AddSeconds(185)),
            ],
            templateRoot,
            replayLogs.Add);

        AssertEqual(3, result.Steps.Count, "minimap/journal replay should evaluate all three steps");
        AssertTrue(result.Steps[0].Counted, "fresh Southern Highlands minimap evidence should count");
        AssertFalse(result.Steps[1].Counted, "matching Southern Highlands journal evidence should not double-count after minimap count");
        AssertEqual("EncounterAlreadyAutoCounted", result.Steps[1].Reason, "matching journal evidence should suppress as the same counted encounter");
        AssertFalse(result.Steps[2].Counted, "old Southern Highlands journal evidence must not count after moving to Cave Level 1");
        AssertEqual("EncounterAlreadyAutoCounted", result.Steps[2].Reason, "old Cave journal replay should remain attached to the counted Southern Highlands encounter");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayFixtureStepResult", StringComparison.Ordinal) && line.Contains("areaKey=Cave Of The Moon Clan Level 1", StringComparison.Ordinal) && line.Contains("reason=EncounterAlreadyAutoCounted", StringComparison.Ordinal)), "replay should log the Cave stale-journal suppression clearly");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplaySuppressesMoonClanLevelOneEvidenceAfterLevelTwoTransition()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayMoonClan_{Guid.NewGuid():N}");
    string templateRoot = Path.Combine(fixtureRoot, "templates");
    Directory.CreateDirectory(templateRoot);
    try
    {
        string templatePath = Path.Combine(templateRoot, "Treasure Goblin Killed Journal.png");
        string journalFramePath = Path.Combine(fixtureRoot, "moon-clan-level-one-journal.png");
        using Bitmap template = CreateFixturePatternBitmap(13, 11);
        template.Save(templatePath);
        SaveFixtureFrame(journalFramePath, 160, 340, template, new Point(24, 300));

        DateTime startUtc = new(2026, 6, 7, 12, 0, 0, DateTimeKind.Utc);
        List<string> replayLogs = [];
        GoblinReplayFixtureScenarioResult result = GoblinReplayFixtureRunner.RunExplicitScenarioForHarness(
            "Moon Clan Level 1 stale into Level 2",
            [
                new GoblinReplayFixtureStep(
                    "Level 1 fresh killed line",
                    new GoblinReplayFixture("level one journal", journalFramePath, null),
                    "Cave Of The Moon Clan Level 1",
                    startUtc),
                new GoblinReplayFixtureStep(
                    "Level 2 sees old Level 1 line",
                    new GoblinReplayFixture("level two old journal", journalFramePath, null),
                    "Cave Of The Moon Clan Level 2",
                    startUtc.AddSeconds(20)),
            ],
            templateRoot,
            replayLogs.Add);

        AssertEqual(2, result.Steps.Count, "Moon Clan replay should evaluate both fixture steps");
        AssertTrue(result.Steps[0].Counted, "fresh Level 1 fixture evidence should count in Level 1");
        AssertEqual("Eligible", result.Steps[0].Reason, "fresh Level 1 fixture evidence should be eligible");
        AssertFalse(result.Steps[1].Counted, "old Level 1 fixture evidence must not count as Level 2");
        AssertEqual("StaleEvidence", result.Steps[1].Reason, "old Level 1 fixture evidence should suppress as stale after Level 2 transition");
        AssertEqual("JournalKilledIgnoredStale", result.Steps[1].FreshnessReason, "Level 2 replay should use the journal killed stale-area reason");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayFixtureStepResult", StringComparison.Ordinal) && line.Contains("areaKey=Cave Of The Moon Clan Level 2", StringComparison.Ordinal) && line.Contains("staleFreshReason=JournalKilledIgnoredStale", StringComparison.Ordinal)), "Moon Clan replay should log the stale Level 2 step clearly");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplaySuppressesBattlefieldsJournalHistoryEvidence()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayBattlefields_{Guid.NewGuid():N}");
    string templateRoot = Path.Combine(fixtureRoot, "templates");
    Directory.CreateDirectory(templateRoot);
    try
    {
        string templatePath = Path.Combine(templateRoot, "Odious Collector Killed Journal.png");
        string activeJournalFramePath = Path.Combine(fixtureRoot, "fields-active-journal.png");
        string historyJournalFramePath = Path.Combine(fixtureRoot, "battlefields-history-journal.png");
        using Bitmap template = CreateFixturePatternBitmap(15, 11);
        template.Save(templatePath);
        SaveFixtureFrame(activeJournalFramePath, 180, 340, template, new Point(30, 300));
        SaveFixtureFrame(historyJournalFramePath, 180, 120, template, new Point(30, 32));

        DateTime startUtc = new(2026, 6, 7, 13, 0, 0, DateTimeKind.Utc);
        List<string> replayLogs = [];
        GoblinReplayFixtureScenarioResult result = GoblinReplayFixtureRunner.RunExplicitScenarioForHarness(
            "Battlefields journal history stale row",
            [
                new GoblinReplayFixtureStep(
                    "Fields of Slaughter fresh journal",
                    new GoblinReplayFixture("fields active journal", activeJournalFramePath, null),
                    "Fields of Slaughter",
                    startUtc),
                new GoblinReplayFixtureStep(
                    "Battlefields journal history row",
                    new GoblinReplayFixture("battlefields history journal", historyJournalFramePath, null),
                    "Battlefields",
                    startUtc.AddSeconds(15)),
            ],
            templateRoot,
            replayLogs.Add);

        AssertEqual(2, result.Steps.Count, "Battlefields replay should evaluate both fixture steps");
        AssertTrue(result.Steps[0].Counted, "fresh Fields of Slaughter journal evidence should count before the area transition");
        AssertFalse(result.Steps[1].Counted, "journal history row must not count after moving to Battlefields");
        AssertEqual("JournalCandidateIgnoredHistoryRow", result.Steps[1].Reason, "Battlefields history replay should keep the production history-row suppression reason");
        AssertEqual("JournalCandidateIgnoredHistoryRow", result.Steps[1].FreshnessReason, "Battlefields history replay should identify the stale/fresh reason");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayFixtureStepResult", StringComparison.Ordinal) && line.Contains("areaKey=Battlefields", StringComparison.Ordinal) && line.Contains("staleFreshReason=JournalCandidateIgnoredHistoryRow", StringComparison.Ordinal)), "Battlefields replay should log the history-row suppression clearly");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayCaptureFolderLoaderSuppressesOldAreaEvidenceAfterTransition()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayCaptureMoonClan_{Guid.NewGuid():N}");
    string templateRoot = Path.Combine(fixtureRoot, "templates");
    Directory.CreateDirectory(templateRoot);
    try
    {
        string templatePath = Path.Combine(templateRoot, "Rainbow Goblin Killed Journal.png");
        using Bitmap template = CreateFixturePatternBitmap(14, 12);
        template.Save(templatePath);

        DateTime startUtc = new(2026, 6, 7, 16, 0, 0, DateTimeKind.Utc);
        string levelOneCapture = CreateReplayCaptureFolder(
            fixtureRoot,
            "MoonClanLevel1Capture",
            "GoblinEncounter_20260607_160000_000_AutomaticObservation_Journal_RainbowGoblin_CaveOfTheMoonClanLevel1",
            "Cave Of The Moon Clan Level 1",
            startUtc,
            template,
            new Point(24, 300),
            180,
            340);
        string levelTwoCapture = CreateReplayCaptureFolder(
            fixtureRoot,
            "MoonClanLevel2OldVisibleCapture",
            "GoblinEncounter_20260607_160020_000_AutomaticObservation_Journal_RainbowGoblin_CaveOfTheMoonClanLevel2",
            "Cave Of The Moon Clan Level 2",
            startUtc.AddSeconds(20),
            template,
            new Point(24, 300),
            180,
            340);

        List<string> replayLogs = [];
        GoblinReplayCaptureFolderScenarioResult result = GoblinReplayFixtureRunner.RunExplicitCaptureFoldersForHarness(
            "Capture folder Moon Clan Level 1 stale into Level 2",
            [
                new GoblinReplayCaptureFolderStep("Level 1 real capture", levelOneCapture),
                new GoblinReplayCaptureFolderStep("Level 2 old visible real capture", levelTwoCapture),
            ],
            templateRoot,
            replayLogs.Add);

        AssertEqual(2, result.CaptureLoads.Count, "capture-folder replay should load both real-style folders");
        AssertTrue(result.CaptureLoads.All(load => load.Loaded), "both capture folders should load cleanly");
        AssertEqual(2, result.Steps.Count, "capture-folder replay should evaluate both loaded steps");
        AssertTrue(result.Steps[0].Counted, "fresh Level 1 capture evidence should count in Level 1");
        AssertFalse(result.Steps[1].Counted, "old Level 1 capture evidence must not count after Level 2 transition");
        AssertEqual("StaleEvidence", result.Steps[1].Reason, "old capture evidence should suppress as stale after area transition");
        AssertEqual("JournalKilledIgnoredStale", result.Steps[1].FreshnessReason, "capture-folder replay should reuse the shared journal killed stale-area reason");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayCaptureFolderLoaded", StringComparison.Ordinal) && line.Contains("areaKey=Cave Of The Moon Clan Level 2", StringComparison.Ordinal)), "capture-folder loader should log the resolved Level 2 area");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayFixtureStepResult", StringComparison.Ordinal) && line.Contains("staleFreshReason=JournalKilledIgnoredStale", StringComparison.Ordinal)), "capture-folder replay should log the stale decision");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayCaptureFolderLoaderSuppressesJournalHistoryRows()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayCaptureBattlefields_{Guid.NewGuid():N}");
    string templateRoot = Path.Combine(fixtureRoot, "templates");
    Directory.CreateDirectory(templateRoot);
    try
    {
        string templatePath = Path.Combine(templateRoot, "Treasure Goblin Killed Journal.png");
        using Bitmap template = CreateFixturePatternBitmap(16, 12);
        template.Save(templatePath);

        DateTime startUtc = new(2026, 6, 7, 17, 0, 0, DateTimeKind.Utc);
        string fieldsCapture = CreateReplayCaptureFolder(
            fixtureRoot,
            "FieldsActiveCapture",
            "GoblinEncounter_20260607_170000_000_AutomaticObservation_Journal_TreasureGoblin_FieldsOfSlaughter",
            "Fields of Slaughter",
            startUtc,
            template,
            new Point(30, 300),
            180,
            340);
        string battlefieldsCapture = CreateReplayCaptureFolder(
            fixtureRoot,
            "BattlefieldsHistoryCapture",
            "GoblinCapture_20260607_170015_000_VsDebugCaptureButton_Battlefields",
            "Battlefields",
            startUtc.AddSeconds(15),
            template,
            new Point(30, 32),
            180,
            120);

        List<string> replayLogs = [];
        GoblinReplayCaptureFolderScenarioResult result = GoblinReplayFixtureRunner.RunExplicitCaptureFoldersForHarness(
            "Capture folder Battlefields journal history row",
            [
                new GoblinReplayCaptureFolderStep("Fields active capture", fieldsCapture),
                new GoblinReplayCaptureFolderStep("Battlefields history capture", battlefieldsCapture),
            ],
            templateRoot,
            replayLogs.Add);

        AssertEqual(2, result.CaptureLoads.Count, "capture-folder history replay should load both folders");
        AssertTrue(result.CaptureLoads.All(load => load.Loaded), "history replay capture folders should load cleanly");
        AssertEqual(2, result.Steps.Count, "history replay should evaluate both loaded steps");
        AssertTrue(result.Steps[0].Counted, "active Fields journal row should count before the transition");
        AssertFalse(result.Steps[1].Counted, "Battlefields journal history row must not count");
        AssertEqual("JournalCandidateIgnoredHistoryRow", result.Steps[1].Reason, "history-row replay should preserve the production suppression reason");
        AssertEqual("JournalCandidateIgnoredHistoryRow", result.Steps[1].FreshnessReason, "history-row replay should identify the stale/fresh reason");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayCaptureFolderLoaded", StringComparison.Ordinal) && line.Contains("fixture=GoblinCapture_20260607_170015_000_VsDebugCaptureButton_Battlefields", StringComparison.Ordinal)), "loader should understand manual-capture style prefixes");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayCaptureFolderLoaderReportsMissingFoldersClearly()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayCaptureMissing_{Guid.NewGuid():N}");
    string templateRoot = Path.Combine(fixtureRoot, "templates");
    Directory.CreateDirectory(templateRoot);
    try
    {
        string missingCaptureFolder = Path.Combine(fixtureRoot, "MissingCaptureFolder");
        List<string> replayLogs = [];
        GoblinReplayCaptureFolderScenarioResult result = GoblinReplayFixtureRunner.RunExplicitCaptureFoldersForHarness(
            "Capture folder missing case",
            [new GoblinReplayCaptureFolderStep("Missing capture", missingCaptureFolder, "Battlefields", new DateTime(2026, 6, 7, 18, 0, 0, DateTimeKind.Utc))],
            templateRoot,
            replayLogs.Add);

        AssertEqual(1, result.CaptureLoads.Count, "missing-folder replay should return one load result");
        AssertFalse(result.CaptureLoads[0].Loaded, "missing capture folder should not load");
        AssertEqual("CaptureFolderMissing", result.CaptureLoads[0].Reason, "missing capture folder should report a clear harness reason");
        AssertEqual(0, result.Steps.Count, "missing capture folders should not create replay steps");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayCaptureFolderSkipped", StringComparison.Ordinal) && line.Contains("reason=CaptureFolderMissing", StringComparison.Ordinal)), "missing capture folder should be logged clearly");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayMetadataFileLoaderReplaysSpecificCapture()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayMetadata_{Guid.NewGuid():N}");
    string templateRoot = Path.Combine(fixtureRoot, "templates");
    Directory.CreateDirectory(templateRoot);
    try
    {
        string templatePath = Path.Combine(templateRoot, "Gem Hoarder Killed Journal.png");
        using Bitmap template = CreateFixturePatternBitmap(14, 12);
        template.Save(templatePath);

        DateTime timestampUtc = new(2026, 6, 7, 19, 2, 45, DateTimeKind.Utc);
        string captureFolder = CreateReplayCaptureFolder(
            fixtureRoot,
            "SharedEncounterCaptures",
            "GoblinEncounter_20260607_190245_000_AutomaticObservation_Journal_GemHoarder_Battlefields",
            "Battlefields",
            timestampUtc,
            template,
            new Point(30, 300),
            180,
            340);
        string metadataPath = Directory.GetFiles(captureFolder, "*_Metadata.txt").Single();

        List<string> replayLogs = [];
        GoblinReplayCaptureFolderScenarioResult result = GoblinReplayFixtureRunner.RunExplicitMetadataFilesForHarness(
            "Specific metadata replay",
            [new GoblinReplayCaptureFolderStep("Specific metadata", metadataPath)],
            templateRoot,
            replayLogs.Add);

        AssertEqual(1, result.CaptureLoads.Count, "metadata replay should return one load result");
        AssertTrue(result.CaptureLoads[0].Loaded, "specific metadata replay should load");
        AssertEqual(metadataPath[..^"_Metadata.txt".Length], result.CaptureLoads[0].CaptureFolderPath, "metadata replay should use the exact capture prefix");
        AssertEqual("Battlefields", result.CaptureLoads[0].AreaKey, "metadata replay should resolve the metadata area");
        AssertEqual(1, result.Steps.Count, "metadata replay should evaluate one decision");
        AssertTrue(result.Steps[0].Counted, "metadata replay should feed the saved journal frame through detection");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayCapturePrefixLoaded", StringComparison.Ordinal) && line.Contains("areaKey=Battlefields", StringComparison.Ordinal)), "metadata replay should log the resolved prefix");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayCapturePrefixLoaderReplaysSpecificCapture()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayPrefix_{Guid.NewGuid():N}");
    string templateRoot = Path.Combine(fixtureRoot, "templates");
    Directory.CreateDirectory(templateRoot);
    try
    {
        string templatePath = Path.Combine(templateRoot, "Blood Thief Killed Journal.png");
        using Bitmap template = CreateFixturePatternBitmap(12, 12);
        template.Save(templatePath);

        string captureFolder = CreateReplayCaptureFolder(
            fixtureRoot,
            "SharedEncounterCaptures",
            "GoblinEncounter_20260607_191500_000_AutomaticObservation_Journal_BloodThief_EasternChannelLevel2",
            "Eastern Channel Level 2",
            new DateTime(2026, 6, 7, 19, 15, 0, DateTimeKind.Utc),
            template,
            new Point(24, 300),
            170,
            340);
        string prefix = Directory.GetFiles(captureFolder, "*_Metadata.txt").Single()[..^"_Metadata.txt".Length];

        GoblinReplayCaptureFolderScenarioResult result = GoblinReplayFixtureRunner.RunExplicitCapturePrefixesForHarness(
            "Specific prefix replay",
            [new GoblinReplayCaptureFolderStep("Specific prefix", prefix)],
            templateRoot);

        AssertEqual(1, result.CaptureLoads.Count, "prefix replay should return one load result");
        AssertTrue(result.CaptureLoads[0].Loaded, "specific prefix replay should load");
        AssertEqual("Eastern Channel Level 2", result.CaptureLoads[0].AreaKey, "prefix replay should keep the exact Level 2 area");
        AssertEqual(1, result.Steps.Count, "prefix replay should evaluate one step");
        AssertTrue(result.Steps[0].Counted, "prefix replay should detect the saved journal frame");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayCapturePrefixLoaderReportsMissingMetadataClearly()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayPrefixMissing_{Guid.NewGuid():N}");
    string templateRoot = Path.Combine(fixtureRoot, "templates");
    Directory.CreateDirectory(templateRoot);
    try
    {
        string captureFolder = Path.Combine(fixtureRoot, "SharedEncounterCaptures");
        Directory.CreateDirectory(captureFolder);
        string prefix = Path.Combine(captureFolder, "GoblinEncounter_20260607_192000_000_MissingMetadata");
        using Bitmap frame = new(20, 20);
        frame.Save($"{prefix}_Journal.png");

        List<string> replayLogs = [];
        GoblinReplayCaptureFolderScenarioResult result = GoblinReplayFixtureRunner.RunExplicitCapturePrefixesForHarness(
            "Missing metadata prefix replay",
            [new GoblinReplayCaptureFolderStep("Missing metadata prefix", prefix)],
            templateRoot,
            replayLogs.Add);

        AssertEqual(1, result.CaptureLoads.Count, "missing metadata replay should return one load result");
        AssertFalse(result.CaptureLoads[0].Loaded, "missing metadata prefix should not load");
        AssertEqual("MetadataFileMissing", result.CaptureLoads[0].Reason, "missing metadata should report a clear reason");
        AssertEqual(0, result.Steps.Count, "missing metadata should not create replay decisions");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayCapturePrefixSkipped", StringComparison.Ordinal) && line.Contains("reason=MetadataFileMissing", StringComparison.Ordinal)), "missing metadata should be logged clearly");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayDecisionBundleLoaderReportsAvailableEvidence()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayDecisionBundle_{Guid.NewGuid():N}");
    string templateRoot = Path.Combine(fixtureRoot, "templates");
    Directory.CreateDirectory(templateRoot);
    try
    {
        string bundleFolder = Path.Combine(fixtureRoot, "DecisionBundles", "gdt-20260607190245219-19fd3ca7");
        Directory.CreateDirectory(bundleFolder);
        string tracePath = Path.Combine(bundleFolder, "decision-trace.txt");
        string evidencePath = Path.Combine(bundleFolder, "evidence.png");
        File.WriteAllLines(tracePath,
        [
            "GoblinDecisionTrace: correlationId=gdt-20260607190245219-19fd3ca7; mode=Live; source=Journal; imageFile=GoblinEvidence_20260607_190245_JournalKill.png; imagePath=D:\\Missing\\GoblinEvidence_20260607_190245_JournalKill.png; areaRaw=Battlefields; areaKey=Battlefields; goblinType=Blood Thief; reason=Eligible",
            $"sourceImagePath={evidencePath}",
        ]);
        using (Bitmap evidence = new(160, 90))
        {
            evidence.Save(evidencePath);
        }

        List<string> replayLogs = [];
        GoblinReplayCaptureFolderScenarioResult result = GoblinReplayFixtureRunner.RunExplicitDecisionBundlesForHarness(
            "Decision bundle not replay-ready",
            [new GoblinReplayCaptureFolderStep("Decision bundle", bundleFolder)],
            templateRoot,
            replayLogs.Add);

        AssertEqual(1, result.CaptureLoads.Count, "decision bundle replay should return one load result");
        AssertFalse(result.CaptureLoads[0].Loaded, "fullscreen-only decision bundle should not claim replay-ready frames");
        AssertEqual("DecisionBundleMissingReplayFrames", result.CaptureLoads[0].Reason, "decision bundle should explain missing replay crop frames");
        AssertTrue(result.CaptureLoads[0].Metadata.ContainsKey("DecisionTracePath"), "decision bundle result should expose the trace path");
        AssertTrue(result.CaptureLoads[0].Metadata.ContainsKey("EvidencePath"), "decision bundle result should expose the evidence path");
        AssertEqual(0, result.Steps.Count, "non-replay-ready decision bundle should not create replay decisions");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayDecisionBundleSkipped", StringComparison.Ordinal) && line.Contains("Replay-ready decision bundles contain", StringComparison.Ordinal)), "decision bundle should log a plain explanation");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayDecisionBundleLoaderCanResolveReplayReadyPrefix()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayDecisionBundleReady_{Guid.NewGuid():N}");
    string templateRoot = Path.Combine(fixtureRoot, "templates");
    Directory.CreateDirectory(templateRoot);
    try
    {
        string templatePath = Path.Combine(templateRoot, "Menagerist Killed Journal.png");
        using Bitmap template = CreateFixturePatternBitmap(13, 13);
        template.Save(templatePath);

        string captureFolder = CreateReplayCaptureFolder(
            fixtureRoot,
            "SharedEncounterCaptures",
            "GoblinEncounter_20260607_193000_000_AutomaticObservation_Journal_Menagerist_CavernsOfFrostLevel2",
            "Caverns of Frost Level 2",
            new DateTime(2026, 6, 7, 19, 30, 0, DateTimeKind.Utc),
            template,
            new Point(26, 300),
            180,
            340);
        string journalPath = Directory.GetFiles(captureFolder, "*_Journal.png").Single();
        string bundleFolder = Path.Combine(fixtureRoot, "DecisionBundles", "gdt-replay-ready");
        Directory.CreateDirectory(bundleFolder);
        File.WriteAllLines(Path.Combine(bundleFolder, "decision-trace.txt"),
        [
            $"GoblinDecisionTrace: correlationId=gdt-replay-ready; mode=Live; source=Journal; imageFile={Path.GetFileName(journalPath)}; imagePath={journalPath}; areaRaw=Caverns of Frost Level 2; areaKey=Caverns of Frost Level 2; goblinType=Menagerist; reason=Eligible",
            $"sourceImagePath={journalPath}",
        ]);

        GoblinReplayCaptureFolderScenarioResult result = GoblinReplayFixtureRunner.RunExplicitDecisionBundlesForHarness(
            "Decision bundle replay-ready source path",
            [new GoblinReplayCaptureFolderStep("Decision bundle", bundleFolder)],
            templateRoot);

        AssertEqual(1, result.CaptureLoads.Count, "replay-ready decision bundle should return one load result");
        AssertTrue(result.CaptureLoads[0].Loaded, "decision bundle with a replay-ready source prefix should load");
        AssertEqual("LoadedFromDecisionBundle", result.CaptureLoads[0].Reason, "decision bundle should report that it loaded through a replay prefix");
        AssertEqual("Caverns of Frost Level 2", result.CaptureLoads[0].AreaKey, "decision bundle prefix replay should keep the metadata area");
        AssertEqual(1, result.Steps.Count, "decision bundle prefix replay should evaluate one decision");
        AssertTrue(result.Steps[0].Counted, "decision bundle prefix replay should reach candidate detection");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayDecisionBundleLoaderReadsLocalReplayCrops()
{
    string fixtureRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayDecisionBundleLocal_{Guid.NewGuid():N}");
    string templateRoot = Path.Combine(fixtureRoot, "templates");
    Directory.CreateDirectory(templateRoot);
    try
    {
        string templatePath = Path.Combine(templateRoot, "Blood Thief Killed Journal.png");
        using Bitmap template = CreateFixturePatternBitmap(12, 12);
        template.Save(templatePath);

        string bundleFolder = Path.Combine(fixtureRoot, "DecisionBundles", "gdt-local-replay-ready");
        Directory.CreateDirectory(bundleFolder);
        string replayPrefix = Path.Combine(bundleFolder, "decision_gdt-local-replay-ready");
        string journalPath = $"{replayPrefix}_Journal.png";
        string minimapPath = $"{replayPrefix}_Minimap.png";
        string metadataPath = $"{replayPrefix}_Metadata.txt";
        SaveFixtureFrame(journalPath, 160, 320, template, new Point(20, 260));
        using (Bitmap minimap = new(24, 24))
        {
            using Graphics graphics = Graphics.FromImage(minimap);
            graphics.Clear(Color.Black);
            minimap.Save(minimapPath);
        }

        File.WriteAllLines(metadataPath,
        [
            "Goblin Decision Bundle Capture",
            "CreatedUtc=2026-06-07T19:02:45.2240000Z",
            "AreaKey=Battlefields",
            "DisplayLocation=Battlefields",
            $"JournalPath={journalPath}",
            $"MinimapPath={minimapPath}",
            "FullImagePolicy=DisabledByDefault",
        ]);
        File.WriteAllLines(Path.Combine(bundleFolder, "decision-trace.txt"),
        [
            "GoblinDecisionTrace: correlationId=gdt-local-replay-ready; mode=Live; source=Journal; imageFile=Blood Thief Killed Journal.png; imagePath=Debug\\GoblinEvidence\\DecisionBundles\\gdt-local-replay-ready\\decision_gdt-local-replay-ready_Journal.png; areaRaw=Battlefields; areaKey=Battlefields; goblinType=Blood Thief; reason=Eligible",
            $"metadataPath={metadataPath}",
            $"journalPath={journalPath}",
            $"minimapPath={minimapPath}",
            "fullImageCopied=False",
            "fullImagePolicy=DisabledByDefault",
        ]);

        GoblinReplayCaptureFolderScenarioResult result = GoblinReplayFixtureRunner.RunExplicitDecisionBundlesForHarness(
            "Decision bundle local replay crops",
            [new GoblinReplayCaptureFolderStep("Decision bundle", bundleFolder)],
            templateRoot);

        AssertEqual(1, result.CaptureLoads.Count, "local replay-ready decision bundle should return one load result");
        AssertTrue(result.CaptureLoads[0].Loaded, "local replay-ready decision bundle should load its own crop frames");
        AssertEqual("LoadedFromDecisionBundle", result.CaptureLoads[0].Reason, "decision bundle should report the replay-ready bundle load");
        AssertEqual("Battlefields", result.CaptureLoads[0].AreaKey, "local replay metadata should keep the bundle area");
        AssertEqual(1, result.Steps.Count, "local replay-ready decision bundle should evaluate one decision");
        AssertTrue(result.Steps[0].Counted, "local replay-ready decision bundle should reach shared candidate detection");
        AssertEqual("Blood Thief", result.Steps[0].GoblinType, "local replay-ready decision bundle should preserve detected goblin type");
    }
    finally
    {
        if (Directory.Exists(fixtureRoot))
        {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayTemplateScenarioSuppressesResetCarryoverJournalEvidence()
{
    string repoRoot = FindRepositoryRootForTests();
    string templateRoot = Path.Combine(repoRoot, "Images", "Goblin Evidence");
    string scenarioRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayScenarioManifest_{Guid.NewGuid():N}");
    Directory.CreateDirectory(scenarioRoot);
    try
    {
        string templateName = "Blood Thief Killed Journal.png";
        AssertTrue(File.Exists(Path.Combine(templateRoot, templateName)), "template scenario test requires the Blood Thief killed journal template in Images\\Goblin Evidence");
        string scenarioPath = Path.Combine(scenarioRoot, "new-game-reset-carryover.txt");
        File.WriteAllLines(scenarioPath, [
            "Scenario=New Game reset carryover",
            $"Step=Weeping fresh killed|Scan|Area=The Weeping Hollow|Journal={templateName}|JournalLineBucket=11|AdvanceSeconds=1",
            "Step=Make New Game|NewGame|AdvanceSeconds=6",
            $"Step=Southern old visible killed|Scan|Area=Southern Highlands|Journal={templateName}|JournalLineBucket=8|AdvanceSeconds=1",
        ]);

        GoblinReplayTemplateScenarioManifestLoadResult load =
            GoblinReplayFixtureRunner.LoadExplicitTemplateScenarioManifestForHarness(scenarioPath);
        AssertTrue(load.Loaded, $"template scenario manifest should load cleanly, reason={load.Reason}");
        AssertEqual(3, load.Steps.Count, "template scenario manifest should parse all three steps");

        List<string> replayLogs = [];
        DateTime startUtc = new(2026, 6, 8, 11, 18, 48, DateTimeKind.Utc);
        GoblinReplayFixtureScenarioResult result = GoblinReplayFixtureRunner.RunExplicitTemplateScenarioForHarness(
            load.ScenarioName,
            load.Steps,
            templateRoot,
            replayLogs.Add,
            writeAppLog: false,
            startUtc: startUtc);

        AssertEqual(3, result.Steps.Count, "template scenario should produce scan/action/scan results");
        AssertTrue(result.Steps[0].Counted, "fresh pre-new-game journal evidence should count");
        AssertEqual("NewGame", result.Steps[1].Reason, "scenario action should record the NewGame reset step");
        AssertFalse(result.Steps[2].Counted, "old visible journal evidence should not count after New Game state reset");
        AssertEqual("JournalCandidateIgnoredResetCarryover", result.Steps[2].Reason, "old visible journal evidence should suppress as reset carryover");
        AssertTrue(result.Steps[2].FreshnessReason.StartsWith("JournalCandidateIgnoredResetCarryover", StringComparison.Ordinal), "reset-carryover freshness details should be preserved");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayTemplateScenarioAction", StringComparison.Ordinal) && line.Contains("rememberedResetCarryover=1", StringComparison.Ordinal)), "template scenario should log remembered reset carryover rows");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayTemplateScenarioStepResult", StringComparison.Ordinal) && line.Contains("reason=JournalCandidateIgnoredResetCarryover", StringComparison.Ordinal)), "template scenario should log the reset-carryover suppression result");
    }
    finally
    {
        if (Directory.Exists(scenarioRoot))
        {
            Directory.Delete(scenarioRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayTemplateScenarioSuppressesStaleJournalRowVariants()
{
    string repoRoot = FindRepositoryRootForTests();
    string templateRoot = Path.Combine(repoRoot, "Images", "Goblin Evidence");
    string scenarioRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayStaleVariants_{Guid.NewGuid():N}");
    Directory.CreateDirectory(scenarioRoot);
    try
    {
        string killedTemplate = "Blood Thief Killed Journal.png";
        string engagedTemplate = "Blood Thief Engaged.png";
        AssertTrue(File.Exists(Path.Combine(templateRoot, killedTemplate)), "template scenario test requires the Blood Thief killed journal template");
        AssertTrue(File.Exists(Path.Combine(templateRoot, engagedTemplate)), "template scenario test requires the Blood Thief engaged journal template");
        string scenarioPath = Path.Combine(scenarioRoot, "stale-blood-thief-row-variants.txt");
        File.WriteAllLines(scenarioPath, [
            "Scenario=Stale Blood Thief row variants",
            $"Step=Western Channel killed|Scan|Area=Western Channel Level 1|Journal={killedTemplate}|JournalLineBucket=11|AdvanceSeconds=1",
            $"Step=Stinging old killed|Scan|Area=Stinging Winds|Journal={killedTemplate}|JournalLineBucket=11|AdvanceSeconds=1",
            $"Step=Stinging old engaged partner|Scan|Area=Stinging Winds|Journal={engagedTemplate}|JournalLineBucket=10|AdvanceSeconds=1",
        ]);

        GoblinReplayTemplateScenarioManifestLoadResult load =
            GoblinReplayFixtureRunner.LoadExplicitTemplateScenarioManifestForHarness(scenarioPath);
        AssertTrue(load.Loaded, $"template scenario manifest should load cleanly, reason={load.Reason}");

        List<string> replayLogs = [];
        GoblinReplayFixtureScenarioResult result = GoblinReplayFixtureRunner.RunExplicitTemplateScenarioForHarness(
            load.ScenarioName,
            load.Steps,
            templateRoot,
            replayLogs.Add,
            writeAppLog: false,
            startUtc: new DateTime(2026, 6, 8, 22, 50, 56, DateTimeKind.Utc));

        AssertEqual(3, result.Steps.Count, "stale variant scenario should produce all three decisions");
        AssertTrue(result.Steps[0].Counted, "fresh Western Channel killed evidence should count");
        AssertFalse(result.Steps[1].Counted, "old Western Channel killed text should not count in Stinging Winds");
        AssertEqual("JournalKilledIgnoredStale", result.Steps[1].FreshnessReason, "old killed text should suppress as stale before its engaged partner can count");
        AssertFalse(result.Steps[2].Counted, "old engaged partner text should not consume a Stinging Winds count slot");
        AssertEqual("JournalCandidateIgnoredStaleVisibleLine", result.Steps[2].Reason, "old engaged partner should be suppressed by same-goblin stale visible-line protection");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayTemplateScenarioStepResult", StringComparison.Ordinal) && line.Contains("reason=JournalCandidateIgnoredStaleVisibleLine", StringComparison.Ordinal)), "template scenario should log stale visible-line suppression");
    }
    finally
    {
        if (Directory.Exists(scenarioRoot))
        {
            Directory.Delete(scenarioRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayTemplateScenarioSuppressesLateNewGameJournalCarryover()
{
    string repoRoot = FindRepositoryRootForTests();
    string templateRoot = Path.Combine(repoRoot, "Images", "Goblin Evidence");
    string scenarioRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayLateNewGame_{Guid.NewGuid():N}");
    Directory.CreateDirectory(scenarioRoot);
    try
    {
        string templateName = "Malevolent Tormentor Killed Journal.png";
        AssertTrue(File.Exists(Path.Combine(templateRoot, templateName)), "template scenario test requires the Malevolent Tormentor killed journal template");
        string scenarioPath = Path.Combine(scenarioRoot, "late-new-game-carryover.txt");
        File.WriteAllLines(scenarioPath, [
            "Scenario=Late New Game carryover",
            "Step=Make New Game|NewGame|AdvanceSeconds=6",
            $"Step=Southern late old killed|Scan|Area=Southern Highlands|Journal={templateName}|JournalLineBucket=8|AdvanceSeconds=1",
        ]);

        GoblinReplayTemplateScenarioManifestLoadResult load =
            GoblinReplayFixtureRunner.LoadExplicitTemplateScenarioManifestForHarness(scenarioPath);
        AssertTrue(load.Loaded, $"template scenario manifest should load cleanly, reason={load.Reason}");

        List<string> replayLogs = [];
        GoblinReplayFixtureScenarioResult result = GoblinReplayFixtureRunner.RunExplicitTemplateScenarioForHarness(
            load.ScenarioName,
            load.Steps,
            templateRoot,
            replayLogs.Add,
            writeAppLog: false,
            startUtc: new DateTime(2026, 6, 8, 22, 57, 13, DateTimeKind.Utc));

        AssertEqual(2, result.Steps.Count, "late New Game scenario should produce action and scan results");
        AssertEqual("NewGame", result.Steps[0].Reason, "scenario action should record NewGame");
        AssertFalse(result.Steps[1].Counted, "journal-only evidence appearing shortly after New Game should not count as fresh");
        AssertEqual("JournalCandidateIgnoredNewGameCarryoverWindow", result.Steps[1].Reason, "late previous-game journal row should be suppressed by the New Game carryover window");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayTemplateScenarioStepResult", StringComparison.Ordinal) && line.Contains("reason=JournalCandidateIgnoredNewGameCarryoverWindow", StringComparison.Ordinal)), "template scenario should log New Game carryover-window suppression");
    }
    finally
    {
        if (Directory.Exists(scenarioRoot))
        {
            Directory.Delete(scenarioRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayTemplateScenarioUsesCurrentLocationResolver()
{
    string repoRoot = FindRepositoryRootForTests();
    string templateRoot = Path.Combine(repoRoot, "Images", "Goblin Evidence");
    string locationRoot = Path.Combine(repoRoot, "Images", "Current Location");
    string scenarioRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayLocationScenario_{Guid.NewGuid():N}");
    Directory.CreateDirectory(scenarioRoot);
    try
    {
        string journalTemplate = "Blood Thief Killed Journal.png";
        string locationTemplate = "Eastern Channel Level 1.png";
        AssertTrue(File.Exists(Path.Combine(templateRoot, journalTemplate)), "template scenario test requires Blood Thief killed journal evidence");
        AssertTrue(File.Exists(Path.Combine(locationRoot, locationTemplate)), "template scenario test requires Eastern Channel Level 1 current-location evidence");
        string scenarioPath = Path.Combine(scenarioRoot, "eastern-channel-location-resolved.txt");
        File.WriteAllLines(scenarioPath, [
            "Scenario=Eastern Channel location resolved",
            $"Step=Eastern Channel fresh killed|Scan|Location={locationTemplate}|Journal={journalTemplate}|JournalLineBucket=10|AdvanceSeconds=1",
        ]);

        GoblinReplayTemplateScenarioManifestLoadResult load =
            GoblinReplayFixtureRunner.LoadExplicitTemplateScenarioManifestForHarness(scenarioPath);
        AssertTrue(load.Loaded, $"template scenario manifest should load cleanly, reason={load.Reason}");

        List<string> replayLogs = [];
        GoblinReplayFixtureScenarioResult result = GoblinReplayFixtureRunner.RunExplicitTemplateScenarioForHarness(
            load.ScenarioName,
            load.Steps,
            templateRoot,
            replayLogs.Add,
            writeAppLog: false,
            startUtc: new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc),
            currentLocationTemplateDirectory: locationRoot);

        AssertEqual(1, result.Steps.Count, "location-resolved scenario should produce one decision");
        AssertTrue(result.Steps[0].Counted, "fresh Eastern Channel journal evidence should count");
        AssertEqual("Eastern Channel Level 1", result.Steps[0].AreaKey, "scenario should resolve count area from the current-location template");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayTemplateScenarioLocationResolved", StringComparison.Ordinal) && line.Contains("areaKey=Eastern Channel Level 1", StringComparison.Ordinal)), "scenario replay should log the resolved current-location area");
    }
    finally
    {
        if (Directory.Exists(scenarioRoot))
        {
            Directory.Delete(scenarioRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayTemplateScenarioAllowsFreshMinimapAfterStaleCrossAreaJournal()
{
    string repoRoot = FindRepositoryRootForTests();
    string templateRoot = Path.Combine(repoRoot, "Images", "Goblin Evidence");
    string scenarioRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayStaleJournalMinimap_{Guid.NewGuid():N}");
    Directory.CreateDirectory(scenarioRoot);
    try
    {
        string journalTemplate = "Treasure Goblin Killed Journal.png";
        string minimapTemplate = "Treasure Goblin Minimap.png";
        AssertTrue(File.Exists(Path.Combine(templateRoot, journalTemplate)), "template scenario test requires Treasure Goblin killed journal evidence");
        AssertTrue(File.Exists(Path.Combine(templateRoot, minimapTemplate)), "template scenario test requires Treasure Goblin minimap evidence");
        string scenarioPath = Path.Combine(scenarioRoot, "stale-journal-fresh-minimap.txt");
        File.WriteAllLines(scenarioPath, [
            "Scenario=Stale journal should not block fresh minimap",
            $"Step=Eastern Channel fresh journal|Scan|Area=Eastern Channel Level 1|Journal={journalTemplate}|JournalLineBucket=10|AdvanceSeconds=1",
            $"Step=Stinging stale journal from prior area|Scan|Area=Stinging Winds|Journal={journalTemplate}|JournalLineBucket=11|AdvanceSeconds=8",
            $"Step=Stinging fresh minimap|Scan|Area=Stinging Winds|Minimap={minimapTemplate}|AdvanceSeconds=20",
        ]);

        GoblinReplayTemplateScenarioManifestLoadResult load =
            GoblinReplayFixtureRunner.LoadExplicitTemplateScenarioManifestForHarness(scenarioPath);
        AssertTrue(load.Loaded, $"template scenario manifest should load cleanly, reason={load.Reason}");

        List<string> replayLogs = [];
        GoblinReplayFixtureScenarioResult result = GoblinReplayFixtureRunner.RunExplicitTemplateScenarioForHarness(
            load.ScenarioName,
            load.Steps,
            templateRoot,
            replayLogs.Add,
            writeAppLog: false,
            startUtc: new DateTime(2026, 6, 8, 16, 35, 29, DateTimeKind.Utc));

        AssertEqual(3, result.Steps.Count, "stale-journal scenario should produce all three decisions");
        AssertTrue(result.Steps[0].Counted, "fresh Eastern Channel journal evidence should count");
        AssertFalse(result.Steps[1].Counted, "same visible Treasure Goblin journal text should suppress after moving to Stinging Winds");
        AssertEqual("EncounterAlreadyAutoCounted", result.Steps[1].Reason, "old cross-area journal text should stay attached to the prior encounter");
        AssertTrue(result.Steps[2].Counted, "fresh Stinging Winds minimap evidence should not be blocked by the old Eastern Channel journal row");
        AssertEqual("Stinging Winds", result.Steps[2].AreaKey, "fresh minimap count should remain in Stinging Winds");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayTemplateScenarioStepResult", StringComparison.Ordinal) && line.Contains("step=Stinging fresh minimap", StringComparison.Ordinal) && line.Contains("counted=True", StringComparison.Ordinal)), "template scenario should log the fresh minimap count");
    }
    finally
    {
        if (Directory.Exists(scenarioRoot))
        {
            Directory.Delete(scenarioRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayTemplateScenarioWaitsForKilledConfirmationAfterEngagedJournal()
{
    string repoRoot = FindRepositoryRootForTests();
    string templateRoot = Path.Combine(repoRoot, "Images", "Goblin Evidence");
    string scenarioRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayEngagedWaits_{Guid.NewGuid():N}");
    Directory.CreateDirectory(scenarioRoot);
    try
    {
        string engagedTemplate = "Treasure Goblin Engaged Journal.png";
        string killedTemplate = "Treasure Goblin Killed Journal.png";
        AssertTrue(File.Exists(Path.Combine(templateRoot, engagedTemplate)), "template scenario test requires Treasure Goblin engaged journal evidence");
        AssertTrue(File.Exists(Path.Combine(templateRoot, killedTemplate)), "template scenario test requires Treasure Goblin killed journal evidence");
        string scenarioPath = Path.Combine(scenarioRoot, "engaged-waits-for-killed.txt");
        File.WriteAllLines(scenarioPath, [
            "Scenario=Engaged waits for killed confirmation",
            $"Step=Southern engaged only|Scan|Area=Southern Highlands|Journal={engagedTemplate}|JournalLineBucket=10|AdvanceSeconds=1",
            $"Step=Southern killed confirmation|Scan|Area=Southern Highlands|Journal={killedTemplate}|JournalLineBucket=10|AdvanceSeconds=1",
        ]);

        GoblinReplayTemplateScenarioManifestLoadResult load =
            GoblinReplayFixtureRunner.LoadExplicitTemplateScenarioManifestForHarness(scenarioPath);
        AssertTrue(load.Loaded, $"template scenario manifest should load cleanly, reason={load.Reason}");

        List<string> replayLogs = [];
        GoblinReplayFixtureScenarioResult result = GoblinReplayFixtureRunner.RunExplicitTemplateScenarioForHarness(
            load.ScenarioName,
            load.Steps,
            templateRoot,
            replayLogs.Add,
            writeAppLog: false,
            startUtc: new DateTime(2026, 6, 8, 18, 0, 0, DateTimeKind.Utc));

        AssertEqual(2, result.Steps.Count, "engaged/killed scenario should produce both decisions");
        AssertFalse(result.Steps[0].Counted, "Engaged-only journal evidence should not auto-count without killed/minimap confirmation");
        AssertEqual(
            GoblinAutoCountEvidenceReliabilityPolicy.JournalPendingKilledOrMinimapConfirmation,
            result.Steps[0].Reason,
            "Engaged-only replay evidence should report the pending-confirmation reason");
        AssertTrue(result.Steps[1].Counted, "fresh killed journal evidence should count after the Engaged diagnostic");
        AssertEqual("Eligible", result.Steps[1].Reason, "killed journal confirmation should remain eligible");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayTemplateScenarioStepResult", StringComparison.Ordinal) && line.Contains("reason=JournalPendingKilledOrMinimapConfirmation", StringComparison.Ordinal)), "template scenario should log pending Engaged-only evidence");
    }
    finally
    {
        if (Directory.Exists(scenarioRoot))
        {
            Directory.Delete(scenarioRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayTemplateScenarioLetsStrongMinimapOverridePendingEngagedJournal()
{
    string repoRoot = FindRepositoryRootForTests();
    string templateRoot = Path.Combine(repoRoot, "Images", "Goblin Evidence");
    string scenarioRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayMinimapOverridesEngaged_{Guid.NewGuid():N}");
    Directory.CreateDirectory(scenarioRoot);
    try
    {
        string engagedTemplate = "Treasure Goblin Engaged Journal.png";
        string minimapTemplate = "Treasure Goblin Minimap.png";
        AssertTrue(File.Exists(Path.Combine(templateRoot, engagedTemplate)), "template scenario test requires Treasure Goblin engaged journal evidence");
        AssertTrue(File.Exists(Path.Combine(templateRoot, minimapTemplate)), "template scenario test requires Treasure Goblin minimap evidence");
        string scenarioPath = Path.Combine(scenarioRoot, "minimap-overrides-pending-engaged.txt");
        File.WriteAllLines(scenarioPath, [
            "Scenario=Strong minimap overrides pending engaged journal",
            $"Step=Northern same-scan minimap and engaged|Scan|Area=Northern Highlands|Journal={engagedTemplate}|Minimap={minimapTemplate}|JournalLineBucket=10|AdvanceSeconds=1",
        ]);

        GoblinReplayTemplateScenarioManifestLoadResult load =
            GoblinReplayFixtureRunner.LoadExplicitTemplateScenarioManifestForHarness(scenarioPath);
        AssertTrue(load.Loaded, $"template scenario manifest should load cleanly, reason={load.Reason}");

        List<string> replayLogs = [];
        GoblinReplayFixtureScenarioResult result = GoblinReplayFixtureRunner.RunExplicitTemplateScenarioForHarness(
            load.ScenarioName,
            load.Steps,
            templateRoot,
            replayLogs.Add,
            writeAppLog: false,
            startUtc: new DateTime(2026, 6, 8, 18, 0, 0, DateTimeKind.Utc));

        AssertEqual(1, result.Steps.Count, "same-scan minimap/journal scenario should produce one decision");
        AssertTrue(result.Steps[0].Counted, "strong Minimap should count immediately even when Journal Engaged is also present");
        AssertEqual("MinimapCandidate", result.Steps[0].Source, "same-scan strong Minimap should be selected over pending Journal Engaged");
        AssertEqual("Eligible", result.Steps[0].Reason, "strong Minimap selection should use existing eligible count path");
    }
    finally
    {
        if (Directory.Exists(scenarioRoot))
        {
            Directory.Delete(scenarioRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayTemplateScenarioCountsSustainedActiveEngagedJournal()
{
    string repoRoot = FindRepositoryRootForTests();
    string templateRoot = Path.Combine(repoRoot, "Images", "Goblin Evidence");
    string scenarioRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplaySustainedEngaged_{Guid.NewGuid():N}");
    Directory.CreateDirectory(scenarioRoot);
    try
    {
        string engagedTemplate = "Treasure Goblin Engaged Journal.png";
        AssertTrue(File.Exists(Path.Combine(templateRoot, engagedTemplate)), "template scenario test requires Treasure Goblin engaged journal evidence");
        string scenarioPath = Path.Combine(scenarioRoot, "sustained-engaged-counts.txt");
        File.WriteAllLines(scenarioPath, [
            "Scenario=Sustained engaged journal counts",
            $"Step=Southern engaged first seen|Scan|Area=Southern Highlands|Journal={engagedTemplate}|JournalLineBucket=10|AdvanceSeconds=3",
            $"Step=Southern engaged sustained|Scan|Area=Southern Highlands|Journal={engagedTemplate}|JournalLineBucket=10|AdvanceSeconds=1",
        ]);

        GoblinReplayTemplateScenarioManifestLoadResult load =
            GoblinReplayFixtureRunner.LoadExplicitTemplateScenarioManifestForHarness(scenarioPath);
        AssertTrue(load.Loaded, $"template scenario manifest should load cleanly, reason={load.Reason}");

        List<string> replayLogs = [];
        GoblinReplayFixtureScenarioResult result = GoblinReplayFixtureRunner.RunExplicitTemplateScenarioForHarness(
            load.ScenarioName,
            load.Steps,
            templateRoot,
            replayLogs.Add,
            writeAppLog: false,
            startUtc: new DateTime(2026, 6, 8, 18, 0, 0, DateTimeKind.Utc));

        AssertEqual(2, result.Steps.Count, "sustained engaged scenario should produce both decisions");
        AssertFalse(result.Steps[0].Counted, "first Engaged-only journal sighting should remain pending");
        AssertEqual(
            GoblinAutoCountEvidenceReliabilityPolicy.JournalPendingKilledOrMinimapConfirmation,
            result.Steps[0].Reason,
            "first Engaged-only sighting should report the pending-confirmation reason");
        AssertTrue(result.Steps[1].Counted, "same-area active Engaged evidence sustained past the confirmation window should count");
        AssertEqual("Eligible", result.Steps[1].Reason, "sustained same-area Engaged should become eligible");
        AssertTrue(replayLogs.Any(line => line.Contains("GoblinReplayTemplateScenarioStepResult", StringComparison.Ordinal) && line.Contains("countDecision=Count", StringComparison.Ordinal)), "template scenario should log the sustained Engaged count");
    }
    finally
    {
        if (Directory.Exists(scenarioRoot))
        {
            Directory.Delete(scenarioRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayTemplateScenarioAllowsPandemoniumSameSignatureSecondMinimap()
{
    string repoRoot = FindRepositoryRootForTests();
    string templateRoot = Path.Combine(repoRoot, "Images", "Goblin Evidence");
    string scenarioRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayPfSameMinimap_{Guid.NewGuid():N}");
    Directory.CreateDirectory(scenarioRoot);
    try
    {
        string bloodThiefMinimap = "Blood Thief Minimap.png";
        string treasureMinimap = "Treasure Goblin Minimap.png";
        AssertTrue(File.Exists(Path.Combine(templateRoot, bloodThiefMinimap)), "template scenario test requires Blood Thief minimap evidence");
        AssertTrue(File.Exists(Path.Combine(templateRoot, treasureMinimap)), "template scenario test requires Treasure Goblin minimap evidence");
        string scenarioPath = Path.Combine(scenarioRoot, "pf2-two-blood-thief-minimap.txt");
        File.WriteAllLines(scenarioPath, [
            "Scenario=PF2 same-signature second minimap",
            $"Step=PF2 first Blood Thief|Scan|Area=Pandemonium Fortress Level 2|Minimap={bloodThiefMinimap}|AdvanceSeconds=1",
            $"Step=PF2 immediate duplicate Blood Thief|Scan|Area=Pandemonium Fortress Level 2|Minimap={bloodThiefMinimap}|AdvanceSeconds=9",
            $"Step=PF2 second Blood Thief|Scan|Area=Pandemonium Fortress Level 2|Minimap={bloodThiefMinimap}|AdvanceSeconds=10",
            $"Step=PF2 third Treasure Goblin|Scan|Area=Pandemonium Fortress Level 2|Minimap={treasureMinimap}|AdvanceSeconds=1",
        ]);

        GoblinReplayTemplateScenarioManifestLoadResult load =
            GoblinReplayFixtureRunner.LoadExplicitTemplateScenarioManifestForHarness(scenarioPath);
        AssertTrue(load.Loaded, $"template scenario manifest should load cleanly, reason={load.Reason}");

        List<string> replayLogs = [];
        GoblinReplayFixtureScenarioResult result = GoblinReplayFixtureRunner.RunExplicitTemplateScenarioForHarness(
            load.ScenarioName,
            load.Steps,
            templateRoot,
            replayLogs.Add,
            writeAppLog: false,
            startUtc: new DateTime(2026, 6, 9, 6, 0, 42, DateTimeKind.Utc));

        AssertEqual(4, result.Steps.Count, "PF2 same-signature scenario should produce all four decisions");
        AssertTrue(result.Steps[0].Counted, "PF2 first Blood Thief minimap should count");
        AssertFalse(result.Steps[1].Counted, "PF2 immediate same-signature duplicate should still suppress");
        AssertEqual("EvidenceAlreadyAutoCounted", result.Steps[1].Reason, "immediate duplicate should keep exact evidence suppression");
        AssertTrue(result.Steps[2].Counted, "PF2 second Blood Thief with the same minimap signature after the threshold should count");
        AssertFalse(result.Steps[3].Counted, "PF2 third goblin should not exceed the two-count area limit");
        AssertEqual("AreaLimitReached", result.Steps[3].Reason, "PF2 third fresh goblin should suppress by area limit");
        AssertTrue(replayLogs.Any(line => line.Contains("step=PF2 second Blood Thief", StringComparison.Ordinal) && line.Contains("counted=True", StringComparison.Ordinal)), "template scenario should log the second PF2 same-signature count");
    }
    finally
    {
        if (Directory.Exists(scenarioRoot))
        {
            Directory.Delete(scenarioRoot, recursive: true);
        }
    }
}

static void TestGoblinReplayCaptureFolderCommandRemainsHarnessOnly()
{
    string repoRoot = FindRepositoryRootForTests();
    string programSource = File.ReadAllText(Path.Combine(repoRoot, "Tests", "GoblinFarmer.Tests", "Program.cs"));
    string replayRunnerSource = File.ReadAllText(Path.Combine(repoRoot, "GoblinReplayFixtureRunner.cs"));

    AssertTrue(programSource.Contains("--goblin-replay-captures", StringComparison.Ordinal), "developer replay command should live in the test harness CLI");
    AssertTrue(programSource.Contains("--goblin-replay-metadata", StringComparison.Ordinal), "metadata replay command should live in the test harness CLI");
    AssertTrue(programSource.Contains("--goblin-replay-prefix", StringComparison.Ordinal), "prefix replay command should live in the test harness CLI");
    AssertTrue(programSource.Contains("--goblin-replay-decision-bundle", StringComparison.Ordinal), "decision-bundle replay command should live in the test harness CLI");
    AssertTrue(programSource.Contains("--goblin-replay-scenario", StringComparison.Ordinal), "template-scenario replay command should live in the test harness CLI");
    AssertTrue(programSource.Contains("RunExplicitCaptureFoldersForHarness", StringComparison.Ordinal), "developer replay command should call the explicit capture-folder runner");
    AssertTrue(programSource.Contains("RunExplicitMetadataFilesForHarness", StringComparison.Ordinal), "developer replay command should call the explicit metadata-file runner");
    AssertTrue(programSource.Contains("RunExplicitCapturePrefixesForHarness", StringComparison.Ordinal), "developer replay command should call the explicit capture-prefix runner");
    AssertTrue(programSource.Contains("RunExplicitDecisionBundlesForHarness", StringComparison.Ordinal), "developer replay command should call the explicit decision-bundle runner");
    AssertTrue(programSource.Contains("RunExplicitTemplateScenarioForHarness", StringComparison.Ordinal), "developer replay command should call the explicit template-scenario runner");
    AssertTrue(programSource.Contains("writeAppLog: false", StringComparison.Ordinal), "developer replay command should avoid persistent app logs by default");
    AssertTrue(replayRunnerSource.Contains("writeAppLog = true", StringComparison.Ordinal), "replay runner should keep existing harness logging defaults unless the command opts out");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "Form1.cs")).Contains("--goblin-replay-captures", StringComparison.Ordinal), "WinForms startup should not know about the replay command");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "Form1.cs")).Contains("--goblin-replay-metadata", StringComparison.Ordinal), "WinForms startup should not know about metadata replay");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "Form1.cs")).Contains("--goblin-replay-prefix", StringComparison.Ordinal), "WinForms startup should not know about prefix replay");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "Form1.cs")).Contains("--goblin-replay-decision-bundle", StringComparison.Ordinal), "WinForms startup should not know about decision-bundle replay");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "Form1.cs")).Contains("--goblin-replay-scenario", StringComparison.Ordinal), "WinForms startup should not know about template-scenario replay");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs")).Contains("--goblin-replay-captures", StringComparison.Ordinal), "live scanner should not know about the replay command");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs")).Contains("--goblin-replay-metadata", StringComparison.Ordinal), "live scanner should not know about metadata replay");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs")).Contains("--goblin-replay-prefix", StringComparison.Ordinal), "live scanner should not know about prefix replay");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs")).Contains("--goblin-replay-decision-bundle", StringComparison.Ordinal), "live scanner should not know about decision-bundle replay");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs")).Contains("--goblin-replay-scenario", StringComparison.Ordinal), "live scanner should not know about template-scenario replay");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "Scripts", "create-debug-package.ps1")).Contains("--goblin-replay-captures", StringComparison.Ordinal), "debug package creation should not invoke replay");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "Scripts", "create-debug-package.ps1")).Contains("--goblin-replay-metadata", StringComparison.Ordinal), "debug package creation should not invoke metadata replay");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "Scripts", "create-debug-package.ps1")).Contains("--goblin-replay-prefix", StringComparison.Ordinal), "debug package creation should not invoke prefix replay");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "Scripts", "create-debug-package.ps1")).Contains("--goblin-replay-decision-bundle", StringComparison.Ordinal), "debug package creation should not invoke decision-bundle replay");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "Scripts", "create-debug-package.ps1")).Contains("--goblin-replay-scenario", StringComparison.Ordinal), "debug package creation should not invoke template-scenario replay");
}

static bool TryRunGoblinReplayCaptureCommand(string[] commandArgs, out int exitCode)
{
    exitCode = 0;
    if (commandArgs.Length == 0 || !GoblinReplayCommandIsSupported(commandArgs[0]))
    {
        return false;
    }

    string repoRoot = FindRepositoryRootForTests();
    string templateDirectory = Path.Combine(repoRoot, "Images", "Goblin Evidence");
    string scenarioName = $"Explicit capture replay {DateTime.Now:yyyyMMdd_HHmmss}";
    bool scenarioNameOverridden = false;
    string command = commandArgs[0];
    string inputMode = GoblinReplayInputMode(command);
    List<string> replayInputs = [];

    for (int i = 1; i < commandArgs.Length; i++)
    {
        string arg = commandArgs[i];
        if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
        {
            PrintGoblinReplayCaptureCommandHelp();
            return true;
        }

        if (arg.Equals("--templates", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("--template-dir", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 >= commandArgs.Length)
            {
                Console.Error.WriteLine("FAIL missing value for --templates.");
                PrintGoblinReplayCaptureCommandHelp();
                exitCode = 1;
                return true;
            }

            templateDirectory = Path.GetFullPath(commandArgs[++i], repoRoot);
            continue;
        }

        if (arg.Equals("--scenario", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 >= commandArgs.Length)
            {
                Console.Error.WriteLine("FAIL missing value for --scenario.");
                PrintGoblinReplayCaptureCommandHelp();
                exitCode = 1;
                return true;
            }

            scenarioName = commandArgs[++i];
            scenarioNameOverridden = true;
            continue;
        }

        if (arg.StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"FAIL unknown Goblin Replay option: {arg}");
            PrintGoblinReplayCaptureCommandHelp();
            exitCode = 1;
            return true;
        }

        replayInputs.Add(Path.GetFullPath(arg, repoRoot));
    }

    if (replayInputs.Count == 0)
    {
        Console.Error.WriteLine($"FAIL at least one {inputMode} path is required.");
        PrintGoblinReplayCaptureCommandHelp();
        exitCode = 1;
        return true;
    }

    if (!Directory.Exists(templateDirectory))
    {
        Console.Error.WriteLine($"FAIL template directory does not exist: {templateDirectory}");
        exitCode = 1;
        return true;
    }

    try
    {
        Console.WriteLine("Goblin Replay Capture Folder Harness");
        Console.WriteLine("Mode: ExplicitOnDemand");
        Console.WriteLine("WritesPersistentDebugFiles: False");
        Console.WriteLine($"InputMode: {inputMode}");
        Console.WriteLine($"Scenario: {scenarioName}");
        Console.WriteLine($"TemplateDirectory: {templateDirectory}");
        Console.WriteLine($"InputCount: {replayInputs.Count}");

        if (command.Equals("--goblin-replay-scenario", StringComparison.OrdinalIgnoreCase))
        {
            int failedScenarios = 0;
            int totalSteps = 0;
            int totalCounted = 0;
            foreach (string scenarioPath in replayInputs)
            {
                GoblinReplayTemplateScenarioManifestLoadResult load =
                    GoblinReplayFixtureRunner.LoadExplicitTemplateScenarioManifestForHarness(scenarioPath);
                string loadStatus = load.Loaded ? "PASS" : "FAIL";
                Console.WriteLine(
                    $"{loadStatus} SCENARIO_LOAD reason={load.Reason} scenario=\"{load.ScenarioName}\" path=\"{load.ScenarioPath}\" steps={load.Steps.Count}");
                foreach (string error in load.Errors)
                {
                    Console.WriteLine($"ERROR scenario=\"{load.ScenarioName}\" message=\"{error}\"");
                }

                if (!load.Loaded)
                {
                    failedScenarios++;
                    continue;
                }

                GoblinReplayFixtureScenarioResult scenarioResult = GoblinReplayFixtureRunner.RunExplicitTemplateScenarioForHarness(
                    scenarioNameOverridden ? scenarioName : load.ScenarioName,
                    load.Steps,
                    templateDirectory,
                    log: null,
                    setFrameSourceForReplay: null,
                    writeAppLog: false);
                foreach (GoblinReplayFixtureStepResult step in scenarioResult.Steps)
                {
                    string status = step.FrameSource.Equals("ScenarioAction", StringComparison.OrdinalIgnoreCase) ||
                        step.CandidateFound ||
                        step.Counted
                        ? "PASS"
                        : "FAIL";
                    Console.WriteLine(
                        $"{status} SCENARIO_STEP step=\"{step.StepName}\" area=\"{step.AreaKey}\" candidate={step.CandidateResult} source={step.Source} goblinType=\"{step.GoblinType}\" decision={step.CountDecision} reason={step.Reason} freshness={step.FreshnessReason} counted={step.Counted}");
                }

                totalSteps += scenarioResult.Steps.Count;
                totalCounted += scenarioResult.Steps.Count(step => step.Counted);
            }

            Console.WriteLine(
                $"SUMMARY scenarioFiles={replayInputs.Count} failedScenarios={failedScenarios} decisions={totalSteps} counted={totalCounted} suppressed={totalSteps - totalCounted}");
            if (failedScenarios > 0 || totalSteps == 0)
            {
                Console.Error.WriteLine("FAIL Goblin Replay scenario command completed with missing/invalid scenarios or no replay decisions.");
                exitCode = 1;
                return true;
            }

            Console.WriteLine("PASS Goblin Replay scenario completed.");
            return true;
        }

        IReadOnlyList<GoblinReplayCaptureFolderStep> inputSteps = replayInputs
            .Select((input, index) => new GoblinReplayCaptureFolderStep($"{inputMode} {index + 1}", input))
            .ToList();
        GoblinReplayCaptureFolderScenarioResult result = command.ToLowerInvariant() switch
        {
            "--goblin-replay-metadata" => GoblinReplayFixtureRunner.RunExplicitMetadataFilesForHarness(
                scenarioName,
                inputSteps,
                templateDirectory,
                log: null,
                setFrameSourceForReplay: null,
                writeAppLog: false),
            "--goblin-replay-prefix" => GoblinReplayFixtureRunner.RunExplicitCapturePrefixesForHarness(
                scenarioName,
                inputSteps,
                templateDirectory,
                log: null,
                setFrameSourceForReplay: null,
                writeAppLog: false),
            "--goblin-replay-decision-bundle" => GoblinReplayFixtureRunner.RunExplicitDecisionBundlesForHarness(
                scenarioName,
                inputSteps,
                templateDirectory,
                log: null,
                setFrameSourceForReplay: null,
                writeAppLog: false),
            _ => GoblinReplayFixtureRunner.RunExplicitCaptureFoldersForHarness(
                scenarioName,
                inputSteps,
                templateDirectory,
                log: null,
                setFrameSourceForReplay: null,
                writeAppLog: false),
        };

        foreach (GoblinReplayCaptureFolderLoadResult load in result.CaptureLoads)
        {
            string status = load.Loaded ? "PASS" : "FAIL";
            string available = load.Metadata.Count == 0
                ? ""
                : $" metadata=\"{string.Join(",", load.Metadata.Select(pair => $"{pair.Key}={pair.Value}"))}\"";
            Console.WriteLine(
                $"{status} LOAD step=\"{load.StepName}\" reason={load.Reason} area=\"{load.AreaKey}\" input=\"{load.CaptureFolderPath}\" journal=\"{load.JournalPath ?? ""}\" minimap=\"{load.MinimapPath ?? ""}\"{available}");
        }

        foreach (GoblinReplayFixtureStepResult step in result.Steps)
        {
            string status = step.CandidateFound ? "PASS" : "FAIL";
            Console.WriteLine(
                $"{status} DECISION step=\"{step.StepName}\" area=\"{step.AreaKey}\" candidate={step.CandidateResult} source={step.Source} goblinType=\"{step.GoblinType}\" decision={step.CountDecision} reason={step.Reason} freshness={step.FreshnessReason} counted={step.Counted}");
        }

        int loadFailures = result.CaptureLoads.Count(load => !load.Loaded);
        Console.WriteLine(
            $"SUMMARY loaded={result.CaptureLoads.Count(load => load.Loaded)} skipped={loadFailures} decisions={result.Steps.Count} counted={result.Steps.Count(step => step.Counted)} suppressed={result.Steps.Count(step => !step.Counted)}");

        if (loadFailures > 0 || result.Steps.Count == 0)
        {
            Console.Error.WriteLine("FAIL Goblin Replay completed with missing/invalid capture folders or no replay decisions.");
            exitCode = 1;
            return true;
        }

        Console.WriteLine("PASS Goblin Replay completed.");
        return true;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL Goblin Replay command failed: {ex.Message}");
        exitCode = 1;
        return true;
    }
}

static bool GoblinReplayCommandIsSupported(string command)
{
    return command.Equals("--goblin-replay-captures", StringComparison.OrdinalIgnoreCase) ||
        command.Equals("--goblin-replay-metadata", StringComparison.OrdinalIgnoreCase) ||
        command.Equals("--goblin-replay-prefix", StringComparison.OrdinalIgnoreCase) ||
        command.Equals("--goblin-replay-decision-bundle", StringComparison.OrdinalIgnoreCase) ||
        command.Equals("--goblin-replay-scenario", StringComparison.OrdinalIgnoreCase);
}

static string GoblinReplayInputMode(string command)
{
    return command.ToLowerInvariant() switch
    {
        "--goblin-replay-metadata" => "Metadata",
        "--goblin-replay-prefix" => "Prefix",
        "--goblin-replay-decision-bundle" => "DecisionBundle",
        "--goblin-replay-scenario" => "TemplateScenario",
        _ => "CaptureFolder",
    };
}

static void PrintGoblinReplayCaptureCommandHelp()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project .\\Tests\\GoblinFarmer.Tests\\GoblinFarmer.Tests.csproj -- --goblin-replay-captures [--templates \".\\Images\\Goblin Evidence\"] [--scenario \"Name\"] \"CaptureFolder1\" [\"CaptureFolder2\" ...]");
    Console.WriteLine("  dotnet run --project .\\Tests\\GoblinFarmer.Tests\\GoblinFarmer.Tests.csproj -- --goblin-replay-metadata [--templates \".\\Images\\Goblin Evidence\"] \"Capture_Metadata.txt\"");
    Console.WriteLine("  dotnet run --project .\\Tests\\GoblinFarmer.Tests\\GoblinFarmer.Tests.csproj -- --goblin-replay-prefix [--templates \".\\Images\\Goblin Evidence\"] \"CapturePrefix\"");
    Console.WriteLine("  dotnet run --project .\\Tests\\GoblinFarmer.Tests\\GoblinFarmer.Tests.csproj -- --goblin-replay-decision-bundle [--templates \".\\Images\\Goblin Evidence\"] \"DecisionBundleFolder\"");
    Console.WriteLine("  dotnet run --project .\\Tests\\GoblinFarmer.Tests\\GoblinFarmer.Tests.csproj -- --goblin-replay-scenario [--templates \".\\Images\\Goblin Evidence\"] \"ScenarioFile.txt\"");
    Console.WriteLine();
    Console.WriteLine("Capture folders are real saved Goblin Evidence encounter/manual capture folders containing *_Metadata.txt plus *_Journal.png and/or *_Minimap.png.");
    Console.WriteLine("Metadata and prefix inputs let you target a specific older capture inside shared EncounterCaptures or ManualCaptures folders.");
    Console.WriteLine("Decision bundles can replay only when they contain or point to replay-ready Journal/Minimap capture frames; otherwise they report available evidence and explain the limitation.");
    Console.WriteLine("Scenario files synthesize replay frames from Images\\Goblin Evidence templates. Use Step=Name|Scan|Area=...|Journal=... and Step=Name|NewGame to model reset/transition cases.");
    Console.WriteLine("Replay is explicit/on-demand only and does not run during app startup, VS Debug startup, live scanning, automation workflows, or debug package creation.");
}

static Bitmap CreateFixturePatternBitmap(int width, int height)
{
    Bitmap bitmap = new(width, height);
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            int red = (x * 29 + y * 11) % 256;
            int green = (x * 7 + y * 37 + 64) % 256;
            int blue = (x * 19 + y * 5 + 128) % 256;
            bitmap.SetPixel(x, y, Color.FromArgb(red, green, blue));
        }
    }

    return bitmap;
}

static void SaveFixtureFrame(string path, int width, int height, Bitmap template, Point matchPoint)
{
    using Bitmap frame = new(width, height);
    using Graphics graphics = Graphics.FromImage(frame);
    graphics.Clear(Color.Black);
    graphics.DrawImageUnscaled(template, matchPoint);
    frame.Save(path);
}

static string CreateReplayCaptureFolder(
    string root,
    string folderName,
    string prefix,
    string areaKey,
    DateTime timestampUtc,
    Bitmap journalTemplate,
    Point journalMatchPoint,
    int journalFrameWidth,
    int journalFrameHeight)
{
    string folder = Path.Combine(root, folderName);
    Directory.CreateDirectory(folder);
    string journalPath = Path.Combine(folder, $"{prefix}_Journal.png");
    string minimapPath = Path.Combine(folder, $"{prefix}_Minimap.png");
    string metadataPath = Path.Combine(folder, $"{prefix}_Metadata.txt");

    SaveFixtureFrame(journalPath, journalFrameWidth, journalFrameHeight, journalTemplate, journalMatchPoint);
    using (Bitmap minimap = new(24, 24))
    {
        using Graphics graphics = Graphics.FromImage(minimap);
        graphics.Clear(Color.Black);
        minimap.Save(minimapPath);
    }

    File.WriteAllLines(metadataPath,
    [
        "Goblin Encounter Debug Capture",
        $"CreatedUtc={timestampUtc:O}",
        $"AreaKey={areaKey}",
        $"DisplayLocation={areaKey}",
        $"JournalPath={journalPath}",
        $"MinimapPath={minimapPath}",
    ]);
    File.SetLastWriteTimeUtc(journalPath, timestampUtc);
    File.SetLastWriteTimeUtc(minimapPath, timestampUtc);
    File.SetLastWriteTimeUtc(metadataPath, timestampUtc);
    return folder;
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

static void TestDebugPackageIncludesBuiltInAnalysisReports()
{
    string testRoot = Path.Combine(Path.GetTempPath(), "GoblinFarmer.PackageTests", Guid.NewGuid().ToString("N"));
    string logs = Path.Combine(testRoot, "Logs");
    string config = Path.Combine(testRoot, "Config");
    string decisionBundles = Path.Combine(testRoot, "Debug", "GoblinEvidence", "DecisionBundles", "20260607_120000");
    Directory.CreateDirectory(logs);
    Directory.CreateDirectory(config);
    Directory.CreateDirectory(decisionBundles);

    try
    {
        DateTime sessionStart = DateTime.Now.AddMinutes(-5);
        File.WriteAllLines(Path.Combine(testRoot, "session-info.txt"),
        [
            $"SessionStartLocal={sessionStart:O}",
            $"SessionStartUtc={sessionStart.ToUniversalTime():O}",
            "GoblinCount=1",
            "GoblinObservationCount=2",
            "JournalObservationCount=1",
            "MinimapObservationCount=1",
            "LastGoblinObservationSource=ManualHotkey",
            "LastGoblinObservationType=Treasure Goblin",
            "LastGoblinObservationAreaKey=Cave Of The Moon Clan Level 2",
            "LastGoblinObservationReason=Counted",
        ]);
        File.WriteAllText(Path.Combine(config, "AppSettings.json"), "{}");
        File.WriteAllLines(Path.Combine(logs, "GoblinFarmer.log"),
        [
            "[2026-06-07 12:00:00.000] GoblinObservationCandidate source=Journal; goblinType=Treasure Goblin; areaKey=Cave Of The Moon Clan Level 2; wouldCount=True; reason=Eligible; evidenceHash=abc123",
            "[2026-06-07 12:00:01.000] GoblinCountAccepted source=ManualHotkey; goblinType=Treasure Goblin; areaKey=Cave Of The Moon Clan Level 2; reason=Counted; evidenceHash=abc123",
            "[2026-06-07 12:00:02.000] LastObservationUpdated source=ManualHotkey; goblinType=Treasure Goblin; areaKey=Cave Of The Moon Clan Level 2; reason=Counted",
        ]);
        File.WriteAllText(Path.Combine(decisionBundles, "trace.txt"), "correlationId=abc123");

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
        string analysisText = ReadRequiredZipText(archive, "debug-package-analysis.txt");
        string timelineText = ReadRequiredZipText(archive, "goblin-tracker-timeline.md");
        string healthText = ReadRequiredZipText(archive, "goblin-evidence-health.txt");
        string manifestText = ReadRequiredZipText(archive, "debug-package-manifest.txt");
        string reviewIndexText = ReadRequiredZipText(archive, "goblin-tracker-review.html");

        AssertTrue(analysisText.Contains("Recommended review order", StringComparison.OrdinalIgnoreCase), "analysis report should guide review order");
        AssertTrue(analysisText.Contains("GoblinCountAccepted", StringComparison.OrdinalIgnoreCase), "analysis report should summarize count markers");
        AssertTrue(timelineText.Contains("GoblinCountAccepted", StringComparison.OrdinalIgnoreCase), "timeline should include accepted count log markers");
        AssertTrue(timelineText.Contains("Cave Of The Moon Clan Level 2", StringComparison.OrdinalIgnoreCase), "timeline should include the resolved area");
        AssertTrue(healthText.Contains("DecisionBundles", StringComparison.OrdinalIgnoreCase), "evidence health report should count decision bundles");
        AssertTrue(manifestText.Contains("Debug analysis files included: True", StringComparison.OrdinalIgnoreCase), "manifest should state that analysis reports are included");
        AssertTrue(reviewIndexText.Contains("debug-package-analysis.txt", StringComparison.OrdinalIgnoreCase), "review index should link the analysis report");
        AssertTrue(reviewIndexText.Contains("goblin-tracker-timeline.md", StringComparison.OrdinalIgnoreCase), "review index should link the tracker timeline");
        AssertTrue(reviewIndexText.Contains("goblin-evidence-health.txt", StringComparison.OrdinalIgnoreCase), "review index should link the evidence health report");
    }
    finally
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }
}

static string ReadRequiredZipText(ZipArchive archive, string entryName)
{
    ZipArchiveEntry entry = archive.GetEntry(entryName) ?? throw new InvalidOperationException($"{entryName} missing from debug package");
    using StreamReader reader = new(entry.Open());
    return reader.ReadToEnd();
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
    AssertTrue(sessionStatsSource.Contains("AreaChanged", StringComparison.Ordinal), "no-candidate clears should still identify area changes for non-accepted observation state");
    AssertTrue(sessionStatsSource.Contains("currentAreaKey", StringComparison.Ordinal), "Last Observation clear logs should report the current area used for stale-area decisions");
    AssertTrue(sessionStatsSource.Contains("displayHoldSeconds={PortAutomaticGoblinObservationDisplayHold.TotalSeconds:0}", StringComparison.Ordinal), "automatic observation update logs should include the display hold duration");
    AssertTrue(sessionStatsSource.Contains("LastObservationCleared", StringComparison.Ordinal), "reset and non-accepted clear paths should log LastObservationCleared when the UI state changes");
    AssertTrue(evidenceSource.Contains("PortMarkGoblinObservationNoCurrent(\"No current observation\")", StringComparison.Ordinal), "no-candidate scans should route through the Last Observation state helper");
    AssertTrue(evidenceSource.Contains("private const int GoblinEvidenceScanIntervalMs = 500", StringComparison.Ordinal), "observation scan interval should be responsive enough for live diagnostic feedback without loosening evidence thresholds");
}

static void TestGoblinTrackerStatsUiRefreshesAfterCountChanges()
{
    string repoRoot = FindRepositoryRootForTests();
    string sessionStatsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.cs"));
    string updateMethod = ExtractMethodBody(sessionStatsSource, "private void PortUpdateGoblinTrackerStats");

    AssertTrue(updateMethod.Contains("DiagnosticsSessionSnapshot snapshot = DebugManager.Session.Snapshot(DateTime.Now)", StringComparison.Ordinal), "Goblin Tracker UI should refresh from the current session snapshot");
    AssertTrue(updateMethod.Contains("lblGoblinCount.Text = $\"Goblins: {snapshot.GoblinCount}\"", StringComparison.Ordinal), "Goblin count label should use the latest session total");
    AssertTrue(updateMethod.Contains("lblGoblinCount.Refresh()", StringComparison.Ordinal), "Goblin count label should repaint immediately after count changes");
    AssertTrue(updateMethod.Contains("lblGoblinObservation.Refresh()", StringComparison.Ordinal), "Last Observation label should repaint immediately after count/observation changes");
    AssertTrue(updateMethod.Contains("StatsUiRefreshed", StringComparison.Ordinal), "stats refresh should log visible count changes for future UI investigations");
    AssertTrue(updateMethod.Contains("snapshot.GoblinCount != portLastLoggedGoblinStatsUiCount", StringComparison.Ordinal), "stats refresh logs should be throttled to actual visible state changes");
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
    string autoCountSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.AutoCount.cs"));
    string configSource = File.ReadAllText(Path.Combine(repoRoot, "Config", "AppSettings.json"));
    string automaticEnabledMethod = ExtractMethodBody(evidenceSource, "private static bool PortGoblinAutomaticCountingEnabled");
    string observeMethod = ExtractMethodBody(sessionStatsSource, "private bool PortObserveGoblinCandidate");
    string autoCountMethod = ExtractMethodBody(autoCountSource, "private bool PortTryRecordAutomaticGoblinCount");

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
    string retiredEvidenceReviewCloseMethod = "PortCreate" + "Goblin" + ("Re" + "play") + "ReviewFilesOnVsDebugClose";

    AssertTrue(automationSource.Contains("chkGoblinObservationMode", StringComparison.Ordinal), "VS Debug form should expose an Observation Mode checkbox");
    AssertTrue(automationSource.Contains("chkGoblinAutomaticCounting", StringComparison.Ordinal), "VS Debug form should expose an Automatic Counting checkbox");
    AssertTrue(automationSource.Contains("chkGoblinDecisionTrace", StringComparison.Ordinal), "VS Debug form should expose a Decision Trace checkbox");
    AssertTrue(automationSource.Contains("btnGoblinRecognitionCapture", StringComparison.Ordinal), "VS Debug form should expose a manual recognition Capture button");
    AssertTrue(automationSource.Contains("btnGoblinDebugSimulateCount", StringComparison.Ordinal), "VS Debug form should expose a safe count simulation button");
    AssertTrue(automationSource.Contains("cboGoblinDebugSimulationArea", StringComparison.Ordinal), "VS Debug form should expose a count simulation area selector");
    AssertTrue(automationSource.Contains("Text = \"Capture\"", StringComparison.Ordinal), "manual recognition capture button should be labeled Capture");
    AssertTrue(automationSource.Contains("Text = \"Sim Count\"", StringComparison.Ordinal), "VS Debug count simulation button should be labeled Sim Count");
    AssertFalse(automationSource.Contains("chkGoblinManualTestCountOverride", StringComparison.Ordinal), "VS Debug form should not expose the retired manual test count override checkbox");
    AssertFalse(automationSource.Contains("btnCreateGoblinReviewFiles", StringComparison.Ordinal), "VS Debug form should not require a manual review files button");
    AssertFalse(automationSource.Contains(retiredEvidenceReviewCloseMethod, StringComparison.Ordinal), "VS Debug form close should not create loose review files or derived evidence artifacts");
    AssertTrue(automationSource.Contains("ShutdownCleanupStarted", StringComparison.Ordinal), "VS Debug form close should log the quiet shutdown cleanup path");
    AssertTrue(automationSource.Contains("debugArtifactCreationSkipped=True", StringComparison.Ordinal), "shutdown logs should make skipped debug artifact creation explicit");
    AssertTrue(releaseSource.Contains("PortInitializeGoblinTrackerDebugPreferenceControls();", StringComparison.Ordinal), "VS Debug Goblin Tracker checkboxes should initialize before runtime validation can stop startup");
    AssertTrue(automationSource.Contains("portSettingsGroup.Controls.Add(chkGoblinObservationMode)", StringComparison.Ordinal), "Observation Mode checkbox should be placed inside the visible Settings group");
    AssertTrue(automationSource.Contains("portSettingsGroup.Controls.Add(chkGoblinAutomaticCounting)", StringComparison.Ordinal), "Automatic Counting checkbox should be placed inside the visible Settings group");
    AssertTrue(automationSource.Contains("portSettingsGroup.Controls.Add(chkGoblinDecisionTrace)", StringComparison.Ordinal), "Decision Trace checkbox should be placed inside the visible Settings group");
    AssertTrue(automationSource.Contains("portSettingsGroup.Controls.Add(btnGoblinRecognitionCapture)", StringComparison.Ordinal), "Capture button should be placed inside the visible Settings group");
    AssertTrue(automationSource.Contains("portSettingsGroup.Controls.Add(cboGoblinDebugSimulationArea)", StringComparison.Ordinal), "VS Debug count simulation area selector should be placed inside the visible Settings group");
    AssertTrue(automationSource.Contains("portSettingsGroup.Controls.Add(btnGoblinDebugSimulateCount)", StringComparison.Ordinal), "VS Debug count simulation button should be placed inside the visible Settings group");
    AssertFalse(automationSource.Contains("Controls.Add(grpGoblinTrackerDebugSettings)", StringComparison.Ordinal), "VS Debug Goblin Tracker checkboxes should not be added as a layered top-level overlay");
    AssertTrue(automationSource.Contains("AppSettings.GoblinTracker.EnableObservationMode = chkGoblinObservationMode.Checked", StringComparison.Ordinal), "Observation Mode checkbox changes should persist to AppSettings");
    AssertTrue(automationSource.Contains("AppSettings.GoblinTracker.EnableAutomaticCounting = chkGoblinAutomaticCounting.Checked", StringComparison.Ordinal), "Automatic Counting checkbox changes should persist to AppSettings");
    AssertTrue(automationSource.Contains("AppSettings.GoblinTracker.EnableDecisionTrace = chkGoblinDecisionTrace.Checked", StringComparison.Ordinal), "Decision Trace checkbox changes should persist to AppSettings");
    AssertTrue(automationSource.Contains("PortSetGoblinAutomaticCountingArmedState(source)", StringComparison.Ordinal), "toggling automatic counting should re-arm the freshness gate");
    AssertTrue(automationSource.Contains("PortStartGoblinObservationScanner(source)", StringComparison.Ordinal), "enabling Observation Mode from the form should ensure the scanner is running");
    AssertTrue(automationSource.Contains("PortQueueGoblinRecognitionDebugCapture(\"VsDebugCaptureButton\")", StringComparison.Ordinal), "Capture button should create a manual recognition capture only when clicked");
    AssertTrue(automationSource.Contains("PortSimulateGoblinTrackerVsDebugCount()", StringComparison.Ordinal), "VS Debug simulation button should invoke the safe simulation path");
    AssertTrue(automationSource.Contains("portSettingsGroup.Height = Math.Max(portSettingsGroup.Height, 244)", StringComparison.Ordinal), "VS Debug Settings group should still expand for the added Goblin Tracker test controls");
}

static void TestVsDebugDiagnosticsOmitNextTestStepsTab()
{
    string repoRoot = FindRepositoryRootForTests();
    string diagnosticsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.Diagnostics.cs"));

    AssertFalse(diagnosticsSource.Contains("showNextTests", StringComparison.Ordinal), "VS Debug diagnostics should not include the retired Next Tests gate");
    AssertFalse(diagnosticsSource.Contains("tabNextTestSteps", StringComparison.Ordinal), "VS Debug diagnostics should not include a Next Tests tab");
    AssertFalse(diagnosticsSource.Contains("Text = \"Next Tests\"", StringComparison.Ordinal), "Next Tests tab title should be removed");
    AssertFalse(diagnosticsSource.Contains("PortCreateNextTestStepsPanel", StringComparison.Ordinal), "Next Tests checklist panel should be removed");
    AssertFalse(diagnosticsSource.Contains("PortNextTestStepMetadataLines", StringComparison.Ordinal), "Next Tests metadata should no longer be generated by the app");
    AssertFalse(diagnosticsSource.Contains("portNextTestStepCheckboxes", StringComparison.Ordinal), "Next Tests checkbox state should no longer live in the form");
}

static void TestGoblinVsDebugRecognitionCaptureButtonIsManualOnly()
{
    string repoRoot = FindRepositoryRootForTests();
    string automationSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.PortedAutomation.cs.cs"));
    string hotkeysSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.Hotkeys.cs"));
    string evidenceCaptureSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.Captures.cs"));
    string autoCountSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.AutoCount.cs"));

    AssertFalse(hotkeysSource.Contains("PortVkX", StringComparison.Ordinal), "physical X should no longer be a Goblin Tracker count hotkey");
    AssertFalse(hotkeysSource.Contains("PortIncrementGoblinCount", StringComparison.Ordinal), "keyboard hook should not invoke the manual count path");
    AssertTrue(automationSource.Contains("btnGoblinRecognitionCapture", StringComparison.Ordinal), "VS Debug should expose the recognition Capture button");
    AssertTrue(automationSource.Contains("Text = \"Capture\"", StringComparison.Ordinal), "recognition capture button should be labeled Capture");
    AssertTrue(automationSource.Contains("PortQueueGoblinRecognitionDebugCapture(\"VsDebugCaptureButton\")", StringComparison.Ordinal), "Capture button should create files only from an explicit click");
    AssertTrue(evidenceCaptureSource.Contains("ManualCaptures", StringComparison.Ordinal), "manual recognition captures should go to a separate ManualCaptures folder");
    AssertTrue(evidenceCaptureSource.Contains("GoblinRecognitionCaptureQueued", StringComparison.Ordinal), "manual recognition capture should log when it is queued");
    AssertTrue(evidenceCaptureSource.Contains("GoblinRecognitionCaptureSaved", StringComparison.Ordinal), "manual recognition capture should log saved paths");
    AssertTrue(evidenceCaptureSource.Contains("createdOnlyByButton=True", StringComparison.Ordinal), "manual recognition capture logs should make click-only creation explicit");
    AssertTrue(evidenceCaptureSource.Contains("counterWorkflowCapturesRemainAutomatic=True", StringComparison.Ordinal), "manual capture logs should state that counter-workflow captures remain automatic");
    AssertTrue(evidenceCaptureSource.Contains("GoblinEvidenceRootScreenshotSkipped", StringComparison.Ordinal), "normal evidence events should skip redundant root fullscreen event images");
    AssertTrue(evidenceCaptureSource.Contains("RedundantWithDecisionBundleAndEncounterCrops", StringComparison.Ordinal), "root evidence screenshot skip should explain the storage policy");
    AssertTrue(evidenceCaptureSource.Contains("manualCaptureStillSavesFullscreen=True", StringComparison.Ordinal), "root evidence screenshot skip should confirm manual Capture keeps fullscreen");
    AssertTrue(evidenceCaptureSource.Contains("_Fullscreen.png", StringComparison.Ordinal), "manual recognition capture should save a fullscreen image");
    AssertTrue(evidenceCaptureSource.Contains("_Minimap.png", StringComparison.Ordinal), "manual recognition capture should save a minimap crop");
    AssertTrue(evidenceCaptureSource.Contains("_Journal.png", StringComparison.Ordinal), "manual recognition capture should save a journal crop");
    AssertTrue(autoCountSource.Contains("PortQueueGoblinEncounterDebugCapture(source, observation.Source", StringComparison.Ordinal), "automatic accepted counts should continue creating encounter captures automatically");
}

static void TestGoblinVsDebugSimulationControlsUseCountGuards()
{
    string repoRoot = FindRepositoryRootForTests();
    string automationSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.PortedAutomation.cs.cs"));
    string method = ExtractMethodBody(automationSource, "private void PortSimulateGoblinTrackerVsDebugCount");

    AssertTrue(method.Contains("if (!AppSettings.IsVsDebugProfile)", StringComparison.Ordinal), "VS Debug simulation path should be guarded out of release profiles");
    AssertTrue(method.Contains("const string source = \"VsDebugSimulation\"", StringComparison.Ordinal), "simulation count source should be explicit in logs and session records");
    AssertTrue(method.Contains("PortResolveCurrentGoblinArea(source)", StringComparison.Ordinal), "simulation should be able to use the existing current-area resolution path");
    AssertTrue(method.Contains("GoblinAreaResolver.Resolve(selectedArea)", StringComparison.Ordinal), "simulation should use existing area-key resolution for selected test areas");
    AssertTrue(method.Contains("GoblinManualCountBlockList.IsBlocked(area.AreaKey)", StringComparison.Ordinal), "simulation should preserve blocked-area behavior");
    AssertTrue(method.Contains("portGoblinAreaDuplicateGuard.TryAccept(area.AreaKey, out guardResult)", StringComparison.Ordinal), "simulation should consume the existing duplicate guard");
    AssertTrue(method.Contains("suppressionReason = guardResult.AreaLimit > 1 ? \"AreaLimitReached\" : \"AreaAlreadyCounted\"", StringComparison.Ordinal), "simulation should expose duplicate and area-limit suppression paths");
    AssertTrue(method.Contains("DebugManager.Session.RecordGoblinFound(countedRecord)", StringComparison.Ordinal), "accepted simulations should use the same session count path");
    AssertTrue(method.Contains("DebugManager.Session.RecordGoblinFoundRecord(suppressedRecord)", StringComparison.Ordinal), "suppressed simulations should be recorded for diagnostics");
    AssertTrue(method.Contains("GoblinCountAccepted", StringComparison.Ordinal), "accepted simulations should log the standard accepted marker");
    AssertTrue(method.Contains("GoblinCountSuppressed", StringComparison.Ordinal), "suppressed simulations should log the standard suppressed marker");
    AssertTrue(method.Contains("PortPublishManualGoblinCountObservation(area, goblinType, source, guardResult)", StringComparison.Ordinal), "accepted simulations should refresh the Last Observation UI");
    AssertFalse(method.Contains("PortQueueGoblinEncounterDebugCapture", StringComparison.Ordinal), "debug simulations should not create encounter captures or replay artifacts");
}

static void TestGoblinVsDebugSimulationAreaListCoversRouteAndBlockedAreas()
{
    string repoRoot = FindRepositoryRootForTests();
    string automationSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.PortedAutomation.cs.cs"));
    IReadOnlyList<string> areas = GoblinTrackerDebugSimulationAreas.DropdownItems();

    AssertTrue(automationSource.Contains("GoblinTrackerDebugSimulationAreas.DropdownItems()", StringComparison.Ordinal), "VS Debug simulation dropdown should use the centralized area list");
    AssertEqual("Current Area", areas[0], "simulation dropdown should keep Current Area as the first option");
    AssertSequenceEqual(
        areas.Skip(1).OrderBy(area => area, StringComparer.OrdinalIgnoreCase),
        areas.Skip(1),
        "simulation dropdown areas after Current Area should be alphabetized");

    foreach (string expectedArea in new[]
    {
        "Southern Highlands",
        "Northern Highlands",
        "The Weeping Hollow",
        "The Festering Woods",
        "Cathedral Level 3",
        "Royal Crypts",
        "Western Channel Level 2",
        "Eastern Channel Level 2",
        "Stinging Winds",
        "Black Canyon Mines",
        "Battlefields",
        "Rakkis Crossing",
        "Caverns of Frost Level 2",
        "Cave Of The Moon Clan Level 2",
        "Pandemonium Fortress Level 1",
        "Pandemonium Fortress Level 2",
        "City of Caldeum",
        "Gates of Caldeum",
        "Caldeum Bazaar",
        "Flooded Causeway",
        "Ancient Waterway",
        "Western Channel Level 1",
        "Eastern Channel Level 1",
        "The Bridge Of Korsikk",
        "WhimsyDale",
        "New Tristram",
    })
    {
        AssertTrue(areas.Contains(expectedArea, StringComparer.OrdinalIgnoreCase), $"simulation dropdown should include {expectedArea}");
    }

    foreach (string knownArea in GoblinAreaResolver.KnownAreas)
    {
        string areaKey = GoblinAreaResolver.Resolve(knownArea).AreaKey;
        bool countable = !GoblinManualCountBlockList.IsBlocked(areaKey);
        bool blocked = GoblinManualCountBlockList.IsBlocked(areaKey);
        AssertTrue(areas.Contains(areaKey, StringComparer.OrdinalIgnoreCase), $"simulation dropdown should include known area {areaKey}; countable={countable}; blocked={blocked}");
    }

    AssertEqual(areas.Count, areas.Distinct(StringComparer.OrdinalIgnoreCase).Count(), "simulation dropdown should not contain duplicate area entries");
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
        "Live",
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

static void TestDebugPackageBatchUsesLiveEvidenceOnly()
{
    string repoRoot = FindRepositoryRootForTests();
    string evidenceSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs"));
    string evidenceCaptureSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.Captures.cs"));
    string evidenceTimingSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.Timing.cs"));
    string goblinTrackerEventsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinTrackerEvents.cs"));
    string diagnosticsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.Diagnostics.cs"));
    string packageScript = File.ReadAllText(Path.Combine(repoRoot, "Scripts", "create-debug-package.ps1"));
    string configSource = File.ReadAllText(Path.Combine(repoRoot, "Config", "AppSettings.json"));
    string appSettingsSource = File.ReadAllText(Path.Combine(repoRoot, "AppSettings.cs"));
    string sessionStatsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.cs"));
    string autoCountSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.AutoCount.cs"));
    string automationSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.PortedAutomation.cs.cs"));
    string releaseSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.Release.cs"));
    string debugManagerSource = File.ReadAllText(Path.Combine(repoRoot, "DebugManager.cs"));
    string programSource = File.ReadAllText(Path.Combine(repoRoot, "Program.cs"));
    string projectSource = File.ReadAllText(Path.Combine(repoRoot, "GoblinFarmer.csproj"));
    string packageLauncher = File.ReadAllText(Path.Combine(repoRoot, "Scripts", "Create Debug Package.bat"));
    string cleanupLauncher = File.ReadAllText(Path.Combine(repoRoot, "Scripts", "Cleanup Project.bat"));
    string cleanupDeleteLauncher = File.ReadAllText(Path.Combine(repoRoot, "Scripts", "Cleanup Project Delete.bat"));
    string cleanupScriptSource = File.ReadAllText(Path.Combine(repoRoot, "Scripts", "cleanup-project.ps1"));
    string debugAnalysisToolsSource = File.ReadAllText(Path.Combine(repoRoot, "Scripts", "debug-analysis-tools.ps1"));
    string[] expectedBatchScripts =
    [
        "Cleanup Project.bat",
        "Cleanup Project Delete.bat",
        "Create Debug Package.bat",
        "Create Project Brain.bat",
    ];
    string[] expectedActivePowerShellScripts =
    [
        "cleanup-project.ps1",
        "create-debug-package.ps1",
        "create-project-brain.ps1",
        "debug-analysis-tools.ps1",
    ];
    string retiredEvidenceToken = "Goblin" + ("Re" + "play");
    string retiredEvidenceName = "Goblin " + ("Re" + "play");
    string retiredEvidenceSwitch = "--goblin-" + ("re" + "play");
    string retiredReviewCloseMethod = "PortCreate" + retiredEvidenceToken + "ReviewFilesOnVsDebugClose";
    string retiredReviewButtonMethod = "PortCreate" + retiredEvidenceToken + "ReviewFilesFromButton";
    string retiredReviewRunMethod = "PortRun" + retiredEvidenceToken + "ForReview";
    string retiredCliRunMethod = "PortRun" + retiredEvidenceToken + "ForCommandLine";
    string retiredSkipSwitch = "Skip" + retiredEvidenceToken;
    string retiredTerminalScript = "Scripts\\" + ("re" + "play") + "-goblin-evidence.ps1";

    AssertTrue(appSettingsSource.Contains("public bool EnableDecisionTrace { get; set; } = false", StringComparison.Ordinal), "Release decision trace should default off unless Debug Mode enables it");
    AssertTrue(appSettingsSource.Contains("settings.GoblinTracker.EnableDecisionTrace = true", StringComparison.Ordinal), "VS Debug/dev defaults should enable decision trace");
    AssertTrue(configSource.Contains("\"EnableDecisionTrace\": false", StringComparison.Ordinal), "tracked project config should keep decision trace sanitized/off; VS Debug can enable it through debug defaults or AppSettings.local.json");
    AssertFalse(automationSource.Contains("Text = \"Review Files\"", StringComparison.Ordinal), "VS Debug troubleshooting should no longer expose a manual Review Files button");
    AssertFalse(automationSource.Contains(retiredReviewCloseMethod, StringComparison.Ordinal), "VS Debug close should not create loose review files or derived evidence artifacts");
    AssertFalse(evidenceSource.Contains("private void " + retiredReviewCloseMethod, StringComparison.Ordinal), "close-specific derived-evidence review generation should be removed");
    AssertTrue(sessionStatsSource.Contains("SessionSummaryExportSkipped", StringComparison.Ordinal), "VS Debug close should skip session summary export");
    AssertTrue(automationSource.Contains("ReviewArtifactExport|SessionSummaryExport|ShutdownScreenshots", StringComparison.Ordinal), "VS Debug close should log that review export work was skipped");
    AssertTrue(automationSource.Contains("ShutdownCleanupFinished", StringComparison.Ordinal), "VS Debug close should log a fast quiet shutdown finish");
    AssertFalse(automationSource.Contains("txtGoblinScenarioArea", StringComparison.Ordinal), "VS Debug package creation should not require scenario area text input");
    AssertFalse(automationSource.Contains("txtGoblinScenarioGoblin", StringComparison.Ordinal), "VS Debug package creation should not require expected goblin text input");
    AssertFalse(automationSource.Contains("txtGoblinScenarioExpected", StringComparison.Ordinal), "VS Debug package creation should not require expected outcome text input");
    AssertFalse(releaseSource.Contains(retiredReviewButtonMethod, StringComparison.Ordinal), "Release form should not wire the removed VS Debug loose review button");
    AssertFalse(releaseSource.Contains("Create Package", StringComparison.Ordinal), "Release form package UI should remain unchanged unless explicitly requested");
    AssertFalse(automationSource.Contains("OpenFileDialog", StringComparison.Ordinal), "VS Debug close should not ask for a debug package ZIP");
    AssertFalse(automationSource.Contains("FolderBrowserDialog", StringComparison.Ordinal), "VS Debug close should not ask for a folder");
    AssertFalse(automationSource.Contains("InputBox", StringComparison.Ordinal), "VS Debug close should not ask for freeform debug metadata");
    AssertFalse(automationSource.Contains("PortCreateDebugPackage", StringComparison.Ordinal), "VS Debug close should not invoke ZIP package creation");
    AssertFalse(automationSource.Contains(retiredReviewRunMethod, StringComparison.Ordinal), "VS Debug close should not run derived evidence processing");
    AssertFalse(diagnosticsSource.Contains("PortWriteGoblinTrackerNextTestMetadata", StringComparison.Ordinal), "VS Debug should not save retired Next Tests metadata");
    AssertFalse(evidenceSource.Contains("GoblinTrackerNextTests.txt", StringComparison.Ordinal), "VS Debug should not write retired Next Tests metadata");
    AssertFalse(evidenceSource.Contains("PortNextTestStepMetadataLines()", StringComparison.Ordinal), "Next Tests metadata generation should be removed");
    AssertFalse(evidenceSource.Contains("GoblinTrackerNextTestsSaveSkipped", StringComparison.Ordinal), "retired Next Tests close-time logs should be removed");
    AssertFalse(evidenceSource.Contains("PortWriteGoblinTrackerReviewScenarioMetadata", StringComparison.Ordinal), "legacy scenario metadata writer should be removed");
    AssertFalse(debugManagerSource.Contains(retiredEvidenceToken + "Review", StringComparison.Ordinal), "DebugManager should not advertise retired derived-evidence review folders");
    AssertTrue(evidenceCaptureSource.Contains("_Fullscreen", StringComparison.Ordinal), "VS Debug encounter capture should save fullscreen evidence locally");
    AssertTrue(evidenceCaptureSource.Contains("_Minimap", StringComparison.Ordinal), "VS Debug encounter capture should save minimap evidence");
    AssertTrue(evidenceCaptureSource.Contains("_Journal", StringComparison.Ordinal), "VS Debug encounter capture should save journal evidence");
    AssertTrue(programSource.Contains("static void Main()", StringComparison.Ordinal), "app startup should stay on the normal UI path");
    AssertFalse(programSource.Contains(retiredEvidenceSwitch, StringComparison.Ordinal), "app should not expose a retired derived-evidence CLI");
    AssertFalse(programSource.Contains("TryHandleCommandLine", StringComparison.Ordinal), "app startup should not branch into debug command handling");
    AssertFalse(programSource.Contains("ReadArgumentValue", StringComparison.Ordinal), "retired CLI argument parsing should be removed");
    AssertFalse(programSource.Contains(retiredCliRunMethod, StringComparison.Ordinal), "app startup should not call a derived-evidence engine wrapper");
    AssertFalse(evidenceSource.Contains(retiredEvidenceToken, StringComparison.Ordinal), "Goblin evidence code should not retain retired derived-evidence implementation paths");
    AssertFalse(evidenceSource.Contains("ReviewDebugPackageComplete", StringComparison.Ordinal), "app code should not carry a duplicate in-app ZIP package creation flow");
    AssertFalse(evidenceSource.Contains("PortExtractDebugPackagePathFromOutput", StringComparison.Ordinal), "app code should not parse package script output when ZIP export is script-only");
    AssertFalse(evidenceSource.Contains("PortCreateDebugPackage(", StringComparison.Ordinal), "app code should not spawn the ZIP package script from VS Debug review flow");
    AssertTrue(autoCountSource.Contains("PortShouldWriteGoblinDecisionBundle(trace", StringComparison.Ordinal), "live decision traces should throttle repeated suppressed decision bundles");
    AssertTrue(autoCountSource.Contains("PortWriteGoblinDecisionBundle(trace)", StringComparison.Ordinal), "live decision traces should write evidence bundles");
    AssertTrue(autoCountSource.Contains("GoblinDecisionBundleSaved", StringComparison.Ordinal), "live decision bundles should log their saved folder");
    AssertTrue(autoCountSource.Contains("_Metadata.txt", StringComparison.Ordinal), "decision bundles should include replay-ready metadata");
    AssertTrue(autoCountSource.Contains("_Journal.png", StringComparison.Ordinal), "decision bundles should include replay-ready journal crops");
    AssertTrue(autoCountSource.Contains("_Minimap.png", StringComparison.Ordinal), "decision bundles should include replay-ready minimap crops");
    AssertTrue(autoCountSource.Contains("fullImagePolicy=DisabledByDefault", StringComparison.Ordinal), "decision bundles should disable full evidence image copies by default");
    AssertFalse(autoCountSource.Contains("Path.Combine(bundleDirectory, $\"evidence{extension}\")", StringComparison.Ordinal), "decision bundles should not copy full evidence.png by default");
    AssertTrue(File.Exists(Path.Combine(repoRoot, "frmMain.GoblinEvidence.Captures.cs")), "Goblin Evidence capture helpers should be split into a dedicated partial file");
    AssertTrue(File.Exists(Path.Combine(repoRoot, "frmMain.SessionStats.AutoCount.cs")), "automatic count helpers should be split into a dedicated partial file");
    AssertTrue(evidenceCaptureSource.Contains("PortQueueGoblinRecognitionDebugCapture", StringComparison.Ordinal), "manual recognition capture should live in the capture partial");
    AssertTrue(autoCountSource.Contains("PortTryRecordAutomaticGoblinCount", StringComparison.Ordinal), "automatic counting should live in the auto-count partial");
    AssertTrue(evidenceSource.Contains("portGoblinEvidenceTemplateMatCache", StringComparison.Ordinal), "Goblin evidence scans should cache template mats instead of re-reading images every template check");
    AssertTrue(evidenceSource.Contains("PortCreateGoblinEvidenceScanContext", StringComparison.Ordinal), "Goblin evidence scans should capture each scan region once per source pass");
    AssertTrue(evidenceSource.Contains("MinimapCandidate", StringComparison.Ordinal) && evidenceSource.Contains("journalPrimary=True", StringComparison.Ordinal), "normal scans should use minimap-first evidence while preserving journal as the primary confirmation when found");
    AssertTrue(evidenceTimingSource.Contains("GoblinEvidenceTimingSummary", StringComparison.Ordinal), "Goblin Evidence should log scan-stage timing histograms");
    AssertTrue(goblinTrackerEventsSource.Contains("GoblinTrackerEvents.jsonl", StringComparison.Ordinal), "Goblin Tracker structured JSONL events should be written beside Goblin Evidence diagnostics");
    AssertTrue(sessionStatsSource.Contains("PortWriteGoblinTrackerJsonEvent", StringComparison.Ordinal), "observation decisions should mirror to structured JSONL events");
    AssertTrue(autoCountSource.Contains("PortWriteGoblinTrackerJsonEvent", StringComparison.Ordinal), "automatic count decisions should mirror to structured JSONL events");
    AssertTrue(packageScript.Contains("single intentional review package workflow", StringComparison.Ordinal), "debug package script should identify itself as the one review package path");
    AssertTrue(packageScript.Contains("Review export path: single batch/PowerShell ZIP package for VS Debug and Release", StringComparison.Ordinal), "debug package manifest should identify the active review export path");
    AssertTrue(packageScript.Contains("App shutdown artifact creation: skipped by design", StringComparison.Ordinal), "debug package manifest should document quiet app shutdown");
    AssertTrue(packageScript.Contains("debug-analysis-tools.ps1", StringComparison.Ordinal), "debug package script should load the shared debug analysis helper");
    AssertTrue(packageScript.Contains("Write-DgaAnalysisFiles", StringComparison.Ordinal), "debug package script should write analysis reports into the same ZIP workflow");
    AssertTrue(packageScript.Contains("debug-package-analysis.txt", StringComparison.Ordinal), "debug package script should include the root analysis report");
    AssertTrue(packageScript.Contains("goblin-tracker-timeline.md", StringComparison.Ordinal), "debug package script should include the root Goblin Tracker timeline");
    AssertTrue(packageScript.Contains("goblin-evidence-health.txt", StringComparison.Ordinal), "debug package script should include the root Goblin Evidence health report");
    AssertTrue(packageScript.Contains("PSScriptRoot", StringComparison.Ordinal), "debug package script should self-discover from the clicked batch/script location");
    AssertTrue(packageScript.Contains("Resolve-RuntimeRoot", StringComparison.Ordinal), "debug package script should resolve VS Debug and Release runtime roots");
    AssertTrue(packageScript.Contains("Get-PackageRuntimeRoots", StringComparison.Ordinal), "debug package script should search package roots for VS Debug and Release evidence");
    AssertFalse(packageScript.Contains(retiredEvidenceToken, StringComparison.Ordinal), "debug package script should not collect or generate retired derived-evidence artifacts");
    AssertFalse(packageScript.Contains(retiredEvidenceName, StringComparison.Ordinal), "debug package script output should not advertise retired derived-evidence behavior");
    AssertFalse(packageScript.Contains(retiredSkipSwitch, StringComparison.Ordinal), "debug package script should not keep an emergency bypass for a removed workflow");
    AssertFalse(packageScript.Contains(retiredEvidenceSwitch, StringComparison.Ordinal), "debug package script should not invoke the retired CLI");
    AssertFalse(packageScript.Contains("GOBLINFARMER_APPSETTINGS_PATH", StringComparison.Ordinal), "debug package script should not spin up the app for derived evidence processing");
    AssertTrue(packageScript.Contains("session-info.txt", StringComparison.Ordinal), "debug packages should include runtime session metadata");
    AssertTrue(packageScript.Contains("Config\\AppSettings.json", StringComparison.Ordinal), "debug packages should include active runtime config");
    AssertTrue(packageScript.Contains("route-failure-summary.txt", StringComparison.Ordinal), "debug packages should include route failure summaries");
    AssertTrue(packageScript.Contains("debug-screenshot-manifest.txt", StringComparison.Ordinal), "debug packages should include screenshot manifests");
    AssertFalse(packageScript.Contains("GoblinTrackerNextTests.txt", StringComparison.Ordinal), "debug packages should not include retired VS Debug Next Tests metadata");
    AssertFalse(packageScript.Contains("goblin-tracker-next-tests.txt", StringComparison.Ordinal), "debug packages should not include retired root Next Tests metadata");
    AssertFalse(packageScript.Contains("goblin-tracker-scenario.txt", StringComparison.Ordinal), "debug packages should not depend on legacy scenario input metadata");
    AssertTrue(packageScript.Contains("goblin-tracker-summary.txt", StringComparison.Ordinal), "debug packages should include a root Goblin Tracker review summary");
    AssertTrue(packageScript.Contains("goblin-tracker-review.html", StringComparison.Ordinal), "debug packages should include a root review index");
    AssertTrue(packageScript.Contains("Debug\\GoblinEvidence", StringComparison.Ordinal), "debug packages should include current GoblinEvidence diagnostic files");
    AssertTrue(packageScript.Contains("png|jpg|jpeg|bmp|txt|jsonl", StringComparison.Ordinal), "debug packages should include image, text, and structured JSONL evidence from GoblinEvidence folders");
    AssertTrue(packageScript.Contains("Live evidence artifacts:", StringComparison.Ordinal), "debug package summary should report live evidence rather than derived output");
    AssertTrue(packageScript.Contains("DecisionBundles", StringComparison.Ordinal), "debug packages should include live decision bundles");
    AssertTrue(packageScript.Contains("IncludeGoblinDecisionBundleFullImages", StringComparison.Ordinal), "debug packages should make full decision-bundle evidence images opt-in");
    AssertTrue(packageScript.Contains("Debug\\GoblinEvidence\\DecisionBundles\\evidence.* full images are excluded by default", StringComparison.Ordinal), "debug packages should exclude old full decision-bundle evidence images by default");
    AssertTrue(packageScript.Contains("IncludeGoblinCaptureFullscreenImages", StringComparison.Ordinal), "debug packages should make encounter/manual fullscreen images opt-in");
    AssertTrue(packageScript.Contains("EncounterCaptures and ManualCaptures *_Fullscreen images are excluded by default", StringComparison.Ordinal), "debug packages should keep replay crops while excluding fullscreen capture images by default");
    AssertTrue(packageScript.Contains("EncounterCaptures", StringComparison.Ordinal), "debug packages should include live encounter captures");
    AssertTrue(packageScript.Contains("ObservationDiagnostics", StringComparison.Ordinal), "debug packages should include live observation diagnostics");
    AssertFalse(packageScript.Contains("generated automatically as loose files", StringComparison.Ordinal), "debug package script should not point to the old form-close loose review flow");
    AssertTrue(packageLauncher.Contains("Supported debug ZIP export path for both VS Debug and Release", StringComparison.Ordinal), "debug package launcher should identify itself as the single review export path");
    AssertTrue(packageLauncher.Contains("single intentional review package workflow", StringComparison.Ordinal), "debug package launcher should make the batch workflow clear");
    AssertTrue(packageLauncher.Contains("create-debug-package.ps1", StringComparison.Ordinal), "debug package launcher should delegate to the PowerShell package script");
    AssertTrue(cleanupLauncher.Contains("Default mode is DRY RUN", StringComparison.Ordinal), "cleanup launcher should identify dry-run default");
    AssertTrue(cleanupLauncher.Contains("cleanup-project.ps1", StringComparison.Ordinal), "cleanup launcher should delegate to the PowerShell cleanup script");
    AssertTrue(cleanupDeleteLauncher.Contains("choice /C YN", StringComparison.Ordinal), "cleanup delete launcher should require confirmation");
    AssertTrue(cleanupDeleteLauncher.Contains("cleanup-project.ps1", StringComparison.Ordinal), "cleanup delete launcher should delegate to the PowerShell cleanup script");
    AssertTrue(cleanupDeleteLauncher.Contains("-Delete", StringComparison.Ordinal), "cleanup delete launcher should pass the explicit delete flag");
    AssertFalse(cleanupDeleteLauncher.Contains("-RuntimeArtifacts", StringComparison.Ordinal), "cleanup delete launcher should not include optional runtime artifacts by default");
    AssertFalse(cleanupDeleteLauncher.Contains("-PruneOldInstallers", StringComparison.Ordinal), "cleanup delete launcher should not prune old installers by default");
    AssertTrue(cleanupScriptSource.Contains("[switch]$Delete", StringComparison.Ordinal), "cleanup script should require an explicit delete switch");
    AssertTrue(cleanupScriptSource.Contains("DRY RUN", StringComparison.Ordinal), "cleanup script should clearly report dry-run mode");
    AssertTrue(cleanupScriptSource.Contains("Refusing to consider path outside project root", StringComparison.Ordinal), "cleanup script should guard against paths outside the project root");
    AssertTrue(cleanupScriptSource.Contains("Cleanup_Report.md", StringComparison.Ordinal), "cleanup script should write the cleanup report");
    AssertTrue(cleanupScriptSource.Contains("DebugPackageRetention = 10", StringComparison.Ordinal), "cleanup script should default to active-testing debug package retention");
    AssertTrue(cleanupScriptSource.Contains("RuntimeArtifacts", StringComparison.Ordinal), "cleanup script should gate runtime artifact cleanup behind an explicit flag");
    AssertTrue(cleanupScriptSource.Contains("PruneOldInstallers", StringComparison.Ordinal), "cleanup script should gate old installer pruning behind an explicit flag");
    AssertTrue(debugAnalysisToolsSource.Contains("New-DgaDebugPackageAnalysisContent", StringComparison.Ordinal), "shared debug helper should build the package analysis report");
    AssertTrue(debugAnalysisToolsSource.Contains("New-DgaGoblinTrackerTimelineContent", StringComparison.Ordinal), "shared debug helper should build the Goblin Tracker timeline");
    AssertTrue(debugAnalysisToolsSource.Contains("New-DgaGoblinEvidenceHealthContent", StringComparison.Ordinal), "shared debug helper should build the evidence health report");
    AssertTrue(projectSource.Contains("Scripts\\create-debug-package.ps1", StringComparison.Ordinal), "release/export ZIP package script should remain published");
    AssertTrue(projectSource.Contains("Scripts\\Create Debug Package.bat", StringComparison.Ordinal), "release/export ZIP package launcher should remain published");
    AssertTrue(projectSource.Contains("Scripts\\debug-analysis-tools.ps1", StringComparison.Ordinal), "shared debug package helper should remain published because the package script dot-sources it");
    string[] activeBatchScripts = Directory.GetFiles(Path.Combine(repoRoot, "Scripts"), "*.bat", SearchOption.TopDirectoryOnly)
        .Select(Path.GetFileName)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToArray()!;
    string[] activePowerShellScripts = Directory.GetFiles(Path.Combine(repoRoot, "Scripts"), "*.ps1", SearchOption.TopDirectoryOnly)
        .Select(Path.GetFileName)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToArray()!;
    AssertSequenceEqual(expectedBatchScripts.OrderBy(name => name, StringComparer.OrdinalIgnoreCase), activeBatchScripts, "Scripts should expose the two package launchers plus the documented cleanup maintenance launcher");
    AssertSequenceEqual(expectedActivePowerShellScripts.OrderBy(name => name, StringComparer.OrdinalIgnoreCase), activePowerShellScripts, "Scripts should keep package scripts, direct debug-package helper, and cleanup maintenance script");
    foreach (string scriptName in expectedActivePowerShellScripts)
    {
        AssertTrue(File.Exists(Path.Combine(repoRoot, "Scripts", scriptName)), $"{scriptName} should exist under Scripts");
    }
    AssertTrue(projectSource.Contains("Scripts\\debug-analysis-tools.ps1", StringComparison.Ordinal), "debug-analysis-tools.ps1 should be copied to VS Debug and Release publish outputs");
    AssertFalse(projectSource.Contains("Scripts\\analyze-latest-debug-package.ps1", StringComparison.Ordinal), "archived optional package analyzer should not be published");
    AssertFalse(projectSource.Contains("Scripts\\build-goblin-tracker-timeline.ps1", StringComparison.Ordinal), "archived optional timeline helper should not be published");
    AssertFalse(projectSource.Contains("Scripts\\check-goblin-evidence-health.ps1", StringComparison.Ordinal), "archived optional evidence-health helper should not be published");
    AssertFalse(projectSource.Contains("Scripts\\dev-verify.ps1", StringComparison.Ordinal), "archived local verification helper should not be published");
    AssertFalse(projectSource.Contains(retiredTerminalScript, StringComparison.Ordinal), "removed terminal evidence helper should not be published");
    AssertFalse(File.Exists(Path.Combine(repoRoot, "Scripts", ("re" + "play-") + "goblin-evidence.ps1")), "duplicate terminal evidence package reviewer should be removed");
}

static void TestGoblinAutomaticCountingRequiresFreshArmedEvidence()
{
    string repoRoot = FindRepositoryRootForTests();
    string sessionStatsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.cs"));
    string autoCountSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.AutoCount.cs"));
    string evidenceSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs"));
    string evidenceModelSource = File.ReadAllText(Path.Combine(repoRoot, "GoblinEvidence.cs"));
    string automationSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.PortedAutomation.cs.cs"));
    string observeMethod = ExtractMethodBody(sessionStatsSource, "private bool PortObserveGoblinCandidate");
    string autoCountMethod = ExtractMethodBody(autoCountSource, "private bool PortTryRecordAutomaticGoblinCount");

    AssertTrue(automationSource.Contains("portGoblinAutoCountEvidenceBySignature", StringComparison.Ordinal), "automatic counting should remember evidence signatures");
    AssertTrue(evidenceSource.Contains("PortGoblinEvidenceSignature(candidate)", StringComparison.Ordinal), "journal/minimap candidates should carry a stable evidence signature");
    AssertTrue(evidenceSource.Contains("PortGoblinEvidenceNoteValue(candidate.Notes, \"Template\")", StringComparison.Ordinal), "evidence signatures should include the template name");
    AssertTrue(evidenceSource.Contains("PortGoblinEvidenceNoteValue(candidate.Notes, \"Kind\")", StringComparison.Ordinal), "evidence signatures should include the evidence kind");
    AssertTrue(evidenceSource.Contains("PortGoblinEvidenceJournalLineBucket(candidate.Notes)", StringComparison.Ordinal), "journal evidence signatures should include a stable journal-row bucket");
    AssertTrue(autoCountSource.Contains("GoblinAreaResolver.NormalizedKey(observation.AreaKey)", StringComparison.Ordinal), "automatic evidence signatures should be scoped by resolved area key");
    AssertTrue(autoCountSource.Contains("PortGoblinAutoCountGlobalEvidenceKey", StringComparison.Ordinal), "automatic counting should also track the underlying journal row independent of current area");
    AssertFalse(ExtractMethodBody(evidenceSource, "private static string PortGoblinEvidenceSignature").Contains("MatchPoint", StringComparison.Ordinal), "evidence signatures should not include volatile match points");
    AssertFalse(ExtractMethodBody(evidenceSource, "private static string PortGoblinEvidenceSignature").Contains("candidate.Notes.Trim()", StringComparison.Ordinal), "evidence signatures should not include the whole diagnostic note string");
    AssertTrue(autoCountMethod.IndexOf("portGoblinAutoCountEvidenceBySignature[autoEvidenceKey] = evidenceState", StringComparison.Ordinal) < autoCountMethod.IndexOf("AutomaticCountingDisabled", StringComparison.Ordinal), "auto-count evidence should be remembered before the disabled gate returns");
    AssertTrue(autoCountMethod.Contains("EvidenceSeenBeforeAutoCountEnabled", StringComparison.Ordinal), "automatic counting should suppress evidence seen before the auto-count gate was armed");
    AssertTrue(autoCountMethod.Contains("EvidenceAlreadyAutoCounted", StringComparison.Ordinal), "automatic counting should suppress the same evidence signature after it counts once");
    AssertFalse(autoCountMethod.Contains("PortAllowsLinkedJournalAreaRepeat", StringComparison.Ordinal), "automatic counting should not bypass exact evidence suppression for linked Caverns levels");
    AssertTrue(autoCountMethod.Contains("EncounterAlreadyAutoCounted", StringComparison.Ordinal), "automatic counting should suppress the same counted encounter when a source/template variant appears later");
    AssertTrue(autoCountMethod.Contains("PortShouldSuppressEncounterAlreadyAutoCounted(observation, area, globalEvidenceKey", StringComparison.Ordinal), "automatic counting should use encounter-level protection in addition to area-scoped exact signatures");
    AssertTrue(observeMethod.Contains("EncounterAlreadyAutoCounted", StringComparison.Ordinal), "observation summaries should report cross-area journal repeats as not countable before auto-count attempts run");
    AssertTrue(observeMethod.Contains("PortShouldSuppressEncounterAlreadyAutoCounted(observationSource, goblinType", StringComparison.Ordinal), "observation summaries should preflight journal/minimap encounter variants before the auto-count attempt");
    AssertTrue(observeMethod.Contains("PortShouldPreserveDisplayedObservationAgainstIncoming", StringComparison.Ordinal), "suppressed old journal repeats should not replace the Last Observation display");
    AssertTrue(automationSource.Contains("portGoblinAutoCountEncounterByGoblinType", StringComparison.Ordinal), "automatic counting should remember recently counted goblin types across source/template variants");
    AssertTrue(automationSource.Contains("PortAutomaticGoblinJournalEncounterSuppressWindow", StringComparison.Ordinal), "cross-area journal suppression should use an explicit bounded window");
    AssertTrue(automationSource.Contains("PortAutomaticGoblinSourceVariantSuppressWindow", StringComparison.Ordinal), "cross-source stale text suppression should use an explicit recent-variant window");
    AssertTrue(autoCountSource.Contains("GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress", StringComparison.Ordinal), "automatic counting should share a testable encounter suppression policy");
    AssertTrue(autoCountSource.Contains("GoblinAutoCountEvidenceReliabilityPolicy.AllowsAutomaticCount", StringComparison.Ordinal), "automatic counting should require reliable evidence before incrementing");
    AssertTrue(autoCountSource.Contains("PortObservationPendingJournalPromotedByReliability", StringComparison.Ordinal), "sustained active Engaged journal evidence should be able to promote a pending observation into the normal count guards");
    AssertTrue(observeMethod.Contains("GoblinAutoCountEvidenceReliabilityPolicy.AllowsAutomaticCount", StringComparison.Ordinal), "observation summaries should show pending Engaged-only journal evidence before auto-count attempts run");
    AssertTrue(evidenceModelSource.Contains("JournalPendingKilledOrMinimapConfirmation", StringComparison.Ordinal), "Engaged-only journal evidence should have an explicit pending-confirmation reason");
    AssertTrue(autoCountSource.Contains("refreshEncounterLastSeen", StringComparison.Ordinal), "suppressed source variants should refresh encounter last-seen state instead of expiring from the original count time");
    AssertTrue(autoCountSource.Contains("EvidenceKey = string.IsNullOrWhiteSpace(globalEvidenceKey)", StringComparison.Ordinal), "suppressed source variants should refresh the encounter evidence key so old journal rows cannot replay after area changes");
    AssertTrue(evidenceModelSource.Contains("SameEvidenceKey", StringComparison.Ordinal), "encounter suppression should still compare exact area-independent evidence keys");
    AssertTrue(evidenceModelSource.Contains("JournalLineBucket", StringComparison.Ordinal), "encounter suppression should treat nearby journal row buckets as the same visible row");
    AssertTrue(evidenceModelSource.Contains("RecentSourceVariant", StringComparison.Ordinal), "encounter suppression should block quick Journal/Minimap variants from double-counting one encounter");
    AssertTrue(evidenceModelSource.Contains("RecentSourceVariantLastSeen", StringComparison.Ordinal), "source variants should remain suppressed while the same stale encounter is still being seen");
    AssertTrue(autoCountSource.Contains("PortGoblinEvidenceHash", StringComparison.Ordinal), "accepted and suppressed auto-count logs should include a compact evidence hash");
    AssertTrue(autoCountMethod.Contains("encounterMatch=", StringComparison.Ordinal), "auto-count logs should include the duplicate encounter match reason");
    AssertTrue(autoCountMethod.Contains("StaleEvidence", StringComparison.Ordinal), "automatic counting should suppress stale evidence signatures");
    AssertTrue(autoCountMethod.Contains("Goblin auto-counted", StringComparison.Ordinal), "automatic counting should show a visible notification when it increments");
    AssertTrue(autoCountMethod.Contains("GoblinLatencyTrace", StringComparison.Ordinal), "automatic counting should log count-to-notification latency diagnostics");
    AssertTrue(autoCountMethod.Contains("RAINBOW GOBLIN!", StringComparison.Ordinal), "automatic counting should show a special Rainbow Goblin alert");
    AssertTrue(autoCountMethod.Contains("System.Media.SystemSounds.Exclamation.Play()", StringComparison.Ordinal), "Rainbow Goblin automatic counts should play a local alert sound");
    AssertTrue(automationSource.Contains("WS_EX_TRANSPARENT", StringComparison.Ordinal), "notification splash should be click-through so it does not block teleport clicks");
    AssertTrue(sessionStatsSource.Contains("PortResetGoblinAutoCountEvidenceState(\"TrackerStatsReset\")", StringComparison.Ordinal), "Reset Stats should clear auto-count evidence signatures");
    AssertTrue(sessionStatsSource.Contains("PortResetGoblinAutoCountEvidenceState(\"NewGameCreated\")", StringComparison.Ordinal), "New Game should clear auto-count evidence signatures");
    AssertTrue(sessionStatsSource.Contains("DebugManager.Session.ResetGoblinTrackerStats()", StringComparison.Ordinal), "New Game should reset GoblinCount, GPH source time, and found records like Reset Stats");
    AssertTrue(evidenceSource.Contains("clearedAutoCountEvidence", StringComparison.Ordinal), "evidence-state reset logs should report auto-count signature clearing");
    AssertTrue(evidenceSource.Contains("clearedAutoCountEncounters", StringComparison.Ordinal), "evidence-state reset logs should report auto-count encounter clearing");
}

static void TestGoblinAutomaticCountReliabilityRequiresKilledOrMinimapConfirmation()
{
    AssertFalse(
        GoblinAutoCountEvidenceReliabilityPolicy.AllowsAutomaticCount(
            "Journal",
            "JournalEncounter|Journal|Treasure Goblin|Template=Treasure Goblin Engaged Journal.png|Kind=JournalEngaged|LineBucket=10",
            out string engagedReason,
            out string engagedReliability),
        "Engaged-only journal evidence should wait for stronger confirmation before automatic counting");
    AssertEqual(
        GoblinAutoCountEvidenceReliabilityPolicy.JournalPendingKilledOrMinimapConfirmation,
        engagedReason,
        "Engaged-only journal evidence should report the pending-confirmation reason");
    AssertEqual("JournalEngagedOnly", engagedReliability, "Engaged-only journal reliability should be explicit in logs and JSONL");

    AssertTrue(
        GoblinAutoCountEvidenceReliabilityPolicy.AllowsAutomaticCount(
            "Journal",
            "JournalEncounter|Journal|Treasure Goblin|Template=Treasure Goblin Engaged Journal.png|Kind=JournalEngaged|LineBucket=10",
            evidenceFirstSeenAgeSeconds: 2.5,
            combatActive: true,
            out string sustainedEngagedReason,
            out string sustainedEngagedReliability),
        "same-area active Engaged journal evidence sustained past the confirmation window should count");
    AssertEqual("", sustainedEngagedReason, "sustained Engaged journal evidence should not return a suppression reason");
    AssertEqual(
        GoblinAutoCountEvidenceReliabilityPolicy.JournalEngagedSustainedActiveCombat,
        sustainedEngagedReliability,
        "sustained Engaged journal reliability should be explicit");

    AssertFalse(
        GoblinAutoCountEvidenceReliabilityPolicy.AllowsAutomaticCount(
            "Journal",
            "JournalEncounter|Journal|Treasure Goblin|Template=Treasure Goblin Engaged Journal.png|Kind=JournalEngaged|LineBucket=10",
            evidenceFirstSeenAgeSeconds: 12,
            combatActive: true,
            out string oldEngagedReason,
            out _),
        "old Engaged journal evidence should not become countable after the sustained confirmation window");
    AssertEqual(
        GoblinAutoCountEvidenceReliabilityPolicy.JournalPendingKilledOrMinimapConfirmation,
        oldEngagedReason,
        "old Engaged journal evidence should continue to report the pending-confirmation reason");

    AssertFalse(
        GoblinAutoCountEvidenceReliabilityPolicy.AllowsAutomaticCount(
            "JournalCandidate",
            "JournalEncounter|Journal|Blood Thief|Template=Blood Thief Engaged & Killed Journal.png|Kind=JournalEngagedAndKilled|LineBucket=8",
            out string combinedReason,
            out _),
        "Combined Engaged/Killed templates are still Engaged-first evidence and should wait for a killed/minimap confirmation");
    AssertEqual(
        GoblinAutoCountEvidenceReliabilityPolicy.JournalPendingKilledOrMinimapConfirmation,
        combinedReason,
        "combined templates should use the same pending-confirmation reason");

    AssertTrue(
        GoblinAutoCountEvidenceReliabilityPolicy.AllowsAutomaticCount(
            "Journal",
            "JournalKill|Journal|Treasure Goblin|Template=Treasure Goblin Killed Journal.png|Kind=JournalKilled|LineBucket=10",
            out string killedReason,
            out string killedReliability),
        "fresh killed journal evidence should be strong enough for automatic counting");
    AssertEqual("", killedReason, "killed journal evidence should not return a suppression reason");
    AssertEqual("JournalKilledConfirmed", killedReliability, "killed journal reliability should be explicit");

    AssertTrue(
        GoblinAutoCountEvidenceReliabilityPolicy.AllowsAutomaticCount(
            "MinimapCandidate",
            "MinimapIcon|Minimap|Treasure Goblin|Template=Treasure Goblin Minimap.png|Kind=Minimap|LineBucket=",
            out string minimapReason,
            out string minimapReliability),
        "minimap evidence should stay eligible after confidence gating");
    AssertEqual("", minimapReason, "minimap evidence should not return a reliability suppression reason");
    AssertEqual("MinimapConfirmed", minimapReliability, "minimap reliability should be explicit");
}

static void TestGoblinAutoCountSourceVariantSuppressionUsesRecentLastSeenState()
{
    DateTime countedUtc = DateTime.UtcNow - TimeSpan.FromSeconds(21);
    DateTime lastSeenUtc = DateTime.UtcNow - TimeSpan.FromSeconds(1);
    DateTime nowUtc = DateTime.UtcNow;
    bool suppressed = GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress(
        source: "Journal",
        goblinType: "Treasure Goblin",
        areaKey: "Fields of Slaughter",
        globalEvidenceKey: "Journal|Treasure Goblin|fields of slaughter|JournalEncounter|Journal|Treasure Goblin|Template=Treasure Goblin Engaged Journal.png|Kind=JournalEngaged|LineBucket=11",
        countedGoblinType: "Treasure Goblin",
        countedAreaKey: "Battlefields",
        countedSource: "Journal",
        countedEvidenceKey: "Journal|Treasure Goblin|battlefields|JournalEncounter|Journal|Treasure Goblin|Template=Treasure Goblin Engaged Journal.png|Kind=JournalEngaged|LineBucket=11",
        countedUtc: countedUtc,
        lastSeenUtc: lastSeenUtc,
        nowUtc: nowUtc,
        encounterSuppressWindow: TimeSpan.FromMinutes(10),
        sourceVariantWindow: TimeSpan.FromSeconds(20),
        out string matchReason);

    AssertTrue(suppressed, "journal evidence should remain suppressed when the prior counted encounter was just seen as stale text, even after 20 seconds from the original count");
    AssertTrue(matchReason.StartsWith("JournalLineBucket", StringComparison.Ordinal) || matchReason.StartsWith("RecentSourceVariantLastSeen", StringComparison.Ordinal), "suppression should explain whether it matched by row bucket or recent last-seen source variant");

    bool oldVariantSuppressed = GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress(
        source: "Journal",
        goblinType: "Treasure Goblin",
        areaKey: "Fields of Slaughter",
        globalEvidenceKey: "Journal|Treasure Goblin|fields of slaughter|JournalEncounter|Journal|Treasure Goblin|Template=Treasure Goblin Engaged Journal.png|Kind=JournalEngaged|LineBucket=4",
        countedGoblinType: "Treasure Goblin",
        countedAreaKey: "Battlefields",
        countedSource: "Journal",
        countedEvidenceKey: "Journal|Treasure Goblin|battlefields|JournalEncounter|Journal|Treasure Goblin|Template=Treasure Goblin Engaged Journal.png|Kind=JournalEngaged|LineBucket=11",
        countedUtc: nowUtc - TimeSpan.FromSeconds(45),
        lastSeenUtc: nowUtc - TimeSpan.FromSeconds(30),
        nowUtc: nowUtc,
        encounterSuppressWindow: TimeSpan.FromMinutes(10),
        sourceVariantWindow: TimeSpan.FromSeconds(20),
        out _);

    AssertFalse(oldVariantSuppressed, "a same-type journal row with no bucket match and no recent last-seen continuity should not be globally blocked forever");
}

static void TestGoblinPandemoniumMultiCountDuplicateBypassStaysBounded()
{
    DateTime countedUtc = new(2026, 6, 9, 6, 0, 42, DateTimeKind.Utc);
    string pf1 = GoblinAreaResolver.Resolve("Pandemonium Fortress Level 1").AreaKey;
    string pf2 = GoblinAreaResolver.Resolve("Pandemonium Fortress Level 2").AreaKey;
    const string minimapEvidence = "Minimap|Blood Thief|MinimapIcon|Minimap|Blood Thief|Template=Blood Thief Minimap.png|Kind=Minimap|LineBucket=";
    const string engagedEvidence = "Journal|Blood Thief|JournalEncounter|Journal|Blood Thief|Template=Blood Thief Engaged.png|Kind=JournalEngaged|LineBucket=10";
    const string killedEvidence = "Journal|Blood Thief|JournalKill|Journal|Blood Thief|Template=Blood Thief Killed Journal.png|Kind=JournalKilled|LineBucket=10";

    GoblinAreaDuplicateGuard pf2Guard = new();
    AssertTrue(pf2Guard.TryAccept(pf2, out GoblinAreaDuplicateGuardResult pf2First), "PF2 first goblin should count through the normal duplicate guard");
    AssertEqual(1, pf2First.AreaCount, "PF2 first goblin should consume the first slot");
    AssertEqual(2, pf2First.AreaLimit, "PF2 should keep a two-count limit");

    GoblinAreaDuplicateGuardResult pf2PeekAfterFirst = pf2Guard.Peek(pf2);
    AssertFalse(
        GoblinPandemoniumMultiCountDuplicatePolicy.ShouldBypass(
            "Minimap",
            pf2,
            pf2PeekAfterFirst.AreaCount,
            pf2PeekAfterFirst.AreaLimit,
            pf2,
            countedUtc,
            countedUtc.AddSeconds(4),
            minimapEvidence,
            0.865,
            0.85,
            4,
            combatActive: true,
            out string immediateReason,
            out double immediateElapsed),
        "PF2 immediate same-signature minimap duplicate should not bypass exact evidence suppression");
    AssertEqual("ElapsedTooShort", immediateReason, "immediate PF2 duplicate should explain the conservative time gate");
    AssertTrue(immediateElapsed < GoblinPandemoniumMultiCountDuplicatePolicy.MinimumElapsedSinceLastAccepted.TotalSeconds, "immediate duplicate elapsed time should be below the threshold");

    AssertTrue(
        GoblinPandemoniumMultiCountDuplicatePolicy.ShouldBypass(
            "Minimap",
            pf2,
            pf2PeekAfterFirst.AreaCount,
            pf2PeekAfterFirst.AreaLimit,
            pf2,
            countedUtc,
            countedUtc.AddSeconds(10),
            minimapEvidence,
            0.865,
            0.85,
            10,
            combatActive: true,
            out string secondReason,
            out double secondElapsed),
        "PF2 second same-signature minimap evidence after the threshold should bypass duplicate evidence suppression");
    AssertEqual("Allowed", secondReason, "PF2 second same-signature minimap should report an allowed duplicate bypass");
    AssertTrue(secondElapsed >= GoblinPandemoniumMultiCountDuplicatePolicy.MinimumElapsedSinceLastAccepted.TotalSeconds, "PF2 second duplicate elapsed time should satisfy the threshold");
    AssertTrue(pf2Guard.TryAccept(pf2, out GoblinAreaDuplicateGuardResult pf2Second), "PF2 second goblin should still consume only the second area slot");
    AssertEqual(2, pf2Second.AreaCount, "PF2 second goblin should fill the two-count limit");

    GoblinAreaDuplicateGuardResult pf2PeekAfterSecond = pf2Guard.Peek(pf2);
    AssertFalse(
        GoblinPandemoniumMultiCountDuplicatePolicy.ShouldBypass(
            "Minimap",
            pf2,
            pf2PeekAfterSecond.AreaCount,
            pf2PeekAfterSecond.AreaLimit,
            pf2,
            countedUtc.AddSeconds(10),
            countedUtc.AddSeconds(20),
            minimapEvidence,
            0.865,
            0.85,
            20,
            combatActive: true,
            out string thirdReason,
            out _),
        "PF2 third same-signature duplicate should not bypass once both PF slots are consumed");
    AssertEqual("AreaLimitReached", thirdReason, "PF2 third duplicate should report the area-limit gate");
    AssertFalse(pf2Guard.TryAccept(pf2, out GoblinAreaDuplicateGuardResult pf2Third), "PF2 third goblin should still suppress through the duplicate guard");
    AssertEqual(2, pf2Third.AreaLimit, "PF2 third duplicate should retain the two-count limit");

    GoblinAreaDuplicateGuard pf1Guard = new();
    AssertTrue(pf1Guard.TryAccept(pf1), "PF1 first goblin should count through the normal duplicate guard");
    GoblinAreaDuplicateGuardResult pf1Peek = pf1Guard.Peek(pf1);
    AssertTrue(
        GoblinPandemoniumMultiCountDuplicatePolicy.ShouldBypass(
            "Minimap",
            pf1,
            pf1Peek.AreaCount,
            pf1Peek.AreaLimit,
            pf1,
            countedUtc,
            countedUtc.AddSeconds(10),
            minimapEvidence,
            0.865,
            0.85,
            10,
            combatActive: true,
            out _,
            out _),
        "PF1 should use the same bounded second-goblin duplicate bypass as PF2");

    GoblinAreaDuplicateGuard weepingGuard = new();
    string weeping = GoblinAreaResolver.Resolve("The Weeping Hollow").AreaKey;
    AssertTrue(weepingGuard.TryAccept(weeping), "non-PF first goblin should count normally");
    GoblinAreaDuplicateGuardResult weepingPeek = weepingGuard.Peek(weeping);
    AssertFalse(
        GoblinPandemoniumMultiCountDuplicatePolicy.ShouldBypass(
            "Minimap",
            weeping,
            weepingPeek.AreaCount,
            weepingPeek.AreaLimit,
            weeping,
            countedUtc,
            countedUtc.AddSeconds(10),
            minimapEvidence,
            0.865,
            0.85,
            10,
            combatActive: true,
            out string nonPfReason,
            out _),
        "non-PF same-signature evidence should not use the PF duplicate bypass");
    AssertEqual("NotPandemoniumFortressTwoCountArea", nonPfReason, "non-PF duplicate bypass rejection should be explicit");

    AssertFalse(
        GoblinPandemoniumMultiCountDuplicatePolicy.ShouldBypass(
            "Minimap",
            pf2,
            pf2PeekAfterFirst.AreaCount,
            pf2PeekAfterFirst.AreaLimit,
            "Western Channel Level 2",
            countedUtc,
            countedUtc.AddSeconds(10),
            minimapEvidence,
            0.865,
            0.85,
            10,
            combatActive: true,
            out string previousAreaReason,
            out _),
        "same goblin evidence from a prior area should not bypass stale-area protection");
    AssertEqual("PreviousAcceptedDifferentArea", previousAreaReason, "previous-area duplicate bypass rejection should be explicit");

    AssertTrue(
        GoblinPandemoniumMultiCountDuplicatePolicy.ShouldBypass(
            "Journal",
            pf2,
            pf2PeekAfterFirst.AreaCount,
            pf2PeekAfterFirst.AreaLimit,
            pf2,
            countedUtc,
            countedUtc.AddSeconds(10),
            engagedEvidence,
            0.92,
            0.85,
            3,
            combatActive: true,
            out _,
            out _),
        "sustained current-area PF Engaged journal evidence can support a second-goblin duplicate bypass");

    AssertFalse(
        GoblinPandemoniumMultiCountDuplicatePolicy.ShouldBypass(
            "Journal",
            pf2,
            pf2PeekAfterFirst.AreaCount,
            pf2PeekAfterFirst.AreaLimit,
            pf2,
            countedUtc,
            countedUtc.AddSeconds(10),
            killedEvidence,
            0.92,
            0.85,
            3,
            combatActive: true,
            out string noFreshSupportReason,
            out _),
        "same-signature killed journal evidence should not be enough by itself because old killed rows are high-risk stale evidence");
    AssertEqual("NoFreshSupportingEvidence", noFreshSupportReason, "journal killed duplicate rejection should identify the missing fresh support");
}

static void TestGoblinAutoCountMinimapCollisionAllowsNewAreas()
{
    DateTime nowUtc = DateTime.UtcNow;
    const string sharedMinimapKey = "Minimap|Gem Hoarder|MinimapIcon|Minimap|Gem Hoarder|Template=Gem Hoarder Minimap.png|Kind=Minimap|LineBucket=";

    bool crossAreaSuppressed = GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress(
        source: "Minimap",
        goblinType: "Gem Hoarder",
        areaKey: "Rakkis Crossing",
        globalEvidenceKey: sharedMinimapKey,
        countedGoblinType: "Gem Hoarder",
        countedAreaKey: "Western Channel Level 1",
        countedSource: "Minimap",
        countedEvidenceKey: sharedMinimapKey,
        countedUtc: nowUtc - TimeSpan.FromSeconds(30),
        lastSeenUtc: nowUtc - TimeSpan.FromSeconds(30),
        nowUtc: nowUtc,
        encounterSuppressWindow: TimeSpan.FromMinutes(10),
        sourceVariantWindow: TimeSpan.FromSeconds(45),
        out string crossAreaReason);

    AssertFalse(crossAreaSuppressed, "a repeated minimap icon signature in a different area should not suppress a fresh same-type goblin");
    AssertTrue(string.IsNullOrWhiteSpace(crossAreaReason), "cross-area minimap collisions should not report a duplicate match reason");

    bool sameAreaSuppressed = GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress(
        source: "Minimap",
        goblinType: "Gem Hoarder",
        areaKey: "Western Channel Level 1",
        globalEvidenceKey: sharedMinimapKey,
        countedGoblinType: "Gem Hoarder",
        countedAreaKey: "Western Channel Level 1",
        countedSource: "Minimap",
        countedEvidenceKey: sharedMinimapKey,
        countedUtc: nowUtc - TimeSpan.FromSeconds(30),
        lastSeenUtc: nowUtc - TimeSpan.FromSeconds(30),
        nowUtc: nowUtc,
        encounterSuppressWindow: TimeSpan.FromMinutes(10),
        sourceVariantWindow: TimeSpan.FromSeconds(45),
        out string sameAreaReason);

    AssertTrue(sameAreaSuppressed, "the same minimap signature in the same area should still suppress duplicate scans");
    AssertEqual("SameEvidenceKey", sameAreaReason, "same-area minimap duplicate suppression should keep its existing reason");
}

static void TestGoblinAutoCountDelayedJournalAfterMinimapSuppresses()
{
    DateTime nowUtc = DateTime.UtcNow;
    bool suppressed = GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress(
        source: "Journal",
        goblinType: "Treasure Goblin",
        areaKey: "Black Canyon Mines",
        globalEvidenceKey: "Journal|Treasure Goblin|JournalKill|Journal|Treasure Goblin|Template=Treasure Goblin Killed Journal.png|Kind=JournalKilled|LineBucket=11",
        countedGoblinType: "Treasure Goblin",
        countedAreaKey: "Stinging Winds",
        countedSource: "Minimap",
        countedEvidenceKey: "Minimap|Treasure Goblin|MinimapIcon|Minimap|Treasure Goblin|Template=Treasure Goblin Minimap.png|Kind=Minimap|LineBucket=",
        countedUtc: nowUtc - TimeSpan.FromSeconds(29),
        lastSeenUtc: nowUtc - TimeSpan.FromSeconds(29),
        nowUtc: nowUtc,
        encounterSuppressWindow: TimeSpan.FromMinutes(10),
        sourceVariantWindow: TimeSpan.FromSeconds(45),
        out string matchReason);

    AssertTrue(suppressed, "a delayed journal row inside the freshness window should attach to the recent minimap count instead of counting a new area");
    AssertEqual("RecentSourceVariant:Minimap->Journal", matchReason, "delayed minimap-to-journal suppression should explain the source variant");
}

static void TestGoblinAutoCountStaleJournalDoesNotBlockFreshCrossAreaMinimap()
{
    DateTime nowUtc = DateTime.UtcNow;
    string journalEvidenceKey = "Journal|Treasure Goblin|JournalKill|Journal|Treasure Goblin|Template=Treasure Goblin Killed Journal.png|Kind=JournalKilled|LineBucket=11";
    string minimapEvidenceKey = "Minimap|Treasure Goblin|MinimapIcon|Minimap|Treasure Goblin|Template=Treasure Goblin Minimap.png|Kind=Minimap|LineBucket=";

    bool staleJournalSuppressed = GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress(
        source: "Journal",
        goblinType: "Treasure Goblin",
        areaKey: "Stinging Winds",
        globalEvidenceKey: journalEvidenceKey,
        countedGoblinType: "Treasure Goblin",
        countedAreaKey: "Eastern Channel Level 1",
        countedSource: "Journal",
        countedEvidenceKey: journalEvidenceKey,
        countedUtc: nowUtc - TimeSpan.FromSeconds(47),
        lastSeenUtc: nowUtc - TimeSpan.FromSeconds(7),
        nowUtc: nowUtc,
        encounterSuppressWindow: TimeSpan.FromMinutes(10),
        sourceVariantWindow: TimeSpan.FromSeconds(45),
        out string staleJournalReason);

    AssertTrue(staleJournalSuppressed, "old same-type journal text should still suppress after moving into another area");
    AssertEqual("SameEvidenceKey", staleJournalReason, "old visible journal text should explain the exact evidence match");
    AssertFalse(
        GoblinAutoCountEncounterSuppressionPolicy.ShouldRefreshEncounterLastSeenAfterSuppression(
            "Journal",
            "Stinging Winds",
            "Eastern Channel Level 1"),
        "cross-area stale journal suppression should not refresh the counted encounter and block later fresh minimap evidence");

    bool freshMinimapSuppressed = GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress(
        source: "Minimap",
        goblinType: "Treasure Goblin",
        areaKey: "Stinging Winds",
        globalEvidenceKey: minimapEvidenceKey,
        countedGoblinType: "Treasure Goblin",
        countedAreaKey: "Eastern Channel Level 1",
        countedSource: "Journal",
        countedEvidenceKey: journalEvidenceKey,
        countedUtc: nowUtc - TimeSpan.FromSeconds(75),
        lastSeenUtc: nowUtc - TimeSpan.FromSeconds(35),
        nowUtc: nowUtc,
        encounterSuppressWindow: TimeSpan.FromMinutes(10),
        sourceVariantWindow: TimeSpan.FromSeconds(45),
        out string freshMinimapReason);

    AssertFalse(freshMinimapSuppressed, "fresh minimap evidence in a new area should not be suppressed by a prior-area journal row");
    AssertTrue(string.IsNullOrWhiteSpace(freshMinimapReason), "fresh cross-area minimap evidence should not report a stale journal match reason");
}

static void TestGoblinAutoCountSameAreaDuplicateJournalRefreshesEncounterState()
{
    AssertTrue(
        GoblinAutoCountEncounterSuppressionPolicy.ShouldRefreshEncounterLastSeenAfterAreaAlreadyCounted(
            "Journal",
            "Black Canyon Mines",
            "Black Canyon Mines"),
        "same-area duplicate journal evidence should refresh the counted encounter so the visible row cannot recount in the next area");

    AssertFalse(
        GoblinAutoCountEncounterSuppressionPolicy.ShouldRefreshEncounterLastSeenAfterAreaAlreadyCounted(
            "Journal",
            "Rakkis Crossing",
            "Black Canyon Mines"),
        "cross-area duplicate journal evidence should not refresh a previous-area encounter");

    DateTime nowUtc = DateTime.UtcNow;
    bool staleJournalSuppressed = GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress(
        source: "Journal",
        goblinType: "Odious Collector",
        areaKey: "Rakkis Crossing",
        globalEvidenceKey: "Journal|Odious Collector|JournalEncounter|Journal|Odious Collector|Template=Odious Collector Engaged Journal.png|Kind=JournalEngaged|LineBucket=10",
        countedGoblinType: "Odious Collector",
        countedAreaKey: "Black Canyon Mines",
        countedSource: "Journal",
        countedEvidenceKey: "Journal|Odious Collector|JournalEncounter|Journal|Odious Collector|Template=Odious Collector Engaged Journal.png|Kind=JournalEngaged|LineBucket=11",
        countedUtc: nowUtc - TimeSpan.FromSeconds(98),
        lastSeenUtc: nowUtc - TimeSpan.FromSeconds(10),
        nowUtc: nowUtc,
        encounterSuppressWindow: TimeSpan.FromMinutes(10),
        sourceVariantWindow: TimeSpan.FromSeconds(45),
        out string matchReason);

    AssertTrue(staleJournalSuppressed, "same visible journal row should suppress after a counted area's duplicate journal evidence refreshed the encounter last-seen state");
    AssertTrue(matchReason.StartsWith("JournalLineBucket", StringComparison.Ordinal) || matchReason.StartsWith("SameEvidenceKey", StringComparison.Ordinal), "stale Rakkis journal suppression should explain the journal evidence match");
}

static void TestGoblinAutoCountSuppressesShiftedJournalRowAfterPause()
{
    DateTime nowUtc = DateTime.UtcNow;
    bool suppressed = GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress(
        source: "Journal",
        goblinType: "Odious Collector",
        areaKey: "Sewers of Caldeum",
        globalEvidenceKey: "Journal|Odious Collector|JournalKill|Journal|Odious Collector|Template=Odious Collector Killed Journal.png|Kind=JournalKilled|LineBucket=8",
        countedGoblinType: "Odious Collector",
        countedAreaKey: "Royal Crypts",
        countedSource: "Journal",
        countedEvidenceKey: "Journal|Odious Collector|JournalKill|Journal|Odious Collector|Template=Odious Collector Killed Journal.png|Kind=JournalKilled|LineBucket=11",
        countedUtc: nowUtc - TimeSpan.FromMinutes(4),
        lastSeenUtc: nowUtc - TimeSpan.FromMinutes(3),
        nowUtc: nowUtc,
        encounterSuppressWindow: TimeSpan.FromMinutes(10),
        sourceVariantWindow: TimeSpan.FromSeconds(45),
        out string matchReason);

    AssertTrue(suppressed, "a counted journal row that shifts a few feed buckets after a pause should still suppress in the next area");
    AssertEqual("JournalLineBucket:8->11", matchReason, "shifted-row suppression should explain the journal bucket match");

    bool unrelatedRowSuppressed = GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress(
        source: "Journal",
        goblinType: "Odious Collector",
        areaKey: "Sewers of Caldeum",
        globalEvidenceKey: "Journal|Odious Collector|JournalKill|Journal|Odious Collector|Template=Odious Collector Killed Journal.png|Kind=JournalKilled|LineBucket=5",
        countedGoblinType: "Odious Collector",
        countedAreaKey: "Royal Crypts",
        countedSource: "Journal",
        countedEvidenceKey: "Journal|Odious Collector|JournalKill|Journal|Odious Collector|Template=Odious Collector Killed Journal.png|Kind=JournalKilled|LineBucket=11",
        countedUtc: nowUtc - TimeSpan.FromMinutes(4),
        lastSeenUtc: nowUtc - TimeSpan.FromMinutes(3),
        nowUtc: nowUtc,
        encounterSuppressWindow: TimeSpan.FromMinutes(10),
        sourceVariantWindow: TimeSpan.FromSeconds(45),
        out string unrelatedReason);

    AssertFalse(unrelatedRowSuppressed, "a farther-away same-type journal row should not be globally suppressed as the same visible row");
    AssertTrue(string.IsNullOrWhiteSpace(unrelatedReason), "unrelated journal rows should not report a suppression reason");
}

static void TestGoblinAutoCountTreatsDifferentJournalTemplatesAsSeparateLines()
{
    DateTime nowUtc = DateTime.UtcNow;
    bool suppressed = GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress(
        source: "Journal",
        goblinType: "Gelatinous Sire",
        areaKey: "Black Canyon Mines",
        globalEvidenceKey: "Journal|Gelatinous Sire|black canyon mines|JournalKill|Journal|Gelatinous Sire|Template=Gelatinous Spawn Killed Journal.png|Kind=JournalKilled|LineBucket=8",
        countedGoblinType: "Gelatinous Sire",
        countedAreaKey: "Stinging Winds",
        countedSource: "Journal",
        countedEvidenceKey: "Journal|Gelatinous Sire|stinging winds|JournalKill|Journal|Gelatinous Sire|Template=Gelatinous Sire Killed Journal.png|Kind=JournalKilled|LineBucket=11",
        countedUtc: nowUtc - TimeSpan.FromMinutes(4),
        lastSeenUtc: nowUtc - TimeSpan.FromMinutes(4),
        nowUtc: nowUtc,
        encounterSuppressWindow: TimeSpan.FromMinutes(10),
        sourceVariantWindow: TimeSpan.FromSeconds(45),
        out string matchReason);

    AssertFalse(suppressed, "a Gelatinous Spawn journal line in a new area should not be suppressed as the same visible row as a prior Gelatinous Sire journal line");
    AssertTrue(string.IsNullOrWhiteSpace(matchReason), "different journal templates should not report a journal bucket suppression reason");
}

static void TestGoblinAcceptedManualCountUpdatesLastObservationDisplay()
{
    string repoRoot = FindRepositoryRootForTests();
    string sessionStatsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.cs"));
    string publishMethod = ExtractMethodBody(sessionStatsSource, "private void PortPublishManualGoblinCountObservation");
    string acceptedPublishMethod = ExtractMethodBody(sessionStatsSource, "private void PortPublishAcceptedGoblinCountObservation");
    string clearMethod = ExtractMethodBody(sessionStatsSource, "private void PortMarkGoblinObservationNoCurrent");

    AssertTrue(sessionStatsSource.Contains("PortPublishManualGoblinCountObservation(area, goblinType, source, guardResult)", StringComparison.Ordinal), "accepted manual counts should publish the Last Observation display immediately");
    AssertTrue(publishMethod.Contains("\"ManualCountAccepted\"", StringComparison.Ordinal), "manual count display should identify the accepted-count state");
    AssertTrue(acceptedPublishMethod.Contains("\"Counted\"", StringComparison.Ordinal), "accepted count display should show Counted as the Last Observation reason");
    AssertTrue(acceptedPublishMethod.Contains("PortManualGoblinCountDisplayHold", StringComparison.Ordinal), "accepted count display should be held immediately and then persist as the last accepted count");
    AssertTrue(acceptedPublishMethod.Contains("LastObservationUiRefreshRequested", StringComparison.Ordinal), "accepted count display should log an immediate UI refresh request");
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
    AssertTrue(sessionStatsSource.Contains("PortClearDisplayedGoblinObservationAfterConfirmedAreaChange", StringComparison.Ordinal), "confirmed area changes should still be logged for observation synchronization");
    AssertTrue(sessionStatsSource.Contains("ConfirmedAreaChanged", StringComparison.Ordinal), "confirmed area changes should log Last Observation area synchronization");
}

static void TestGoblinAcceptedCountsPersistLastObservationUntilReset()
{
    string repoRoot = FindRepositoryRootForTests();
    string sessionStatsSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.cs"));
    string autoCountSource = File.ReadAllText(Path.Combine(repoRoot, "frmMain.SessionStats.AutoCount.cs"));
    string acceptedPublishMethod = ExtractMethodBody(sessionStatsSource, "private void PortPublishAcceptedGoblinCountObservation");
    string clearMethod = ExtractMethodBody(sessionStatsSource, "private void PortMarkGoblinObservationNoCurrent");
    string preserveIncomingMethod = ExtractMethodBody(sessionStatsSource, "private bool PortShouldPreserveDisplayedObservationAgainstIncoming");

    AssertTrue(autoCountSource.Contains("PortPublishAcceptedGoblinCountObservation(area, observation.GoblinType, observation.Source, \"AutomaticCountAccepted\", guardResult)", StringComparison.Ordinal), "accepted automatic counts should publish the Last Observation as Counted");
    AssertTrue(acceptedPublishMethod.Contains("persistUntilNextAcceptedCount=True", StringComparison.Ordinal), "accepted count update logs should document the persistent display policy");
    AssertTrue(clearMethod.Contains("PortDisplayedObservationIsAcceptedCount(previousObservation)", StringComparison.Ordinal), "clear/no-candidate paths should preserve the last accepted count before area-change clearing");
    AssertTrue(clearMethod.Contains("AcceptedCountPersistent", StringComparison.Ordinal), "clear skips should identify accepted-count persistence");
    AssertTrue(preserveIncomingMethod.Contains("PortDisplayedObservationIsAcceptedCount(displayedObservation)", StringComparison.Ordinal), "suppressed/stale incoming observations should not replace an accepted-count display");
    AssertTrue(sessionStatsSource.Contains("PortResetGoblinEvidenceObservationState(\"TrackerStatsReset\")", StringComparison.Ordinal), "Reset Stats should clear the accepted Last Observation");
    AssertTrue(sessionStatsSource.Contains("PortResetGoblinEvidenceObservationState(\"NewGameCreated\")", StringComparison.Ordinal), "New Game should clear the accepted Last Observation like Reset Stats");
    AssertFalse(File.ReadAllText(Path.Combine(repoRoot, "frmMain.GoblinEvidence.cs")).Contains("preservedAcceptedDisplay", StringComparison.Ordinal), "New Game should not preserve accepted Last Observation display state");
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
    string evidenceModelSource = File.ReadAllText(Path.Combine(repoRoot, "GoblinEvidence.cs"));
    string signatureMethod = ExtractMethodBody(evidenceSource, "private string PortJournalEvidenceLineSignature");
    AssertTrue(evidenceSource.Contains("PortJournalEvidenceLineSignature", StringComparison.Ordinal), "journal freshness should use a line signature, not current area as the freshness key");
    AssertTrue(evidenceSource.Contains("JournalEngagedIgnoredAreaChanged", StringComparison.Ordinal), "Engaged journal lines first seen in another area should log an area-change suppression");
    AssertTrue(evidenceSource.Contains("JournalCandidateIgnoredHistoryInput", StringComparison.Ordinal), "journal evidence should be suppressed briefly after the player opens journal/chat history with Enter");
    AssertTrue(evidenceSource.Contains("JournalCandidateIgnoredHistoryRow", StringComparison.Ordinal), "journal evidence from upper/history rows should be ignored before it can become fresh evidence");
    AssertTrue(evidenceSource.Contains("GoblinJournalActiveFeedMinimumY", StringComparison.Ordinal), "journal history suppression should use an explicit active-feed row boundary");
    AssertTrue(evidenceSource.Contains("GoblinEvidenceJournalNameValidationFailed", StringComparison.Ordinal), "journal template matches should validate the goblin-name portion before becoming candidates");
    AssertTrue(evidenceSource.Contains("JournalNameValidationBelowThreshold", StringComparison.Ordinal), "journal name validation failures should be diagnosable in logs");
    AssertTrue(evidenceSource.Contains("GoblinJournalFreshnessPolicy.EngagedIsFresh", StringComparison.Ordinal), "Engaged journal evidence should use area-strict freshness before being accepted");
    AssertFalse(signatureMethod.Contains("PortDisplayLocation", StringComparison.Ordinal), "journal line freshness signatures must not include current area, or old visible lines can become fresh after moving");
    AssertTrue(signatureMethod.Contains("GoblinJournalEvidencePolicy.LineSignature", StringComparison.Ordinal), "journal freshness should use the shared line-signature policy");
    AssertTrue(evidenceModelSource.Contains("LineBucket(match.MatchPoint)", StringComparison.Ordinal), "journal line freshness signatures should include a coarse row bucket so later legitimate same-template lines can be fresh");
    AssertTrue(evidenceModelSource.Contains("Math.Max(0, matchPoint.Y) / 32", StringComparison.Ordinal), "journal line freshness should bucket the match row instead of using an exact volatile point");
    AssertTrue(evidenceModelSource.Contains("SameVisibleLineFamily", StringComparison.Ordinal), "reset carryover suppression should match the same visible row even if it moves up a few buckets");
    AssertTrue(evidenceSource.Contains("JournalCandidateIgnoredResetCarryover", StringComparison.Ordinal), "New Game/Reset Stats should suppress already-visible journal rows before they can become fresh in the new area");
    AssertTrue(evidenceSource.Contains("JournalCandidateIgnoredNewGameCarryoverWindow", StringComparison.Ordinal), "New Game should briefly suppress newly detected journal-only rows that may be previous-game carryover");
    AssertTrue(evidenceSource.Contains("JournalCandidateIgnoredStaleVisibleLine", StringComparison.Ordinal), "stale journal suppression should cover same-goblin nearby row variants, not only exact signatures");
    AssertTrue(evidenceSource.Contains("PortNewGameJournalCarryoverSuppressionActive", StringComparison.Ordinal), "New Game carryover suppression should be explicit and diagnosable");
    AssertTrue(evidenceSource.Contains("PortTryTouchStaleSuppressedJournalEvidenceByVisibleGoblinLine", StringComparison.Ordinal), "same-goblin stale visible row suppression should have a narrow helper");
    AssertTrue(evidenceSource.Contains("PortRememberJournalResetCarryoverSuppressions(reason", StringComparison.Ordinal), "reset should remember visible journal rows before clearing first-seen state");
    AssertTrue(evidenceSource.Contains("resetCarryoverSuppressionsRemembered", StringComparison.Ordinal), "reset diagnostics should report how many journal rows were protected from carryover recounts");
    AssertFalse(signatureMethod.Contains("ScreenMatchPoint", StringComparison.Ordinal), "journal line freshness signatures should not use absolute screen coordinates");
    AssertFalse(evidenceSource.Contains("nowUtc - state.LastSeenUtc > GoblinJournalEvidenceFreshWindow", StringComparison.Ordinal), "Killed journal first-seen state should not reset just because the same visible line matched again later");

    AssertTrue(
        GoblinJournalEvidencePolicy.SameVisibleLineFamily(
            "JournalKilled|Blood Thief|Blood Thief Killed Journal.png|LineBucket=8",
            "JournalKilled|Blood Thief|Blood Thief Killed Journal.png|LineBucket=11",
            out int currentBucket,
            out int previousBucket),
        "reset carryover suppression should treat a nearby moved-up journal row as the same visible row");
    AssertEqual(8, currentBucket, "current bucket should be parsed for diagnostics");
    AssertEqual(11, previousBucket, "previous bucket should be parsed for diagnostics");
    AssertFalse(
        GoblinJournalEvidencePolicy.SameVisibleLineFamily(
            "JournalKilled|Blood Thief|Blood Thief Killed Journal.png|LineBucket=4",
            "JournalKilled|Blood Thief|Blood Thief Killed Journal.png|LineBucket=11",
            out _,
            out _),
        "a far-away line bucket should be allowed to become a separate future journal row");
    AssertFalse(
        GoblinJournalEvidencePolicy.SameVisibleLineFamily(
            "JournalKilled|Treasure Goblin|Treasure Goblin Killed Journal.png|LineBucket=9",
            "JournalKilled|Blood Thief|Blood Thief Killed Journal.png|LineBucket=11",
            out _,
            out _),
        "different goblin/template families should not be suppressed as reset carryover");
    AssertTrue(
        GoblinJournalEvidencePolicy.SameVisibleGoblinLine(
            "JournalEngaged|Blood Thief|Blood Thief Engaged.png|LineBucket=10",
            "JournalKilled|Blood Thief|Blood Thief Killed Journal.png|LineBucket=11",
            out int engagedBucket,
            out int killedBucket),
        "stale visible-line suppression should associate nearby Engaged/Killed rows from the same goblin when one row is already stale");
    AssertEqual(10, engagedBucket, "same-goblin current bucket should be parsed for diagnostics");
    AssertEqual(11, killedBucket, "same-goblin previous bucket should be parsed for diagnostics");
    AssertFalse(
        GoblinJournalEvidencePolicy.SameVisibleGoblinLine(
            "JournalEngaged|Blood Thief|Blood Thief Engaged.png|LineBucket=3",
            "JournalKilled|Blood Thief|Blood Thief Killed Journal.png|LineBucket=11",
            out _,
            out _),
        "same-goblin stale visible-line suppression should not apply to distant future rows");
    AssertFalse(
        GoblinJournalEvidencePolicy.SameVisibleGoblinLine(
            "JournalEngaged|Treasure Goblin|Treasure Goblin Engaged Journal.png|LineBucket=10",
            "JournalKilled|Blood Thief|Blood Thief Killed Journal.png|LineBucket=11",
            out _,
            out _),
        "same-goblin stale visible-line suppression should not apply to different goblin types");
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
    AssertTrue(townSource.Contains("List<DrawingPoint> cachedSlots = PortFilledInventorySlots()", StringComparison.Ordinal), "salvage should scan inventory once and cache filled slots");
    AssertFalse(townSource.Contains("slot = PortFirstFilledInventorySlot()", StringComparison.Ordinal), "salvage should not rescan inventory after each slot");
    AssertTrue(townSource.Contains("cacheMode=SingleInventoryScan", StringComparison.Ordinal), "salvage timing should identify the single-scan cache mode");
    AssertTrue(townSource.Contains("cachedSlotCount=", StringComparison.Ordinal), "salvage should log the cached slot count");
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
    AssertTrue(buttonSource.Contains("ButtonClickWorkflowCancellationRequested", StringComparison.Ordinal), "route button clicks should cancel active workflows before starting the selected teleport");
    AssertTrue(buttonSource.Contains("ButtonClickCancellationWaitComplete", StringComparison.Ordinal), "route button clicks should wait for cancellation cleanup before starting teleport");
    AssertTrue(buttonSource.Contains("ButtonClickClearingStaleWorkflowState", StringComparison.Ordinal), "route button clicks should clear cancelled stale workflow state instead of staying blocked");
    AssertTrue(buttonSource.Contains("DebugManager.Session.RecordWorkflowCancellation", StringComparison.Ordinal), "manual teleport cancellation should be reflected in diagnostic counters");
    AssertFalse(buttonSource.Contains("Button click ignored because teleport is already waiting for confirmation", StringComparison.Ordinal), "route button clicks should not be ignored while stale arrival confirmation is active");
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

static void TestGoblinJournalPandemoniumAreaUsesRecentChannelMinimapContext()
{
    DateTime nowUtc = DateTime.UtcNow;
    GoblinAreaResolution pf1 = GoblinAreaResolver.Resolve("Pandemonium Fortress Level 1");
    GoblinObservationRecord recentEasternChannel = new(
        nowUtc.AddSeconds(-8),
        "Minimap",
        "Blood Thief",
        "Eastern Channel Level 1",
        "Eastern Channel Level 1",
        true,
        "Eligible",
        "Available",
        1,
        0,
        0.890);

    GoblinJournalAreaOverrideDecision decision = GoblinJournalAreaOverridePolicy.TryUseRecentMinimapChannelArea(
        pf1,
        "Blood Thief",
        recentEasternChannel,
        "Eastern Channel Level 1",
        nowUtc,
        TimeSpan.FromSeconds(45));

    AssertTrue(decision.Overridden, "recent same-goblin minimap evidence should correct journal PF1 back to Eastern Channel Level 1");
    AssertEqual("Eastern Channel Level 1", decision.Area.AreaKey, "journal follow-up should inherit the safer channel area");
    AssertEqual("RecentMinimapChannelContext", decision.Reason, "override reason should identify the minimap/channel context");

    GoblinJournalAreaOverrideDecision expired = GoblinJournalAreaOverridePolicy.TryUseRecentMinimapChannelArea(
        pf1,
        "Blood Thief",
        recentEasternChannel with { TimestampUtc = nowUtc.AddSeconds(-50) },
        "Eastern Channel Level 1",
        nowUtc,
        TimeSpan.FromSeconds(45));

    AssertFalse(expired.Overridden, "expired minimap context should not rewrite journal area");
    AssertEqual("RecentMinimapExpired", expired.Reason, "expired context should explain why no override happened");

    GoblinJournalAreaOverrideDecision mismatchedLevel = GoblinJournalAreaOverridePolicy.TryUseRecentMinimapChannelArea(
        pf1,
        "Blood Thief",
        recentEasternChannel with { AreaKey = "Eastern Channel Level 2", DisplayLocation = "Eastern Channel Level 2" },
        "Eastern Channel Level 2",
        nowUtc,
        TimeSpan.FromSeconds(45));

    AssertFalse(mismatchedLevel.Overridden, "Level 2 minimap context should not rewrite a PF1 journal result");

    GoblinJournalAreaOverrideDecision truePandemoniumContext = GoblinJournalAreaOverridePolicy.TryUseRecentMinimapChannelArea(
        pf1,
        "Blood Thief",
        recentEasternChannel,
        "Rakkis Crossing",
        nowUtc,
        TimeSpan.FromSeconds(45));

    AssertFalse(truePandemoniumContext.Overridden, "Rakkis/PF route context should preserve a true PF journal result");
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

static void AssertSequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message)
    where T : notnull
{
    T[] expectedItems = expected.ToArray();
    T[] actualItems = actual.ToArray();
    if (expectedItems.Length != actualItems.Length)
    {
        throw new InvalidOperationException($"{message}: expectedLength={expectedItems.Length}; actualLength={actualItems.Length}");
    }

    for (int index = 0; index < expectedItems.Length; index++)
    {
        if (!EqualityComparer<T>.Default.Equals(expectedItems[index], actualItems[index]))
        {
            throw new InvalidOperationException($"{message}: index={index}; expected={expectedItems[index]}; actual={actualItems[index]}");
        }
    }
}

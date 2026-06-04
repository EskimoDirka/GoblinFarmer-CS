using System.Drawing;
using System.Diagnostics;
using System.IO.Compression;
using GoblinFarmer;

int failures = 0;

Run("Python no-click rectangles use blacklist safety and Python boundaries", TestPythonNoClickRectangles);
Run("Unsafe cursor suppresses left clicks without stopping Demon Hunter key loop", TestUnsafeCursorDoesNotStopKeyLoop);
Run("Demon Hunter right mouse remains held after initial safe start", TestRightMouseRemainsHeldAfterSafeStart);
Run("Initial safe-wait timeout stops only when Python would stop", TestInitialSafeWaitTimeoutPolicy);
Run("Default AppSettings path/debug profile are launch-surface neutral", TestAppSettingsLaunchParityDefaults);
Run("VS Debug/dev profile suppresses first-run setup and forces internal debug defaults", TestVsDebugDevProfileDefaults);
Run("VS Debug/dev profile prefers project-root AppSettings", TestVsDebugProjectRootConfigPreferred);
Run("DebugManager profile helpers separate VS, release debug, and normal release", TestDebugManagerProfileHelpers);
Run("DebugManager retention cleanup only deletes matching artifacts", TestDebugManagerRetentionCleanupFilters);
Run("GoblinEvidence retention keeps newest 250 files", TestGoblinEvidenceRetentionKeepsNewest250Files);
Run("GoblinEvidence retention breaks timestamp ties by filename", TestGoblinEvidenceRetentionBreaksTimestampTiesByFilename);
Run("GoblinEvidence retention ignores missing folder", TestGoblinEvidenceRetentionMissingFolderDoesNotThrow);
Run("GoblinEvidence retention count less than one disables cleanup", TestGoblinEvidenceRetentionCountLessThanOneDisablesCleanup);
Run("GoblinEvidence retention deletes only inside GoblinEvidence", TestGoblinEvidenceRetentionDeletesOnlyInsideFolder);
Run("Installed/release profile with missing paths still requires first-run setup", TestReleaseProfileRequiresSetupWhenMissingPaths);
Run("Explicit AppSettings path override wins", TestExplicitAppSettingsPathOverrideWins);
Run("AppSettings migration preserves existing runtime paths", TestAppSettingsMigrationPreservesRuntimePaths);
Run("Demon Hunter no-click suppression diagnostic is not named as failure or stall", TestDemonHunterNoClickSuppressionDiagnosticName);
Run("Start Game click policy blocks Leave Game and in-game signals", TestStartGameClickPolicyBlocksInGameSignals);
Run("Goblin journal parser counts escaped goblin encounters", TestGoblinJournalParserCountsEscapedEncounters);
Run("Goblin type normalization maps Gelatinous Spawn to Gelatinous Sire", TestGelatinousSpawnNormalizesToSire);
Run("Debug package excludes success screenshots by default", TestDebugPackageExcludesSuccessScreenshotsByDefault);
Run("Goblin area resolver keeps true areas separate", TestGoblinAreaResolverKeepsTrueAreasSeparate);
Run("Goblin area duplicate guard suppresses same area and resets", TestGoblinAreaDuplicateGuardSuppressesSameAreaAndResets);
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
    Directory.CreateDirectory(projectConfigDirectory);
    Directory.CreateDirectory(binConfigDirectory);

    try
    {
        string projectConfigPath = Path.Combine(projectConfigDirectory, "AppSettings.json");
        string binConfigPath = Path.Combine(binConfigDirectory, "AppSettings.json");
        File.WriteAllText(Path.Combine(root, "GoblinFarmer.csproj"), "<Project />");
        File.WriteAllText(projectConfigPath, """
            {
              "Runtime": {
                "DiabloExecutablePath": "D:\\Games\\Diablo III\\Diablo III64.exe",
                "BattleNetExecutablePath": "D:\\Games\\Battle.net\\Battle.net.exe",
                "ImagesRoot": "Images"
              }
            }
            """);
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
    DebugManager.ApplyVisualStudioDebugDefaults(debug);

    AssertTrue(debug.DebugMode, "VS defaults should force debug mode in memory");
    AssertTrue(debug.ShowDiagnosticOverlay, "VS defaults should show diagnostics in memory");
    AssertTrue(debug.ShowRouteInspector, "VS defaults should show route inspector in memory");
    AssertTrue(debug.EnableDebugScreenshots, "VS defaults should enable screenshots in memory");
    AssertFalse(debug.EnableSuccessScreenshots, "success screenshots should remain disabled by default even in VS defaults");
    AssertTrue(DebugManager.ShouldSuppressFirstRunSetup(AppSettings.DebugDefaultsProfile.VsDebug), "VS defaults should suppress first-run setup");
    AssertFalse(DebugManager.ShouldShowDynamicDebugControls(AppSettings.DebugDefaultsProfile.VsDebug), "VS defaults should hide dynamic debug controls");

    debug = new AppSettings.DebugSettings();
    DebugManager.ApplyReleaseUserDefaultsIfPreferenceUnsaved(debug);
    AssertFalse(debug.DebugMode, "normal release should stay quiet by default");
    AssertFalse(debug.EnableDebugScreenshots, "normal release should not capture screenshots by default");
    AssertFalse(debug.EnableSuccessScreenshots, "normal release should not capture success screenshots by default");

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

static string TouchGoblinEvidenceFile(string path, DateTime lastWriteTimeUtc)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, Path.GetFileName(path));
    File.SetCreationTimeUtc(path, lastWriteTimeUtc);
    File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);
    return path;
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

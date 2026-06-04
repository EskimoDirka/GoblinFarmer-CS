using System.Drawing;
using GoblinFarmer;

int failures = 0;

Run("Python no-click rectangles use blacklist safety and Python boundaries", TestPythonNoClickRectangles);
Run("Unsafe cursor suppresses left clicks without stopping Demon Hunter key loop", TestUnsafeCursorDoesNotStopKeyLoop);
Run("Demon Hunter right mouse remains held after initial safe start", TestRightMouseRemainsHeldAfterSafeStart);
Run("Initial safe-wait timeout stops only when Python would stop", TestInitialSafeWaitTimeoutPolicy);
Run("Default AppSettings path/debug profile are launch-surface neutral", TestAppSettingsLaunchParityDefaults);
Run("VS Debug/dev profile suppresses first-run setup and forces internal debug defaults", TestVsDebugDevProfileDefaults);
Run("DebugManager profile helpers separate VS, release debug, and normal release", TestDebugManagerProfileHelpers);
Run("DebugManager retention cleanup only deletes matching artifacts", TestDebugManagerRetentionCleanupFilters);
Run("Installed/release profile with missing paths still requires first-run setup", TestReleaseProfileRequiresSetupWhenMissingPaths);
Run("Explicit AppSettings path override wins", TestExplicitAppSettingsPathOverrideWins);
Run("Demon Hunter no-click suppression diagnostic is not named as failure or stall", TestDemonHunterNoClickSuppressionDiagnosticName);

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

static void TestDebugManagerProfileHelpers()
{
    AppSettings.DebugSettings debug = new();
    DebugManager.ApplyVisualStudioDebugDefaults(debug);

    AssertTrue(debug.DebugMode, "VS defaults should force debug mode in memory");
    AssertTrue(debug.ShowDiagnosticOverlay, "VS defaults should show diagnostics in memory");
    AssertTrue(debug.ShowRouteInspector, "VS defaults should show route inspector in memory");
    AssertTrue(debug.EnableDebugScreenshots, "VS defaults should enable screenshots in memory");
    AssertTrue(DebugManager.ShouldSuppressFirstRunSetup(AppSettings.DebugDefaultsProfile.VsDebug), "VS defaults should suppress first-run setup");
    AssertFalse(DebugManager.ShouldShowDynamicDebugControls(AppSettings.DebugDefaultsProfile.VsDebug), "VS defaults should hide dynamic debug controls");

    debug = new AppSettings.DebugSettings();
    DebugManager.ApplyReleaseUserDefaultsIfPreferenceUnsaved(debug);
    AssertFalse(debug.DebugMode, "normal release should stay quiet by default");
    AssertFalse(debug.EnableDebugScreenshots, "normal release should not capture screenshots by default");

    DebugManager.ApplyReleaseDebugModePreference(true, debug);
    AssertTrue(debug.DebugModePreferenceSaved, "release debug mode should mark user preference saved");
    AssertTrue(debug.DebugMode, "release debug mode should enable debug mode");
    AssertTrue(debug.EnableDebugScreenshots, "release debug mode should seed screenshots on");
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

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private const int PortReferenceWidth = 2560;
        private const int PortReferenceHeight = 1440;
        private const int PortVkShift = 0x10;
        private const int PortVkCtrl = 0x11;
        private const int PortVkAlt = 0x12;
        private const int PortVkM = 0x4D;
        private const int PortVkQ = 0x51;
        private const int PortVkReturn = 0x0D;
        private const int PortVkEscape = 0x1B;
        private const int PortVkUp = 0x26;
        private const int PortVk1 = 0x31;
        private const int PortVk2 = 0x32;
        private const int PortVkBacktick = 0xC0;
        private const int PortKeyUp = 0x0002;
        private const int PortWhKeyboardLl = 13;
        private const int PortWmKeyDown = 0x0100;
        private const int PortWmKeyUp = 0x0101;
        private const int PortWmSysKeyDown = 0x0104;
        private const int PortWmSysKeyUp = 0x0105;
        private const int PortLaunchGracePeriodMs = 45000;
        private const int PortDiabloMissingExitThreshold = 4;
        private const int PortArrivalConfirmationTimeoutMs = 18000;
        private const double PortStartGameButtonConfidence = 0.85;
        private const double PortCharacterLoadConfidence = 0.82;
        private const double PortGameMenuConfidence = 0.80;
        private const double PortVendorUiConfidence = 0.80;
        private const double PortBlankInventoryTileConfidence = 0.78;
        private const double PortCurrentLocationConfidence = 0.82;
        private const double PortBlockedLocationConfidence = 0.68;
        private const double PortMapActHeaderConfidence = 0.92;
        private const double PortWorldMapConfidence = 0.80;
        private IntPtr portOriginalCursorHandle = IntPtr.Zero;

        private readonly Dictionary<string, PortMapPoint> portActCoords = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PortMapPoint> portLocationCoords = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> portCurrentLocationTemplates = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Button> portTeleportButtons = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Button, Color> portButtonDefaultBackColors = new();
        private readonly Dictionary<Button, Color> portButtonDefaultForeColors = new();

        private readonly PortCombatNoClickRegion[] portCombatNoClickRegions =
        [
            new("TopLeftPortraitNoClickRegion", 0 / 2560.0, 0 / 1440.0, 166 / 2560.0, 226 / 1440.0),
            new("FollowerPortraitNoClickRegion", 141 / 2560.0, 4 / 1440.0, 135 / 2560.0, 159 / 1440.0),
            new("ChatButtonNoClickRegion", 6 / 2560.0, 1270 / 1440.0, 118 / 2560.0, 134 / 1440.0),
            new("HealthGlobeNoClickRegion", 430 / 2560.0, 1220 / 1440.0, 390 / 2560.0, 220 / 1440.0),
            new("SkillBarNoClickRegion", 818 / 2560.0, 1300 / 1440.0, 930 / 2560.0, 130 / 1440.0),
            new("ResourceGlobeNoClickRegion", 1690 / 2560.0, 1220 / 1440.0, 410 / 2560.0, 220 / 1440.0),
            new("BottomRightButtonsNoClickRegion", 2310 / 2560.0, 1278 / 1440.0, 214 / 2560.0, 124 / 1440.0),
            new("RightQuestIconNoClickRegion", 2448 / 2560.0, 472 / 1440.0, 44 / 2560.0, 62 / 1440.0),
            new("TopRightStatusNoClickRegion", 2237 / 2560.0, 28 / 1440.0, 115 / 2560.0, 50 / 1440.0),
            new("ExtendedRightMenuNoClickRegion", 2410 / 2560.0, 1120 / 1440.0, 150 / 2560.0, 240 / 1440.0),
        ];

        private volatile bool portCombatRunning;
        private volatile bool portCombatStopping;
        private volatile bool portHotkeysRunning;
        private Thread? portHotkeyThread;
        private IntPtr portKeyboardHookHandle = IntPtr.Zero;
        private LowLevelKeyboardProc? portKeyboardProc;
        private CancellationTokenSource? portAutomationCts;
        private CancellationTokenSource? portCombatCts;
        private bool portInitialized;
        private long portIgnoreEscapeHotkeyUntilTicks;
        private string portCombatClass = "";
        private string portLastTeleportKey = "";
        private string portQueuedTeleportKey = "";
        private string portLastConfirmedLocation = "";
        private string portQueuedRetryTeleportKey = "";
        private string portLastRequestedTeleportKey = "";
        private string portTeleportWaitingConfirmationKey = "";
        private volatile bool portTeleportRetryFailedOrInterrupted;
        private volatile bool portTeleportAlreadyHereNotified;
        private volatile bool portTeleportWaitingForConfirmation;
        private volatile bool portAutomationBlockedByTeleportFailsafe;
        private volatile bool portSuppressSkill1KeyUp;
        private volatile bool portSkill1TeleportHandled;
        private volatile bool portSkill2CombatHandled;
        private volatile bool portLootSpamLeftClickDown;
        private volatile bool portRuntimeLeftMouseHeld;
        private volatile bool portRuntimeRightMouseHeld;
        private volatile bool portRuntimeShiftHeld;
        private volatile bool portDemonHunterRightHeldFromSafeRegion;
        private volatile bool portWitchDoctorHeldInputFromSafeRegion;
        private volatile bool portDiabloWasRunning;
        private volatile bool portBlockSkill1TeleportHotkey = true;
        private bool portWitchDoctorLastHexReady;
        private long portLastWitchDoctorHexLogTicks;
        private long portLastLocationTemplateReloadTicks;
        private long portLastTeleportNextHotkeyTicks;
        private long portIgnoreTeleportNextUntilTicks;
        private long portLaunchGraceUntilTicks;
        private long portLastLaunchGraceMissingLogTicks;
        private long portLastLaunchFlowMissingLogTicks;
        private int portConsecutiveDiabloMissingChecks;
        private bool portLaunchGraceStableLogged;
        private volatile bool portBattleNetLaunchFlowActive;
        private long portLastCombatCursorDecisionLogTicks;
        private long portLastDemonHunterDecisionLogTicks;
        private long portLastDemonHunterRightHeldNoClickLogTicks;
        private long portLastWitchDoctorHeldInputNoClickLogTicks;
        private long portLastWitchDoctorScrollSuppressedNoClickLogTicks;
        private long portLastLootSpamDecisionLogTicks;
        private bool? portLastCombatCursorDecisionAllowed;
        private bool? portLastDemonHunterDecisionAllowed;
        private bool? portLastLootSpamDecisionAllowed;
        private Form? portSplashForm;
        private Label? portSplashLabel;
        private System.Windows.Forms.Timer? portSplashTimer;
        private PortScanRegionManager? portScanRegionManager;

        private Dictionary<string, DrawingPoint> portRepairCoords = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, DrawingPoint> portSalvageCoords = new(StringComparer.OrdinalIgnoreCase);
        private DrawingPoint portLeaveGamePoint = new(326, 639);
        private readonly DrawingPoint portMapRightClickPoint = new(1272, 594);

        private sealed record PortMapPoint(string Name, string Act, int X, int Y);
        private sealed record PortLocationDetectionResult(string Detected, string BestName, double BestConfidence, string SecondName, double SecondConfidence, int TemplateCount, long ElapsedMilliseconds);
        private sealed record PortCombatNoClickRegion(string Name, double Left, double Top, double Width, double Height);

        private sealed class PortNoActivateSplashForm : Form
        {
            private const int WS_EX_NOACTIVATE = 0x08000000;
            private const int WS_EX_TOOLWINDOW = 0x00000080;

            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams createParams = base.CreateParams;
                    createParams.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                    return createParams;
                }
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out DrawingPoint lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ClipCursor(ref RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClipCursor(IntPtr lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public DrawingPoint ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (portInitialized)
            {
                return;
            }

            portInitialized = true;
            _ = PortScanRegions;
            PortLoadCoordinates();
            PortLoadImageCaches();
            PortWireButtons();

            if (!radMonk.Checked && !radDH.Checked && !radWD.Checked)
            {
                radMonk.Checked = true;
            }

            chkCombat.Checked = true;
            chkKadala.Checked = true;
            chkLoot.Checked = true;
            chkKeepDebugScreenshots.Checked = true;
            chkBlockSkill1TeleportHotkey.Checked = true;
            portBlockSkill1TeleportHotkey = chkBlockSkill1TeleportHotkey.Checked;
            chkBlockSkill1TeleportHotkey.CheckedChanged += (_, _) => portBlockSkill1TeleportHotkey = chkBlockSkill1TeleportHotkey.Checked;

            portHotkeysRunning = true;
            portHotkeyThread = new Thread(PortHotkeyLoop) { IsBackground = true };
            portHotkeyThread.Start();
            PortInstallKeyboardHook();
            portDiabloWasRunning = IsDiabloRunning();
            PortCleanupOldDebugScreenshots(AppSettings.RetentionDays);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            portHotkeysRunning = false;
            PortUninstallKeyboardHook();
            PortHideSplash();
            portSplashTimer?.Dispose();
            portSplashForm?.Dispose();
            PortStopAllAutomation("app closing");
            PortStopCombat("app closing");
            ForceReleaseAllRuntimeInputs("app closing");
            ClipCursor(IntPtr.Zero);
            PortLogSessionSummary();
            base.OnFormClosing(e);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            ForceReleaseAllRuntimeInputs("app lost focus");
            base.OnDeactivate(e);
        }

        private void PortWireButtons()
        {
            btnMakeNewGame.Click += (_, _) => _ = PortRunAutomationAsync(PortMakeNewGameFlow);
            btnExitGame.Click += (_, _) => _ = PortRunAutomationAsync(PortExitGameFlow);

            PortWireTeleportButton(btnNewTristram, "New Tristram");
            PortWireTeleportButton(btnSouthernHighlands, "Southern Highlands");
            PortWireTeleportButton(btnNorthernHighlands, "Northern Highlands");
            PortWireTeleportButton(btnTheWeepingHollow, "The Weeping Hollow");
            PortWireTeleportButton(btnTheFesteringWoods, "The Festering Woods");
            PortWireTeleportButton(btnCathedral, "Cathedral");
            PortWireTeleportButton(btnRoyalCrypts, "Royal Crypts");
            PortWireTeleportButton(btnHiddenCamp, "Hidden Camp");
            PortWireTeleportButton(btnCityOfCaldeum, "City Of Caldeum");
            PortWireTeleportButton(btnAncientWaterway, "Ancient Waterway");
            PortWireTeleportButton(btnStingingWinds, "Stinging Winds");
            PortWireTeleportButton(btnBattlefields, "Battlefields");
            PortWireTeleportButton(btnRakkisCrossing, "Rakkis Crossing");
            PortWireTeleportButton(btnPandemoniumFortressLevel1, "Pandemonium Fortress Level 1");
            PortWireTeleportButton(btnPandemoniumFortressLevel2, "Pandemonium Fortress Level 2");
        }

        private bool PortRunTeleportButton(string location, CancellationToken token, bool ignoreBlocking, string source)
        {
            string previousConfirmedLocation = portLastConfirmedLocation;
            string previousLastTeleportKey = portLastTeleportKey;
            string previousQueuedTeleportKey = portQueuedTeleportKey;
            string previousRetryTeleportKey = portQueuedRetryTeleportKey;
            bool wasRetry = source.Equals("ButtonRetry", StringComparison.OrdinalIgnoreCase);
            portTeleportAlreadyHereNotified = false;
            portLastRequestedTeleportKey = PortLocationKey(location);
            AppLogger.Info($"Teleport run start: requested={PortDisplayLocation(location)}; source={source}; ignoreBlocking={ignoreBlocking}; confirmedBefore={PortDisplayLocation(previousConfirmedLocation)}; displayBefore={PortDisplayLocation(PortGetButtonLocationForDetectedLocation(previousConfirmedLocation))}; queuedBefore={PortDisplayLocation(PortTeleportLocationForKey(previousQueuedTeleportKey))}; retryQueuedBefore={PortDisplayLocation(PortTeleportLocationForKey(previousRetryTeleportKey))}; failedOrInterrupted={portTeleportRetryFailedOrInterrupted}");
            bool arrived = PortTeleportToLocation(location, token, verifyArrival: true, bypassFailsafe: ignoreBlocking, source: source);
            if (arrived)
            {
                PortRecordTeleport(location, portLastConfirmedLocation);
                if (wasRetry)
                {
                    AppLogger.Info($"Button retry succeeded: requested={PortDisplayLocation(location)}; confirmed={PortDisplayLocation(portLastConfirmedLocation)}");
                }
                PortClearPreservedTeleportRetry($"teleport confirmed: {location}");
            }
            else
            {
                if (token.IsCancellationRequested)
                {
                    PortCaptureFailureScreenshot("TeleportInterrupted");
                }

                if (source.Equals("Button", StringComparison.OrdinalIgnoreCase) ||
                    source.Equals("ButtonRetry", StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Info($"Button teleport failed/interrupted: requested={PortDisplayLocation(location)}; source={source}; cancelled={token.IsCancellationRequested}; blocked={portAutomationBlockedByTeleportFailsafe}; alreadyHere={portTeleportAlreadyHereNotified}; confirmedBefore={PortDisplayLocation(previousConfirmedLocation)}; confirmedAfter={PortDisplayLocation(portLastConfirmedLocation)}");
                }
                else
                {
                    AppLogger.Info($"Teleport failed/interrupted: requested={PortDisplayLocation(location)}; source={source}; cancelled={token.IsCancellationRequested}; blocked={portAutomationBlockedByTeleportFailsafe}; alreadyHere={portTeleportAlreadyHereNotified}; confirmedBefore={PortDisplayLocation(previousConfirmedLocation)}; confirmedAfter={PortDisplayLocation(portLastConfirmedLocation)}");
                }

                if (!string.Equals(portLastConfirmedLocation, previousConfirmedLocation, StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Info($"Teleport failed; restoring ConfirmedLocation from {PortDisplayLocation(portLastConfirmedLocation)} to {PortDisplayLocation(previousConfirmedLocation)}");
                    portLastConfirmedLocation = previousConfirmedLocation;
                }

                if (!portAutomationBlockedByTeleportFailsafe && !portTeleportAlreadyHereNotified)
                {
                    portLastTeleportKey = previousLastTeleportKey;
                    portQueuedTeleportKey = previousQueuedTeleportKey;
                    PortPreserveTeleportRetry(location, previousLastTeleportKey, previousQueuedTeleportKey, $"teleport did not confirm: {location}");
                }
                else
                {
                    portLastTeleportKey = previousLastTeleportKey;
                    portQueuedTeleportKey = previousQueuedTeleportKey;
                    AppLogger.Info($"Teleport did not start/confirm; route state preserved without new retry: confirmed={PortDisplayLocation(portLastConfirmedLocation)}; current={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}; queued={PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey))}; blocked={portAutomationBlockedByTeleportFailsafe}; alreadyHere={portTeleportAlreadyHereNotified}");
                }

                if (wasRetry)
                {
                    AppLogger.Info($"Button retry failed: requested={PortDisplayLocation(location)}; confirmed={PortDisplayLocation(portLastConfirmedLocation)}; current={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}; queued={PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey))}; retryQueued={PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey))}");
                }
            }

            if (wasRetry)
            {
                AppLogger.Info($"Route state after retry: requested={PortDisplayLocation(location)}; success={arrived}; confirmed={PortDisplayLocation(portLastConfirmedLocation)}; current={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}; queued={PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey))}; retryQueued={PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey))}; failedOrInterrupted={portTeleportRetryFailedOrInterrupted}");
            }

            return arrived;
        }

        private async Task PortRunAutomationAsync(Func<CancellationToken, bool> work)
        {
            if (isAutomationRunning || portCombatRunning)
            {
                return;
            }

            if (!IsDiabloRunning() && work.Method.Name != nameof(PortMakeNewGameFlow))
            {
                PortSetAppStatus("Diablo Not Running / Idle");
                AddWorkflowStep("Automation ignored: Diablo not running");
                ForceReleaseAllRuntimeInputs("automation ignored: Diablo not running");
                return;
            }

            isAutomationRunning = true;
            portAutomationBlockedByTeleportFailsafe = false;
            portAutomationCts = new CancellationTokenSource();
            var localCts = portAutomationCts;
            var token = localCts?.Token ?? CancellationToken.None;

            PortSetEscapeStatus("Esc stops script activity");
            AddWorkflowStep("Automation started");

            try
            {
                bool isMakeNewGameFlow = work.Method.Name == nameof(PortMakeNewGameFlow);
                int failuresBefore = PortFailureCount();
                bool ok = await Task.Run(() => work(token));
                if (token.IsCancellationRequested)
                {
                    PortCaptureFailureScreenshot("WorkflowCancelled");
                    PortSetAppStatus("Cancelled");
                    AddWorkflowStep("Flow cancelled");
                }
                else if (portAutomationBlockedByTeleportFailsafe)
                {
                    AddWorkflowStep("Flow blocked");
                }
                else
                {
                    PortSetAppStatus(ok ? "Idle" : "Flow Failed");
                    AddWorkflowStep(ok ? "Flow completed" : "Flow failed");
                    if (!ok && PortFailureCount() == failuresBefore)
                    {
                        PortIncrementFailures();
                    }
                    if (!ok && isMakeNewGameFlow)
                    {
                        PortCaptureDebugScreenshot("MakeNewGameFailed");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                PortCaptureFailureScreenshot("WorkflowCancelled");
                PortSetAppStatus("Cancelled");
                AddWorkflowStep("Flow cancelled");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Unhandled exception in automation run.", ex);
                PortIncrementFailures();
                PortCaptureFailureScreenshot("UnexpectedException");
                PortSetAppStatus("Flow Failed");
                AddWorkflowStep("Flow failed");
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ForceReleaseAllRuntimeInputs("workflow exit");

                if (ReferenceEquals(portAutomationCts, localCts))
                {
                    portAutomationCts?.Dispose();
                    portAutomationCts = null;
                    isAutomationRunning = false;
                    portAutomationBlockedByTeleportFailsafe = false;
                }

                PortSetEscapeStatus("Press Esc to stop");
            }
        }

        private bool PortBounceNewTristramThroughHiddenCamp(CancellationToken token)
        {
            AddWorkflowStep("Already in New Tristram, bouncing to Hidden Camp");

            if (!PortOpenMapAndWait(token))
            {
                return PortWorkflowFailed("Opening map before Hidden Camp teleport");
            }

            AddWorkflowStep("Teleporting to Hidden Camp");
            if (!PortTeleportToLocation("Hidden Camp", token, verifyArrival: true, mapAlreadyOpen: true, bypassFailsafe: true))
            {
                return PortWorkflowFailed("Teleporting to Hidden Camp");
            }

            AddWorkflowStep("Hidden Camp arrival confirmed");

            if (!PortOpenMapAndWait(token))
            {
                return PortWorkflowFailed("Opening map before New Tristram teleport");
            }

            AddWorkflowStep("Teleporting back to New Tristram");
            if (!PortTeleportToLocation("New Tristram", token, verifyArrival: true, mapAlreadyOpen: true, bypassFailsafe: true))
            {
                return PortWorkflowFailed("Teleporting back to New Tristram");
            }

            AddWorkflowStep("New Tristram arrival confirmed");
            return true;
        }

        /// <summary>
        /// Opens the map, switches acts when needed, clicks the requested waypoint, and optionally verifies arrival.
        /// </summary>
        private bool PortTeleportToLocation(string displayName, CancellationToken token, bool verifyArrival = false, bool mapAlreadyOpen = false, bool bypassFailsafe = false, string source = "Workflow")
        {
            AddWorkflowStep($"Teleporting to {displayName}");
            portLastTeleportSource = source;
            bool isManualButtonSource =
                source.Equals("Button", StringComparison.OrdinalIgnoreCase) ||
                source.Equals("ButtonRetry", StringComparison.OrdinalIgnoreCase);
            bool shouldCheckBlocking = !bypassFailsafe && source.Equals("Hotkey", StringComparison.OrdinalIgnoreCase);
            bool blockingSkippedForManualSource = isManualButtonSource && !shouldCheckBlocking;
            AppLogger.Info($"Teleport target location: {displayName}; source={source}; ignoreBlocking={bypassFailsafe}; blockingChecked={shouldCheckBlocking}; blockingSkippedForManualSource={blockingSkippedForManualSource}");
            if (blockingSkippedForManualSource)
            {
                portLastBlockingDecision = $"Skipped; Source={source}; Requested={PortDisplayLocation(displayName)}; IgnoreBlocking={bypassFailsafe}; BlockingChecked={shouldCheckBlocking}";
                portLastBlockingReason = "Manual button source skips teleport blocking";
                AppLogger.Info($"Teleport blocking skipped because source is {source}: requested={displayName}; ignoreBlocking={bypassFailsafe}; blockingChecked={shouldCheckBlocking}");
            }
            PortSetAppStatus($"Teleporting To {displayName}");
            if (token.IsCancellationRequested)
            {
                return false;
            }
            string targetKey = PortLocationKey(displayName);
            if (!portLocationCoords.TryGetValue(targetKey, out PortMapPoint? target))
            {
                MessageBox.Show($"No map coordinates found for {displayName}.");
                return false;
            }

            if (!ActivateDiabloWindow())
            {
                return false;
            }

            string currentLocation = PortDetectSpecificLocation(displayName);
            AppLogger.Info($"Teleport detected current location before teleport: raw={PortDisplayLocation(currentLocation)}; normalized={PortDisplayLocation(PortNormalizeBlockingLocation(currentLocation))}; button={PortDisplayLocation(PortGetButtonLocationForDetectedLocation(currentLocation))}; requested={displayName}; previousConfirmed={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}");
            AddWorkflowStep($"Current location detected: {PortDisplayLocation(currentLocation)}");
            if (source.Equals("Button", StringComparison.OrdinalIgnoreCase) &&
                PortLocationKey(displayName) == PortLocationKey("Ancient Waterway"))
            {
                string exactWaterwayLocation = PortRefreshBlockingLocationForTarget(displayName);
                if (string.IsNullOrWhiteSpace(exactWaterwayLocation))
                {
                    exactWaterwayLocation = currentLocation;
                }

                if (PortLocationKey(exactWaterwayLocation) == PortLocationKey("Ancient Waterway"))
                {
                    AppLogger.Info($"Ancient Waterway already-here/self-teleport block: requested={displayName}; rawDetected={PortDisplayLocation(exactWaterwayLocation)}; confirmed={PortDisplayLocation(portLastConfirmedLocation)}; display={PortDisplayLocation(PortGetButtonLocationForDetectedLocation(portLastConfirmedLocation))}; currentKey={portLastTeleportKey}; nextKey={portQueuedTeleportKey}; blockingRule=Already inside Ancient Waterway");
                    PortNotifyAlreadyHere("Ancient Waterway", displayName, source);
                    return false;
                }
            }

            if (PortLocationIsAlreadyAtTarget(currentLocation, displayName))
            {
                AppLogger.Info($"Already at target location: {displayName}");
                portLastConfirmedLocation = currentLocation;
                PortSetAppStatus("Already At Target");
                AddWorkflowStep($"Already at {displayName}; teleport complete");
                return true;
            }

            if (shouldCheckBlocking && !PortTeleportFailsafeAllows(displayName, out string blockedLocation))
            {
                PortNotifyTeleportBlocked(blockedLocation, displayName, source);
                return false;
            }

            if (mapAlreadyOpen && !PortWaitForMapReady(token, 750))
            {
                PortSetAppStatus("Map Not Ready, Reopening");
                mapAlreadyOpen = false;
            }

            if (!mapAlreadyOpen)
            {
                if (!PortOpenMapAndWait(token))
                {
                    PortSetAppStatus("Map Not Ready");
                    return false;
                }
            }

            string currentAct = PortDetectMapActHeader(logPerf: true);
            AppLogger.Info($"Teleport to {displayName}: current map act={PortDisplayLocation(currentAct)}, target act={target.Act}, target coords={target.X},{target.Y}");
            if (!string.Equals(currentAct, target.Act, StringComparison.OrdinalIgnoreCase))
            {
                PortSetAppStatus("Switching Act");
                if (!portActCoords.TryGetValue(target.Act, out PortMapPoint? act))
                {
                    AppLogger.Info($"Teleport to {displayName} failed: missing world map coordinates for {target.Act}");
                    return false;
                }

                AppLogger.Info($"Teleport to {displayName}: switching act via world map {target.Act} at {act.X},{act.Y}");
                if (!PortSafeRightClick(PortScaleGamePoint(portMapRightClickPoint)))
                {
                    AppLogger.Info($"Teleport to {displayName} failed: right-click to world map was unsafe");
                    return false;
                }

                if (!PortWaitForWorldMapReady(token, 5000))
                {
                    AppLogger.Info($"Teleport to {displayName} failed: world map did not become ready");
                    return false;
                }

                if (!PortSafeLeftClick(PortScaleGamePoint(new DrawingPoint(act.X, act.Y))))
                {
                    AppLogger.Info($"Teleport to {displayName} failed: act click for {target.Act} was unsafe");
                    return false;
                }

                PortSetAppStatus("Waiting For Act");
                if (!PortWaitForMapActHeader(target.Act, token, 6000))
                {
                    AppLogger.Info($"Teleport to {displayName} failed: act header did not change to {target.Act}");
                    return false;
                }
            }

            PortSetAppStatus("Clicking Destination");
            AppLogger.Info($"Teleport to {displayName}: clicking destination at {target.X},{target.Y}");
            if (!PortSafeLeftClick(PortScaleGamePoint(new DrawingPoint(target.X, target.Y))))
            {
                return false;
            }

            PortSetAppStatus("Waiting For Location Confirmation");
            int failuresBeforeArrivalConfirmation = PortFailureCount();
            string confirmedAfter = "";
            bool arrived = true;
            if (verifyArrival)
            {
                portTeleportWaitingForConfirmation = true;
                portTeleportWaitingConfirmationKey = PortLocationKey(displayName);
                try
                {
                    arrived = PortWaitForSpecificLocation(displayName, token, PortArrivalConfirmationTimeoutMs, out confirmedAfter);
                }
                finally
                {
                    portTeleportWaitingForConfirmation = false;
                    portTeleportWaitingConfirmationKey = "";
                }
            }

            if (arrived && string.IsNullOrWhiteSpace(confirmedAfter))
            {
                confirmedAfter = PortDetectSpecificLocation(displayName);
            }
            if (arrived)
            {
                portLastConfirmedLocation = confirmedAfter;
            }
            else if (verifyArrival && PortFailureCount() == failuresBeforeArrivalConfirmation)
            {
                PortIncrementFailures();
                PortCaptureFailureScreenshot(token.IsCancellationRequested ? "TeleportInterrupted" : "TeleportConfirmationTimeout");
            }

            if (!arrived && verifyArrival)
            {
                PortRestoreTeleportButtonStateFromLastConfirmedLocation($"arrival confirmation failed: {displayName}");
            }
            AppLogger.Info($"Teleport confirmed current location after teleport: raw={PortDisplayLocation(confirmedAfter)}; normalized={PortDisplayLocation(PortNormalizeBlockingLocation(confirmedAfter))}; button={PortDisplayLocation(PortGetButtonLocationForDetectedLocation(confirmedAfter))}; requested={displayName}; success={arrived}");
            PortSetAppStatus(arrived ? "Teleport Complete" : "Teleport Failed");
            AddWorkflowStep(arrived ? $"Teleport complete: {displayName}" : $"Teleport failed: {displayName}");
            return arrived;
        }

        private bool PortOpenMapAndWait(CancellationToken token)
        {
            AddWorkflowStep("Opening map");
            PortSetAppStatus("Opening Map");
            Stopwatch openPerf = Stopwatch.StartNew();
            if (!ActivateDiabloWindow())
            {
                PortWorkflowFailed("Activating Diablo before opening map");
                return false;
            }

            if (PortWaitForMapReady(token, 100))
            {
                AddWorkflowStep("Map already open");
                return true;
            }

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 8000)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }
                ActivateDiabloWindow();
                PortPressKey(PortVkM);
                PortSleep(token, 350);

                if (PortWaitForMapReady(token, 1500))
                {
                    AppLogger.Info($"Teleport map opening time: {openPerf.ElapsedMilliseconds}ms");
                    PortSetAppStatus("Map Opened");
                    AddWorkflowStep("Map opened");
                    return true;
                }
            }

            PortSetAppStatus("Map Did Not Open");
            AppLogger.Info($"Teleport map opening time: failed after {openPerf.ElapsedMilliseconds}ms");
            PortWorkflowFailed("Opening map");
            return false;
        }

        private bool PortCloseOpenPanels(CancellationToken token)
        {
            AddWorkflowStep("Closing open panels");

            for (int attempt = 0; attempt < 6; attempt++)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (!PortVendorPanelVisible())
                {
                    AddWorkflowStep("Open panels closed");
                    return true;
                }

                PortPressEscapeForAutomation();
                PortSleep(token, 700);
            }

            bool closed = !PortVendorPanelVisible();
            AddWorkflowStep(closed ? "Open panels closed" : "Open panels still visible");
            return closed;
        }

        private bool PortOpenGameMenu(CancellationToken token)
        {
            AddWorkflowStep("Opening game menu");

            for (int attempt = 0; attempt < 5; attempt++)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (PortGameMenuVisible())
                {
                    return true;
                }

                PortPressEscapeForAutomation();
                PortSleep(token, 900);
            }

            return PortGameMenuVisible();
        }

        private bool PortWaitForLeaveGameButtonAndClick(CancellationToken token, int timeoutMs)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (FindImageInDiabloWindow(Img("Leave Game", "Leave Game Button.png"), out DrawingPoint centerPoint, confidence: PortGameMenuConfidence))
                {
                    return PortSafeLeftClick(centerPoint);
                }

                PortSleep(token, 150);
            }

            AddWorkflowStep("Leave Game button not found");
            return false;
        }

        private void PortStopAllAutomation(string reason)
        {
            portAutomationCts?.Cancel();
            PortStopCombat(reason);
            ForceReleaseAllRuntimeInputs($"automation stop: {reason}");
            ClipCursor(IntPtr.Zero);

            if (isAutomationRunning)
            {
                PortSetAppStatus($"Stopping ({reason})");
            }

            PortSetEscapeStatus($"Stop requested ({reason})");
        }

        private void PortHandleDiabloExited()
        {
            AppLogger.Info("Diablo process exited; cancelling runtime automation");
            portAutomationCts?.Cancel();
            PortStopCombat("Diablo exited");
            portLootSpamLeftClickDown = false;
            ForceReleaseAllRuntimeInputs("Diablo exited");
            ClipCursor(IntPtr.Zero);
            isAutomationRunning = false;
            portAutomationBlockedByTeleportFailsafe = false;
            PortSetAppStatus("Diablo Not Running / Idle");
            SetCombatStatus("Idle");
            AddWorkflowStep("Diablo exited; runtime stopped");
        }

        private bool PortSafeLeftClick(DrawingPoint point)
        {
            return PortSafeClick(point, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP);
        }

        private bool PortSafeRightClick(DrawingPoint point)
        {
            return PortSafeClick(point, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP);
        }

        private bool PortSafeClick(DrawingPoint point, uint down, uint up)
        {
            if (!PortClickPointIsSafe(point))
            {
                PortSetAppStatus("Unsafe Click Blocked");
                return false;
            }

            SetCursorPos(point.X, point.Y);
            Thread.Sleep(80);
            PortRuntimeMouseDown(down);
            Thread.Sleep(40);
            PortRuntimeMouseUp(up);
            return true;
        }

        private bool PortClickPointIsSafe(DrawingPoint point)
        {
            if (!SystemInformation.VirtualScreen.Contains(point))
            {
                return false;
            }

            if (!PortTryGetDiabloRect(out RECT rect))
            {
                return false;
            }

            return point.X >= rect.Left && point.X < rect.Right && point.Y >= rect.Top && point.Y < rect.Bottom;
        }

        private void PortShowSplash(string message, int durationMs)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    if (portSplashForm == null || portSplashForm.IsDisposed)
                    {
                        portSplashForm = new PortNoActivateSplashForm
                        {
                            FormBorderStyle = FormBorderStyle.None,
                            StartPosition = FormStartPosition.Manual,
                            ShowInTaskbar = false,
                            TopMost = true,
                            BackColor = Color.FromArgb(33, 7, 7),
                            Opacity = 0.90,
                            Width = 520,
                            Height = 92,
                        };

                        portSplashLabel = new Label
                        {
                            Dock = DockStyle.Fill,
                            ForeColor = Color.White,
                            BackColor = Color.Transparent,
                            Font = new Font(Font.FontFamily, 16.0f, FontStyle.Bold),
                            TextAlign = ContentAlignment.MiddleCenter,
                        };

                        portSplashForm.Controls.Add(portSplashLabel);
                    }

                    if (portSplashTimer == null)
                    {
                        portSplashTimer = new System.Windows.Forms.Timer();
                        portSplashTimer.Tick += (_, _) => PortHideSplash();
                    }

                    portSplashTimer.Stop();
                    portSplashTimer.Interval = Math.Max(500, durationMs);
                    if (portSplashLabel != null)
                    {
                        portSplashLabel.Text = message;
                    }

                    PortPositionSplash();
                    portSplashForm.Show();
                    portSplashTimer.Start();
                    AppLogger.Info($"Notification shown without focus steal: message={message}; diabloActive={PortDiabloIsActive()}");
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Unable to show teleport blocked splash.", ex);
                }
            });
        }

        private void PortPositionSplash()
        {
            if (portSplashForm == null)
            {
                return;
            }

            if (PortTryGetDiabloRect(out RECT rect))
            {
                portSplashForm.Left = rect.Left + ((rect.Right - rect.Left) - portSplashForm.Width) / 2;
                portSplashForm.Top = rect.Top + ((rect.Bottom - rect.Top) - portSplashForm.Height) / 2;
                return;
            }

            Rectangle screen = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.VirtualScreen;
            portSplashForm.Left = screen.Left + (screen.Width - portSplashForm.Width) / 2;
            portSplashForm.Top = screen.Top + (screen.Height - portSplashForm.Height) / 2;
        }

        private void PortHideSplash()
        {
            if (portSplashTimer != null)
            {
                portSplashTimer.Stop();
            }

            if (portSplashForm != null && !portSplashForm.IsDisposed)
            {
                portSplashForm.Hide();
            }
        }

        private void PortLoadCoordinates()
        {
            PortParseMapCoordinates(Img("Teleport Function", "Map X Y Coordinates.txt"));
            portRepairCoords = PortParseNamedCoordinates(Img("Repair", "Repair Station Coordinates.txt"), new()
            {
                ["Repair Station"] = new(1841, 198),
                ["Repair Tab"] = new(677, 815),
                ["Repair Button"] = new(361, 715),
            });
            portSalvageCoords = PortParseNamedCoordinates(Img("Salvage", "Salvage Coordinates.txt"), new()
            {
                ["Salvage Tab"] = new(683, 638),
                ["Salvage Button"] = new(215, 382),
            });
            portLeaveGamePoint = PortParseSingleCoordinate(Img("Leave Game", "Leave Game Button Coordinates.txt"), new(326, 639));
        }

        private void PortLoadImageCaches()
        {
            Stopwatch perf = Stopwatch.StartNew();
            portCurrentLocationTemplates.Clear();

            string currentLocationFolder = Img("Current Location");
            string teleportFunctionFolder = Img("Teleport Function");
            AppLogger.Info($"Image folder resolved: Current Location={currentLocationFolder}");
            AppLogger.Info($"Image folder resolved: Teleport Function={teleportFunctionFolder}");
            if (!Directory.Exists(currentLocationFolder))
            {
                AppLogger.Info($"WARNING PortLoadImageCaches: Current Location folder missing: {currentLocationFolder}");
                AppLogger.Info($"PERF PortLoadImageCaches: Current Location folder missing in {perf.ElapsedMilliseconds}ms");
                return;
            }

            if (!Directory.Exists(teleportFunctionFolder))
            {
                AppLogger.Info($"WARNING PortLoadImageCaches: Teleport Function folder missing: {teleportFunctionFolder}");
            }

            foreach (string imagePath in Directory.GetFiles(currentLocationFolder, "*.png"))
            {
                string name = PortNormalizeLocation(Path.GetFileNameWithoutExtension(imagePath));
                portCurrentLocationTemplates[name] = imagePath;
            }

            foreach (string requiredLocationTemplate in new[]
            {
                "Ancient Waterway",
                "Eastern Channel Level 1",
                "Eastern Channel Level 2",
                "Western Channel Level 1",
                "Western Channel Level 2",
            })
            {
                if (portCurrentLocationTemplates.ContainsKey(PortNormalizeLocation(requiredLocationTemplate)))
                {
                    AppLogger.Info($"Current-location template available: {requiredLocationTemplate}");
                }
                else
                {
                    AppLogger.Info($"WARNING Current-location template missing: {requiredLocationTemplate}");
                }
            }

            AppLogger.Info($"PERF PortLoadImageCaches: cached {portCurrentLocationTemplates.Count} current-location templates in {perf.ElapsedMilliseconds}ms");
        }

        private void PortParseMapCoordinates(string path)
        {
            if (!File.Exists(path))
            {
                AppLogger.Info($"Teleport coordinate file missing: {path}");
                return;
            }

            string currentAct = "";
            foreach (string rawLine in File.ReadLines(path))
            {
                string line = rawLine.Trim();
                Match header = Regex.Match(line, @"=+\s*(Act\s+\d+)(?:\s+Locations)?\s*=+", RegexOptions.IgnoreCase);
                if (header.Success)
                {
                    currentAct = PortTitleCase(header.Groups[1].Value);
                    continue;
                }

                Match match = Regex.Match(line, @"(.+?)\s*-\s*(\d+)\s*,\s*(\d+)");
                if (!match.Success)
                {
                    continue;
                }

                string name = PortNormalizeLocation(match.Groups[1].Value);
                int x = int.Parse(match.Groups[2].Value);
                int y = int.Parse(match.Groups[3].Value);

                if (Regex.IsMatch(name, @"^Act\s+\d+$", RegexOptions.IgnoreCase))
                {
                    string actName = PortTitleCase(name);
                    portActCoords[actName] = new(actName, actName, x, y);
                }
                else if (!string.IsNullOrWhiteSpace(currentAct))
                {
                    portLocationCoords[PortLocationKey(name)] = new(name, currentAct, x, y);
                }
            }

            string act2 = portActCoords.TryGetValue("Act 2", out PortMapPoint? act2Point)
                ? $"{act2Point.X},{act2Point.Y}"
                : "missing";
            string city = portLocationCoords.TryGetValue(PortLocationKey("City Of Caldeum"), out PortMapPoint? cityPoint)
                ? $"{cityPoint.Act} {cityPoint.X},{cityPoint.Y}"
                : "missing";
            AppLogger.Info($"Teleport coordinates loaded: acts={portActCoords.Count}, locations={portLocationCoords.Count}, Act 2={act2}, City Of Caldeum={city}");
        }

        private Dictionary<string, DrawingPoint> PortParseNamedCoordinates(string path, Dictionary<string, DrawingPoint> defaults)
        {
            Dictionary<string, DrawingPoint> points = new(defaults, StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path))
            {
                return points;
            }

            string currentName = "";
            foreach (string rawLine in File.ReadLines(path))
            {
                string line = rawLine.Trim();
                Match header = Regex.Match(line, @"=+\s*(.+?)\s*=+");
                if (header.Success)
                {
                    currentName = header.Groups[1].Value.Trim();
                    continue;
                }

                Match match = Regex.Match(line, @"(\d+)\s*,\s*(\d+)");
                if (!string.IsNullOrWhiteSpace(currentName) && match.Success)
                {
                    points[currentName] = new(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
                }
            }

            return points;
        }

        private DrawingPoint PortParseSingleCoordinate(string path, DrawingPoint fallback)
        {
            if (!File.Exists(path))
            {
                return fallback;
            }

            Match match = Regex.Match(File.ReadAllText(path), @"(\d+)\s*,\s*(\d+)");
            return match.Success ? new DrawingPoint(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value)) : fallback;
        }

        private IntPtr PortCurrentCursorHandle()
        {
            CURSORINFO info = new()
            {
                cbSize = Marshal.SizeOf<CURSORINFO>()
            };

            return GetCursorInfo(out info) ? info.hCursor : IntPtr.Zero;
        }

        private bool PortIsWaypointExactMatchRequired(string location)
        {
            string key = PortLocationKey(location);
            return PortIsPandemoniumLocation(location);
        }

        private bool PortIsAncientWaterwayRegion(string location)
        {
            string key = PortLocationKey(location);
            return key == PortLocationKey("Ancient Waterway") ||
                key == PortLocationKey("Ruined Cistern") ||
                key == PortLocationKey("Western Channel Level 1") ||
                key == PortLocationKey("Western Channel Level 2") ||
                key == PortLocationKey("Eastern Channel Level 1") ||
                key == PortLocationKey("Eastern Channel Level 2");
        }

        private bool PortTryGetDiabloRect(out RECT rect)
        {
            rect = default;
            IntPtr diabloWindow = FindDiabloWindow();
            return diabloWindow != IntPtr.Zero && GetWindowRect(diabloWindow, out rect) && rect.Right > rect.Left && rect.Bottom > rect.Top;
        }

        private DrawingPoint PortScaleGamePoint(DrawingPoint point)
        {
            if (!PortTryGetDiabloRect(out RECT rect))
            {
                return point;
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            return new(
                rect.Left + (int)Math.Round(point.X * width / (double)PortReferenceWidth),
                rect.Top + (int)Math.Round(point.Y * height / (double)PortReferenceHeight));
        }

        private Rectangle PortScaleReferenceRectangle(Rectangle rectangle, RECT rect)
        {
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            return new(
                rect.Left + (int)Math.Round(rectangle.Left * width / (double)PortReferenceWidth),
                rect.Top + (int)Math.Round(rectangle.Top * height / (double)PortReferenceHeight),
                (int)Math.Round(rectangle.Width * width / (double)PortReferenceWidth),
                (int)Math.Round(rectangle.Height * height / (double)PortReferenceHeight));
        }

        private Rectangle PortReferenceRegion(int left, int top, int width, int height)
        {
            return new Rectangle(left, top, width, height);
        }

        private void ForceReleaseAllRuntimeInputs(string reason)
        {
            IntPtr diabloWindow = FindDiabloWindow();
            RECT diabloRect = default;
            bool hasDiabloRect = diabloWindow != IntPtr.Zero && GetWindowRect(diabloWindow, out diabloRect) && diabloRect.Right > diabloRect.Left && diabloRect.Bottom > diabloRect.Top;
            string windowText = diabloWindow == IntPtr.Zero ? "0x0" : $"0x{diabloWindow.ToInt64():X}";
            string rectText = hasDiabloRect
                ? $"{diabloRect.Left},{diabloRect.Top},{diabloRect.Right - diabloRect.Left}x{diabloRect.Bottom - diabloRect.Top}"
                : "unavailable";
            bool heldLeft = portRuntimeLeftMouseHeld;
            bool heldRight = portRuntimeRightMouseHeld;
            bool heldShift = portRuntimeShiftHeld;
            bool hadLootSpam = portLootSpamLeftClickDown;

            AppLogger.Info($"ForceReleaseAllRuntimeInputs called: reason={reason}; heldLeft={heldLeft}; heldRight={heldRight}; heldShift={heldShift}; lootSpamActive={hadLootSpam}; diabloWindow={windowText}; diabloRect={rectText}; {PortCombatInputContext()}");
            portLootSpamLeftClickDown = false;
            if (hadLootSpam)
            {
                AppLogger.Info($"Loot spam force cleanup; {PortCombatInputContext()}");
            }

            if (!hasDiabloRect)
            {
                portRuntimeLeftMouseHeld = false;
                portRuntimeRightMouseHeld = false;
                portRuntimeShiftHeld = false;
                AppLogger.Info($"ForceReleaseAllRuntimeInputs release skipped: reason={reason}; input=left; held={heldLeft}; sent=false; Diablo window unavailable; state cleared");
                AppLogger.Info($"ForceReleaseAllRuntimeInputs release skipped: reason={reason}; input=right; held={heldRight}; sent=false; Diablo window unavailable; state cleared");
                AppLogger.Info($"ForceReleaseAllRuntimeInputs release skipped: reason={reason}; input=shift; held={heldShift}; sent=false; Diablo window unavailable; state cleared");
                AppLogger.Info($"ForceReleaseAllRuntimeInputs complete: reason={reason}; sentLeft=false; sentRight=false; sentShift=false; diabloWindow={windowText}; diabloRect={rectText}; {PortCombatInputContext()}");
                return;
            }

            bool sentLeft = false;
            bool sentRight = false;
            bool sentShift = false;

            if (heldLeft)
            {
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                portRuntimeLeftMouseHeld = false;
                sentLeft = true;
                AppLogger.Info($"ForceReleaseAllRuntimeInputs release sent: reason={reason}; input=left; held=true; diabloWindow={windowText}; diabloRect={rectText}");
            }
            else
            {
                AppLogger.Info($"ForceReleaseAllRuntimeInputs release skipped: reason={reason}; input=left; held=false; sent=false; diabloWindow={windowText}; diabloRect={rectText}");
            }

            if (heldRight)
            {
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                portRuntimeRightMouseHeld = false;
                sentRight = true;
                AppLogger.Info($"ForceReleaseAllRuntimeInputs release sent: reason={reason}; input=right; held=true; diabloWindow={windowText}; diabloRect={rectText}");
            }
            else
            {
                AppLogger.Info($"ForceReleaseAllRuntimeInputs release skipped: reason={reason}; input=right; held=false; sent=false; diabloWindow={windowText}; diabloRect={rectText}");
            }

            if (heldShift)
            {
                keybd_event(PortVkShift, 0, PortKeyUp, UIntPtr.Zero);
                portRuntimeShiftHeld = false;
                sentShift = true;
                AppLogger.Info($"ForceReleaseAllRuntimeInputs release sent: reason={reason}; input=shift; held=true; diabloWindow={windowText}; diabloRect={rectText}");
            }
            else
            {
                AppLogger.Info($"ForceReleaseAllRuntimeInputs release skipped: reason={reason}; input=shift; held=false; sent=false; diabloWindow={windowText}; diabloRect={rectText}");
            }

            AppLogger.Info($"ForceReleaseAllRuntimeInputs complete: reason={reason}; sentLeft={sentLeft}; sentRight={sentRight}; sentShift={sentShift}; diabloWindow={windowText}; diabloRect={rectText}; {PortCombatInputContext()}");
        }

        private void PortReleaseInputs(string reason = "cleanup")
        {
            ForceReleaseAllRuntimeInputs(reason);
        }

        private void PortPressKey(int vk)
        {
            keybd_event((byte)vk, 0, 0, UIntPtr.Zero);
            Thread.Sleep(10);
            keybd_event((byte)vk, 0, PortKeyUp, UIntPtr.Zero);
        }

        private void PortRuntimeMouseDown(uint buttonDown)
        {
            if (buttonDown == MOUSEEVENTF_LEFTDOWN)
            {
                portRuntimeLeftMouseHeld = true;
            }
            else if (buttonDown == MOUSEEVENTF_RIGHTDOWN)
            {
                portRuntimeRightMouseHeld = true;
            }

            mouse_event(buttonDown, 0, 0, 0, UIntPtr.Zero);
        }

        private void PortRuntimeMouseUp(uint buttonUp)
        {
            mouse_event(buttonUp, 0, 0, 0, UIntPtr.Zero);

            if (buttonUp == MOUSEEVENTF_LEFTUP)
            {
                portRuntimeLeftMouseHeld = false;
            }
            else if (buttonUp == MOUSEEVENTF_RIGHTUP)
            {
                portRuntimeRightMouseHeld = false;
            }
        }

        private void PortRuntimeShiftDown()
        {
            portRuntimeShiftHeld = true;
            keybd_event(PortVkShift, 0, 0, UIntPtr.Zero);
        }

        private void PortRuntimeShiftUp()
        {
            keybd_event(PortVkShift, 0, PortKeyUp, UIntPtr.Zero);
            portRuntimeShiftHeld = false;
        }

        private void PortPressEscapeForAutomation()
        {
            Interlocked.Exchange(ref portIgnoreEscapeHotkeyUntilTicks, DateTime.UtcNow.AddMilliseconds(900).Ticks);
            PortPressKey(PortVkEscape);
        }

        private void PortSetAppStatus(string status)
        {
            SetAppStatus(status);
        }

        private void PortSetEscapeStatus(string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => PortSetEscapeStatus(status)));
                return;
            }

            lblEscape.Text = status;
        }

        private bool PortWorkflowFailed(string step)
        {
            AddWorkflowStep($"FAILED: {step}");
            AppLogger.Error($"Workflow step failed: {step}");
            PortIncrementFailures();
            PortCaptureDebugScreenshot("WorkflowFailed");
            ForceReleaseAllRuntimeInputs($"workflow failed: {step}");
            return false;
        }

        private static void PortSleep(CancellationToken token, int milliseconds)
        {
            int remaining = milliseconds;
            while (remaining > 0)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                int chunk = Math.Min(100, remaining);
                Thread.Sleep(chunk);
                remaining -= chunk;
            }
        }
    }
}

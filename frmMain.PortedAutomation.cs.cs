using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using OpenCvSharp;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private const int PortReferenceWidth = 2560;
        private const int PortReferenceHeight = 1440;
        private const int PortVkShift = 0x10;
        private const int PortVkAlt = 0x12;
        private const int PortVkM = 0x4D;
        private const int PortVkReturn = 0x0D;
        private const int PortVkEscape = 0x1B;
        private const int PortVkUp = 0x26;
        private const int PortVk1 = 0x31;
        private const int PortVkBacktick = 0xC0;
        private const int PortKeyUp = 0x0002;
        private const int PortWhKeyboardLl = 13;
        private const int PortWmKeyDown = 0x0100;
        private const int PortWmKeyUp = 0x0101;
        private const int PortWmSysKeyDown = 0x0104;
        private const int PortWmSysKeyUp = 0x0105;
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

        private readonly (double Left, double Top, double Width, double Height)[] portCombatNoClickRegions =
        [
            (0 / 2560.0, 0 / 1440.0, 166 / 2560.0, 226 / 1440.0),
            (141 / 2560.0, 4 / 1440.0, 135 / 2560.0, 159 / 1440.0),
            (6 / 2560.0, 1270 / 1440.0, 118 / 2560.0, 134 / 1440.0),
            (430 / 2560.0, 1220 / 1440.0, 390 / 2560.0, 220 / 1440.0),
            (818 / 2560.0, 1300 / 1440.0, 930 / 2560.0, 130 / 1440.0),
            (1690 / 2560.0, 1220 / 1440.0, 410 / 2560.0, 220 / 1440.0),
            (2310 / 2560.0, 1278 / 1440.0, 214 / 2560.0, 124 / 1440.0),
            (2448 / 2560.0, 472 / 1440.0, 44 / 2560.0, 62 / 1440.0),
            (2237 / 2560.0, 28 / 1440.0, 115 / 2560.0, 50 / 1440.0),
        ];

        private volatile bool portCombatRunning;
        private volatile bool portHotkeysRunning;
        private Thread? portHotkeyThread;
        private IntPtr portKeyboardHookHandle = IntPtr.Zero;
        private LowLevelKeyboardProc? portKeyboardProc;
        private CancellationTokenSource? portAutomationCts;
        private CancellationTokenSource? portCombatCts;
        private bool portInitialized;
        private int portMonkKeyIndex = 1;
        private long portIgnoreEscapeHotkeyUntilTicks;
        private string portCombatClass = "";
        private string portLastTeleportKey = "";
        private string portQueuedTeleportKey = "";
        private string portLastConfirmedLocation = "";
        private volatile bool portAutomationBlockedByTeleportFailsafe;
        private volatile bool portSuppressSkill1KeyUp;
        private volatile bool portBlockSkill1TeleportHotkey = true;
        private long portLastTeleportNextHotkeyTicks;
        private long portIgnoreTeleportNextUntilTicks;
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
            chkBlockSkill1TeleportHotkey.Checked = true;
            portBlockSkill1TeleportHotkey = chkBlockSkill1TeleportHotkey.Checked;
            chkBlockSkill1TeleportHotkey.CheckedChanged += (_, _) => portBlockSkill1TeleportHotkey = chkBlockSkill1TeleportHotkey.Checked;

            portHotkeysRunning = true;
            portHotkeyThread = new Thread(PortHotkeyLoop) { IsBackground = true };
            portHotkeyThread.Start();
            PortInstallKeyboardHook();
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
            PortReleaseInputs();
            ClipCursor(IntPtr.Zero);
            base.OnFormClosing(e);
        }

        private void PortWireButtons()
        {
            btnMakeNewGame.Click += (_, _) => _ = PortRunAutomationAsync(PortMakeNewGameFlow);
            btnExitGame.Click += (_, _) => _ = PortRunAutomationAsync(token => PortLeaveCurrentGame(token));

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
            PortWireTeleportButton(btnTheBridgeOfKorsikk, "The Bridge of Korsikk");
            PortWireTeleportButton(btnRakkisCrossing, "Rakkis Crossing");
            PortWireTeleportButton(btnPandemoniumFortressLevel1, "Pandemonium Fortress Level 1");
            PortWireTeleportButton(btnPandemoniumFortressLevel2, "Pandemonium Fortress Level 2");
        }

        private void PortWireTeleportButton(Button button, string location)
        {
            string key = PortLocationKey(location);
            portTeleportButtons[key] = button;
            portButtonDefaultBackColors.TryAdd(button, button.BackColor);
            portButtonDefaultForeColors.TryAdd(button, button.ForeColor);
            button.UseVisualStyleBackColor = false;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.AutoEllipsis = true;
            button.Click += (_, _) => _ = PortRunAutomationAsync(token => PortRunTeleportButton(location, token, ignoreBlocking: true, source: "Button"));
        }

        private bool PortRunTeleportButton(string location, CancellationToken token, bool ignoreBlocking, string source)
        {
            bool arrived = PortTeleportToLocation(location, token, verifyArrival: true, bypassFailsafe: ignoreBlocking, source: source);
            if (arrived)
            {
                PortRecordTeleport(location, portLastConfirmedLocation);
            }
            else if (!portAutomationBlockedByTeleportFailsafe)
            {
                PortClearTeleportButtonStates($"teleport did not confirm: {location}");
            }

            return arrived;
        }

        private async Task PortRunAutomationAsync(Func<CancellationToken, bool> work)
        {
            if (isAutomationRunning || portCombatRunning)
            {
                return;
            }

            isAutomationRunning = true;
            portAutomationBlockedByTeleportFailsafe = false;
            portAutomationCts = new CancellationTokenSource();
            PortSetEscapeStatus("Esc stops script activity");
            AddWorkflowStep("Automation started");

            try
            {
                bool ok = await Task.Run(() => work(portAutomationCts.Token));
                if (portAutomationCts.IsCancellationRequested)
                {
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
                }
            }
            catch (OperationCanceledException)
            {
                PortSetAppStatus("Cancelled");
                AddWorkflowStep("Flow cancelled");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Unhandled exception in automation run.", ex);
                PortSetAppStatus("Flow Failed");
                AddWorkflowStep("Flow failed");
                MessageBox.Show(ex.Message);
            }
            finally
            {
                portAutomationCts?.Dispose();
                portAutomationCts = null;
                isAutomationRunning = false;
                portAutomationBlockedByTeleportFailsafe = false;
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

        private bool PortTeleportToLocation(string displayName, CancellationToken token, bool verifyArrival = false, bool mapAlreadyOpen = false, bool bypassFailsafe = false, string source = "Workflow")
        {
            AddWorkflowStep($"Teleporting to {displayName}");
            bool shouldCheckBlocking = !bypassFailsafe && source.Equals("Hotkey", StringComparison.OrdinalIgnoreCase);
            AppLogger.Info($"Teleport target location: {displayName}; source={source}; ignoreBlocking={bypassFailsafe}; blockingChecked={shouldCheckBlocking}");
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
            AppLogger.Info($"Teleport detected current location before teleport: {PortDisplayLocation(currentLocation)}; requested={displayName}; previousConfirmed={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}");
            AddWorkflowStep($"Current location detected: {PortDisplayLocation(currentLocation)}");
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
            bool arrived = !verifyArrival || PortWaitForSpecificLocation(displayName, token, PortArrivalConfirmationTimeoutMs);
            string confirmedAfter = arrived ? PortDetectSpecificLocation(displayName) : "";
            if (arrived)
            {
                portLastConfirmedLocation = confirmedAfter;
            }
            AppLogger.Info($"Teleport confirmed current location after teleport: {PortDisplayLocation(confirmedAfter)}; requested={displayName}; success={arrived}");
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
            PortReleaseInputs();
            ClipCursor(IntPtr.Zero);

            if (isAutomationRunning)
            {
                PortSetAppStatus($"Stopping ({reason})");
            }

            PortSetEscapeStatus($"Stop requested ({reason})");
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
            mouse_event(down, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(40);
            mouse_event(up, 0, 0, 0, UIntPtr.Zero);
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

        private bool PortWaitForImageInDiablo(string imagePath, CancellationToken token, int timeoutMs, double confidence)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }
                if (PortImageVisibleInDiablo(imagePath, confidence))
                {
                    return true;
                }

                PortSleep(token, 100);
            }

            return false;
        }

        private bool PortImageVisibleInDiablo(string imagePath, double confidence, ImageMatchMode matchMode = ImageMatchMode.Default)
        {
            return FindImageInDiabloWindow(imagePath, out _, confidence, matchMode);
        }

        private bool PortImageVisibleInDiabloRegion(string imagePath, Rectangle referenceRegion, double confidence)
        {
            return PortBestTemplateConfidenceInDiabloRegion(imagePath, referenceRegion) >= confidence;
        }

        private bool PortStartGameButtonVisible(bool logPerf = false)
        {
            Stopwatch perf = Stopwatch.StartNew();
            bool visible = PortImageVisibleInDiablo(Img("Start Game", "Start Game Button.png"), PortStartGameButtonConfidence, ImageMatchMode.Color);
            if (logPerf || visible || perf.ElapsedMilliseconds >= 1000)
            {
                AppLogger.Info($"PERF PortStartGameButtonVisible: {visible} in {perf.ElapsedMilliseconds}ms");
            }
            return visible;
        }

        private bool PortCharacterLoadConfirmationVisible()
        {
            string imagePath = Img("Start Game", "Character Load Confirmation.png");
            return File.Exists(imagePath) && PortImageVisibleInDiabloRegion(imagePath, PortScanRegion("CharacterLoad", imagePath), PortCharacterLoadConfidence);
        }

        private bool PortPlayerIsInGame()
        {
            return PortCharacterLoadConfirmationVisible() ||
                PortGameMenuVisible() ||
                !string.IsNullOrWhiteSpace(PortDetectSpecificLocation("New Tristram")) ||
                !string.IsNullOrWhiteSpace(PortDetectSpecificLocation("Southern Highlands"));
        }

        private bool PortGameMenuVisible()
        {
            return PortImageVisibleInDiablo(Img("Leave Game", "Leave Game Button.png"), PortGameMenuConfidence);
        }

        private bool PortCloseGameMenuIfOpen(CancellationToken token)
        {
            if (!PortGameMenuVisible())
            {
                return true;
            }

            PortSetAppStatus("Closing Game Menu");
            PortPressEscapeForAutomation();

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 3000)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (!PortGameMenuVisible())
                {
                    PortSleep(token, 250);
                    return true;
                }

                PortSleep(token, 100);
            }

            return false;
        }

        private bool PortWaitForMapReady(CancellationToken token, int timeoutMs)
        {
            Stopwatch perf = Stopwatch.StartNew();
            int scans = 0;
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    AppLogger.Info($"PERF PortWaitForMapReady: cancelled after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return false;
                }
                scans++;
                if (!string.IsNullOrWhiteSpace(PortDetectMapActHeader()))
                {
                    AppLogger.Info($"PERF PortWaitForMapReady: ready after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return true;
                }

                PortSleep(token, 100);
            }

            AppLogger.Info($"PERF PortWaitForMapReady: timeout after {scans} scans in {perf.ElapsedMilliseconds}ms");
            return false;
        }

        private bool PortWaitForWorldMapReady(CancellationToken token, int timeoutMs)
        {
            Stopwatch perf = Stopwatch.StartNew();
            int scans = 0;
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    AppLogger.Info($"PERF PortWaitForWorldMapReady: cancelled after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return false;
                }
                scans++;
                if (PortImageVisibleInDiabloRegion(Img("Teleport Function", "World Map.png"), PortMapHeaderRegion(), PortWorldMapConfidence))
                {
                    AppLogger.Info($"PERF PortWaitForWorldMapReady: matched after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return true;
                }

                PortSleep(token, 100);
            }

            AppLogger.Info($"PERF PortWaitForWorldMapReady: timeout after {scans} scans in {perf.ElapsedMilliseconds}ms");
            return false;
        }

        private bool PortWaitForMapActHeader(string actName, CancellationToken token, int timeoutMs)
        {
            Stopwatch perf = Stopwatch.StartNew();
            int scans = 0;
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    AppLogger.Info($"PERF PortWaitForMapActHeader {actName}: cancelled after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return false;
                }
                scans++;
                string detectedAct = PortDetectMapActHeader();
                if (string.Equals(detectedAct, actName, StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Info($"PERF PortWaitForMapActHeader {actName}: matched after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return true;
                }

                PortSleep(token, 100);
            }

            string finalAct = PortDetectMapActHeader(logPerf: true);
            AppLogger.Info($"PERF PortWaitForMapActHeader {actName}: timeout after {scans} scans in {perf.ElapsedMilliseconds}ms, final={PortDisplayLocation(finalAct)}");
            return false;
        }

        private string PortDetectMapActHeader(bool logPerf = false)
        {
            Stopwatch perf = Stopwatch.StartNew();
            (string Act, string File)[] templates =
            [
                ("Act 1", "Act 1 Map Header.png"),
                ("Act 2", "Act 2.png"),
                ("Act 3", "Act 3.png"),
                ("Act 4", "Act 4.png"),
                ("Act 5", "Act 5.png"),
            ];

            string bestAct = "";
            double bestConfidence = 0;

            foreach ((string act, string file) in templates)
            {
                double confidence = PortBestTemplateConfidenceInDiabloRegion(Img("Teleport Function", file), PortMapHeaderRegion());
                if (confidence > bestConfidence)
                {
                    bestAct = act;
                    bestConfidence = confidence;
                }
            }

            string detected = bestConfidence >= PortMapActHeaderConfidence ? bestAct : "";
            if (logPerf)
            {
                AppLogger.Info($"PERF PortDetectMapActHeader: scanned {templates.Length} templates in {perf.ElapsedMilliseconds}ms, best={PortDisplayLocation(bestAct)} confidence={bestConfidence:0.000}, detected={PortDisplayLocation(detected)}");
            }
            return detected;
        }

        private bool PortIsInGame()
        {
            return !string.IsNullOrWhiteSpace(PortDetectCurrentLocation());
        }

        private bool PortGameLoadedLocationTitleVisible()
        {
            if (!PortTryGetDiabloRect(out RECT rect))
            {
                return false;
            }

            Rectangle titleRegion = PortScaleReferenceRectangle(new Rectangle(
                (int)Math.Round(PortReferenceWidth * 0.82),
                0,
                (int)Math.Round(PortReferenceWidth * 0.18),
                (int)Math.Round(PortReferenceHeight * 0.04)),
                rect);

            if (titleRegion.Width <= 0 || titleRegion.Height <= 0)
            {
                return false;
            }

            using Bitmap screenshot = new(titleRegion.Width, titleRegion.Height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(titleRegion.Left, titleRegion.Top, 0, 0, screenshot.Size);
            }

            using Mat raw = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
            using Mat bgr = new();
            using Mat mask = new();
            Cv2.CvtColor(raw, bgr, ColorConversionCodes.BGRA2BGR);
            Cv2.InRange(bgr, new Scalar(185, 185, 185), new Scalar(255, 255, 255), mask);
            return Cv2.CountNonZero(mask) > 8;
        }

        private string PortDetectCurrentLocation()
        {
            return PortDetectCurrentLocationFromTemplates(portCurrentLocationTemplates, "full current-location detection", logPerf: true);
        }

        private string PortDetectSpecificLocation(string locationName)
        {
            Stopwatch perf = Stopwatch.StartNew();
            Dictionary<string, string> targetTemplates = PortCurrentLocationTemplatesForTarget(locationName, fallbackToAll: false);
            double threshold = PortLocationConfidenceForTarget(locationName);
            PortLocationDetectionResult result = PortDetectCurrentLocationFromTemplatesDetailed(targetTemplates, $"specific location: {locationName}", logPerf: false, threshold);
            string detected = result.Detected;
            bool matched = PortLocationMatches(detected, locationName);
            AppLogger.Info($"PERF PortDetectSpecificLocation {locationName}: {(matched ? "matched" : "not matched")} {PortDisplayLocation(detected)} with {targetTemplates.Count} templates in {perf.ElapsedMilliseconds}ms; best={PortDisplayLocation(result.BestName)} confidence={result.BestConfidence:0.000}, second={PortDisplayLocation(result.SecondName)} confidence={result.SecondConfidence:0.000}, threshold={threshold:0.000}");
            return matched ? detected : "";
        }

        private bool PortWaitForCurrentLocation(string targetLocation, CancellationToken token, int timeoutMs)
        {
            return PortWaitForSpecificLocation(targetLocation, token, timeoutMs);
        }

        private bool PortWaitForSpecificLocation(string targetLocation, CancellationToken token, int timeoutMs)
        {
            Stopwatch perf = Stopwatch.StartNew();
            int scans = 0;
            Dictionary<string, string> targetTemplates = PortCurrentLocationTemplatesForTarget(targetLocation);
            double threshold = PortLocationConfidenceForTarget(targetLocation);
            PortLocationDetectionResult lastResult = new("", "", 0, "", 0, targetTemplates.Count, 0);
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    AppLogger.Info($"PERF PortWaitForSpecificLocation {targetLocation}: cancelled after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return false;
                }

                scans++;
                lastResult = PortDetectCurrentLocationFromTemplatesDetailed(targetTemplates, $"specific wait: {targetLocation}", logPerf: false, threshold);
                string detectedLocation = lastResult.Detected;
                if (PortLocationMatches(detectedLocation, targetLocation))
                {
                    AppLogger.Info($"PERF PortWaitForSpecificLocation {targetLocation}: matched {detectedLocation} with {targetTemplates.Count} templates after {scans} scans in {perf.ElapsedMilliseconds}ms; best={PortDisplayLocation(lastResult.BestName)} confidence={lastResult.BestConfidence:0.000}, second={PortDisplayLocation(lastResult.SecondName)} confidence={lastResult.SecondConfidence:0.000}, threshold={threshold:0.000}");
                    return true;
                }

                PortSleep(token, 250);
            }

            AppLogger.Info($"PERF PortWaitForSpecificLocation {targetLocation}: timeout with {targetTemplates.Count} templates after {scans} scans in {perf.ElapsedMilliseconds}ms; best={PortDisplayLocation(lastResult.BestName)} confidence={lastResult.BestConfidence:0.000}, second={PortDisplayLocation(lastResult.SecondName)} confidence={lastResult.SecondConfidence:0.000}, threshold={threshold:0.000}");
            return false;
        }

        private string PortDetectBlockedTeleportLocation()
        {
            Dictionary<string, string> blockedTemplates = PortCurrentLocationTemplatesForNames(portTeleportBlockedLocations);
            return PortDetectCurrentLocationFromTemplatesDetailed(blockedTemplates, "blocked teleport location detection", logPerf: true, PortBlockedLocationConfidence).Detected;
        }

        private void PortShowSplash(string message, int durationMs)
        {
            RunOnUiThread(() =>
            {
                try
                {
                    if (portSplashForm == null || portSplashForm.IsDisposed)
                    {
                        portSplashForm = new Form
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
                    AppLogger.Info($"Teleport blocked splash shown: {message}");
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

        private Dictionary<string, string> PortCurrentLocationTemplatesForTarget(string targetLocation, bool fallbackToAll = true)
        {
            Dictionary<string, string> templates = PortCurrentLocationTemplatesForNames(PortLocationNamesForTarget(targetLocation));

            return templates.Count > 0 || !fallbackToAll ? templates : portCurrentLocationTemplates;
        }

        private Dictionary<string, string> PortCurrentLocationTemplatesForNames(IEnumerable<string> locationNames)
        {
            Dictionary<string, string> templates = new(StringComparer.OrdinalIgnoreCase);
            foreach (string locationName in locationNames)
            {
                string key = PortNormalizeLocation(locationName);
                if (portCurrentLocationTemplates.TryGetValue(key, out string? imagePath))
                {
                    templates[key] = imagePath;
                }
            }

            return templates;
        }

        private IEnumerable<string> PortLocationNamesForTarget(string targetLocation)
        {
            yield return targetLocation;

            string key = PortLocationKey(targetLocation);
            if (portArrivalAliases.TryGetValue(key, out string[]? aliases))
            {
                foreach (string alias in aliases)
                {
                    yield return alias;
                }
            }
        }

        private string PortDetectCurrentLocationFromTemplates(IReadOnlyDictionary<string, string> templates, string label, bool logPerf)
        {
            return PortDetectCurrentLocationFromTemplatesDetailed(templates, label, logPerf, PortCurrentLocationConfidence).Detected;
        }

        private PortLocationDetectionResult PortDetectCurrentLocationFromTemplatesDetailed(IReadOnlyDictionary<string, string> templates, string label, bool logPerf, double threshold)
        {
            Stopwatch perf = Stopwatch.StartNew();
            string bestName = "";
            double bestConfidence = 0;
            string secondName = "";
            double secondConfidence = 0;

            if (templates.Count == 0)
            {
                if (logPerf)
                {
                    AppLogger.Info($"PERF PortDetectCurrentLocation ({label}): 0 templates scanned in 0ms");
                }
                return new("", "", 0, "", 0, 0, perf.ElapsedMilliseconds);
            }

            if (!PortTryGetDiabloRect(out _))
            {
                if (logPerf)
                {
                    AppLogger.Info($"PERF PortDetectCurrentLocation ({label}): window missing, {templates.Count} templates available in {perf.ElapsedMilliseconds}ms");
                }
                return new("", "", 0, "", 0, templates.Count, perf.ElapsedMilliseconds);
            }

            using Bitmap screenshot = PortCaptureDiabloRegion(PortCurrentLocationTitleRegion());
            if (screenshot.Width <= 1 || screenshot.Height <= 1)
            {
                if (logPerf)
                {
                    AppLogger.Info($"PERF PortDetectCurrentLocation ({label}): title-region capture failed, {templates.Count} templates available in {perf.ElapsedMilliseconds}ms");
                }
                return new("", "", 0, "", 0, templates.Count, perf.ElapsedMilliseconds);
            }

            using Mat rawScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
            using Mat screenMat = new();
            Cv2.CvtColor(rawScreenMat, screenMat, ColorConversionCodes.BGRA2BGR);

            foreach ((string name, string imagePath) in templates)
            {
                double confidence = PortLocationKey(name).StartsWith("pandemonium fortress level", StringComparison.Ordinal)
                    ? PortBestPandemoniumTemplateConfidenceInMat(screenMat, imagePath)
                    : PortBestTemplateConfidenceInMat(screenMat, imagePath);
                if (confidence > bestConfidence)
                {
                    secondName = bestName;
                    secondConfidence = bestConfidence;
                    bestName = name;
                    bestConfidence = confidence;
                }
                else if (confidence > secondConfidence)
                {
                    secondName = name;
                    secondConfidence = confidence;
                }
            }

            string detected = PortResolveDetectedLocation(bestName, bestConfidence, secondName, secondConfidence, threshold);
            if (logPerf)
            {
                AppLogger.Info($"PERF PortDetectCurrentLocation ({label}): scanned {templates.Count} templates in title region in {perf.ElapsedMilliseconds}ms, best={PortDisplayLocation(bestName)} confidence={bestConfidence:0.000}, second={PortDisplayLocation(secondName)} confidence={secondConfidence:0.000}, threshold={threshold:0.000}, detected={PortDisplayLocation(detected)}");
            }
            return new(detected, bestName, bestConfidence, secondName, secondConfidence, templates.Count, perf.ElapsedMilliseconds);
        }

        private string PortResolveDetectedLocation(string bestName, double bestConfidence, string secondName, double secondConfidence, double threshold)
        {
            if (bestConfidence < threshold)
            {
                return "";
            }

            string bestKey = PortLocationKey(bestName);
            string secondKey = PortLocationKey(secondName);
            bool bestIsPandemonium = PortIsPandemoniumLocation(bestName);
            bool secondIsPandemonium = PortIsPandemoniumLocation(secondName);

            if (bestIsPandemonium && secondIsPandemonium && bestKey != secondKey && bestConfidence - secondConfidence < 0.025)
            {
                AppLogger.Info($"Pandemonium location detection ambiguous: best={bestName} {bestConfidence:0.000}, second={secondName} {secondConfidence:0.000}");
                return "";
            }

            return PortNormalizeLocation(bestName);
        }

        private double PortLocationConfidenceForTarget(string targetLocation)
        {
            string key = PortLocationKey(targetLocation);
            if (key == PortLocationKey("City Of Caldeum") || key == PortLocationKey("Ancient Waterway"))
            {
                return 0.68;
            }

            if (key == PortLocationKey("Pandemonium Fortress Level 1") || key == PortLocationKey("Pandemonium Fortress Level 2"))
            {
                return 0.72;
            }

            return PortCurrentLocationConfidence;
        }

        private bool PortIsPandemoniumLocation(string location)
        {
            string key = PortLocationKey(location);
            return key == PortLocationKey("Pandemonium Fortress Level 1") ||
                key == PortLocationKey("Pandemonium Fortress Level 2");
        }

        private static double PortBestTemplateConfidenceInMat(Mat screenMat, string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                return 0;
            }

            using Mat templateMat = Cv2.ImRead(imagePath, ImreadModes.Color);
            if (templateMat.Empty() || templateMat.Width > screenMat.Width || templateMat.Height > screenMat.Height)
            {
                return 0;
            }

            using Mat result = new();
            Cv2.MatchTemplate(screenMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
            return maxVal;
        }

        private static double PortBestPandemoniumTemplateConfidenceInMat(Mat screenMat, string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                return 0;
            }

            using Mat templateMat = Cv2.ImRead(imagePath, ImreadModes.Color);
            if (templateMat.Empty() || templateMat.Width > screenMat.Width || templateMat.Height > screenMat.Height)
            {
                return 0;
            }

            using Mat result = new();
            Cv2.MatchTemplate(screenMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double fullScore, out _, out OpenCvSharp.Point maxLoc);

            int suffixWidth = Math.Min(44, templateMat.Width);
            int suffixX = templateMat.Width - suffixWidth;
            int screenX = maxLoc.X + suffixX;
            if (screenX < 0 || screenX + suffixWidth > screenMat.Width || maxLoc.Y < 0 || maxLoc.Y + templateMat.Height > screenMat.Height)
            {
                return fullScore;
            }

            using Mat templateSuffix = new(templateMat, new OpenCvSharp.Rect(suffixX, 0, suffixWidth, templateMat.Height));
            using Mat screenSuffix = new(screenMat, new OpenCvSharp.Rect(screenX, maxLoc.Y, suffixWidth, templateMat.Height));
            using Mat suffixResult = new();
            Cv2.MatchTemplate(screenSuffix, templateSuffix, suffixResult, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(suffixResult, out _, out double suffixScore, out _, out _);
            return suffixScore;
        }

        private double PortBestTemplateConfidenceInDiablo(string imagePath)
        {
            Stopwatch perf = Stopwatch.StartNew();
            IntPtr diabloWindow = FindDiabloWindow();
            if (diabloWindow == IntPtr.Zero || !File.Exists(imagePath))
            {
                AppLogger.Info($"PERF PortBestTemplateConfidenceInDiablo {Path.GetFileName(imagePath)}: unavailable in {perf.ElapsedMilliseconds}ms");
                return 0;
            }

            Rectangle? referenceRegion = PortScanRegionForImage(imagePath);
            Bitmap? screenshot = referenceRegion.HasValue
                ? PortCaptureDiabloRegion(referenceRegion.Value)
                : CaptureWindow(diabloWindow);
            if (screenshot == null)
            {
                AppLogger.Info($"PERF PortBestTemplateConfidenceInDiablo {Path.GetFileName(imagePath)}: capture failed in {perf.ElapsedMilliseconds}ms");
                return 0;
            }

            using (screenshot)
            {
                using Mat rawScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
                using Mat screenMat = new();
                Cv2.CvtColor(rawScreenMat, screenMat, ColorConversionCodes.BGRA2BGR);

                using Mat templateMat = Cv2.ImRead(imagePath, ImreadModes.Color);
                if (templateMat.Empty() || templateMat.Width > screenMat.Width || templateMat.Height > screenMat.Height)
                {
                    return 0;
                }

                using Mat result = new();
                Cv2.MatchTemplate(screenMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
                AppLogger.Info($"PERF PortBestTemplateConfidenceInDiablo {Path.GetFileName(imagePath)}: confidence={maxVal:0.000} in {perf.ElapsedMilliseconds}ms");
                return maxVal;
            }
        }

        private Bitmap PortCaptureDiabloRegion(Rectangle referenceRegion)
        {
            if (!PortTryGetDiabloRect(out RECT rect))
            {
                return new Bitmap(1, 1);
            }

            Rectangle screenRegion = PortScaleReferenceRectangle(referenceRegion, rect);
            screenRegion = Rectangle.Intersect(SystemInformation.VirtualScreen, screenRegion);

            if (screenRegion.Width <= 0 || screenRegion.Height <= 0)
            {
                return new Bitmap(1, 1);
            }

            Bitmap screenshot = new(screenRegion.Width, screenRegion.Height);

            using Graphics graphics = Graphics.FromImage(screenshot);
            graphics.CopyFromScreen(screenRegion.Left, screenRegion.Top, 0, 0, screenshot.Size);

            return screenshot;
        }

        private double PortBestTemplateConfidenceInDiabloRegion(string imagePath, Rectangle referenceRegion)
        {
            if (!PortTryGetDiabloRect(out RECT rect) || !File.Exists(imagePath))
            {
                return 0;
            }

            Rectangle screenRegion = PortScaleReferenceRectangle(referenceRegion, rect);
            screenRegion = Rectangle.Intersect(SystemInformation.VirtualScreen, screenRegion);
            if (screenRegion.Width <= 0 || screenRegion.Height <= 0)
            {
                return 0;
            }

            using Bitmap screenshot = new(screenRegion.Width, screenRegion.Height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(screenRegion.Left, screenRegion.Top, 0, 0, screenshot.Size);
            }

            using Mat rawScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
            using Mat screenMat = new();
            Cv2.CvtColor(rawScreenMat, screenMat, ColorConversionCodes.BGRA2BGR);

            using Mat templateMat = Cv2.ImRead(imagePath, ImreadModes.Color);
            if (templateMat.Empty() || templateMat.Width > screenMat.Width || templateMat.Height > screenMat.Height)
            {
                return 0;
            }

            using Mat result = new();
            Cv2.MatchTemplate(screenMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
            return maxVal;
        }

        private DrawingPoint? PortFirstFilledInventorySlot()
        {
            if (!PortTryGetDiabloRect(out RECT rect))
            {
                return null;
            }

            Rectangle grid = PortScaleReferenceRectangle(new Rectangle(1864, 725, 687, 423), rect);
            int columns = 10;
            int rows = 6;
            int slotWidth = grid.Width / columns;
            int slotHeight = grid.Height / rows;
            string blankPath = Img("Salvage", "Blank Inventory Tile.png");

            using Bitmap screenshot = new(grid.Width, grid.Height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(grid.Left, grid.Top, 0, 0, screenshot.Size);
            }

            using Mat rawMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
            using Mat gray = new();
            Cv2.CvtColor(rawMat, gray, ColorConversionCodes.BGRA2GRAY);
            using Mat? blank = File.Exists(blankPath) ? Cv2.ImRead(blankPath, ImreadModes.Grayscale) : null;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    Rectangle local = new(col * slotWidth, row * slotHeight, slotWidth, slotHeight);
                    using Mat slot = new(gray, new OpenCvSharp.Rect(local.Left, local.Top, local.Width, local.Height));
                    Cv2.MeanStdDev(slot, out Scalar mean, out Scalar stdDev);

                    bool blankLike = mean.Val0 < 18.0 && stdDev.Val0 < 10.0;
                    if (!blankLike && blank != null)
                    {
                        using Mat resized = new();
                        using Mat result = new();
                        Cv2.Resize(blank, resized, new OpenCvSharp.Size(slot.Width, slot.Height));
                        Cv2.MatchTemplate(slot, resized, result, TemplateMatchModes.CCoeffNormed);
                        Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
                        blankLike = maxVal >= PortBlankInventoryTileConfidence;
                    }

                    if (!blankLike)
                    {
                        return new DrawingPoint(grid.Left + local.Left + slotWidth / 2, grid.Top + local.Top + slotHeight / 2);
                    }
                }
            }

            return null;
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

            string currentLocationFolder = Path.Combine(ImagesPath, "Current Location");
            string teleportFunctionFolder = Path.Combine(ImagesPath, "Teleport Function");
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
            return key == PortLocationKey("Ancient Waterway") || PortIsPandemoniumLocation(location);
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

        private PortScanRegionManager PortScanRegions => portScanRegionManager ??= PortCreateScanRegionManager();

        private PortScanRegionManager PortCreateScanRegionManager()
        {
            Dictionary<string, Rectangle> hardCodedRegions = new(StringComparer.OrdinalIgnoreCase)
            {
                ["CurrentLocation"] = PortReferenceRegion(2050, 0, 500, 42),
                ["MapHeader"] = PortReferenceRegion(970, 55, 620, 110),
                ["CharacterLoad"] = PortReferenceRegion(600, 1200, 1200, 220),
                ["WitchDoctorHex"] = PortReferenceRegion(842, 1336, 73, 73),
                ["MomentumStack"] = PortReferenceRegion(
                    (int)(PortReferenceWidth * 0.325),
                    (int)(PortReferenceHeight * 0.835),
                    (int)(PortReferenceWidth * 0.354),
                    (int)(PortReferenceHeight * 0.072)),
            };

            return new PortScanRegionManager(
                ImagesPath,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ScanRegions.json"),
                hardCodedRegions);
        }

        private Rectangle PortScanRegion(string key, string? imagePath = null)
        {
            return PortScanRegions.GetRegion(key, imagePath) ?? PortReferenceRegion(0, 0, PortReferenceWidth, PortReferenceHeight);
        }

        private Rectangle? PortScanRegionForImage(string imagePath)
        {
            return PortScanRegions.GetRegion(PortScanRegionManager.KeyFromImagePath(imagePath), imagePath);
        }

        private Rectangle PortCurrentLocationTitleRegion()
        {
            return PortScanRegion("CurrentLocation", Img("Current Location", "Current Location Scan Region.png"));
        }

        private Rectangle PortMapHeaderRegion()
        {
            return PortScanRegion("MapHeader", Img("Teleport Function", "Map Scan Region.jpg"));
        }

        private void PortReleaseInputs()
        {
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
            keybd_event(PortVkShift, 0, PortKeyUp, UIntPtr.Zero);
        }

        private void PortPressKey(int vk)
        {
            keybd_event((byte)vk, 0, 0, UIntPtr.Zero);
            Thread.Sleep(10);
            keybd_event((byte)vk, 0, PortKeyUp, UIntPtr.Zero);
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
            return false;
        }

        private sealed class PortScanRegionManager
        {
            private readonly string imagesRoot;
            private readonly string cachePath;
            private readonly Dictionary<string, Rectangle> hardCodedRegions;
            private readonly Dictionary<string, PortScanRegionEntry> discoveredRegions = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> missingRegions = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> loggedRegions = new(StringComparer.OrdinalIgnoreCase);
            private bool cacheLoaded;

            public PortScanRegionManager(string imagesRoot, string cachePath, Dictionary<string, Rectangle> hardCodedRegions)
            {
                this.imagesRoot = imagesRoot;
                this.cachePath = cachePath;
                this.hardCodedRegions = new(hardCodedRegions, StringComparer.OrdinalIgnoreCase);
                AppLogger.Info($"ScanRegion manager initialized: root={imagesRoot}; cache={cachePath}");
            }

            public Rectangle? GetRegion(string key, string? imagePath = null)
            {
                key = NormalizeKey(key);
                LoadCache();

                if (hardCodedRegions.TryGetValue(key, out Rectangle hardCoded))
                {
                    LogOnce($"hardcoded:{key}", $"ScanRegion loaded hard-coded: {key}={FormatRegion(hardCoded)}");
                    return hardCoded;
                }

                if (discoveredRegions.TryGetValue(key, out PortScanRegionEntry cached))
                {
                    LogOnce($"cache:{key}", $"ScanRegion loaded from cache: {key} image={cached.Image} region={cached.Region}");
                    return cached.ToRectangle();
                }

                if (missingRegions.Contains(key))
                {
                    return null;
                }

                if (TryDiscoverRegion(key, imagePath, out PortScanRegionEntry discovered))
                {
                    discoveredRegions[key] = discovered;
                    AppLogger.Info($"ScanRegion discovered: {key} image={discovered.Image} region={discovered.Region}");
                    SaveCache();
                    AppLogger.Info($"ScanRegion saved: {key}");
                    return discovered.ToRectangle();
                }

                missingRegions.Add(key);
                AppLogger.Info($"ScanRegion missing: {key}; falling back to full-window scan");
                return null;
            }

            public static string KeyFromImagePath(string imagePath)
            {
                return NormalizeKey(Path.GetFileNameWithoutExtension(imagePath));
            }

            private bool TryDiscoverRegion(string key, string? imagePath, out PortScanRegionEntry entry)
            {
                entry = default;
                if (!Directory.Exists(imagesRoot))
                {
                    AppLogger.Info($"ScanRegion missing image root: {imagesRoot}");
                    return false;
                }

                string? scanImage = Directory.EnumerateFiles(imagesRoot, "*.*", SearchOption.AllDirectories)
                    .Where(IsScanRegionImage)
                    .Select(path => new { Path = path, Score = MatchScore(key, path) })
                    .Where(item => item.Score > 0)
                    .OrderByDescending(item => item.Score)
                    .ThenBy(item => item.Path.Length)
                    .Select(item => item.Path)
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(scanImage))
                {
                    return false;
                }

                if (!TryDeriveRegion(scanImage, imagePath, out Rectangle region))
                {
                    AppLogger.Info($"ScanRegion derive failed: {key} image={scanImage}");
                    return false;
                }

                entry = new PortScanRegionEntry(Path.GetFileName(scanImage), FormatRegion(region));
                return true;
            }

            private static bool IsScanRegionImage(string path)
            {
                string file = Path.GetFileName(path);
                string key = NormalizeKey(file);
                return key.Contains("scanregion", StringComparison.OrdinalIgnoreCase) &&
                    (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                     file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                     file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));
            }

            private static int MatchScore(string key, string path)
            {
                string scanKey = NormalizeKey(Path.GetFileNameWithoutExtension(path)).Replace("scanregion", "", StringComparison.OrdinalIgnoreCase);
                if (string.IsNullOrWhiteSpace(scanKey))
                {
                    return 0;
                }

                if (scanKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return 1000;
                }

                if (scanKey.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    return 700;
                }

                if (key.Contains(scanKey, StringComparison.OrdinalIgnoreCase))
                {
                    return 500;
                }

                string[] keyParts = SplitKey(key);
                int overlap = keyParts.Count(part => scanKey.Contains(part, StringComparison.OrdinalIgnoreCase));
                return overlap >= Math.Min(2, keyParts.Length) ? overlap * 100 : 0;
            }

            private static string[] SplitKey(string key)
            {
                return Regex.Matches(key, "[A-Z]?[a-z]+|[0-9]+")
                    .Select(match => NormalizeKey(match.Value))
                    .Where(part => part.Length > 2)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            private static bool TryDeriveRegion(string scanImagePath, string? templateImagePath, out Rectangle region)
            {
                region = Rectangle.Empty;
                using Mat image = Cv2.ImRead(scanImagePath, ImreadModes.Color);
                if (image.Empty())
                {
                    return false;
                }

                using Mat hsv = new();
                Cv2.CvtColor(image, hsv, ColorConversionCodes.BGR2HSV);

                using Mat red1 = new();
                using Mat red2 = new();
                using Mat green = new();
                using Mat blue = new();
                using Mat mask = new();
                Cv2.InRange(hsv, new Scalar(0, 80, 120), new Scalar(10, 255, 255), red1);
                Cv2.InRange(hsv, new Scalar(170, 80, 120), new Scalar(179, 255, 255), red2);
                Cv2.InRange(hsv, new Scalar(35, 80, 120), new Scalar(95, 255, 255), green);
                Cv2.InRange(hsv, new Scalar(95, 80, 120), new Scalar(135, 255, 255), blue);
                Cv2.BitwiseOr(red1, red2, mask);
                Cv2.BitwiseOr(mask, green, mask);
                Cv2.BitwiseOr(mask, blue, mask);

                Cv2.FindContours(mask, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                List<Rectangle> candidates = [];
                foreach (OpenCvSharp.Point[] contour in contours)
                {
                    OpenCvSharp.Rect rect = Cv2.BoundingRect(contour);
                    if (rect.Width < 20 || rect.Height < 10)
                    {
                        continue;
                    }

                    using Mat roi = new(mask, rect);
                    double fillRatio = Cv2.CountNonZero(roi) / (double)(rect.Width * rect.Height);
                    if (fillRatio > 0.45)
                    {
                        continue;
                    }

                    candidates.Add(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height));
                }

                if (candidates.Count == 0)
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(templateImagePath) && File.Exists(templateImagePath))
                {
                    using Mat template = Cv2.ImRead(templateImagePath, ImreadModes.Color);
                    if (!template.Empty())
                    {
                        Rectangle? best = null;
                        double bestScore = 0;
                        foreach (Rectangle candidate in candidates)
                        {
                            Rectangle inflated = InflateWithin(candidate, image.Width, image.Height, 4);
                            if (template.Width > inflated.Width || template.Height > inflated.Height)
                            {
                                continue;
                            }

                            using Mat crop = new(image, new OpenCvSharp.Rect(inflated.X, inflated.Y, inflated.Width, inflated.Height));
                            using Mat result = new();
                            Cv2.MatchTemplate(crop, template, result, TemplateMatchModes.CCoeffNormed);
                            Cv2.MinMaxLoc(result, out _, out double score, out _, out _);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                best = inflated;
                            }
                        }

                        if (best.HasValue && bestScore >= 0.45)
                        {
                            region = best.Value;
                            return true;
                        }
                    }
                }

                region = candidates
                    .OrderByDescending(candidate => candidate.Width * candidate.Height)
                    .First();
                return true;
            }

            private static Rectangle InflateWithin(Rectangle rectangle, int maxWidth, int maxHeight, int padding)
            {
                int left = Math.Max(0, rectangle.Left - padding);
                int top = Math.Max(0, rectangle.Top - padding);
                int right = Math.Min(maxWidth, rectangle.Right + padding);
                int bottom = Math.Min(maxHeight, rectangle.Bottom + padding);
                return Rectangle.FromLTRB(left, top, right, bottom);
            }

            private void LoadCache()
            {
                if (cacheLoaded)
                {
                    return;
                }

                cacheLoaded = true;
                if (!File.Exists(cachePath))
                {
                    return;
                }

                try
                {
                    Dictionary<string, PortScanRegionEntry>? loaded = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, PortScanRegionEntry>>(File.ReadAllText(cachePath));
                    if (loaded == null)
                    {
                        return;
                    }

                    foreach ((string key, PortScanRegionEntry entry) in loaded)
                    {
                        if (entry.ToRectangle().Width > 0 && entry.ToRectangle().Height > 0)
                        {
                            discoveredRegions[NormalizeKey(key)] = entry;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error("ScanRegion cache load failed.", ex);
                }
            }

            private void SaveCache()
            {
                try
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(discoveredRegions, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(cachePath, json);
                }
                catch (Exception ex)
                {
                    AppLogger.Error("ScanRegion cache save failed.", ex);
                }
            }

            private void LogOnce(string key, string message)
            {
                if (loggedRegions.Add(key))
                {
                    AppLogger.Info(message);
                }
            }

            private static string FormatRegion(Rectangle region)
            {
                return $"{region.X},{region.Y},{region.Width},{region.Height}";
            }

            private static string NormalizeKey(string value)
            {
                return Regex.Replace(value ?? "", "[^a-zA-Z0-9]+", "");
            }

            private readonly record struct PortScanRegionEntry(string Image, string Region)
            {
                public Rectangle ToRectangle()
                {
                    string[] parts = Region.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 4 ||
                        !int.TryParse(parts[0], out int x) ||
                        !int.TryParse(parts[1], out int y) ||
                        !int.TryParse(parts[2], out int width) ||
                        !int.TryParse(parts[3], out int height))
                    {
                        return Rectangle.Empty;
                    }

                    return new Rectangle(x, y, width, height);
                }
            }
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

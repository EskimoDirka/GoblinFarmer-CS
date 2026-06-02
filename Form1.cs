using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenCvSharp;
using DrawingPoint = System.Drawing.Point;
using CvPoint = OpenCvSharp.Point;

namespace GoblinFarmer
{
    public partial class frmMain : Form
    {
        // State variable to prevent overlapping automation runs
        private bool isAutomationRunning = false;

        private enum ImageMatchMode
        {
            Default,
            Grayscale,
            Color,
        }

        private const int SW_RESTORE = 9;

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            PortInitializeSessionStats();
            PortInitializeDiagnosticOverlay();
            tmrStatus.Start();

            SetDiabloStatus("Unknown");
            SetCombatStatus("Idle");
            SetAppStatus("Idle");
        }

        // Windows API imports =========================================
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref DrawingPoint lpPoint);

        // Used to activate the Diablo window when found
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // Mouse-click support
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Timer event to check Diablo status every second =========================
        private void tmrStatus_Tick(object sender, EventArgs e)
        {
            IntPtr diabloWindow = FindDiabloWindow();
            bool diabloRunning = diabloWindow != IntPtr.Zero;

            SetDiabloStatus(diabloRunning ? "Running" : "Not Running");
            PortUpdateDiabloRuntimeMonitor(diabloRunning);
            PortUpdateSessionStats();
            PortUpdateDiagnosticOverlay(diabloRunning);
        }

        private void PortUpdateDiabloRuntimeMonitor(bool diabloRunning)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            bool inLaunchGrace = nowTicks < Interlocked.Read(ref portLaunchGraceUntilTicks);

            if (diabloRunning)
            {
                portConsecutiveDiabloMissingChecks = 0;
                if (Interlocked.Read(ref portLaunchGraceUntilTicks) > 0 && !portLaunchGraceStableLogged)
                {
                    portLaunchGraceStableLogged = true;
                    AppLogger.Info("Diablo process confirmed stable");
                }

                portDiabloWasRunning = true;
                return;
            }

            if (inLaunchGrace)
            {
                long lastLogTicks = Interlocked.Read(ref portLastLaunchGraceMissingLogTicks);
                if (nowTicks - lastLogTicks >= TimeSpan.FromSeconds(2).Ticks)
                {
                    Interlocked.Exchange(ref portLastLaunchGraceMissingLogTicks, nowTicks);
                    AppLogger.Info("Diablo missing during launch grace period ignored");
                }

                portConsecutiveDiabloMissingChecks = 0;
                return;
            }

            if (portBattleNetLaunchFlowActive)
            {
                long lastLogTicks = Interlocked.Read(ref portLastLaunchFlowMissingLogTicks);
                if (nowTicks - lastLogTicks >= TimeSpan.FromSeconds(2).Ticks)
                {
                    Interlocked.Exchange(ref portLastLaunchFlowMissingLogTicks, nowTicks);
                    AppLogger.Info("Diablo missing during Battle.net launch/start-game flow ignored");
                }

                portConsecutiveDiabloMissingChecks = 0;
                return;
            }

            if (!portDiabloWasRunning && !isAutomationRunning)
            {
                portConsecutiveDiabloMissingChecks = 0;
                return;
            }

            portConsecutiveDiabloMissingChecks++;
            if (portConsecutiveDiabloMissingChecks < PortDiabloMissingExitThreshold)
            {
                return;
            }

            AppLogger.Info($"Diablo exited after grace period: consecutiveMissingChecks={portConsecutiveDiabloMissingChecks}");
            portDiabloWasRunning = false;
            portConsecutiveDiabloMissingChecks = 0;
            PortHandleDiabloExited();
        }

        // Method to find the Diablo window handle =========================================
        private IntPtr FindDiabloWindow()
        {
            IntPtr foundWindow = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                {
                    return true;
                }

                GetWindowThreadProcessId(hWnd, out uint processId);

                try
                {
                    Process process = Process.GetProcessById((int)processId);

                    if (process.ProcessName.Equals("Diablo III", StringComparison.OrdinalIgnoreCase) ||
                        process.ProcessName.Equals("Diablo III64", StringComparison.OrdinalIgnoreCase))
                    {
                        foundWindow = hWnd;
                        return false;
                    }
                }
                catch
                {
                    // Ignore processes that can't be accessed.
                }

                return true;
            }, IntPtr.Zero);

            return foundWindow;
        }

        private IntPtr FindBattleNetWindow()
        {
            IntPtr foundWindow = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                {
                    return true;
                }

                GetWindowThreadProcessId(hWnd, out uint processId);

                try
                {
                    Process process = Process.GetProcessById((int)processId);

                    if (process.ProcessName.Equals("Battle.net", StringComparison.OrdinalIgnoreCase))
                    {
                        foundWindow = hWnd;
                        return false;
                    }
                }
                catch
                {
                    // Ignore processes that can't be accessed.
                }

                return true;
            }, IntPtr.Zero);

            return foundWindow;
        }

        // Button click event to activate Diablo window =========================================
        private bool ActivateDiabloWindow()
        {
            IntPtr diabloWindow = FindDiabloWindow();

            if (diabloWindow == IntPtr.Zero)
            {
                MessageBox.Show("Diablo III window not found.");
                SetAppStatus("Diablo Not Found");
                return false;
            }

            SetForegroundWindow(diabloWindow);
            return true;
        }

        // Methods =========================================

        private bool IsDiabloRunning()
        {
            return FindDiabloWindow() != IntPtr.Zero;
        }

        // Main method to start Diablo III =========================================
        private bool StartDiablo()
        {
            portBattleNetLaunchFlowActive = true;
            Interlocked.Exchange(ref portLastLaunchFlowMissingLogTicks, 0);

            try
            {
                AddWorkflowStep("Starting Battle.net");
                StartBattleNet();
                if (!PrepareBattleNetForDiabloLaunch())
                {
                    SetAppStatus("Battle.net Setup Failed");
                    return false;
                }

                AddWorkflowStep("Waiting for Battle.net Play Button");
                SetAppStatus("Waiting For Battle.net Play Button");

                bool clickedPlay = WaitForBattleNetPlayButtonAndClick(
                    Img("Start Game", "Battle Net Play Button.png"),
                    timeoutMs: 60000,
                    confidence: 0.85);

                if (!clickedPlay)
                {
                    MessageBox.Show("Could not find Battle.net Play button.");
                    SetAppStatus("Play Button Not Found");
                    return false;
                }

                AddWorkflowStep("Clicking Play button");
                Thread.Sleep(2000);
                CloseBattleNet();

                SetAppStatus("Launching Diablo III");

                Stopwatch sw = Stopwatch.StartNew();
                AddWorkflowStep("Waiting for Diablo process");

                while (sw.ElapsedMilliseconds < 120000)
                {
                    if (IsDiabloRunning())
                    {
                        AppLogger.Info($"Diablo process detected after {sw.ElapsedMilliseconds}ms");
                        SetAppStatus("Diablo III Started");
                        return true;
                    }

                    Thread.Sleep(1000);
                }

                MessageBox.Show("Diablo III did not start within the timeout.");
                SetAppStatus("Diablo Start Timeout");
                return false;
            }
            finally
            {
                portBattleNetLaunchFlowActive = false;
            }
        }

        private void StartBattleNet()
        {
            if (IsBattleNetRunning())
            {
                AppLogger.Info("Battle.net process exists");
                IntPtr existingWindow = FindBattleNetWindow();
                if (existingWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(existingWindow);
                    AppLogger.Info($"Battle.net visible window found: hwnd=0x{existingWindow.ToInt64():X}");
                    AppLogger.Info($"Battle.net already running; focused existing window hwnd=0x{existingWindow.ToInt64():X}");
                }
                else
                {
                    AppLogger.Info("Battle.net process exists but no visible window found");
                }

                return;
            }

            LaunchBattleNetExecutable();
        }

        private bool LaunchBattleNetExecutable()
        {
            string battleNetPath = @"D:\Battle.net\Battle.net.exe";

            if (!File.Exists(battleNetPath))
            {
                MessageBox.Show("Battle.net not found.");
                AppLogger.Info($"Battle.net launch failed: executable not found at {battleNetPath}");
                return false;
            }

            AppLogger.Info($"launching Battle.net.exe: {battleNetPath}");
            Process.Start(battleNetPath);
            AppLogger.Info($"Battle.net launch requested: {battleNetPath}");
            return true;
        }

        private bool PrepareBattleNetForDiabloLaunch(CancellationToken token = default)
        {
            AddWorkflowStep("Focusing Battle.net");
            if (!WaitForBattleNetWindowAndFocus(token, timeoutMs: 1500, logFoundAfterLaunch: false))
            {
                if (IsBattleNetRunning())
                {
                    AppLogger.Info("Battle.net process exists but no visible window found");
                }

                if (!LaunchBattleNetExecutable())
                {
                    AppLogger.Info("Battle.net setup failed after retry");
                    return false;
                }

                if (!WaitForBattleNetWindowAndFocus(token, timeoutMs: 30000, logFoundAfterLaunch: true))
                {
                    AppLogger.Info("Battle.net setup failed after retry");
                    return false;
                }
            }

            if (token.IsCancellationRequested)
            {
                return false;
            }

            string playButtonPath = Img("Start Game", "Battle Net Play Button.png");

            Stopwatch playPrecheckWait = Stopwatch.StartNew();

            while (playPrecheckWait.ElapsedMilliseconds < 3000)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (TryFindBattleNetImage(
                    playButtonPath,
                    "BattleNetPlayButton",
                    "Battle.net Play button pre-check",
                    out DrawingPoint precheckPlayButtonCenter,
                    confidence: 0.85))
                {
                    AppLogger.Info($"Battle.net Play button visible during pre-check; skipping Diablo III tab selection; point={precheckPlayButtonCenter.X},{precheckPlayButtonCenter.Y}; elapsed={playPrecheckWait.ElapsedMilliseconds}ms");
                    return true;
                }

                PortSleepOrThreadSleep(token, 250);
            }

            AppLogger.Info($"Battle.net Play button not visible after pre-check wait; falling back to Diablo III tab selection; elapsed={playPrecheckWait.ElapsedMilliseconds}ms");

            AddWorkflowStep("Selecting Diablo III in Battle.net");
            if (!ClickBattleNetDiabloTab())
            {
                PortCaptureFailureScreenshot("DiabloTabNotFound");
                AppLogger.Info("Diablo III tab not clicked; continuing to Battle.net Play button search");
            }

            if (!PortSleepOrThreadSleep(token, 1200))
            {
                return false;
            }

            ConfirmBattleNetDiabloPage(token);
            return !token.IsCancellationRequested;
        }

        private bool WaitForBattleNetWindowAndFocus(CancellationToken token = default, int timeoutMs = 30000, bool logFoundAfterLaunch = false)
        {
            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                IntPtr battleNetWindow = FindBattleNetWindow();
                if (battleNetWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(battleNetWindow);
                    AppLogger.Info($"Battle.net visible window found: hwnd=0x{battleNetWindow.ToInt64():X}");
                    if (logFoundAfterLaunch)
                    {
                        AppLogger.Info($"Battle.net window found after launch: hwnd=0x{battleNetWindow.ToInt64():X}; elapsed={sw.ElapsedMilliseconds}ms");
                    }

                    AppLogger.Info($"Battle.net focused: hwnd=0x{battleNetWindow.ToInt64():X}; elapsed={sw.ElapsedMilliseconds}ms");
                    return true;
                }

                PortSleepOrThreadSleep(token, 250);
            }

            return false;
        }

        private bool ClickBattleNetDiabloTab()
        {
            AppLogger.Info("Diablo III tab scan started");
            bool foundTemplate = false;
            foreach (string imagePath in BattleNetDiabloTabImageCandidates())
            {
                if (!File.Exists(imagePath))
                {
                    continue;
                }

                foundTemplate = true;
                if (TryFindBattleNetImage(
                    imagePath,
                    "BattleNetD3Tab",
                    "Diablo III tab",
                    out DrawingPoint tabCenter,
                    confidence: 0.80))
                {
                    AppLogger.Info($"Diablo III tab found by image: {Path.GetFileName(imagePath)} at {tabCenter.X},{tabCenter.Y}");
                    LeftClick(tabCenter);
                    AppLogger.Info($"Diablo III tab clicked: method=image; point={tabCenter.X},{tabCenter.Y}");
                    return true;
                }

                AppLogger.Info($"Diablo III tab image not matched: {Path.GetFileName(imagePath)}");
            }

            if (!foundTemplate)
            {
                AppLogger.Info("Diablo III tab image not found in Images/Start Game");
            }
            else
            {
                AppLogger.Info("Diablo III tab not found in Battle.net scan region");
            }

            return false;
        }

        private string[] BattleNetDiabloTabImageCandidates()
        {
            string imagesRoot = Img();
            List<string> candidates =
            [
                Img("Start Game", "Battle Net Diablo III Tab.png"),
                Img("Start Game", "Battle Net Diablo III Icon.png"),
                Img("Start Game", "Battlenet D3 Tab.png"),
                Img("Start Game", "Battle.net D3 Tab.png"),
                Img("Start Game", "Diablo III Tab.png"),
                Img("Start Game", "Diablo III Icon.png"),
            ];

            if (Directory.Exists(imagesRoot))
            {
                candidates.AddRange(
                    Directory
                        .EnumerateFiles(imagesRoot, "*.png", SearchOption.AllDirectories)
                        .Where(path =>
                        {
                            string fileName = Path.GetFileNameWithoutExtension(path);
                            if (fileName.Contains("Scan Region", StringComparison.OrdinalIgnoreCase))
                            {
                                return false;
                            }

                            bool isDiablo = fileName.Contains("Diablo", StringComparison.OrdinalIgnoreCase) ||
                                fileName.Contains("D3", StringComparison.OrdinalIgnoreCase);
                            return isDiablo &&
                                (fileName.Contains("Tab", StringComparison.OrdinalIgnoreCase) ||
                                 fileName.Contains("Icon", StringComparison.OrdinalIgnoreCase));
                        }));
            }

            return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private void ConfirmBattleNetDiabloPage(CancellationToken token = default)
        {
            string playButtonPath = Img("Start Game", "Battle Net Play Button.png");
            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < 5000)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (FindImageOnScreen(playButtonPath, out DrawingPoint playButtonCenter, confidence: 0.75))
                {
                    AppLogger.Info($"Diablo III page confirmed: Battle.net Play button visible at {playButtonCenter.X},{playButtonCenter.Y}; elapsed={sw.ElapsedMilliseconds}ms");
                    return;
                }

                PortSleepOrThreadSleep(token, 250);
            }

            AppLogger.Info("Diablo III page confirmation inconclusive: Battle.net Play button not visible during brief post-tab wait");
        }

        private bool WaitForBattleNetPlayButtonAndClick(
            string imagePath,
            int timeoutMs,
            double confidence,
            CancellationToken token = default)
        {
            AppLogger.Info($"Play button scan started: image={Path.GetFileName(imagePath)}; timeoutMs={timeoutMs}; confidence={confidence:0.000}");
            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (TryFindBattleNetImage(
                    imagePath,
                    "BattleNetPlayButton",
                    "Battle.net Play button",
                    out DrawingPoint centerPoint,
                    confidence))
                {
                    AppLogger.Info($"Play button found: point={centerPoint.X},{centerPoint.Y}; elapsed={sw.ElapsedMilliseconds}ms");
                    LeftClick(centerPoint);
                    AppLogger.Info($"Play button clicked: point={centerPoint.X},{centerPoint.Y}");
                    PortStartLaunchGracePeriod("Battle.net Play clicked");
                    return true;
                }

                PortSleepOrThreadSleep(token, 250);
            }

            AppLogger.Info($"Play button not found before timeout: timeoutMs={timeoutMs}");
            PortCaptureFailureScreenshot("BattleNetPlayButtonNotFound");
            return false;
        }

        private bool TryFindBattleNetImage(
            string imagePath,
            string scanRegionKey,
            string label,
            out DrawingPoint centerPoint,
            double confidence)
        {
            centerPoint = DrawingPoint.Empty;

            if (!File.Exists(imagePath))
            {
                AppLogger.Info($"{label} image missing: {imagePath}");
                return false;
            }

            Rectangle? referenceRegion = PortScanRegion(scanRegionKey, imagePath);
            if (referenceRegion.HasValue)
            {
                AppLogger.Info($"{label} cached Battle.net scan region loaded: key={scanRegionKey}; cached={FormatRectangle(referenceRegion.Value)}");
                if (TryResolveBattleNetWindowRelativeScanRegion(referenceRegion.Value, label, out Rectangle screenRegion))
                {
                    AppLogger.Info($"{label} resolved Battle.net screen scan region: cached={FormatRectangle(referenceRegion.Value)}; screen={FormatRectangle(screenRegion)}");
                    if (TryFindImageInScreenRegion(imagePath, screenRegion, out centerPoint, confidence))
                    {
                        AppLogger.Info($"{label} found in Battle.net window-relative region: point={centerPoint.X},{centerPoint.Y}; region={FormatRectangle(screenRegion)}");
                        return true;
                    }

                    AppLogger.Info($"{label} not found in Battle.net window-relative region: region={FormatRectangle(screenRegion)}");
                }

                AppLogger.Info($"{label} fallback full-screen search used after Battle.net region miss");
            }
            else
            {
                AppLogger.Info($"{label} scan region unavailable: key={scanRegionKey}; fallback full-screen search used");
            }

            bool found = FindImageOnScreen(imagePath, out centerPoint, confidence);
            AppLogger.Info(found
                ? $"{label} found by fallback full-screen search: point={centerPoint.X},{centerPoint.Y}"
                : $"{label} not found by fallback full-screen search");
            return found;
        }

        private bool TryResolveBattleNetWindowRelativeScanRegion(Rectangle cachedRegion, string label, out Rectangle screenRegion)
        {
            screenRegion = Rectangle.Empty;
            if (!TryGetFocusedBattleNetWindowRect(label, out Rectangle battleNetRect))
            {
                return false;
            }

            AppLogger.Info($"Battle.net window rect for {label}: {FormatRectangle(battleNetRect)}");
            if (cachedRegion.Width <= 0 ||
                cachedRegion.Height <= 0 ||
                cachedRegion.Left < 0 ||
                cachedRegion.Top < 0 ||
                cachedRegion.Right > battleNetRect.Width ||
                cachedRegion.Bottom > battleNetRect.Height)
            {
                AppLogger.Info($"WARNING {label} cached Battle.net scan region outside window; cached={FormatRectangle(cachedRegion)}; windowRect={FormatRectangle(battleNetRect)}; fallback full-screen search will be used");
                return false;
            }

            screenRegion = new Rectangle(
                battleNetRect.Left + cachedRegion.Left,
                battleNetRect.Top + cachedRegion.Top,
                cachedRegion.Width,
                cachedRegion.Height);
            AppLogger.Info($"{label} Battle.net scan region resolved from window origin: cached={FormatRectangle(cachedRegion)}; windowRect={FormatRectangle(battleNetRect)}; screen={FormatRectangle(screenRegion)}");
            return true;
        }

        private bool TryGetFocusedBattleNetWindowRect(string label, out Rectangle rect)
        {
            rect = Rectangle.Empty;
            IntPtr battleNetWindow = FindBattleNetWindow();
            if (battleNetWindow == IntPtr.Zero)
            {
                AppLogger.Info($"Battle.net window unavailable while resolving {label} scan region");
                return false;
            }

            SetForegroundWindow(battleNetWindow);
            if (!GetWindowRect(battleNetWindow, out RECT windowRect))
            {
                AppLogger.Info($"Battle.net window rect unavailable while resolving {label} scan region: hwnd=0x{battleNetWindow.ToInt64():X}");
                return false;
            }

            rect = new Rectangle(
                windowRect.Left,
                windowRect.Top,
                windowRect.Right - windowRect.Left,
                windowRect.Bottom - windowRect.Top);
            AppLogger.Info($"Battle.net window focused for {label}: hwnd=0x{battleNetWindow.ToInt64():X}; rect={FormatRectangle(rect)}");
            return true;
        }

        private bool TryFindImageInScreenRegion(
            string imagePath,
            Rectangle screenRegion,
            out DrawingPoint centerPoint,
            double confidence)
        {
            centerPoint = DrawingPoint.Empty;
            screenRegion = Rectangle.Intersect(SystemInformation.VirtualScreen, screenRegion);
            if (screenRegion.Width <= 0 || screenRegion.Height <= 0)
            {
                return false;
            }

            using Bitmap screenshot = new(screenRegion.Width, screenRegion.Height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(
                    screenRegion.Left,
                    screenRegion.Top,
                    0,
                    0,
                    screenshot.Size);
            }

            if (!FindImageInBitmap(screenshot, imagePath, out DrawingPoint localCenterPoint, confidence, ImageMatchMode.Color))
            {
                return false;
            }

            centerPoint = new DrawingPoint(
                screenRegion.Left + localCenterPoint.X,
                screenRegion.Top + localCenterPoint.Y);
            return true;
        }

        private Rectangle PortScaleReferenceRectangleToVirtualScreen(Rectangle rectangle)
        {
            Rectangle screen = SystemInformation.VirtualScreen;
            return new Rectangle(
                screen.Left + (int)Math.Round(rectangle.Left * screen.Width / (double)PortReferenceWidth),
                screen.Top + (int)Math.Round(rectangle.Top * screen.Height / (double)PortReferenceHeight),
                (int)Math.Round(rectangle.Width * screen.Width / (double)PortReferenceWidth),
                (int)Math.Round(rectangle.Height * screen.Height / (double)PortReferenceHeight));
        }

        private static string FormatRectangle(Rectangle rectangle)
        {
            return $"{rectangle.Left},{rectangle.Top},{rectangle.Width},{rectangle.Height}";
        }

        private bool PortSleepOrThreadSleep(CancellationToken token, int milliseconds)
        {
            if (token.CanBeCanceled)
            {
                PortSleep(token, milliseconds);
                return !token.IsCancellationRequested;
            }

            Thread.Sleep(milliseconds);
            return true;
        }

        private void PortStartLaunchGracePeriod(string reason)
        {
            DateTime graceUntil = DateTime.UtcNow.AddMilliseconds(PortLaunchGracePeriodMs);
            Interlocked.Exchange(ref portLaunchGraceUntilTicks, graceUntil.Ticks);
            Interlocked.Exchange(ref portLastLaunchGraceMissingLogTicks, 0);
            portConsecutiveDiabloMissingChecks = 0;
            portLaunchGraceStableLogged = false;
            AppLogger.Info($"Launch grace period started: reason={reason}; durationMs={PortLaunchGracePeriodMs}; untilUtc={graceUntil:O}");
        }

        // Method to check if Battle.net is running =========================================
        private bool IsBattleNetRunning()
        {
            return Process.GetProcessesByName("Battle.net").Length > 0;
        }

        private bool IsDiabloMainMenuVisible()
        {
            return false;
        }

        private void ClickBattleNetPlayButton()
        {
            // Image recognition later
        }

        // Close Battlnet after starting Diablo
        private void CloseBattleNet()
        {
            foreach (Process process in Process.GetProcessesByName("Battle.net"))
            {
                try
                {
                    process.CloseMainWindow();
                }
                catch
                {
                    // Ignore if Battle.net cannot be closed.
                }
            }
        }

        // Mouse click helper method =========================================
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

        private void PerformMouseClick(
            DrawingPoint point,
            uint buttonDown,
            uint buttonUp)
        {
            SetCursorPos(point.X, point.Y);

            Thread.Sleep(100);

            mouse_event(buttonDown, 0, 0, 0, UIntPtr.Zero);

            Thread.Sleep(50);

            mouse_event(buttonUp, 0, 0, 0, UIntPtr.Zero);
        }

        // Left click helper
        private void LeftClick(DrawingPoint point)
        {
            PerformMouseClick(
                point,
                MOUSEEVENTF_LEFTDOWN,
                MOUSEEVENTF_LEFTUP);
        }

        // Right click helper
        private void RightClick(DrawingPoint point)
        {
            PerformMouseClick(
                point,
                MOUSEEVENTF_RIGHTDOWN,
                MOUSEEVENTF_RIGHTUP);
        }

        // Convenience method for left-clicking the center of an image on screen
        private bool ClickImageCenter(string imagePath, double confidence = 0.85)
        {
            if (FindImageOnScreen(imagePath, out DrawingPoint centerPoint, confidence))
            {
                LeftClick(centerPoint);
                return true;
            }

            return false;
        }

        // Waits for image then clicks its center, with timeout
        private bool WaitForImageAndClick(
            string imagePath,
            int timeoutMs = 30000,
            double confidence = 0.85)
        {
            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (FindImageOnScreen(imagePath, out DrawingPoint centerPoint, confidence))
                {
                    LeftClick(centerPoint);
                    return true;
                }

                Thread.Sleep(250);
            }

            return false;
        }

        // Diablo click helper that searches for image within Diablo window only
        private bool ClickImageCenterInDiabloWindow(
            string imagePath,
            double confidence = 0.85)
        {
            if (FindImageInDiabloWindow(
                imagePath,
                out DrawingPoint centerPoint,
                confidence))
            {
                LeftClick(centerPoint);
                return true;
            }

            return false;
        }

        // Clicks the start game button and uses Diablo specific image search with timeout
        private bool WaitForDiabloImageAndClick(
            string imagePath,
            int timeoutMs = 30000,
            double confidence = 0.85,
            ImageMatchMode matchMode = ImageMatchMode.Default)
        {
            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (FindImageInDiabloWindow(
                    imagePath,
                    out DrawingPoint centerPoint,
                    confidence,
                    matchMode))
                {
                    LeftClick(centerPoint);
                    return true;
                }

                Thread.Sleep(250);
            }

            return false;
        }

        // Image recognition methods =========================================
        private bool ImageExistsOnScreen(string imagePath, double confidence = 0.85)
        {
            if (!File.Exists(imagePath))
            {
                MessageBox.Show($"Image not found:\n{imagePath}");
                return false;
            }

            using Bitmap screenshot = new Bitmap(
                Screen.PrimaryScreen!.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height);

            using Graphics graphics = Graphics.FromImage(screenshot);
            graphics.CopyFromScreen(0, 0, 0, 0, screenshot.Size);

            using Mat rawScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
            using Mat screenMat = new Mat();

            Cv2.CvtColor(rawScreenMat, screenMat, ColorConversionCodes.BGRA2BGR);

            using Mat templateMat = Cv2.ImRead(imagePath, ImreadModes.Color);
            using Mat result = new Mat();

            Cv2.MatchTemplate(screenMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);

            return maxVal >= confidence;
        }

        private bool FindImageOnScreen(string imagePath, out DrawingPoint centerPoint, double confidence = 0.85)
        {
            centerPoint = DrawingPoint.Empty;

            if (!File.Exists(imagePath))
            {
                MessageBox.Show($"Image not found:\n{imagePath}");
                return false;
            }

            using Bitmap screenshot = new Bitmap(
                Screen.PrimaryScreen!.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height);

            using Graphics graphics = Graphics.FromImage(screenshot);
            graphics.CopyFromScreen(0, 0, 0, 0, screenshot.Size);

            using Mat rawScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
            using Mat screenMat = new Mat();

            Cv2.CvtColor(rawScreenMat, screenMat, ColorConversionCodes.BGRA2BGR);

            using Mat templateMat = Cv2.ImRead(imagePath, ImreadModes.Color);
            using Mat result = new Mat();

            Cv2.MatchTemplate(screenMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal < confidence)
            {
                return false;
            }

            centerPoint = new DrawingPoint(
                maxLoc.X + templateMat.Width / 2,
                maxLoc.Y + templateMat.Height / 2);

            return true;
        }

        // Window Specific Image Search ==========================================
        private Bitmap? CaptureWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return null;
            }

            if (!GetWindowRect(hWnd, out RECT rect))
            {
                return null;
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
            {
                return null;
            }

            Bitmap screenshot = new Bitmap(width, height);

            using Graphics graphics = Graphics.FromImage(screenshot);
            graphics.CopyFromScreen(
                rect.Left,
                rect.Top,
                0,
                0,
                screenshot.Size);

            return screenshot;
        }

        // Diablo-Specific Image Search
        private bool FindImageInDiabloWindow(
            string imagePath,
            out DrawingPoint centerPoint,
            double confidence = 0.85,
            ImageMatchMode matchMode = ImageMatchMode.Default)
        {
            Stopwatch perf = Stopwatch.StartNew();
            centerPoint = DrawingPoint.Empty;
            string imageName = Path.GetFileName(imagePath);

            IntPtr diabloWindow = FindDiabloWindow();

            if (diabloWindow == IntPtr.Zero)
            {
                if (perf.ElapsedMilliseconds >= 1000)
                {
                    AppLogger.Info($"PERF FindImageInDiabloWindow {imageName}: window missing in {perf.ElapsedMilliseconds}ms");
                }
                return false;
            }

            if (!GetWindowRect(diabloWindow, out RECT rect))
            {
                if (perf.ElapsedMilliseconds >= 1000)
                {
                    AppLogger.Info($"PERF FindImageInDiabloWindow {imageName}: rect missing in {perf.ElapsedMilliseconds}ms");
                }
                return false;
            }

            Rectangle? referenceRegion = PortScanRegionForImage(imagePath);
            Rectangle screenOffsetRegion = new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            Bitmap? screenshot = null;
            if (referenceRegion.HasValue)
            {
                screenOffsetRegion = PortScaleReferenceRectangle(referenceRegion.Value, rect);
                screenOffsetRegion = Rectangle.Intersect(SystemInformation.VirtualScreen, screenOffsetRegion);
                screenshot = PortCaptureDiabloRegion(referenceRegion.Value);
            }
            else
            {
                screenshot = CaptureWindow(diabloWindow);
            }

            if (screenshot == null)
            {
                if (perf.ElapsedMilliseconds >= 1000)
                {
                    AppLogger.Info($"PERF FindImageInDiabloWindow {imageName}: capture failed in {perf.ElapsedMilliseconds}ms");
                }
                return false;
            }

            using (screenshot)
            {
                if (!FindImageInBitmap(
                    screenshot,
                    imagePath,
                    out DrawingPoint localCenterPoint,
                    confidence,
                    matchMode))
                {
                    if (perf.ElapsedMilliseconds >= 1000)
                    {
                        AppLogger.Info($"PERF FindImageInDiabloWindow {imageName}: no match in {perf.ElapsedMilliseconds}ms");
                    }
                    return false;
                }

                centerPoint = new DrawingPoint(
                    screenOffsetRegion.Left + localCenterPoint.X,
                    screenOffsetRegion.Top + localCenterPoint.Y);
            }

            AppLogger.Info($"PERF FindImageInDiabloWindow {imageName}: matched in {perf.ElapsedMilliseconds}ms");
            return true;
        }

        // Reusable bitmap matching method for window-specific searches
        private bool FindImageInBitmap(
            Bitmap screenshot,
            string imagePath,
            out DrawingPoint centerPoint,
            double confidence = 0.85,
            ImageMatchMode matchMode = ImageMatchMode.Default)
        {
            centerPoint = DrawingPoint.Empty;

            if (!File.Exists(imagePath))
            {
                MessageBox.Show($"Image not found:\n{imagePath}");
                return false;
            }

            using Mat rawScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
            using Mat screenMat = new();
            using Mat templateMat = Cv2.ImRead(imagePath, matchMode == ImageMatchMode.Color ? ImreadModes.Color : ImreadModes.Grayscale);
            if (matchMode == ImageMatchMode.Color)
            {
                Cv2.CvtColor(rawScreenMat, screenMat, ColorConversionCodes.BGRA2BGR);
            }
            else
            {
                Cv2.CvtColor(rawScreenMat, screenMat, ColorConversionCodes.BGRA2GRAY);
            }

            using Mat result = new Mat();

            Cv2.MatchTemplate(
                screenMat,
                templateMat,
                result,
                TemplateMatchModes.CCoeffNormed);

            Cv2.MinMaxLoc(
                result,
                out _,
                out double maxVal,
                out _,
                out CvPoint maxLoc);

            if (maxVal < confidence)
            {
                return false;
            }

            centerPoint = new DrawingPoint(
                maxLoc.X + templateMat.Width / 2,
                maxLoc.Y + templateMat.Height / 2);

            return true;
        }

        // Waits for an image to appear on screen within a timeout period
        private bool WaitForImage(
            string imagePath,
            int timeoutMs = 30000,
            double confidence = 0.85)
        {
            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (ImageExistsOnScreen(imagePath, confidence))
                {
                    return true;
                }

                Thread.Sleep(250);
            }

            return false;
        }

        private static string Img(params string[] parts)
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Images",
                Path.Combine(parts));
        }

        // Folder Scanner ==========================================
        private string[] GetImagesFromFolder(string folder)
        {
            return Directory.GetFiles(
                Img(folder),
                "*.png",
                SearchOption.AllDirectories);
        }
    }
}

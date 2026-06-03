using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenCvSharp;
using DrawingPoint = System.Drawing.Point;
using CvPoint = OpenCvSharp.Point;
using System.Reflection;

namespace GoblinFarmer
{
    public partial class frmMain : Form
    {
        // State variable to prevent overlapping automation runs
        private bool isAutomationRunning = false;
        private readonly object portMissingAssetPromptLock = new();
        private readonly HashSet<string> portMissingAssetPromptHandled = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> portMissingAssetPromptActive = new(StringComparer.OrdinalIgnoreCase);

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
            //  Dynamic App Version
            this.Text = "GoblinFarmer v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
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

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

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
                if (portBattleNetLaunchFlowActive && !diabloLaunchedAfterAppPlayClick && !diabloLaunchedWithoutAppPlayClick)
                {
                    PortRecordDiabloLaunchAfterBattleNet(-1);
                }

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
            ResetBattleNetLaunchDiagnostics();
            portBattleNetLaunchFlowActive = true;
            Interlocked.Exchange(ref portLastLaunchFlowMissingLogTicks, 0);

            try
            {
                AddWorkflowStep("Starting Battle.net");
                if (!StartBattleNet())
                {
                    SetAppStatus("Battle.net Launch Failed");
                    return false;
                }

                if (!PrepareBattleNetForDiabloLaunch())
                {
                    SetAppStatus("Battle.net Setup Failed");
                    return false;
                }

                AddWorkflowStep("Waiting for Battle.net Play Button");
                SetAppStatus("Waiting For Battle.net Play Button");

                bool clickedPlay = WaitForBattleNetPlayButtonAndClick(
                    Img("Start Game", "Battle Net Play Button.png"),
                    timeoutMs: AppSettings.Launch.BattleNetPlayButtonTimeoutMs,
                    confidence: AppSettings.ImageRecognition.BattleNetPlayButtonConfidence);

                if (!clickedPlay)
                {
                    MessageBox.Show("Could not find Battle.net Play button.");
                    SetAppStatus("Play Button Not Found");
                    return false;
                }

                if (battleNetPlayClickAcceptedByBattleNet)
                {
                    AddWorkflowStep("Battle.net accepted Play click");
                    Thread.Sleep(AppSettings.Launch.BattleNetPostPlayAcceptedDelayMs);
                    CloseBattleNet();
                }
                else
                {
                    AddWorkflowStep("Waiting for Diablo after unconfirmed Play click");
                    AppLogger.Info("Battle.net close skipped before Diablo launch because app Play click was not confirmed accepted.");
                }

                SetAppStatus("Launching Diablo III");

                Stopwatch sw = Stopwatch.StartNew();
                AddWorkflowStep("Waiting for Diablo process");

                while (sw.ElapsedMilliseconds < AppSettings.Launch.DiabloStartTimeoutMs)
                {
                    if (IsDiabloRunning())
                    {
                        AppLogger.Info($"Diablo process detected after {sw.ElapsedMilliseconds}ms");
                        SetAppStatus("Diablo III Started");
                        PortRecordDiabloLaunchAfterBattleNet(sw.ElapsedMilliseconds);
                        return true;
                    }

                    Thread.Sleep(AppSettings.Launch.DiabloStartPollIntervalMs);
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

        private bool StartBattleNet(CancellationToken token = default)
        {
            const int launchRetryIntervalMs = 1000;
            const int launchStartupTimeoutMs = 5000;

            string battleNetPath = AppSettings.ResolveBattleNetExecutablePathForLaunch(
                out string battleNetPathSource,
                out string configuredBattleNetPath,
                out string detectedBattleNetPath);
            AppLogger.Info($"Battle.net executable path selected: source={battleNetPathSource}; path={PortLogField(battleNetPath)}; configuredPath={PortLogField(configuredBattleNetPath)}; detectedPath={PortLogField(detectedBattleNetPath)}");

            if (!File.Exists(battleNetPath))
            {
                MessageBox.Show("Battle.net executable is not configured or could not be found. Open Settings to select Battle.net.exe.");
                AppLogger.Info($"Battle.net launch failed: executable not found; source={battleNetPathSource}; path={PortLogField(battleNetPath)}; configuredPath={PortLogField(AppSettings.Runtime.BattleNetExecutablePath)}; detectedPath={PortLogField(detectedBattleNetPath)}");
                return false;
            }

            Stopwatch sw = Stopwatch.StartNew();
            int launchAttempts = 0;

            while (sw.ElapsedMilliseconds < launchStartupTimeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    AppLogger.Info($"Battle.net launch cancelled: attempts={launchAttempts}; elapsedMs={sw.ElapsedMilliseconds}");
                    return false;
                }

                if (TryDetectBattleNetWindowForLaunch(sw.ElapsedMilliseconds, launchAttempts, out IntPtr existingWindow))
                {
                    SetForegroundWindow(existingWindow);
                    AppLogger.Info($"Battle.net launch-ready window already detected; focused existing window hwnd=0x{existingWindow.ToInt64():X}; attempts={launchAttempts}; elapsedMs={sw.ElapsedMilliseconds}");
                    return true;
                }

                launchAttempts++;
                if (!LaunchBattleNetExecutable(battleNetPath, launchAttempts, sw.ElapsedMilliseconds))
                {
                    return false;
                }

                if (TryDetectBattleNetWindowForLaunch(sw.ElapsedMilliseconds, launchAttempts, out IntPtr launchedWindow))
                {
                    SetForegroundWindow(launchedWindow);
                    AppLogger.Info($"Battle.net launch-ready window detected after launch attempt: hwnd=0x{launchedWindow.ToInt64():X}; attempts={launchAttempts}; elapsedMs={sw.ElapsedMilliseconds}");
                    return true;
                }

                int sleepMs = (int)Math.Min(launchRetryIntervalMs, Math.Max(0, launchStartupTimeoutMs - sw.ElapsedMilliseconds));
                AppLogger.Info($"Battle.net launch retry pending: retryCount={launchAttempts}; nextRetryInMs={sleepMs}; elapsedMs={sw.ElapsedMilliseconds}");
                if (sleepMs <= 0 || !PortSleepOrThreadSleep(token, sleepMs))
                {
                    break;
                }
            }

            bool processDetected = IsBattleNetRunning();
            IntPtr finalWindow = FindBattleNetWindow();
            AppLogger.Info($"Battle.net launch failed: launch-ready Battle.net window not detected within {launchStartupTimeoutMs}ms; attempts={launchAttempts}; elapsedMs={sw.ElapsedMilliseconds}; processObserved={processDetected}; windowDetected={finalWindow != IntPtr.Zero}; executablePath={PortLogField(battleNetPath)}");
            return false;
        }

        private bool LaunchBattleNetExecutable(string battleNetPath, int attempt, long elapsedMs)
        {
            try
            {
                AppLogger.Info($"Battle.net launch attempt: attempt={attempt}; elapsedMs={elapsedMs}; executablePath={PortLogField(battleNetPath)}");
                Process.Start(battleNetPath);
                AppLogger.Info($"Battle.net launch requested: attempt={attempt}; elapsedMs={elapsedMs}; executablePath={PortLogField(battleNetPath)}");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Battle.net launch attempt failed: attempt={attempt}; elapsedMs={elapsedMs}; executablePath={PortLogField(battleNetPath)}", ex);
                return false;
            }
        }

        private bool TryDetectBattleNetWindowForLaunch(long elapsedMs, int launchAttempts, out IntPtr battleNetWindow)
        {
            bool processObserved = IsBattleNetRunning();
            battleNetWindow = FindBattleNetWindow();
            bool windowDetected = battleNetWindow != IntPtr.Zero;
            AppLogger.Info($"Battle.net launch window detection check: attempts={launchAttempts}; elapsedMs={elapsedMs}; processObserved={processObserved}; windowDetected={windowDetected}; hwnd=0x{battleNetWindow.ToInt64():X}; launchReady={windowDetected}");
            return windowDetected;
        }

        private bool PrepareBattleNetForDiabloLaunch(CancellationToken token = default)
        {
            AddWorkflowStep("Focusing Battle.net");
            if (!WaitForBattleNetWindowAndFocus(token, timeoutMs: 1500, logFoundAfterLaunch: false))
            {
                AppLogger.Info($"Battle.net setup failed: confirmed launch window could not be focused; processDetected={IsBattleNetRunning()}; windowDetected={FindBattleNetWindow() != IntPtr.Zero}");
                return false;
            }

            if (token.IsCancellationRequested)
            {
                return false;
            }

            string playButtonPath = Img("Start Game", "Battle Net Play Button.png");

            Stopwatch playPrecheckWait = Stopwatch.StartNew();
            int playPrecheckAttempts = 0;

            while (playPrecheckWait.ElapsedMilliseconds < AppSettings.Launch.BattleNetPlayPrecheckMs)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                playPrecheckAttempts++;
                AppLogger.Info($"Battle.net Play button pre-check attempt: attempt={playPrecheckAttempts}; elapsedMs={playPrecheckWait.ElapsedMilliseconds}");
                if (TryFindBattleNetImage(
                    playButtonPath,
                    "BattleNetPlayButton",
                    "Battle.net Play button pre-check",
                    out DrawingPoint precheckPlayButtonCenter,
                    confidence: AppSettings.ImageRecognition.BattleNetPlayButtonConfidence))
                {
                    AppLogger.Info($"Battle.net Play button visible during pre-check; skipping Diablo III tab selection; point={precheckPlayButtonCenter.X},{precheckPlayButtonCenter.Y}; elapsed={playPrecheckWait.ElapsedMilliseconds}ms");
                    return true;
                }

                PortSleepOrThreadSleep(token, 100);
            }

            AppLogger.Info($"Battle.net Play button not visible after pre-check wait; falling back to Diablo III tab selection; attempts={playPrecheckAttempts}; elapsed={playPrecheckWait.ElapsedMilliseconds}ms");

            AddWorkflowStep("Selecting Diablo III in Battle.net");
            if (!ClickBattleNetDiabloTab())
            {
                PortCaptureFailureScreenshot("DiabloTabNotFound", "BattleNetLaunch");
                AppLogger.Info("Diablo III tab not clicked; continuing to Battle.net Play button search");
            }

            if (!PortSleepOrThreadSleep(token, AppSettings.Launch.BattleNetPostTabSettleMs))
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
                    confidence: AppSettings.ImageRecognition.BattleNetDiabloTabConfidence))
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
                PortOfferMissingAssetCapture(
                    Img("Start Game", "Battle Net Diablo III Tab.png"),
                    "BattleNet",
                    "Diablo III tab",
                    scanRegionKey: "BattleNetD3Tab",
                    captureInstruction: "Capture the Battle.net Diablo III tab or icon while it is visible.");
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

                if (FindImageOnScreen(playButtonPath, out DrawingPoint playButtonCenter, confidence: Math.Max(0.50, AppSettings.ImageRecognition.BattleNetPlayButtonConfidence - 0.10)))
                {
                    AppLogger.Info($"Diablo III page confirmed: Battle.net Play button visible at {playButtonCenter.X},{playButtonCenter.Y}; elapsed={sw.ElapsedMilliseconds}ms");
                    return;
                }

                PortSleepOrThreadSleep(token, 100);
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
            int attempts = 0;

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (IsDiabloRunning())
                {
                    PortRecordDiabloLaunchAfterBattleNet(sw.ElapsedMilliseconds);
                    AppLogger.Info($"Play button wait ended because Diablo is already running: elapsed={sw.ElapsedMilliseconds}ms; battleNetPlayClickSentByApp={battleNetPlayClickSentByApp}; battleNetPlayClickAcceptedByBattleNet={battleNetPlayClickAcceptedByBattleNet}");
                    return battleNetPlayClickSentByApp;
                }

                attempts++;
                AppLogger.Info($"Battle.net Play button detection attempt: attempt={attempts}; elapsedMs={sw.ElapsedMilliseconds}; confidence={confidence:0.000}");
                if (TryFindBattleNetImage(
                    imagePath,
                    "BattleNetPlayButton",
                    "Battle.net Play button",
                    out DrawingPoint centerPoint,
                    confidence))
                {
                    AppLogger.Info($"Play button found by current image match: attempt={attempts}; point={centerPoint.X},{centerPoint.Y}; elapsed={sw.ElapsedMilliseconds}ms; confidenceThreshold={confidence:0.000}");
                    LeftClick(centerPoint);
                    PortRecordBattleNetPlayClickByApp(centerPoint);
                    AppLogger.Info($"Successful Battle.net Play button click sent: attempt={attempts}; point={centerPoint.X},{centerPoint.Y}; elapsedMs={sw.ElapsedMilliseconds}");
                    PortStartLaunchGracePeriod("Battle.net Play clicked");
                    bool accepted = WaitForBattleNetPlayClickAccepted(
                        imagePath,
                        confidence,
                        token,
                        timeoutMs: AppSettings.Launch.BattleNetPlayClickAcceptedTimeoutMs,
                        clickElapsedMs: sw.ElapsedMilliseconds);
                    if (!accepted)
                    {
                        AppLogger.Info($"Battle.net Play click acceptance not verified before timeout: battleNetPlayClickSentByApp=True; battleNetPlayClickAcceptedByBattleNet=False; elapsedMs={sw.ElapsedMilliseconds}");
                    }

                    return true;
                }

                PortSleepOrThreadSleep(token, 100);
            }

            AppLogger.Info($"Play button not found before timeout: timeoutMs={timeoutMs}; attempts={attempts}; elapsedMs={sw.ElapsedMilliseconds}");
            PortCaptureFailureScreenshot("BattleNetPlayButtonNotFound", "BattleNetLaunch");
            string notClickedScreenshotPath = PortCaptureFailureScreenshot("BattleNetPlayButtonNotClickedByApp", "BattleNetLaunch");
            AppLogger.Info($"BattleNetLaunchSummary: event=BattleNetPlayButtonNotClickedByApp; launchSuccessful=False; battleNetPlayClickSentByApp=False; battleNetPlayClickAcceptedByBattleNet=False; battleNetPlayClickAcceptedReason=Unknown; battleNetPlayClickPoint=Unknown; battleNetPlayClickTimestamp=Unknown; battleNetPlayClickAcceptedTimestamp=Unknown; diabloLaunched=False; diabloLaunchedAfterAppPlayClick=False; diabloLaunchedWithoutAppPlayClick=False; battleNetManualPlaySuspected=False; {PortBattleNetCloseSummaryFields()}; attempts={attempts}; elapsedMs={sw.ElapsedMilliseconds}; screenshotPath={PortLogField(PortDisplayLocation(notClickedScreenshotPath))}; likelyExplanation=GoblinFarmer did not find and click Battle.net Play before timeout.");
            return false;
        }

        private bool WaitForBattleNetPlayClickAccepted(
            string imagePath,
            double confidence,
            CancellationToken token,
            int timeoutMs,
            long clickElapsedMs)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int consecutivePlayButtonMissing = 0;
            int attempts = 0;
            AppLogger.Info($"Battle.net Play click acceptance verification started: timeoutMs={timeoutMs}; clickElapsedMs={clickElapsedMs}; battleNetPlayClickSentByApp={battleNetPlayClickSentByApp}");

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (IsDiabloRunning())
                {
                    PortRecordBattleNetPlayClickAccepted("Diablo process started after app Play click", clickElapsedMs + sw.ElapsedMilliseconds);
                    return true;
                }

                IntPtr battleNetWindow = FindBattleNetWindow();
                bool battleNetRunning = IsBattleNetRunning();
                if (battleNetWindow == IntPtr.Zero || !battleNetRunning)
                {
                    PortRecordBattleNetPlayClickAccepted("Battle.net launch transition observed after app Play click", clickElapsedMs + sw.ElapsedMilliseconds);
                    return true;
                }

                attempts++;
                AppLogger.Info($"Battle.net Play button post-click verification attempt: attempt={attempts}; elapsedMs={sw.ElapsedMilliseconds}; confidence={confidence:0.000}");
                bool playButtonStillVisible = TryFindBattleNetImage(
                    imagePath,
                    "BattleNetPlayButton",
                    "Battle.net Play button post-click verification",
                    out _,
                    confidence);
                if (!playButtonStillVisible)
                {
                    consecutivePlayButtonMissing++;
                    if (consecutivePlayButtonMissing >= 2)
                    {
                        PortRecordBattleNetPlayClickAccepted("Battle.net Play button disappeared after app Play click", clickElapsedMs + sw.ElapsedMilliseconds);
                        return true;
                    }
                }
                else
                {
                    consecutivePlayButtonMissing = 0;
                }

                PortSleepOrThreadSleep(token, 100);
            }

            return false;
        }

        private void ResetBattleNetLaunchDiagnostics()
        {
            battleNetPlayClickSentByApp = false;
            battleNetPlayClickAcceptedByBattleNet = false;
            battleNetPlayClickPoint = DrawingPoint.Empty;
            battleNetPlayClickTimestamp = null;
            battleNetPlayClickAcceptedTimestamp = null;
            battleNetPlayClickAcceptedReason = "Unknown";
            diabloLaunchedAfterAppPlayClick = false;
            diabloLaunchedWithoutAppPlayClick = false;
            battleNetManualPlaySuspected = false;
            battleNetLaunchOutcomeRecorded = false;
            battleNetPostLaunchCloseEvaluated = false;
            battleNetStillOpenAfterLaunch = false;
            battleNetCloseRequested = false;
            battleNetCloseSucceeded = false;
            battleNetCloseTimedOut = false;
            battleNetCloseProcessRemaining = false;
            battleNetCloseVisibleWindowRemaining = false;
            AppLogger.Info("BattleNetLaunchStateReset: battleNetPlayClickSentByApp=False; battleNetPlayClickAcceptedByBattleNet=False; battleNetPlayClickPoint=Unknown; battleNetPlayClickTimestamp=Unknown; battleNetPlayClickAcceptedTimestamp=Unknown; diabloLaunchedAfterAppPlayClick=False; diabloLaunchedWithoutAppPlayClick=False; battleNetManualPlaySuspected=False");
        }

        private void PortRecordBattleNetPlayClickByApp(DrawingPoint point)
        {
            battleNetPlayClickSentByApp = true;
            battleNetPlayClickPoint = point;
            battleNetPlayClickTimestamp = DateTime.Now;
            AppLogger.Info($"BattleNetPlayClickSentByApp: battleNetPlayClickSentByApp=True; battleNetPlayClickAcceptedByBattleNet=False; battleNetPlayClickPoint={point.X},{point.Y}; battleNetPlayClickTimestamp={battleNetPlayClickTimestamp:O}");
        }

        private void PortRecordBattleNetPlayClickAccepted(string reason, long elapsedMs)
        {
            if (battleNetPlayClickAcceptedByBattleNet)
            {
                return;
            }

            battleNetPlayClickAcceptedByBattleNet = true;
            battleNetPlayClickAcceptedTimestamp = DateTime.Now;
            battleNetPlayClickAcceptedReason = reason;
            AppLogger.Info($"BattleNetPlayClickAccepted: battleNetPlayClickSentByApp={battleNetPlayClickSentByApp}; battleNetPlayClickAcceptedByBattleNet=True; battleNetPlayClickAcceptedReason={PortLogField(reason)}; battleNetPlayClickPoint={(battleNetPlayClickSentByApp ? $"{battleNetPlayClickPoint.X},{battleNetPlayClickPoint.Y}" : "Unknown")}; battleNetPlayClickTimestamp={(battleNetPlayClickTimestamp.HasValue ? battleNetPlayClickTimestamp.Value.ToString("O") : "Unknown")}; battleNetPlayClickAcceptedTimestamp={battleNetPlayClickAcceptedTimestamp:O}; elapsedMs={elapsedMs}");
            PortCaptureSuccessScreenshot("BattleNetLaunch", "BattleNetPlayClickAccepted");
        }

        private void PortRecordDiabloLaunchAfterBattleNet(long elapsedMs)
        {
            if (battleNetLaunchOutcomeRecorded)
            {
                return;
            }

            battleNetLaunchOutcomeRecorded = true;

            if (battleNetPlayClickAcceptedByBattleNet)
            {
                diabloLaunchedAfterAppPlayClick = true;
                diabloLaunchedWithoutAppPlayClick = false;
                battleNetManualPlaySuspected = false;
                AppLogger.Info($"BattleNetLaunchSummary: event=DiabloLaunchedBecauseOfAppClick; launchSuccessful=True; battleNetPlayClickSentByApp={battleNetPlayClickSentByApp}; battleNetPlayClickAcceptedByBattleNet=True; battleNetPlayClickAcceptedReason={PortLogField(battleNetPlayClickAcceptedReason)}; battleNetPlayClickPoint={battleNetPlayClickPoint.X},{battleNetPlayClickPoint.Y}; battleNetPlayClickTimestamp={battleNetPlayClickTimestamp:O}; battleNetPlayClickAcceptedTimestamp={(battleNetPlayClickAcceptedTimestamp.HasValue ? battleNetPlayClickAcceptedTimestamp.Value.ToString("O") : "Unknown")}; diabloLaunched=True; diabloLaunchedAfterAppPlayClick=True; diabloLaunchedWithoutAppPlayClick=False; battleNetManualPlaySuspected=False; {PortBattleNetCloseSummaryFields()}; elapsedMs={elapsedMs}; likelyExplanation=App sent Battle.net Play click, Battle.net accepted it, then Diablo launched.");
            }
            else
            {
                diabloLaunchedAfterAppPlayClick = false;
                diabloLaunchedWithoutAppPlayClick = true;
                battleNetManualPlaySuspected = true;
                string manualScreenshotPath = PortCaptureFailureScreenshot("BattleNetManualPlaySuspected", "BattleNetLaunch");
                if (!battleNetPlayClickSentByApp)
                {
                    PortCaptureFailureScreenshot("BattleNetPlayButtonNotClickedByApp", "BattleNetLaunch");
                }

                const string manualReason = "Diablo launched but app Play click was not confirmed accepted";
                AppLogger.Info($"BattleNetManualPlaySuspected: reason={PortLogField(manualReason)}; battleNetPlayClickSentByApp={battleNetPlayClickSentByApp}; battleNetPlayClickAcceptedByBattleNet=False; battleNetPlayClickPoint={(battleNetPlayClickSentByApp ? $"{battleNetPlayClickPoint.X},{battleNetPlayClickPoint.Y}" : "Unknown")}; battleNetPlayClickTimestamp={(battleNetPlayClickTimestamp.HasValue ? battleNetPlayClickTimestamp.Value.ToString("O") : "Unknown")}; diabloLaunched=True; diabloLaunchedAfterAppPlayClick=False; diabloLaunchedWithoutAppPlayClick=True; battleNetManualPlaySuspected=True; elapsedMs={elapsedMs}; screenshotPath={PortLogField(PortDisplayLocation(manualScreenshotPath))}; likelyExplanation={PortLogField(manualReason)}.");
                AppLogger.Info($"BattleNetLaunchSummary: event=BattleNetManualPlaySuspected; launchSuccessful=False; reason={PortLogField(manualReason)}; battleNetPlayClickSentByApp={battleNetPlayClickSentByApp}; battleNetPlayClickAcceptedByBattleNet=False; battleNetPlayClickAcceptedReason=Unknown; battleNetPlayClickPoint={(battleNetPlayClickSentByApp ? $"{battleNetPlayClickPoint.X},{battleNetPlayClickPoint.Y}" : "Unknown")}; battleNetPlayClickTimestamp={(battleNetPlayClickTimestamp.HasValue ? battleNetPlayClickTimestamp.Value.ToString("O") : "Unknown")}; battleNetPlayClickAcceptedTimestamp=Unknown; diabloLaunched=True; diabloLaunchedAfterAppPlayClick=False; diabloLaunchedWithoutAppPlayClick=True; battleNetManualPlaySuspected=True; {PortBattleNetCloseSummaryFields()}; elapsedMs={elapsedMs}; screenshotPath={PortLogField(PortDisplayLocation(manualScreenshotPath))}; likelyExplanation={PortLogField(manualReason)}.");
            }

            PortLogBattleNetStillOpenAfterDiabloLaunch();
        }

        private void PortLogBattleNetStillOpenAfterDiabloLaunch()
        {
            IntPtr battleNetWindow = FindBattleNetWindow();
            bool battleNetVisibleWindowOpen = battleNetWindow != IntPtr.Zero;
            bool battleNetBackgroundProcessObserved = IsBattleNetRunning();
            battleNetStillOpenAfterLaunch = battleNetVisibleWindowOpen;
            if (!battleNetVisibleWindowOpen)
            {
                AppLogger.Info($"Battle.net visible window closed after Diablo launch; backgroundProcessObserved={battleNetBackgroundProcessObserved}; backgroundProcessIsFailure=False");
                AppLogger.Info($"BattleNetLaunchSummary: event=BattleNetVisibleWindowClosedAfterDiabloLaunch; launchSuccessful={battleNetPlayClickAcceptedByBattleNet && diabloLaunchedAfterAppPlayClick}; battleNetStillOpenAfterLaunch=False; battleNetVisibleWindowOpenAfterLaunch=False; battleNetBackgroundProcessObserved={battleNetBackgroundProcessObserved}; battleNetBackgroundProcessIsFailure=False; battleNetPlayClickSentByApp={battleNetPlayClickSentByApp}; battleNetPlayClickAcceptedByBattleNet={battleNetPlayClickAcceptedByBattleNet}; battleNetPlayClickAcceptedReason={PortLogField(battleNetPlayClickAcceptedReason)}; battleNetPlayClickPoint={(battleNetPlayClickSentByApp ? $"{battleNetPlayClickPoint.X},{battleNetPlayClickPoint.Y}" : "Unknown")}; battleNetPlayClickTimestamp={(battleNetPlayClickTimestamp.HasValue ? battleNetPlayClickTimestamp.Value.ToString("O") : "Unknown")}; battleNetPlayClickAcceptedTimestamp={(battleNetPlayClickAcceptedTimestamp.HasValue ? battleNetPlayClickAcceptedTimestamp.Value.ToString("O") : "Unknown")}; diabloLaunched=True; diabloLaunchedAfterAppPlayClick={diabloLaunchedAfterAppPlayClick}; diabloLaunchedWithoutAppPlayClick={diabloLaunchedWithoutAppPlayClick}; battleNetManualPlaySuspected={battleNetManualPlaySuspected}; {PortBattleNetCloseSummaryFields()}; likelyExplanation=Battle.net visible window was closed or gone after Diablo launch; background tray process state is informational.");
                return;
            }

            string stillOpenScreenshotPath = PortCaptureFailureScreenshot("BattleNetStillOpenAfterDiabloLaunch", "BattleNetLaunch");
            AppLogger.Info($"BattleNetStillOpenAfterDiabloLaunch: battleNetStillOpenAfterLaunch=True; battleNetVisibleWindowOpenAfterLaunch=True; battleNetBackgroundProcessObserved={battleNetBackgroundProcessObserved}; battleNetWindow=0x{battleNetWindow.ToInt64():X}; battleNetPlayClickSentByApp={battleNetPlayClickSentByApp}; battleNetPlayClickAcceptedByBattleNet={battleNetPlayClickAcceptedByBattleNet}; diabloLaunchedAfterAppPlayClick={diabloLaunchedAfterAppPlayClick}; diabloLaunchedWithoutAppPlayClick={diabloLaunchedWithoutAppPlayClick}; battleNetManualPlaySuspected={battleNetManualPlaySuspected}; screenshotPath={PortLogField(PortDisplayLocation(stillOpenScreenshotPath))}; likelyExplanation=The visible Battle.net window remained open after Diablo launched. Existing close behavior will be requested again.");
            AppLogger.Info($"BattleNetLaunchSummary: event=BattleNetVisibleWindowStillOpenAfterDiabloLaunch; launchSuccessful={battleNetPlayClickAcceptedByBattleNet && diabloLaunchedAfterAppPlayClick}; battleNetStillOpenAfterLaunch=True; battleNetVisibleWindowOpenAfterLaunch=True; battleNetBackgroundProcessObserved={battleNetBackgroundProcessObserved}; battleNetBackgroundProcessIsFailure=False; battleNetPlayClickSentByApp={battleNetPlayClickSentByApp}; battleNetPlayClickAcceptedByBattleNet={battleNetPlayClickAcceptedByBattleNet}; battleNetPlayClickAcceptedReason={PortLogField(battleNetPlayClickAcceptedReason)}; battleNetPlayClickPoint={(battleNetPlayClickSentByApp ? $"{battleNetPlayClickPoint.X},{battleNetPlayClickPoint.Y}" : "Unknown")}; battleNetPlayClickTimestamp={(battleNetPlayClickTimestamp.HasValue ? battleNetPlayClickTimestamp.Value.ToString("O") : "Unknown")}; battleNetPlayClickAcceptedTimestamp={(battleNetPlayClickAcceptedTimestamp.HasValue ? battleNetPlayClickAcceptedTimestamp.Value.ToString("O") : "Unknown")}; diabloLaunched=True; diabloLaunchedAfterAppPlayClick={diabloLaunchedAfterAppPlayClick}; diabloLaunchedWithoutAppPlayClick={diabloLaunchedWithoutAppPlayClick}; battleNetManualPlaySuspected={battleNetManualPlaySuspected}; {PortBattleNetCloseSummaryFields()}; screenshotPath={PortLogField(PortDisplayLocation(stillOpenScreenshotPath))}; likelyExplanation=Visible Battle.net window remained open after Diablo launch.");
            CloseBattleNet();
        }

        private string PortBattleNetCloseSummaryFields()
        {
            return $"battleNetCloseRequested={battleNetCloseRequested}; battleNetCloseSucceeded={battleNetCloseSucceeded}; battleNetCloseTimedOut={battleNetCloseTimedOut}; battleNetCloseProcessRemaining={battleNetCloseProcessRemaining}; battleNetCloseProcessRemainingIsFailure=False; battleNetCloseVisibleWindowRemaining={battleNetCloseVisibleWindowRemaining}";
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
                Rectangle? missingAssetRegion = null;
                Rectangle? missingAssetReferenceRegion = PortScanRegions.GetRegion(scanRegionKey, imagePath);
                if (missingAssetReferenceRegion.HasValue &&
                    TryResolveBattleNetWindowRelativeScanRegion(missingAssetReferenceRegion.Value, label, out Rectangle missingAssetScreenRegion, out _))
                {
                    missingAssetRegion = missingAssetScreenRegion;
                }

                PortOfferMissingAssetCapture(
                    imagePath,
                    "BattleNet",
                    label,
                    missingAssetRegion,
                    scanRegionKey,
                    $"Capture the missing Battle.net template for {label}. Make sure the expected Battle.net UI element is visible inside the known scan area.");
                CaptureDebugScreenshot("BattleNet", $"{label} image missing");
                return false;
            }

            Rectangle? referenceRegion = PortScanRegion(scanRegionKey, imagePath);
            Rectangle? resolvedScreenRegion = null;
            Rectangle? resolvedBattleNetRect = null;
            if (referenceRegion.HasValue)
            {
                AppLogger.Info($"{label} cached Battle.net scan region loaded: key={scanRegionKey}; cached={FormatRectangle(referenceRegion.Value)}");
                if (TryResolveBattleNetWindowRelativeScanRegion(referenceRegion.Value, label, out Rectangle screenRegion, out Rectangle battleNetRect))
                {
                    resolvedScreenRegion = screenRegion;
                    resolvedBattleNetRect = battleNetRect;
                    AppLogger.Info($"{label} resolved Battle.net screen scan region: cached={FormatRectangle(referenceRegion.Value)}; screen={FormatRectangle(screenRegion)}");
                    if (TryFindImageInScreenRegion(imagePath, screenRegion, out centerPoint, confidence))
                    {
                        AppLogger.Info($"{label} found in Battle.net window-relative region: point={centerPoint.X},{centerPoint.Y}; region={FormatRectangle(screenRegion)}");
                        return true;
                    }

                    AppLogger.Info($"{label} not found in Battle.net window-relative region: region={FormatRectangle(screenRegion)}");
                    CaptureDebugScreenshot("BattleNet", $"{label} not found in window-relative region", screenRegion);
                }

                AppLogger.Info($"{label} fallback full-screen search used after Battle.net region miss");
            }
            else
            {
                AppLogger.Info($"{label} scan region unavailable: key={scanRegionKey}; fallback full-screen search used");
                CaptureDebugScreenshot("BattleNet", $"{label} scan region unavailable");
            }

            bool found = FindImageOnScreen(imagePath, out centerPoint, confidence);
            AppLogger.Info(found
                ? $"{label} found by fallback full-screen search: point={centerPoint.X},{centerPoint.Y}"
                : $"{label} not found by fallback full-screen search");
            if (!found)
            {
                CaptureDebugScreenshot("BattleNet", $"{label} not found by fallback full-screen search");
            }

            if (found && referenceRegion.HasValue)
            {
                LogBattleNetFallbackRegionDiagnostic(label, referenceRegion.Value, resolvedBattleNetRect, resolvedScreenRegion, centerPoint);
            }

            return found;
        }

        private bool TryResolveBattleNetWindowRelativeScanRegion(Rectangle cachedRegion, string label, out Rectangle screenRegion, out Rectangle battleNetRect)
        {
            screenRegion = Rectangle.Empty;
            battleNetRect = Rectangle.Empty;
            if (!TryGetFocusedBattleNetWindowRect(label, out battleNetRect))
            {
                CaptureDebugScreenshot("BattleNet", $"{label} Battle.net window unavailable");
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
                CaptureDebugScreenshot("BattleNet", $"{label} cached scan region outside window");
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

        private void LogBattleNetFallbackRegionDiagnostic(
            string label,
            Rectangle cachedRegion,
            Rectangle? battleNetRect,
            Rectangle? resolvedScreenRegion,
            DrawingPoint fallbackPoint)
        {
            if (!battleNetRect.HasValue || !resolvedScreenRegion.HasValue)
            {
                AppLogger.Info($"{label} fallback region diagnostic: cached={FormatRectangle(cachedRegion)}; windowRect=unavailable; resolvedScreenRegion=unavailable; fallbackPoint={FormatPoint(fallbackPoint)}; expectedCenter=unavailable; distance=unavailable; assessment=unable to compare because the window-relative region was not resolved");
                return;
            }

            Rectangle window = battleNetRect.Value;
            Rectangle screenRegion = resolvedScreenRegion.Value;
            DrawingPoint expectedCenter = new(
                screenRegion.Left + (screenRegion.Width / 2),
                screenRegion.Top + (screenRegion.Height / 2));
            int deltaX = fallbackPoint.X - expectedCenter.X;
            int deltaY = fallbackPoint.Y - expectedCenter.Y;
            double distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
            bool fallbackInsideResolvedRegion = screenRegion.Contains(fallbackPoint);
            DrawingPoint fallbackWindowLocalPoint = new(
                fallbackPoint.X - window.Left,
                fallbackPoint.Y - window.Top);
            Rectangle suggestedCachedRegion = new(
                Math.Max(0, fallbackWindowLocalPoint.X - (cachedRegion.Width / 2)),
                Math.Max(0, fallbackWindowLocalPoint.Y - (cachedRegion.Height / 2)),
                cachedRegion.Width,
                cachedRegion.Height);
            string assessment = fallbackInsideResolvedRegion
                ? "fallback point is inside resolved region; likely template/confidence/content issue rather than region offset"
                : "fallback point is outside resolved region; cached region is likely offset for this Battle.net window";

            AppLogger.Info($"{label} fallback region diagnostic: cached={FormatRectangle(cachedRegion)}; windowRect={FormatRectangle(window)}; resolvedScreenRegion={FormatRectangle(screenRegion)}; expectedCenter={FormatPoint(expectedCenter)}; fallbackPoint={FormatPoint(fallbackPoint)}; delta={deltaX},{deltaY}; distance={distance:0.0}px; fallbackInsideResolvedRegion={fallbackInsideResolvedRegion}; fallbackWindowLocalPoint={FormatPoint(fallbackWindowLocalPoint)}; suggestedCachedRegionSameSize={FormatRectangle(suggestedCachedRegion)}; assessment={assessment}");
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

            if (!File.Exists(imagePath))
            {
                PortOfferMissingAssetCapture(
                    imagePath,
                    "ScreenRegionImageSearch",
                    Path.GetFileName(imagePath),
                    screenRegion,
                    captureInstruction: "Capture the missing template from the known screen scan region.");
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

        private string CaptureDebugScreenshot(string actionName, string reason, Rectangle? region = null)
        {
            actionName = string.IsNullOrWhiteSpace(actionName) ? "Unknown" : actionName.Trim();
            reason = string.IsNullOrWhiteSpace(reason) ? "Unknown" : reason.Trim();
            string throttleKey = $"{actionName}|{reason}";
            Rectangle captureRegion = region.HasValue
                ? Rectangle.Intersect(SystemInformation.VirtualScreen, region.Value)
                : SystemInformation.VirtualScreen;
            string regionText = region.HasValue ? FormatRectangle(region.Value) : "full-screen";

            if (!AppSettings.Debug.EnableDebugScreenshots)
            {
                AppLogger.Info($"DebugScreenshotSkipped: action={PortLogField(actionName)}; reason={PortLogField(reason)}; skipReason=disabled by config");
                return "";
            }

            if (captureRegion.Width <= 0 || captureRegion.Height <= 0)
            {
                AppLogger.Info($"DebugScreenshotSkipped: action={PortLogField(actionName)}; reason={PortLogField(reason)}; skipReason=empty or invalid region");
                return "";
            }

            lock (portDebugScreenshotLock)
            {
                if (portDebugScreenshotCountThisRun >= 50)
                {
                    AppLogger.Info($"DebugScreenshotSkipped: action={PortLogField(actionName)}; reason={PortLogField(reason)}; skipReason=run cap reached");
                    return "";
                }

                if (portDebugScreenshotLastCaptured.TryGetValue(throttleKey, out DateTime lastCaptured) &&
                    DateTime.Now - lastCaptured < TimeSpan.FromSeconds(10))
                {
                    AppLogger.Info($"DebugScreenshotSkipped: action={PortLogField(actionName)}; reason={PortLogField(reason)}; skipReason=throttled");
                    return "";
                }

                portDebugScreenshotLastCaptured[throttleKey] = DateTime.Now;
                portDebugScreenshotCountThisRun++;
            }

            string windowTitle = PortForegroundWindowTitle();

            try
            {
                string category = PortDebugScreenshotCategory(actionName);
                string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug-screenshots", category);
                Directory.CreateDirectory(directory);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string regionSuffix = region.HasValue ? $"_{PortSafeFileName(regionText)}" : "";
                string fileName = $"{timestamp}_{PortSafeFileName(actionName)}_{PortSafeFileName(reason)}{regionSuffix}.png";
                string path = Path.Combine(directory, fileName);

                using Bitmap screenshot = new(captureRegion.Width, captureRegion.Height);
                using (Graphics graphics = Graphics.FromImage(screenshot))
                {
                    graphics.CopyFromScreen(
                        captureRegion.Left,
                        captureRegion.Top,
                        0,
                        0,
                        screenshot.Size);
                }

                screenshot.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                AppLogger.Info($"DebugScreenshotCaptured: action={PortLogField(actionName)}; reason={PortLogField(reason)}; path={PortLogField(path)}; region={PortLogField(regionText)}; windowTitle={PortLogField(windowTitle)}");
                return path;
            }
            catch (Exception ex)
            {
                lock (portDebugScreenshotLock)
                {
                    portDebugScreenshotCountThisRun = Math.Max(0, portDebugScreenshotCountThisRun - 1);
                }

                AppLogger.Info($"DebugScreenshotSkipped: action={PortLogField(actionName)}; reason={PortLogField(reason)}; skipReason=capture failed: {PortLogField(ex.Message)}");
                return "";
            }
        }

        private void PortOfferMissingAssetCapture(
            string imagePath,
            string callingFlow,
            string missingAssetName = "",
            Rectangle? knownScreenRegion = null,
            string scanRegionKey = "",
            string captureInstruction = "")
        {
            missingAssetName = string.IsNullOrWhiteSpace(missingAssetName)
                ? Path.GetFileName(imagePath)
                : missingAssetName.Trim();
            callingFlow = string.IsNullOrWhiteSpace(callingFlow) ? "Unknown" : callingFlow.Trim();
            string normalizedImagePath = string.IsNullOrWhiteSpace(imagePath) ? "" : imagePath.Trim();
            string promptKey = string.IsNullOrWhiteSpace(normalizedImagePath)
                ? $"{callingFlow}|{missingAssetName}|{scanRegionKey}"
                : normalizedImagePath;
            string knownRegionText = knownScreenRegion.HasValue ? FormatRectangle(knownScreenRegion.Value) : "Unknown";
            string state = PortMissingAssetStateFields();

            AppLogger.Info($"MissingAssetDetected: asset={PortLogField(missingAssetName)}; path={PortLogField(normalizedImagePath)}; flow={PortLogField(callingFlow)}; scanRegionKey={PortLogField(scanRegionKey)}; knownScreenRegion={PortLogField(knownRegionText)}; {state}");

            if (!AppSettings.Debug.EnableMissingAssetPrompts)
            {
                AppLogger.Info($"MissingAssetPromptSkipped: asset={PortLogField(missingAssetName)}; flow={PortLogField(callingFlow)}; reason=disabled by config");
                return;
            }

            if (portCombatRunning || portCombatStopping)
            {
                AppLogger.Info($"MissingAssetPromptSkipped: asset={PortLogField(missingAssetName)}; flow={PortLogField(callingFlow)}; reason=combat active; combatRunning={portCombatRunning}; combatStopping={portCombatStopping}");
                return;
            }

            lock (portMissingAssetPromptLock)
            {
                if (portMissingAssetPromptHandled.Contains(promptKey) || portMissingAssetPromptActive.Contains(promptKey))
                {
                    AppLogger.Info($"MissingAssetPromptSkipped: asset={PortLogField(missingAssetName)}; flow={PortLogField(callingFlow)}; reason=already prompted this session");
                    return;
                }

                portMissingAssetPromptActive.Add(promptKey);
            }

            void ShowPrompt()
            {
                if (portCombatRunning || portCombatStopping)
                {
                    AppLogger.Info($"MissingAssetPromptSkipped: asset={PortLogField(missingAssetName)}; flow={PortLogField(callingFlow)}; reason=combat became active before prompt");
                    PortCompleteMissingAssetPrompt(promptKey);
                    return;
                }

                string targetFolder = string.IsNullOrWhiteSpace(normalizedImagePath)
                    ? ""
                    : Path.GetDirectoryName(normalizedImagePath) ?? "";
                string targetText = string.IsNullOrWhiteSpace(targetFolder)
                    ? "The target image folder is unknown, so a timestamped capture will be saved under Images."
                    : $"The capture will be saved in:\r\n{targetFolder}";
                string instruction = string.IsNullOrWhiteSpace(captureInstruction)
                    ? "Capture the current Diablo window or the known scan region showing the missing UI/template as clearly as possible."
                    : captureInstruction;

                Form prompt = new()
                {
                    Text = "Missing Screenshot/Template Asset",
                    StartPosition = FormStartPosition.CenterScreen,
                    Size = new System.Drawing.Size(560, 300),
                    TopMost = true,
                    ShowInTaskbar = false,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                };

                Label label = new()
                {
                    AutoSize = false,
                    Location = new DrawingPoint(18, 18),
                    Size = new System.Drawing.Size(520, 165),
                    Text = $"Missing asset:\r\n{missingAssetName}\r\n\r\nFlow: {callingFlow}\r\nScan region: {knownRegionText}\r\n\r\n{instruction}\r\n\r\n{targetText}",
                };

                Button captureButton = new()
                {
                    Text = "Capture Now",
                    Location = new DrawingPoint(300, 210),
                    Size = new System.Drawing.Size(110, 32),
                };

                Button skipButton = new()
                {
                    Text = "Skip",
                    Location = new DrawingPoint(420, 210),
                    Size = new System.Drawing.Size(90, 32),
                };

                captureButton.Click += (_, _) =>
                {
                    try
                    {
                        if (portCombatRunning || portCombatStopping)
                        {
                            AppLogger.Info($"MissingAssetManualCaptureSkipped: asset={PortLogField(missingAssetName)}; flow={PortLogField(callingFlow)}; reason=combat active at capture time; combatRunning={portCombatRunning}; combatStopping={portCombatStopping}");
                            return;
                        }

                        string savedPath = PortCaptureMissingAssetTemplate(normalizedImagePath, missingAssetName, callingFlow, knownScreenRegion);
                        AppLogger.Info($"MissingAssetManualCaptureAccepted: asset={PortLogField(missingAssetName)}; flow={PortLogField(callingFlow)}; savedPath={PortLogField(PortDisplayLocation(savedPath))}; scanRegion={PortLogField(knownRegionText)}");
                    }
                    finally
                    {
                        PortCompleteMissingAssetPrompt(promptKey);
                        prompt.Close();
                    }
                };

                skipButton.Click += (_, _) =>
                {
                    AppLogger.Info($"MissingAssetManualCaptureSkipped: asset={PortLogField(missingAssetName)}; flow={PortLogField(callingFlow)}; reason=user declined");
                    PortCompleteMissingAssetPrompt(promptKey);
                    prompt.Close();
                };

                prompt.FormClosed += (_, _) => PortCompleteMissingAssetPrompt(promptKey);
                prompt.Controls.Add(label);
                prompt.Controls.Add(captureButton);
                prompt.Controls.Add(skipButton);
                prompt.Show(this);
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(ShowPrompt));
            }
            else
            {
                ShowPrompt();
            }
        }

        private void PortCompleteMissingAssetPrompt(string promptKey)
        {
            lock (portMissingAssetPromptLock)
            {
                portMissingAssetPromptActive.Remove(promptKey);
                portMissingAssetPromptHandled.Add(promptKey);
            }
        }

        private string PortCaptureMissingAssetTemplate(string imagePath, string missingAssetName, string callingFlow, Rectangle? knownScreenRegion)
        {
            Rectangle captureRegion = PortMissingAssetCaptureRegion(imagePath, knownScreenRegion);
            if (captureRegion.Width <= 0 || captureRegion.Height <= 0)
            {
                AppLogger.Info($"MissingAssetManualCaptureFailed: asset={PortLogField(missingAssetName)}; flow={PortLogField(callingFlow)}; reason=empty capture region");
                return "";
            }

            string directory = PortMissingAssetTargetDirectory(imagePath);
            Directory.CreateDirectory(directory);

            string fileName = PortMissingAssetTargetFileName(imagePath, missingAssetName, callingFlow);
            string path = Path.Combine(directory, fileName);
            if (File.Exists(path))
            {
                string extension = Path.GetExtension(path);
                string withoutExtension = Path.GetFileNameWithoutExtension(path);
                path = Path.Combine(directory, $"{withoutExtension}_{DateTime.Now:yyyyMMdd_HHmmss_fff}{extension}");
            }

            using Bitmap screenshot = new(captureRegion.Width, captureRegion.Height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(captureRegion.Left, captureRegion.Top, 0, 0, screenshot.Size);
            }

            screenshot.Save(path, PortImageFormatForPath(path));
            return path;
        }

        private Rectangle PortMissingAssetCaptureRegion(string imagePath, Rectangle? knownScreenRegion)
        {
            if (knownScreenRegion.HasValue)
            {
                Rectangle known = Rectangle.Intersect(SystemInformation.VirtualScreen, knownScreenRegion.Value);
                if (known.Width > 0 && known.Height > 0)
                {
                    return known;
                }
            }

            IntPtr diabloWindow = FindDiabloWindow();
            if (diabloWindow != IntPtr.Zero && GetWindowRect(diabloWindow, out RECT rect))
            {
                Rectangle? referenceRegion = string.IsNullOrWhiteSpace(imagePath) ? null : PortScanRegionForImage(imagePath);
                Rectangle region = referenceRegion.HasValue
                    ? PortScaleReferenceRectangle(referenceRegion.Value, rect)
                    : new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                region = Rectangle.Intersect(SystemInformation.VirtualScreen, region);
                if (region.Width > 0 && region.Height > 0)
                {
                    return region;
                }
            }

            return SystemInformation.VirtualScreen;
        }

        private string PortMissingAssetTargetDirectory(string imagePath)
        {
            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                string? directory = Path.GetDirectoryName(imagePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    return directory;
                }
            }

            return Path.Combine(AppSettings.ImagesRootPath, "Manual Captures");
        }

        private string PortMissingAssetTargetFileName(string imagePath, string missingAssetName, string callingFlow)
        {
            string requestedName = string.IsNullOrWhiteSpace(imagePath) ? "" : Path.GetFileName(imagePath);
            if (!string.IsNullOrWhiteSpace(requestedName))
            {
                string extension = Path.GetExtension(requestedName);
                return string.IsNullOrWhiteSpace(extension)
                    ? $"{PortSafeFileName(requestedName)}.png"
                    : requestedName;
            }

            return $"{DateTime.Now:yyyyMMdd_HHmmss_fff}_{PortSafeFileName(callingFlow)}_{PortSafeFileName(missingAssetName)}.png";
        }

        private static System.Drawing.Imaging.ImageFormat PortImageFormatForPath(string path)
        {
            string extension = Path.GetExtension(path);
            if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return System.Drawing.Imaging.ImageFormat.Jpeg;
            }

            if (extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
            {
                return System.Drawing.Imaging.ImageFormat.Bmp;
            }

            return System.Drawing.Imaging.ImageFormat.Png;
        }

        private string PortMissingAssetStateFields()
        {
            return $"automationRunning={isAutomationRunning}; combatRunning={portCombatRunning}; combatStopping={portCombatStopping}; workflow={PortLogField(PortDisplayLocation(portLastWorkflowStep))}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}; foregroundWindow={PortLogField(PortForegroundWindowTitle())}; lastConfirmedLocation={PortLogField(PortDisplayLocation(portLastConfirmedLocation))}; lastTeleportSource={PortLogField(PortDisplayLocation(portLastTeleportSource))}";
        }

        private static string PortDebugScreenshotCategory(string actionName)
        {
            if (actionName.Contains("BattleNet", StringComparison.OrdinalIgnoreCase) ||
                actionName.Contains("Battle.net", StringComparison.OrdinalIgnoreCase))
            {
                return "BattleNet";
            }

            if (actionName.Contains("DiabloLaunch", StringComparison.OrdinalIgnoreCase) ||
                actionName.Contains("Diablo Launch", StringComparison.OrdinalIgnoreCase))
            {
                return "DiabloLaunch";
            }

            if (actionName.Contains("Teleport", StringComparison.OrdinalIgnoreCase))
            {
                return "Teleport";
            }

            if (actionName.Contains("Repair", StringComparison.OrdinalIgnoreCase))
            {
                return "Repair";
            }

            if (actionName.Contains("Salvage", StringComparison.OrdinalIgnoreCase))
            {
                return "Salvage";
            }

            if (actionName.Contains("Stash", StringComparison.OrdinalIgnoreCase))
            {
                return "Stash";
            }

            if (actionName.Contains("StartGame", StringComparison.OrdinalIgnoreCase) ||
                actionName.Contains("Start Game", StringComparison.OrdinalIgnoreCase))
            {
                return "StartGame";
            }

            return "Unknown";
        }

        private static string PortSafeFileName(string value)
        {
            string safe = string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                safe = safe.Replace(invalid, '_');
            }

            safe = System.Text.RegularExpressions.Regex.Replace(safe, @"\s+", "_");
            safe = System.Text.RegularExpressions.Regex.Replace(safe, @"[^A-Za-z0-9_.-]+", "_");
            safe = safe.Trim('_');
            return string.IsNullOrWhiteSpace(safe) ? "Unknown" : safe;
        }

        private string PortForegroundWindowTitle()
        {
            IntPtr foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                return "Unknown";
            }

            System.Text.StringBuilder title = new(256);
            int length = GetWindowText(foreground, title, title.Capacity);
            return length > 0 ? title.ToString() : "Unknown";
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

        private static string FormatPoint(DrawingPoint point)
        {
            return $"{point.X},{point.Y}";
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

        // Close Battlnet after starting Diablo
        private void CloseBattleNet()
        {
            int closeRequested = 0;
            foreach (Process process in Process.GetProcessesByName("Battle.net"))
            {
                try
                {
                    process.CloseMainWindow();
                    closeRequested++;
                }
                catch
                {
                    // Ignore if Battle.net cannot be closed.
                }
            }

            battleNetCloseRequested = closeRequested > 0;
            AppLogger.Info($"Battle.net close requested: processes={closeRequested}; battleNetCloseRequested={battleNetCloseRequested}");

            Stopwatch sw = Stopwatch.StartNew();
            bool stillRunning;
            IntPtr stillVisibleWindow;
            do
            {
                Thread.Sleep(AppSettings.Launch.BattleNetClosePollIntervalMs);
                stillRunning = IsBattleNetRunning();
                stillVisibleWindow = FindBattleNetWindow();
                if (stillVisibleWindow == IntPtr.Zero)
                {
                    break;
                }
            }
            while (sw.ElapsedMilliseconds < AppSettings.Launch.BattleNetCloseTimeoutMs);

            battleNetCloseProcessRemaining = stillRunning;
            battleNetCloseVisibleWindowRemaining = stillVisibleWindow != IntPtr.Zero;
            battleNetCloseTimedOut = battleNetCloseVisibleWindowRemaining;
            battleNetCloseSucceeded = !battleNetCloseVisibleWindowRemaining;
            AppLogger.Info($"Battle.net close result: closeRequested={closeRequested}; battleNetCloseRequested={battleNetCloseRequested}; battleNetCloseSucceeded={battleNetCloseSucceeded}; battleNetCloseTimedOut={battleNetCloseTimedOut}; battleNetCloseProcessRemaining={battleNetCloseProcessRemaining}; battleNetCloseProcessRemainingIsFailure=False; battleNetCloseVisibleWindowRemaining={battleNetCloseVisibleWindowRemaining}; visibleWindow=0x{stillVisibleWindow.ToInt64():X}; elapsedMs={sw.ElapsedMilliseconds}; likelyExplanation={(battleNetCloseVisibleWindowRemaining ? "Visible Battle.net window remained open after close request." : "Visible Battle.net window is closed/gone; background tray process state is informational.")}");

            if (portBattleNetLaunchFlowActive || battleNetLaunchOutcomeRecorded)
            {
                battleNetPostLaunchCloseEvaluated = true;
                AppLogger.Info($"BattleNetPostLaunchCloseSummary: event=BattleNetPostLaunchCloseSummary; launchSuccessful={battleNetPlayClickAcceptedByBattleNet && diabloLaunchedAfterAppPlayClick}; battleNetPlayClickSentByApp={battleNetPlayClickSentByApp}; battleNetPlayClickAcceptedByBattleNet={battleNetPlayClickAcceptedByBattleNet}; battleNetPlayClickAcceptedReason={PortLogField(battleNetPlayClickAcceptedReason)}; battleNetPlayClickPoint={(battleNetPlayClickSentByApp ? $"{battleNetPlayClickPoint.X},{battleNetPlayClickPoint.Y}" : "Unknown")}; battleNetPlayClickTimestamp={(battleNetPlayClickTimestamp.HasValue ? battleNetPlayClickTimestamp.Value.ToString("O") : "Unknown")}; battleNetPlayClickAcceptedTimestamp={(battleNetPlayClickAcceptedTimestamp.HasValue ? battleNetPlayClickAcceptedTimestamp.Value.ToString("O") : "Unknown")}; diabloLaunched={diabloLaunchedAfterAppPlayClick || diabloLaunchedWithoutAppPlayClick}; diabloLaunchedAfterAppPlayClick={diabloLaunchedAfterAppPlayClick}; diabloLaunchedWithoutAppPlayClick={diabloLaunchedWithoutAppPlayClick}; battleNetManualPlaySuspected={battleNetManualPlaySuspected}; battleNetStillOpenAfterLaunch={battleNetStillOpenAfterLaunch}; battleNetPostLaunchCloseEvaluated={battleNetPostLaunchCloseEvaluated}; {PortBattleNetCloseSummaryFields()}; likelyExplanation=Battle.net close state recorded after launch handling.");
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
                PortOfferMissingAssetCapture(imagePath, "FullScreenImageExists", Path.GetFileName(imagePath), captureInstruction: "Capture the missing fullscreen template while the expected UI element is visible.");
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
                PortOfferMissingAssetCapture(imagePath, "FullScreenImageSearch", Path.GetFileName(imagePath), captureInstruction: "Capture the missing fullscreen template while the expected UI element is visible.");
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

            if (!File.Exists(imagePath))
            {
                PortOfferMissingAssetCapture(
                    imagePath,
                    "DiabloWindowImageSearch",
                    imageName,
                    captureInstruction: "Capture the missing Diablo template while the expected in-game UI element is visible.");
                return false;
            }

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
                PortOfferMissingAssetCapture(imagePath, "BitmapImageSearch", Path.GetFileName(imagePath), captureInstruction: "Capture the missing template for this bitmap/image search.");
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
            return parts.Length == 0
                ? AppSettings.ImagesRootPath
                : Path.Combine(AppSettings.ImagesRootPath, Path.Combine(parts));
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

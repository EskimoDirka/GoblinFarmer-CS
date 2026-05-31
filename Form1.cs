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

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
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

        // Used to activate the Diablo window when found
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

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

            AddWorkflowStep("Starting Battle.net");
            StartBattleNet();

            AddWorkflowStep("Waiting for Battle.net Play Button");
            SetAppStatus("Waiting For Battle.net Play Button");

            bool clickedPlay = WaitForImageAndClick(
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
                    SetAppStatus("Diablo III Started");
                    return true;
                }

                Thread.Sleep(1000);
            }

            MessageBox.Show("Diablo III did not start within the timeout.");
            SetAppStatus("Diablo Start Timeout");
            return false;
        }

        private void StartBattleNet()
        {
            string battleNetPath = @"D:\Battle.net\Battle.net.exe";

            if (!File.Exists(battleNetPath))
            {
                MessageBox.Show("Battle.net not found.");
                return;
            }

            Process.Start(battleNetPath);
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

        // Image Path Builder Helper
        private const string ImagesPath = @"D:\D3\Projects\Images";

        private string Img(string folder, string fileName)
        {
            return Path.Combine(ImagesPath, folder, fileName);
        }

        // Folder Scanner ==========================================
        private string[] GetImagesFromFolder(string folder)
        {
            return Directory.GetFiles(
                Path.Combine(ImagesPath, folder),
                "*.png",
                SearchOption.AllDirectories);
        }
    }
}

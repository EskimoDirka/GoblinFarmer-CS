using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private int sessionGamesCreated;
        private int sessionTeleportsCompleted;
        private int sessionBlockedTeleports;
        private int sessionFailures;
        private DateTime sessionStartTime;
        private readonly DateTime sessionScreenshotRetentionStartTime = DateTime.Now;

        private void PortInitializeSessionStats()
        {
            sessionStartTime = DateTime.Now;
            PortWriteSessionMetadata();
            PortUpdateSessionStats();
        }

        private void PortIncrementGamesCreated()
        {
            Interlocked.Increment(ref sessionGamesCreated);
            PortUpdateSessionStats();
        }

        private void PortIncrementTeleportsCompleted()
        {
            Interlocked.Increment(ref sessionTeleportsCompleted);
            PortUpdateSessionStats();
        }

        private void PortIncrementBlockedTeleports()
        {
            Interlocked.Increment(ref sessionBlockedTeleports);
            PortUpdateSessionStats();
        }

        private void PortIncrementFailures()
        {
            int failures = Interlocked.Increment(ref sessionFailures);
            AppLogger.Info($"Failure counter incremented: {failures}");
            PortUpdateSessionStats();
        }

        private int PortFailureCount()
        {
            return Volatile.Read(ref sessionFailures);
        }

        private void PortUpdateSessionStats()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(PortUpdateSessionStats));
                return;
            }

            TimeSpan runtime = sessionStartTime == default ? TimeSpan.Zero : DateTime.Now - sessionStartTime;
            lblSessionGames.Text = $"Games: {sessionGamesCreated}";
            lblSessionTeleports.Text = $"Teleports: {sessionTeleportsCompleted}";
            lblSessionBlocked.Text = $"Blocked: {sessionBlockedTeleports}";
            lblSessionFailures.Text = $"Failures: {sessionFailures}";
            lblSessionRuntime.Text = $"Runtime: {runtime:hh\\:mm\\:ss}";
        }

        private void PortLogSessionSummary()
        {
            TimeSpan runtime = sessionStartTime == default ? TimeSpan.Zero : DateTime.Now - sessionStartTime;
            AppLogger.Info("========== Session Summary ==========");
            AppLogger.Info($"Games Created: {sessionGamesCreated}");
            AppLogger.Info($"Teleports Completed: {sessionTeleportsCompleted}");
            AppLogger.Info($"Blocked Teleports: {sessionBlockedTeleports}");
            AppLogger.Info($"Failures: {sessionFailures}");
            AppLogger.Info($"Runtime: {runtime:hh\\:mm\\:ss}");
            AppLogger.Info("=====================================");
        }

        private void PortWriteSessionMetadata()
        {
            try
            {
                string metadataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "session-info.txt");
                string[] lines =
                [
                    $"SessionStartLocal={sessionStartTime:O}",
                    $"SessionStartUtc={sessionStartTime.ToUniversalTime():O}",
                    $"ProcessId={Environment.ProcessId}",
                    $"BaseDirectory={AppDomain.CurrentDomain.BaseDirectory}"
                ];
                File.WriteAllLines(metadataPath, lines);
                AppLogger.Info($"Session metadata written: {metadataPath}; sessionStartLocal={sessionStartTime:O}");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Session metadata write failed.", ex);
            }
        }

        private string PortCaptureFailureScreenshot(string failureType, string workflow = "Workflow")
        {
            portDiagnosticLatestFailureScreenshotType = string.IsNullOrWhiteSpace(failureType) ? "Unknown" : failureType;
            CaptureDebugScreenshot(workflow, portDiagnosticLatestFailureScreenshotType);
            PortScreenshotPair pair = PortCaptureDiagnosticScreenshotPair("Failure", workflow, portDiagnosticLatestFailureScreenshotType);
            if (!string.IsNullOrWhiteSpace(pair.DiabloPath) || !string.IsNullOrWhiteSpace(pair.AppPath))
            {
                string path = !string.IsNullOrWhiteSpace(pair.DiabloPath) ? pair.DiabloPath : pair.AppPath;
                AppLogger.Info($"Failure screenshot saved: type={portDiagnosticLatestFailureScreenshotType}; path={path}");
            }

            return pair.DiabloPath;
        }

        private PortScreenshotPair PortCaptureSuccessScreenshot(string workflow, string action)
        {
            return PortCaptureDiagnosticScreenshotPair("Success", workflow, action);
        }

        private string PortCaptureDebugScreenshot(string reason)
        {
            try
            {
                if (!AppSettings.Debug.EnableDebugScreenshots)
                {
                    AppLogger.Info($"Debug screenshot skipped: disabled by AppSettings; reason={reason}");
                    return "";
                }

                if (chkKeepDebugScreenshots != null && !chkKeepDebugScreenshots.Checked)
                {
                    AppLogger.Info($"Debug screenshot skipped: disabled by Keep Debug Screenshots setting; reason={reason}");
                    return "";
                }

                IntPtr diabloWindow = FindDiabloWindow();
                RECT rect;
                if (diabloWindow != IntPtr.Zero && PortTryGetDiabloClientScreenRect(diabloWindow, reason, out rect))
                {
                    AppLogger.Info($"Debug screenshot capturing Diablo client: reason={reason}");
                }
                else
                {
                    Rectangle screen = SystemInformation.VirtualScreen;
                    rect = new RECT
                    {
                        Left = screen.Left,
                        Top = screen.Top,
                        Right = screen.Right,
                        Bottom = screen.Bottom,
                    };
                    AppLogger.Info($"Debug screenshot capturing virtual screen fallback: reason={reason}; bounds={screen.Left},{screen.Top},{screen.Width},{screen.Height}");
                }

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                if (width <= 0 || height <= 0)
                {
                    AppLogger.Info($"Debug screenshot skipped: capture rectangle invalid; reason={reason}");
                    return "";
                }

                string screenshotDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                Directory.CreateDirectory(screenshotDirectory);

                string safeReason = string.Join("_", reason.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                if (string.IsNullOrWhiteSpace(safeReason))
                {
                    safeReason = "Debug";
                }

                string fileName = $"Debug_{DateTime.Now:yyyy-MM-dd_HHmmss}_{safeReason}.png";
                string path = Path.Combine(screenshotDirectory, fileName);

                using Bitmap screenshot = new(width, height);
                using (Graphics graphics = Graphics.FromImage(screenshot))
                {
                    graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, screenshot.Size);
                }

                screenshot.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                portDiagnosticLatestScreenshotPath = path;
                AppLogger.Info($"Debug screenshot saved: {path}");
                return path;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Debug screenshot capture failed: {reason}", ex);
                return "";
            }
        }

        private PortScreenshotPair PortCaptureDiagnosticScreenshotPair(string outcome, string workflow, string action)
        {
            try
            {
                if (!AppSettings.Debug.EnableDebugScreenshots)
                {
                    AppLogger.Info($"Diagnostic screenshot pair skipped: disabled by AppSettings; outcome={outcome}; workflow={workflow}; action={action}");
                    return new PortScreenshotPair("", "");
                }

                if (chkKeepDebugScreenshots != null && !chkKeepDebugScreenshots.Checked)
                {
                    AppLogger.Info($"Diagnostic screenshot pair skipped: disabled by Keep Debug Screenshots setting; outcome={outcome}; workflow={workflow}; action={action}");
                    return new PortScreenshotPair("", "");
                }

                string screenshotDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                Directory.CreateDirectory(screenshotDirectory);

                DateTime timestamp = DateTime.Now;
                string safeOutcome = PortSafeScreenshotName(outcome, "Debug");
                string safeWorkflow = PortSafeScreenshotName(workflow, "Workflow");
                string safeAction = PortSafeScreenshotName(action, "Action");
                string filePrefix = $"{timestamp:yyyy-MM-dd_HHmmss_fff}_{safeOutcome}_{safeWorkflow}_{safeAction}";

                string diabloPath = Path.Combine(screenshotDirectory, $"{filePrefix}_Diablo.png");
                string appPath = Path.Combine(screenshotDirectory, $"{filePrefix}_App.png");

                string savedDiabloPath = PortCaptureDiabloScreenshotToFile(diabloPath, $"{outcome}:{workflow}:{action}");
                string savedAppPath = PortCaptureAppScreenshotToFile(appPath, $"{outcome}:{workflow}:{action}");

                string latestPath = !string.IsNullOrWhiteSpace(savedDiabloPath) ? savedDiabloPath : savedAppPath;
                if (!string.IsNullOrWhiteSpace(latestPath))
                {
                    portDiagnosticLatestScreenshotPath = latestPath;
                }

                AppLogger.Info($"Diagnostic screenshot pair saved: timestamp={timestamp:yyyy-MM-dd HH:mm:ss.fff}; outcome={safeOutcome}; workflow={safeWorkflow}; action={safeAction}; diablo={PortDisplayLocation(savedDiabloPath)}; app={PortDisplayLocation(savedAppPath)}");
                return new PortScreenshotPair(savedDiabloPath, savedAppPath);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Diagnostic screenshot pair capture failed: outcome={outcome}; workflow={workflow}; action={action}", ex);
                return new PortScreenshotPair("", "");
            }
        }

        private string PortCaptureDiabloScreenshotToFile(string path, string reason)
        {
            IntPtr diabloWindow = FindDiabloWindow();
            RECT rect;
            if (diabloWindow != IntPtr.Zero && PortTryGetDiabloClientScreenRect(diabloWindow, reason, out rect))
            {
                AppLogger.Info($"Diagnostic screenshot capturing Diablo client: reason={reason}");
            }
            else
            {
                Rectangle screen = SystemInformation.VirtualScreen;
                rect = new RECT
                {
                    Left = screen.Left,
                    Top = screen.Top,
                    Right = screen.Right,
                    Bottom = screen.Bottom,
                };
                AppLogger.Info($"Diagnostic screenshot capturing virtual screen fallback for Diablo evidence: reason={reason}; bounds={screen.Left},{screen.Top},{screen.Width},{screen.Height}");
            }

            return PortCaptureScreenRectangleToFile(rect, path, reason);
        }

        private string PortCaptureAppScreenshotToFile(string path, string reason)
        {
            if (InvokeRequired)
            {
                return (string)Invoke(new Func<string>(() => PortCaptureAppScreenshotToFile(path, reason)));
            }

            Rectangle bounds = Bounds;
            IntPtr appHandle = Handle;
            IntPtr foregroundWindow = GetForegroundWindow();
            bool visible = Visible && IsWindowVisible(appHandle);
            bool minimized = WindowState == FormWindowState.Minimized || IsIconic(appHandle);
            bool foreground = foregroundWindow == appHandle;
            bool possiblyOccluded = visible && !minimized && !foreground;
            RECT rect = new()
            {
                Left = bounds.Left,
                Top = bounds.Top,
                Right = bounds.Right,
                Bottom = bounds.Bottom,
            };

            AppLogger.Info($"Diagnostic screenshot capturing app window: reason={reason}; visible={visible}; minimized={minimized}; foreground={foreground}; possiblyOccluded={possiblyOccluded}; bounds={bounds.Left},{bounds.Top},{bounds.Width},{bounds.Height}; foregroundWindow=0x{foregroundWindow.ToInt64():X}; appWindow=0x{appHandle.ToInt64():X}");
            if (!visible || minimized || possiblyOccluded)
            {
                AppLogger.Info($"Diagnostic screenshot app window warning: reason={reason}; mayCaptureAnotherWindow=true; visible={visible}; minimized={minimized}; possiblyOccluded={possiblyOccluded}; bounds={bounds.Left},{bounds.Top},{bounds.Width},{bounds.Height}");
            }

            return PortCaptureScreenRectangleToFile(rect, path, reason);
        }

        private string PortCaptureScreenRectangleToFile(RECT rect, string path, string reason)
        {
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0)
            {
                AppLogger.Info($"Diagnostic screenshot skipped: capture rectangle invalid; reason={reason}; path={path}");
                return "";
            }

            using Bitmap screenshot = new(width, height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, screenshot.Size);
            }

            screenshot.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            return path;
        }

        private static string PortSafeScreenshotName(string value, string fallback)
        {
            string safe = string.Join("_", (value ?? "").Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            safe = safe.Replace(" ", "");
            return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
        }

        private bool PortTryGetDiabloClientScreenRect(IntPtr diabloWindow, string reason, out RECT rect)
        {
            rect = new RECT();
            GetWindowThreadProcessId(diabloWindow, out uint processId);
            try
            {
                using System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById((int)processId);
                if (!process.ProcessName.Equals("Diablo III", StringComparison.OrdinalIgnoreCase) &&
                    !process.ProcessName.Equals("Diablo III64", StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Info($"Debug screenshot skipped Diablo client: handle is not Diablo; process={process.ProcessName}; reason={reason}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Debug screenshot skipped Diablo client: process verification failed; reason={reason}", ex);
                return false;
            }

            if (!GetClientRect(diabloWindow, out RECT clientRect))
            {
                AppLogger.Info($"Debug screenshot skipped Diablo client: client rectangle unavailable; reason={reason}");
                return false;
            }

            DrawingPoint clientTopLeft = new(clientRect.Left, clientRect.Top);
            if (!ClientToScreen(diabloWindow, ref clientTopLeft))
            {
                AppLogger.Info($"Debug screenshot skipped Diablo client: client origin unavailable; reason={reason}");
                return false;
            }

            rect = new RECT
            {
                Left = clientTopLeft.X,
                Top = clientTopLeft.Y,
                Right = clientTopLeft.X + (clientRect.Right - clientRect.Left),
                Bottom = clientTopLeft.Y + (clientRect.Bottom - clientRect.Top),
            };
            return true;
        }

        private void PortCleanupOldDebugScreenshots(int retentionDays)
        {
            try
            {
                string screenshotDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                if (!Directory.Exists(screenshotDirectory))
                {
                    AppLogger.Info($"Screenshot retention cleanup complete: deleted=0, retentionDays={retentionDays}");
                    return;
                }

                DateTime cutoff = DateTime.Now.AddDays(-retentionDays);
                int deleted = 0;
                foreach (string file in Directory.GetFiles(screenshotDirectory, "*.png"))
                {
                    try
                    {
                        DateTime lastWrite = File.GetLastWriteTime(file);
                        if (lastWrite >= sessionScreenshotRetentionStartTime || lastWrite >= cutoff)
                        {
                            continue;
                        }

                        File.Delete(file);
                        deleted++;
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"Failed to delete old screenshot: {file}", ex);
                    }
                }

                AppLogger.Info($"Screenshot retention cleanup complete: deleted={deleted}, retentionDays={retentionDays}");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Screenshot retention cleanup failed.", ex);
            }
        }

        private sealed record PortScreenshotPair(string DiabloPath, string AppPath);
    }
}

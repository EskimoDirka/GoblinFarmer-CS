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

        private void PortCaptureDebugScreenshot(string reason)
        {
            try
            {
                if (chkKeepDebugScreenshots != null && !chkKeepDebugScreenshots.Checked)
                {
                    AppLogger.Info($"Debug screenshot skipped: disabled by Keep Debug Screenshots setting; reason={reason}");
                    return;
                }

                IntPtr diabloWindow = FindDiabloWindow();
                if (diabloWindow == IntPtr.Zero)
                {
                    AppLogger.Info($"Debug screenshot skipped: Diablo window unavailable; reason={reason}");
                    return;
                }

                GetWindowThreadProcessId(diabloWindow, out uint processId);
                try
                {
                    using System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById((int)processId);
                    if (!process.ProcessName.Equals("Diablo III", StringComparison.OrdinalIgnoreCase) &&
                        !process.ProcessName.Equals("Diablo III64", StringComparison.OrdinalIgnoreCase))
                    {
                        AppLogger.Info($"Debug screenshot skipped: handle is not Diablo; process={process.ProcessName}; reason={reason}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Debug screenshot skipped: Diablo process verification failed; reason={reason}", ex);
                    return;
                }

                if (!GetClientRect(diabloWindow, out RECT clientRect))
                {
                    AppLogger.Info($"Debug screenshot skipped: Diablo client rectangle unavailable; reason={reason}");
                    return;
                }

                DrawingPoint clientTopLeft = new(clientRect.Left, clientRect.Top);
                if (!ClientToScreen(diabloWindow, ref clientTopLeft))
                {
                    AppLogger.Info($"Debug screenshot skipped: Diablo client origin unavailable; reason={reason}");
                    return;
                }

                RECT rect = new()
                {
                    Left = clientTopLeft.X,
                    Top = clientTopLeft.Y,
                    Right = clientTopLeft.X + (clientRect.Right - clientRect.Left),
                    Bottom = clientTopLeft.Y + (clientRect.Bottom - clientRect.Top),
                };

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                if (width <= 0 || height <= 0)
                {
                    AppLogger.Info($"Debug screenshot skipped: Diablo window rectangle invalid; reason={reason}");
                    return;
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
                AppLogger.Info($"Debug screenshot saved: {path}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Debug screenshot capture failed: {reason}", ex);
            }
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
    }
}

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private int sessionGamesCreated;
        private int sessionTeleportsCompleted;
        private int sessionBlockedTeleports;
        private int sessionFailures;
        private DateTime sessionStartTime;

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
            Interlocked.Increment(ref sessionFailures);
            PortUpdateSessionStats();
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
                IntPtr diabloWindow = FindDiabloWindow();
                if (diabloWindow == IntPtr.Zero || !GetWindowRect(diabloWindow, out RECT rect))
                {
                    AppLogger.Info($"Debug screenshot skipped: Diablo window unavailable; reason={reason}");
                    return;
                }

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
    }
}

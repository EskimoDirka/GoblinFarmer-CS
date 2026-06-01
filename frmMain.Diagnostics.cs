namespace GoblinFarmer
{
    public partial class frmMain
    {
        private readonly Dictionary<string, Label> portDiagnosticLabels = new(StringComparer.OrdinalIgnoreCase);
        private DateTime portLastDiagnosticFileScan = DateTime.MinValue;
        private string portDiagnosticLastLogFile = "none";
        private string portDiagnosticLatestPackagePath = "none";
        private string portLastWorkflowStep = "Idle";
        private int portDiagnosticLogCount;
        private int portDiagnosticScreenshotCount;

        private void PortInitializeDiagnosticOverlay()
        {
            if (portDiagnosticLabels.Count > 0)
            {
                return;
            }

            ClientSize = new Size(1350, 567);
            MinimumSize = new Size(1080, 606);

            GroupBox grpDiagnostics = new()
            {
                Location = new Point(914, 63),
                Name = "grpDiagnostics",
                Size = new Size(420, 493),
                TabStop = false,
                Text = "Diagnostic Overlay",
            };

            TableLayoutPanel table = new()
            {
                AutoScroll = true,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 12, 10, 10),
                RowCount = 0,
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            grpDiagnostics.Controls.Add(table);
            Controls.Add(grpDiagnostics);

            PortAddDiagnosticRow(table, "Raw Location", "RawLocation");
            PortAddDiagnosticRow(table, "Normalized Location", "NormalizedLocation");
            PortAddDiagnosticRow(table, "Display Location", "DisplayLocation");
            PortAddDiagnosticRow(table, "Blocking Location", "BlockingLocation");
            PortAddDiagnosticRow(table, "Current Teleport Target", "CurrentTeleportTarget");
            PortAddDiagnosticRow(table, "Next Teleport Target", "NextTeleportTarget");
            PortAddDiagnosticRow(table, "Queued Retry Target", "QueuedRetryTarget");
            PortAddDiagnosticRow(table, "Last Requested Target", "LastRequestedTarget");
            PortAddDiagnosticRow(table, "Failed/Interrupted", "FailedInterruptedState");
            PortAddDiagnosticRow(table, "Route State", "RouteState", 42);
            PortAddDiagnosticRow(table, "Combat State", "CombatState", 42);
            PortAddDiagnosticRow(table, "Failure Counter", "FailureCounter");
            PortAddDiagnosticRow(table, "Diablo Running Status", "DiabloRunningStatus");
            PortAddDiagnosticRow(table, "Active Workflow", "ActiveWorkflow", 42);
            PortAddDiagnosticRow(table, "Last Log File", "LastLogFile", 42);
            PortAddDiagnosticRow(table, "Screenshot Count", "ScreenshotCount");
            PortAddDiagnosticRow(table, "Log Count", "LogCount");
            PortAddDiagnosticRow(table, "Debug Package Path", "DebugPackagePath", 58);
        }

        private void PortAddDiagnosticRow(TableLayoutPanel table, string labelText, string key, int rowHeight = 26)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));

            Label label = new()
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font(Font, FontStyle.Bold),
                Text = labelText,
                TextAlign = ContentAlignment.TopLeft,
            };

            Label value = new()
            {
                AutoEllipsis = true,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "Unknown",
                TextAlign = ContentAlignment.TopLeft,
                UseMnemonic = false,
            };

            table.Controls.Add(label, 0, row);
            table.Controls.Add(value, 1, row);
            portDiagnosticLabels[key] = value;
        }

        private void PortUpdateDiagnosticOverlay(bool diabloRunning)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => PortUpdateDiagnosticOverlay(diabloRunning)));
                return;
            }

            if (portDiagnosticLabels.Count == 0)
            {
                return;
            }

            PortRefreshDiagnosticFileStatsIfNeeded();

            string rawLocation = PortDisplayLocation(portLastConfirmedLocation);
            string normalizedLocation = PortDisplayLocation(PortNormalizeBlockingLocation(portLastConfirmedLocation));
            string displayLocation = PortDisplayLocation(PortGetButtonLocationForDetectedLocation(portLastConfirmedLocation));
            string blockingLocation = PortDisplayLocation(PortGetConfirmedCurrentLocation());
            string currentTeleportTarget = PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey));
            string nextTeleportTarget = PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey));
            string queuedRetryTarget = PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey));
            string lastRequestedTarget = PortDisplayLocation(PortTeleportLocationForKey(portLastRequestedTeleportKey));
            string failedInterruptedState = portTeleportRetryFailedOrInterrupted ? "True" : "False";
            string routeState = $"CurrentKey={PortDisplayLocation(portLastTeleportKey)}; NextKey={PortDisplayLocation(portQueuedTeleportKey)}; RetryKey={PortDisplayLocation(portQueuedRetryTeleportKey)}; FailsafeBlocked={portAutomationBlockedByTeleportFailsafe}";
            string combatState = $"Running={portCombatRunning}; Stopping={portCombatStopping}; Class={PortDisplayLocation(portCombatClass)}; LootLeftDown={portLootSpamLeftClickDown}";
            string appStatus = lblAppStatus.Text.Replace("App Status:", "", StringComparison.OrdinalIgnoreCase).Trim();
            string activeWorkflow = string.IsNullOrWhiteSpace(portLastWorkflowStep)
                ? appStatus
                : $"{appStatus} / {portLastWorkflowStep}";

            PortSetDiagnosticLabel("RawLocation", rawLocation);
            PortSetDiagnosticLabel("NormalizedLocation", normalizedLocation);
            PortSetDiagnosticLabel("DisplayLocation", displayLocation);
            PortSetDiagnosticLabel("BlockingLocation", blockingLocation);
            PortSetDiagnosticLabel("CurrentTeleportTarget", currentTeleportTarget);
            PortSetDiagnosticLabel("NextTeleportTarget", nextTeleportTarget);
            PortSetDiagnosticLabel("QueuedRetryTarget", queuedRetryTarget);
            PortSetDiagnosticLabel("LastRequestedTarget", lastRequestedTarget);
            PortSetDiagnosticLabel("FailedInterruptedState", failedInterruptedState);
            PortSetDiagnosticLabel("RouteState", routeState);
            PortSetDiagnosticLabel("CombatState", combatState);
            PortSetDiagnosticLabel("FailureCounter", PortFailureCount().ToString());
            PortSetDiagnosticLabel("DiabloRunningStatus", diabloRunning ? "Running" : "Not Running");
            PortSetDiagnosticLabel("ActiveWorkflow", activeWorkflow);
            PortSetDiagnosticLabel("LastLogFile", portDiagnosticLastLogFile);
            PortSetDiagnosticLabel("ScreenshotCount", portDiagnosticScreenshotCount.ToString());
            PortSetDiagnosticLabel("LogCount", portDiagnosticLogCount.ToString());
            PortSetDiagnosticLabel("DebugPackagePath", portDiagnosticLatestPackagePath);
        }

        private void PortSetDiagnosticLabel(string key, string value)
        {
            if (portDiagnosticLabels.TryGetValue(key, out Label? label))
            {
                label.Text = string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
            }
        }

        private void PortRefreshDiagnosticFileStatsIfNeeded()
        {
            if (DateTime.Now - portLastDiagnosticFileScan < TimeSpan.FromSeconds(5))
            {
                return;
            }

            portLastDiagnosticFileScan = DateTime.Now;
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string logDirectory = Path.Combine(baseDirectory, "Logs");
            string screenshotDirectory = Path.Combine(baseDirectory, "Screenshots");

            FileInfo[] logFiles = Directory.Exists(logDirectory)
                ? new DirectoryInfo(logDirectory).GetFiles("*.*").Where(file => file.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase) || file.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)).ToArray()
                : [];

            FileInfo[] screenshotFiles = Directory.Exists(screenshotDirectory)
                ? new DirectoryInfo(screenshotDirectory).GetFiles("*.*").Where(file => file.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) || file.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || file.Extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) || file.Extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)).ToArray()
                : [];

            portDiagnosticLogCount = logFiles.Length;
            portDiagnosticScreenshotCount = screenshotFiles.Length;
            portDiagnosticLastLogFile = logFiles.OrderByDescending(file => file.LastWriteTime).FirstOrDefault()?.FullName ?? "none";
            portDiagnosticLatestPackagePath = PortFindLatestDebugPackagePath();
        }

        private static string PortFindLatestDebugPackagePath()
        {
            DirectoryInfo? directory = new(AppDomain.CurrentDomain.BaseDirectory);
            for (int depth = 0; directory != null && depth < 8; depth++, directory = directory.Parent)
            {
                string packageDirectory = Path.Combine(directory.FullName, "DebugPackages");
                if (!Directory.Exists(packageDirectory))
                {
                    continue;
                }

                FileInfo? latestPackage = new DirectoryInfo(packageDirectory)
                    .GetFiles("GoblinFarmer_Debug_*.zip")
                    .OrderByDescending(file => file.LastWriteTime)
                    .FirstOrDefault();

                if (latestPackage != null)
                {
                    return latestPackage.FullName;
                }
            }

            return "none";
        }
    }
}

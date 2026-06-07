namespace GoblinFarmer
{
    public partial class frmMain
    {
        private readonly Dictionary<string, Label> portDiagnosticLabels = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Label> portRouteInspectorLabels = new(StringComparer.OrdinalIgnoreCase);
        private DateTime portLastDiagnosticFileScan = DateTime.MinValue;
        private string portDiagnosticLastLogFile = "none";
        private string portDiagnosticLatestScreenshotPath = "none";
        private string portDiagnosticLatestFailureScreenshotType = "none";
        private string portDiagnosticLatestPackagePath = "none";
        private string portLastWorkflowStep = "Idle";
        private string portLastTeleportSource = "Unknown";
        private string portLastBlockingDecision = "Unknown";
        private string portLastBlockingReason = "Unknown";
        private string portLastRouteDecisionOutput = "Unknown";
        private int portDiagnosticLogCount;
        private int portDiagnosticScreenshotCount;

        private void PortInitializeDiagnosticOverlay()
        {
            bool showOverlay = DebugManager.ShouldShowDiagnosticOverlay();
            bool showInspector = DebugManager.ShouldShowRouteInspector();
            bool showNextTests = AppSettings.IsVsDebugProfile;
            if (!showOverlay && !showInspector && !showNextTests)
            {
                AppLogger.Info("Diagnostic UI hidden by config: ShowDiagnosticOverlay=False; ShowRouteInspector=False; ShowNextTests=False");
                return;
            }

            if (portDiagnosticLabels.Count > 0 || portRouteInspectorLabels.Count > 0)
            {
                return;
            }

            int clientHeight = AppSettings.IsVsDebugProfile ? 836 : 676;
            int minimumHeight = AppSettings.IsVsDebugProfile ? 875 : 715;
            int diagnosticsHeight = AppSettings.IsVsDebugProfile ? 719 : 559;

            ClientSize = new Size(1350, clientHeight);
            MinimumSize = new Size(1080, minimumHeight);

            TabControl tabs = new()
            {
                Location = new Point(914, 63),
                Name = "tabDiagnostics",
                Size = new Size(420, diagnosticsHeight),
            };

            TabPage compactTab = new()
            {
                Name = "tabDiagnosticOverlay",
                Text = "Overlay",
                UseVisualStyleBackColor = true,
            };

            TabPage inspectorTab = new()
            {
                Name = "tabRouteStateInspector",
                Text = "Route State",
                UseVisualStyleBackColor = true,
            };

            TabPage nextTestsTab = new()
            {
                Name = "tabNextTestSteps",
                Text = "Next Tests",
                UseVisualStyleBackColor = true,
            };

            TableLayoutPanel table = PortCreateDiagnosticTable(labelWidth: 155F);
            TableLayoutPanel inspectorTable = PortCreateDiagnosticTable(labelWidth: 170F);

            compactTab.Controls.Add(table);
            inspectorTab.Controls.Add(inspectorTable);
            nextTestsTab.Controls.Add(PortCreateNextTestStepsPanel());
            if (showOverlay)
            {
                tabs.TabPages.Add(compactTab);
            }

            if (showInspector)
            {
                tabs.TabPages.Add(inspectorTab);
            }

            if (showNextTests)
            {
                tabs.TabPages.Add(nextTestsTab);
            }

            Controls.Add(tabs);

            PortAddDiagnosticRow(table, portDiagnosticLabels, "Raw Location", "RawLocation");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Normalized Location", "NormalizedLocation");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Display Location", "DisplayLocation");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Blocking Location", "BlockingLocation");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Current Teleport Target", "CurrentTeleportTarget");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Next Teleport Target", "NextTeleportTarget");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Queued Retry Target", "QueuedRetryTarget");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Last Requested Target", "LastRequestedTarget");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Failed/Interrupted", "FailedInterruptedState");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Route State", "RouteState", 42);
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Combat State", "CombatState", 42);
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Goblin Tracker", "GoblinTracker", 42);
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Goblin Evidence", "GoblinEvidence", 42);
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Failure Counter", "FailureCounter");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Diablo Running Status", "DiabloRunningStatus");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Active Workflow", "ActiveWorkflow", 42);
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Last Log File", "LastLogFile", 42);
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Latest Screenshot", "LatestScreenshotPath", 42);
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Latest Failure Type", "LatestFailureScreenshotType");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Screenshot Count", "ScreenshotCount");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Log Count", "LogCount");
            PortAddDiagnosticRow(table, portDiagnosticLabels, "Debug Package Path", "DebugPackagePath", 58);

            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Raw Detected Location", "RawDetectedLocation");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Normalized App Location", "NormalizedAppLocation");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Display Location", "DisplayLocation");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Blocking Location", "BlockingLocation");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Current Button Location", "CurrentButtonLocation");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Next Button Location", "NextButtonLocation");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Queued Teleport Target", "QueuedTeleportTarget");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Retry Queued Target", "RetryQueuedTarget");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Last Requested Target", "LastRequestedTeleportTarget");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Last Teleport Source", "LastTeleportSource");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Last Blocking Decision", "LastBlockingDecision", 42);
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Last Blocking Reason", "LastBlockingReason", 42);
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Last Route Output", "LastRouteDecisionOutput", 42);
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Waiting Confirmation", "WaitingConfirmation");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Waiting Target", "WaitingConfirmationTarget");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Retry State Active", "RetryStateActive");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Failure Counter", "FailureCounter");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Goblin Tracker", "GoblinTracker", 42);
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Goblin Evidence", "GoblinEvidence", 42);
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Latest Log Path", "LatestLogPath", 42);
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Latest Screenshot Path", "LatestScreenshotPath", 42);
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Latest Failure Type", "LatestFailureScreenshotType");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Screenshot Count", "ScreenshotCount");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Log Count", "LogCount");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Active Workflow", "ActiveWorkflow", 42);
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Diablo Running Status", "DiabloRunningStatus");
            PortAddDiagnosticRow(inspectorTable, portRouteInspectorLabels, "Diablo Active Status", "DiabloActiveStatus");
        }

        private Panel PortCreateNextTestStepsPanel()
        {
            Panel panel = new()
            {
                AutoScroll = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 10, 10, 10),
            };

            TableLayoutPanel table = new()
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Top,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            PortAddNextTestHeader(table, "Goblin Tracker Automatic Count Readiness");
            PortAddNextTestHeader(table, "Setup");
            PortAddNextTestCheck(table, "1. Observation Mode and Auto Goblin Count are on.");
            PortAddNextTestCheck(table, "2. Test Count Override is off before real automatic-count validation.");
            PortAddNextTestCheck(table, "3. Start a fresh game or press Reset Stats; confirm Goblins, GPH, and Last Observation reset.");

            PortAddNextTestHeader(table, "Route-order auto-count checks");
            PortAddNextTestCheck(table, "4. Southern Highlands / Cave Of The Moon Clan Level 2: after Level 1, find a fresh Level 2 goblin; expect Level 2 to count independently and never reuse Level 1 evidence.", 68);
            PortAddNextTestCheck(table, "5. Eastern Channel Level 2: find a fresh goblin, preferably Blood Thief; expect exactly one auto-count, notification, and Last Observation.", 58);
            PortAddNextTestCheck(table, "6. Stinging Winds: live goblins should count #1 and #2, then suppress #3 with AreaLimitReached.", 48);
            PortAddNextTestCheck(table, "7. Battlefields: find Treasure Goblin; expect one auto-count and notification, with no stale Treasure journal replay into later areas.", 58);
            PortAddNextTestCheck(table, "8. Pandemonium Fortress Level 1: live goblins should count #1 and #2, then suppress #3 with AreaLimitReached.", 54);
            PortAddNextTestCheck(table, "9. Pandemonium Fortress Level 2: live goblins should count #1 and #2, then suppress #3 with AreaLimitReached.", 54);

            PortAddNextTestHeader(table, "Safety and display checks");
            PortAddNextTestCheck(table, "10. Blocked areas with evidence: New Tristram and Caldeum blocked areas notify BlockedArea and never consume area slots.", 56);
            PortAddNextTestCheck(table, "11. Stale journal/area transition: count a goblin, move areas while old journal text remains visible, and confirm it does not count again or appear current in the wrong area.", 72);
            PortAddNextTestCheck(table, "12. Reset Stats and New Game: fresh evidence in the same area can count again after cleanup, while old evidence cannot.", 56);
            PortAddNextTestCheck(table, "13. Gilded Baron and Malevolent Tormentor: notification and Last Observation match the accepted evidence type.", 50);
            PortAddNextTestCheck(table, "14. Last Observation: latest real goblin stays readable until a new goblin, reset/new game, or confirmed area change replaces it.", 58);
            PortAddNextTestCheck(table, "15. Notification latency: note area, source, and goblin type if a count notification feels delayed.", 48);

            PortAddNextTestHeader(table, "Package rule");
            PortAddNextTestCheck(table, "16. After any miss, wrong type, stale display, or slow notification, click Create Debug Package and review goblin-tracker-review.html plus GoblinReplay decision traces.", 72);

            panel.Controls.Add(table);
            return panel;
        }

        private void PortAddNextTestHeader(TableLayoutPanel table, string text)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            Label label = new()
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font(Font, FontStyle.Bold),
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
            };
            table.Controls.Add(label, 0, row);
        }

        private static void PortAddNextTestCheck(TableLayoutPanel table, string text, int rowHeight = 38)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
            CheckBox checkBox = new()
            {
                AutoSize = false,
                CheckAlign = ContentAlignment.TopLeft,
                Checked = false,
                Dock = DockStyle.Fill,
                Padding = new Padding(2, 3, 0, 0),
                Text = text,
                TextAlign = ContentAlignment.TopLeft,
                UseMnemonic = false,
            };
            table.Controls.Add(checkBox, 0, row);
        }

        private TableLayoutPanel PortCreateDiagnosticTable(float labelWidth)
        {
            TableLayoutPanel table = new()
            {
                AutoScroll = true,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 12, 10, 10),
                RowCount = 0,
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return table;
        }

        private void PortAddDiagnosticRow(TableLayoutPanel table, Dictionary<string, Label> labels, string labelText, string key, int rowHeight = 26)
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
            labels[key] = value;
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
            string displayLocation = PortDisplayLocation(PortDetectedLocationDisplayName(portLastConfirmedLocation));
            string blockingLocation = PortDisplayLocation(PortGetConfirmedCurrentLocation());
            string currentTeleportTarget = PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey));
            string nextTeleportTarget = PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey));
            string queuedRetryTarget = PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey));
            string lastRequestedTarget = PortDisplayLocation(PortTeleportLocationForKey(portLastRequestedTeleportKey));
            string failedInterruptedState = portTeleportRetryFailedOrInterrupted ? "True" : "False";
            string waitingConfirmation = portTeleportWaitingForConfirmation ? "True" : "False";
            string waitingConfirmationTarget = PortDisplayLocation(PortTeleportLocationForKey(portTeleportWaitingConfirmationKey));
            string diabloActiveStatus = PortDiabloIsActive() ? "Active" : "Not Active";
            string routeState = $"CurrentKey={PortDisplayLocation(portLastTeleportKey)}; NextKey={PortDisplayLocation(portQueuedTeleportKey)}; RetryKey={PortDisplayLocation(portQueuedRetryTeleportKey)}; FailsafeBlocked={portAutomationBlockedByTeleportFailsafe}";
            string combatState = $"Running={portCombatRunning}; Stopping={portCombatStopping}; Class={PortDisplayLocation(portCombatClass)}; LootLeftDown={portLootSpamLeftClickDown}; DemonHunterRightHeld={portDemonHunterRightHeldFromSafeRegion}; RuntimeRightHeld={portRuntimeRightMouseHeld}";
            DiagnosticsSessionSnapshot sessionSnapshot = DebugManager.Session.Snapshot(DateTime.Now);
            string goblinTracker = $"Goblins={sessionSnapshot.GoblinCount}; ActiveCombatTime={sessionSnapshot.GoblinActiveCombatTime:hh\\:mm\\:ss}; GPH={sessionSnapshot.GoblinsPerHour:0.00}; Observations={sessionSnapshot.GoblinObservationCount}";
            string goblinEvidence = $"Events={sessionSnapshot.GoblinEvidenceEventCount}; Last={(sessionSnapshot.GoblinEvidenceEventCount > 0 ? sessionSnapshot.LastGoblinEvidenceType.ToString() : "None")}; Confidence={sessionSnapshot.LastGoblinEvidenceConfidence:0.00}; Time={(sessionSnapshot.LastGoblinEvidenceTime.HasValue ? sessionSnapshot.LastGoblinEvidenceTime.Value.ToString("HH:mm:ss") : "--")}";
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
            PortSetDiagnosticLabel("GoblinTracker", goblinTracker);
            PortSetDiagnosticLabel("GoblinEvidence", goblinEvidence);
            PortSetDiagnosticLabel("FailureCounter", PortFailureCount().ToString());
            PortSetDiagnosticLabel("DiabloRunningStatus", diabloRunning ? "Running" : "Not Running");
            PortSetDiagnosticLabel("ActiveWorkflow", activeWorkflow);
            PortSetDiagnosticLabel("LastLogFile", portDiagnosticLastLogFile);
            PortSetDiagnosticLabel("LatestScreenshotPath", portDiagnosticLatestScreenshotPath);
            PortSetDiagnosticLabel("LatestFailureScreenshotType", portDiagnosticLatestFailureScreenshotType);
            PortSetDiagnosticLabel("ScreenshotCount", portDiagnosticScreenshotCount.ToString());
            PortSetDiagnosticLabel("LogCount", portDiagnosticLogCount.ToString());
            PortSetDiagnosticLabel("DebugPackagePath", portDiagnosticLatestPackagePath);

            PortSetRouteInspectorLabel("RawDetectedLocation", rawLocation);
            PortSetRouteInspectorLabel("NormalizedAppLocation", normalizedLocation);
            PortSetRouteInspectorLabel("DisplayLocation", displayLocation);
            PortSetRouteInspectorLabel("BlockingLocation", blockingLocation);
            PortSetRouteInspectorLabel("CurrentButtonLocation", currentTeleportTarget);
            PortSetRouteInspectorLabel("NextButtonLocation", nextTeleportTarget);
            PortSetRouteInspectorLabel("QueuedTeleportTarget", nextTeleportTarget);
            PortSetRouteInspectorLabel("RetryQueuedTarget", queuedRetryTarget);
            PortSetRouteInspectorLabel("LastRequestedTeleportTarget", lastRequestedTarget);
            PortSetRouteInspectorLabel("LastTeleportSource", portLastTeleportSource);
            PortSetRouteInspectorLabel("LastBlockingDecision", portLastBlockingDecision);
            PortSetRouteInspectorLabel("LastBlockingReason", portLastBlockingReason);
            PortSetRouteInspectorLabel("LastRouteDecisionOutput", portLastRouteDecisionOutput);
            PortSetRouteInspectorLabel("WaitingConfirmation", waitingConfirmation);
            PortSetRouteInspectorLabel("WaitingConfirmationTarget", waitingConfirmationTarget);
            PortSetRouteInspectorLabel("RetryStateActive", failedInterruptedState);
            PortSetRouteInspectorLabel("FailureCounter", PortFailureCount().ToString());
            PortSetRouteInspectorLabel("GoblinTracker", goblinTracker);
            PortSetRouteInspectorLabel("GoblinEvidence", goblinEvidence);
            PortSetRouteInspectorLabel("LatestLogPath", portDiagnosticLastLogFile);
            PortSetRouteInspectorLabel("LatestScreenshotPath", portDiagnosticLatestScreenshotPath);
            PortSetRouteInspectorLabel("LatestFailureScreenshotType", portDiagnosticLatestFailureScreenshotType);
            PortSetRouteInspectorLabel("ScreenshotCount", portDiagnosticScreenshotCount.ToString());
            PortSetRouteInspectorLabel("LogCount", portDiagnosticLogCount.ToString());
            PortSetRouteInspectorLabel("ActiveWorkflow", activeWorkflow);
            PortSetRouteInspectorLabel("DiabloRunningStatus", diabloRunning ? "Running" : "Not Running");
            PortSetRouteInspectorLabel("DiabloActiveStatus", diabloActiveStatus);
        }

        private void PortSetDiagnosticLabel(string key, string value)
        {
            if (portDiagnosticLabels.TryGetValue(key, out Label? label))
            {
                label.Text = string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
            }
        }

        private void PortSetRouteInspectorLabel(string key, string value)
        {
            if (portRouteInspectorLabels.TryGetValue(key, out Label? label))
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
            portDiagnosticLatestScreenshotPath = screenshotFiles.OrderByDescending(file => file.LastWriteTime).FirstOrDefault()?.FullName ?? "none";
            portDiagnosticLatestPackagePath = PortFindLatestDebugPackagePath();
        }

        private static string PortFindLatestDebugPackagePath()
        {
            string path = DebugManager.FindLatestDebugPackagePath();
            return string.IsNullOrWhiteSpace(path) ? "none" : path;
        }
    }
}

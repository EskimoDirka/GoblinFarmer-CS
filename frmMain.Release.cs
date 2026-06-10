using System.Globalization;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private bool portRuntimeStartupComplete;
        private GroupBox? portSettingsGroup;
        private Label? portDiabloPathLabel;
        private Label? portBattleNetPathLabel;
        private Label? portImagesPathLabel;
        private CheckBox? portDebugModeCheckBox;
        private Button? portSettingsButton;
        private Button? portValidateSettingsButton;
        private GroupBox? portObsStatusGroup;
        private Label? portObsStartTimeLabel;
        private Label? portObsStatusLabel;
        private Label? portObsEndTimeLabel;

        private void PortInitializeReleaseUi()
        {
            if (portSettingsGroup != null)
            {
                return;
            }

            portSettingsGroup = new GroupBox
            {
                Name = "grpRuntimeSettings",
                Text = "Settings",
                Location = new DrawingPoint(349, 462),
                Size = new Size(544, 136),
                TabStop = false,
            };

            Label diabloCaption = PortCreateSettingsCaption("Diablo:", 25);
            Label battleNetCaption = PortCreateSettingsCaption("Battle.net:", 53);
            Label imagesCaption = PortCreateSettingsCaption("Images:", 81);

            portDiabloPathLabel = PortCreatePathLabel(88, 25);
            portBattleNetPathLabel = PortCreatePathLabel(88, 53);
            portImagesPathLabel = PortCreatePathLabel(88, 81);

            portSettingsButton = new Button
            {
                Text = "Change...",
                Location = new DrawingPoint(424, 24),
                Size = new Size(112, 28),
            };
            portSettingsButton.Click += (_, _) => PortShowSettingsDialog(firstRun: false);

            portValidateSettingsButton = new Button
            {
                Text = "Verify Paths",
                Location = new DrawingPoint(424, 59),
                Size = new Size(112, 28),
            };
            portValidateSettingsButton.Click += (_, _) => PortValidateRuntimeSettings(showMessage: true);

            portDebugModeCheckBox = new CheckBox
            {
                Text = "Debug Mode",
                AutoSize = false,
                Location = new DrawingPoint(424, 96),
                Size = new Size(112, 24),
                Checked = AppSettings.Debug.DebugMode,
            };
            portDebugModeCheckBox.CheckedChanged += (_, _) =>
            {
                bool oldDebugMode = AppSettings.Debug.DebugMode;
                bool diagnosticsBefore = Controls.Find("tabDiagnostics", searchAllChildren: false).Any();
                if (AppSettings.IsVsDebugProfile)
                {
                    AppSettings.ApplyDebugDefaultsProfile();
                    PortRefreshDebugControlsFromSettings();
                    PortApplyDebugModeUi();
                    PortLogDebugModeToggle(oldDebugMode, AppSettings.Debug.DebugMode, diagnosticsBefore);
                    return;
                }

                bool debugModeEnabled = portDebugModeCheckBox.Checked;
                DebugManager.ApplyReleaseDebugModePreference(debugModeEnabled, AppSettings.Debug);
                AppSettings.Save();
                PortApplyDebugModeUi();
                PortLogDebugModeToggle(oldDebugMode, AppSettings.Debug.DebugMode, diagnosticsBefore);
            };

            portSettingsGroup.Controls.Add(diabloCaption);
            portSettingsGroup.Controls.Add(battleNetCaption);
            portSettingsGroup.Controls.Add(imagesCaption);
            portSettingsGroup.Controls.Add(portDiabloPathLabel);
            portSettingsGroup.Controls.Add(portBattleNetPathLabel);
            portSettingsGroup.Controls.Add(portImagesPathLabel);
            portSettingsGroup.Controls.Add(portSettingsButton);
            portSettingsGroup.Controls.Add(portValidateSettingsButton);
            if (!AppSettings.IsVsDebugProfile)
            {
                portSettingsGroup.Controls.Add(portDebugModeCheckBox);
                PortMoveKeepDebugScreenshotsToSettingsGroup();
            }
            else
            {
                PortInitializeGoblinTrackerDebugPreferenceControls();
            }

            Controls.Add(portSettingsGroup);
            PortInitializeObsStatusGroup();
            PortApplyDebugModeUi();
        }

        private void PortMoveKeepDebugScreenshotsToSettingsGroup()
        {
            if (portSettingsGroup == null || chkKeepDebugScreenshots == null)
            {
                return;
            }

            grpHotkeys.Controls.Remove(chkKeepDebugScreenshots);
            chkKeepDebugScreenshots.AutoSize = false;
            chkKeepDebugScreenshots.Location = new DrawingPoint(258, 96);
            chkKeepDebugScreenshots.Size = new Size(166, 24);
            portSettingsGroup.Controls.Add(chkKeepDebugScreenshots);
        }

        private static Label PortCreateSettingsCaption(string text, int y)
        {
            return new Label
            {
                Text = text,
                Location = new DrawingPoint(14, y),
                Size = new Size(78, 18),
                TextAlign = ContentAlignment.MiddleLeft,
            };
        }

        private static Label PortCreatePathLabel(int x, int y)
        {
            return new Label
            {
                AutoEllipsis = true,
                Location = new DrawingPoint(x, y),
                Size = new Size(318, 20),
                UseMnemonic = false,
            };
        }

        private void PortInitializeObsStatusGroup()
        {
            if (!AppSettings.IsVsDebugProfile || portSettingsGroup == null || portObsStatusGroup != null)
            {
                return;
            }

            portObsStatusGroup = new GroupBox
            {
                Name = "grpObsStatus",
                Text = "OBS",
                Location = new DrawingPoint(portSettingsGroup.Left, portSettingsGroup.Bottom + 10),
                Size = new Size(portSettingsGroup.Width, 94),
                TabStop = false,
            };

            portObsStartTimeLabel = PortCreateObsStatusLabel("Start Time: --", 22);
            portObsStatusLabel = PortCreateObsStatusLabel("Status: closed", 44);
            portObsEndTimeLabel = PortCreateObsStatusLabel("End Time: --", 66);

            portObsStatusGroup.Controls.Add(portObsStartTimeLabel);
            portObsStatusGroup.Controls.Add(portObsStatusLabel);
            portObsStatusGroup.Controls.Add(portObsEndTimeLabel);
            Controls.Add(portObsStatusGroup);
            PortUpdateObsStatusDisplay();
        }

        private static Label PortCreateObsStatusLabel(string text, int y)
        {
            return new Label
            {
                AutoEllipsis = true,
                Location = new DrawingPoint(14, y),
                Size = new Size(516, 18),
                Text = text,
            };
        }

        private void PortUpdateObsStatusDisplay()
        {
            if (portObsStatusGroup == null || portObsStartTimeLabel == null || portObsStatusLabel == null || portObsEndTimeLabel == null)
            {
                return;
            }

            (DateTime? startTime, string status, DateTime? endTime) = PortReadObsStatusSnapshot();
            portObsStartTimeLabel.Text = $"Start Time: {PortFormatObsStatusTime(startTime)}";
            portObsStatusLabel.Text = $"Status: {status}";
            portObsEndTimeLabel.Text = $"End Time: {PortFormatObsStatusTime(endTime)}";
        }

        private (DateTime? StartTime, string Status, DateTime? EndTime) PortReadObsStatusSnapshot()
        {
            bool obsProcessRunning = false;
            try
            {
                obsProcessRunning =
                    System.Diagnostics.Process.GetProcessesByName("obs64").Length > 0 ||
                    System.Diagnostics.Process.GetProcessesByName("obs").Length > 0;
            }
            catch
            {
                obsProcessRunning = false;
            }

            DateTime? lastRecordingStart = null;
            DateTime? lastRecordingStop = null;
            DateTime? lastLaunch = null;
            DateTime? lastCloseRequested = null;
            string logPath = Path.Combine(PortResolveProjectRootForLocalTools(), "Video Clip Review", "obs-diablo-auto-record.log");
            try
            {
                if (File.Exists(logPath))
                {
                    foreach (string line in File.ReadAllLines(logPath).TakeLast(250))
                    {
                        if (!PortTryParseObsLogTimestamp(line, out DateTime timestamp))
                        {
                            continue;
                        }

                        if (line.Contains("OBS recording started", StringComparison.OrdinalIgnoreCase))
                        {
                            lastRecordingStart = timestamp;
                        }
                        else if (line.Contains("OBS recording stopped", StringComparison.OrdinalIgnoreCase))
                        {
                            lastRecordingStop = timestamp;
                        }
                        else if (line.Contains("OBS launched", StringComparison.OrdinalIgnoreCase))
                        {
                            lastLaunch = timestamp;
                        }
                        else if (line.Contains("OBS close requested", StringComparison.OrdinalIgnoreCase))
                        {
                            lastCloseRequested = timestamp;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"OBS status log read failed: path={logPath}", ex);
            }

            string status;
            if (lastRecordingStart.HasValue && (!lastRecordingStop.HasValue || lastRecordingStart.Value > lastRecordingStop.Value))
            {
                status = "recording";
            }
            else if (lastCloseRequested.HasValue && (!lastLaunch.HasValue || lastCloseRequested.Value >= lastLaunch.Value) && obsProcessRunning)
            {
                status = "stopping";
            }
            else if (lastRecordingStop.HasValue && lastLaunch.HasValue && lastRecordingStop.Value >= lastLaunch.Value && obsProcessRunning)
            {
                status = "not recording";
            }
            else if (lastCloseRequested.HasValue && (!lastLaunch.HasValue || lastCloseRequested.Value >= lastLaunch.Value) && !obsProcessRunning)
            {
                status = "closed";
            }
            else if (obsProcessRunning && lastLaunch.HasValue)
            {
                status = "starting";
            }
            else if (obsProcessRunning)
            {
                status = "not recording";
            }
            else
            {
                status = "closed";
            }

            DateTime? startTime = lastRecordingStart;
            DateTime? endTime = lastRecordingStop;
            return (startTime, status, endTime);
        }

        private static bool PortTryParseObsLogTimestamp(string line, out DateTime timestamp)
        {
            timestamp = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(line) || line.Length < 21 || line[0] != '[')
            {
                return false;
            }

            string text = line.Substring(1, 19);
            return DateTime.TryParseExact(
                text,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out timestamp);
        }

        private static string PortFormatObsStatusTime(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("HH:mm:ss", CultureInfo.CurrentCulture) : "--";
        }

        private static string PortResolveProjectRootForLocalTools()
        {
            string? configDirectory = Path.GetDirectoryName(AppSettings.ConfigPath);
            if (!string.IsNullOrWhiteSpace(configDirectory) &&
                string.Equals(Path.GetFileName(configDirectory), "Config", StringComparison.OrdinalIgnoreCase))
            {
                DirectoryInfo? root = Directory.GetParent(configDirectory);
                if (root != null)
                {
                    return root.FullName;
                }
            }

            DirectoryInfo? directory = new(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "GoblinFarmer.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private void PortRefreshReleaseSettingsUi()
        {
            if (portDiabloPathLabel == null || portBattleNetPathLabel == null || portImagesPathLabel == null)
            {
                return;
            }

            portDiabloPathLabel.Text = PortDisplayConfiguredPath(AppSettings.Runtime.DiabloExecutablePath);
            portBattleNetPathLabel.Text = PortDisplayConfiguredPath(AppSettings.Runtime.BattleNetExecutablePath);
            portImagesPathLabel.Text = PortDisplayConfiguredPath(AppSettings.Runtime.ImagesRoot);
            PortRefreshDebugControlsFromSettings();
        }

        private void PortRefreshDebugControlsFromSettings()
        {
            if (portDebugModeCheckBox != null && portDebugModeCheckBox.Checked != AppSettings.Debug.DebugMode)
            {
                portDebugModeCheckBox.Checked = AppSettings.Debug.DebugMode;
            }

            if (chkKeepDebugScreenshots != null && chkKeepDebugScreenshots.Checked != AppSettings.Debug.EnableDebugScreenshots)
            {
                chkKeepDebugScreenshots.Checked = AppSettings.Debug.EnableDebugScreenshots;
            }
        }

        private static string PortDisplayConfiguredPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? "(not set)" : path;
        }

        private bool PortEnsureRequiredConfiguration()
        {
            bool requiredConfigurationIsValid = AppSettings.RequiredRuntimeConfigurationIsValid(out string validationMessage);
            if (!AppSettings.ShouldRequireFirstRunSetup(AppSettings.CurrentDebugDefaultsProfile, requiredConfigurationIsValid))
            {
                AppLogger.Info("Runtime configuration validated.");

                return true;
            }

            AppLogger.Info(
                "Runtime configuration invalid; setup required: " +
                $"DebugDefaultsProfile={AppSettings.CurrentDebugDefaultsProfile}; " +
                $"FirstRunSetupSuppressed={AppSettings.FirstRunSetupSuppressed}; " +
                $"ConfigPath={AppSettings.ConfigPath}; " +
                $"Validation={validationMessage.Replace(Environment.NewLine, " | ")}");

            return PortShowSettingsDialog(firstRun: true) &&
                AppSettings.RequiredRuntimeConfigurationIsValid(out _);
        }

        private bool PortShowSettingsDialog(bool firstRun)
        {
            using Form dialog = new()
            {
                Text = firstRun ? "GoblinFarmer First-Run Setup" : "GoblinFarmer Settings",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(720, 360),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
            };

            Label intro = new()
            {
                Location = new DrawingPoint(18, 16),
                Size = new Size(665, 34),
                Text = firstRun
                    ? "Select the Diablo III and Battle.net executables before starting automation."
                    : "Update executable and template folder paths. Changes are saved to Config\\AppSettings.json.",
            };

            TextBox diabloTextBox = PortCreateSettingsTextBox(AppSettings.Runtime.DiabloExecutablePath, 74);
            TextBox battleNetTextBox = PortCreateSettingsTextBox(AppSettings.Runtime.BattleNetExecutablePath, 114);
            TextBox imagesTextBox = PortCreateSettingsTextBox(AppSettings.Runtime.ImagesRoot, 154);

            Label validationLabel = new()
            {
                Location = new DrawingPoint(18, 205),
                Size = new Size(665, 54),
                ForeColor = Color.DarkRed,
                UseMnemonic = false,
            };

            dialog.Controls.Add(intro);
            dialog.Controls.Add(PortCreateDialogLabel("Diablo III executable", 74));
            dialog.Controls.Add(PortCreateDialogLabel("Battle.net executable", 114));
            dialog.Controls.Add(PortCreateDialogLabel("Image/templates folder", 154));
            dialog.Controls.Add(diabloTextBox);
            dialog.Controls.Add(battleNetTextBox);
            dialog.Controls.Add(imagesTextBox);
            dialog.Controls.Add(PortCreateBrowseExecutableButton(dialog, diabloTextBox, "Select Diablo III executable", "Diablo III*.exe|Diablo III*.exe|Executable files (*.exe)|*.exe", 74));
            dialog.Controls.Add(PortCreateBrowseExecutableButton(dialog, battleNetTextBox, "Select Battle.net executable", "Battle.net.exe|Battle.net.exe|Executable files (*.exe)|*.exe", 114));
            dialog.Controls.Add(PortCreateBrowseFolderButton(dialog, imagesTextBox, "Select image/template folder", 154));
            dialog.Controls.Add(validationLabel);

            Button verifyButton = new()
            {
                Text = "Verify Folders",
                Location = new DrawingPoint(386, 272),
                Size = new Size(110, 30),
            };
            verifyButton.Click += (_, _) => validationLabel.Text = PortValidateSettingsValues(diabloTextBox.Text, battleNetTextBox.Text, imagesTextBox.Text);

            Button saveButton = new()
            {
                Text = "Save",
                DialogResult = DialogResult.None,
                Location = new DrawingPoint(506, 272),
                Size = new Size(82, 30),
            };
            saveButton.Click += (_, _) =>
            {
                string validation = PortValidateSettingsValues(diabloTextBox.Text, battleNetTextBox.Text, imagesTextBox.Text);
                validationLabel.Text = validation;
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return;
                }

                string previousImagesRoot = AppSettings.Runtime.ImagesRoot;
                AppSettings.Runtime.DiabloExecutablePath = diabloTextBox.Text.Trim();
                AppSettings.Runtime.BattleNetExecutablePath = battleNetTextBox.Text.Trim();
                AppSettings.Runtime.ImagesRoot = PortPortableAppPath(imagesTextBox.Text.Trim());
                AppSettings.Save();
                if (portRuntimeStartupComplete && !string.Equals(previousImagesRoot, AppSettings.Runtime.ImagesRoot, StringComparison.OrdinalIgnoreCase))
                {
                    portScanRegionManager = null;
                    PortLoadCoordinates();
                    PortLoadImageCaches();
                    AppLogger.Info($"Runtime image configuration reloaded after ImagesRoot change: {AppSettings.Runtime.ImagesRoot}");
                }

                PortRefreshReleaseSettingsUi();
                PortApplyDebugModeUi();
                PortSetAutomationControlsEnabled(true);
                PortCompleteRuntimeStartup();
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            };

            Button cancelButton = new()
            {
                Text = firstRun ? "Skip" : "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new DrawingPoint(598, 272),
                Size = new Size(82, 30),
            };

            dialog.Controls.Add(verifyButton);
            dialog.Controls.Add(saveButton);
            dialog.Controls.Add(cancelButton);
            dialog.AcceptButton = saveButton;
            dialog.CancelButton = cancelButton;

            DialogResult result = dialog.ShowDialog(this);
            if (result != DialogResult.OK)
            {
                string validation = PortValidateSettingsValues(diabloTextBox.Text, battleNetTextBox.Text, imagesTextBox.Text);
                AppLogger.Info($"Settings dialog closed without saving: firstRun={firstRun}; valid={string.IsNullOrWhiteSpace(validation)}; validation={validation.Replace(Environment.NewLine, " | ")}");
            }

            PortRefreshReleaseSettingsUi();
            return result == DialogResult.OK;
        }

        private static Label PortCreateDialogLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Location = new DrawingPoint(18, y + 4),
                Size = new Size(150, 22),
                TextAlign = ContentAlignment.MiddleLeft,
            };
        }

        private static TextBox PortCreateSettingsTextBox(string text, int y)
        {
            return new TextBox
            {
                Text = text,
                Location = new DrawingPoint(170, y),
                Size = new Size(430, 23),
            };
        }

        private static Button PortCreateBrowseExecutableButton(IWin32Window owner, TextBox target, string title, string filter, int y)
        {
            Button button = new()
            {
                Text = "Browse...",
                Location = new DrawingPoint(610, y - 1),
                Size = new Size(80, 26),
            };
            button.Click += (_, _) =>
            {
                using OpenFileDialog dialog = new()
                {
                    Title = title,
                    Filter = filter,
                    CheckFileExists = true,
                    Multiselect = false,
                };
                if (dialog.ShowDialog(owner) == DialogResult.OK)
                {
                    target.Text = dialog.FileName;
                }
            };
            return button;
        }

        private static Button PortCreateBrowseFolderButton(IWin32Window owner, TextBox target, string description, int y)
        {
            Button button = new()
            {
                Text = "Browse...",
                Location = new DrawingPoint(610, y - 1),
                Size = new Size(80, 26),
            };
            button.Click += (_, _) =>
            {
                using FolderBrowserDialog dialog = new()
                {
                    Description = description,
                    UseDescriptionForTitle = true,
                    SelectedPath = Directory.Exists(AppSettings.ResolveRuntimePath(target.Text))
                        ? AppSettings.ResolveRuntimePath(target.Text)
                        : AppSettings.ImagesRootPath,
                };
                if (dialog.ShowDialog(owner) == DialogResult.OK)
                {
                    target.Text = dialog.SelectedPath;
                }
            };
            return button;
        }

        private static string PortValidateSettingsValues(string diabloPath, string battleNetPath, string imagesPath)
        {
            List<string> errors = [];
            if (!AppSettings.ExecutableExists(diabloPath))
            {
                errors.Add("Select a valid Diablo III executable.");
            }

            if (!AppSettings.ExecutableExists(battleNetPath))
            {
                errors.Add("Select a valid Battle.net executable.");
            }

            string resolvedImages = AppSettings.ResolveRuntimePath(imagesPath);
            if (!Directory.Exists(resolvedImages))
            {
                errors.Add($"Image/template folder does not exist: {resolvedImages}");
            }

            foreach (string folder in new[] { "Combat", "Current Location", "Leave Game", "Repair", "Salvage", "Start Game", "Teleport Function" })
            {
                string path = Path.Combine(resolvedImages, folder);
                if (!Directory.Exists(path))
                {
                    errors.Add($"Missing template folder: {folder}");
                }
            }

            return string.Join(Environment.NewLine, errors);
        }

        private bool PortValidateRuntimeSettings(bool showMessage)
        {
            bool valid = AppSettings.RequiredRuntimeConfigurationIsValid(out string message);
            AppLogger.Info($"Runtime settings validation: valid={valid}; message={message.Replace(Environment.NewLine, " | ")}");
            if (showMessage)
            {
                MessageBox.Show(
                    this,
                    valid ? "Settings are valid." : message,
                    "GoblinFarmer Settings",
                    MessageBoxButtons.OK,
                    valid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }

            return valid;
        }

        private static string PortPortableAppPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "";
            }

            string fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
            string basePath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, fullPath);
            }

            return fullPath;
        }

        private void PortSetAutomationControlsEnabled(bool enabled)
        {
            foreach (Control control in new Control[]
            {
                btnMakeNewGame,
                btnExitGame,
                btnNewTristram,
                btnSouthernHighlands,
                btnNorthernHighlands,
                btnTheWeepingHollow,
                btnTheFesteringWoods,
                btnCathedral,
                btnRoyalCrypts,
                btnHiddenCamp,
                btnCityOfCaldeum,
                btnAncientWaterway,
                btnStingingWinds,
                btnBattlefields,
                btnRakkisCrossing,
                btnPandemoniumFortressLevel1,
                btnPandemoniumFortressLevel2,
                chkCombat,
                chkTeleportNextHotkey,
                chkExitGameHotkey,
                chkKadala,
                chkLoot,
            })
            {
                control.Enabled = enabled;
            }
        }

        private void PortApplyDebugModeUi()
        {
            if (AppSettings.IsVsDebugProfile)
            {
                AppSettings.ApplyDebugDefaultsProfile();
                PortRefreshDebugControlsFromSettings();
                PortApplyVsDebugGoblinTrackerLayout();
            }

            bool debugMode = AppSettings.Debug.DebugMode;
            bool dynamicDebugControlsVisible = DebugManager.ShouldShowDynamicDebugControls(AppSettings.CurrentDebugDefaultsProfile);
            bool debugControlsForcedVisible = !dynamicDebugControlsVisible;
            if (portDebugModeCheckBox != null)
            {
                portDebugModeCheckBox.Visible = dynamicDebugControlsVisible;
                portDebugModeCheckBox.Enabled = dynamicDebugControlsVisible;
            }

            if (chkKeepDebugScreenshots != null)
            {
                chkKeepDebugScreenshots.Checked = AppSettings.Debug.EnableDebugScreenshots;
                chkKeepDebugScreenshots.Enabled = dynamicDebugControlsVisible && debugMode;
                chkKeepDebugScreenshots.Visible = dynamicDebugControlsVisible && debugMode;
            }

            grpHotkeys.Size = new Size(270, 157);

            Control? diagnostics = Controls.Find("tabDiagnostics", searchAllChildren: false).FirstOrDefault();
            if (!debugControlsForcedVisible && !debugMode)
            {
                MinimumSize = new Size(934, 875);
                ClientSize = new Size(918, 836);
                if (diagnostics != null)
                {
                    Controls.Remove(diagnostics);
                    diagnostics.Dispose();
                    portDiagnosticLabels.Clear();
                    portRouteInspectorLabels.Clear();
                    AppLogger.Info("Diagnostic UI removed because Debug Mode is disabled.");
                }
            }
            else if ((debugControlsForcedVisible || debugMode) && diagnostics == null)
            {
                PortInitializeDiagnosticOverlay();
            }
        }

        private void PortApplyVsDebugGoblinTrackerLayout()
        {
            SuspendLayout();
            grpGoblinTracker.SuspendLayout();

            MinimumSize = new Size(Math.Max(MinimumSize.Width, 934), 875);
            ClientSize = new Size(Math.Max(ClientSize.Width, 918), 836);

            grpGoblinTracker.Size = new Size(311, 304);
            lblGoblinCount.Location = new Point(12, 24);
            lblGoblinGph.Location = new Point(12, 48);
            lblGoblinActiveTime.Location = new Point(12, 72);
            lblGoblinEvidenceLast.Location = new Point(12, 108);
            lblGoblinEvidenceType.Location = new Point(12, 132);
            lblGoblinEvidenceConfidence.Location = new Point(12, 156);
            lblGoblinEvidenceTime.Location = new Point(12, 180);
            lblGoblinObservation.Location = new Point(12, 212);
            lblGoblinObservation.Size = new Size(287, 84);

            grpGoblinTracker.ResumeLayout(false);
            grpGoblinTracker.PerformLayout();
            ResumeLayout(false);
        }

        private void PortLogDebugModeToggle(bool oldDebugMode, bool newDebugMode, bool diagnosticsBefore)
        {
            bool diagnosticsAfter = Controls.Find("tabDiagnostics", searchAllChildren: false).Any();
            AppLogger.Info(
                "DebugModeToggled: " +
                $"oldDebugMode={oldDebugMode}; " +
                $"newDebugMode={newDebugMode}; " +
                $"diagnosticUiBefore={diagnosticsBefore}; " +
                $"diagnosticUiAfter={diagnosticsAfter}; " +
                $"diagnosticUiAdded={!diagnosticsBefore && diagnosticsAfter}; " +
                $"diagnosticUiRemoved={diagnosticsBefore && !diagnosticsAfter}; " +
                $"debugScreenshotsEnabled={AppSettings.Debug.EnableDebugScreenshots}; " +
                $"debugPackageCaptureAvailable=True; " +
                $"ShowDiagnosticOverlay={AppSettings.Debug.ShowDiagnosticOverlay}; " +
                $"ShowRouteInspector={AppSettings.Debug.ShowRouteInspector}; " +
                $"AppSettingsPath={AppSettings.ConfigPath}");
        }
    }
}

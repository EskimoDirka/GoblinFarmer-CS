using System.Drawing;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private string PortCaptureGoblinEvidenceScreenshot(GoblinEvidenceType type, DateTime timestamp)
        {
            try
            {
                string directory = DebugManager.GoblinEvidenceDirectory;
                Directory.CreateDirectory(directory);

                string safeType = PortSafeScreenshotName(type.ToString(), "Unknown");
                string path = Path.Combine(directory, $"GoblinEvidence_{timestamp:yyyyMMdd_HHmmss}_{safeType}.png");
                string savedPath = PortCaptureDiabloScreenshotToFile(path, $"GoblinEvidence:{type}");
                if (!string.IsNullOrWhiteSpace(savedPath))
                {
                    DebugManager.RecordDebugScreenshotPath(savedPath);
                }

                // TODO: Save cropped evidence-region screenshots after journal/minimap regions are calibrated.
                return savedPath;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Goblin evidence screenshot capture failed: type={type}", ex);
                return "";
            }
        }

        private void PortQueueGoblinRecognitionDebugCapture(string source)
        {
            if (!AppSettings.IsVsDebugProfile)
            {
                AppLogger.Info($"GoblinRecognitionCaptureSkipped: reason=NotVsDebugProfile; source={PortLogField(source)}");
                return;
            }

            string normalizedSource = string.IsNullOrWhiteSpace(source) ? "VsDebugCaptureButton" : source.Trim();
            AppLogger.Info(
                "GoblinRecognitionCaptureQueued: " +
                $"source={PortLogField(normalizedSource)}; " +
                $"currentArea={PortLogField(PortDisplayLocation(portLastConfirmedLocation))}; " +
                $"diabloRunning={IsDiabloRunning()}; " +
                $"diabloActive={PortDiabloIsActive()}");
            _ = Task.Run(() => PortSaveGoblinRecognitionDebugCapture(normalizedSource));
        }

        private void PortSaveGoblinRecognitionDebugCapture(string source)
        {
            string savedFullscreenPath = "";
            string savedMinimapPath = "";
            string savedJournalPath = "";
            string metadataPath = "";
            try
            {
                DateTime timestamp = DateTime.Now;
                string directory = Path.Combine(DebugManager.GoblinEvidenceDirectory, "ManualCaptures");
                Directory.CreateDirectory(directory);

                string safeSource = PortSafeScreenshotName(source, "Capture");
                string currentArea = PortDisplayLocation(portLastConfirmedLocation);
                string safeArea = PortSafeScreenshotName(currentArea, "UnknownArea");
                string prefix = $"GoblinCapture_{timestamp:yyyyMMdd_HHmmss_fff}_{safeSource}_{safeArea}";

                string fullscreenPath = Path.Combine(directory, $"{prefix}_Fullscreen.png");
                string minimapPath = Path.Combine(directory, $"{prefix}_Minimap.png");
                string journalPath = Path.Combine(directory, $"{prefix}_Journal.png");
                metadataPath = Path.Combine(directory, $"{prefix}_Metadata.txt");

                savedFullscreenPath = PortCaptureDiabloScreenshotToFile(fullscreenPath, $"GoblinRecognitionCapture:{source}:Fullscreen");
                if (!string.IsNullOrWhiteSpace(savedFullscreenPath))
                {
                    DebugManager.RecordDebugScreenshotPath(savedFullscreenPath);
                }

                savedMinimapPath = PortCaptureGoblinEncounterRegionCrop("ManualCaptureMinimap", PortGoblinEvidenceMinimapRegion(), minimapPath, timestamp);
                savedJournalPath = PortCaptureGoblinEncounterRegionCrop("ManualCaptureJournal", PortGoblinEvidenceJournalRegion(), journalPath, timestamp);

                GoblinObservationRecord? displayedObservation;
                string displayedObservationStatus;
                lock (portGoblinTrackerLock)
                {
                    displayedObservation = portDisplayedGoblinObservation;
                    displayedObservationStatus = portDisplayedGoblinObservationStatus;
                }

                List<string> metadata =
                [
                    "Goblin Recognition Manual Capture",
                    "Purpose=Manual image-recognition troubleshooting only",
                    $"CreatedLocal={timestamp:O}",
                    $"CreatedUtc={timestamp.ToUniversalTime():O}",
                    $"Source={source}",
                    $"CurrentArea={currentArea}",
                    $"CombatActive={portCombatRunning}",
                    $"CombatStopping={portCombatStopping}",
                    $"AutomationRunning={isAutomationRunning}",
                    $"DiabloRunning={IsDiabloRunning()}",
                    $"DiabloActive={PortDiabloIsActive()}",
                    $"FullscreenPath={savedFullscreenPath}",
                    $"MinimapPath={savedMinimapPath}",
                    $"JournalPath={savedJournalPath}",
                    $"MinimapReferenceRegion={FormatRectangle(PortGoblinEvidenceMinimapRegion())}",
                    $"JournalReferenceRegion={FormatRectangle(PortGoblinEvidenceJournalRegion())}",
                    $"LastObservationStatus={displayedObservationStatus}",
                ];

                if (displayedObservation != null)
                {
                    metadata.Add($"LastObservationSource={displayedObservation.Source}");
                    metadata.Add($"LastObservationType={displayedObservation.GoblinType}");
                    metadata.Add($"LastObservationArea={PortDisplayLocation(displayedObservation.AreaKey)}");
                    metadata.Add($"LastObservationReason={displayedObservation.Reason}");
                    metadata.Add($"LastObservationDuplicateState={displayedObservation.DuplicateState}");
                    metadata.Add($"LastObservationTimestampUtc={displayedObservation.TimestampUtc:O}");
                }

                File.WriteAllLines(metadataPath, metadata);

                AppLogger.Info(
                    "GoblinRecognitionCaptureSaved: " +
                    $"source={PortLogField(source)}; " +
                    $"currentArea={PortLogField(currentArea)}; " +
                    $"fullscreenPath={PortLogField(savedFullscreenPath)}; " +
                    $"minimapPath={PortLogField(savedMinimapPath)}; " +
                    $"journalPath={PortLogField(savedJournalPath)}; " +
                    $"metadataPath={PortLogField(metadataPath)}; " +
                    "createdOnlyByButton=True; " +
                    "counterWorkflowCapturesRemainAutomatic=True");

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => PortShowSplash("Goblin capture saved", 2000)));
                }
                else
                {
                    PortShowSplash("Goblin capture saved", 2000);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Goblin recognition capture failed: source={source}; metadataPath={metadataPath}", ex);
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => PortShowSplash("Goblin capture failed", 2500)));
                }
                else
                {
                    PortShowSplash("Goblin capture failed", 2500);
                }
            }
        }

        private void PortQueueGoblinEncounterDebugCapture(
            string countSource,
            string evidenceSource,
            string goblinType,
            string areaKey,
            string displayLocation,
            int total)
        {
            if (!AppSettings.IsVsDebugProfile)
            {
                return;
            }

            string normalizedType = GoblinTypeNormalizer.Normalize(goblinType);
            string normalizedCountSource = string.IsNullOrWhiteSpace(countSource) ? "Unknown" : countSource.Trim();
            string normalizedEvidenceSource = string.IsNullOrWhiteSpace(evidenceSource) ? "Unknown" : evidenceSource.Trim();
            string normalizedAreaKey = PortDisplayLocation(areaKey);
            string normalizedDisplayLocation = PortDisplayLocation(displayLocation);
            _ = Task.Run(() => PortSaveGoblinEncounterDebugCapture(
                normalizedCountSource,
                normalizedEvidenceSource,
                normalizedType,
                normalizedAreaKey,
                normalizedDisplayLocation,
                total));
        }

        private void PortSaveGoblinEncounterDebugCapture(
            string countSource,
            string evidenceSource,
            string goblinType,
            string areaKey,
            string displayLocation,
            int total)
        {
            try
            {
                DateTime timestamp = DateTime.Now;
                string directory = Path.Combine(DebugManager.GoblinEvidenceDirectory, "EncounterCaptures");
                Directory.CreateDirectory(directory);

                string safeSource = PortSafeScreenshotName(countSource, "Count");
                string safeEvidenceSource = PortSafeScreenshotName(evidenceSource, "Evidence");
                string safeType = PortSafeScreenshotName(goblinType, "UnknownGoblin");
                string safeArea = PortSafeScreenshotName(areaKey, "UnknownArea");
                string prefix = $"GoblinEncounter_{timestamp:yyyyMMdd_HHmmss_fff}_{safeSource}_{safeEvidenceSource}_{safeType}_{safeArea}";

                string fullscreenPath = Path.Combine(directory, $"{prefix}_Fullscreen.png");
                string minimapPath = Path.Combine(directory, $"{prefix}_Minimap.png");
                string journalPath = Path.Combine(directory, $"{prefix}_Journal.png");
                string metadataPath = Path.Combine(directory, $"{prefix}_Metadata.txt");

                string savedFullscreenPath = PortCaptureDiabloScreenshotToFile(fullscreenPath, $"GoblinEncounter:{countSource}:Fullscreen");
                if (!string.IsNullOrWhiteSpace(savedFullscreenPath))
                {
                    DebugManager.RecordDebugScreenshotPath(savedFullscreenPath);
                }

                string savedMinimapPath = PortCaptureGoblinEncounterRegionCrop("Minimap", PortGoblinEvidenceMinimapRegion(), minimapPath, timestamp);
                string savedJournalPath = PortCaptureGoblinEncounterRegionCrop("Journal", PortGoblinEvidenceJournalRegion(), journalPath, timestamp);
                File.WriteAllLines(metadataPath,
                [
                    "Goblin Encounter Debug Capture",
                    $"CreatedLocal={timestamp:O}",
                    $"CreatedUtc={timestamp.ToUniversalTime():O}",
                    $"CountSource={countSource}",
                    $"EvidenceSource={evidenceSource}",
                    $"GoblinType={goblinType}",
                    $"AreaKey={areaKey}",
                    $"DisplayLocation={displayLocation}",
                    $"Total={total}",
                    $"FullscreenPath={savedFullscreenPath}",
                    $"MinimapPath={savedMinimapPath}",
                    $"JournalPath={savedJournalPath}",
                    $"MinimapReferenceRegion={FormatRectangle(PortGoblinEvidenceMinimapRegion())}",
                    $"JournalReferenceRegion={FormatRectangle(PortGoblinEvidenceJournalRegion())}",
                ]);

                AppLogger.Info(
                    "GoblinEncounterCaptureSaved: " +
                    $"countSource={PortLogField(countSource)}; " +
                    $"evidenceSource={PortLogField(evidenceSource)}; " +
                    $"goblinType={PortLogField(goblinType)}; " +
                    $"areaKey={PortLogField(areaKey)}; " +
                    $"displayLocation={PortLogField(displayLocation)}; " +
                    $"total={total}; " +
                    $"fullscreenPath={PortLogField(savedFullscreenPath)}; " +
                    $"minimapPath={PortLogField(savedMinimapPath)}; " +
                    $"journalPath={PortLogField(savedJournalPath)}; " +
                    $"metadataPath={PortLogField(metadataPath)}; " +
                    "reviewIncludes=MinimapAndJournalOnly");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Goblin encounter debug capture failed: source={countSource}; type={goblinType}; area={areaKey}", ex);
            }
        }

        private string PortCaptureGoblinEncounterRegionCrop(string label, Rectangle referenceRegion, string path, DateTime timestamp)
        {
            try
            {
                if (!PortTryGetDiabloRect(out RECT diabloRect))
                {
                    AppLogger.Info($"GoblinEncounterCaptureCropSkipped: label={label}; reason=DiabloRectUnavailable; scanRegion={FormatRectangle(referenceRegion)}");
                    return "";
                }

                Rectangle screenRegion = PortScaleReferenceRectangle(referenceRegion, diabloRect);
                screenRegion = Rectangle.Intersect(SystemInformation.VirtualScreen, screenRegion);
                if (screenRegion.Width <= 0 || screenRegion.Height <= 0)
                {
                    AppLogger.Info($"GoblinEncounterCaptureCropSkipped: label={label}; reason=InvalidScreenRegion; scanRegion={FormatRectangle(referenceRegion)}; screenRegion={FormatRectangle(screenRegion)}");
                    return "";
                }

                RECT captureRect = new()
                {
                    Left = screenRegion.Left,
                    Top = screenRegion.Top,
                    Right = screenRegion.Right,
                    Bottom = screenRegion.Bottom,
                };
                string savedPath = PortCaptureScreenRectangleToFile(captureRect, path, $"GoblinEncounter:{label}");
                if (!string.IsNullOrWhiteSpace(savedPath))
                {
                    DebugManager.RecordDebugScreenshotPath(savedPath);
                }

                AppLogger.Info($"GoblinEncounterCaptureCropSaved: label={label}; timestamp={timestamp:O}; path={PortLogField(savedPath)}; scanRegion={FormatRectangle(referenceRegion)}; screenRegion={FormatRectangle(screenRegion)}");
                return savedPath;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Goblin encounter capture crop failed: label={label}", ex);
                return "";
            }
        }

        private void PortCaptureGoblinEvidenceCalibrationSnapshot()
        {
            if (Interlocked.Exchange(ref portGoblinEvidenceCalibrationCaptureActive, 1) == 1)
            {
                AppLogger.Info("GoblinCalibration: Snapshot skipped; capture already active");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    PortSaveGoblinEvidenceCalibrationSnapshot();
                }
                catch (Exception ex)
                {
                    AppLogger.Error("GoblinCalibration: Snapshot failed.", ex);
                }
                finally
                {
                    Interlocked.Exchange(ref portGoblinEvidenceCalibrationCaptureActive, 0);
                    DebugManager.CleanupOldGoblinEvidence(AppSettings.Debug.GoblinEvidenceRetentionCount);
                }
            });
        }

        private void PortSaveGoblinEvidenceCalibrationSnapshot()
        {
            DateTime timestamp = DateTime.Now;
            string directory = Path.Combine(DebugManager.GoblinEvidenceDirectory, "Calibration");
            Directory.CreateDirectory(directory);

            string prefix = $"GoblinCalibration_{timestamp:yyyyMMdd_HHmmss}";
            string fullPath = Path.Combine(directory, $"{prefix}_Full.png");
            string minimapPath = Path.Combine(directory, $"{prefix}_Minimap.png");
            string journalPath = Path.Combine(directory, $"{prefix}_Journal.png");
            string metadataPath = Path.Combine(directory, $"{prefix}_Metadata.txt");

            string savedFullPath = PortCaptureDiabloScreenshotToFile(fullPath, "GoblinCalibration");
            if (string.IsNullOrWhiteSpace(savedFullPath) || !File.Exists(savedFullPath))
            {
                AppLogger.Info("GoblinCalibration: Snapshot failed; reason=source screenshot unavailable");
                return;
            }

            using (Bitmap screenshot = new(savedFullPath))
            {
                // TODO: These are calibration starter regions for 2560x1440 and may need tuning per resolution/UI scale.
                Rectangle minimapRegion = PortScaleAndClampGoblinCalibrationRegion(
                    GoblinEvidenceCalibrationMinimapReferenceRegion,
                    screenshot.Size);
                Rectangle journalRegion = PortScaleAndClampGoblinCalibrationRegion(
                    GoblinEvidenceCalibrationJournalReferenceRegion,
                    screenshot.Size);

                string savedMinimapPath = PortSaveGoblinCalibrationCrop(screenshot, minimapRegion, minimapPath, "Minimap");
                string savedJournalPath = PortSaveGoblinCalibrationCrop(screenshot, journalRegion, journalPath, "Journal");

                string currentLocation = PortDisplayLocation(portLastConfirmedLocation);
                File.WriteAllLines(metadataPath,
                [
                    $"Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss}",
                    $"Screenshot: {screenshot.Width}x{screenshot.Height}",
                    $"Combat Active: {portCombatRunning}",
                    $"Combat Profile: {PortGoblinEvidenceCombatProfileDisplayName()}",
                    $"Current Location: {currentLocation}",
                    $"Full Screenshot Path: {savedFullPath}",
                    $"Minimap Crop Path: {savedMinimapPath}",
                    $"Journal Crop Path: {savedJournalPath}",
                    $"Minimap Region: {PortFormatGoblinCalibrationRegion(minimapRegion)}",
                    $"Journal Region: {PortFormatGoblinCalibrationRegion(journalRegion)}",
                ]);

                DebugManager.RecordDebugScreenshotPath(savedFullPath);
                AppLogger.Info("GoblinCalibration: Snapshot saved");
                AppLogger.Info($"GoblinCalibration: Full={PortLogField(savedFullPath)}");
                AppLogger.Info($"GoblinCalibration: Minimap={PortLogField(PortDisplayLocation(savedMinimapPath))}; Region={PortFormatGoblinCalibrationRegion(minimapRegion)}");
                AppLogger.Info($"GoblinCalibration: Journal={PortLogField(PortDisplayLocation(savedJournalPath))}; Region={PortFormatGoblinCalibrationRegion(journalRegion)}");
                AppLogger.Info($"GoblinCalibration: Metadata={PortLogField(metadataPath)}");
            }
        }

        private static Rectangle PortScaleAndClampGoblinCalibrationRegion(Rectangle referenceRegion, Size screenshotSize)
        {
            Rectangle scaled = new(
                (int)Math.Round(referenceRegion.X * screenshotSize.Width / (double)PortReferenceWidth),
                (int)Math.Round(referenceRegion.Y * screenshotSize.Height / (double)PortReferenceHeight),
                (int)Math.Round(referenceRegion.Width * screenshotSize.Width / (double)PortReferenceWidth),
                (int)Math.Round(referenceRegion.Height * screenshotSize.Height / (double)PortReferenceHeight));

            return Rectangle.Intersect(new Rectangle(Point.Empty, screenshotSize), scaled);
        }

        private static string PortSaveGoblinCalibrationCrop(Bitmap source, Rectangle cropRegion, string path, string label)
        {
            if (cropRegion.Width <= 0 || cropRegion.Height <= 0)
            {
                AppLogger.Info($"GoblinCalibration: {label} crop skipped; reason=empty crop after clamp; region={PortFormatGoblinCalibrationRegion(cropRegion)}");
                return "";
            }

            using Bitmap crop = source.Clone(cropRegion, source.PixelFormat);
            crop.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            return path;
        }

        private string PortGoblinEvidenceCombatProfileDisplayName()
        {
            return portCombatClass switch
            {
                "monk" => "Monk",
                "witch_doctor" => "Witch Doctor",
                "demon_hunter" => "Demon Hunter",
                "" => "None",
                _ => portCombatClass,
            };
        }

        private static string PortFormatGoblinCalibrationRegion(Rectangle region)
        {
            return $"X={region.X} Y={region.Y} W={region.Width} H={region.Height}";
        }
    }
}

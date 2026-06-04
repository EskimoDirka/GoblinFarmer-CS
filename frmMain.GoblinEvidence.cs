using System.Drawing;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private const int GoblinEvidenceScanIntervalMs = 750;
        private static readonly TimeSpan GoblinEvidenceCooldown = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan GoblinEvidenceMissingTemplateLogCooldown = TimeSpan.FromMinutes(5);
        private static readonly Rectangle GoblinEvidenceCalibrationMinimapReferenceRegion = new(2050, 20, 480, 420);
        private static readonly Rectangle GoblinEvidenceCalibrationJournalReferenceRegion = new(25, 650, 900, 420);
        private readonly object portGoblinEvidenceLock = new();
        private readonly Dictionary<GoblinEvidenceType, DateTime> portLastGoblinEvidenceByType = new();
        private readonly Dictionary<GoblinEvidenceType, DateTime> portLastGoblinEvidenceMissingTemplateLogByType = new();
        private Task? portGoblinEvidenceScannerTask;
        private int portGoblinEvidenceCalibrationCaptureActive;

        private void PortStartGoblinEvidenceScanner(CancellationToken token)
        {
            if (portGoblinEvidenceScannerTask != null && !portGoblinEvidenceScannerTask.IsCompleted)
            {
                return;
            }

            portGoblinEvidenceScannerTask = Task.Run(() =>
            {
                try
                {
                    PortGoblinEvidenceScannerLoop(token);
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Goblin evidence scanner failed.", ex);
                }
            }, CancellationToken.None);
        }

        private void PortGoblinEvidenceScannerLoop(CancellationToken token)
        {
            AppLogger.Info($"GoblinEvidenceScannerStarted: intervalMs={GoblinEvidenceScanIntervalMs}; cooldownSeconds={GoblinEvidenceCooldown.TotalSeconds:0}; combatActive={portCombatRunning}");

            while (!token.IsCancellationRequested && portCombatRunning)
            {
                if (PortShouldScanGoblinEvidence(token))
                {
                    PortScanGoblinEvidence();
                }

                PortSleep(token, GoblinEvidenceScanIntervalMs);
            }

            AppLogger.Info($"GoblinEvidenceScannerStopped: combatActive={portCombatRunning}; combatStopping={portCombatStopping}; cancelled={token.IsCancellationRequested}");
        }

        private bool PortShouldScanGoblinEvidence(CancellationToken token)
        {
            return !token.IsCancellationRequested &&
                portCombatRunning &&
                !portCombatStopping &&
                !isAutomationRunning &&
                IsDiabloRunning() &&
                PortDiabloIsActive();
        }

        private void PortScanGoblinEvidence()
        {
            foreach (GoblinEvidenceCandidate candidate in PortDetectGoblinEvidenceCandidates())
            {
                PortRecordGoblinEvidence(candidate);
            }
        }

        private IEnumerable<GoblinEvidenceCandidate> PortDetectGoblinEvidenceCandidates()
        {
            GoblinEvidenceCandidate? journalKill = PortDetectJournalKillEvidence();
            if (journalKill != null)
            {
                yield return journalKill;
            }

            GoblinEvidenceCandidate? journalEncounter = PortDetectJournalEncounterEvidence();
            if (journalEncounter != null)
            {
                yield return journalEncounter;
            }

            GoblinEvidenceCandidate? minimapIcon = PortDetectMinimapGoblinEvidence();
            if (minimapIcon != null)
            {
                yield return minimapIcon;
            }
        }

        private GoblinEvidenceCandidate? PortDetectJournalKillEvidence()
        {
            // TODO: Replace template placeholder with calibrated journal kill region/assets.
            return PortDetectGoblinEvidenceTemplate(
                GoblinEvidenceType.JournalKill,
                "Journal",
                Img("Goblin Evidence", "Journal Kill.png"),
                PortGoblinEvidenceJournalRegion(),
                0.90,
                "Template placeholder for a goblin kill journal entry.");
        }

        private GoblinEvidenceCandidate? PortDetectJournalEncounterEvidence()
        {
            // TODO: Replace template placeholder with calibrated journal encounter region/assets.
            return PortDetectGoblinEvidenceTemplate(
                GoblinEvidenceType.JournalEncounter,
                "Journal",
                Img("Goblin Evidence", "Journal Encounter.png"),
                PortGoblinEvidenceJournalRegion(),
                0.90,
                "Template placeholder for a goblin encounter journal entry.");
        }

        private GoblinEvidenceCandidate? PortDetectMinimapGoblinEvidence()
        {
            // TODO: Add calibrated minimap icon template(s) and tighten this region per resolution.
            return PortDetectGoblinEvidenceTemplate(
                GoblinEvidenceType.MinimapIcon,
                "Minimap",
                Img("Goblin Evidence", "Minimap Goblin Icon.png"),
                PortGoblinEvidenceMinimapRegion(),
                0.65,
                "Template placeholder for a possible goblin minimap icon.");
        }

        private GoblinEvidenceCandidate? PortDetectGoblinEvidenceTemplate(
            GoblinEvidenceType type,
            string source,
            string imagePath,
            Rectangle referenceRegion,
            double threshold,
            string notes)
        {
            if (!File.Exists(imagePath))
            {
                PortLogGoblinEvidenceMissingTemplate(type, source, imagePath, referenceRegion);
                return null;
            }

            double confidence = PortBestTemplateConfidenceInDiabloRegion(imagePath, referenceRegion);
            if (confidence < threshold)
            {
                return null;
            }

            return new GoblinEvidenceCandidate(type, confidence, source, notes);
        }

        private Rectangle PortGoblinEvidenceJournalRegion()
        {
            // TODO: Calibrate against the in-game journal kill/encounter message area.
            return PortReferenceRegion(120, 120, 980, 420);
        }

        private Rectangle PortGoblinEvidenceMinimapRegion()
        {
            // TODO: Calibrate against the minimap bounds for each supported Diablo resolution.
            return PortReferenceRegion(1940, 70, 560, 430);
        }

        private void PortLogGoblinEvidenceMissingTemplate(
            GoblinEvidenceType type,
            string source,
            string imagePath,
            Rectangle referenceRegion)
        {
            DateTime now = DateTime.Now;
            lock (portGoblinEvidenceLock)
            {
                if (portLastGoblinEvidenceMissingTemplateLogByType.TryGetValue(type, out DateTime lastLogged) &&
                    now - lastLogged < GoblinEvidenceMissingTemplateLogCooldown)
                {
                    return;
                }

                portLastGoblinEvidenceMissingTemplateLogByType[type] = now;
            }

            AppLogger.Info($"GoblinEvidenceDetectorDisabled: Type={type}; Source={source}; Reason=MissingTemplate; Template={PortLogField(imagePath)}; ReferenceRegion={FormatRectangle(referenceRegion)}; NextStep=Add calibrated evidence image asset and scan region.");
        }

        private void PortRecordGoblinEvidence(GoblinEvidenceCandidate candidate)
        {
            DateTime now = DateTime.Now;
            lock (portGoblinEvidenceLock)
            {
                if (portLastGoblinEvidenceByType.TryGetValue(candidate.Type, out DateTime lastEvidence) &&
                    now - lastEvidence < GoblinEvidenceCooldown)
                {
                    return;
                }

                portLastGoblinEvidenceByType[candidate.Type] = now;
            }

            string screenshotPath = PortCaptureGoblinEvidenceScreenshot(candidate.Type, now);
            GoblinEvidenceEvent evidenceEvent = new(
                now,
                candidate.Type,
                candidate.Confidence,
                candidate.Source,
                screenshotPath,
                candidate.Notes);

            DebugManager.Session.RecordGoblinEvidence(evidenceEvent);
            AppLogger.Info($"GoblinEvidence: Type={candidate.Type}; Confidence={candidate.Confidence:0.00}; Source={candidate.Source}; Screenshot={PortLogField(PortDisplayLocation(screenshotPath))}; Notes={PortLogField(candidate.Notes)}");
            PortWriteSessionMetadata(logSuccess: false);
            PortUpdateGoblinTrackerStats();
        }

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

            if (!PortTryCaptureGoblinEvidenceSourceScreenshot(out Bitmap screenshot, "GoblinCalibration"))
            {
                AppLogger.Info("GoblinCalibration: Snapshot failed; reason=source screenshot unavailable");
                return;
            }

            using (screenshot)
            {
                screenshot.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);

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
                    $"Full Screenshot Path: {fullPath}",
                    $"Minimap Crop Path: {PortDisplayLocation(savedMinimapPath)}",
                    $"Journal Crop Path: {PortDisplayLocation(savedJournalPath)}",
                    $"Minimap Region: {PortFormatGoblinCalibrationRegion(minimapRegion)}",
                    $"Journal Region: {PortFormatGoblinCalibrationRegion(journalRegion)}",
                ]);

                DebugManager.RecordDebugScreenshotPath(fullPath);
                AppLogger.Info("GoblinCalibration: Snapshot saved");
                AppLogger.Info($"GoblinCalibration: Full={PortLogField(fullPath)}");
                AppLogger.Info($"GoblinCalibration: Minimap={PortLogField(PortDisplayLocation(savedMinimapPath))}; Region={PortFormatGoblinCalibrationRegion(minimapRegion)}");
                AppLogger.Info($"GoblinCalibration: Journal={PortLogField(PortDisplayLocation(savedJournalPath))}; Region={PortFormatGoblinCalibrationRegion(journalRegion)}");
                AppLogger.Info($"GoblinCalibration: Metadata={PortLogField(metadataPath)}");
            }
        }

        private bool PortTryCaptureGoblinEvidenceSourceScreenshot(out Bitmap screenshot, string reason)
        {
            screenshot = new Bitmap(1, 1);

            IntPtr diabloWindow = FindDiabloWindow();
            RECT rect;
            if (diabloWindow != IntPtr.Zero && PortTryGetDiabloClientScreenRect(diabloWindow, reason, out rect))
            {
                AppLogger.Info($"GoblinCalibration: Capturing Diablo client screenshot; reason={reason}");
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
                AppLogger.Info($"GoblinCalibration: Capturing virtual screen fallback; reason={reason}; bounds={screen.Left},{screen.Top},{screen.Width},{screen.Height}");
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0)
            {
                AppLogger.Info($"GoblinCalibration: Source screenshot skipped; reason={reason}; invalidBounds={rect.Left},{rect.Top},{rect.Right},{rect.Bottom}");
                return false;
            }

            screenshot.Dispose();
            screenshot = new Bitmap(width, height);
            using Graphics graphics = Graphics.FromImage(screenshot);
            graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, screenshot.Size);
            return true;
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

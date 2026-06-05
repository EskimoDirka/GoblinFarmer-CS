using System.Drawing;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private const int GoblinEvidenceScanIntervalMs = 750;
        private const int GoblinEvidenceObservationDiagnosticRetentionCount = 24;
        private static readonly TimeSpan GoblinEvidenceCooldown = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan GoblinEvidenceMissingTemplateLogCooldown = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan GoblinEvidenceDiagnosticLogCooldown = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan GoblinEvidenceDiagnosticCropCooldown = TimeSpan.FromSeconds(60);
        // Minimap calibration region derived from real calibration capture using ShareX measurements at 2560x1440.
        // May require future scaling adjustments for different resolutions/UI scales.
        private static readonly Rectangle GoblinEvidenceCalibrationMinimapReferenceRegion = new(2108, 66, 421, 423);
        // Journal calibration region derived from ShareX measurements at 2560x1440.
        // Sized to capture the Diablo journal/event feed area used for future goblin evidence detection.
        // May require scaling adjustments for different resolutions or UI scales.
        private static readonly Rectangle GoblinEvidenceCalibrationJournalReferenceRegion = new(64, 736, 645, 417);
        private readonly object portGoblinEvidenceLock = new();
        private readonly Dictionary<GoblinEvidenceType, DateTime> portLastGoblinEvidenceByType = new();
        private readonly Dictionary<GoblinEvidenceType, DateTime> portLastGoblinEvidenceMissingTemplateLogByType = new();
        private readonly Dictionary<string, DateTime> portLastGoblinEvidenceScanDiagnosticByKey = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> portLastGoblinEvidenceDetectorDiagnosticByKey = new(StringComparer.OrdinalIgnoreCase);
        private Task? portGoblinEvidenceScannerTask;
        private int portGoblinEvidenceCalibrationCaptureActive;
        private int portGoblinEvidenceMissingTemplateSetupLogged;
        private int portGoblinEvidenceMissingTemplateNotificationShown;
        private int portGoblinEvidenceTemplateReadyLogged;
        private long portLastGoblinEvidenceDiagnosticCropTicks;
        private long portLastGoblinEvidenceMissingTemplateScanSummaryTicks;

        private void PortStartGoblinEvidenceScanner(CancellationToken token)
        {
            if (portGoblinEvidenceScannerTask != null && !portGoblinEvidenceScannerTask.IsCompleted)
            {
                AppLogger.Info($"GoblinEvidenceScannerStartSkipped: reason=AlreadyRunning; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; cancelled={token.IsCancellationRequested}");
                return;
            }

            PortValidateGoblinEvidenceTemplateSetup("ScannerStart", notifyIfMissing: true);
            AppLogger.Info($"GoblinEvidenceScannerStartRequested: intervalMs={GoblinEvidenceScanIntervalMs}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}; cancelled={token.IsCancellationRequested}");
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
            AppLogger.Info($"GoblinEvidenceScannerStarted: intervalMs={GoblinEvidenceScanIntervalMs}; cooldownSeconds={GoblinEvidenceCooldown.TotalSeconds:0}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}");

            while (!token.IsCancellationRequested && portCombatRunning)
            {
                string skipReason = PortGoblinEvidenceScanSkipReason(token);
                if (string.IsNullOrWhiteSpace(skipReason))
                {
                    PortScanGoblinEvidence();
                }
                else
                {
                    PortLogGoblinEvidenceScanDiagnostic("GoblinEvidenceScanSkipped", skipReason);
                }

                PortSleep(token, GoblinEvidenceScanIntervalMs);
            }

            AppLogger.Info($"GoblinEvidenceScannerStopped: combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; cancelled={token.IsCancellationRequested}; stopReason={(token.IsCancellationRequested ? "CancellationRequested" : portCombatRunning ? "LoopExited" : "CombatStopped")}");
        }

        private bool PortShouldScanGoblinEvidence(CancellationToken token)
        {
            return string.IsNullOrWhiteSpace(PortGoblinEvidenceScanSkipReason(token));
        }

        private string PortGoblinEvidenceScanSkipReason(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return "CancellationRequested";
            }

            if (!portCombatRunning)
            {
                return "CombatInactive";
            }

            if (portCombatStopping)
            {
                return "CombatStopping";
            }

            if (isAutomationRunning)
            {
                return "AutomationRunning";
            }

            if (!IsDiabloRunning())
            {
                return "DiabloNotRunning";
            }

            if (!PortDiabloIsActive())
            {
                return "DiabloInactive";
            }

            return "";
        }

        private void PortScanGoblinEvidence()
        {
            DateTime scanTime = DateTime.Now;
            string journalCropPath = "";
            string minimapCropPath = "";
            if (PortShouldCaptureGoblinEvidenceDiagnosticCrops(scanTime))
            {
                journalCropPath = PortCaptureGoblinEvidenceDiagnosticCrop("Journal", PortGoblinEvidenceJournalRegion(), scanTime);
                minimapCropPath = PortCaptureGoblinEvidenceDiagnosticCrop("Minimap", PortGoblinEvidenceMinimapRegion(), scanTime);
            }

            PortLogGoblinEvidenceScanDiagnostic(
                "GoblinEvidenceScanAttempted",
                $"Eligible; journalCropPath={PortLogField(PortDisplayLocation(journalCropPath))}; minimapCropPath={PortLogField(PortDisplayLocation(minimapCropPath))}");

            IReadOnlyList<GoblinEvidenceTemplateRequirement> missingTemplates = PortMissingGoblinEvidenceTemplates();
            if (missingTemplates.Count > 0)
            {
                PortLogGoblinEvidenceMissingTemplateSetup("ScannerScan", missingTemplates, notifyIfMissing: true);
                PortLogGoblinEvidenceMissingTemplateScanSummary(missingTemplates);
                PortCleanupOldGoblinEvidenceObservationDiagnostics();
                return;
            }

            int candidateCount = 0;
            foreach (GoblinEvidenceCandidate candidate in PortDetectGoblinEvidenceCandidates())
            {
                candidateCount++;
                PortRecordGoblinEvidence(candidate);
            }

            if (candidateCount == 0)
            {
                PortLogGoblinEvidenceScanDiagnostic(
                    "GoblinEvidenceScanResult",
                    "NoCandidate");
            }
            else
            {
                AppLogger.Info($"GoblinEvidenceScanResult: candidateFound=True; candidateCount={candidateCount}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}");
            }

            PortCleanupOldGoblinEvidenceObservationDiagnostics();
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
            GoblinEvidenceTemplateRequirement template = PortGoblinEvidenceTemplateRequirement(GoblinEvidenceType.JournalKill);
            return PortDetectGoblinEvidenceTemplate(
                template.Type,
                template.Source,
                Img("Goblin Evidence", template.FileName),
                PortGoblinEvidenceJournalRegion(),
                template.Threshold,
                "Template placeholder for a goblin kill journal entry.");
        }

        private GoblinEvidenceCandidate? PortDetectJournalEncounterEvidence()
        {
            // TODO: Replace template placeholder with calibrated journal encounter region/assets.
            GoblinEvidenceTemplateRequirement template = PortGoblinEvidenceTemplateRequirement(GoblinEvidenceType.JournalEncounter);
            return PortDetectGoblinEvidenceTemplate(
                template.Type,
                template.Source,
                Img("Goblin Evidence", template.FileName),
                PortGoblinEvidenceJournalRegion(),
                template.Threshold,
                "Template placeholder for a goblin encounter journal entry.");
        }

        private GoblinEvidenceCandidate? PortDetectMinimapGoblinEvidence()
        {
            // TODO: Add calibrated minimap icon template(s) and tighten this region per resolution.
            GoblinEvidenceTemplateRequirement template = PortGoblinEvidenceTemplateRequirement(GoblinEvidenceType.MinimapIcon);
            return PortDetectGoblinEvidenceTemplate(
                template.Type,
                template.Source,
                Img("Goblin Evidence", template.FileName),
                PortGoblinEvidenceMinimapRegion(),
                template.Threshold,
                "Template placeholder for a possible goblin minimap icon.");
        }

        private GoblinEvidenceTemplateRequirement PortGoblinEvidenceTemplateRequirement(GoblinEvidenceType type)
        {
            return GoblinEvidenceTemplateRequirements.Required.First(requirement => requirement.Type == type);
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
                PortLogGoblinEvidenceDetectorDiagnostic(
                    type,
                    source,
                    "Skipped",
                    "MissingTemplate",
                    imagePath,
                    referenceRegion,
                    threshold,
                    0,
                    force: false);
                return null;
            }

            double confidence = PortBestTemplateConfidenceInDiabloRegion(imagePath, referenceRegion);
            if (confidence < threshold)
            {
                PortLogGoblinEvidenceDetectorDiagnostic(
                    type,
                    source,
                    "NotFound",
                    "BelowThreshold",
                    imagePath,
                    referenceRegion,
                    threshold,
                    confidence,
                    force: false);
                return null;
            }

            PortLogGoblinEvidenceDetectorDiagnostic(
                type,
                source,
                "Found",
                "ConfidenceMet",
                imagePath,
                referenceRegion,
                threshold,
                confidence,
                force: true);
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

        private void PortValidateGoblinEvidenceTemplateSetup(string context, bool notifyIfMissing)
        {
            IReadOnlyList<GoblinEvidenceTemplateRequirement> missingTemplates = PortMissingGoblinEvidenceTemplates();
            if (missingTemplates.Count > 0)
            {
                PortLogGoblinEvidenceMissingTemplateSetup(context, missingTemplates, notifyIfMissing);
                return;
            }

            if (Interlocked.Exchange(ref portGoblinEvidenceTemplateReadyLogged, 1) == 0)
            {
                AppLogger.Info($"GoblinEvidenceTemplateSetupReady: context={context}; requiredCount={GoblinEvidenceTemplateRequirements.Required.Count}; folder={PortLogField(PortGoblinEvidenceTemplateDirectory())}");
            }
        }

        private IReadOnlyList<GoblinEvidenceTemplateRequirement> PortMissingGoblinEvidenceTemplates()
        {
            return GoblinEvidenceTemplateRequirements.MissingRequiredTemplates(PortGoblinEvidenceTemplateDirectory());
        }

        private string PortGoblinEvidenceTemplateDirectory()
        {
            string markerPath = Img("Goblin Evidence", "README.md");
            return Path.GetDirectoryName(markerPath) ?? Path.Combine(AppContext.BaseDirectory, "Images", "Goblin Evidence");
        }

        private void PortLogGoblinEvidenceMissingTemplateSetup(
            string context,
            IReadOnlyList<GoblinEvidenceTemplateRequirement> missingTemplates,
            bool notifyIfMissing)
        {
            if (missingTemplates.Count == 0)
            {
                return;
            }

            if (Interlocked.Exchange(ref portGoblinEvidenceMissingTemplateSetupLogged, 1) == 0)
            {
                string missing = string.Join("|", missingTemplates.Select(GoblinEvidenceTemplateRequirements.DisplayPath));
                string required = string.Join("|", GoblinEvidenceTemplateRequirements.Required.Select(GoblinEvidenceTemplateRequirements.DisplayPath));
                AppLogger.Info($"GoblinEvidenceTemplateSetupMissing: context={context}; missingCount={missingTemplates.Count}; missingTemplates={PortLogField(missing)}; requiredTemplates={PortLogField(required)}; folder={PortLogField(PortGoblinEvidenceTemplateDirectory())}; guidance={PortLogField("Add manually calibrated PNG templates. Use Ctrl+Shift+G Calibration or ObservationDiagnostics crops as references only; do not auto-create templates from random crops.")}");
            }

            if (notifyIfMissing &&
                DebugManager.IsVisualStudioDebugSession &&
                Interlocked.Exchange(ref portGoblinEvidenceMissingTemplateNotificationShown, 1) == 0)
            {
                string missingNames = string.Join(", ", missingTemplates.Select(template => template.FileName));
                PortShowSplash($"Goblin Evidence templates missing\r\n{missingNames}\r\nUse Ctrl+Shift+G for capture references", 7000);
            }
        }

        private void PortLogGoblinEvidenceMissingTemplateScanSummary(IReadOnlyList<GoblinEvidenceTemplateRequirement> missingTemplates)
        {
            long nowTicks = DateTime.Now.Ticks;
            long lastTicks = Interlocked.Read(ref portLastGoblinEvidenceMissingTemplateScanSummaryTicks);
            if (nowTicks - lastTicks < GoblinEvidenceMissingTemplateLogCooldown.Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastGoblinEvidenceMissingTemplateScanSummaryTicks, nowTicks);
            string missing = string.Join("|", missingTemplates.Select(GoblinEvidenceTemplateRequirements.DisplayPath));
            AppLogger.Info($"GoblinEvidenceScanResult: candidateFound=False; reason=MissingTemplate; missingCount={missingTemplates.Count}; missingTemplates={PortLogField(missing)}; diagnosticCropsContinue=True; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}");
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

        private void PortLogGoblinEvidenceScanDiagnostic(string eventName, string reason)
        {
            DateTime now = DateTime.Now;
            string key = $"{eventName}|{reason}";
            lock (portGoblinEvidenceLock)
            {
                if (portLastGoblinEvidenceScanDiagnosticByKey.TryGetValue(key, out DateTime lastLogged) &&
                    now - lastLogged < GoblinEvidenceDiagnosticLogCooldown)
                {
                    return;
                }

                portLastGoblinEvidenceScanDiagnosticByKey[key] = now;
            }

            AppLogger.Info($"{eventName}: reason={PortLogField(reason)}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}");
        }

        private void PortLogGoblinEvidenceDetectorDiagnostic(
            GoblinEvidenceType type,
            string source,
            string result,
            string reason,
            string imagePath,
            Rectangle referenceRegion,
            double threshold,
            double confidence,
            bool force)
        {
            DateTime now = DateTime.Now;
            string key = $"{type}|{result}|{reason}";
            if (!force)
            {
                lock (portGoblinEvidenceLock)
                {
                    if (portLastGoblinEvidenceDetectorDiagnosticByKey.TryGetValue(key, out DateTime lastLogged) &&
                        now - lastLogged < GoblinEvidenceDiagnosticLogCooldown)
                    {
                        return;
                    }

                    portLastGoblinEvidenceDetectorDiagnosticByKey[key] = now;
                }
            }

            AppLogger.Info($"GoblinEvidenceCandidateCheck: type={type}; source={source}; result={result}; reason={reason}; confidence={confidence:0.000}; threshold={threshold:0.000}; template={PortLogField(imagePath)}; templateExists={File.Exists(imagePath)}; referenceRegion={FormatRectangle(referenceRegion)}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}");
        }

        private bool PortShouldCaptureGoblinEvidenceDiagnosticCrops(DateTime now)
        {
            long nowTicks = now.Ticks;
            long lastTicks = Interlocked.Read(ref portLastGoblinEvidenceDiagnosticCropTicks);
            if (nowTicks - lastTicks < GoblinEvidenceDiagnosticCropCooldown.Ticks)
            {
                return false;
            }

            Interlocked.Exchange(ref portLastGoblinEvidenceDiagnosticCropTicks, nowTicks);
            return true;
        }

        private string PortCaptureGoblinEvidenceDiagnosticCrop(string label, Rectangle referenceRegion, DateTime timestamp)
        {
            try
            {
                if (!PortTryGetDiabloRect(out RECT diabloRect))
                {
                    AppLogger.Info($"GoblinEvidenceCropSkipped: label={label}; reason=DiabloRectUnavailable; referenceRegion={FormatRectangle(referenceRegion)}");
                    return "";
                }

                Rectangle screenRegion = PortScaleReferenceRectangle(referenceRegion, diabloRect);
                screenRegion = Rectangle.Intersect(SystemInformation.VirtualScreen, screenRegion);
                if (screenRegion.Width <= 0 || screenRegion.Height <= 0)
                {
                    AppLogger.Info($"GoblinEvidenceCropSkipped: label={label}; reason=InvalidScreenRegion; referenceRegion={FormatRectangle(referenceRegion)}; screenRegion={FormatRectangle(screenRegion)}");
                    return "";
                }

                string directory = Path.Combine(DebugManager.GoblinEvidenceDirectory, "ObservationDiagnostics");
                Directory.CreateDirectory(directory);
                string safeLabel = PortSafeScreenshotName(label, "Unknown");
                string path = Path.Combine(directory, $"GoblinEvidenceScan_{timestamp:yyyyMMdd_HHmmss_fff}_{safeLabel}.png");
                RECT captureRect = new()
                {
                    Left = screenRegion.Left,
                    Top = screenRegion.Top,
                    Right = screenRegion.Right,
                    Bottom = screenRegion.Bottom,
                };
                string savedPath = PortCaptureScreenRectangleToFile(captureRect, path, $"GoblinEvidenceScan:{safeLabel}");
                AppLogger.Info($"GoblinEvidenceCropSaved: label={safeLabel}; path={PortLogField(PortDisplayLocation(savedPath))}; referenceRegion={FormatRectangle(referenceRegion)}; screenRegion={FormatRectangle(screenRegion)}");
                return savedPath;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Goblin evidence diagnostic crop failed: label={label}", ex);
                return "";
            }
        }

        private void PortCleanupOldGoblinEvidenceObservationDiagnostics()
        {
            try
            {
                string directory = Path.Combine(DebugManager.GoblinEvidenceDirectory, "ObservationDiagnostics");
                CleanupResult result = DebugManager.CleanupOldGoblinEvidence(directory, GoblinEvidenceObservationDiagnosticRetentionCount);
                if (result.Deleted > 0)
                {
                    AppLogger.Info($"GoblinEvidenceObservationDiagnostics retention cleanup complete: scanned={result.Scanned}; deleted={result.Deleted}; skipped={result.Skipped}; kept={result.Kept}; retentionCount={GoblinEvidenceObservationDiagnosticRetentionCount}; folder={directory}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("GoblinEvidenceObservationDiagnostics retention cleanup failed.", ex);
            }
        }

        private void PortRecordGoblinEvidence(GoblinEvidenceCandidate candidate)
        {
            candidate = candidate with { GoblinType = GoblinTypeNormalizer.Normalize(candidate.GoblinType) };
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
            PortObserveGoblinCandidate(candidate.Source, candidate.GoblinType);
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

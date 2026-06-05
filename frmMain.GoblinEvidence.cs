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
        private static readonly Rectangle GoblinEvidenceCalibrationMinimapReferenceRegion = GoblinEvidenceScanRegions.MinimapReferenceRegion;
        // Journal calibration region derived from ShareX measurements at 2560x1440.
        // Sized to capture the Diablo journal/event feed area used for future goblin evidence detection.
        // May require scaling adjustments for different resolutions or UI scales.
        private static readonly Rectangle GoblinEvidenceCalibrationJournalReferenceRegion = GoblinEvidenceScanRegions.JournalReferenceRegion;
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
        private int portGoblinEvidenceTemplateWarningLogged;
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

            GoblinEvidenceTemplateCatalog templateCatalog = PortGoblinEvidenceTemplateCatalog();
            if (!templateCatalog.HasUsableTemplates)
            {
                PortLogGoblinEvidenceTemplateSetupMissing("ScannerScan", templateCatalog, notifyIfMissing: true);
                PortLogGoblinEvidenceMissingTemplateScanSummary(templateCatalog);
                PortCleanupOldGoblinEvidenceObservationDiagnostics();
                return;
            }

            PortLogGoblinEvidenceTemplateSetupWarning("ScannerScan", templateCatalog);
            int candidateCount = 0;
            foreach (GoblinEvidenceCandidate candidate in PortDetectGoblinEvidenceCandidates(templateCatalog))
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

        private IEnumerable<GoblinEvidenceCandidate> PortDetectGoblinEvidenceCandidates(GoblinEvidenceTemplateCatalog templateCatalog)
        {
            foreach (IGrouping<string, GoblinEvidenceTemplateRequirement> sourceGroup in templateCatalog.Templates.GroupBy(template => template.Source, StringComparer.OrdinalIgnoreCase))
            {
                IReadOnlyList<GoblinEvidenceTemplateRequirement> templates = sourceGroup.ToList();
                Rectangle scanRegion = PortGoblinEvidenceRegionForSource(sourceGroup.Key);
                GoblinEvidenceCandidate? candidate = PortDetectBestGoblinEvidenceTemplate(templates, scanRegion);
                PortLogGoblinEvidenceSourceScanResult(sourceGroup.Key, scanRegion, candidate, templates.Count);

                if (candidate != null)
                {
                    yield return candidate;
                }
            }
        }

        private Rectangle PortGoblinEvidenceRegionForSource(string source)
        {
            return string.Equals(source, "MinimapCandidate", StringComparison.OrdinalIgnoreCase)
                ? PortGoblinEvidenceMinimapRegion()
                : PortGoblinEvidenceJournalRegion();
        }

        private GoblinEvidenceCandidate? PortDetectBestGoblinEvidenceTemplate(
            IReadOnlyList<GoblinEvidenceTemplateRequirement> templates,
            Rectangle referenceRegion)
        {
            GoblinEvidenceTemplateRequirement? bestTemplate = null;
            string bestImagePath = "";
            double bestConfidence = 0;
            foreach (GoblinEvidenceTemplateRequirement template in templates)
            {
                string imagePath = Img("Goblin Evidence", template.FileName);
                if (!File.Exists(imagePath))
                {
                    continue;
                }

                double confidence = PortBestTemplateConfidenceInDiabloRegion(imagePath, referenceRegion);
                if (bestTemplate == null || confidence > bestConfidence)
                {
                    bestTemplate = template;
                    bestImagePath = imagePath;
                    bestConfidence = confidence;
                }
            }

            if (bestTemplate == null)
            {
                return null;
            }

            if (bestConfidence < bestTemplate.Threshold)
            {
                PortLogGoblinEvidenceDetectorDiagnostic(
                    bestTemplate,
                    "NotFound",
                    "BelowThreshold",
                    bestImagePath,
                    referenceRegion,
                    bestConfidence,
                    force: false);
                return null;
            }

            PortLogGoblinEvidenceDetectorDiagnostic(
                bestTemplate,
                "Found",
                "ConfidenceMet",
                bestImagePath,
                referenceRegion,
                bestConfidence,
                force: true);
            return new GoblinEvidenceCandidate(
                bestTemplate.Type,
                bestConfidence,
                bestTemplate.Source,
                $"Template={bestTemplate.FileName}; Kind={bestTemplate.Kind}",
                bestTemplate.GoblinType);
        }

        private Rectangle PortGoblinEvidenceJournalRegion()
        {
            return GoblinEvidenceCalibrationJournalReferenceRegion;
        }

        private Rectangle PortGoblinEvidenceMinimapRegion()
        {
            return GoblinEvidenceCalibrationMinimapReferenceRegion;
        }

        private void PortValidateGoblinEvidenceTemplateSetup(string context, bool notifyIfMissing)
        {
            GoblinEvidenceTemplateCatalog templateCatalog = PortGoblinEvidenceTemplateCatalog();
            if (!templateCatalog.HasUsableTemplates)
            {
                PortLogGoblinEvidenceTemplateSetupMissing(context, templateCatalog, notifyIfMissing);
                return;
            }

            if (Interlocked.Exchange(ref portGoblinEvidenceTemplateReadyLogged, 1) == 0)
            {
                AppLogger.Info($"GoblinEvidenceTemplateSetupReady: context={context}; templateCount={templateCatalog.Templates.Count}; journalTemplateCount={templateCatalog.Templates.Count(template => template.Source.Equals("JournalCandidate", StringComparison.OrdinalIgnoreCase))}; minimapTemplateCount={templateCatalog.Templates.Count(template => template.Source.Equals("MinimapCandidate", StringComparison.OrdinalIgnoreCase))}; invalidTemplateCount={templateCatalog.InvalidTemplates.Count}; folder={PortLogField(PortGoblinEvidenceTemplateDirectory())}; naming={PortLogField(GoblinEvidenceTemplateRequirements.NamingGuidance())}");
            }

            PortLogGoblinEvidenceTemplateSetupWarning(context, templateCatalog);
        }

        private GoblinEvidenceTemplateCatalog PortGoblinEvidenceTemplateCatalog()
        {
            return GoblinEvidenceTemplateRequirements.DiscoverTemplates(PortGoblinEvidenceTemplateDirectory());
        }

        private string PortGoblinEvidenceTemplateDirectory()
        {
            string markerPath = Img("Goblin Evidence", "README.md");
            return Path.GetDirectoryName(markerPath) ?? Path.Combine(AppContext.BaseDirectory, "Images", "Goblin Evidence");
        }

        private void PortLogGoblinEvidenceTemplateSetupMissing(
            string context,
            GoblinEvidenceTemplateCatalog templateCatalog,
            bool notifyIfMissing)
        {
            if (Interlocked.Exchange(ref portGoblinEvidenceMissingTemplateSetupLogged, 1) == 0)
            {
                string invalid = PortGoblinEvidenceInvalidTemplateSummary(templateCatalog);
                AppLogger.Info($"GoblinEvidenceTemplateSetupMissing: context={context}; templateCount={templateCatalog.Templates.Count}; invalidTemplateCount={templateCatalog.InvalidTemplates.Count}; invalidTemplates={PortLogField(invalid)}; folder={PortLogField(PortGoblinEvidenceTemplateDirectory())}; naming={PortLogField(GoblinEvidenceTemplateRequirements.NamingGuidance())}; guidance={PortLogField("Add manually calibrated per-goblin PNG templates. Use Ctrl+Shift+G Calibration or ObservationDiagnostics crops as references only; do not auto-create templates from random crops.")}");
            }

            if (notifyIfMissing &&
                DebugManager.IsVisualStudioDebugSession &&
                Interlocked.Exchange(ref portGoblinEvidenceMissingTemplateNotificationShown, 1) == 0)
            {
                PortShowSplash($"Goblin Evidence templates missing\r\nAdd per-goblin Journal/Minimap PNGs\r\nUse Ctrl+Shift+G for capture references", 7000);
            }
        }

        private void PortLogGoblinEvidenceTemplateSetupWarning(string context, GoblinEvidenceTemplateCatalog templateCatalog)
        {
            if (templateCatalog.InvalidTemplates.Count == 0 && templateCatalog.HasJournalTemplates && templateCatalog.HasMinimapTemplates)
            {
                return;
            }

            if (Interlocked.Exchange(ref portGoblinEvidenceTemplateWarningLogged, 1) != 0)
            {
                return;
            }

            string invalid = PortGoblinEvidenceInvalidTemplateSummary(templateCatalog);
            AppLogger.Info($"GoblinEvidenceTemplateSetupWarning: context={context}; templateCount={templateCatalog.Templates.Count}; journalTemplates={templateCatalog.HasJournalTemplates}; minimapTemplates={templateCatalog.HasMinimapTemplates}; invalidTemplateCount={templateCatalog.InvalidTemplates.Count}; invalidTemplates={PortLogField(invalid)}; folder={PortLogField(PortGoblinEvidenceTemplateDirectory())}; naming={PortLogField(GoblinEvidenceTemplateRequirements.NamingGuidance())}");
        }

        private string PortGoblinEvidenceInvalidTemplateSummary(GoblinEvidenceTemplateCatalog templateCatalog)
        {
            return templateCatalog.InvalidTemplates.Count == 0
                ? "none"
                : string.Join("|", templateCatalog.InvalidTemplates.Select(template => $"{template.FileName}:{template.Reason}"));
        }

        private void PortLogGoblinEvidenceMissingTemplateScanSummary(GoblinEvidenceTemplateCatalog templateCatalog)
        {
            long nowTicks = DateTime.Now.Ticks;
            long lastTicks = Interlocked.Read(ref portLastGoblinEvidenceMissingTemplateScanSummaryTicks);
            if (nowTicks - lastTicks < GoblinEvidenceMissingTemplateLogCooldown.Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastGoblinEvidenceMissingTemplateScanSummaryTicks, nowTicks);
            AppLogger.Info($"GoblinEvidenceScanResult: candidateFound=False; reason=MissingTemplate; templateCount={templateCatalog.Templates.Count}; invalidTemplateCount={templateCatalog.InvalidTemplates.Count}; invalidTemplates={PortLogField(PortGoblinEvidenceInvalidTemplateSummary(templateCatalog))}; diagnosticCropsContinue=True; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}");
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
            GoblinEvidenceTemplateRequirement template,
            string result,
            string reason,
            string imagePath,
            Rectangle referenceRegion,
            double confidence,
            bool force)
        {
            DateTime now = DateTime.Now;
            string key = $"{template.Source}|{template.Type}|{template.Kind}|{template.GoblinType}|{result}|{reason}";
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

            AppLogger.Info($"GoblinEvidenceCandidateCheck: type={template.Type}; source={template.Source}; goblinType={PortLogField(template.GoblinType)}; evidenceKind={template.Kind}; result={result}; reason={reason}; confidence={confidence:0.000}; threshold={template.Threshold:0.000}; template={PortLogField(imagePath)}; templateExists={File.Exists(imagePath)}; scanRegion={FormatRectangle(referenceRegion)}; screenRegion={PortGoblinEvidenceScreenRegionForLog(referenceRegion)}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}");
        }

        private void PortLogGoblinEvidenceSourceScanResult(
            string source,
            Rectangle referenceRegion,
            GoblinEvidenceCandidate? candidate,
            int templateCount)
        {
            string observationSource = PortNormalizeGoblinObservationSource(source);
            bool candidateFound = candidate != null;
            string goblinType = candidateFound ? candidate!.GoblinType : "None";
            double confidence = candidateFound ? candidate!.Confidence : 0;
            AppLogger.Info($"GoblinEvidenceScanResult source={observationSource} scanRegion={FormatRectangle(referenceRegion)} screenRegion={PortGoblinEvidenceScreenRegionForLog(referenceRegion)} candidateFound={candidateFound} templateCount={templateCount} goblinType={PortLogField(goblinType)} confidence={confidence:0.000}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}");
        }

        private string PortGoblinEvidenceScreenRegionForLog(Rectangle referenceRegion)
        {
            if (!PortTryGetDiabloRect(out RECT diabloRect))
            {
                return "Unavailable";
            }

            Rectangle screenRegion = PortScaleReferenceRectangle(referenceRegion, diabloRect);
            screenRegion = Rectangle.Intersect(SystemInformation.VirtualScreen, screenRegion);
            return FormatRectangle(screenRegion);
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
                AppLogger.Info($"GoblinEvidenceCropSkipped: label={label}; reason=DiabloRectUnavailable; scanRegion={FormatRectangle(referenceRegion)}");
                return "";
            }

                Rectangle screenRegion = PortScaleReferenceRectangle(referenceRegion, diabloRect);
                screenRegion = Rectangle.Intersect(SystemInformation.VirtualScreen, screenRegion);
                if (screenRegion.Width <= 0 || screenRegion.Height <= 0)
                {
                    AppLogger.Info($"GoblinEvidenceCropSkipped: label={label}; reason=InvalidScreenRegion; scanRegion={FormatRectangle(referenceRegion)}; screenRegion={FormatRectangle(screenRegion)}");
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
                AppLogger.Info($"GoblinEvidenceCropSaved: label={safeLabel}; path={PortLogField(PortDisplayLocation(savedPath))}; scanRegion={FormatRectangle(referenceRegion)}; screenRegion={FormatRectangle(screenRegion)}");
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

using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private const int GoblinEvidenceScanIntervalMs = 500;
        private const int GoblinJournalActiveFeedMinimumY = GoblinJournalEvidencePolicy.ActiveFeedMinimumY;
        private const double GoblinJournalNameValidationThreshold = 0.80;
        private const int GoblinEvidenceObservationDiagnosticRetentionCount = 24;
        private static readonly TimeSpan GoblinEvidenceCooldown = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan GoblinJournalEvidenceFreshWindow = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan GoblinJournalNewGameCarryoverSuppressWindow = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan GoblinJournalHistoryInputSuppressWindow = TimeSpan.FromSeconds(12);
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
        private readonly Dictionary<string, GoblinJournalEvidenceSeenState> portJournalEvidenceSeenByKey = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GoblinJournalEngagedState> portRecentJournalEngagedByGoblinType = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GoblinJournalStaleSuppressedState> portStaleSuppressedJournalEvidenceByKey = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GoblinJournalKilledState> portJournalKilledEvidenceSeenBySignature = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GoblinJournalResetCarryoverSuppressedState> portJournalResetCarryoverSuppressedBySignature = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GoblinEvidenceCachedTemplate> portGoblinEvidenceTemplateMatCache = new(StringComparer.OrdinalIgnoreCase);
        private GoblinEvidenceTemplateCatalog? portCachedGoblinEvidenceTemplateCatalog;
        private string portCachedGoblinEvidenceTemplateCatalogDirectory = "";
        private DateTime portCachedGoblinEvidenceTemplateCatalogWriteUtc;
        private IGoblinEvidenceFrameSource? portGoblinEvidenceFrameSource;
        private CancellationTokenSource? portGoblinEvidenceObservationCts;
        private Task? portGoblinEvidenceScannerTask;
        private int portGoblinEvidenceCalibrationCaptureActive;
        private int portGoblinEvidenceMissingTemplateSetupLogged;
        private int portGoblinEvidenceMissingTemplateNotificationShown;
        private int portGoblinEvidenceTemplateReadyLogged;
        private int portGoblinEvidenceTemplateWarningLogged;
        private int portGoblinObservationModeConfigurationLogged;
        private long portLastGoblinEvidenceDiagnosticCropTicks;
        private long portLastGoblinEvidenceMissingTemplateScanSummaryTicks;
        private long portGoblinJournalHistorySuppressUntilTicks;
        private long portGoblinJournalNewGameCarryoverSuppressUntilTicks;

        private void PortStartGoblinEvidenceScanner(CancellationToken token)
        {
            PortStartGoblinObservationScanner("CombatStart");
        }

        private void PortStartGoblinObservationScanner(string source)
        {
            if (portGoblinEvidenceScannerTask != null && !portGoblinEvidenceScannerTask.IsCompleted)
            {
                AppLogger.Info($"ObservationScannerStartSkipped: reason=AlreadyRunning; source={PortLogField(source)}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}; currentArea={PortLogField(PortDisplayLocation(portLastConfirmedLocation))}");
                return;
            }

            portGoblinEvidenceObservationCts?.Cancel();
            portGoblinEvidenceObservationCts?.Dispose();
            portGoblinEvidenceObservationCts = new CancellationTokenSource();
            CancellationToken scannerToken = portGoblinEvidenceObservationCts.Token;
            PortValidateGoblinEvidenceTemplateSetup("ObservationScannerStart", notifyIfMissing: true);
            PortLogGoblinObservationModeConfiguration("ObservationScannerStart");
            AppLogger.Info($"ObservationScannerStartRequested: source={PortLogField(source)}; intervalMs={GoblinEvidenceScanIntervalMs}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}; currentArea={PortLogField(PortDisplayLocation(portLastConfirmedLocation))}; enabled={PortGoblinObservationScannerEnabled()}");
            portGoblinEvidenceScannerTask = Task.Run(() =>
            {
                try
                {
                    PortGoblinEvidenceScannerLoop(scannerToken);
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Goblin observation scanner failed.", ex);
                }
            }, CancellationToken.None);
        }

        private void PortStopGoblinObservationScanner(string reason)
        {
            CancellationTokenSource? cts = portGoblinEvidenceObservationCts;
            if (cts == null)
            {
                return;
            }

            AppLogger.Info($"ObservationScannerStopRequested: reason={PortLogField(reason)}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}; currentArea={PortLogField(PortDisplayLocation(portLastConfirmedLocation))}");
            cts.Cancel();
        }

        private void PortGoblinEvidenceScannerLoop(CancellationToken token)
        {
            AppLogger.Info($"ObservationScannerStarted: intervalMs={GoblinEvidenceScanIntervalMs}; evidenceCooldownSeconds={GoblinEvidenceCooldown.TotalSeconds:0}; journalFreshWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}; currentArea={PortLogField(PortDisplayLocation(portLastConfirmedLocation))}; enabled={PortGoblinObservationScannerEnabled()}");

            while (!token.IsCancellationRequested)
            {
                string skipReason = PortGoblinEvidenceScanSkipReason(token);
                if (string.IsNullOrWhiteSpace(skipReason))
                {
                    PortScanGoblinEvidence();
                }
                else
                {
                    PortLogGoblinEvidenceScanDiagnostic("ObservationScanSkipped", skipReason);
                }

                PortSleep(token, GoblinEvidenceScanIntervalMs);
            }

            AppLogger.Info($"ObservationScannerStopped: combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}; currentArea={PortLogField(PortDisplayLocation(portLastConfirmedLocation))}; cancelled={token.IsCancellationRequested}; stopReason={(token.IsCancellationRequested ? "CancellationRequested" : "LoopExited")}");
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

            if (!PortGoblinObservationScannerEnabled())
            {
                return "ObservationModeDisabled";
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

        private static bool PortGoblinObservationScannerEnabled()
        {
            return AppSettings.GoblinTracker.EnableObservationMode;
        }

        private static bool PortGoblinAutomaticCountingEnabled()
        {
            return AppSettings.GoblinTracker.EnableObservationMode &&
                AppSettings.GoblinTracker.EnableAutomaticCounting;
        }

        private static bool PortGoblinDecisionTraceEnabled()
        {
            return AppSettings.GoblinTracker.EnableDecisionTrace ||
                AppSettings.Debug.DebugMode;
        }

        private void PortLogGoblinObservationModeConfiguration(string context)
        {
            if (Interlocked.Exchange(ref portGoblinObservationModeConfigurationLogged, 1) != 0)
            {
                return;
            }

            AppLogger.Info(
                "ObservationModeConfiguration: " +
                $"context={PortLogField(context)}; " +
                $"enabled={PortGoblinObservationScannerEnabled()}; " +
                $"enableObservationMode={AppSettings.GoblinTracker.EnableObservationMode}; " +
                $"enableAutomaticCounting={AppSettings.GoblinTracker.EnableAutomaticCounting}; " +
                $"observationSetting=GoblinTracker.EnableObservationMode; " +
                $"automaticCountingSetting=GoblinTracker.EnableAutomaticCounting; " +
                $"observationDefaultValue=True; " +
                $"automaticCountingDefaultValue=False; " +
                $"debugMode={AppSettings.Debug.DebugMode}; " +
                $"diagnosticLoggingEnabled={DebugManager.DiagnosticLoggingEnabled}; " +
                $"automaticCountingEnabled={PortGoblinAutomaticCountingEnabled()}; " +
                $"manualHotkeyOnlyCountPath={!PortGoblinAutomaticCountingEnabled()}; " +
                $"configPath={PortLogField(AppSettings.ConfigPath)}");
        }

        private void PortScanGoblinEvidence()
        {
            Stopwatch totalStopwatch = Stopwatch.StartNew();
            DateTime scanTime = DateTime.Now;
            string journalCropPath = "";
            string minimapCropPath = "";
            if (PortShouldCaptureGoblinEvidenceDiagnosticCrops(scanTime))
            {
                Stopwatch cropStopwatch = Stopwatch.StartNew();
                journalCropPath = PortCaptureGoblinEvidenceDiagnosticCrop("Journal", PortGoblinEvidenceJournalRegion(), scanTime);
                minimapCropPath = PortCaptureGoblinEvidenceDiagnosticCrop("Minimap", PortGoblinEvidenceMinimapRegion(), scanTime);
                PortRecordGoblinEvidenceTiming("DiagnosticCrops", cropStopwatch.Elapsed);
            }

            PortLogGoblinEvidenceScanDiagnostic(
                "ObservationScanAttempted",
                $"Eligible; scanOrder=MinimapThenJournal; journalPrimary=True; journalCropPath={PortLogField(PortDisplayLocation(journalCropPath))}; minimapCropPath={PortLogField(PortDisplayLocation(minimapCropPath))}");

            Stopwatch catalogStopwatch = Stopwatch.StartNew();
            GoblinEvidenceTemplateCatalog templateCatalog = PortGoblinEvidenceTemplateCatalog();
            PortRecordGoblinEvidenceTiming("TemplateCatalog", catalogStopwatch.Elapsed);
            if (!templateCatalog.HasUsableTemplates)
            {
                PortLogGoblinEvidenceTemplateSetupMissing("ScannerScan", templateCatalog, notifyIfMissing: true);
                PortLogGoblinEvidenceMissingTemplateScanSummary(templateCatalog);
                PortMarkGoblinObservationNoCurrent("MissingTemplate");
                PortCleanupOldGoblinEvidenceObservationDiagnostics();
                return;
            }

            PortLogGoblinEvidenceTemplateSetupWarning("ScannerScan", templateCatalog);
            int candidateCount = 0;
            Stopwatch detectStopwatch = Stopwatch.StartNew();
            foreach (GoblinEvidenceCandidate candidate in PortDetectGoblinEvidenceCandidates(templateCatalog))
            {
                candidateCount++;
                Stopwatch recordStopwatch = Stopwatch.StartNew();
                PortRecordGoblinEvidence(candidate, forceObservation: true);
                PortRecordGoblinEvidenceTiming("RecordCandidate", recordStopwatch.Elapsed);
            }
            PortRecordGoblinEvidenceTiming("DetectCandidates", detectStopwatch.Elapsed);

            if (candidateCount == 0)
            {
                PortLogGoblinEvidenceScanDiagnostic(
                    "GoblinEvidenceScanResult",
                    "NoCandidate");
                PortMarkGoblinObservationNoCurrent("No current observation");
            }
            else
            {
                AppLogger.Info($"GoblinEvidenceScanResult: candidateFound=True; candidateCount={candidateCount}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}");
            }

            PortCleanupOldGoblinEvidenceObservationDiagnostics();
            PortRecordGoblinEvidenceTiming("TotalScan", totalStopwatch.Elapsed);
        }

        private IEnumerable<GoblinEvidenceCandidate> PortDetectGoblinEvidenceCandidates(GoblinEvidenceTemplateCatalog templateCatalog)
        {
            IReadOnlyList<GoblinEvidenceTemplateRequirement> journalTemplates = templateCatalog.Templates
                .Where(template => template.Source.Equals("JournalCandidate", StringComparison.OrdinalIgnoreCase))
                .ToList();
            IReadOnlyList<GoblinEvidenceTemplateRequirement> minimapTemplates = templateCatalog.Templates
                .Where(template => template.Source.Equals("MinimapCandidate", StringComparison.OrdinalIgnoreCase))
                .ToList();
            bool combatPrioritizesJournal = portCombatRunning || portCombatStopping;

            GoblinEvidenceCandidate? primaryJournalCandidate = null;
            if (combatPrioritizesJournal)
            {
                primaryJournalCandidate = PortTryDetectAcceptedGoblinEvidenceCandidate("JournalCandidate", journalTemplates);
                if (primaryJournalCandidate != null &&
                    !PortGoblinEvidenceCandidateIsPendingJournalEngaged(primaryJournalCandidate))
                {
                    AppLogger.Info(
                        "GoblinEvidenceCandidateSelection: " +
                        "selected=Journal; " +
                        "reason=CombatJournalDecisive; " +
                        $"journalGoblinType={PortLogField(primaryJournalCandidate.GoblinType)}; " +
                        $"journalConfidence={primaryJournalCandidate.Confidence:0.000}; " +
                        $"journalKind={PortLogField(PortGoblinEvidenceNoteValue(primaryJournalCandidate.Notes, "Kind"))}");
                    yield return primaryJournalCandidate;
                    yield break;
                }
            }

            GoblinEvidenceCandidate? strongMinimapCandidate = null;
            GoblinEvidenceCandidate? supportingMinimapCandidate = PortTryDetectAcceptedGoblinEvidenceCandidate("MinimapCandidate", minimapTemplates);
            if (supportingMinimapCandidate != null)
            {
                strongMinimapCandidate = PortGoblinEvidenceCandidateIsStrongMinimap(supportingMinimapCandidate)
                    ? supportingMinimapCandidate
                    : null;
            }

            if (!combatPrioritizesJournal)
            {
                primaryJournalCandidate = PortTryDetectAcceptedGoblinEvidenceCandidate("JournalCandidate", journalTemplates);
            }

            if (primaryJournalCandidate != null)
            {
                if (PortGoblinEvidenceCandidateIsPendingJournalEngaged(primaryJournalCandidate) &&
                    strongMinimapCandidate != null)
                {
                    AppLogger.Info(
                        "GoblinEvidenceCandidateSelection: " +
                        "selected=Minimap; " +
                        "reason=JournalPendingMinimapConfirmed; " +
                        $"journalGoblinType={PortLogField(primaryJournalCandidate.GoblinType)}; " +
                        $"journalConfidence={primaryJournalCandidate.Confidence:0.000}; " +
                        $"journalKind={PortLogField(PortGoblinEvidenceNoteValue(primaryJournalCandidate.Notes, "Kind"))}; " +
                        $"minimapGoblinType={PortLogField(strongMinimapCandidate.GoblinType)}; " +
                        $"minimapConfidence={strongMinimapCandidate.Confidence:0.000}; " +
                        $"minimapCountThreshold={PortAutomaticGoblinMinimapCountMinimumConfidenceFor(strongMinimapCandidate.GoblinType):0.000}");
                    yield return strongMinimapCandidate;
                    yield break;
                }

                yield return primaryJournalCandidate;
                yield break;
            }

            if (supportingMinimapCandidate != null)
            {
                yield return supportingMinimapCandidate;
            }
        }

        private GoblinEvidenceCandidate? PortTryDetectAcceptedGoblinEvidenceCandidate(
            string source,
            IReadOnlyList<GoblinEvidenceTemplateRequirement> templates)
        {
            if (templates.Count == 0)
            {
                return null;
            }

            Rectangle scanRegion = PortGoblinEvidenceRegionForSource(source);
            GoblinEvidenceDetectionResult detection = PortDetectBestGoblinEvidenceTemplate(templates, scanRegion);
            PortLogGoblinEvidenceSourceScanResult(source, scanRegion, detection, templates.Count);
            if (detection.Candidate != null &&
                PortTryAcceptGoblinEvidenceCandidate(source, detection, freshKilledWithoutEngagedReason: "Observation", out GoblinEvidenceCandidate? acceptedCandidate))
            {
                return acceptedCandidate;
            }

            return null;
        }

        private static bool PortGoblinEvidenceCandidateIsPendingJournalEngaged(GoblinEvidenceCandidate candidate)
        {
            if (!PortNormalizeGoblinObservationSource(candidate.Source).Equals("Journal", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string kind = PortGoblinEvidenceNoteValue(candidate.Notes, "Kind");
            return candidate.Type == GoblinEvidenceType.JournalEncounter &&
                kind.Equals(nameof(GoblinEvidenceTemplateKind.JournalEngaged), StringComparison.OrdinalIgnoreCase);
        }

        private static bool PortGoblinEvidenceCandidateIsStrongMinimap(GoblinEvidenceCandidate candidate)
        {
            return PortNormalizeGoblinObservationSource(candidate.Source).Equals("Minimap", StringComparison.OrdinalIgnoreCase) &&
                candidate.Confidence >= PortAutomaticGoblinMinimapCountMinimumConfidenceFor(candidate.GoblinType);
        }

        private Rectangle PortGoblinEvidenceRegionForSource(string source)
        {
            return string.Equals(source, "MinimapCandidate", StringComparison.OrdinalIgnoreCase)
                ? PortGoblinEvidenceMinimapRegion()
                : PortGoblinEvidenceJournalRegion();
        }

        private GoblinEvidenceDetectionResult PortDetectBestGoblinEvidenceTemplate(
            IReadOnlyList<GoblinEvidenceTemplateRequirement> templates,
            Rectangle referenceRegion)
        {
            Stopwatch sourceStopwatch = Stopwatch.StartNew();
            string source = templates.FirstOrDefault()?.Source ?? "";
            using GoblinEvidenceScanContext? scanContext = PortCreateGoblinEvidenceScanContext(source, referenceRegion, "GoblinEvidenceTemplateMatch");
            if (scanContext == null)
            {
                PortRecordGoblinEvidenceTiming("ScanContextUnavailable", sourceStopwatch.Elapsed);
                return new GoblinEvidenceDetectionResult(null, null, "", new GoblinEvidenceTemplateMatch(0, Point.Empty, Point.Empty, Size.Empty), []);
            }

            GoblinEvidenceTemplateRequirement? bestTemplate = null;
            string bestImagePath = "";
            GoblinEvidenceTemplateMatch bestMatch = new(0, Point.Empty, Point.Empty, Size.Empty);
            List<(GoblinEvidenceTemplateRequirement Template, string ImagePath, GoblinEvidenceTemplateMatch Match)> rankedMatches = [];
            foreach (GoblinEvidenceTemplateRequirement template in templates)
            {
                string imagePath = Img("Goblin Evidence", template.FileName);
                if (!File.Exists(imagePath))
                {
                    continue;
                }

                Stopwatch templateStopwatch = Stopwatch.StartNew();
                GoblinEvidenceTemplateMatch match = PortBestGoblinEvidenceTemplateMatch(scanContext, imagePath);
                PortRecordGoblinEvidenceTiming($"TemplateMatch:{PortNormalizeGoblinObservationSource(template.Source)}", templateStopwatch.Elapsed);
                rankedMatches.Add((template, imagePath, match));
                if (bestTemplate == null || match.Confidence > bestMatch.Confidence)
                {
                    bestTemplate = template;
                    bestImagePath = imagePath;
                    bestMatch = match;
                }
            }

            IReadOnlyList<ImageRecognitionSampleCandidate> rankedSamples = PortBuildGoblinEvidenceBestSampleCandidates(scanContext, rankedMatches);

            if (bestTemplate == null)
            {
                return new GoblinEvidenceDetectionResult(null, null, bestImagePath, bestMatch, rankedSamples);
            }

            if (bestMatch.Confidence < bestTemplate.Threshold)
            {
                PortLogGoblinEvidenceDetectorDiagnostic(
                    bestTemplate,
                    "NotFound",
                    "BelowThreshold",
                    bestImagePath,
                    referenceRegion,
                    bestMatch,
                    force: false);
                return new GoblinEvidenceDetectionResult(null, bestTemplate, bestImagePath, bestMatch, rankedSamples);
            }

            string journalNameValidationNotes = "";
            if (!PortTryValidateGoblinJournalNameMatch(
                bestTemplate,
                bestImagePath,
                scanContext,
                bestMatch,
                out double journalNameConfidence,
                out Point journalNameMatchPoint,
                out string journalNameValidationReason))
            {
                AppLogger.Info(
                    "GoblinEvidenceJournalNameValidationFailed: " +
                    $"source={PortNormalizeGoblinObservationSource(bestTemplate.Source)}; " +
                    $"goblinType={PortLogField(bestTemplate.GoblinType)}; " +
                    $"evidenceKind={bestTemplate.Kind}; " +
                    $"templateName={PortLogField(bestTemplate.FileName)}; " +
                    $"lineConfidence={bestMatch.Confidence:0.000}; " +
                    $"nameConfidence={journalNameConfidence:0.000}; " +
                    $"nameThreshold={GoblinJournalNameValidationThreshold:0.000}; " +
                    $"lineMatchPoint={FormatPoint(bestMatch.MatchPoint)}; " +
                    $"nameMatchPoint={FormatPoint(journalNameMatchPoint)}; " +
                    $"reason={PortLogField(journalNameValidationReason)}");
                PortLogGoblinEvidenceDetectorDiagnostic(
                    bestTemplate,
                    "NotFound",
                    journalNameValidationReason,
                    bestImagePath,
                    referenceRegion,
                    bestMatch,
                    force: true);
                return new GoblinEvidenceDetectionResult(null, bestTemplate, bestImagePath, bestMatch, rankedSamples);
            }

            if (journalNameConfidence > 0)
            {
                journalNameValidationNotes = $"; JournalNameConfidence={journalNameConfidence:0.000}; JournalNameMatchPoint={FormatPoint(journalNameMatchPoint)}";
            }

            PortLogGoblinEvidenceDetectorDiagnostic(
                bestTemplate,
                "Found",
                "ConfidenceMet",
                bestImagePath,
                referenceRegion,
                bestMatch,
                force: true);
            if (PortShouldSuppressMinimapColorDisagreement(bestTemplate, bestMatch, out string colorGoblinType))
            {
                AppLogger.Info(
                    "GoblinEvidenceMinimapColorDisagreement: " +
                    $"templateGoblinType={PortLogField(bestTemplate.GoblinType)}; " +
                    $"colorGoblinType={PortLogField(colorGoblinType)}; " +
                    $"templateName={PortLogField(bestTemplate.FileName)}; " +
                    $"yellowPixels={bestMatch.MinimapColor.YellowPixels}; " +
                    $"orangePixels={bestMatch.MinimapColor.OrangePixels}; " +
                    $"greenPixels={bestMatch.MinimapColor.GreenPixels}; " +
                    $"purplePixels={bestMatch.MinimapColor.PurplePixels}; " +
                    $"coloredPixels={bestMatch.MinimapColor.ColoredPixels}; " +
                    $"matchPoint={FormatPoint(bestMatch.MatchPoint)}; " +
                    $"screenMatchPoint={FormatPoint(bestMatch.ScreenMatchPoint)}; " +
                    "action=SuppressPendingJournal");
                PortLogGoblinEvidenceDetectorDiagnostic(
                    bestTemplate,
                    "NotFound",
                    "MinimapColorDisagreement",
                    bestImagePath,
                    referenceRegion,
                    bestMatch,
                    force: true);
                return new GoblinEvidenceDetectionResult(null, bestTemplate, bestImagePath, bestMatch, rankedSamples);
            }

            string goblinType = PortApplyMinimapColorDisambiguation(bestTemplate, bestMatch);
            GoblinEvidenceCandidate candidate = new(
                bestTemplate.Type,
                bestMatch.Confidence,
                bestTemplate.Source,
                $"Template={bestTemplate.FileName}; Kind={bestTemplate.Kind}; Threshold={bestTemplate.Threshold:0.000}; MatchPoint={FormatPoint(bestMatch.MatchPoint)}; ScreenMatchPoint={FormatPoint(bestMatch.ScreenMatchPoint)}{journalNameValidationNotes}{PortMinimapColorNotes(bestTemplate, bestMatch)}",
                goblinType,
                rankedSamples);
            PortRecordGoblinEvidenceTiming($"DetectBest:{PortNormalizeGoblinObservationSource(bestTemplate.Source)}", sourceStopwatch.Elapsed);
            return new GoblinEvidenceDetectionResult(candidate, bestTemplate, bestImagePath, bestMatch, rankedSamples);
        }

        private static IReadOnlyList<ImageRecognitionSampleCandidate> PortBuildGoblinEvidenceBestSampleCandidates(
            GoblinEvidenceScanContext scanContext,
            IReadOnlyList<(GoblinEvidenceTemplateRequirement Template, string ImagePath, GoblinEvidenceTemplateMatch Match)> matches)
        {
            return matches
                .OrderByDescending(match => match.Match.Confidence)
                .ThenBy(match => match.Template.Source, StringComparer.OrdinalIgnoreCase)
                .ThenBy(match => match.Template.FileName, StringComparer.OrdinalIgnoreCase)
                .Select((match, index) =>
                {
                    Rectangle cropRegion = new(match.Match.MatchPoint, match.Match.TemplateSize);
                    Rectangle screenRegion = new(match.Match.ScreenMatchPoint, match.Match.TemplateSize);
                    byte[] cropPng = ImageRecognitionBestSamplePromoter.EncodePng(scanContext.Screenshot, cropRegion);
                    bool thresholdMet = match.Match.Confidence >= match.Template.Threshold;
                    string source = PortNormalizeGoblinObservationSource(match.Template.Source);
                    return new ImageRecognitionSampleCandidate(
                        index + 1,
                        match.Template.GoblinType,
                        source,
                        match.Match.Confidence,
                        thresholdMet ? "ConfidenceMet" : "BelowThreshold",
                        match.Template.FileName,
                        match.ImagePath,
                        cropRegion,
                        screenRegion,
                        cropPng,
                        thresholdMet,
                        thresholdMet ? "" : "BelowThreshold",
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["EvidenceKind"] = match.Template.Kind.ToString(),
                            ["EvidenceType"] = match.Template.Type.ToString(),
                            ["Threshold"] = match.Template.Threshold.ToString("0.000", CultureInfo.InvariantCulture),
                            ["MatchPoint"] = $"{match.Match.MatchPoint.X},{match.Match.MatchPoint.Y}",
                            ["ScreenMatchPoint"] = $"{match.Match.ScreenMatchPoint.X},{match.Match.ScreenMatchPoint.Y}",
                            ["TemplateSize"] = $"{match.Match.TemplateSize.Width}x{match.Match.TemplateSize.Height}",
                        });
                })
                .ToArray();
        }

        private sealed record GoblinEvidenceDetectionResult(
            GoblinEvidenceCandidate? Candidate,
            GoblinEvidenceTemplateRequirement? BestTemplate,
            string BestImagePath,
            GoblinEvidenceTemplateMatch BestMatch,
            IReadOnlyList<ImageRecognitionSampleCandidate> CandidateRanking);

        private sealed record GoblinJournalEvidenceSeenState(
            string GoblinType,
            string AreaKey,
            DateTime FirstSeenUtc,
            DateTime LastSeenUtc);

        private void PortTryRefreshGoblinObservationForManualHotkey()
        {
            try
            {
                GoblinEvidenceTemplateCatalog templateCatalog = PortGoblinEvidenceTemplateCatalog();
                if (!templateCatalog.HasUsableTemplates)
                {
                    PortLogGoblinEvidenceTemplateSetupMissing("ManualHotkeyRefresh", templateCatalog, notifyIfMissing: false);
                    PortLogGoblinEvidenceMissingTemplateScanSummary(templateCatalog);
                    PortMarkGoblinObservationNoCurrent("MissingTemplate");
                    return;
                }

                PortLogGoblinEvidenceTemplateSetupWarning("ManualHotkeyRefresh", templateCatalog);
                GoblinEvidenceCandidate? candidate = PortDetectManualHotkeyRefreshGoblinEvidenceCandidate(templateCatalog);
                if (candidate == null)
                {
                    AppLogger.Info($"GoblinEvidenceManualRefreshResult: candidateFound=False; reason=NoCandidate; source=ManualHotkey; refreshOrder=MinimapThenJournal; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}");
                    PortMarkGoblinObservationNoCurrent("No current observation");
                    return;
                }

                PortRecordGoblinEvidence(candidate, forceObservation: true);
                AppLogger.Info($"GoblinEvidenceManualRefreshResult: candidateFound=True; candidateCount=1; source=ManualHotkey; refreshOrder=MinimapThenJournal; candidateSource={PortLogField(candidate.Source)}; goblinType={PortLogField(candidate.GoblinType)}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Goblin evidence manual refresh failed.", ex);
            }
        }

        private GoblinEvidenceCandidate? PortDetectManualHotkeyRefreshGoblinEvidenceCandidate(GoblinEvidenceTemplateCatalog templateCatalog)
        {
            foreach (string source in new[] { "MinimapCandidate", "JournalCandidate" })
            {
                IReadOnlyList<GoblinEvidenceTemplateRequirement> templates = templateCatalog.Templates
                    .Where(template => template.Source.Equals(source, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (templates.Count == 0)
                {
                    continue;
                }

                Rectangle scanRegion = PortGoblinEvidenceRegionForSource(source);
                GoblinEvidenceDetectionResult detection = PortDetectBestGoblinEvidenceTemplate(templates, scanRegion);
                PortLogGoblinEvidenceSourceScanResult(source, scanRegion, detection, templates.Count);
                if (detection.Candidate != null &&
                    PortTryAcceptGoblinEvidenceCandidate(source, detection, freshKilledWithoutEngagedReason: "Manual", out GoblinEvidenceCandidate? acceptedCandidate))
                {
                    return acceptedCandidate;
                }
            }

            return null;
        }

        private bool PortTryAcceptGoblinEvidenceCandidate(
            string source,
            GoblinEvidenceDetectionResult detection,
            string freshKilledWithoutEngagedReason,
            out GoblinEvidenceCandidate? acceptedCandidate)
        {
            acceptedCandidate = detection.Candidate;
            if (acceptedCandidate == null)
            {
                return false;
            }

            GoblinEvidenceTemplateRequirement? template = detection.BestTemplate;
            if (template == null ||
                !string.Equals(source, "JournalCandidate", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return PortTryAcceptJournalEvidenceCandidate(template, acceptedCandidate, detection.BestMatch, freshKilledWithoutEngagedReason, out acceptedCandidate);
        }

        private bool PortTryAcceptJournalEvidenceCandidate(
            GoblinEvidenceTemplateRequirement template,
            GoblinEvidenceCandidate candidate,
            GoblinEvidenceTemplateMatch match,
            string freshKilledWithoutEngagedReason,
            out GoblinEvidenceCandidate? acceptedCandidate)
        {
            acceptedCandidate = null;
            DateTime nowUtc = DateTime.UtcNow;
            string goblinType = GoblinTypeNormalizer.Normalize(template.GoblinType);
            string journalLineSignature = PortJournalEvidenceLineSignature(template, goblinType, match);
            DateTime firstSeenUtc;
            string firstSeenAreaKey = "";
            GoblinJournalEngagedState? recentEngaged = null;
            bool isEngagedSignal = template.Kind == GoblinEvidenceTemplateKind.JournalEngaged ||
                template.Kind == GoblinEvidenceTemplateKind.JournalEngagedAndKilled;

            if (PortJournalHistoryInputSuppressionActive(nowUtc, out double historySuppressRemainingSeconds))
            {
                PortLogJournalEvidenceFreshnessDiagnostic(
                    "JournalCandidateIgnoredHistoryInput",
                    template,
                    match,
                    portLastConfirmedLocation,
                    $"remainingSeconds={historySuppressRemainingSeconds:0.0}; historyInputWindowSeconds={GoblinJournalHistoryInputSuppressWindow.TotalSeconds:0}");
                return false;
            }

            if (!PortJournalEvidenceAppearsInActiveFeed(match))
            {
                PortLogJournalEvidenceFreshnessDiagnostic(
                    "JournalCandidateIgnoredHistoryRow",
                    template,
                    match,
                    portLastConfirmedLocation,
                    $"matchY={match.MatchPoint.Y}; minimumY={GoblinJournalActiveFeedMinimumY}; lineBucket={PortJournalEvidenceLineBucket(match.MatchPoint)}");
                return false;
            }

            if (PortTrySuppressJournalEvidenceFromResetCarryover(journalLineSignature, nowUtc, out string resetCarryoverDetails))
            {
                PortLogJournalEvidenceFreshnessDiagnostic(
                    "JournalCandidateIgnoredResetCarryover",
                    template,
                    match,
                    portLastConfirmedLocation,
                    resetCarryoverDetails);
                return false;
            }

            if (PortNewGameJournalCarryoverSuppressionActive(nowUtc, out double newGameCarryoverRemainingSeconds))
            {
                PortRememberStaleSuppressedJournalEvidence(journalLineSignature, nowUtc, PortResolvedAreaKey(portLastConfirmedLocation), nowUtc);
                PortLogJournalEvidenceFreshnessDiagnostic(
                    "JournalCandidateIgnoredNewGameCarryoverWindow",
                    template,
                    match,
                    portLastConfirmedLocation,
                    $"remainingSeconds={newGameCarryoverRemainingSeconds:0.0}; suppressWindowSeconds={GoblinJournalNewGameCarryoverSuppressWindow.TotalSeconds:0}");
                return false;
            }

            if (PortTryTouchStaleSuppressedJournalEvidenceByVisibleGoblinLine(journalLineSignature, nowUtc, out string staleVisibleLineDetails))
            {
                if (PortTryAllowFreshAreaStaleVisibleJournalEvidence(
                    template,
                    match,
                    goblinType,
                    journalLineSignature,
                    nowUtc,
                    staleVisibleLineDetails,
                    out string freshAreaBypassDetails))
                {
                    PortLogJournalEvidenceFreshnessDiagnostic(
                        "JournalCandidateFreshAreaStaleVisibleLineBypass",
                        template,
                        match,
                        PortGoblinEvidenceNoteValue(freshAreaBypassDetails, "currentArea"),
                        freshAreaBypassDetails);
                }
                else
                {
                    PortLogJournalEvidenceFreshnessDiagnostic(
                        "JournalCandidateIgnoredStaleVisibleLine",
                        template,
                        match,
                        portLastConfirmedLocation,
                        $"{staleVisibleLineDetails}; areaResolutionSkippedReason=StaleVisibleLinePreAreaResolution");
                    return false;
                }
            }

            lock (portGoblinEvidenceLock)
            {
                if (!portJournalEvidenceSeenByKey.TryGetValue(journalLineSignature, out GoblinJournalEvidenceSeenState? seenState))
                {
                    firstSeenUtc = nowUtc;
                    portJournalEvidenceSeenByKey[journalLineSignature] = new GoblinJournalEvidenceSeenState(goblinType, "", firstSeenUtc, nowUtc);
                }
                else
                {
                    firstSeenUtc = seenState.FirstSeenUtc;
                    firstSeenAreaKey = seenState.AreaKey;
                    portJournalEvidenceSeenByKey[journalLineSignature] = seenState with { LastSeenUtc = nowUtc };
                }

                if (portRecentJournalEngagedByGoblinType.TryGetValue(goblinType, out GoblinJournalEngagedState? state))
                {
                    recentEngaged = state;
                }
            }

            TimeSpan firstSeenAge = nowUtc - firstSeenUtc;
            if (isEngagedSignal)
            {
                bool staleSuppressionActive = !GoblinJournalFreshnessPolicy.IsFresh(firstSeenUtc, nowUtc, GoblinJournalEvidenceFreshWindow) &&
                    PortTryTouchStaleSuppressedJournalEvidence(journalLineSignature, nowUtc);
                if (staleSuppressionActive)
                {
                    PortLogJournalEvidenceFreshnessDiagnostic(
                        "JournalEngagedIgnoredStale",
                        template,
                        match,
                        portLastConfirmedLocation,
                        $"firstSeenAgeSeconds={firstSeenAge.TotalSeconds:0.0}; firstSeenArea={PortLogField(firstSeenAreaKey)}; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}; staleSuppressed=True");
                    return false;
                }
            }

            PortGoblinTrackerAreaResolution areaResult = PortResolveCurrentGoblinArea("Journal");
            areaResult = PortApplyJournalMinimapAreaOverride(goblinType, areaResult, nowUtc, "JournalFreshness");
            areaResult = PortApplyJournalSuppressedMinimapAreaAnchor(goblinType, areaResult, nowUtc, "JournalFreshness");
            string areaKey = areaResult.Area.Resolved ? areaResult.Area.AreaKey : "";
            string displayArea = areaResult.Area.Resolved ? areaResult.Area.DisplayLocation : "Unknown";
            bool journalAreaAnchoredToSuppressedMinimap = areaResult.DisambiguationReason.Equals("RecentSuppressedMinimapAreaAnchor", StringComparison.OrdinalIgnoreCase);
            string titleResolverOverrideNote = journalAreaAnchoredToSuppressedMinimap
                ? "; TitleResolverOverride=BlockedByFreshMinimapAnchor"
                : "";

            if (isEngagedSignal)
            {
                lock (portGoblinEvidenceLock)
                {
                    if (portJournalEvidenceSeenByKey.TryGetValue(journalLineSignature, out GoblinJournalEvidenceSeenState? seenState) &&
                        string.IsNullOrWhiteSpace(seenState.AreaKey) &&
                        !string.IsNullOrWhiteSpace(areaKey))
                    {
                        firstSeenAreaKey = areaKey;
                        portJournalEvidenceSeenByKey[journalLineSignature] = seenState with { AreaKey = areaKey };
                    }
                }

                bool firstSeenAreaChanged = !string.IsNullOrWhiteSpace(firstSeenAreaKey) &&
                    !string.IsNullOrWhiteSpace(areaKey) &&
                    !string.Equals(firstSeenAreaKey, areaKey, StringComparison.OrdinalIgnoreCase);
                if (firstSeenAreaChanged)
                {
                    bool firstSeenAreaBlocked = GoblinManualCountBlockList.IsBlocked(firstSeenAreaKey);
                    if (!firstSeenAreaBlocked &&
                        GoblinJournalFreshnessPolicy.IsFresh(firstSeenUtc, nowUtc, GoblinJournalEvidenceFreshWindow))
                    {
                        string firstSeenDisplayArea = PortDisplayLocation(firstSeenAreaKey);
                        lock (portGoblinEvidenceLock)
                        {
                            portRecentJournalEngagedByGoblinType[goblinType] = new GoblinJournalEngagedState(goblinType, firstSeenAreaKey, nowUtc);
                        }

                        PortLogJournalEvidenceFreshnessDiagnostic(
                            "JournalEngagedAcceptedFirstSeenAreaLock",
                            template,
                            match,
                            firstSeenDisplayArea,
                            $"firstSeenAgeSeconds={firstSeenAge.TotalSeconds:0.0}; firstSeenArea={PortLogField(firstSeenAreaKey)}; currentArea={PortLogField(areaKey)}; acceptedArea={PortLogField(firstSeenAreaKey)}; areaChangedDuringPendingEvidence=True; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}");
                        acceptedCandidate = candidate with
                        {
                            Notes = $"{candidate.Notes}; JournalFreshness=EngagedAcceptedFirstSeenAreaLock; JournalArea={firstSeenDisplayArea}"
                        };
                        return true;
                    }

                    if (PortTryGetRecentMinimapJournalConfirmation(goblinType, areaKey, nowUtc, out GoblinObservationRecord? recentMinimap, out double recentMinimapAgeSeconds))
                    {
                        PortLogJournalEvidenceFreshnessDiagnostic(
                            "JournalEngagedAcceptedRecentMinimapConfirmation",
                            template,
                            match,
                            displayArea,
                            $"firstSeenAgeSeconds={firstSeenAge.TotalSeconds:0.0}; firstSeenArea={PortLogField(firstSeenAreaKey)}; currentArea={PortLogField(areaKey)}; recentMinimapArea={PortLogField(recentMinimap.AreaKey)}; recentMinimapAgeSeconds={recentMinimapAgeSeconds:0.0}; recentMinimapConfidence={recentMinimap.EvidenceConfidence:0.000}; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}");
                        acceptedCandidate = candidate with
                        {
                            Notes = $"{candidate.Notes}; JournalFreshness=EngagedAcceptedRecentMinimapConfirmation; JournalArea={displayArea}{titleResolverOverrideNote}"
                        };
                        return true;
                    }

                    PortRememberStaleSuppressedJournalEvidence(journalLineSignature, nowUtc, firstSeenAreaKey, firstSeenUtc);
                    PortLogJournalEvidenceFreshnessDiagnostic(
                        "JournalEngagedIgnoredAreaChanged",
                        template,
                        match,
                        displayArea,
                        $"firstSeenAgeSeconds={firstSeenAge.TotalSeconds:0.0}; firstSeenArea={PortLogField(firstSeenAreaKey)}; currentArea={PortLogField(areaKey)}; acceptedArea=None; areaChangedDuringPendingEvidence=True; discardReason={(firstSeenAreaBlocked ? "FirstSeenAreaBlocked" : "FirstSeenEvidenceStale")}; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}; staleSuppressed=True");
                    return false;
                }

                if (!GoblinJournalFreshnessPolicy.EngagedIsFresh(firstSeenUtc, firstSeenAreaKey, areaKey, nowUtc, GoblinJournalEvidenceFreshWindow))
                {
                    if (PortTryGetRecentMinimapJournalConfirmation(goblinType, areaKey, nowUtc, out GoblinObservationRecord? recentMinimap, out double recentMinimapAgeSeconds))
                    {
                        PortLogJournalEvidenceFreshnessDiagnostic(
                            "JournalEngagedAcceptedRecentMinimapConfirmation",
                            template,
                            match,
                            displayArea,
                            $"firstSeenAgeSeconds={firstSeenAge.TotalSeconds:0.0}; firstSeenArea={PortLogField(firstSeenAreaKey)}; currentArea={PortLogField(areaKey)}; recentMinimapArea={PortLogField(recentMinimap.AreaKey)}; recentMinimapAgeSeconds={recentMinimapAgeSeconds:0.0}; recentMinimapConfidence={recentMinimap.EvidenceConfidence:0.000}; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}");
                        acceptedCandidate = candidate with
                        {
                            Notes = $"{candidate.Notes}; JournalFreshness=EngagedAcceptedRecentMinimapConfirmation; JournalArea={displayArea}{titleResolverOverrideNote}"
                        };
                        return true;
                    }

                    PortRememberStaleSuppressedJournalEvidence(journalLineSignature, nowUtc, firstSeenAreaKey, firstSeenUtc);
                    PortLogJournalEvidenceFreshnessDiagnostic(
                        "JournalEngagedIgnoredStale",
                        template,
                        match,
                        displayArea,
                        $"firstSeenAgeSeconds={firstSeenAge.TotalSeconds:0.0}; firstSeenArea={PortLogField(firstSeenAreaKey)}; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}; staleSuppressed=True");
                    return false;
                }

                lock (portGoblinEvidenceLock)
                {
                    portRecentJournalEngagedByGoblinType[goblinType] = new GoblinJournalEngagedState(goblinType, areaKey, nowUtc);
                }

                PortLogJournalEvidenceFreshnessDiagnostic(
                    "JournalEngagedAccepted",
                    template,
                    match,
                    displayArea,
                    $"firstSeenAgeSeconds={firstSeenAge.TotalSeconds:0.0}; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}");
                acceptedCandidate = candidate with
                {
                    Notes = $"{candidate.Notes}; JournalFreshness=EngagedAccepted; JournalArea={displayArea}{titleResolverOverrideNote}"
                };
                return true;
            }

            if (template.Kind == GoblinEvidenceTemplateKind.JournalKilled)
            {
                string killedSignature = PortJournalEvidenceLineSignature(template, goblinType, match);
                GoblinJournalKilledState killedState = PortRememberJournalKilledEvidence(killedSignature, goblinType, areaKey, nowUtc);
                TimeSpan killedFirstSeenAge = nowUtc - killedState.FirstSeenUtc;
                bool killedFreshInCurrentArea = GoblinJournalFreshnessPolicy.KilledIsFresh(
                    killedState,
                    areaKey,
                    nowUtc,
                    GoblinJournalEvidenceFreshWindow);
                bool recentEngagedMatches = GoblinJournalFreshnessPolicy.KilledHasRecentEngaged(
                    recentEngaged,
                    areaKey,
                    nowUtc,
                    GoblinJournalEvidenceFreshWindow);
                bool recentEngagedFreshAnyArea = GoblinJournalFreshnessPolicy.KilledHasRecentEngagedForAreaChangedLock(
                    recentEngaged,
                    goblinType,
                    nowUtc);
                string acceptedAreaKey = recentEngagedFreshAnyArea ? recentEngaged!.AreaKey : areaKey;
                string acceptedDisplayArea = !string.IsNullOrWhiteSpace(acceptedAreaKey)
                    ? PortDisplayLocation(acceptedAreaKey)
                    : displayArea;
                bool killedAreaChangedDuringPendingEvidence = !string.IsNullOrWhiteSpace(acceptedAreaKey) &&
                    !string.IsNullOrWhiteSpace(areaKey) &&
                    !string.Equals(acceptedAreaKey, areaKey, StringComparison.OrdinalIgnoreCase);
                bool recentEngagedAreaDiffers = recentEngaged != null &&
                    !string.IsNullOrWhiteSpace(recentEngaged.AreaKey) &&
                    !string.IsNullOrWhiteSpace(areaKey) &&
                    !string.Equals(recentEngaged.AreaKey, areaKey, StringComparison.OrdinalIgnoreCase);
                bool recentMinimapKilledConfirmation = PortTryGetRecentMinimapJournalConfirmation(
                    goblinType,
                    areaKey,
                    nowUtc,
                    out GoblinObservationRecord recentMinimap,
                    out double recentMinimapAgeSeconds);
                if (!killedFreshInCurrentArea && !recentEngagedMatches && !recentEngagedFreshAnyArea)
                {
                    if (recentMinimapKilledConfirmation)
                    {
                        PortLogJournalEvidenceFreshnessDiagnostic(
                            "JournalKilledAcceptedRecentMinimapConfirmation",
                            template,
                            match,
                            displayArea,
                            $"firstSeenAgeSeconds={killedFirstSeenAge.TotalSeconds:0.0}; firstSeenArea={PortLogField(killedState.AreaKey)}; currentArea={PortLogField(areaKey)}; acceptedArea={PortLogField(areaKey)}; areaChangedDuringPendingEvidence=False; recentMinimapArea={PortLogField(recentMinimap.AreaKey)}; recentMinimapAgeSeconds={recentMinimapAgeSeconds:0.0}; recentMinimapConfidence={recentMinimap.EvidenceConfidence:0.000}; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}; recentMinimapWindowSeconds={PortAutomaticGoblinRecentMinimapJournalConfirmationWindow.TotalSeconds:0}");
                        acceptedCandidate = candidate with
                        {
                            Notes = $"{candidate.Notes}; JournalFreshness=KilledAcceptedRecentMinimapConfirmation; JournalArea={displayArea}{titleResolverOverrideNote}"
                        };
                        return true;
                    }

                    string staleKilledPreviousAreaKey = killedState.AreaKey;
                    TimeSpan staleKilledPreviousFirstSeenAge = killedFirstSeenAge;
                    bool staleKilledDifferentArea = !string.IsNullOrWhiteSpace(staleKilledPreviousAreaKey) &&
                        !string.IsNullOrWhiteSpace(areaKey) &&
                        !GoblinAreaResolver.NormalizedKey(staleKilledPreviousAreaKey).Equals(
                            GoblinAreaResolver.NormalizedKey(areaKey),
                            StringComparison.OrdinalIgnoreCase);
                    bool staleKilledOldEnoughForTitleReset = killedFirstSeenAge >= TimeSpan.FromMinutes(2);
                    string staleResetConfirmedAreaKey = "";
                    string staleResetAreaAnchorReason = "";
                    bool staleKilledHasTrustedCombatTitle = staleKilledDifferentArea &&
                        staleKilledOldEnoughForTitleReset &&
                        portCombatRunning &&
                        string.Equals(freshKilledWithoutEngagedReason, "Observation", StringComparison.OrdinalIgnoreCase) &&
                        PortFreshKilledObservationHasTrustedAreaAnchor(areaResult, areaKey, out staleResetConfirmedAreaKey, out staleResetAreaAnchorReason);
                    if (staleKilledHasTrustedCombatTitle)
                    {
                        PortForgetJournalFreshnessStateForVisibleLine(killedSignature);
                        killedState = PortRememberJournalKilledEvidence(killedSignature, goblinType, areaKey, nowUtc);
                        killedFirstSeenAge = nowUtc - killedState.FirstSeenUtc;
                        string staleResetDiagnosticEvent = string.Equals(staleResetAreaAnchorReason, "CurrentTitleArea", StringComparison.OrdinalIgnoreCase)
                            ? "JournalKilledStaleStateResetByCurrentTitleCombat"
                            : "JournalKilledStaleStateResetByTrustedCombatArea";
                        PortLogJournalEvidenceFreshnessDiagnostic(
                            staleResetDiagnosticEvent,
                            template,
                            match,
                            displayArea,
                            $"previousFirstSeenAgeSeconds={Math.Max(0, staleKilledPreviousFirstSeenAge.TotalSeconds):0.0}; previousArea={PortLogField(staleKilledPreviousAreaKey)}; currentArea={PortLogField(areaKey)}; confirmedArea={PortLogField(staleResetConfirmedAreaKey)}; acceptedArea={PortLogField(areaKey)}; areaAnchorReason={PortLogField(staleResetAreaAnchorReason)}; combatActive=True; staleKilledStateReset=True; minimumResetAgeSeconds=120; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}");
                        acceptedCandidate = candidate with
                        {
                            Notes = $"{candidate.Notes}; JournalFreshness=KilledAcceptedStaleStateReset; JournalArea={displayArea}; StaleKilledStateReset={staleResetAreaAnchorReason}"
                        };
                        return true;
                    }

                    PortRememberStaleSuppressedJournalEvidence(killedSignature, nowUtc, killedState.AreaKey, killedState.FirstSeenUtc);
                    PortLogJournalEvidenceFreshnessDiagnostic(
                        "JournalKilledIgnoredStale",
                        template,
                        match,
                        displayArea,
                        $"firstSeenAgeSeconds={killedFirstSeenAge.TotalSeconds:0.0}; firstSeenArea={PortLogField(killedState.AreaKey)}; currentArea={PortLogField(areaKey)}; acceptedArea=None; areaChangedDuringPendingEvidence={recentEngagedAreaDiffers}; recentEngagedArea={PortLogField(recentEngaged?.AreaKey ?? "")}; recentEngagedAgeSeconds={(recentEngaged == null ? -1 : Math.Max(0, (nowUtc - recentEngaged.SeenUtc).TotalSeconds)):0.0}; discardReason=KilledEvidenceStaleWithoutFreshEngagedAnchor; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}; areaLockWindowSeconds={GoblinJournalFreshnessPolicy.AreaChangedKilledRecentEngagedWindow.TotalSeconds:0}");
                    return false;
                }

                if (!recentEngagedMatches && !recentEngagedFreshAnyArea && !string.IsNullOrWhiteSpace(freshKilledWithoutEngagedReason))
                {
                    bool acceptedForManualRefresh = string.Equals(freshKilledWithoutEngagedReason, "Manual", StringComparison.OrdinalIgnoreCase);
                    string confirmedAreaKey = "";
                    string areaAnchorReason = acceptedForManualRefresh
                        ? "ManualRefresh"
                        : journalAreaAnchoredToSuppressedMinimap
                            ? "FreshMinimapAreaAnchor"
                            : "";
                    if (!acceptedForManualRefresh &&
                        !journalAreaAnchoredToSuppressedMinimap &&
                        !PortFreshKilledObservationHasTrustedAreaAnchor(areaResult, areaKey, out confirmedAreaKey, out areaAnchorReason))
                    {
                        PortRememberStaleSuppressedJournalEvidence(killedSignature, nowUtc, killedState.AreaKey, killedState.FirstSeenUtc);
                        PortLogJournalEvidenceFreshnessDiagnostic(
                            "JournalKilledIgnoredFreshObservationAreaUnconfirmed",
                            template,
                            match,
                            displayArea,
                            $"firstSeenAgeSeconds={killedFirstSeenAge.TotalSeconds:0.0}; firstSeenArea={PortLogField(killedState.AreaKey)}; currentArea={PortLogField(areaKey)}; confirmedArea={PortLogField(confirmedAreaKey)}; acceptedArea=None; areaChangedDuringPendingEvidence=True; discardReason=KilledFreshObservationAreaUnconfirmed; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}; staleSuppressed=True");
                        return false;
                    }

                    string freshnessReason = acceptedForManualRefresh ? "Manual" : "Observation";
                    string diagnosticEventName = acceptedForManualRefresh
                        ? "JournalKilledAcceptedFreshManual"
                        : journalAreaAnchoredToSuppressedMinimap
                            ? "JournalKilledAcceptedFreshMinimapAreaAnchor"
                            : string.Equals(areaAnchorReason, "CurrentTitleArea", StringComparison.OrdinalIgnoreCase)
                                ? "JournalKilledAcceptedFreshTitleArea"
                                : "JournalKilledAcceptedFreshObservation";
                    PortLogJournalEvidenceFreshnessDiagnostic(
                        diagnosticEventName,
                        template,
                        match,
                        displayArea,
                        $"firstSeenAgeSeconds={killedFirstSeenAge.TotalSeconds:0.0}; firstSeenArea={PortLogField(killedState.AreaKey)}; currentArea={PortLogField(areaKey)}; confirmedArea={PortLogField(confirmedAreaKey)}; acceptedArea={PortLogField(areaKey)}; areaChangedDuringPendingEvidence=False; titleResolverOverride={(journalAreaAnchoredToSuppressedMinimap ? "BlockedByFreshMinimapAnchor" : "None")}; areaAnchorReason={PortLogField(areaAnchorReason)}; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}");
                    acceptedCandidate = candidate with
                    {
                        Notes = $"{candidate.Notes}; JournalFreshness=KilledAcceptedFresh{freshnessReason}; JournalArea={displayArea}{titleResolverOverrideNote}"
                    };
                    return true;
                }

                if (!recentEngagedMatches && !recentEngagedFreshAnyArea)
                {
                    double recentAgeSeconds = recentEngaged == null ? -1 : Math.Max(0, (nowUtc - recentEngaged.SeenUtc).TotalSeconds);
                    PortLogJournalEvidenceFreshnessDiagnostic(
                        "JournalKilledIgnoredNoRecentEngaged",
                        template,
                        match,
                        displayArea,
                        $"recentEngagedArea={PortLogField(recentEngaged?.AreaKey ?? "")}; recentEngagedAgeSeconds={recentAgeSeconds:0.0}; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}");
                    return false;
                }

                if (recentEngagedFreshAnyArea && killedAreaChangedDuringPendingEvidence)
                {
                    PortLogJournalEvidenceFreshnessDiagnostic(
                        "JournalKilledAcceptedAfterEngagedFirstSeenAreaLock",
                        template,
                        match,
                        acceptedDisplayArea,
                        $"recentEngagedArea={PortLogField(recentEngaged!.AreaKey)}; recentEngagedAgeSeconds={Math.Max(0, (nowUtc - recentEngaged.SeenUtc).TotalSeconds):0.0}; currentArea={PortLogField(areaKey)}; acceptedArea={PortLogField(acceptedAreaKey)}; areaChangedDuringPendingEvidence=True; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}; areaLockWindowSeconds={GoblinJournalFreshnessPolicy.AreaChangedKilledRecentEngagedWindow.TotalSeconds:0}");
                    acceptedCandidate = candidate with
                    {
                        Notes = $"{candidate.Notes}; JournalFreshness=KilledAcceptedAfterEngagedFirstSeenAreaLock; JournalArea={acceptedDisplayArea}"
                    };
                    return true;
                }

                PortLogJournalEvidenceFreshnessDiagnostic(
                    "JournalKilledAcceptedAfterEngaged",
                    template,
                    match,
                    displayArea,
                    $"recentEngagedArea={PortLogField(recentEngaged!.AreaKey)}; recentEngagedAgeSeconds={Math.Max(0, (nowUtc - recentEngaged.SeenUtc).TotalSeconds):0.0}; currentArea={PortLogField(areaKey)}; acceptedArea={PortLogField(areaKey)}; areaChangedDuringPendingEvidence=False; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}");
                acceptedCandidate = candidate with
                {
                    Notes = $"{candidate.Notes}; JournalFreshness=KilledAcceptedAfterEngaged; JournalArea={displayArea}{titleResolverOverrideNote}"
                };
                return true;
            }

            acceptedCandidate = candidate;
            return true;
        }

        private bool PortFreshKilledObservationAreaMatchesConfirmedArea(string areaKey, out string confirmedAreaKey)
        {
            confirmedAreaKey = PortResolvedAreaKey(portLastConfirmedLocation);
            return !string.IsNullOrWhiteSpace(areaKey) &&
                !string.IsNullOrWhiteSpace(confirmedAreaKey) &&
                GoblinAreaResolver.NormalizedKey(areaKey).Equals(
                    GoblinAreaResolver.NormalizedKey(confirmedAreaKey),
                    StringComparison.OrdinalIgnoreCase);
        }

        private bool PortFreshKilledObservationHasTrustedAreaAnchor(
            PortGoblinTrackerAreaResolution areaResult,
            string areaKey,
            out string confirmedAreaKey,
            out string areaAnchorReason)
        {
            areaAnchorReason = "";
            if (PortFreshKilledObservationAreaMatchesConfirmedArea(areaKey, out confirmedAreaKey))
            {
                areaAnchorReason = "ConfirmedRouteArea";
                return true;
            }

            if (areaResult.Area.Resolved &&
                !string.IsNullOrWhiteSpace(areaKey) &&
                !string.IsNullOrWhiteSpace(areaResult.BestName) &&
                areaResult.BestConfidence >= PortCurrentLocationConfidence &&
                GoblinAreaResolver.NormalizedKey(areaResult.Area.AreaKey).Equals(
                    GoblinAreaResolver.NormalizedKey(areaKey),
                    StringComparison.OrdinalIgnoreCase))
            {
                areaAnchorReason = "CurrentTitleArea";
                return true;
            }

            areaAnchorReason = "UntrustedArea";
            return false;
        }

        private GoblinJournalKilledState PortRememberJournalKilledEvidence(
            string signature,
            string goblinType,
            string areaKey,
            DateTime nowUtc)
        {
            lock (portGoblinEvidenceLock)
            {
                if (!portJournalKilledEvidenceSeenBySignature.TryGetValue(signature, out GoblinJournalKilledState? state))
                {
                    state = new GoblinJournalKilledState(goblinType, areaKey, nowUtc, nowUtc);
                }
                else
                {
                    state = state with { LastSeenUtc = nowUtc };
                }

                portJournalKilledEvidenceSeenBySignature[signature] = state;
                return state;
            }
        }

        private string PortJournalEvidenceLineSignature(
            GoblinEvidenceTemplateRequirement template,
            string goblinType,
            GoblinEvidenceTemplateMatch match)
        {
            return GoblinJournalEvidencePolicy.LineSignature(template, goblinType, match);
        }

        private static int PortJournalEvidenceLineBucket(Point matchPoint)
        {
            return GoblinJournalEvidencePolicy.LineBucket(matchPoint);
        }

        private static bool PortJournalEvidenceAppearsInActiveFeed(GoblinEvidenceTemplateMatch match)
        {
            return GoblinJournalEvidencePolicy.AppearsInActiveFeed(match);
        }

        private void PortSuppressJournalEvidenceAfterHistoryInput(string reason)
        {
            DateTime nowUtc = DateTime.UtcNow;
            DateTime suppressUntilUtc = nowUtc + GoblinJournalHistoryInputSuppressWindow;
            Interlocked.Exchange(ref portGoblinJournalHistorySuppressUntilTicks, suppressUntilUtc.Ticks);
            AppLogger.Info(
                "GoblinJournalHistorySuppressionArmed: " +
                $"reason={PortLogField(reason)}; " +
                $"suppressUntilUtc={suppressUntilUtc:O}; " +
                $"windowSeconds={GoblinJournalHistoryInputSuppressWindow.TotalSeconds:0}; " +
                $"combatActive={portCombatRunning}; " +
                $"combatStopping={portCombatStopping}; " +
                $"automationRunning={isAutomationRunning}; " +
                $"diabloRunning={IsDiabloRunning()}; " +
                $"diabloActive={PortDiabloIsActive()}; " +
                $"currentArea={PortLogField(PortDisplayLocation(portLastConfirmedLocation))}");
        }

        private bool PortJournalHistoryInputSuppressionActive(DateTime nowUtc, out double remainingSeconds)
        {
            long suppressUntilTicks = Interlocked.Read(ref portGoblinJournalHistorySuppressUntilTicks);
            remainingSeconds = 0;
            if (suppressUntilTicks <= nowUtc.Ticks)
            {
                return false;
            }

            remainingSeconds = Math.Max(0, (new DateTime(suppressUntilTicks, DateTimeKind.Utc) - nowUtc).TotalSeconds);
            return true;
        }

        private bool PortNewGameJournalCarryoverSuppressionActive(DateTime nowUtc, out double remainingSeconds)
        {
            long suppressUntilTicks = Interlocked.Read(ref portGoblinJournalNewGameCarryoverSuppressUntilTicks);
            remainingSeconds = 0;
            if (suppressUntilTicks <= nowUtc.Ticks)
            {
                return false;
            }

            remainingSeconds = Math.Max(0, (new DateTime(suppressUntilTicks, DateTimeKind.Utc) - nowUtc).TotalSeconds);
            return true;
        }

        private bool PortTryTouchStaleSuppressedJournalEvidence(string signature, DateTime nowUtc)
        {
            lock (portGoblinEvidenceLock)
            {
                if (!portStaleSuppressedJournalEvidenceByKey.TryGetValue(signature, out GoblinJournalStaleSuppressedState? state))
                {
                    return false;
                }

                if (!GoblinJournalFreshnessPolicy.StaleSuppressionActive(state.LastSeenUtc, nowUtc, GoblinJournalEvidenceFreshWindow))
                {
                    portStaleSuppressedJournalEvidenceByKey.Remove(signature);
                    return false;
                }

                portStaleSuppressedJournalEvidenceByKey[signature] = state with { LastSeenUtc = nowUtc };
                return true;
            }
        }

        private bool PortTryTouchStaleSuppressedJournalEvidenceByVisibleGoblinLine(string signature, DateTime nowUtc, out string details)
        {
            details = "";
            lock (portGoblinEvidenceLock)
            {
                List<string> expiredKeys = [];
                foreach (KeyValuePair<string, GoblinJournalStaleSuppressedState> pair in portStaleSuppressedJournalEvidenceByKey)
                {
                    if (!GoblinJournalFreshnessPolicy.StaleSuppressionActive(pair.Value.LastSeenUtc, nowUtc, GoblinJournalEvidenceFreshWindow))
                    {
                        expiredKeys.Add(pair.Key);
                        continue;
                    }

                    if (!GoblinJournalEvidencePolicy.SameVisibleGoblinLine(signature, pair.Key, out int currentBucket, out int previousBucket))
                    {
                        continue;
                    }

                    portStaleSuppressedJournalEvidenceByKey[pair.Key] = pair.Value with { LastSeenUtc = nowUtc };
                    details =
                        $"signature={PortLogField(signature)}; " +
                        $"staleSignature={PortLogField(pair.Key)}; " +
                        $"currentLineBucket={currentBucket}; " +
                        $"staleLineBucket={previousBucket}; " +
                        $"staleArea={PortLogField(pair.Value.AreaKey)}; " +
                        $"staleEvidenceFirstSeenAgeSeconds={Math.Max(0, (nowUtc - pair.Value.EvidenceFirstSeenUtc).TotalSeconds):0.0}; " +
                        $"staleFirstSuppressedAgeSeconds={Math.Max(0, (nowUtc - pair.Value.FirstSuppressedUtc).TotalSeconds):0.0}; " +
                        $"staleLastSeenAgeSeconds={Math.Max(0, (nowUtc - pair.Value.LastSeenUtc).TotalSeconds):0.0}; " +
                        $"freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}";

                    foreach (string expiredKey in expiredKeys)
                    {
                        portStaleSuppressedJournalEvidenceByKey.Remove(expiredKey);
                    }

                    return true;
                }

                foreach (string expiredKey in expiredKeys)
                {
                    portStaleSuppressedJournalEvidenceByKey.Remove(expiredKey);
                }
            }

            return false;
        }

        private bool PortTryAllowFreshAreaStaleVisibleJournalEvidence(
            GoblinEvidenceTemplateRequirement template,
            GoblinEvidenceTemplateMatch match,
            string goblinType,
            string signature,
            DateTime nowUtc,
            string staleVisibleLineDetails,
            out string details)
        {
            details = "";
            if (template.Kind == GoblinEvidenceTemplateKind.JournalKilled)
            {
                details = $"{staleVisibleLineDetails}; bypassAllowed=False; bypassReason=KilledLineRequiresFreshAnchor";
                return false;
            }

            string staleArea = PortGoblinEvidenceNoteValue(staleVisibleLineDetails, "staleArea");
            if (string.IsNullOrWhiteSpace(staleArea))
            {
                details = $"{staleVisibleLineDetails}; bypassAllowed=False; bypassReason=MissingStaleArea";
                return false;
            }

            PortGoblinTrackerAreaResolution areaResult = PortResolveCurrentGoblinArea("Journal");
            areaResult = PortApplyJournalMinimapAreaOverride(goblinType, areaResult, nowUtc, "JournalStaleVisibleFreshAreaBypass");
            string currentArea = areaResult.Area.Resolved ? areaResult.Area.AreaKey : "";
            if (string.IsNullOrWhiteSpace(currentArea))
            {
                details = $"{staleVisibleLineDetails}; bypassAllowed=False; bypassReason=UnresolvedCurrentArea";
                return false;
            }

            double firstSuppressedAgeSeconds = PortParseLogDouble(PortGoblinEvidenceNoteValue(staleVisibleLineDetails, "staleFirstSuppressedAgeSeconds"));
            double evidenceFirstSeenAgeSeconds = PortParseLogDouble(PortGoblinEvidenceNoteValue(staleVisibleLineDetails, "staleEvidenceFirstSeenAgeSeconds"));
            bool oldEnoughForFreshAreaBypass = firstSuppressedAgeSeconds >= 20 || evidenceFirstSeenAgeSeconds >= 20;
            if (!oldEnoughForFreshAreaBypass)
            {
                details = $"{staleVisibleLineDetails}; currentArea={PortLogField(currentArea)}; bypassAllowed=False; bypassReason=StaleLineTooRecent; minimumAgeSeconds=20";
                return false;
            }

            if (string.Equals(staleArea, currentArea, StringComparison.OrdinalIgnoreCase))
            {
                details = $"{staleVisibleLineDetails}; currentArea={PortLogField(currentArea)}; bypassAllowed=False; bypassReason=SameArea";
                return false;
            }

            if (GoblinManualCountBlockList.IsBlocked(currentArea))
            {
                details = $"{staleVisibleLineDetails}; currentArea={PortLogField(currentArea)}; bypassAllowed=False; bypassReason=BlockedArea";
                return false;
            }

            PortForgetJournalFreshnessStateForVisibleLine(signature);
            details =
                $"{staleVisibleLineDetails}; " +
                $"currentArea={PortLogField(currentArea)}; " +
                $"bypassAllowed=True; " +
                $"bypassReason=FreshResolvedAreaAfterOldStaleLine; " +
                $"templateKind={PortLogField(template.Kind.ToString())}; " +
                $"matchLineBucket={PortJournalEvidenceLineBucket(match.MatchPoint)}";
            return true;
        }

        private void PortForgetJournalFreshnessStateForVisibleLine(string signature)
        {
            lock (portGoblinEvidenceLock)
            {
                List<string> staleKeys = portStaleSuppressedJournalEvidenceByKey.Keys
                    .Where(key => string.Equals(key, signature, StringComparison.OrdinalIgnoreCase) ||
                        GoblinJournalEvidencePolicy.SameVisibleGoblinLine(signature, key, out _, out _))
                    .ToList();
                foreach (string key in staleKeys)
                {
                    portStaleSuppressedJournalEvidenceByKey.Remove(key);
                }

                List<string> seenKeys = portJournalEvidenceSeenByKey.Keys
                    .Where(key => string.Equals(key, signature, StringComparison.OrdinalIgnoreCase) ||
                        GoblinJournalEvidencePolicy.SameVisibleGoblinLine(signature, key, out _, out _))
                    .ToList();
                foreach (string key in seenKeys)
                {
                    portJournalEvidenceSeenByKey.Remove(key);
                }

                List<string> killedKeys = portJournalKilledEvidenceSeenBySignature.Keys
                    .Where(key => string.Equals(key, signature, StringComparison.OrdinalIgnoreCase) ||
                        GoblinJournalEvidencePolicy.SameVisibleGoblinLine(signature, key, out _, out _))
                    .ToList();
                foreach (string key in killedKeys)
                {
                    portJournalKilledEvidenceSeenBySignature.Remove(key);
                }
            }
        }

        private static double PortParseLogDouble(string value)
        {
            return double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsed)
                ? parsed
                : 0;
        }

        private static string PortResolvedAreaKey(string location)
        {
            GoblinAreaResolution area = GoblinAreaResolver.Resolve(location);
            return area.Resolved ? area.AreaKey : "";
        }

        private void PortRememberStaleSuppressedJournalEvidence(string signature, DateTime nowUtc, string areaKey = "", DateTime? evidenceFirstSeenUtc = null)
        {
            lock (portGoblinEvidenceLock)
            {
                if (portStaleSuppressedJournalEvidenceByKey.TryGetValue(signature, out GoblinJournalStaleSuppressedState? state))
                {
                    string rememberedArea = string.IsNullOrWhiteSpace(state.AreaKey) ? areaKey : state.AreaKey;
                    DateTime rememberedFirstSeen = evidenceFirstSeenUtc.HasValue && evidenceFirstSeenUtc.Value < state.EvidenceFirstSeenUtc
                        ? evidenceFirstSeenUtc.Value
                        : state.EvidenceFirstSeenUtc;
                    portStaleSuppressedJournalEvidenceByKey[signature] = state with { LastSeenUtc = nowUtc, AreaKey = rememberedArea, EvidenceFirstSeenUtc = rememberedFirstSeen };
                    return;
                }

                portStaleSuppressedJournalEvidenceByKey[signature] = new GoblinJournalStaleSuppressedState(nowUtc, nowUtc, areaKey, evidenceFirstSeenUtc ?? nowUtc);
            }
        }

        private bool PortTrySuppressJournalEvidenceFromResetCarryover(string signature, DateTime nowUtc, out string details)
        {
            details = "";
            lock (portGoblinEvidenceLock)
            {
                List<string> expiredKeys = [];
                foreach (KeyValuePair<string, GoblinJournalResetCarryoverSuppressedState> pair in portJournalResetCarryoverSuppressedBySignature)
                {
                    if (!GoblinJournalFreshnessPolicy.StaleSuppressionActive(pair.Value.LastSeenUtc, nowUtc, GoblinJournalEvidenceFreshWindow))
                    {
                        expiredKeys.Add(pair.Key);
                        continue;
                    }

                    if (!GoblinJournalEvidencePolicy.SameVisibleLineFamily(signature, pair.Value.Signature, out int currentBucket, out int previousBucket))
                    {
                        continue;
                    }

                    portJournalResetCarryoverSuppressedBySignature[pair.Key] = pair.Value with { LastSeenUtc = nowUtc };
                    details =
                        $"signature={PortLogField(signature)}; " +
                        $"previousSignature={PortLogField(pair.Value.Signature)}; " +
                        $"currentLineBucket={currentBucket}; " +
                        $"previousLineBucket={previousBucket}; " +
                        $"resetReason={PortLogField(pair.Value.ResetReason)}; " +
                        $"resetUtc={pair.Value.ResetUtc:O}; " +
                        $"lastSeenAgeSeconds={Math.Max(0, (nowUtc - pair.Value.LastSeenUtc).TotalSeconds):0.0}; " +
                        $"freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}";

                    foreach (string expiredKey in expiredKeys)
                    {
                        portJournalResetCarryoverSuppressedBySignature.Remove(expiredKey);
                    }

                    return true;
                }

                foreach (string expiredKey in expiredKeys)
                {
                    portJournalResetCarryoverSuppressedBySignature.Remove(expiredKey);
                }
            }

            return false;
        }

        private int PortRememberJournalResetCarryoverSuppressions(string reason, DateTime nowUtc)
        {
            int remembered = 0;
            lock (portGoblinEvidenceLock)
            {
                List<string> expiredKeys = [];
                foreach (KeyValuePair<string, GoblinJournalResetCarryoverSuppressedState> pair in portJournalResetCarryoverSuppressedBySignature)
                {
                    if (!GoblinJournalFreshnessPolicy.StaleSuppressionActive(pair.Value.LastSeenUtc, nowUtc, GoblinJournalEvidenceFreshWindow))
                    {
                        expiredKeys.Add(pair.Key);
                    }
                }

                foreach (string expiredKey in expiredKeys)
                {
                    portJournalResetCarryoverSuppressedBySignature.Remove(expiredKey);
                }

                foreach (KeyValuePair<string, GoblinJournalEvidenceSeenState> pair in portJournalEvidenceSeenByKey)
                {
                    if (!GoblinJournalFreshnessPolicy.StaleSuppressionActive(pair.Value.LastSeenUtc, nowUtc, GoblinJournalEvidenceFreshWindow))
                    {
                        continue;
                    }

                    portJournalResetCarryoverSuppressedBySignature[pair.Key] = new GoblinJournalResetCarryoverSuppressedState(
                        pair.Key,
                        nowUtc,
                        pair.Value.LastSeenUtc,
                        reason);
                    remembered++;
                }
            }

            return remembered;
        }

        internal void PortSetGoblinEvidenceFrameSourceForReplayFixtures(IGoblinEvidenceFrameSource? frameSource)
        {
            portGoblinEvidenceFrameSource = frameSource;
        }

        private IGoblinEvidenceFrameSource PortGoblinEvidenceFrameSource()
        {
            return portGoblinEvidenceFrameSource ??= new LiveGoblinEvidenceFrameSource(PortResolveGoblinEvidenceLiveScreenRegion);
        }

        private GoblinEvidenceScanContext? PortCreateGoblinEvidenceScanContext(string source, Rectangle referenceRegion, string reason)
        {
            return PortGoblinEvidenceFrameSource().TryCreateScanContext(source, referenceRegion, reason);
        }

        private Rectangle? PortResolveGoblinEvidenceLiveScreenRegion(Rectangle referenceRegion)
        {
            if (!PortTryGetDiabloRect(out RECT rect))
            {
                return null;
            }

            Rectangle screenRegion = PortScaleReferenceRectangle(referenceRegion, rect);
            screenRegion = Rectangle.Intersect(SystemInformation.VirtualScreen, screenRegion);
            if (screenRegion.Width <= 0 || screenRegion.Height <= 0)
            {
                return null;
            }

            return screenRegion;
        }

        private GoblinEvidenceTemplateMatch PortBestGoblinEvidenceTemplateMatch(GoblinEvidenceScanContext scanContext, string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                return new GoblinEvidenceTemplateMatch(0, Point.Empty, Point.Empty, Size.Empty);
            }

            using OpenCvSharp.Mat templateMat = PortGetGoblinEvidenceTemplateMat(imagePath);
            return GoblinEvidenceFrameTemplateMatcher.MatchTemplate(
                scanContext,
                templateMat,
                PortClassifyGoblinMinimapColor);
        }

        private OpenCvSharp.Mat PortGetGoblinEvidenceTemplateMat(string imagePath)
        {
            try
            {
                FileInfo fileInfo = new(imagePath);
                if (!fileInfo.Exists)
                {
                    return new OpenCvSharp.Mat();
                }

                lock (portGoblinEvidenceLock)
                {
                    if (portGoblinEvidenceTemplateMatCache.TryGetValue(imagePath, out GoblinEvidenceCachedTemplate? cached) &&
                        cached.LastWriteUtc == fileInfo.LastWriteTimeUtc &&
                        cached.Length == fileInfo.Length)
                    {
                        return cached.TemplateMat.Clone();
                    }
                }

                using OpenCvSharp.Mat loaded = OpenCvSharp.Cv2.ImRead(imagePath, OpenCvSharp.ImreadModes.Color);
                if (loaded.Empty())
                {
                    return new OpenCvSharp.Mat();
                }

                OpenCvSharp.Mat cachedMat = loaded.Clone();
                lock (portGoblinEvidenceLock)
                {
                    if (portGoblinEvidenceTemplateMatCache.TryGetValue(imagePath, out GoblinEvidenceCachedTemplate? old))
                    {
                        old.TemplateMat.Dispose();
                    }

                    portGoblinEvidenceTemplateMatCache[imagePath] = new GoblinEvidenceCachedTemplate(
                        cachedMat,
                        fileInfo.LastWriteTimeUtc,
                        fileInfo.Length);
                    return cachedMat.Clone();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Goblin evidence template cache load failed: {imagePath}", ex);
                return new OpenCvSharp.Mat();
            }
        }

        private bool PortTryValidateGoblinJournalNameMatch(
            GoblinEvidenceTemplateRequirement template,
            string imagePath,
            GoblinEvidenceScanContext scanContext,
            GoblinEvidenceTemplateMatch lineMatch,
            out double nameConfidence,
            out Point nameMatchPoint,
            out string reason)
        {
            nameConfidence = 0;
            nameMatchPoint = Point.Empty;
            reason = "NotRequired";
            if (!PortNormalizeGoblinObservationSource(template.Source).Equals("Journal", StringComparison.OrdinalIgnoreCase) ||
                template.Kind == GoblinEvidenceTemplateKind.Minimap)
            {
                return true;
            }

            if (!File.Exists(imagePath))
            {
                reason = "JournalNameValidationUnavailable";
                return false;
            }

            using OpenCvSharp.Mat templateMat = PortGetGoblinEvidenceTemplateMat(imagePath);
            if (templateMat.Empty() || templateMat.Width <= 0 || templateMat.Height <= 0)
            {
                reason = "JournalNameValidationTemplateUnreadable";
                return false;
            }

            Rectangle nameTemplateRect = PortGoblinJournalNameValidationTemplateRect(template.Kind, new Size(templateMat.Width, templateMat.Height));
            if (nameTemplateRect.Width <= 12 || nameTemplateRect.Height <= 8)
            {
                reason = "JournalNameValidationTemplateTooSmall";
                return false;
            }

            Rectangle searchRect = new(
                Math.Max(0, lineMatch.MatchPoint.X + nameTemplateRect.X - 12),
                Math.Max(0, lineMatch.MatchPoint.Y - 6),
                nameTemplateRect.Width + 24,
                nameTemplateRect.Height + 12);
            searchRect = Rectangle.Intersect(new Rectangle(Point.Empty, new Size(scanContext.ScreenMat.Width, scanContext.ScreenMat.Height)), searchRect);
            if (searchRect.Width < nameTemplateRect.Width || searchRect.Height < nameTemplateRect.Height)
            {
                reason = "JournalNameValidationSearchTooSmall";
                return false;
            }

            using OpenCvSharp.Mat nameTemplateMat = new(
                templateMat,
                new OpenCvSharp.Rect(
                    nameTemplateRect.X,
                    nameTemplateRect.Y,
                    nameTemplateRect.Width,
                    nameTemplateRect.Height));
            using OpenCvSharp.Mat nameSearchMat = new(
                scanContext.ScreenMat,
                new OpenCvSharp.Rect(
                    searchRect.X,
                    searchRect.Y,
                    searchRect.Width,
                    searchRect.Height));
            using OpenCvSharp.Mat result = new();
            OpenCvSharp.Cv2.MatchTemplate(nameSearchMat, nameTemplateMat, result, OpenCvSharp.TemplateMatchModes.CCoeffNormed);
            OpenCvSharp.Cv2.MinMaxLoc(result, out _, out nameConfidence, out _, out OpenCvSharp.Point maxLoc);
            nameMatchPoint = new(searchRect.X + maxLoc.X, searchRect.Y + maxLoc.Y);
            if (nameConfidence < GoblinJournalNameValidationThreshold)
            {
                reason = "JournalNameValidationBelowThreshold";
                return false;
            }

            reason = "JournalNameValidated";
            return true;
        }

        private static Rectangle PortGoblinJournalNameValidationTemplateRect(GoblinEvidenceTemplateKind kind, Size templateSize)
        {
            if (templateSize.Width <= 0 || templateSize.Height <= 0)
            {
                return Rectangle.Empty;
            }

            double startRatio = kind == GoblinEvidenceTemplateKind.JournalEngaged
                ? 0.40
                : 0.34;
            int startX = Math.Clamp((int)Math.Round(templateSize.Width * startRatio), 0, Math.Max(0, templateSize.Width - 1));
            int width = templateSize.Width - startX;
            return new Rectangle(startX, 0, width, templateSize.Height);
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
            string directory = PortGoblinEvidenceTemplateDirectory();
            DateTime directoryWriteUtc = PortGoblinEvidenceTemplateDirectoryWriteUtc(directory);
            lock (portGoblinEvidenceLock)
            {
                if (portCachedGoblinEvidenceTemplateCatalog != null &&
                    string.Equals(portCachedGoblinEvidenceTemplateCatalogDirectory, directory, StringComparison.OrdinalIgnoreCase) &&
                    portCachedGoblinEvidenceTemplateCatalogWriteUtc == directoryWriteUtc)
                {
                    return portCachedGoblinEvidenceTemplateCatalog;
                }
            }

            GoblinEvidenceTemplateCatalog catalog = GoblinEvidenceTemplateRequirements.DiscoverTemplates(directory);
            lock (portGoblinEvidenceLock)
            {
                portCachedGoblinEvidenceTemplateCatalog = catalog;
                portCachedGoblinEvidenceTemplateCatalogDirectory = directory;
                portCachedGoblinEvidenceTemplateCatalogWriteUtc = directoryWriteUtc;
            }

            return catalog;
        }

        private static DateTime PortGoblinEvidenceTemplateDirectoryWriteUtc(string directory)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                {
                    return DateTime.MinValue;
                }

                DateTime latest = Directory.GetLastWriteTimeUtc(directory);
                foreach (string file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                    .Where(file => file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
                {
                    DateTime writeUtc = File.GetLastWriteTimeUtc(file);
                    if (writeUtc > latest)
                    {
                        latest = writeUtc;
                    }
                }

                return latest;
            }
            catch
            {
                return DateTime.MinValue;
            }
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
            AppLogger.Info($"GoblinEvidenceScanResult: candidateFound=False; reason=MissingTemplate; templateCount={templateCatalog.Templates.Count}; invalidTemplateCount={templateCatalog.InvalidTemplates.Count}; invalidTemplates={PortLogField(PortGoblinEvidenceInvalidTemplateSummary(templateCatalog))}; diagnosticCropsContinue=True; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}; currentArea={PortLogField(PortDisplayLocation(portLastConfirmedLocation))}; cooldownState={PortLogField(PortGoblinEvidenceCooldownStateForLog())}");
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

            AppLogger.Info($"{eventName}: reason={PortLogField(reason)}; observationModeEnabled={PortGoblinObservationScannerEnabled()}; automaticCountingEnabled={PortGoblinAutomaticCountingEnabled()}; observationModeSetting=GoblinTracker.EnableObservationMode; automaticCountingSetting=GoblinTracker.EnableAutomaticCounting; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}; currentArea={PortLogField(PortDisplayLocation(portLastConfirmedLocation))}; {PortGoblinEvidenceRouteContextForLog()}; cooldownState={PortLogField(PortGoblinEvidenceCooldownStateForLog())}");
        }

        private void PortLogJournalEvidenceFreshnessDiagnostic(
            string eventName,
            GoblinEvidenceTemplateRequirement template,
            GoblinEvidenceTemplateMatch match,
            string currentArea,
            string details)
        {
            DateTime now = DateTime.Now;
            string key = $"{eventName}|{template.Kind}|{template.GoblinType}";
            lock (portGoblinEvidenceLock)
            {
                if (portLastGoblinEvidenceDetectorDiagnosticByKey.TryGetValue(key, out DateTime lastLogged) &&
                    now - lastLogged < GoblinEvidenceDiagnosticLogCooldown)
                {
                    return;
                }

                portLastGoblinEvidenceDetectorDiagnosticByKey[key] = now;
            }

            AppLogger.Info($"{eventName}: source=Journal; goblinType={PortLogField(template.GoblinType)}; evidenceKind={template.Kind}; templateName={PortLogField(template.FileName)}; currentArea={PortLogField(PortDisplayLocation(currentArea))}; bestConfidence={match.Confidence:0.000}; threshold={template.Threshold:0.000}; matchPoint={FormatPoint(match.MatchPoint)}; screenMatchPoint={FormatPoint(match.ScreenMatchPoint)}; {details}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}; {PortGoblinEvidenceRouteContextForLog()}; cooldownState={PortLogField(PortGoblinEvidenceCooldownStateForLog())}");
        }

        private string PortGoblinEvidenceRouteContextForLog()
        {
            string rawArea = PortDisplayLocation(portLastConfirmedLocation);
            string normalizedArea = PortDisplayLocation(PortNormalizeBlockingLocation(portLastConfirmedLocation));
            string displayArea = PortDisplayLocation(PortDetectedLocationDisplayName(portLastConfirmedLocation));
            string routeGroup = PortDisplayLocation(PortGetRouteLocationForDetectedLocation(portLastConfirmedLocation));
            string buttonArea = PortDisplayLocation(PortGetButtonLocationForDetectedLocation(portLastConfirmedLocation));
            string blockingArea = PortDisplayLocation(PortGetConfirmedCurrentLocation());
            string currentButton = PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey));
            string nextButton = PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey));
            string retryButton = PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey));
            return $"routeRawArea={PortLogField(rawArea)}; routeNormalizedArea={PortLogField(normalizedArea)}; routeDisplayArea={PortLogField(displayArea)}; routeGroup={PortLogField(routeGroup)}; routeButtonArea={PortLogField(buttonArea)}; routeBlockingArea={PortLogField(blockingArea)}; routeCurrentButton={PortLogField(currentButton)}; routeNextButton={PortLogField(nextButton)}; routeRetryButton={PortLogField(retryButton)}";
        }

        private string PortGoblinEvidenceCooldownStateForLog()
        {
            lock (portGoblinEvidenceLock)
            {
                string evidenceTypes = portLastGoblinEvidenceByType.Count == 0
                    ? "none"
                    : string.Join("|", portLastGoblinEvidenceByType.Select(pair => $"{pair.Key}:{Math.Max(0, (DateTime.Now - pair.Value).TotalSeconds):0.0}s"));
                string engaged = portRecentJournalEngagedByGoblinType.Count == 0
                    ? "none"
                    : string.Join("|", portRecentJournalEngagedByGoblinType.Select(pair => $"{pair.Key}@{PortDisplayLocation(pair.Value.AreaKey)}:{Math.Max(0, (DateTime.UtcNow - pair.Value.SeenUtc).TotalSeconds):0.0}s"));
                return $"evidence={evidenceTypes}; engaged={engaged}";
            }
        }

        private void PortLogGoblinEvidenceDetectorDiagnostic(
            GoblinEvidenceTemplateRequirement template,
            string result,
            string reason,
            string imagePath,
            Rectangle referenceRegion,
            GoblinEvidenceTemplateMatch match,
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

            double templateCoverage = 0;
            if (referenceRegion.Width > 0 && referenceRegion.Height > 0 && match.TemplateSize.Width > 0 && match.TemplateSize.Height > 0)
            {
                templateCoverage = (match.TemplateSize.Width * match.TemplateSize.Height * 100.0) / (referenceRegion.Width * referenceRegion.Height);
            }

            string journalDiagnosis = "None";
            if (PortNormalizeGoblinObservationSource(template.Source).Equals("Journal", StringComparison.OrdinalIgnoreCase))
            {
                bool fullRegionTemplate = referenceRegion.Width > 0 &&
                    referenceRegion.Height > 0 &&
                    match.TemplateSize.Width >= referenceRegion.Width * 0.85 &&
                    match.TemplateSize.Height >= referenceRegion.Height * 0.85;
                journalDiagnosis = fullRegionTemplate
                    ? "FullRegionTemplate; if journal observations remain zero, capture cropped text-line templates or lower threshold only after crop review"
                    : "CroppedTemplate; if confidence stays below threshold, compare timing and crop visibility";
            }

            AppLogger.Info($"GoblinEvidenceCandidateCheck: type={template.Type}; source={PortNormalizeGoblinObservationSource(template.Source)}; goblinType={PortLogField(template.GoblinType)}; evidenceKind={template.Kind}; result={result}; reason={reason}; bestConfidence={match.Confidence:0.000}; threshold={template.Threshold:0.000}; templateName={PortLogField(template.FileName)}; template={PortLogField(imagePath)}; templateExists={File.Exists(imagePath)}; templateSize={FormatSize(match.TemplateSize)}; templateCoveragePct={templateCoverage:0.0}; journalDiagnosis={PortLogField(journalDiagnosis)}; scanRegion={FormatRectangle(referenceRegion)}; screenRegion={PortGoblinEvidenceScreenRegionForLog(referenceRegion)}; matchPoint={FormatPoint(match.MatchPoint)}; screenMatchPoint={FormatPoint(match.ScreenMatchPoint)}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; {PortGoblinEvidenceRouteContextForLog()}");
        }

        private void PortLogGoblinEvidenceSourceScanResult(
            string source,
            Rectangle referenceRegion,
            GoblinEvidenceDetectionResult detection,
            int templateCount)
        {
            string observationSource = PortNormalizeGoblinObservationSource(source);
            GoblinEvidenceCandidate? candidate = detection.Candidate;
            bool candidateFound = candidate != null;
            GoblinEvidenceTemplateRequirement? bestTemplate = detection.BestTemplate;
            GoblinEvidenceTemplateMatch bestMatch = detection.BestMatch;
            string goblinType = candidateFound ? candidate!.GoblinType : bestTemplate?.GoblinType ?? "None";
            double confidence = candidateFound ? candidate!.Confidence : bestMatch.Confidence;
            string templateName = bestTemplate?.FileName ?? "None";
            string matchPoint = FormatPoint(bestMatch.MatchPoint);
            string threshold = bestTemplate == null ? "None" : bestTemplate.Threshold.ToString("0.000");
            string templateSize = FormatSize(bestMatch.TemplateSize);
            double templateCoverage = PortGoblinEvidenceTemplateCoveragePct(referenceRegion, bestMatch.TemplateSize);
            string journalDiagnosis = bestTemplate == null
                ? "NoMatchingTemplateChecked"
                : PortGoblinEvidenceJournalDiagnosis(bestTemplate.Source, referenceRegion, bestMatch.TemplateSize);
            if (candidateFound)
            {
                templateName = PortExtractGoblinEvidenceNoteValue(candidate!.Notes, "Template");
                matchPoint = PortExtractGoblinEvidenceNoteValue(candidate!.Notes, "MatchPoint");
                threshold = PortExtractGoblinEvidenceNoteValue(candidate!.Notes, "Threshold");
            }

            AppLogger.Info($"GoblinEvidenceScanResult source={observationSource} scanRegion={FormatRectangle(referenceRegion)} screenRegion={PortGoblinEvidenceScreenRegionForLog(referenceRegion)} candidateFound={candidateFound} templateCount={templateCount} templateName={PortLogField(templateName)} goblinType={PortLogField(goblinType)} bestConfidence={confidence:0.000} threshold={PortLogField(threshold)} matchPoint={PortLogField(matchPoint)} templateSize={templateSize} templateCoveragePct={templateCoverage:0.0} journalDiagnosis={PortLogField(journalDiagnosis)}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; {PortGoblinEvidenceRouteContextForLog()}");
        }

        private static double PortGoblinEvidenceTemplateCoveragePct(Rectangle referenceRegion, Size templateSize)
        {
            if (referenceRegion.Width <= 0 ||
                referenceRegion.Height <= 0 ||
                templateSize.Width <= 0 ||
                templateSize.Height <= 0)
            {
                return 0;
            }

            return (templateSize.Width * templateSize.Height * 100.0) / (referenceRegion.Width * referenceRegion.Height);
        }

        private string PortGoblinEvidenceJournalDiagnosis(string source, Rectangle referenceRegion, Size templateSize)
        {
            if (!PortNormalizeGoblinObservationSource(source).Equals("Journal", StringComparison.OrdinalIgnoreCase))
            {
                return "None";
            }

            if (templateSize.Width <= 0 || templateSize.Height <= 0)
            {
                return "TemplateUnreadableOrLargerThanCrop";
            }

            bool fullRegionTemplate = referenceRegion.Width > 0 &&
                referenceRegion.Height > 0 &&
                templateSize.Width >= referenceRegion.Width * 0.85 &&
                templateSize.Height >= referenceRegion.Height * 0.85;
            return fullRegionTemplate
                ? "FullRegionTemplate; if journal observations remain zero, capture cropped text-line templates or lower threshold only after crop review"
                : "CroppedTemplate; if confidence stays below threshold, compare timing and crop visibility";
        }

        private static string PortExtractGoblinEvidenceNoteValue(string notes, string key)
        {
            if (string.IsNullOrWhiteSpace(notes) || string.IsNullOrWhiteSpace(key))
            {
                return "None";
            }

            foreach (string part in notes.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                int separator = part.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                string partKey = part[..separator].Trim();
                if (string.Equals(partKey, key, StringComparison.OrdinalIgnoreCase))
                {
                    return part[(separator + 1)..].Trim();
                }
            }

            return "None";
        }

        private static string FormatSize(Size size)
        {
            return $"{size.Width}x{size.Height}";
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

        private void PortRecordGoblinEvidence(GoblinEvidenceCandidate candidate, bool forceObservation = false)
        {
            candidate = candidate with { GoblinType = GoblinTypeNormalizer.Normalize(candidate.GoblinType) };
            DateTime now = DateTime.Now;
            DateTime detectedUtc = now.ToUniversalTime();
            string evidenceSignature = PortGoblinEvidenceSignature(candidate);
            AppLogger.Info(
                "GoblinLatencyTrace: " +
                "stage=EvidenceDetected; " +
                $"detectedUtc={detectedUtc:O}; " +
                $"source={PortLogField(PortNormalizeGoblinObservationSource(candidate.Source))}; " +
                $"goblinType={PortLogField(candidate.GoblinType)}; " +
                $"evidenceType={candidate.Type}; " +
                $"evidenceKind={PortLogField(PortGoblinEvidenceNoteValue(candidate.Notes, "Kind"))}; " +
                $"confidence={candidate.Confidence:0.000}; " +
                $"evidenceHash={PortGoblinEvidenceHash(evidenceSignature)}; " +
                $"forceObservation={forceObservation}");
            bool suppressedByCooldown = false;
            lock (portGoblinEvidenceLock)
            {
                if (portLastGoblinEvidenceByType.TryGetValue(candidate.Type, out DateTime lastEvidence) &&
                    now - lastEvidence < GoblinEvidenceCooldown)
                {
                    suppressedByCooldown = true;
                }
                else
                {
                    portLastGoblinEvidenceByType[candidate.Type] = now;
                }
            }

            if (suppressedByCooldown)
            {
                if (forceObservation)
                {
                    AppLogger.Info($"GoblinEvidenceManualRefreshResult: candidateFound=True; reason=EvidenceCooldownObservationOnly; type={candidate.Type}; source={candidate.Source}; goblinType={PortLogField(candidate.GoblinType)}; cooldownSeconds={GoblinEvidenceCooldown.TotalSeconds:0}");
                    PortObserveGoblinCandidate(candidate.Source, candidate.GoblinType, evidenceSignature, candidate.Confidence, evidenceNotes: candidate.Notes, rankedSamples: candidate.RankedSamples);
                }

                return;
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
            PortObserveGoblinCandidate(candidate.Source, candidate.GoblinType, evidenceSignature, candidate.Confidence, screenshotPath, candidate.Notes, candidate.RankedSamples);
        }

        private static string PortResolveDebugPackageRuntimeRoot()
        {
            if (AppSettings.IsVsDebugProfile && PortTryResolveConfigRoot(out string configRoot))
            {
                return configRoot;
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static bool PortTryResolveConfigRoot(out string configRoot)
        {
            string? configDirectory = Path.GetDirectoryName(AppSettings.ConfigPath);
            if (!string.IsNullOrWhiteSpace(configDirectory) &&
                string.Equals(Path.GetFileName(configDirectory), "Config", StringComparison.OrdinalIgnoreCase))
            {
                DirectoryInfo? root = Directory.GetParent(configDirectory);
                if (root != null)
                {
                    configRoot = root.FullName;
                    return true;
                }
            }

            configRoot = "";
            return false;
        }

        private static string PortGoblinEvidenceSignature(GoblinEvidenceCandidate candidate)
        {
            string templateName = PortGoblinEvidenceNoteValue(candidate.Notes, "Template");
            string evidenceKind = PortGoblinEvidenceNoteValue(candidate.Notes, "Kind");
            string source = PortNormalizeGoblinObservationSource(candidate.Source);
            string lineBucket = source.Equals("Journal", StringComparison.OrdinalIgnoreCase)
                ? PortGoblinEvidenceJournalLineBucket(candidate.Notes)
                : "";
            return string.Join("|",
                candidate.Type,
                source,
                GoblinTypeNormalizer.Normalize(candidate.GoblinType),
                $"Template={templateName}",
                $"Kind={evidenceKind}",
                $"LineBucket={lineBucket}");
        }

        private static string PortGoblinEvidenceJournalLineBucket(string notes)
        {
            string matchPoint = PortGoblinEvidenceNoteValue(notes, "MatchPoint");
            if (string.IsNullOrWhiteSpace(matchPoint))
            {
                return "";
            }

            string[] parts = matchPoint.Split(',');
            if (parts.Length < 2 || !int.TryParse(parts[1].Trim(), out int y))
            {
                return "";
            }

            return (Math.Max(0, y) / 32).ToString(CultureInfo.InvariantCulture);
        }

        private static string PortApplyMinimapColorDisambiguation(
            GoblinEvidenceTemplateRequirement template,
            GoblinEvidenceTemplateMatch match)
        {
            if (!PortNormalizeGoblinObservationSource(template.Source).Equals("Minimap", StringComparison.OrdinalIgnoreCase) ||
                !PortGoblinTypeUsesMinimapColorDisambiguation(template.GoblinType))
            {
                return template.GoblinType;
            }

            string classifiedGoblinType = PortClassifyMinimapColorGoblinType(template.GoblinType, match.MinimapColor);
            if (string.IsNullOrWhiteSpace(classifiedGoblinType))
            {
                return template.GoblinType;
            }

            if (!classifiedGoblinType.Equals(template.GoblinType, StringComparison.OrdinalIgnoreCase))
            {
                AppLogger.Info(
                    "GoblinEvidenceMinimapColorOverride: " +
                    $"templateGoblinType={PortLogField(template.GoblinType)}; " +
                    $"colorGoblinType={PortLogField(classifiedGoblinType)}; " +
                    $"templateName={PortLogField(template.FileName)}; " +
                    $"yellowPixels={match.MinimapColor.YellowPixels}; " +
                    $"orangePixels={match.MinimapColor.OrangePixels}; " +
                    $"greenPixels={match.MinimapColor.GreenPixels}; " +
                    $"purplePixels={match.MinimapColor.PurplePixels}; " +
                    $"coloredPixels={match.MinimapColor.ColoredPixels}; " +
                    $"matchPoint={FormatPoint(match.MatchPoint)}; " +
                    $"screenMatchPoint={FormatPoint(match.ScreenMatchPoint)}");
            }

            return classifiedGoblinType;
        }

        private static bool PortShouldSuppressMinimapColorDisagreement(
            GoblinEvidenceTemplateRequirement template,
            GoblinEvidenceTemplateMatch match,
            out string colorGoblinType)
        {
            colorGoblinType = "";
            if (!PortNormalizeGoblinObservationSource(template.Source).Equals("Minimap", StringComparison.OrdinalIgnoreCase) ||
                !PortGoblinTypeUsesTreasureOdiousMinimapColor(template.GoblinType))
            {
                return false;
            }

            colorGoblinType = PortClassifyTreasureOdiousMinimapColor(match.MinimapColor);
            return !string.IsNullOrWhiteSpace(colorGoblinType) &&
                !colorGoblinType.Equals(template.GoblinType, StringComparison.OrdinalIgnoreCase);
        }

        private static bool PortGoblinTypeUsesMinimapColorDisambiguation(string goblinType)
        {
            return PortGoblinTypeUsesTreasureOdiousMinimapColor(goblinType) ||
                PortGoblinTypeUsesGildedMalevolentMinimapColor(goblinType);
        }

        private static bool PortGoblinTypeUsesTreasureOdiousMinimapColor(string goblinType)
        {
            string normalized = GoblinTypeNormalizer.Normalize(goblinType);
            return normalized.Equals("Treasure Goblin", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Odious Collector", StringComparison.OrdinalIgnoreCase);
        }

        private static bool PortGoblinTypeUsesGildedMalevolentMinimapColor(string goblinType)
        {
            string normalized = GoblinTypeNormalizer.Normalize(goblinType);
            return normalized.Equals("Gilded Baron", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Malevolent Tormentor", StringComparison.OrdinalIgnoreCase);
        }

        private static string PortClassifyMinimapColorGoblinType(
            string templateGoblinType,
            GoblinMinimapColorClassification color)
        {
            if (PortGoblinTypeUsesTreasureOdiousMinimapColor(templateGoblinType))
            {
                return PortClassifyTreasureOdiousMinimapColor(color);
            }

            if (PortGoblinTypeUsesGildedMalevolentMinimapColor(templateGoblinType))
            {
                return PortClassifyGildedMalevolentMinimapColor(color);
            }

            return "";
        }

        private static string PortClassifyTreasureOdiousMinimapColor(GoblinMinimapColorClassification color)
        {
            const int minimumDominantPixels = 45;
            const double dominanceRatio = 2.0;
            if (color.YellowPixels >= minimumDominantPixels && color.YellowPixels >= color.GreenPixels * dominanceRatio)
            {
                return "Treasure Goblin";
            }

            if (color.GreenPixels >= minimumDominantPixels && color.GreenPixels >= color.YellowPixels * dominanceRatio)
            {
                return "Odious Collector";
            }

            return "";
        }

        private static string PortClassifyGildedMalevolentMinimapColor(GoblinMinimapColorClassification color)
        {
            const int minimumDominantPixels = 45;
            const double dominanceRatio = 2.0;
            if (color.YellowPixels >= minimumDominantPixels && color.YellowPixels >= color.OrangePixels * dominanceRatio)
            {
                return "Gilded Baron";
            }

            if (color.OrangePixels >= minimumDominantPixels && color.OrangePixels >= color.YellowPixels * dominanceRatio)
            {
                return "Malevolent Tormentor";
            }

            return "";
        }

        private static string PortMinimapColorNotes(
            GoblinEvidenceTemplateRequirement template,
            GoblinEvidenceTemplateMatch match)
        {
            if (!PortNormalizeGoblinObservationSource(template.Source).Equals("Minimap", StringComparison.OrdinalIgnoreCase) ||
                !PortGoblinTypeUsesMinimapColorDisambiguation(template.GoblinType))
            {
                return "";
            }

            string classifiedGoblinType = PortClassifyMinimapColorGoblinType(template.GoblinType, match.MinimapColor);
            if (string.IsNullOrWhiteSpace(classifiedGoblinType) &&
                match.MinimapColor.ColoredPixels == 0)
            {
                return "";
            }

            return $"; MinimapColorType={classifiedGoblinType}; MinimapYellowPixels={match.MinimapColor.YellowPixels}; MinimapOrangePixels={match.MinimapColor.OrangePixels}; MinimapGreenPixels={match.MinimapColor.GreenPixels}; MinimapPurplePixels={match.MinimapColor.PurplePixels}; MinimapColoredPixels={match.MinimapColor.ColoredPixels}";
        }

        private static GoblinMinimapColorClassification PortClassifyGoblinMinimapColor(
            Bitmap screenshot,
            Point matchPoint,
            Size templateSize)
        {
            Rectangle matchRect = new(matchPoint, templateSize);
            Rectangle screenRect = new(Point.Empty, screenshot.Size);
            matchRect = Rectangle.Intersect(screenRect, matchRect);
            if (matchRect.Width <= 0 || matchRect.Height <= 0)
            {
                return GoblinMinimapColorClassification.Empty;
            }

            int yellowPixels = 0;
            int orangePixels = 0;
            int greenPixels = 0;
            int purplePixels = 0;
            int coloredPixels = 0;
            for (int y = matchRect.Top; y < matchRect.Bottom; y++)
            {
                for (int x = matchRect.Left; x < matchRect.Right; x++)
                {
                    Color color = screenshot.GetPixel(x, y);
                    PortRgbToHsv(color, out double hue, out double saturation, out double value);
                    if (value < 0.20 || saturation < 0.25)
                    {
                        continue;
                    }

                    coloredPixels++;
                    if (hue >= 10 && hue < 35)
                    {
                        orangePixels++;
                    }
                    else if (hue >= 35 && hue <= 75)
                    {
                        yellowPixels++;
                    }
                    else if (hue >= 80 && hue <= 155)
                    {
                        greenPixels++;
                    }
                    else if (hue >= 250 && hue <= 320)
                    {
                        purplePixels++;
                    }
                }
            }

            return new GoblinMinimapColorClassification(
                "",
                yellowPixels,
                orangePixels,
                greenPixels,
                purplePixels,
                coloredPixels);
        }

        private static void PortRgbToHsv(Color color, out double hue, out double saturation, out double value)
        {
            double red = color.R / 255.0;
            double green = color.G / 255.0;
            double blue = color.B / 255.0;
            double max = Math.Max(red, Math.Max(green, blue));
            double min = Math.Min(red, Math.Min(green, blue));
            double delta = max - min;

            if (delta == 0)
            {
                hue = 0;
            }
            else if (max == red)
            {
                hue = 60 * (((green - blue) / delta) % 6);
            }
            else if (max == green)
            {
                hue = 60 * (((blue - red) / delta) + 2);
            }
            else
            {
                hue = 60 * (((red - green) / delta) + 4);
            }

            if (hue < 0)
            {
                hue += 360;
            }

            saturation = max == 0 ? 0 : delta / max;
            value = max;
        }

        private static string PortGoblinEvidenceNoteValue(string notes, string key)
        {
            if (string.IsNullOrWhiteSpace(notes) || string.IsNullOrWhiteSpace(key))
            {
                return "";
            }

            foreach (string part in notes.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                int separatorIndex = part.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                string partKey = part[..separatorIndex].Trim();
                if (!partKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return part[(separatorIndex + 1)..].Trim();
            }

            return "";
        }

        private void PortResetGoblinEvidenceObservationState(string reason)
        {
            DateTime nowUtc = DateTime.UtcNow;
            int resetCarryoverRemembered = PortRememberJournalResetCarryoverSuppressions(reason, nowUtc);
            int evidenceCooldownsCleared;
            int missingTemplateCooldownsCleared;
            int scanDiagnosticsCleared;
            int detectorDiagnosticsCleared;
            int journalFirstSeenCleared;
            int journalEngagedCleared;
            int staleJournalSuppressedCleared;
            int journalKilledCleared;
            int autoCountEvidenceCleared;
            int autoCountEncounterCleared;
            int recentMinimapObservationsCleared;
            int suppressedMinimapAreaAnchorsCleared;
            lock (portGoblinEvidenceLock)
            {
                evidenceCooldownsCleared = portLastGoblinEvidenceByType.Count;
                missingTemplateCooldownsCleared = portLastGoblinEvidenceMissingTemplateLogByType.Count;
                scanDiagnosticsCleared = portLastGoblinEvidenceScanDiagnosticByKey.Count;
                detectorDiagnosticsCleared = portLastGoblinEvidenceDetectorDiagnosticByKey.Count;
                journalFirstSeenCleared = portJournalEvidenceSeenByKey.Count;
                journalEngagedCleared = portRecentJournalEngagedByGoblinType.Count;
                staleJournalSuppressedCleared = portStaleSuppressedJournalEvidenceByKey.Count;
                journalKilledCleared = portJournalKilledEvidenceSeenBySignature.Count;
                portLastGoblinEvidenceByType.Clear();
                portLastGoblinEvidenceMissingTemplateLogByType.Clear();
                portLastGoblinEvidenceScanDiagnosticByKey.Clear();
                portLastGoblinEvidenceDetectorDiagnosticByKey.Clear();
                portJournalEvidenceSeenByKey.Clear();
                portRecentJournalEngagedByGoblinType.Clear();
                portStaleSuppressedJournalEvidenceByKey.Clear();
                portJournalKilledEvidenceSeenBySignature.Clear();
                Interlocked.Exchange(ref portLastGoblinEvidenceDiagnosticCropTicks, 0);
                Interlocked.Exchange(ref portLastGoblinEvidenceMissingTemplateScanSummaryTicks, 0);
                Interlocked.Exchange(ref portGoblinJournalHistorySuppressUntilTicks, 0);
                DateTime carryoverSuppressUntilUtc = string.Equals(reason, "NewGameCreated", StringComparison.OrdinalIgnoreCase)
                    ? nowUtc + GoblinJournalNewGameCarryoverSuppressWindow
                    : DateTime.MinValue;
                Interlocked.Exchange(ref portGoblinJournalNewGameCarryoverSuppressUntilTicks, carryoverSuppressUntilUtc.Ticks);
            }

            bool hadManualObservation;
            bool hadDisplayedObservation;
            lock (portGoblinTrackerLock)
            {
                autoCountEvidenceCleared = portGoblinAutoCountEvidenceBySignature.Count;
                autoCountEncounterCleared = portGoblinAutoCountEncounterByGoblinType.Count;
                recentMinimapObservationsCleared = portRecentMinimapGoblinObservationByType.Count;
                suppressedMinimapAreaAnchorsCleared = portSuppressedMinimapAreaAnchorByType.Count;
                portGoblinAutoCountEvidenceBySignature.Clear();
                portGoblinAutoCountEncounterByGoblinType.Clear();
                portRecentMinimapGoblinObservationByType.Clear();
                portSuppressedMinimapAreaAnchorByType.Clear();
                hadManualObservation = portLastGoblinObservationForManualCount != null;
                hadDisplayedObservation = portDisplayedGoblinObservation != null || !string.IsNullOrWhiteSpace(portDisplayedGoblinObservationStatus);
                portLastGoblinObservationForManualCount = null;
                portDisplayedGoblinObservation = null;
                portDisplayedGoblinObservationStatus = "No current observation";
                portDisplayedGoblinObservationStickyUntilUtc = DateTime.MinValue;
            }

            AppLogger.Info($"GoblinTracker: Evidence observation state reset reason='{PortLogField(reason)}' clearedEvidenceCooldowns={evidenceCooldownsCleared} clearedMissingTemplateCooldowns={missingTemplateCooldownsCleared} clearedScanDiagnostics={scanDiagnosticsCleared} clearedDetectorDiagnostics={detectorDiagnosticsCleared} clearedJournalFirstSeen={journalFirstSeenCleared} clearedJournalEngaged={journalEngagedCleared} clearedStaleJournalSuppressed={staleJournalSuppressedCleared} clearedJournalKilled={journalKilledCleared} resetCarryoverSuppressionsRemembered={resetCarryoverRemembered} clearedAutoCountEvidence={autoCountEvidenceCleared} clearedAutoCountEncounters={autoCountEncounterCleared} clearedRecentMinimapObservations={recentMinimapObservationsCleared} clearedSuppressedMinimapAreaAnchors={suppressedMinimapAreaAnchorsCleared} clearedManualObservation={hadManualObservation} clearedDisplayedObservation={hadDisplayedObservation}");
            AppLogger.Info($"GoblinTracker: LastObservationCleared reason={PortLogField(reason)} previousDisplayed={hadDisplayedObservation}");
        }


        private sealed record GoblinJournalStaleSuppressedState(
            DateTime FirstSuppressedUtc,
            DateTime LastSeenUtc,
            string AreaKey,
            DateTime EvidenceFirstSeenUtc);

        private sealed record GoblinJournalResetCarryoverSuppressedState(
            string Signature,
            DateTime ResetUtc,
            DateTime LastSeenUtc,
            string ResetReason);

        private sealed record GoblinEvidenceCachedTemplate(
            OpenCvSharp.Mat TemplateMat,
            DateTime LastWriteUtc,
            long Length);

    }
}

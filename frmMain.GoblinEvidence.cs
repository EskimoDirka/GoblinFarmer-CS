using System.Drawing;
using System.Globalization;
using System.IO.Compression;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private const int GoblinEvidenceScanIntervalMs = 750;
        private const int GoblinEvidenceObservationDiagnosticRetentionCount = 24;
        private static readonly TimeSpan GoblinEvidenceCooldown = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan GoblinJournalEvidenceFreshWindow = TimeSpan.FromSeconds(45);
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
            DateTime scanTime = DateTime.Now;
            string journalCropPath = "";
            string minimapCropPath = "";
            if (PortShouldCaptureGoblinEvidenceDiagnosticCrops(scanTime))
            {
                journalCropPath = PortCaptureGoblinEvidenceDiagnosticCrop("Journal", PortGoblinEvidenceJournalRegion(), scanTime);
                minimapCropPath = PortCaptureGoblinEvidenceDiagnosticCrop("Minimap", PortGoblinEvidenceMinimapRegion(), scanTime);
            }

            PortLogGoblinEvidenceScanDiagnostic(
                "ObservationScanAttempted",
                $"Eligible; journalCropPath={PortLogField(PortDisplayLocation(journalCropPath))}; minimapCropPath={PortLogField(PortDisplayLocation(minimapCropPath))}");

            GoblinEvidenceTemplateCatalog templateCatalog = PortGoblinEvidenceTemplateCatalog();
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
            foreach (GoblinEvidenceCandidate candidate in PortDetectGoblinEvidenceCandidates(templateCatalog))
            {
                candidateCount++;
                PortRecordGoblinEvidence(candidate, forceObservation: true);
            }

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
        }

        private IEnumerable<GoblinEvidenceCandidate> PortDetectGoblinEvidenceCandidates(GoblinEvidenceTemplateCatalog templateCatalog)
        {
            List<IGrouping<string, GoblinEvidenceTemplateRequirement>> sourceGroups = templateCatalog.Templates
                .GroupBy(template => template.Source, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => string.Equals(group.Key, "JournalCandidate", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ToList();

            GoblinEvidenceCandidate? primaryJournalCandidate = null;
            List<GoblinEvidenceCandidate> supportingCandidates = [];
            foreach (IGrouping<string, GoblinEvidenceTemplateRequirement> sourceGroup in sourceGroups)
            {
                IReadOnlyList<GoblinEvidenceTemplateRequirement> templates = sourceGroup.ToList();
                Rectangle scanRegion = PortGoblinEvidenceRegionForSource(sourceGroup.Key);
                GoblinEvidenceDetectionResult detection = PortDetectBestGoblinEvidenceTemplate(templates, scanRegion);
                GoblinEvidenceCandidate? candidate = detection.Candidate;
                PortLogGoblinEvidenceSourceScanResult(sourceGroup.Key, scanRegion, detection, templates.Count);

                if (candidate != null &&
                    PortTryAcceptGoblinEvidenceCandidate(sourceGroup.Key, detection, freshKilledWithoutEngagedReason: "Observation", out GoblinEvidenceCandidate? acceptedCandidate))
                {
                    if (string.Equals(sourceGroup.Key, "JournalCandidate", StringComparison.OrdinalIgnoreCase))
                    {
                        primaryJournalCandidate = acceptedCandidate;
                    }
                    else
                    {
                        supportingCandidates.Add(acceptedCandidate!);
                    }
                }
            }

            if (primaryJournalCandidate != null)
            {
                yield return primaryJournalCandidate;
                yield break;
            }

            foreach (GoblinEvidenceCandidate candidate in supportingCandidates)
            {
                yield return candidate;
            }
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
            GoblinEvidenceTemplateRequirement? bestTemplate = null;
            string bestImagePath = "";
            GoblinEvidenceTemplateMatch bestMatch = new(0, Point.Empty, Point.Empty, Size.Empty);
            foreach (GoblinEvidenceTemplateRequirement template in templates)
            {
                string imagePath = Img("Goblin Evidence", template.FileName);
                if (!File.Exists(imagePath))
                {
                    continue;
                }

                GoblinEvidenceTemplateMatch match = PortBestGoblinEvidenceTemplateMatchInDiabloRegion(imagePath, referenceRegion);
                if (bestTemplate == null || match.Confidence > bestMatch.Confidence)
                {
                    bestTemplate = template;
                    bestImagePath = imagePath;
                    bestMatch = match;
                }
            }

            if (bestTemplate == null)
            {
                return new GoblinEvidenceDetectionResult(null, null, bestImagePath, bestMatch, []);
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
                return new GoblinEvidenceDetectionResult(null, bestTemplate, bestImagePath, bestMatch, []);
            }

            PortLogGoblinEvidenceDetectorDiagnostic(
                bestTemplate,
                "Found",
                "ConfidenceMet",
                bestImagePath,
                referenceRegion,
                bestMatch,
                force: true);
            string goblinType = PortApplyMinimapColorDisambiguation(bestTemplate, bestMatch);
            GoblinEvidenceCandidate candidate = new(
                bestTemplate.Type,
                bestMatch.Confidence,
                bestTemplate.Source,
                $"Template={bestTemplate.FileName}; Kind={bestTemplate.Kind}; Threshold={bestTemplate.Threshold:0.000}; MatchPoint={FormatPoint(bestMatch.MatchPoint)}; ScreenMatchPoint={FormatPoint(bestMatch.ScreenMatchPoint)}{PortMinimapColorNotes(bestTemplate, bestMatch)}",
                goblinType);
            return new GoblinEvidenceDetectionResult(candidate, bestTemplate, bestImagePath, bestMatch, []);
        }

        private sealed record GoblinEvidenceDetectionResult(
            GoblinEvidenceCandidate? Candidate,
            GoblinEvidenceTemplateRequirement? BestTemplate,
            string BestImagePath,
            GoblinEvidenceTemplateMatch BestMatch,
            IReadOnlyList<GoblinEvidenceCandidateRank> CandidateRanking);

        private sealed record GoblinEvidenceCandidateRank(
            string TemplateName,
            string GoblinType,
            string Source,
            GoblinEvidenceTemplateKind Kind,
            double Confidence,
            double Threshold,
            string MatchPoint);

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
            string areaKey = areaResult.Area.Resolved ? areaResult.Area.AreaKey : "";
            string displayArea = areaResult.Area.Resolved ? areaResult.Area.DisplayLocation : "Unknown";

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
                    PortLogJournalEvidenceFreshnessDiagnostic(
                        "JournalEngagedIgnoredAreaChanged",
                        template,
                        match,
                        displayArea,
                        $"firstSeenAgeSeconds={firstSeenAge.TotalSeconds:0.0}; firstSeenArea={PortLogField(firstSeenAreaKey)}; currentArea={PortLogField(areaKey)}; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}; staleSuppressed=True");
                    return false;
                }

                if (!GoblinJournalFreshnessPolicy.EngagedIsFresh(firstSeenUtc, firstSeenAreaKey, areaKey, nowUtc, GoblinJournalEvidenceFreshWindow))
                {
                    PortRememberStaleSuppressedJournalEvidence(journalLineSignature, nowUtc);
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
                    Notes = $"{candidate.Notes}; JournalFreshness=EngagedAccepted; JournalArea={displayArea}"
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
                if (!killedFreshInCurrentArea)
                {
                    PortLogJournalEvidenceFreshnessDiagnostic(
                        "JournalKilledIgnoredStale",
                        template,
                        match,
                        displayArea,
                        $"firstSeenAgeSeconds={killedFirstSeenAge.TotalSeconds:0.0}; firstSeenArea={PortLogField(killedState.AreaKey)}; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}");
                    return false;
                }

                bool recentEngagedMatches = GoblinJournalFreshnessPolicy.KilledHasRecentEngaged(
                    recentEngaged,
                    areaKey,
                    nowUtc,
                    GoblinJournalEvidenceFreshWindow);
                if (!recentEngagedMatches && !string.IsNullOrWhiteSpace(freshKilledWithoutEngagedReason))
                {
                    bool acceptedForManualRefresh = string.Equals(freshKilledWithoutEngagedReason, "Manual", StringComparison.OrdinalIgnoreCase);
                    string freshnessReason = acceptedForManualRefresh ? "Manual" : "Observation";
                    string diagnosticEventName = acceptedForManualRefresh
                        ? "JournalKilledAcceptedFreshManual"
                        : "JournalKilledAcceptedFreshObservation";
                    PortLogJournalEvidenceFreshnessDiagnostic(
                        diagnosticEventName,
                        template,
                        match,
                        displayArea,
                        $"firstSeenAgeSeconds={killedFirstSeenAge.TotalSeconds:0.0}; firstSeenArea={PortLogField(killedState.AreaKey)}; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}");
                    acceptedCandidate = candidate with
                    {
                        Notes = $"{candidate.Notes}; JournalFreshness=KilledAcceptedFresh{freshnessReason}; JournalArea={displayArea}"
                    };
                    return true;
                }

                if (!recentEngagedMatches)
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

                PortLogJournalEvidenceFreshnessDiagnostic(
                    "JournalKilledAcceptedAfterEngaged",
                    template,
                    match,
                    displayArea,
                    $"recentEngagedArea={PortLogField(recentEngaged!.AreaKey)}; recentEngagedAgeSeconds={Math.Max(0, (nowUtc - recentEngaged.SeenUtc).TotalSeconds):0.0}; freshnessWindowSeconds={GoblinJournalEvidenceFreshWindow.TotalSeconds:0}");
                acceptedCandidate = candidate with
                {
                    Notes = $"{candidate.Notes}; JournalFreshness=KilledAcceptedAfterEngaged; JournalArea={displayArea}"
                };
                return true;
            }

            acceptedCandidate = candidate;
            return true;
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
            return string.Join("|",
                template.Kind,
                goblinType,
                template.FileName,
                $"LineBucket={PortJournalEvidenceLineBucket(match.MatchPoint)}");
        }

        private static int PortJournalEvidenceLineBucket(Point matchPoint)
        {
            return Math.Max(0, matchPoint.Y) / 32;
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

        private void PortRememberStaleSuppressedJournalEvidence(string signature, DateTime nowUtc)
        {
            lock (portGoblinEvidenceLock)
            {
                if (portStaleSuppressedJournalEvidenceByKey.TryGetValue(signature, out GoblinJournalStaleSuppressedState? state))
                {
                    portStaleSuppressedJournalEvidenceByKey[signature] = state with { LastSeenUtc = nowUtc };
                    return;
                }

                portStaleSuppressedJournalEvidenceByKey[signature] = new GoblinJournalStaleSuppressedState(nowUtc, nowUtc);
            }
        }

        private GoblinEvidenceTemplateMatch PortBestGoblinEvidenceTemplateMatchInDiabloRegion(string imagePath, Rectangle referenceRegion)
        {
            if (!File.Exists(imagePath) || !PortTryGetDiabloRect(out RECT rect))
            {
                return new GoblinEvidenceTemplateMatch(0, Point.Empty, Point.Empty, Size.Empty);
            }

            Rectangle screenRegion = PortScaleReferenceRectangle(referenceRegion, rect);
            screenRegion = Rectangle.Intersect(SystemInformation.VirtualScreen, screenRegion);
            if (screenRegion.Width <= 0 || screenRegion.Height <= 0)
            {
                return new GoblinEvidenceTemplateMatch(0, Point.Empty, Point.Empty, Size.Empty);
            }

            using Bitmap screenshot = new(screenRegion.Width, screenRegion.Height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(screenRegion.Left, screenRegion.Top, 0, 0, screenshot.Size);
            }

            using OpenCvSharp.Mat rawScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot);
            using OpenCvSharp.Mat screenMat = new();
            OpenCvSharp.Cv2.CvtColor(rawScreenMat, screenMat, OpenCvSharp.ColorConversionCodes.BGRA2BGR);

            using OpenCvSharp.Mat templateMat = OpenCvSharp.Cv2.ImRead(imagePath, OpenCvSharp.ImreadModes.Color);
            if (templateMat.Empty() || templateMat.Width > screenMat.Width || templateMat.Height > screenMat.Height)
            {
                return new GoblinEvidenceTemplateMatch(0, Point.Empty, Point.Empty, new Size(templateMat.Width, templateMat.Height));
            }

            using OpenCvSharp.Mat result = new();
            OpenCvSharp.Cv2.MatchTemplate(screenMat, templateMat, result, OpenCvSharp.TemplateMatchModes.CCoeffNormed);
            OpenCvSharp.Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);
            Point matchPoint = new(maxLoc.X, maxLoc.Y);
            Point screenMatchPoint = new(screenRegion.Left + maxLoc.X, screenRegion.Top + maxLoc.Y);
            Size templateSize = new(templateMat.Width, templateMat.Height);
            GoblinMinimapColorClassification minimapColor = PortClassifyGoblinMinimapColor(screenshot, matchPoint, templateSize);
            return new GoblinEvidenceTemplateMatch(maxVal, matchPoint, screenMatchPoint, templateSize, minimapColor);
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

            AppLogger.Info($"{eventName}: reason={PortLogField(reason)}; observationModeEnabled={PortGoblinObservationScannerEnabled()}; automaticCountingEnabled={PortGoblinAutomaticCountingEnabled()}; observationModeSetting=GoblinTracker.EnableObservationMode; automaticCountingSetting=GoblinTracker.EnableAutomaticCounting; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}; currentArea={PortLogField(PortDisplayLocation(portLastConfirmedLocation))}; cooldownState={PortLogField(PortGoblinEvidenceCooldownStateForLog())}");
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

            AppLogger.Info($"{eventName}: source=Journal; goblinType={PortLogField(template.GoblinType)}; evidenceKind={template.Kind}; templateName={PortLogField(template.FileName)}; currentArea={PortLogField(PortDisplayLocation(currentArea))}; bestConfidence={match.Confidence:0.000}; threshold={template.Threshold:0.000}; matchPoint={FormatPoint(match.MatchPoint)}; screenMatchPoint={FormatPoint(match.ScreenMatchPoint)}; {details}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}; diabloRunning={IsDiabloRunning()}; diabloActive={PortDiabloIsActive()}; cooldownState={PortLogField(PortGoblinEvidenceCooldownStateForLog())}");
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

            AppLogger.Info($"GoblinEvidenceCandidateCheck: type={template.Type}; source={PortNormalizeGoblinObservationSource(template.Source)}; goblinType={PortLogField(template.GoblinType)}; evidenceKind={template.Kind}; result={result}; reason={reason}; bestConfidence={match.Confidence:0.000}; threshold={template.Threshold:0.000}; templateName={PortLogField(template.FileName)}; template={PortLogField(imagePath)}; templateExists={File.Exists(imagePath)}; templateSize={FormatSize(match.TemplateSize)}; templateCoveragePct={templateCoverage:0.0}; journalDiagnosis={PortLogField(journalDiagnosis)}; scanRegion={FormatRectangle(referenceRegion)}; screenRegion={PortGoblinEvidenceScreenRegionForLog(referenceRegion)}; matchPoint={FormatPoint(match.MatchPoint)}; screenMatchPoint={FormatPoint(match.ScreenMatchPoint)}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}");
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

            AppLogger.Info($"GoblinEvidenceScanResult source={observationSource} scanRegion={FormatRectangle(referenceRegion)} screenRegion={PortGoblinEvidenceScreenRegionForLog(referenceRegion)} candidateFound={candidateFound} templateCount={templateCount} templateName={PortLogField(templateName)} goblinType={PortLogField(goblinType)} bestConfidence={confidence:0.000} threshold={PortLogField(threshold)} matchPoint={PortLogField(matchPoint)} templateSize={templateSize} templateCoveragePct={templateCoverage:0.0} journalDiagnosis={PortLogField(journalDiagnosis)}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}");
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
                    PortObserveGoblinCandidate(candidate.Source, candidate.GoblinType, PortGoblinEvidenceSignature(candidate), candidate.Confidence);
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
            PortObserveGoblinCandidate(candidate.Source, candidate.GoblinType, PortGoblinEvidenceSignature(candidate), candidate.Confidence, screenshotPath);
        }

        private GoblinReplayReviewFilesResult PortCreateGoblinReplayReviewFilesForReview(string nextTestsPath, string source = "Unknown")
        {
            GoblinReplaySummary? replaySummary = PortRunGoblinReplayForReview(source);
            return PortWriteGoblinReplayReviewFiles(replaySummary, nextTestsPath);
        }

        private string PortWriteGoblinTrackerNextTestMetadata(string source = "Unknown")
        {
            if (!AppSettings.IsVsDebugProfile)
            {
                return "";
            }

            if (portApplicationClosing)
            {
                AppLogger.Info(
                    "GoblinTrackerNextTestsSaveSkipped: " +
                    "reason=AppClosing; " +
                    $"source={PortLogField(source)}");
                return "";
            }

            string directory = Path.Combine(PortResolveDebugPackageRuntimeRoot(), "Debug");
            string path = Path.Combine(directory, "GoblinTrackerNextTests.txt");
            string legacyScenarioPath = Path.Combine(directory, "GoblinTrackerScenario.txt");
            Directory.CreateDirectory(directory);
            bool legacyScenarioDeleted = false;
            if (File.Exists(legacyScenarioPath))
            {
                File.Delete(legacyScenarioPath);
                legacyScenarioDeleted = true;
            }

            List<string> lines = PortNextTestStepMetadataLines();
            File.WriteAllLines(path, lines);
            int checkedCount = portNextTestStepCheckboxes.Count(checkBox => checkBox.Checked);
            int uncheckedCount = portNextTestStepCheckboxes.Count - checkedCount;
            AppLogger.Info(
                "GoblinTrackerNextTestsSaved: " +
                $"source={PortLogField(source)}; " +
                $"path={PortLogField(path)}; " +
                $"testCount={portNextTestStepCheckboxes.Count}; " +
                $"checkedCount={checkedCount}; " +
                $"uncheckedCount={uncheckedCount}; " +
                $"legacyScenarioDeleted={legacyScenarioDeleted}");
            return path;
        }

        private GoblinReplayReviewFilesResult PortWriteGoblinReplayReviewFiles(
            GoblinReplaySummary? replaySummary,
            string nextTestsPath)
        {
            try
            {
                string reviewRoot = PortResolveGoblinReplayReviewRoot();
                string latestDirectory = Path.Combine(reviewRoot, "Latest");
                PortPrepareGoblinReplayReviewDirectory(reviewRoot, latestDirectory);

                string rootNextTestsPath = PortCopyReviewFile(nextTestsPath, latestDirectory, "goblin-tracker-next-tests.txt");
                string debugNextTestsPath = PortCopyReviewFile(nextTestsPath, Path.Combine(latestDirectory, "Debug"), "GoblinTrackerNextTests.txt");
                string sessionInfoPath = PortCopyReviewFile(DebugManager.SessionInfoPath, latestDirectory, "session-info.txt");
                string latestLogPath = PortCopyReviewFile(AppLogger.CurrentLogFilePath, Path.Combine(latestDirectory, "Logs"), Path.GetFileName(AppLogger.CurrentLogFilePath));

                string replayDirectory = Path.Combine(latestDirectory, "Logs", "GoblinReplay");
                Directory.CreateDirectory(replayDirectory);
                string replayLogPath = replaySummary == null ? "" : PortCopyReviewFile(replaySummary.LogPath, replayDirectory, Path.GetFileName(replaySummary.LogPath));
                string replayHtmlPath = replaySummary == null ? "" : PortCopyReviewFile(replaySummary.HtmlReportPath, replayDirectory, Path.GetFileName(replaySummary.HtmlReportPath));
                string replaySummaryPath = replaySummary == null ? "" : PortCopyReviewFile(replaySummary.SummaryPath, replayDirectory, Path.GetFileName(replaySummary.SummaryPath));
                string replayChangedPath = replaySummary == null ? "" : PortCopyReviewFile(replaySummary.ChangedSummaryPath, replayDirectory, Path.GetFileName(replaySummary.ChangedSummaryPath));
                string replayAssetDirectory = "";
                string replayBundleDirectory = "";
                if (replaySummary != null)
                {
                    replayAssetDirectory = PortCopyReviewDirectory(replaySummary.AssetDirectory, replayDirectory, Path.GetFileName(replaySummary.AssetDirectory));
                    replayBundleDirectory = PortCopyReviewDirectory(replaySummary.BundleDirectory, replayDirectory, Path.GetFileName(replaySummary.BundleDirectory));
                }
                GoblinEncounterReviewCopyResult encounterCopy = PortCopyGoblinEncounterReviewCrops(latestDirectory);

                string manifestPath = Path.Combine(latestDirectory, "goblin-replay-review-manifest.txt");
                string summaryPath = Path.Combine(latestDirectory, "goblin-tracker-summary.txt");
                string indexPath = Path.Combine(latestDirectory, "goblin-tracker-review.html");
                PortWriteGoblinReplayLooseReviewManifest(
                    manifestPath,
                    latestDirectory,
                    replaySummary,
                    nextTestsPath,
                    rootNextTestsPath,
                    debugNextTestsPath,
                    sessionInfoPath,
                    latestLogPath,
                    replayLogPath,
                    replayHtmlPath,
                    replaySummaryPath,
                    replayChangedPath,
                    replayAssetDirectory,
                    replayBundleDirectory,
                    encounterCopy);
                PortWriteGoblinReplayLooseReviewSummary(summaryPath, latestDirectory, replaySummary, rootNextTestsPath, encounterCopy);
                PortWriteGoblinReplayLooseReviewIndex(indexPath, latestDirectory);

                AppLogger.Info(
                    "GoblinReplayReviewFilesComplete: " +
                    "success=True; " +
                    $"reviewDirectory={PortLogField(latestDirectory)}; " +
                    $"summaryPath={PortLogField(summaryPath)}; " +
                    $"indexPath={PortLogField(indexPath)}; " +
                    $"manifestPath={PortLogField(manifestPath)}; " +
                    $"nextTestsPath={PortLogField(rootNextTestsPath)}; " +
                    $"replayLogPath={PortLogField(replayLogPath)}; " +
                    $"replayHtmlPath={PortLogField(replayHtmlPath)}; " +
                    $"encounterReviewDirectory={PortLogField(encounterCopy.DestinationDirectory)}; " +
                    $"encounterCropCount={encounterCopy.CopiedCount}; " +
                    $"encounterFullscreenExcluded={encounterCopy.FullscreenExcludedCount}; " +
                    $"zipCreated=False");
                return new GoblinReplayReviewFilesResult(true, latestDirectory, summaryPath, indexPath, "Created");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Goblin replay review files failed: nextTestsPath={nextTestsPath}", ex);
                return new GoblinReplayReviewFilesResult(false, "", "", "", "Exception");
            }
        }

        private static string PortResolveGoblinReplayReviewRoot()
        {
            return Path.Combine(PortResolveDebugPackageRuntimeRoot(), "Debug", "GoblinReplayReview");
        }

        private static void PortPrepareGoblinReplayReviewDirectory(string reviewRoot, string latestDirectory)
        {
            string fullReviewRoot = Path.GetFullPath(reviewRoot);
            string fullLatestDirectory = Path.GetFullPath(latestDirectory);
            if (!fullLatestDirectory.StartsWith(fullReviewRoot, StringComparison.OrdinalIgnoreCase) ||
                fullLatestDirectory.Equals(fullReviewRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unsafe Goblin replay review directory: {latestDirectory}");
            }

            if (Directory.Exists(fullLatestDirectory))
            {
                Directory.Delete(fullLatestDirectory, recursive: true);
            }

            Directory.CreateDirectory(fullLatestDirectory);
        }

        private static string PortCopyReviewFile(string sourcePath, string destinationDirectory, string destinationFileName)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) ||
                string.IsNullOrWhiteSpace(destinationFileName) ||
                !File.Exists(sourcePath))
            {
                return "";
            }

            Directory.CreateDirectory(destinationDirectory);
            string destinationPath = Path.Combine(destinationDirectory, destinationFileName);
            File.Copy(sourcePath, destinationPath, overwrite: true);
            return destinationPath;
        }

        private static string PortCopyReviewDirectory(string sourceDirectory, string destinationParentDirectory, string destinationDirectoryName)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory) ||
                string.IsNullOrWhiteSpace(destinationDirectoryName) ||
                !Directory.Exists(sourceDirectory))
            {
                return "";
            }

            string destinationDirectory = Path.Combine(destinationParentDirectory, destinationDirectoryName);
            foreach (string sourcePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourceDirectory, sourcePath);
                string destinationPath = Path.Combine(destinationDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? destinationDirectory);
                File.Copy(sourcePath, destinationPath, overwrite: true);
            }

            return destinationDirectory;
        }

        private static GoblinEncounterReviewCopyResult PortCopyGoblinEncounterReviewCrops(string latestDirectory)
        {
            string sourceDirectory = Path.Combine(DebugManager.GoblinEvidenceDirectory, "EncounterCaptures");
            string destinationDirectory = Path.Combine(latestDirectory, "GoblinEvidence", "EncounterCaptures");
            if (!Directory.Exists(sourceDirectory))
            {
                return new GoblinEncounterReviewCopyResult("", 0, 0, 0);
            }

            int copied = 0;
            int skipped = 0;
            int fullscreenExcluded = 0;
            Directory.CreateDirectory(destinationDirectory);
            foreach (FileInfo file in new DirectoryInfo(sourceDirectory)
                .EnumerateFiles("*.png", SearchOption.TopDirectoryOnly)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Take(200))
            {
                bool isJournal = file.Name.Contains("_Journal", StringComparison.OrdinalIgnoreCase);
                bool isMinimap = file.Name.Contains("_Minimap", StringComparison.OrdinalIgnoreCase);
                bool isFullscreen = file.Name.Contains("_Fullscreen", StringComparison.OrdinalIgnoreCase);
                if (isFullscreen)
                {
                    fullscreenExcluded++;
                    continue;
                }

                if (!isJournal && !isMinimap)
                {
                    skipped++;
                    continue;
                }

                File.Copy(file.FullName, Path.Combine(destinationDirectory, file.Name), overwrite: true);
                copied++;
            }

            if (copied == 0)
            {
                try
                {
                    Directory.Delete(destinationDirectory);
                    destinationDirectory = "";
                }
                catch
                {
                    // A failed cleanup of an empty review crop folder should not block the review bundle.
                }
            }

            return new GoblinEncounterReviewCopyResult(destinationDirectory, copied, fullscreenExcluded, skipped);
        }

        private static void PortWriteGoblinReplayLooseReviewManifest(
            string manifestPath,
            string reviewDirectory,
            GoblinReplaySummary? replaySummary,
            string sourceNextTestsPath,
            string rootNextTestsPath,
            string debugNextTestsPath,
            string sessionInfoPath,
            string latestLogPath,
            string replayLogPath,
            string replayHtmlPath,
            string replaySummaryPath,
            string replayChangedPath,
            string replayAssetDirectory,
            string replayBundleDirectory,
            GoblinEncounterReviewCopyResult encounterCopy)
        {
            List<string> lines =
            [
                "Goblin Replay Loose Review Manifest",
                $"CreatedLocal={DateTime.Now:O}",
                $"CreatedUtc={DateTime.UtcNow:O}",
                $"ReviewDirectory={reviewDirectory}",
                $"RuntimeRoot={AppDomain.CurrentDomain.BaseDirectory}",
                $"ReviewRoot={PortResolveGoblinReplayReviewRoot()}",
                $"VsDebugProfile={AppSettings.IsVsDebugProfile}",
                $"ConfigPath={AppSettings.ConfigPath}",
                $"ZipCreated=False",
                $"ReplayRan={replaySummary != null}",
                $"SourceNextTestsPath={sourceNextTestsPath}",
                $"RootNextTestsPath={rootNextTestsPath}",
                $"DebugNextTestsPath={debugNextTestsPath}",
                $"SessionInfoPath={sessionInfoPath}",
                $"LatestLogPath={latestLogPath}",
                $"ReplayLogPath={replayLogPath}",
                $"ReplayHtmlPath={replayHtmlPath}",
                $"ReplaySummaryPath={replaySummaryPath}",
                $"ReplayChangedPath={replayChangedPath}",
                $"ReplayAssetDirectory={replayAssetDirectory}",
                $"ReplayBundleDirectory={replayBundleDirectory}",
                $"EncounterCaptureReviewDirectory={encounterCopy.DestinationDirectory}",
                $"EncounterCaptureCropsCopied={encounterCopy.CopiedCount}",
                $"EncounterCaptureFullscreenExcluded={encounterCopy.FullscreenExcludedCount}",
                $"EncounterCaptureSkipped={encounterCopy.SkippedCount}",
            ];

            if (replaySummary != null)
            {
                lines.Add($"ReplayTotalFiles={replaySummary.TotalFiles}");
                lines.Add($"ReplayEvidenceFiles={replaySummary.EvidenceFiles}");
                lines.Add($"ReplayAccepted={replaySummary.Accepted}");
                lines.Add($"ReplayRejected={replaySummary.Rejected}");
                lines.Add($"ReplayUnknown={replaySummary.Unknown}");
                lines.Add($"ReplayComparisonChanged={replaySummary.ComparisonChanged}");
                lines.Add($"ReplayComparisonNew={replaySummary.ComparisonNew}");
                lines.Add($"ReplayComparisonSame={replaySummary.ComparisonSame}");
            }

            File.WriteAllLines(manifestPath, lines);
        }

        private static void PortWriteGoblinReplayLooseReviewSummary(
            string summaryPath,
            string reviewDirectory,
            GoblinReplaySummary? replaySummary,
            string nextTestsPath,
            GoblinEncounterReviewCopyResult encounterCopy)
        {
            List<string> lines =
            [
                "Goblin Tracker Loose Review Summary",
                $"CreatedLocal={DateTime.Now:O}",
                $"ReviewDirectory={reviewDirectory}",
                $"ZipCreated=False",
                $"NextTests={(string.IsNullOrWhiteSpace(nextTestsPath) ? "Missing" : Path.GetRelativePath(reviewDirectory, nextTestsPath))}",
                "",
            ];

            if (replaySummary == null)
            {
                lines.Add("Replay: skipped or unavailable");
            }
            else
            {
                lines.Add("Replay:");
                lines.Add($"  Total files: {replaySummary.TotalFiles}");
                lines.Add($"  Evidence files: {replaySummary.EvidenceFiles}");
                lines.Add($"  Accepted: {replaySummary.Accepted}");
                lines.Add($"  Rejected: {replaySummary.Rejected}");
                lines.Add($"  Unknown: {replaySummary.Unknown}");
                lines.Add($"  Comparison changed/new/same: {replaySummary.ComparisonChanged}/{replaySummary.ComparisonNew}/{replaySummary.ComparisonSame}");
            }

            lines.Add("");
            lines.Add("Encounter captures:");
            lines.Add($"  Journal/minimap crops copied: {encounterCopy.CopiedCount}");
            lines.Add($"  Fullscreen captures excluded from review: {encounterCopy.FullscreenExcludedCount}");
            lines.Add($"  Review crop folder: {(!string.IsNullOrWhiteSpace(encounterCopy.DestinationDirectory) ? Path.GetRelativePath(reviewDirectory, encounterCopy.DestinationDirectory) : "none")}");

            if (!string.IsNullOrWhiteSpace(nextTestsPath) && File.Exists(nextTestsPath))
            {
                lines.Add("");
                lines.Add("Next tests:");
                foreach (string line in File.ReadLines(nextTestsPath))
                {
                    lines.Add($"  {line}");
                }
            }

            File.WriteAllLines(summaryPath, lines);
        }

        private static void PortWriteGoblinReplayLooseReviewIndex(string indexPath, string reviewDirectory)
        {
            static string Html(string value) => System.Net.WebUtility.HtmlEncode(value ?? "");

            List<string> lines =
            [
                "<!doctype html>",
                "<html><head><meta charset=\"utf-8\"><title>Goblin Replay Review Files</title>",
                "<style>body{font-family:Segoe UI,Arial,sans-serif;margin:20px;color:#202124}li{margin:6px 0}</style>",
                "</head><body>",
                "<h1>Goblin Replay Review Files</h1>",
                $"<p><strong>Created:</strong> {DateTime.Now:O}<br><strong>Folder:</strong> {Html(reviewDirectory)}<br><strong>ZIP:</strong> not created</p>",
                "<ul>",
            ];

            foreach (string relativePath in new[]
            {
                "goblin-tracker-summary.txt",
                "goblin-replay-review-manifest.txt",
                "goblin-tracker-next-tests.txt",
                "session-info.txt",
            })
            {
                PortAddLooseReviewIndexLink(lines, reviewDirectory, relativePath);
            }

            foreach (FileInfo file in new DirectoryInfo(reviewDirectory)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(file =>
                    file.Name.StartsWith("GoblinReplay_", StringComparison.OrdinalIgnoreCase) &&
                    (file.Extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
                    file.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase) ||
                    file.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase))
            {
                PortAddLooseReviewIndexLink(lines, reviewDirectory, Path.GetRelativePath(reviewDirectory, file.FullName));
            }

            lines.Add("</ul></body></html>");
            File.WriteAllLines(indexPath, lines);
        }

        private static void PortAddLooseReviewIndexLink(List<string> lines, string reviewDirectory, string relativePath)
        {
            string path = Path.Combine(reviewDirectory, relativePath);
            if (!File.Exists(path))
            {
                return;
            }

            string href = relativePath.Replace('\\', '/');
            string text = relativePath.Replace('\\', '/');
            lines.Add($"<li><a href=\"{System.Net.WebUtility.HtmlEncode(href)}\">{System.Net.WebUtility.HtmlEncode(text)}</a></li>");
        }

        private GoblinReplaySummary? PortRunGoblinReplayForReview(string source = "Unknown")
        {
            if (!AppSettings.IsVsDebugProfile)
            {
                AppLogger.Info($"ReviewGoblinReplaySkipped: reason=NonVsDebugProfile; source={PortLogField(source)}");
                return null;
            }

            string replayInputPath = PortResolveReviewGoblinReplayInputPath();
            if (!Directory.Exists(replayInputPath))
            {
                AppLogger.Info(
                    "ReviewGoblinReplaySkipped: " +
                    "reason=InputFolderMissing; " +
                    $"source={PortLogField(source)}; " +
                    $"inputPath={PortLogField(replayInputPath)}; " +
                    $"runtimeRoot={PortLogField(AppDomain.CurrentDomain.BaseDirectory)}; " +
                    $"packageRuntimeRoot={PortLogField(PortResolveDebugPackageRuntimeRoot())}");
                return null;
            }

            try
            {
                AppLogger.Info(
                    "ReviewGoblinReplayStarted: " +
                    $"source={PortLogField(source)}; " +
                    $"inputPath={PortLogField(replayInputPath)}; " +
                    $"runtimeRoot={PortLogField(AppDomain.CurrentDomain.BaseDirectory)}; " +
                    $"packageRuntimeRoot={PortLogField(PortResolveDebugPackageRuntimeRoot())}");
                GoblinReplaySummary summary = PortReplayGoblinEvidenceFolder(replayInputPath);
                AppLogger.Info(
                    "ReviewGoblinReplayComplete: " +
                    $"source={PortLogField(source)}; " +
                    $"totalFiles={summary.TotalFiles}; " +
                    $"evidenceFiles={summary.EvidenceFiles}; " +
                    $"accepted={summary.Accepted}; " +
                    $"rejected={summary.Rejected}; " +
                    $"unknown={summary.Unknown}; " +
                    $"logPath={PortLogField(summary.LogPath)}; " +
                    $"htmlReportPath={PortLogField(summary.HtmlReportPath)}");
                return summary;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Review Goblin replay failed before loose review file creation: inputPath={replayInputPath}", ex);
                return null;
            }
        }

        private static string PortResolveReviewGoblinReplayInputPath()
        {
            return DebugManager.GoblinEvidenceDirectory;
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

        private GoblinReplaySummary PortReplayGoblinEvidenceFolder(string folder)
        {
            DateTime started = DateTime.Now;
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);
            string timestamp = started.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string logPath = Path.Combine(logDirectory, $"GoblinReplay_{timestamp}.log");
            string htmlPath = Path.Combine(logDirectory, $"GoblinReplay_{timestamp}.html");
            string assetDirectory = Path.Combine(logDirectory, $"GoblinReplay_{timestamp}_files");
            string summaryPath = Path.Combine(logDirectory, $"GoblinReplay_{timestamp}_summary.txt");
            string changedPath = Path.Combine(logDirectory, $"GoblinReplay_{timestamp}_changed.txt");
            string bundleDirectory = Path.Combine(logDirectory, $"GoblinReplay_{timestamp}_bundles");
            string extractionRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplay_{timestamp}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(assetDirectory);
            Directory.CreateDirectory(bundleDirectory);
            Directory.CreateDirectory(extractionRoot);

            GoblinEvidenceTemplateCatalog templateCatalog = GoblinEvidenceTemplateRequirements.DiscoverTemplates(PortGoblinEvidenceTemplateDirectory());
            List<GoblinReplayInputFile> files = PortCollectGoblinReplayInputFiles(folder, extractionRoot);
            Dictionary<string, GoblinReplayPreviousDecision> previousDecisions = PortLoadPreviousGoblinReplayDecisions(logDirectory, logPath);
            GoblinAreaDuplicateGuard replayDuplicateGuard = new();
            List<GoblinReplayReportRow> reportRows = [];
            int evidenceFiles = 0;
            int accepted = 0;
            int rejected = 0;
            int unknown = 0;
            int replayTotal = 0;
            int comparisonChanged = 0;
            int comparisonNew = 0;
            int comparisonSame = 0;
            List<string> lines =
            [
                $"GoblinReplayStart: inputPath={folder}; started={started:O}; dryRun=True; templateCount={templateCatalog.Templates.Count}; inputFiles={files.Count}; extractedZipWorkspace={extractionRoot}; enableObservationMode={AppSettings.GoblinTracker.EnableObservationMode}; enableAutomaticCounting={AppSettings.GoblinTracker.EnableAutomaticCounting}; enableDecisionTrace={AppSettings.GoblinTracker.EnableDecisionTrace}; previousReplayDecisionCount={previousDecisions.Count}",
            ];

            try
            {
                foreach (GoblinReplayInputFile file in files)
                {
                    string source = PortInferGoblinReplaySource(file);
                    if (string.IsNullOrWhiteSpace(source))
                    {
                        unknown++;
                        lines.Add($"GoblinReplayFile: file={file.DisplayPath}; evidenceFile=False; archivePath={PortLogField(file.ArchivePath)}; reason=SourceUnknown");
                        continue;
                    }

                    evidenceFiles++;
                    GoblinReplayAreaInference areaInference = PortInferGoblinReplayArea(file);
                    lines.Add($"GoblinReplayAreaInference: imageFile={PortLogField(file.DisplayPath)}; archivePath={PortLogField(file.ArchivePath)}; relativePath={PortLogField(file.RelativePath)}; areaRaw={PortLogField(areaInference.Area.RawLocation)}; areaKey={PortLogField(areaInference.Area.AreaKey)}; inferenceSource={PortLogField(areaInference.Source)}; reason={PortLogField(areaInference.Reason)}");

                    GoblinEvidenceDetectionResult detection = PortDetectBestGoblinEvidenceTemplateInImageFile(source, templateCatalog.Templates, file.ExtractedPath);
                    int rank = 0;
                    foreach (GoblinEvidenceCandidateRank candidateRank in detection.CandidateRanking.Take(5))
                    {
                        rank++;
                        lines.Add($"GoblinReplayCandidateRanking: imageFile={PortLogField(file.DisplayPath)}; rank={rank}; source={PortNormalizeGoblinObservationSource(candidateRank.Source)}; goblinType={PortLogField(candidateRank.GoblinType)}; evidenceKind={candidateRank.Kind}; templateName={PortLogField(candidateRank.TemplateName)}; confidence={candidateRank.Confidence:0.000}; threshold={candidateRank.Threshold:0.000}; matchPoint={PortLogField(candidateRank.MatchPoint)}");
                    }

                    if (detection.Candidate == null)
                    {
                        unknown++;
                        lines.Add($"GoblinReplayFile: file={file.DisplayPath}; evidenceFile=True; source={PortNormalizeGoblinObservationSource(source)}; candidateFound=False; bestTemplate={PortLogField(detection.BestTemplate?.FileName ?? "")}; bestConfidence={detection.BestMatch.Confidence:0.000}; htmlReportPath={PortLogField(htmlPath)}");
                        reportRows.Add(PortCreateGoblinReplayReportRow(file, null, detection, areaInference, null, "Unknown", "", "", assetDirectory));
                        continue;
                    }

                    GoblinEvidenceCandidate candidate = detection.Candidate with
                    {
                        GoblinType = GoblinTypeNormalizer.Normalize(detection.Candidate.GoblinType),
                        Source = PortNormalizeGoblinObservationSource(detection.Candidate.Source),
                    };
                    GoblinAreaResolution area = areaInference.Area;
                    DateTime nowUtc = DateTime.UtcNow;
                    DateTime evidenceSeenUtc = file.LastWriteTimeUtc == DateTime.MinValue ? nowUtc : file.LastWriteTimeUtc;
                    double evidenceAgeSeconds = Math.Max(0, (nowUtc - evidenceSeenUtc).TotalSeconds);
                    bool autoCountingEnabled = PortGoblinAutomaticCountingEnabled();
                    GoblinAreaDuplicateGuardResult guardResult = area.Resolved
                        ? replayDuplicateGuard.Peek(area.AreaKey)
                        : new GoblinAreaDuplicateGuardResult(false, 0, 0);
                    string suppressionReason = "";

                    if (!autoCountingEnabled)
                    {
                        suppressionReason = "AutomaticCountingDisabled";
                    }
                    else if (portGoblinAutomaticCountingArmedAtUtc != DateTime.MinValue &&
                        evidenceSeenUtc < portGoblinAutomaticCountingArmedAtUtc)
                    {
                        suppressionReason = "EvidenceSeenBeforeAutoCountEnabled";
                    }
                    else if (!GoblinJournalFreshnessPolicy.IsFresh(evidenceSeenUtc, nowUtc, GoblinJournalEvidenceFreshWindow))
                    {
                        suppressionReason = "StaleEvidence";
                    }
                    else if (!area.Resolved)
                    {
                        suppressionReason = "AreaUnresolved";
                    }
                    else if (GoblinManualCountBlockList.IsBlocked(area.AreaKey))
                    {
                        suppressionReason = "BlockedArea";
                        guardResult = new GoblinAreaDuplicateGuardResult(false, 0, 0);
                    }
                    else if (!guardResult.Accepted)
                    {
                        suppressionReason = guardResult.AreaLimit > 1 ? "AreaLimitReached" : "AreaAlreadyCounted";
                    }

                    bool counted = string.IsNullOrWhiteSpace(suppressionReason);
                    int areaCountBefore = guardResult.AreaCount;
                    if (counted)
                    {
                        replayDuplicateGuard.TryAccept(area.AreaKey, out guardResult);
                        replayTotal++;
                        accepted++;
                    }
                    else
                    {
                        rejected++;
                    }

                    string evidenceSignature = PortGoblinEvidenceSignature(candidate);
                    GoblinDecisionTraceRecord trace = GoblinDecisionTracePolicy.Create(
                        nowUtc,
                        "Replay",
                        candidate.Source,
                        Path.GetFileName(file.DisplayPath),
                        file.DisplayPath,
                        area.RawLocation,
                        area.AreaKey,
                        candidate.GoblinType,
                        evidenceSignature,
                        evidenceAgeSeconds,
                        evidenceAgeSeconds,
                        autoCountingEnabled,
                        AppSettings.GoblinTracker.EnableObservationMode,
                        suppressionReason,
                        counted,
                        areaCountBefore,
                        guardResult.AreaLimit,
                        replayTotal - (counted ? 1 : 0));
                    string comparisonKey = PortGoblinReplayComparisonKey(trace);
                    string comparison = "New";
                    string previousDecision = "";
                    string previousReason = "";
                    if (previousDecisions.TryGetValue(comparisonKey, out GoblinReplayPreviousDecision? previous))
                    {
                        previousDecision = previous.Decision;
                        previousReason = previous.Reason;
                        comparison = previous.Decision.Equals(trace.Decision, StringComparison.OrdinalIgnoreCase) &&
                            previous.Reason.Equals(trace.Reason, StringComparison.OrdinalIgnoreCase)
                                ? "Same"
                                : "Changed";
                    }

                    if (comparison.Equals("Same", StringComparison.OrdinalIgnoreCase))
                    {
                        comparisonSame++;
                    }
                    else if (comparison.Equals("Changed", StringComparison.OrdinalIgnoreCase))
                    {
                        comparisonChanged++;
                    }
                    else
                    {
                        comparisonNew++;
                    }

                    lines.Add($"{GoblinDecisionTracePolicy.ToLogLine(trace)}; replayComparison={comparison}; previousDecision={PortLogField(previousDecision)}; previousReason={PortLogField(previousReason)}; replayComparisonKey={PortLogField(comparisonKey)}");
                    reportRows.Add(PortCreateGoblinReplayReportRow(file, candidate, detection, areaInference, trace, comparison, previousDecision, previousReason, assetDirectory));
                }
            }
            finally
            {
                try
                {
                    Directory.Delete(extractionRoot, recursive: true);
                }
                catch
                {
                }
            }

            GoblinReplaySummary summary = new(
                files.Count,
                evidenceFiles,
                accepted,
                rejected,
                unknown,
                logPath,
                htmlPath,
                summaryPath,
                changedPath,
                bundleDirectory,
                assetDirectory,
                comparisonChanged,
                comparisonNew,
                comparisonSame);
            lines.Add($"GoblinReplaySummary: totalFiles={summary.TotalFiles}; evidenceFiles={summary.EvidenceFiles}; accepted={summary.Accepted}; rejected={summary.Rejected}; unknown={summary.Unknown}; comparisonChanged={summary.ComparisonChanged}; comparisonNew={summary.ComparisonNew}; comparisonSame={summary.ComparisonSame}; logPath={summary.LogPath}; htmlReportPath={summary.HtmlReportPath}; summaryPath={summaryPath}; changedSummaryPath={changedPath}; bundleDirectory={bundleDirectory}");
            File.WriteAllLines(logPath, lines);
            PortWriteGoblinReplayHtmlReport(htmlPath, summary, reportRows, folder, started);
            PortWriteGoblinReplayDecisionArtifacts(summaryPath, changedPath, bundleDirectory, summary, reportRows, htmlPath, folder, started);
            AppLogger.Info($"GoblinReplaySummary: totalFiles={summary.TotalFiles}; evidenceFiles={summary.EvidenceFiles}; accepted={summary.Accepted}; rejected={summary.Rejected}; unknown={summary.Unknown}; comparisonChanged={summary.ComparisonChanged}; comparisonNew={summary.ComparisonNew}; comparisonSame={summary.ComparisonSame}; logPath={PortLogField(summary.LogPath)}; htmlReportPath={PortLogField(summary.HtmlReportPath)}; summaryPath={PortLogField(summaryPath)}; changedSummaryPath={PortLogField(changedPath)}; bundleDirectory={PortLogField(bundleDirectory)}");
            return summary;
        }

        private GoblinEvidenceDetectionResult PortDetectBestGoblinEvidenceTemplateInImageFile(
            string source,
            IReadOnlyList<GoblinEvidenceTemplateRequirement> templates,
            string evidenceImagePath)
        {
            string observationSource = PortNormalizeGoblinObservationSource(source);
            List<GoblinEvidenceTemplateRequirement> sourceTemplates = templates
                .Where(template => PortNormalizeGoblinObservationSource(template.Source).Equals(observationSource, StringComparison.OrdinalIgnoreCase))
                .ToList();
            GoblinEvidenceTemplateRequirement? bestTemplate = null;
            string bestImagePath = "";
            GoblinEvidenceTemplateMatch bestMatch = new(0, Point.Empty, Point.Empty, Size.Empty);
            List<GoblinEvidenceCandidateRank> ranking = [];

            foreach (GoblinEvidenceTemplateRequirement template in sourceTemplates)
            {
                string imagePath = Path.Combine(PortGoblinEvidenceTemplateDirectory(), template.FileName);
                GoblinEvidenceTemplateMatch match = PortBestGoblinEvidenceTemplateMatchInImageFile(evidenceImagePath, imagePath, observationSource);
                ranking.Add(new GoblinEvidenceCandidateRank(
                    template.FileName,
                    template.GoblinType,
                    template.Source,
                    template.Kind,
                    match.Confidence,
                    template.Threshold,
                    FormatPoint(match.MatchPoint)));
                if (bestTemplate == null || match.Confidence > bestMatch.Confidence)
                {
                    bestTemplate = template;
                    bestImagePath = imagePath;
                    bestMatch = match;
                }
            }

            if (bestTemplate == null || bestMatch.Confidence < bestTemplate.Threshold)
            {
                return new GoblinEvidenceDetectionResult(
                    null,
                    bestTemplate,
                    bestImagePath,
                    bestMatch,
                    ranking.OrderByDescending(item => item.Confidence).Take(5).ToList());
            }

            string goblinType = PortApplyMinimapColorDisambiguation(bestTemplate, bestMatch);
            GoblinEvidenceCandidate candidate = new(
                bestTemplate.Type,
                bestMatch.Confidence,
                observationSource,
                $"Template={bestTemplate.FileName}; Kind={bestTemplate.Kind}; Threshold={bestTemplate.Threshold:0.000}; MatchPoint={FormatPoint(bestMatch.MatchPoint)}; ScreenMatchPoint={FormatPoint(bestMatch.ScreenMatchPoint)}{PortMinimapColorNotes(bestTemplate, bestMatch)}",
                goblinType);
            return new GoblinEvidenceDetectionResult(
                candidate,
                bestTemplate,
                bestImagePath,
                bestMatch,
                ranking.OrderByDescending(item => item.Confidence).Take(5).ToList());
        }

        private static GoblinEvidenceTemplateMatch PortBestGoblinEvidenceTemplateMatchInImageFile(
            string evidenceImagePath,
            string templatePath,
            string source)
        {
            if (!File.Exists(evidenceImagePath) || !File.Exists(templatePath))
            {
                return new GoblinEvidenceTemplateMatch(0, Point.Empty, Point.Empty, Size.Empty);
            }

            using OpenCvSharp.Mat evidenceMat = OpenCvSharp.Cv2.ImRead(evidenceImagePath, OpenCvSharp.ImreadModes.Color);
            using OpenCvSharp.Mat templateMat = OpenCvSharp.Cv2.ImRead(templatePath, OpenCvSharp.ImreadModes.Color);
            if (evidenceMat.Empty() || templateMat.Empty() || templateMat.Width > evidenceMat.Width || templateMat.Height > evidenceMat.Height)
            {
                return new GoblinEvidenceTemplateMatch(0, Point.Empty, Point.Empty, new Size(templateMat.Width, templateMat.Height));
            }

            using OpenCvSharp.Mat result = new();
            OpenCvSharp.Cv2.MatchTemplate(evidenceMat, templateMat, result, OpenCvSharp.TemplateMatchModes.CCoeffNormed);
            OpenCvSharp.Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);
            Point matchPoint = new(maxLoc.X, maxLoc.Y);
            Size templateSize = new(templateMat.Width, templateMat.Height);
            GoblinMinimapColorClassification minimapColor = GoblinMinimapColorClassification.Empty;
            if (source.Equals("Minimap", StringComparison.OrdinalIgnoreCase))
            {
                using Bitmap evidenceBitmap = new(evidenceImagePath);
                minimapColor = PortClassifyGoblinMinimapColor(evidenceBitmap, matchPoint, templateSize);
            }

            return new GoblinEvidenceTemplateMatch(maxVal, matchPoint, matchPoint, templateSize, minimapColor);
        }

        private static List<GoblinReplayInputFile> PortCollectGoblinReplayInputFiles(string inputPath, string extractionRoot)
        {
            string[] supportedExtensions = [".png", ".jpg", ".jpeg", ".bmp"];
            List<GoblinReplayInputFile> files = [];
            if (File.Exists(inputPath) && inputPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                PortExtractGoblinReplayZip(inputPath, extractionRoot, files, supportedExtensions);
            }
            else if (Directory.Exists(inputPath))
            {
                foreach (FileInfo file in new DirectoryInfo(inputPath)
                    .EnumerateFiles("*.*", SearchOption.AllDirectories)
                    .Where(file => supportedExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)))
                {
                    files.Add(new GoblinReplayInputFile(
                        file.FullName,
                        file.FullName,
                        file.LastWriteTimeUtc,
                        "",
                        Path.GetRelativePath(inputPath, file.FullName)));
                }

                foreach (FileInfo zip in new DirectoryInfo(inputPath)
                    .EnumerateFiles("*.zip", SearchOption.AllDirectories)
                    .OrderBy(file => file.LastWriteTimeUtc)
                    .ThenBy(file => file.FullName, StringComparer.OrdinalIgnoreCase))
                {
                    PortExtractGoblinReplayZip(zip.FullName, extractionRoot, files, supportedExtensions);
                }
            }

            return files
                .OrderBy(file => file.LastWriteTimeUtc)
                .ThenBy(file => file.DisplayPath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void PortExtractGoblinReplayZip(
            string zipPath,
            string extractionRoot,
            List<GoblinReplayInputFile> files,
            string[] supportedExtensions)
        {
            string safeName = System.Text.RegularExpressions.Regex.Replace(Path.GetFileNameWithoutExtension(zipPath), @"[^A-Za-z0-9_.-]+", "_");
            string extractDirectory = Path.Combine(extractionRoot, $"{safeName}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(extractDirectory);
            ZipFile.ExtractToDirectory(zipPath, extractDirectory, overwriteFiles: true);
            foreach (FileInfo file in new DirectoryInfo(extractDirectory)
                .EnumerateFiles("*.*", SearchOption.AllDirectories)
                .Where(file => supportedExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)))
            {
                string relativePath = Path.GetRelativePath(extractDirectory, file.FullName);
                files.Add(new GoblinReplayInputFile(
                    $"{zipPath}!{relativePath}",
                    file.FullName,
                    file.LastWriteTimeUtc,
                    zipPath,
                    relativePath));
            }
        }

        private static string PortInferGoblinReplaySource(GoblinReplayInputFile file)
        {
            string value = $"{file.DisplayPath} {file.RelativePath}";
            if (value.Contains("Journal", StringComparison.OrdinalIgnoreCase))
            {
                return "JournalCandidate";
            }

            if (value.Contains("Minimap", StringComparison.OrdinalIgnoreCase))
            {
                return "MinimapCandidate";
            }

            return "";
        }

        private static GoblinReplayAreaInference PortInferGoblinReplayArea(GoblinReplayInputFile file)
        {
            string normalizedPath = GoblinAreaResolver.NormalizedKey($"{file.DisplayPath} {file.RelativePath}");
            foreach (string areaName in GoblinAreaResolver.KnownAreas.OrderByDescending(area => area.Length))
            {
                string areaKey = GoblinAreaResolver.NormalizedKey(areaName);
                if (normalizedPath.Contains(areaKey, StringComparison.OrdinalIgnoreCase))
                {
                    return new GoblinReplayAreaInference(
                        GoblinAreaResolver.Resolve(areaName),
                        "PathKnownArea",
                        $"MatchedKnownArea:{areaName}");
                }
            }

            return new GoblinReplayAreaInference(
                GoblinAreaResolver.Resolve(""),
                "Unresolved",
                "NoKnownAreaNameInPath");
        }

        private static Dictionary<string, GoblinReplayPreviousDecision> PortLoadPreviousGoblinReplayDecisions(string logDirectory, string currentLogPath)
        {
            Dictionary<string, GoblinReplayPreviousDecision> decisions = new(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(logDirectory))
            {
                return decisions;
            }

            FileInfo? previousLog = new DirectoryInfo(logDirectory)
                .EnumerateFiles("GoblinReplay_*.log", SearchOption.TopDirectoryOnly)
                .Where(file => !file.FullName.Equals(currentLogPath, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault();
            if (previousLog == null)
            {
                return decisions;
            }

            foreach (string line in File.ReadLines(previousLog.FullName))
            {
                if (!line.Contains("GoblinDecisionTrace:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Dictionary<string, string> fields = PortParseGoblinReplayLogFields(line);
                string key = string.Join("|",
                    fields.GetValueOrDefault("source", ""),
                    fields.GetValueOrDefault("imageFile", ""),
                    fields.GetValueOrDefault("areaKey", ""),
                    fields.GetValueOrDefault("goblinType", ""));
                if (string.IsNullOrWhiteSpace(key.Replace("|", "")))
                {
                    continue;
                }

                decisions[key] = new GoblinReplayPreviousDecision(
                    fields.GetValueOrDefault("decision", ""),
                    fields.GetValueOrDefault("reason", ""),
                    previousLog.FullName);
            }

            return decisions;
        }

        private static Dictionary<string, string> PortParseGoblinReplayLogFields(string line)
        {
            Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase);
            string payload = line.Contains(':') ? line[(line.IndexOf(':') + 1)..] : line;
            foreach (string part in payload.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                int separator = part.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                fields[part[..separator].Trim()] = part[(separator + 1)..].Trim();
            }

            return fields;
        }

        private static string PortGoblinReplayComparisonKey(GoblinDecisionTraceRecord trace)
        {
            return string.Join("|",
                trace.Source,
                trace.ImageFile,
                trace.AreaKey,
                trace.GoblinType);
        }

        private static GoblinReplayReportRow PortCreateGoblinReplayReportRow(
            GoblinReplayInputFile file,
            GoblinEvidenceCandidate? candidate,
            GoblinEvidenceDetectionResult detection,
            GoblinReplayAreaInference areaInference,
            GoblinDecisionTraceRecord? trace,
            string comparison,
            string previousDecision,
            string previousReason,
            string assetDirectory)
        {
            string assetPath = "";
            try
            {
                string extension = Path.GetExtension(file.ExtractedPath);
                if (string.IsNullOrWhiteSpace(extension))
                {
                    extension = ".png";
                }

                string baseName = trace == null
                    ? System.Text.RegularExpressions.Regex.Replace(Path.GetFileNameWithoutExtension(file.DisplayPath), @"[^A-Za-z0-9_.-]+", "_")
                    : trace.CorrelationId;
                string destination = Path.Combine(assetDirectory, $"{baseName}{extension}");
                File.Copy(file.ExtractedPath, destination, overwrite: true);
                assetPath = Path.GetRelativePath(Path.GetDirectoryName(assetDirectory)!, destination);
            }
            catch
            {
            }

            return new GoblinReplayReportRow(
                file.DisplayPath,
                assetPath,
                candidate?.GoblinType ?? detection.BestTemplate?.GoblinType ?? "Unknown",
                candidate?.Source ?? detection.BestTemplate?.Source ?? "Unknown",
                detection.BestTemplate?.FileName ?? "None",
                detection.BestMatch.Confidence,
                areaInference.Area.AreaKey,
                areaInference.Source,
                areaInference.Reason,
                trace?.Decision ?? "Unknown",
                trace?.Reason ?? "NoCandidate",
                comparison,
                previousDecision,
                previousReason,
                detection.CandidateRanking);
        }

        private static void PortWriteGoblinReplayHtmlReport(
            string htmlPath,
            GoblinReplaySummary summary,
            IReadOnlyList<GoblinReplayReportRow> rows,
            string inputPath,
            DateTime started)
        {
            static string Html(string value) => System.Net.WebUtility.HtmlEncode(value ?? "");
            List<string> lines =
            [
                "<!doctype html>",
                "<html><head><meta charset=\"utf-8\"><title>Goblin Replay Report</title>",
                "<style>body{font-family:Segoe UI,Arial,sans-serif;margin:20px;color:#202124}table{border-collapse:collapse;width:100%;font-size:13px}th,td{border:1px solid #d0d7de;padding:6px;vertical-align:top}th{background:#f6f8fa;text-align:left}img{max-width:220px;max-height:160px}.Count{background:#e6ffed}.Stale,.Block,.Duplicate,.Suppress,.Unknown{background:#fff5b1}.Changed{outline:2px solid #d1242f}.Same{outline:2px solid #1a7f37}.New{outline:2px solid #0969da}</style>",
                "</head><body>",
                "<h1>Goblin Replay Report</h1>",
                $"<p><strong>Input:</strong> {Html(inputPath)}<br><strong>Started:</strong> {started:O}<br><strong>Log:</strong> {Html(summary.LogPath)}</p>",
                $"<p><strong>Total files:</strong> {summary.TotalFiles} <strong>Evidence:</strong> {summary.EvidenceFiles} <strong>Accepted:</strong> {summary.Accepted} <strong>Rejected:</strong> {summary.Rejected} <strong>Unknown:</strong> {summary.Unknown} <strong>Comparison changed/new/same:</strong> {summary.ComparisonChanged}/{summary.ComparisonNew}/{summary.ComparisonSame}</p>",
                "<table><thead><tr><th>Evidence</th><th>Image</th><th>Decision</th><th>Area</th><th>Best Match</th><th>Top Candidates</th><th>Comparison</th></tr></thead><tbody>",
            ];

            foreach (GoblinReplayReportRow row in rows)
            {
                string candidates = string.Join("<br>", row.CandidateRanking.Select((candidate, index) =>
                    $"{index + 1}. {Html(candidate.TemplateName)} ({Html(candidate.GoblinType)}) {candidate.Confidence:0.000}/{candidate.Threshold:0.000}"));
                string image = string.IsNullOrWhiteSpace(row.AssetPath)
                    ? ""
                    : $"<img src=\"{Html(row.AssetPath.Replace('\\', '/'))}\" alt=\"evidence\">";
                lines.Add($"<tr class=\"{Html(row.Decision)} {Html(row.Comparison)}\"><td>{Html(row.DisplayPath)}</td><td>{image}</td><td><strong>{Html(row.Decision)}</strong><br>{Html(row.Reason)}</td><td>{Html(row.AreaKey)}<br>{Html(row.AreaInferenceSource)}<br>{Html(row.AreaInferenceReason)}</td><td>{Html(row.BestTemplate)}<br>{Html(row.GoblinType)}<br>{row.BestConfidence:0.000}</td><td>{candidates}</td><td>{Html(row.Comparison)}<br>previous={Html(row.PreviousDecision)}<br>{Html(row.PreviousReason)}</td></tr>");
            }

            lines.Add("</tbody></table></body></html>");
            File.WriteAllLines(htmlPath, lines);
        }

        private static void PortWriteGoblinReplayDecisionArtifacts(
            string summaryPath,
            string changedPath,
            string bundleDirectory,
            GoblinReplaySummary summary,
            IReadOnlyList<GoblinReplayReportRow> rows,
            string htmlPath,
            string inputPath,
            DateTime started)
        {
            List<string> summaryLines =
            [
                "Goblin Replay Decision Summary",
                $"Input={inputPath}",
                $"Started={started:O}",
                $"LogPath={summary.LogPath}",
                $"HtmlReportPath={htmlPath}",
                $"TotalFiles={summary.TotalFiles}",
                $"EvidenceFiles={summary.EvidenceFiles}",
                $"Accepted={summary.Accepted}",
                $"Rejected={summary.Rejected}",
                $"Unknown={summary.Unknown}",
                $"ComparisonChanged={summary.ComparisonChanged}",
                $"ComparisonNew={summary.ComparisonNew}",
                $"ComparisonSame={summary.ComparisonSame}",
                "",
                "Grouped decisions:",
            ];

            foreach (var group in rows
                .GroupBy(row => new { row.AreaKey, row.GoblinType, row.Decision, row.Reason })
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key.AreaKey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(group => group.Key.GoblinType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(group => group.Key.Decision, StringComparer.OrdinalIgnoreCase))
            {
                summaryLines.Add($"count={group.Count()}; area={group.Key.AreaKey}; goblinType={group.Key.GoblinType}; decision={group.Key.Decision}; reason={group.Key.Reason}");
            }

            File.WriteAllLines(summaryPath, summaryLines);

            List<GoblinReplayReportRow> changedRows = rows
                .Where(row => row.Comparison.Equals("Changed", StringComparison.OrdinalIgnoreCase))
                .ToList();
            List<string> changedLines =
            [
                "Goblin Replay Changed Decisions",
                $"ChangedCount={changedRows.Count}",
                $"PreviousComparisonChanged={summary.ComparisonChanged}",
                "",
            ];
            if (changedRows.Count == 0)
            {
                changedLines.Add("No changed replay decisions compared with the previous replay log.");
            }
            else
            {
                foreach (GoblinReplayReportRow row in changedRows)
                {
                    changedLines.Add($"file={row.DisplayPath}; area={row.AreaKey}; goblinType={row.GoblinType}; decision={row.Decision}; reason={row.Reason}; previousDecision={row.PreviousDecision}; previousReason={row.PreviousReason}; bestTemplate={row.BestTemplate}; confidence={row.BestConfidence:0.000}");
                }
            }

            File.WriteAllLines(changedPath, changedLines);

            string htmlDirectory = Path.GetDirectoryName(htmlPath) ?? "";
            int bundleIndex = 0;
            foreach (GoblinReplayReportRow row in rows.Where(row =>
                !row.Decision.Equals("Count", StringComparison.OrdinalIgnoreCase) ||
                row.Comparison.Equals("Changed", StringComparison.OrdinalIgnoreCase)))
            {
                bundleIndex++;
                string bundleName = $"{bundleIndex:000}_{PortSanitizeReplayBundleName(row.Decision)}_{PortSanitizeReplayBundleName(row.GoblinType)}_{PortSanitizeReplayBundleName(row.AreaKey)}";
                string rowBundleDirectory = Path.Combine(bundleDirectory, bundleName);
                Directory.CreateDirectory(rowBundleDirectory);
                List<string> decisionLines =
                [
                    "Goblin Replay Decision Bundle",
                    $"DisplayPath={row.DisplayPath}",
                    $"AssetPath={row.AssetPath}",
                    $"GoblinType={row.GoblinType}",
                    $"Source={row.Source}",
                    $"BestTemplate={row.BestTemplate}",
                    $"BestConfidence={row.BestConfidence:0.000}",
                    $"AreaKey={row.AreaKey}",
                    $"AreaInferenceSource={row.AreaInferenceSource}",
                    $"AreaInferenceReason={row.AreaInferenceReason}",
                    $"Decision={row.Decision}",
                    $"Reason={row.Reason}",
                    $"Comparison={row.Comparison}",
                    $"PreviousDecision={row.PreviousDecision}",
                    $"PreviousReason={row.PreviousReason}",
                    "",
                    "Candidate ranking:",
                ];
                int rank = 0;
                foreach (GoblinEvidenceCandidateRank candidate in row.CandidateRanking.Take(10))
                {
                    rank++;
                    decisionLines.Add($"{rank}. template={candidate.TemplateName}; goblinType={candidate.GoblinType}; source={candidate.Source}; kind={candidate.Kind}; confidence={candidate.Confidence:0.000}; threshold={candidate.Threshold:0.000}; matchPoint={candidate.MatchPoint}");
                }

                File.WriteAllLines(Path.Combine(rowBundleDirectory, "decision.txt"), decisionLines);
                if (!string.IsNullOrWhiteSpace(row.AssetPath))
                {
                    string assetSourcePath = Path.Combine(htmlDirectory, row.AssetPath);
                    if (File.Exists(assetSourcePath))
                    {
                        File.Copy(assetSourcePath, Path.Combine(rowBundleDirectory, Path.GetFileName(assetSourcePath)), overwrite: true);
                    }
                }
            }
        }

        private static string PortSanitizeReplayBundleName(string value)
        {
            value = string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
            string sanitized = System.Text.RegularExpressions.Regex.Replace(value, @"[^A-Za-z0-9_.-]+", "_").Trim('_');
            return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized;
        }

        private sealed record GoblinReplayInputFile(
            string DisplayPath,
            string ExtractedPath,
            DateTime LastWriteTimeUtc,
            string ArchivePath,
            string RelativePath);

        private sealed record GoblinReplayAreaInference(
            GoblinAreaResolution Area,
            string Source,
            string Reason);

        private sealed record GoblinReplayPreviousDecision(
            string Decision,
            string Reason,
            string LogPath);

        private sealed record GoblinReplayReportRow(
            string DisplayPath,
            string AssetPath,
            string GoblinType,
            string Source,
            string BestTemplate,
            double BestConfidence,
            string AreaKey,
            string AreaInferenceSource,
            string AreaInferenceReason,
            string Decision,
            string Reason,
            string Comparison,
            string PreviousDecision,
            string PreviousReason,
            IReadOnlyList<GoblinEvidenceCandidateRank> CandidateRanking);

        private sealed record GoblinReplaySummary(
            int TotalFiles,
            int EvidenceFiles,
            int Accepted,
            int Rejected,
            int Unknown,
            string LogPath,
            string HtmlReportPath,
            string SummaryPath,
            string ChangedSummaryPath,
            string BundleDirectory,
            string AssetDirectory,
            int ComparisonChanged,
            int ComparisonNew,
            int ComparisonSame);

        private sealed record GoblinReplayReviewFilesResult(
            bool Success,
            string ReviewDirectory,
            string SummaryPath,
            string IndexPath,
            string Reason);

        private sealed record GoblinEncounterReviewCopyResult(
            string DestinationDirectory,
            int CopiedCount,
            int FullscreenExcludedCount,
            int SkippedCount);

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
            }

            bool hadManualObservation;
            bool hadDisplayedObservation;
            lock (portGoblinTrackerLock)
            {
                autoCountEvidenceCleared = portGoblinAutoCountEvidenceBySignature.Count;
                autoCountEncounterCleared = portGoblinAutoCountEncounterByGoblinType.Count;
                portGoblinAutoCountEvidenceBySignature.Clear();
                portGoblinAutoCountEncounterByGoblinType.Clear();
                hadManualObservation = portLastGoblinObservationForManualCount != null;
                hadDisplayedObservation = portDisplayedGoblinObservation != null || !string.IsNullOrWhiteSpace(portDisplayedGoblinObservationStatus);
                portLastGoblinObservationForManualCount = null;
                portDisplayedGoblinObservation = null;
                portDisplayedGoblinObservationStatus = "No current observation";
                portDisplayedGoblinObservationStickyUntilUtc = DateTime.MinValue;
            }

            AppLogger.Info($"GoblinTracker: Evidence observation state reset reason='{PortLogField(reason)}' clearedEvidenceCooldowns={evidenceCooldownsCleared} clearedMissingTemplateCooldowns={missingTemplateCooldownsCleared} clearedScanDiagnostics={scanDiagnosticsCleared} clearedDetectorDiagnostics={detectorDiagnosticsCleared} clearedJournalFirstSeen={journalFirstSeenCleared} clearedJournalEngaged={journalEngagedCleared} clearedStaleJournalSuppressed={staleJournalSuppressedCleared} clearedJournalKilled={journalKilledCleared} clearedAutoCountEvidence={autoCountEvidenceCleared} clearedAutoCountEncounters={autoCountEncounterCleared} clearedManualObservation={hadManualObservation} clearedDisplayedObservation={hadDisplayedObservation}");
            AppLogger.Info($"GoblinTracker: LastObservationCleared reason={PortLogField(reason)} previousDisplayed={hadDisplayedObservation}");
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

        private sealed record GoblinJournalStaleSuppressedState(DateTime FirstSuppressedUtc, DateTime LastSeenUtc);
    }
}

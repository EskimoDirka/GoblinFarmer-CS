using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private int sessionGamesCreated;
        private int sessionTeleportsCompleted;
        private int sessionBlockedTeleports;
        private int sessionFailures;
        private DateTime sessionStartTime;
        private readonly DateTime sessionScreenshotRetentionStartTime = DateTime.Now;
        private int portLastLoggedGoblinStatsUiCount = -1;
        private string portLastLoggedGoblinStatsUiActiveTime = "";
        private string portLastLoggedGoblinStatsUiObservation = "";
        private const double PortAutomaticGoblinMinimapCountMinimumConfidence = 0.85;
        private const double PortAutomaticGoblinAmbiguousMinimapCountMinimumConfidence = 0.90;

        private void PortInitializeSessionStats()
        {
            sessionStartTime = DateTime.Now;
            PortResetGoblinAreaDuplicateGuard("SessionStart");
            PortResetGoblinAutoCountEvidenceState("SessionStart");
            PortSetGoblinAutomaticCountingArmedState("SessionStart");
            PortValidateGoblinEvidenceTemplateSetup("Startup", notifyIfMissing: false);
            PortStartGoblinObservationScanner("Startup");
            PortWriteSessionMetadata();
            PortUpdateSessionStats();
            PortUpdateGoblinTrackerStats();
        }

        private void PortIncrementGamesCreated()
        {
            Interlocked.Increment(ref sessionGamesCreated);
            DebugManager.Session.RecordGameCreated();
            DebugManager.Session.ResetGoblinTrackerStats();
            PortResetGoblinAreaDuplicateGuard("NewGameCreated");
            PortResetGoblinAutoCountEvidenceState("NewGameCreated");
            PortResetGoblinEvidenceObservationState("NewGameCreated");
            AppLogger.Info("GoblinTracker: Session statistics reset reason='NewGameCreated'");
            PortUpdateSessionStats();
        }

        private void PortIncrementTeleportsCompleted()
        {
            Interlocked.Increment(ref sessionTeleportsCompleted);
            DebugManager.Session.RecordTeleportConfirmed();
            PortUpdateSessionStats();
        }

        private void PortIncrementBlockedTeleports()
        {
            Interlocked.Increment(ref sessionBlockedTeleports);
            DebugManager.Session.RecordTeleportBlocked($"Teleport blocked: {PortDisplayLocation(portLastBlockingReason)}");
            PortUpdateSessionStats();
        }

        private void PortIncrementFailures()
        {
            int failures = Interlocked.Increment(ref sessionFailures);
            AppLogger.Info($"Failure counter incremented: {failures}");
            PortUpdateSessionStats();
        }

        private int PortFailureCount()
        {
            return Volatile.Read(ref sessionFailures);
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
            PortUpdateGoblinTrackerStats();
        }

        private void PortResetGoblinTrackerStats()
        {
            DebugManager.Session.ResetGoblinTrackerStats();
            PortResetGoblinAreaDuplicateGuard("TrackerStatsReset");
            PortResetGoblinAutoCountEvidenceState("TrackerStatsReset");
            PortResetGoblinEvidenceObservationState("TrackerStatsReset");
            AppLogger.Info("GoblinTracker: Session statistics reset");
            PortWriteSessionMetadata(logSuccess: false);
            PortUpdateGoblinTrackerStats();
        }

        private bool PortTryRecordGoblinFound(string source, string goblinType, bool allowUnresolvedFallback)
        {
            PortGoblinTrackerAreaResolution areaResult = PortResolveCurrentGoblinArea(source);
            GoblinAreaResolution area = areaResult.Area;
            source = string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
            goblinType = GoblinTypeNormalizer.Normalize(goblinType);
            bool manualUnknownResolved = source.Equals("ManualHotkey", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(goblinType, "Unknown", StringComparison.OrdinalIgnoreCase) &&
                area.Resolved;
            string preRefreshType = "";
            GoblinObservationRecord? preRefreshObservation = null;
            bool canReusePreRefreshObservation = manualUnknownResolved &&
                PortCanReuseRecentManualObservationType(area.AreaKey, goblinType, out preRefreshType, out preRefreshObservation);
            if (manualUnknownResolved && !canReusePreRefreshObservation)
            {
                PortTryRefreshGoblinObservationForManualHotkey();
            }
            else if (canReusePreRefreshObservation && preRefreshObservation != null)
            {
                double ageSeconds = Math.Max(0, (DateTime.UtcNow - preRefreshObservation.TimestampUtc).TotalSeconds);
                AppLogger.Info($"GoblinTracker: Manual count skipped evidence refresh reason=RecentSameAreaObservation goblinType={PortLogField(preRefreshType)} areaKey={PortLogField(PortDisplayLocation(area.AreaKey))} observationSource={PortLogField(preRefreshObservation.Source)} observationAgeSeconds={ageSeconds:0.0} maxAgeSeconds={PortManualGoblinObservationTypeReuseWindow.TotalSeconds:0}");
            }

            goblinType = PortResolveGoblinTypeForManualCount(source, goblinType, area.AreaKey);
            bool hasFreshManualObservationForUnknown = area.Resolved &&
                PortHasFreshManualCountObservation(area.AreaKey);
            string rawLocation = PortDisplayLocation(area.RawLocation);
            string areaKey = PortDisplayLocation(area.AreaKey);
            string displayLocation = PortDisplayLocation(area.DisplayLocation);
            string suppressionReason = "";
            int total = 0;
            GoblinAreaDuplicateGuardResult guardResult = new(true, 0, 0);

            lock (portGoblinTrackerLock)
            {
                if (areaResult.Blocked)
                {
                    suppressionReason = areaResult.SuppressionReason;
                    GoblinFoundRecord suppressedRecord = new(
                        area.AreaKey,
                        area.DisplayLocation,
                        goblinType,
                        source,
                        DateTime.UtcNow,
                        false,
                        suppressionReason);
                    DebugManager.Session.RecordGoblinFoundRecord(suppressedRecord);
                    AppLogger.Info($"GoblinTracker: GoblinCountSuppressed reason={suppressionReason} blockListStatus=Allowed countResult=Suppressed areaCount=0 areaLimit=0 best='{PortLogField(areaResult.BestName)}' bestConfidence={areaResult.BestConfidence:0.000} second='{PortLogField(areaResult.SecondName)}' secondConfidence={areaResult.SecondConfidence:0.000} delta={areaResult.Delta:0.000} ambiguityGroup={PortLogField(areaResult.AmbiguityGroup)} source='{PortLogField(source)}'");
                    if (source.Equals("ManualHotkey", StringComparison.OrdinalIgnoreCase))
                    {
                        PortShowSplash("Goblin count skipped: ambiguous area", 2500);
                    }

                    return false;
                }

                if (!area.Resolved)
                {
                    if (!allowUnresolvedFallback)
                    {
                        suppressionReason = "AreaUnresolved";
                        GoblinFoundRecord suppressedRecord = new(
                            "",
                            "",
                            goblinType,
                            source,
                            DateTime.UtcNow,
                            false,
                            suppressionReason);
                        DebugManager.Session.RecordGoblinFoundRecord(suppressedRecord);
                        AppLogger.Info($"GoblinTracker: Count suppressed rawLocation='{PortLogField(rawLocation)}' areaKey='Unknown' type='{PortLogField(goblinType)}' source='{PortLogField(source)}' reason='{suppressionReason}'");
                        return false;
                    }

                    AppLogger.Info($"GoblinTracker: Area unresolved source='{PortLogField(source)}'; rawLocation='{PortLogField(rawLocation)}'; falling back to existing count behavior");
                }
                else if (source.Equals("ManualHotkey", StringComparison.OrdinalIgnoreCase) &&
                    GoblinManualCountBlockList.IsBlocked(area.AreaKey))
                {
                    suppressionReason = "BlockedArea";
                    GoblinFoundRecord suppressedRecord = new(
                        area.AreaKey,
                        area.DisplayLocation,
                        goblinType,
                        source,
                        DateTime.UtcNow,
                        false,
                        suppressionReason);
                    DebugManager.Session.RecordGoblinFoundRecord(suppressedRecord);
                    AppLogger.Info($"GoblinTracker: GoblinCountSuppressed areaKey={areaKey} reason={suppressionReason} source={PortLogField(source)} blockListStatus=Blocked countResult=Suppressed areaCount=0 areaLimit=0 rawLocation='{PortLogField(rawLocation)}' displayLocation='{PortLogField(displayLocation)}' type='{PortLogField(goblinType)}'");
                    PortShowSplash("Goblin count skipped: blocked area", 2500);
                    return false;
                }
                else if (area.Resolved)
                {
                    guardResult = portGoblinAreaDuplicateGuard.Peek(area.AreaKey);
                    if (!guardResult.Accepted)
                    {
                        suppressionReason = guardResult.AreaLimit > 1 ? "AreaLimitReached" : "AreaAlreadyCounted";
                        GoblinFoundRecord suppressedRecord = new(
                            area.AreaKey,
                            area.DisplayLocation,
                            goblinType,
                            source,
                            DateTime.UtcNow,
                            false,
                            suppressionReason);
                        DebugManager.Session.RecordGoblinFoundRecord(suppressedRecord);
                        AppLogger.Info($"GoblinTracker: GoblinCountSuppressed areaKey={areaKey} areaCount={guardResult.AreaCount} areaLimit={guardResult.AreaLimit} reason={suppressionReason} blockListStatus=Allowed countResult=Suppressed rawLocation='{PortLogField(rawLocation)}' type='{PortLogField(goblinType)}' source='{PortLogField(source)}'");
                        return false;
                    }

                    bool suppressUnknownManualCount = GoblinManualCountPolicy.RequiresFreshObservationForUnknownManualCount(
                        source,
                        goblinType,
                        area.Resolved,
                        AppSettings.GoblinTracker.AllowUnknownManualCount,
                        hasFreshManualObservationForUnknown);
                    if (suppressUnknownManualCount)
                    {
                        suppressionReason = "NoFreshObservation";
                        GoblinFoundRecord suppressedRecord = new(
                            area.AreaKey,
                            area.DisplayLocation,
                            goblinType,
                            source,
                            DateTime.UtcNow,
                            false,
                            suppressionReason);
                        DebugManager.Session.RecordGoblinFoundRecord(suppressedRecord);
                        AppLogger.Info($"GoblinTracker: GoblinCountSuppressed areaKey={areaKey} reason={suppressionReason} source={PortLogField(source)} blockListStatus=Allowed countResult=Suppressed areaCount={guardResult.AreaCount} areaLimit={guardResult.AreaLimit} rawLocation='{PortLogField(rawLocation)}' displayLocation='{PortLogField(displayLocation)}' type='{PortLogField(goblinType)}' allowUnknownManualCount={AppSettings.GoblinTracker.AllowUnknownManualCount}");
                        PortShowSplash("No fresh goblin observation to count.", 3000);
                        return false;
                    }

                    if (!portGoblinAreaDuplicateGuard.TryAccept(area.AreaKey, out guardResult))
                    {
                        suppressionReason = guardResult.AreaLimit > 1 ? "AreaLimitReached" : "AreaAlreadyCounted";
                        GoblinFoundRecord suppressedRecord = new(
                            area.AreaKey,
                            area.DisplayLocation,
                            goblinType,
                            source,
                            DateTime.UtcNow,
                            false,
                            suppressionReason);
                        DebugManager.Session.RecordGoblinFoundRecord(suppressedRecord);
                        AppLogger.Info($"GoblinTracker: GoblinCountSuppressed areaKey={areaKey} areaCount={guardResult.AreaCount} areaLimit={guardResult.AreaLimit} reason={suppressionReason} blockListStatus=Allowed countResult=Suppressed rawLocation='{PortLogField(rawLocation)}' type='{PortLogField(goblinType)}' source='{PortLogField(source)}'");
                        return false;
                    }
                }

                GoblinFoundRecord countedRecord = new(
                    area.AreaKey,
                    area.DisplayLocation,
                    goblinType,
                    source,
                    DateTime.UtcNow,
                    true,
                    "");
                total = DebugManager.Session.RecordGoblinFound(countedRecord);
            }

            if (area.Resolved)
            {
                AppLogger.Info($"GoblinTracker: GoblinCountAccepted areaKey={areaKey} areaCount={guardResult.AreaCount} areaLimit={guardResult.AreaLimit} blockListStatus=Allowed countResult=Accepted rawLocation='{PortLogField(rawLocation)}' displayLocation='{PortLogField(displayLocation)}' type='{PortLogField(goblinType)}' source='{PortLogField(source)}' total={total}");
            }
            else
            {
                AppLogger.Info($"GoblinTracker: Count accepted rawLocation='{PortLogField(rawLocation)}' areaKey='{PortLogField(areaKey)}' displayLocation='{PortLogField(displayLocation)}' type='{PortLogField(goblinType)}' source='{PortLogField(source)}' total={total}");
            }

            if (source.Equals("ManualHotkey", StringComparison.OrdinalIgnoreCase))
            {
                string notificationArea = area.Resolved ? displayLocation : "Unknown area";
                PortPublishManualGoblinCountObservation(area, goblinType, source, guardResult);
                PortShowSplash($"Goblin counted\r\n{notificationArea}\r\nType: {goblinType}\r\nTotal: {total}", 5000);
                PortQueueGoblinEncounterDebugCapture(source, "ManualHotkey", goblinType, areaKey, notificationArea, total);
            }

            PortWriteSessionMetadata(logSuccess: false);
            PortUpdateGoblinTrackerStats();
            return true;
        }

        private void PortPublishManualGoblinCountObservation(
            GoblinAreaResolution area,
            string goblinType,
            string source,
            GoblinAreaDuplicateGuardResult guardResult)
        {
            PortPublishAcceptedGoblinCountObservation(area, goblinType, source, "ManualCountAccepted", guardResult);
        }

        private void PortPublishAcceptedGoblinCountObservation(
            GoblinAreaResolution area,
            string goblinType,
            string source,
            string duplicateState,
            GoblinAreaDuplicateGuardResult guardResult)
        {
            string observationSource = string.IsNullOrWhiteSpace(source) ? "ManualHotkey" : source.Trim();
            string observationAreaKey = area.Resolved ? area.AreaKey : "Unknown";
            string observationDisplayLocation = area.Resolved ? area.DisplayLocation : "Unknown";
            duplicateState = string.IsNullOrWhiteSpace(duplicateState) ? "CountAccepted" : duplicateState.Trim();
            GoblinObservationRecord observation = new(
                DateTime.UtcNow,
                observationSource,
                GoblinTypeNormalizer.Normalize(goblinType),
                observationAreaKey,
                observationDisplayLocation,
                true,
                "Counted",
                duplicateState,
                guardResult.AreaLimit,
                guardResult.AreaCount);

            lock (portGoblinTrackerLock)
            {
                portDisplayedGoblinObservation = observation;
                portDisplayedGoblinObservationStatus = "";
                portDisplayedGoblinObservationStickyUntilUtc = DateTime.UtcNow + PortManualGoblinCountDisplayHold;
            }

            AppLogger.Info($"GoblinTracker: LastObservationUpdated source={PortLogField(observationSource)} goblinType={PortLogField(observation.GoblinType)} areaKey={PortLogField(PortDisplayLocation(observation.AreaKey))} displayLocation={PortLogField(PortDisplayLocation(observation.DisplayLocation))} wouldCount=True reason=Counted duplicateState={PortLogField(duplicateState)} displayHoldSeconds={PortManualGoblinCountDisplayHold.TotalSeconds:0} persistUntilNextAcceptedCountOrNewGame=True");
            AppLogger.Info($"GoblinTracker: LastObservationUiRefreshRequested source={PortLogField(observationSource)} goblinType={PortLogField(observation.GoblinType)} areaKey={PortLogField(PortDisplayLocation(observation.AreaKey))} reason=Counted invokeRequired={InvokeRequired}");
            PortWriteSessionMetadata(logSuccess: false);
            PortUpdateGoblinTrackerStats();
        }

        private bool PortObserveGoblinCandidate(
            string source,
            string goblinType,
            string evidenceSignature = "",
            double evidenceConfidence = 0,
            string evidenceImagePath = "",
            string evidenceNotes = "",
            IReadOnlyList<ImageRecognitionSampleCandidate>? rankedSamples = null)
        {
            string observationSource = PortNormalizeGoblinObservationSource(source);
            PortGoblinTrackerAreaResolution areaResult = PortResolveCurrentGoblinArea(observationSource);
            if (observationSource.Equals("Journal", StringComparison.OrdinalIgnoreCase))
            {
                areaResult = PortApplyJournalEvidenceAreaFromNotes(areaResult, evidenceNotes, "ObservationCandidate");
                areaResult = PortApplyJournalMinimapAreaOverride(goblinType, areaResult, DateTime.UtcNow, "ObservationCandidate");
                areaResult = PortApplyJournalSuppressedMinimapAreaAnchor(goblinType, areaResult, DateTime.UtcNow, "ObservationCandidate");
            }

            GoblinAreaResolution area = areaResult.Area;
            goblinType = GoblinTypeNormalizer.Normalize(goblinType);
            string areaKey = PortDisplayLocation(area.AreaKey);
            string displayLocation = PortDisplayLocation(area.DisplayLocation);
            DateTime nowUtc = DateTime.UtcNow;
            string currentAreaAtDetection = PortResolvedAreaKey(portLastConfirmedLocation);
            string staleArea = PortGoblinEvidenceNoteValue(evidenceNotes, "staleArea");
            string titleResolverOverride = PortGoblinEvidenceNoteValue(evidenceNotes, "TitleResolverOverride");
            string reason = "Eligible";
            string duplicateState = "Available";
            string globalEvidenceKey = PortGoblinAutoCountGlobalEvidenceKey(evidenceSignature, observationSource, goblinType);
            string encounterSuppressionMatch = "";
            bool wouldCount = true;
            GoblinAreaDuplicateGuardResult guardResult = new(true, 0, 0);
            bool displayUpdated;
            string displaySkipKind = "";
            double manualHoldRemainingMs = 0;
            GoblinObservationRecord? preservedObservation = null;
            GoblinObservationRecord observation;
            bool allowObservationPublish = true;

            lock (portGoblinTrackerLock)
            {
                if (areaResult.Blocked)
                {
                    wouldCount = false;
                    reason = areaResult.SuppressionReason;
                    duplicateState = reason;
                }
                else if (!area.Resolved)
                {
                    wouldCount = false;
                    reason = "AreaUnresolved";
                    duplicateState = reason;
                }
                else
                {
                    guardResult = portGoblinAreaDuplicateGuard.Peek(area.AreaKey);
                    if (GoblinManualCountBlockList.IsBlocked(area.AreaKey))
                    {
                        wouldCount = false;
                        reason = "BlockedArea";
                        duplicateState = reason;
                    }
                    else if (!guardResult.Accepted)
                    {
                        wouldCount = false;
                        reason = guardResult.AreaLimit > 1 ? "AreaLimitReached" : "AreaAlreadyCounted";
                        duplicateState = reason;
                    }
                    else if (PortGoblinAutomaticCountingEnabled() &&
                        portGoblinAutoCountEncounterByGoblinType.TryGetValue(PortGoblinAutoCountEncounterKey(goblinType), out PortGoblinAutoCountEncounterState? encounterState) &&
                        PortShouldSuppressEncounterAlreadyAutoCounted(observationSource, goblinType, area.AreaKey, globalEvidenceKey, encounterState, nowUtc, out encounterSuppressionMatch))
                    {
                        if (GoblinPandemoniumMultiCountDuplicatePolicy.ShouldBypass(
                            observationSource,
                            area.AreaKey,
                            guardResult.AreaCount,
                            guardResult.AreaLimit,
                            encounterState.AreaKey,
                            encounterState.CountedUtc,
                            nowUtc,
                            globalEvidenceKey,
                            evidenceConfidence,
                            PortAutomaticGoblinMinimapCountMinimumConfidenceFor(goblinType),
                            Math.Max(0, (nowUtc - encounterState.CountedUtc).TotalSeconds),
                            portCombatRunning,
                            out string pfBypassReason,
                            out double pfElapsedSeconds))
                        {
                            encounterSuppressionMatch = $"PfMultiCountDuplicateBypass;{encounterSuppressionMatch}";
                            AppLogger.Info(
                                "GoblinTracker: PfMultiCountObservationDuplicateBypass " +
                                $"areaKey={areaKey} " +
                                $"areaCount={guardResult.AreaCount} " +
                                $"areaLimit={guardResult.AreaLimit} " +
                                $"elapsedSinceLastAcceptedSeconds={pfElapsedSeconds:0.0} " +
                                $"source={PortLogField(observationSource)} " +
                                $"goblinType={PortLogField(goblinType)} " +
                                $"evidenceHash={PortGoblinEvidenceHash(globalEvidenceKey)}");
                        }
                        else
                        {
                            wouldCount = false;
                            reason = "EncounterAlreadyAutoCounted";
                            duplicateState = reason;
                            encounterSuppressionMatch = string.IsNullOrWhiteSpace(pfBypassReason)
                                ? encounterSuppressionMatch
                                : $"{encounterSuppressionMatch};{pfBypassReason}";
                        }
                    }
                    else if (!GoblinAutoCountEvidenceReliabilityPolicy.AllowsAutomaticCount(
                        observationSource,
                        evidenceSignature,
                        out string reliabilityReason,
                        out string evidenceReliability))
                    {
                        wouldCount = false;
                        reason = reliabilityReason;
                        duplicateState = evidenceReliability;
                    }
                }

                observation = new(
                    nowUtc,
                    observationSource,
                    goblinType,
                    area.AreaKey,
                    area.DisplayLocation,
                    wouldCount,
                    reason,
                    duplicateState,
                    guardResult.AreaLimit,
                    guardResult.AreaCount,
                    evidenceConfidence);
                DebugManager.Session.RecordGoblinObservation(observation);
                if (area.Resolved)
                {
                    allowObservationPublish = wouldCount ||
                        !observationSource.Equals("Journal", StringComparison.OrdinalIgnoreCase) ||
                        portCombatRunning;
                    if (allowObservationPublish)
                    {
                        portLastGoblinObservationForManualCount = observation;
                    }
                }

                if (observationSource.Equals("Minimap", StringComparison.OrdinalIgnoreCase) &&
                    area.Resolved)
                {
                    portRecentMinimapGoblinObservationByType[goblinType] = observation;
                }

                if (!allowObservationPublish)
                {
                    displayUpdated = false;
                    displaySkipKind = "ObservationPublishGuard";
                    preservedObservation = portDisplayedGoblinObservation;
                    manualHoldRemainingMs = Math.Max(0, (portDisplayedGoblinObservationStickyUntilUtc - nowUtc).TotalMilliseconds);
                }
                else if (PortManualCountDisplayHoldActive(portDisplayedGoblinObservation, nowUtc))
                {
                    displayUpdated = false;
                    displaySkipKind = "ManualCountDisplayHold";
                    preservedObservation = portDisplayedGoblinObservation;
                    manualHoldRemainingMs = Math.Max(0, (portDisplayedGoblinObservationStickyUntilUtc - nowUtc).TotalMilliseconds);
                }
                else if (PortShouldPreserveDisplayedObservationAgainstIncoming(observation, portDisplayedGoblinObservation))
                {
                    displayUpdated = false;
                    displaySkipKind = "LastObservationPersistent";
                    preservedObservation = portDisplayedGoblinObservation;
                    manualHoldRemainingMs = Math.Max(0, (portDisplayedGoblinObservationStickyUntilUtc - nowUtc).TotalMilliseconds);
                }
                else
                {
                    displayUpdated = true;
                    portDisplayedGoblinObservation = observation;
                    portDisplayedGoblinObservationStatus = "";
                    portDisplayedGoblinObservationStickyUntilUtc = nowUtc + PortAutomaticGoblinObservationDisplayHold;
                }
            }

            AppLogger.Info($"GoblinTracker: GoblinObservationCandidate source={PortLogField(observationSource)} goblinType={PortLogField(goblinType)} areaKey={areaKey} displayLocation={displayLocation} currentAreaAtDetection={PortLogField(currentAreaAtDetection)} staleArea={PortLogField(staleArea)} titleResolverOverride={PortLogField(titleResolverOverride)} wouldCount={wouldCount} reason={reason} evidenceHash={PortGoblinEvidenceHash(globalEvidenceKey)} encounterMatch={PortLogField(encounterSuppressionMatch)}");
            AppLogger.Info($"GoblinTracker: GoblinObservationSummary source={PortLogField(observationSource)} goblinType={PortLogField(goblinType)} areaKey={areaKey} displayLocation={displayLocation} currentAreaAtDetection={PortLogField(currentAreaAtDetection)} staleArea={PortLogField(staleArea)} titleResolverOverride={PortLogField(titleResolverOverride)} wouldCount={wouldCount} reason={reason} duplicateState={duplicateState} areaLimit={guardResult.AreaLimit} currentAreaCount={guardResult.AreaCount} evidenceConfidence={evidenceConfidence:0.000} evidenceHash={PortGoblinEvidenceHash(globalEvidenceKey)} encounterMatch={PortLogField(encounterSuppressionMatch)}");
            PortWriteGoblinTrackerJsonEvent(
                "GoblinObservationCandidate",
                new Dictionary<string, object?>
                {
                    ["source"] = observationSource,
                    ["goblinType"] = goblinType,
                    ["areaKey"] = areaKey,
                    ["displayLocation"] = displayLocation,
                    ["currentAreaAtDetection"] = currentAreaAtDetection,
                    ["staleArea"] = staleArea,
                    ["titleResolverOverride"] = titleResolverOverride,
                    ["wouldCount"] = wouldCount,
                    ["reason"] = reason,
                    ["duplicateState"] = duplicateState,
                    ["areaLimit"] = guardResult.AreaLimit,
                    ["currentAreaCount"] = guardResult.AreaCount,
                    ["evidenceConfidence"] = evidenceConfidence,
                    ["evidenceHash"] = PortGoblinEvidenceHash(globalEvidenceKey),
                    ["encounterMatch"] = encounterSuppressionMatch,
                });
            if (displayUpdated)
            {
                AppLogger.Info($"GoblinTracker: LastObservationUpdated source={PortLogField(observationSource)} goblinType={PortLogField(goblinType)} areaKey={areaKey} displayLocation={displayLocation} wouldCount={wouldCount} reason={reason} duplicateState={duplicateState} displayHoldSeconds={PortAutomaticGoblinObservationDisplayHold.TotalSeconds:0}");
            }
            else
            {
                string eventName = displaySkipKind.Equals("ManualCountDisplayHold", StringComparison.OrdinalIgnoreCase)
                    ? "LastObservationUpdateSkippedDuringManualHold"
                    : displaySkipKind.Equals("ObservationPublishGuard", StringComparison.OrdinalIgnoreCase)
                        ? "LastObservationUpdateSkippedPublishGuard"
                        : "LastObservationUpdateSkippedPreserved";
                AppLogger.Info($"GoblinTracker: {eventName} incomingSource={PortLogField(observationSource)} incomingGoblinType={PortLogField(goblinType)} incomingAreaKey={areaKey} incomingReason={PortLogField(reason)} incomingDuplicateState={PortLogField(duplicateState)} preserveKind={PortLogField(displaySkipKind)} allowObservationPublish={allowObservationPublish} combatActive={portCombatRunning} combatStopping={portCombatStopping} automationRunning={isAutomationRunning} diabloRunning={IsDiabloRunning()} diabloActive={PortDiabloIsActive()} preservedSource={PortLogField(preservedObservation?.Source ?? "")} preservedGoblinType={PortLogField(preservedObservation?.GoblinType ?? "")} preservedAreaKey={PortLogField(preservedObservation?.AreaKey ?? "")} preservedReason={PortLogField(preservedObservation?.Reason ?? "")} remainingMs={manualHoldRemainingMs:0}");
            }
            PortTryRecordAutomaticGoblinCount(observation, area, evidenceSignature, evidenceImagePath, rankedSamples, evidenceNotes);
            PortWriteSessionMetadata(logSuccess: false);
            PortUpdateGoblinTrackerStats();
            return wouldCount;
        }

        private PortGoblinTrackerAreaResolution PortApplyJournalEvidenceAreaFromNotes(
            PortGoblinTrackerAreaResolution areaResult,
            string evidenceNotes,
            string reason)
        {
            string journalArea = PortGoblinEvidenceNoteValue(evidenceNotes, "JournalArea");
            if (string.IsNullOrWhiteSpace(journalArea))
            {
                return areaResult;
            }

            GoblinAreaResolution resolvedJournalArea = GoblinAreaResolver.Resolve(journalArea);
            if (!resolvedJournalArea.Resolved)
            {
                AppLogger.Info(
                    "GoblinTracker: JournalEvidenceAreaIgnored " +
                    $"reason={PortLogField(reason)} " +
                    $"journalArea={PortLogField(PortDisplayLocation(journalArea))} " +
                    "ignoredReason=Unresolved");
                return areaResult;
            }

            if (string.Equals(resolvedJournalArea.AreaKey, areaResult.Area.AreaKey, StringComparison.OrdinalIgnoreCase))
            {
                return areaResult;
            }

            AppLogger.Info(
                "GoblinTracker: JournalEvidenceAreaApplied " +
                $"reason={PortLogField(reason)} " +
                $"originalAreaKey={PortLogField(PortDisplayLocation(areaResult.Area.AreaKey))} " +
                $"journalAreaKey={PortLogField(PortDisplayLocation(resolvedJournalArea.AreaKey))} " +
                $"journalDisplayLocation={PortLogField(PortDisplayLocation(resolvedJournalArea.DisplayLocation))}");

            return areaResult with
            {
                Area = resolvedJournalArea,
                AmbiguityGroup = string.IsNullOrWhiteSpace(areaResult.AmbiguityGroup)
                    ? "JournalEvidenceArea"
                    : areaResult.AmbiguityGroup,
                DisambiguationReason = "JournalEvidenceArea",
            };
        }

        private PortGoblinTrackerAreaResolution PortApplyJournalMinimapAreaOverride(
            string goblinType,
            PortGoblinTrackerAreaResolution areaResult,
            DateTime nowUtc,
            string reason)
        {
            GoblinObservationRecord? recentMinimapObservation = null;
            string normalizedGoblinType = GoblinTypeNormalizer.Normalize(goblinType);
            lock (portGoblinTrackerLock)
            {
                portRecentMinimapGoblinObservationByType.TryGetValue(normalizedGoblinType, out recentMinimapObservation);
            }

            string routeContext = PortGoblinTrackerAreaRouteContext();
            GoblinJournalAreaOverrideDecision overrideDecision = GoblinJournalAreaOverridePolicy.TryUseRecentMinimapChannelArea(
                areaResult.Area,
                normalizedGoblinType,
                recentMinimapObservation,
                routeContext,
                nowUtc,
                PortAutomaticGoblinSourceVariantSuppressWindow);

            if (!overrideDecision.Overridden)
            {
                return areaResult;
            }

            AppLogger.Info(
                "GoblinTracker: JournalAreaOverrideApplied " +
                $"reason={PortLogField(reason)} " +
                $"goblinType={PortLogField(normalizedGoblinType)} " +
                $"originalAreaKey={PortLogField(PortDisplayLocation(areaResult.Area.AreaKey))} " +
                $"overrideAreaKey={PortLogField(PortDisplayLocation(overrideDecision.Area.AreaKey))} " +
                $"overrideReason={PortLogField(overrideDecision.Reason)} " +
                $"recentMinimapSource={PortLogField(recentMinimapObservation?.Source ?? "")} " +
                $"recentMinimapAreaKey={PortLogField(PortDisplayLocation(recentMinimapObservation?.AreaKey ?? ""))} " +
                $"recentMinimapAgeSeconds={overrideDecision.RecentObservationAgeSeconds:0.0} " +
                $"maxAgeSeconds={PortAutomaticGoblinSourceVariantSuppressWindow.TotalSeconds:0} " +
                $"routeContext={PortLogField(PortDisplayLocation(routeContext))}");

            return areaResult with
            {
                Area = overrideDecision.Area,
                AmbiguityGroup = string.IsNullOrWhiteSpace(areaResult.AmbiguityGroup)
                    ? "ChannelVsPandemonium"
                    : areaResult.AmbiguityGroup,
                DisambiguationReason = overrideDecision.Reason,
            };
        }

        private PortGoblinTrackerAreaResolution PortApplyJournalSuppressedMinimapAreaAnchor(
            string goblinType,
            PortGoblinTrackerAreaResolution areaResult,
            DateTime nowUtc,
            string reason)
        {
            if (!PortTryGetSuppressedMinimapAreaAnchor(
                goblinType,
                nowUtc,
                out GoblinMinimapAreaAnchorState anchor,
                out double anchorAgeSeconds))
            {
                return areaResult;
            }

            if (!areaResult.Area.Resolved ||
                string.Equals(
                    GoblinAreaResolver.NormalizedKey(areaResult.Area.AreaKey),
                    GoblinAreaResolver.NormalizedKey(anchor.AreaKey),
                    StringComparison.OrdinalIgnoreCase))
            {
                return areaResult;
            }

            GoblinAreaResolution anchorArea = GoblinAreaResolver.Resolve(anchor.AreaKey);
            if (!anchorArea.Resolved || GoblinManualCountBlockList.IsBlocked(anchorArea.AreaKey))
            {
                return areaResult;
            }

            AppLogger.Info(
                "GoblinTracker: JournalEvidenceAreaAnchoredToRecentMinimap " +
                $"reason={PortLogField(reason)} " +
                $"goblinType={PortLogField(GoblinTypeNormalizer.Normalize(goblinType))} " +
                $"originalAreaKey={PortLogField(PortDisplayLocation(areaResult.Area.AreaKey))} " +
                $"acceptedArea={PortLogField(PortDisplayLocation(anchorArea.AreaKey))} " +
                $"firstSeenArea={PortLogField(PortDisplayLocation(anchor.AreaKey))} " +
                $"currentAreaAtDetection={PortLogField(PortDisplayLocation(anchor.CurrentAreaAtDetection))} " +
                $"titleResolverOverride=BlockedByFreshMinimapAnchor " +
                $"anchorAgeSeconds={anchorAgeSeconds:0.0} " +
                $"anchorConfidence={anchor.EvidenceConfidence:0.000} " +
                $"anchorSuppressionReason={PortLogField(anchor.SuppressionReason)} " +
                $"anchorEvidenceHash={anchor.EvidenceHash} " +
                $"maxAgeSeconds={PortAutomaticGoblinSuppressedMinimapAreaAnchorWindow.TotalSeconds:0}");
            PortWriteGoblinTrackerJsonEvent(
                "JournalEvidenceAreaAnchoredToRecentMinimap",
                new Dictionary<string, object?>
                {
                    ["reason"] = reason,
                    ["goblinType"] = GoblinTypeNormalizer.Normalize(goblinType),
                    ["originalAreaKey"] = PortDisplayLocation(areaResult.Area.AreaKey),
                    ["firstSeenArea"] = PortDisplayLocation(anchor.AreaKey),
                    ["acceptedArea"] = PortDisplayLocation(anchorArea.AreaKey),
                    ["notificationDisplayArea"] = PortDisplayLocation(anchorArea.DisplayLocation),
                    ["currentAreaAtDetection"] = PortDisplayLocation(anchor.CurrentAreaAtDetection),
                    ["titleResolverOverride"] = "BlockedByFreshMinimapAnchor",
                    ["anchorAgeSeconds"] = anchorAgeSeconds,
                    ["anchorConfidence"] = anchor.EvidenceConfidence,
                    ["anchorSuppressionReason"] = anchor.SuppressionReason,
                    ["anchorEvidenceHash"] = anchor.EvidenceHash,
                    ["maxAgeSeconds"] = PortAutomaticGoblinSuppressedMinimapAreaAnchorWindow.TotalSeconds,
                });

            return areaResult with
            {
                Area = anchorArea,
                AmbiguityGroup = string.IsNullOrWhiteSpace(areaResult.AmbiguityGroup)
                    ? "SuppressedMinimapAreaAnchor"
                    : areaResult.AmbiguityGroup,
                DisambiguationReason = "RecentSuppressedMinimapAreaAnchor",
            };
        }

        private void PortRememberSuppressedMinimapAreaAnchor(
            GoblinObservationRecord observation,
            GoblinAreaResolution area,
            string currentAreaAtDetection,
            string suppressionReason,
            DateTime nowUtc,
            string evidenceHash)
        {
            if (!observation.Source.Equals("Minimap", StringComparison.OrdinalIgnoreCase) ||
                !area.Resolved ||
                string.IsNullOrWhiteSpace(observation.GoblinType) ||
                string.IsNullOrWhiteSpace(area.AreaKey))
            {
                return;
            }

            string normalizedGoblinType = GoblinTypeNormalizer.Normalize(observation.GoblinType);
            GoblinMinimapAreaAnchorState anchor = new(
                normalizedGoblinType,
                area.AreaKey,
                area.DisplayLocation,
                nowUtc,
                observation.EvidenceConfidence,
                suppressionReason,
                currentAreaAtDetection,
                evidenceHash);
            lock (portGoblinTrackerLock)
            {
                portSuppressedMinimapAreaAnchorByType[normalizedGoblinType] = anchor;
            }

            AppLogger.Info(
                "GoblinTracker: SuppressedMinimapAreaAnchorRemembered " +
                $"source=Minimap " +
                $"goblinType={PortLogField(normalizedGoblinType)} " +
                $"firstSeenArea={PortLogField(PortDisplayLocation(area.AreaKey))} " +
                $"currentAreaAtDetection={PortLogField(PortDisplayLocation(currentAreaAtDetection))} " +
                $"acceptedArea=None " +
                $"notificationDisplayArea=None " +
                $"titleResolverOverride=BlockedUntilFreshJournal " +
                $"reason={PortLogField(suppressionReason)} " +
                $"evidenceConfidence={observation.EvidenceConfidence:0.000} " +
                $"evidenceHash={evidenceHash} " +
                $"maxAgeSeconds={PortAutomaticGoblinSuppressedMinimapAreaAnchorWindow.TotalSeconds:0}");
            PortWriteGoblinTrackerJsonEvent(
                "SuppressedMinimapAreaAnchorRemembered",
                new Dictionary<string, object?>
                {
                    ["source"] = "Minimap",
                    ["goblinType"] = normalizedGoblinType,
                    ["firstSeenArea"] = PortDisplayLocation(area.AreaKey),
                    ["currentAreaAtDetection"] = PortDisplayLocation(currentAreaAtDetection),
                    ["acceptedArea"] = null,
                    ["notificationDisplayArea"] = null,
                    ["titleResolverOverride"] = "BlockedUntilFreshJournal",
                    ["reason"] = suppressionReason,
                    ["evidenceConfidence"] = observation.EvidenceConfidence,
                    ["evidenceHash"] = evidenceHash,
                    ["maxAgeSeconds"] = PortAutomaticGoblinSuppressedMinimapAreaAnchorWindow.TotalSeconds,
                });
        }

        private bool PortTryGetSuppressedMinimapAreaAnchor(
            string goblinType,
            DateTime nowUtc,
            out GoblinMinimapAreaAnchorState anchor,
            out double anchorAgeSeconds)
        {
            anchor = default!;
            anchorAgeSeconds = -1;
            string normalizedGoblinType = GoblinTypeNormalizer.Normalize(goblinType);
            if (string.IsNullOrWhiteSpace(normalizedGoblinType))
            {
                return false;
            }

            lock (portGoblinTrackerLock)
            {
                if (!portSuppressedMinimapAreaAnchorByType.TryGetValue(normalizedGoblinType, out GoblinMinimapAreaAnchorState? candidate))
                {
                    return false;
                }

                anchor = candidate;
            }

            if (anchor.EvidenceConfidence < PortAutomaticGoblinMinimapCountMinimumConfidenceFor(normalizedGoblinType))
            {
                return false;
            }

            anchorAgeSeconds = Math.Max(0, (nowUtc - anchor.SeenUtc).TotalSeconds);
            return anchorAgeSeconds <= PortAutomaticGoblinSuppressedMinimapAreaAnchorWindow.TotalSeconds;
        }

        private bool PortTryGetRecentMinimapJournalConfirmation(
            string goblinType,
            string areaKey,
            DateTime nowUtc,
            out GoblinObservationRecord recentMinimapObservation,
            out double recentMinimapAgeSeconds)
        {
            recentMinimapObservation = default!;
            recentMinimapAgeSeconds = -1;
            string normalizedGoblinType = GoblinTypeNormalizer.Normalize(goblinType);
            if (string.IsNullOrWhiteSpace(normalizedGoblinType) ||
                string.IsNullOrWhiteSpace(areaKey))
            {
                return false;
            }

            lock (portGoblinTrackerLock)
            {
                if (!portRecentMinimapGoblinObservationByType.TryGetValue(normalizedGoblinType, out GoblinObservationRecord? candidate))
                {
                    return false;
                }

                recentMinimapObservation = candidate;
            }

            if (!recentMinimapObservation.Source.Equals("Minimap", StringComparison.OrdinalIgnoreCase) ||
                !GoblinTypeNormalizer.Normalize(recentMinimapObservation.GoblinType).Equals(normalizedGoblinType, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(GoblinAreaResolver.NormalizedKey(recentMinimapObservation.AreaKey), GoblinAreaResolver.NormalizedKey(areaKey), StringComparison.OrdinalIgnoreCase) ||
                recentMinimapObservation.EvidenceConfidence < PortAutomaticGoblinRecentMinimapJournalConfirmationMinimumConfidence)
            {
                return false;
            }

            recentMinimapAgeSeconds = Math.Max(0, (nowUtc - recentMinimapObservation.TimestampUtc).TotalSeconds);
            return recentMinimapAgeSeconds <= PortAutomaticGoblinRecentMinimapJournalConfirmationWindow.TotalSeconds;
        }

        private static string PortNormalizeGoblinObservationSource(string source)
        {
            if (string.Equals(source, "JournalCandidate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source, "Journal", StringComparison.OrdinalIgnoreCase))
            {
                return "Journal";
            }

            if (string.Equals(source, "MinimapCandidate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source, "Minimap", StringComparison.OrdinalIgnoreCase))
            {
                return "Minimap";
            }

            return string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
        }

        private PortGoblinTrackerAreaResolution PortResolveCurrentGoblinArea(string source)
        {
            string rawLocation = "";
            string bestName = "";
            double bestConfidence = 0;
            string secondName = "";
            double secondConfidence = 0;
            double delta = 0;
            string ambiguityGroup = "";
            string disambiguationReason = "";
            try
            {
                if (IsDiabloRunning())
                {
                    if (PortShouldUseGoblinTrackerDetailedAreaResolution(source))
                    {
                        string flow = source.Equals("ManualHotkey", StringComparison.OrdinalIgnoreCase)
                            ? "goblin tracker manual current-location detection"
                            : $"goblin tracker {PortNormalizeGoblinObservationSource(source).ToLowerInvariant()} current-location detection";
                        PortLocationDetectionResult detection = PortDetectCurrentLocationFromTemplatesDetailed(
                            portCurrentLocationTemplates,
                            flow,
                            logPerf: true,
                            PortCurrentLocationConfidence);
                        rawLocation = detection.Detected;
                        bestName = detection.BestName;
                        bestConfidence = detection.BestConfidence;
                        secondName = detection.SecondName;
                        secondConfidence = detection.SecondConfidence;
                        string routeContext = PortGoblinTrackerAreaRouteContext();
                        GoblinAreaDetectionDisambiguationResult disambiguation = GoblinAreaDetectionDisambiguator.Disambiguate(
                            detection.BestName,
                            detection.BestConfidence,
                            detection.SecondName,
                            detection.SecondConfidence,
                            routeContext);
                        if (disambiguation.Ambiguous)
                        {
                            delta = disambiguation.Delta;
                            ambiguityGroup = disambiguation.AmbiguityGroup;
                            disambiguationReason = disambiguation.Reason;
                            rawLocation = disambiguation.SelectedLocation;
                            AppLogger.Info($"GoblinTracker: AreaDetectionAmbiguous source={PortLogField(source)} best='{PortLogField(detection.BestName)}' bestConfidence={detection.BestConfidence:0.000} second='{PortLogField(detection.SecondName)}' secondConfidence={detection.SecondConfidence:0.000} delta={disambiguation.Delta:0.000} ambiguityGroup={PortLogField(disambiguation.AmbiguityGroup)} selected='{PortLogField(disambiguation.SelectedLocation)}' reason={PortLogField(disambiguation.Reason)} routeContext='{PortLogField(routeContext)}' currentButton='{PortLogField(PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey)))}' nextButton='{PortLogField(PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey)))}'");
                            if (disambiguation.Blocked)
                            {
                                return new PortGoblinTrackerAreaResolution(
                                    new GoblinAreaResolution("", "", ""),
                                    true,
                                    "AmbiguousAreaDetection",
                                    bestName,
                                    bestConfidence,
                                    secondName,
                                    secondConfidence,
                                    delta,
                                    ambiguityGroup,
                                    disambiguationReason);
                            }
                        }
                    }
                    else
                    {
                        rawLocation = PortDetectCurrentLocation();
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"GoblinTracker: Current area scan failed source={PortLogField(source)}", ex);
            }

            if (string.IsNullOrWhiteSpace(rawLocation))
            {
                rawLocation = portLastConfirmedLocation;
            }

            GoblinAreaResolution resolution = GoblinAreaResolver.Resolve(rawLocation);
            AppLogger.Info($"GoblinTracker: Area resolved rawLocation='{PortLogField(PortDisplayLocation(resolution.RawLocation))}' areaKey='{PortLogField(PortDisplayLocation(resolution.AreaKey))}' source='{PortLogField(source)}' resolved={resolution.Resolved}");
            return new PortGoblinTrackerAreaResolution(
                resolution,
                false,
                "",
                bestName,
                bestConfidence,
                secondName,
                secondConfidence,
                delta,
                ambiguityGroup,
                disambiguationReason);
        }

        private static bool PortShouldUseGoblinTrackerDetailedAreaResolution(string source)
        {
            return source.Equals("ManualHotkey", StringComparison.OrdinalIgnoreCase) ||
                source.Equals("Journal", StringComparison.OrdinalIgnoreCase) ||
                source.Equals("Minimap", StringComparison.OrdinalIgnoreCase) ||
                source.Equals("JournalCandidate", StringComparison.OrdinalIgnoreCase) ||
                source.Equals("MinimapCandidate", StringComparison.OrdinalIgnoreCase);
        }

        private string PortResolveGoblinTypeForManualCount(string source, string goblinType, string areaKey)
        {
            if (!source.Equals("ManualHotkey", StringComparison.OrdinalIgnoreCase))
            {
                return goblinType;
            }

            DateTime nowUtc = DateTime.UtcNow;
            GoblinObservationRecord? observation = portLastGoblinObservationForManualCount;
            string resolvedType = GoblinObservationTypeReuse.ResolveForManualCount(
                goblinType,
                areaKey,
                observation,
                nowUtc,
                PortManualGoblinObservationTypeReuseWindow);

            if (!string.Equals(resolvedType, goblinType, StringComparison.OrdinalIgnoreCase))
            {
                double ageSeconds = observation == null ? -1 : Math.Max(0, (nowUtc - observation.TimestampUtc).TotalSeconds);
                AppLogger.Info($"GoblinTracker: Manual count reused recent observation type goblinType={PortLogField(resolvedType)} areaKey={PortLogField(PortDisplayLocation(areaKey))} observationSource={PortLogField(observation?.Source ?? "")} observationAgeSeconds={ageSeconds:0.0} maxAgeSeconds={PortManualGoblinObservationTypeReuseWindow.TotalSeconds:0}");
            }

            return resolvedType;
        }

        private bool PortCanReuseRecentManualObservationType(
            string areaKey,
            string goblinType,
            out string resolvedType,
            out GoblinObservationRecord? observation)
        {
            DateTime nowUtc = DateTime.UtcNow;
            lock (portGoblinTrackerLock)
            {
                observation = portLastGoblinObservationForManualCount;
            }

            resolvedType = GoblinObservationTypeReuse.ResolveForManualCount(
                goblinType,
                areaKey,
                observation,
                nowUtc,
                PortManualGoblinObservationTypeReuseWindow);
            return !string.Equals(resolvedType, "Unknown", StringComparison.OrdinalIgnoreCase);
        }

        private bool PortHasFreshManualCountObservation(string areaKey)
        {
            DateTime nowUtc = DateTime.UtcNow;
            GoblinObservationRecord? manualObservation;
            GoblinObservationRecord? displayedObservation;
            lock (portGoblinTrackerLock)
            {
                manualObservation = portLastGoblinObservationForManualCount;
                displayedObservation = portDisplayedGoblinObservation;
            }

            return PortObservationIsFreshEligibleForManualCount(manualObservation, areaKey, nowUtc) ||
                PortObservationIsFreshEligibleForManualCount(displayedObservation, areaKey, nowUtc);
        }

        private static bool PortObservationIsFreshEligibleForManualCount(
            GoblinObservationRecord? observation,
            string areaKey,
            DateTime nowUtc)
        {
            if (observation == null ||
                !observation.WouldCount ||
                !string.Equals(observation.Reason, "Eligible", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(areaKey) ||
                string.IsNullOrWhiteSpace(observation.AreaKey) ||
                nowUtc - observation.TimestampUtc > PortManualGoblinObservationTypeReuseWindow)
            {
                return false;
            }

            return string.Equals(
                GoblinAreaResolver.NormalizedKey(areaKey),
                GoblinAreaResolver.NormalizedKey(observation.AreaKey),
                StringComparison.OrdinalIgnoreCase);
        }

        private string PortGoblinTrackerAreaRouteContext()
        {
            if (!string.IsNullOrWhiteSpace(portLastConfirmedLocation))
            {
                return portLastConfirmedLocation;
            }

            return PortGetButtonLocationForDetectedLocation(portLastConfirmedLocation);
        }

        private void PortResetGoblinAreaDuplicateGuard(string reason)
        {
            int cleared;
            lock (portGoblinTrackerLock)
            {
                cleared = portGoblinAreaDuplicateGuard.Reset();
            }

            AppLogger.Info($"GoblinTracker: Area duplicate guard reset reason='{PortLogField(reason)}' clearedAreaKeys={cleared}");
        }

        private void PortUpdateGoblinTrackerStats()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(PortUpdateGoblinTrackerStats));
                return;
            }

            DiagnosticsSessionSnapshot snapshot = DebugManager.Session.Snapshot(DateTime.Now);
            lblGoblinCount.Text = $"Goblins: {snapshot.GoblinCount}";
            lblGoblinGph.Text = $"GPH: {snapshot.GoblinsPerHour:0.00}";
            lblGoblinActiveTime.Text = $"Active Time: {snapshot.GoblinActiveCombatTime:hh\\:mm\\:ss}";
            lblGoblinEvidenceLast.Text = $"Last Evidence: {(snapshot.GoblinEvidenceEventCount > 0 ? snapshot.LastGoblinEvidenceType.ToString() : "None")}";
            lblGoblinEvidenceType.Text = $"Evidence Type: {(snapshot.GoblinEvidenceEventCount > 0 ? snapshot.LastGoblinEvidenceType.ToString() : "None")}";
            lblGoblinEvidenceConfidence.Text = $"Evidence Confidence: {snapshot.LastGoblinEvidenceConfidence:0.00}";
            lblGoblinEvidenceTime.Text = $"Evidence Time: {(snapshot.LastGoblinEvidenceTime.HasValue ? snapshot.LastGoblinEvidenceTime.Value.ToString("HH:mm:ss") : "--")}";
            GoblinObservationRecord? displayedObservation;
            string displayedObservationStatus;
            lock (portGoblinTrackerLock)
            {
                displayedObservation = portDisplayedGoblinObservation;
                displayedObservationStatus = portDisplayedGoblinObservationStatus;
            }

            string observationLabel = PortGoblinObservationLabel(displayedObservation, displayedObservationStatus);
            lblGoblinObservation.Text = observationLabel;
            lblGoblinCount.Refresh();
            lblGoblinGph.Refresh();
            lblGoblinActiveTime.Refresh();
            lblGoblinObservation.Refresh();
            string activeTimeText = snapshot.GoblinActiveCombatTime.ToString(@"hh\:mm\:ss");
            if (snapshot.GoblinCount != portLastLoggedGoblinStatsUiCount ||
                !string.Equals(activeTimeText, portLastLoggedGoblinStatsUiActiveTime, StringComparison.Ordinal) ||
                !string.Equals(observationLabel, portLastLoggedGoblinStatsUiObservation, StringComparison.Ordinal))
            {
                portLastLoggedGoblinStatsUiCount = snapshot.GoblinCount;
                portLastLoggedGoblinStatsUiActiveTime = activeTimeText;
                portLastLoggedGoblinStatsUiObservation = observationLabel;
                AppLogger.Info($"GoblinTracker: StatsUiRefreshed goblins={snapshot.GoblinCount} gph={snapshot.GoblinsPerHour:0.00} activeTime={activeTimeText}");
            }
        }

        private void PortMarkGoblinObservationNoCurrent(string reason)
        {
            GoblinObservationRecord? previousObservation;
            string previousStatus;
            DateTime nowUtc = DateTime.UtcNow;
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "NoCandidate" : reason.Trim();
            string currentAreaKey = PortCurrentDisplayedObservationAreaKey();
            bool areaChanged = false;
            lock (portGoblinTrackerLock)
            {
                previousObservation = portDisplayedGoblinObservation;
                previousStatus = portDisplayedGoblinObservationStatus;
                if (PortDisplayedObservationIsAcceptedCount(previousObservation))
                {
                    bool acceptedDisplayAreaChanged = !string.IsNullOrWhiteSpace(currentAreaKey) &&
                        !PortDisplayedObservationMatchesCurrentArea(previousObservation!, currentAreaKey);
                    double remainingMs = Math.Max(0, (portDisplayedGoblinObservationStickyUntilUtc - nowUtc).TotalMilliseconds);
                    AppLogger.Info($"GoblinTracker: LastObservationClearSkipped reason={PortLogField(normalizedReason)} preserveKind=AcceptedCountPersistent preservedSource={PortLogField(previousObservation?.Source ?? "")} preservedGoblinType={PortLogField(previousObservation?.GoblinType ?? "")} preservedAreaKey={PortLogField(previousObservation?.AreaKey ?? "")} preservedReason={PortLogField(previousObservation?.Reason ?? "")} currentAreaKey={PortLogField(currentAreaKey)} acceptedDisplayAreaChanged={acceptedDisplayAreaChanged} remainingMs={remainingMs:0} persistUntilNextAcceptedCountOrNewGame=True");
                    return;
                }

                areaChanged = areaChanged ||
                    normalizedReason.Equals("AreaChanged", StringComparison.OrdinalIgnoreCase) ||
                    previousObservation != null &&
                    !string.IsNullOrWhiteSpace(currentAreaKey) &&
                    !PortDisplayedObservationMatchesCurrentArea(previousObservation, currentAreaKey);
                string effectiveReason = areaChanged ? "AreaChanged" : normalizedReason;
                if (!areaChanged &&
                    PortShouldPreserveDisplayedGoblinObservation(previousObservation, normalizedReason, nowUtc, out string preserveKind))
                {
                    double remainingMs = Math.Max(0, (portDisplayedGoblinObservationStickyUntilUtc - nowUtc).TotalMilliseconds);
                    AppLogger.Info($"GoblinTracker: LastObservationClearSkipped reason={PortLogField(normalizedReason)} preserveKind={PortLogField(preserveKind)} preservedSource={PortLogField(previousObservation?.Source ?? "")} preservedGoblinType={PortLogField(previousObservation?.GoblinType ?? "")} preservedAreaKey={PortLogField(previousObservation?.AreaKey ?? "")} preservedReason={PortLogField(previousObservation?.Reason ?? "")} remainingMs={remainingMs:0}");
                    return;
                }

                portDisplayedGoblinObservation = null;
                portDisplayedGoblinObservationStatus = effectiveReason;
                portDisplayedGoblinObservationStickyUntilUtc = DateTime.MinValue;
                normalizedReason = effectiveReason;
            }

            AppLogger.Info($"GoblinTracker: LastObservationCleared reason={PortLogField(normalizedReason)} previousGoblinType={PortLogField(previousObservation?.GoblinType ?? "")} previousAreaKey={PortLogField(previousObservation?.AreaKey ?? "")} previousSource={PortLogField(previousObservation?.Source ?? "")} previousStatus={PortLogField(previousStatus)} currentAreaKey={PortLogField(currentAreaKey)} areaChanged={areaChanged}");
            PortUpdateGoblinTrackerStats();
        }

        private void PortClearDisplayedGoblinObservationAfterConfirmedAreaChange(
            string previousConfirmedLocation,
            string currentConfirmedLocation,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(currentConfirmedLocation) ||
                string.Equals(
                    GoblinAreaResolver.NormalizedKey(previousConfirmedLocation),
                    GoblinAreaResolver.NormalizedKey(currentConfirmedLocation),
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            AppLogger.Info(
                $"GoblinTracker: ConfirmedAreaChanged previous={PortLogField(PortDisplayLocation(previousConfirmedLocation))} " +
                $"current={PortLogField(PortDisplayLocation(currentConfirmedLocation))} reason={PortLogField(reason)}");
            PortMarkGoblinObservationNoCurrent(reason);
        }

        private string PortCurrentDisplayedObservationAreaKey()
        {
            string currentArea = portLastConfirmedLocation;
            return string.IsNullOrWhiteSpace(currentArea)
                ? ""
                : PortLocationKey(currentArea);
        }

        private bool PortDisplayedObservationMatchesCurrentArea(GoblinObservationRecord observation, string currentAreaKey)
        {
            if (string.IsNullOrWhiteSpace(currentAreaKey))
            {
                return true;
            }

            string observationAreaKey = PortLocationKey(observation.AreaKey);
            return !string.IsNullOrWhiteSpace(observationAreaKey) &&
                observationAreaKey.Equals(currentAreaKey, StringComparison.OrdinalIgnoreCase);
        }

        private bool PortShouldPreserveDisplayedManualCountObservation(
            GoblinObservationRecord? observation,
            string reason,
            DateTime nowUtc)
        {
            if (!PortManualCountDisplayHoldActive(observation, nowUtc))
            {
                return false;
            }

            return reason.Equals("No current observation", StringComparison.OrdinalIgnoreCase) ||
                reason.Equals("NoCandidate", StringComparison.OrdinalIgnoreCase);
        }

        private bool PortShouldPreserveDisplayedObservationAgainstIncoming(
            GoblinObservationRecord incomingObservation,
            GoblinObservationRecord? displayedObservation)
        {
            if (displayedObservation == null)
            {
                return false;
            }

            if (PortDisplayedObservationIsAcceptedCount(displayedObservation))
            {
                return !PortFreshObservationShouldReplaceAcceptedDisplay(incomingObservation, displayedObservation);
            }

            if (incomingObservation.WouldCount)
            {
                return false;
            }

            return incomingObservation.Reason.Equals("EncounterAlreadyAutoCounted", StringComparison.OrdinalIgnoreCase) ||
                incomingObservation.Reason.Equals("AreaAlreadyCounted", StringComparison.OrdinalIgnoreCase) ||
                incomingObservation.Reason.Equals("AreaLimitReached", StringComparison.OrdinalIgnoreCase) ||
                incomingObservation.Reason.Equals("EvidenceAlreadyAutoCounted", StringComparison.OrdinalIgnoreCase) ||
                incomingObservation.Reason.Equals("StaleEvidence", StringComparison.OrdinalIgnoreCase);
        }

        private bool PortFreshObservationShouldReplaceAcceptedDisplay(
            GoblinObservationRecord incomingObservation,
            GoblinObservationRecord displayedObservation)
        {
            bool freshAcceptedCandidate = incomingObservation.WouldCount &&
                incomingObservation.Reason.Equals("Counted", StringComparison.OrdinalIgnoreCase);
            if (!freshAcceptedCandidate)
            {
                return false;
            }

            if (incomingObservation.AreaKey.Equals(displayedObservation.AreaKey, StringComparison.OrdinalIgnoreCase) &&
                incomingObservation.GoblinType.Equals(displayedObservation.GoblinType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            AppLogger.Info(
                "GoblinTracker: LastObservationFreshEvidenceReplacesAcceptedDisplay " +
                $"incomingGoblinType={PortLogField(incomingObservation.GoblinType)} " +
                $"incomingAreaKey={PortLogField(incomingObservation.AreaKey)} " +
                $"incomingReason={PortLogField(incomingObservation.Reason)} " +
                $"incomingWouldCount={incomingObservation.WouldCount} " +
                $"preservedGoblinType={PortLogField(displayedObservation.GoblinType)} " +
                $"preservedAreaKey={PortLogField(displayedObservation.AreaKey)} " +
                $"combatActive={portCombatRunning}");
            return true;
        }

        private bool PortShouldPreserveDisplayedGoblinObservation(
            GoblinObservationRecord? observation,
            string reason,
            DateTime nowUtc,
            out string preserveKind)
        {
            preserveKind = "";
            if (PortShouldPreserveDisplayedManualCountObservation(observation, reason, nowUtc))
            {
                preserveKind = "ManualCountDisplayHold";
                return true;
            }

            if (observation != null &&
                (reason.Equals("No current observation", StringComparison.OrdinalIgnoreCase) ||
                reason.Equals("NoCandidate", StringComparison.OrdinalIgnoreCase) ||
                reason.Equals("MissingTemplate", StringComparison.OrdinalIgnoreCase)))
            {
                preserveKind = PortAutomaticObservationDisplayHoldActive(observation, nowUtc)
                    ? "ObservationDisplayHold"
                    : "LastObservationPersistent";
                return true;
            }

            return false;
        }

        private static bool PortDisplayedObservationIsAcceptedCount(GoblinObservationRecord? observation)
        {
            return observation != null &&
                observation.WouldCount &&
                observation.Reason.Equals("Counted", StringComparison.OrdinalIgnoreCase);
        }

        private bool PortManualCountDisplayHoldActive(GoblinObservationRecord? observation, DateTime nowUtc)
        {
            return observation != null &&
                nowUtc < portDisplayedGoblinObservationStickyUntilUtc &&
                string.Equals(observation.Source, "ManualHotkey", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(observation.Reason, "Counted", StringComparison.OrdinalIgnoreCase);
        }

        private bool PortAutomaticObservationDisplayHoldActive(GoblinObservationRecord? observation, DateTime nowUtc)
        {
            return observation != null &&
                nowUtc < portDisplayedGoblinObservationStickyUntilUtc &&
                !string.Equals(observation.Source, "ManualHotkey", StringComparison.OrdinalIgnoreCase);
        }

        private static string PortGoblinObservationLabel(GoblinObservationRecord? observation, string status)
        {
            if (observation == null)
            {
                string displayStatus = string.IsNullOrWhiteSpace(status) ? "No current observation" : PortDisplayLocation(status);
                return $"Last Observation:\r\n--\r\n--\r\n--\r\n{displayStatus}";
            }

            return $"Last Observation:\r\n{observation.GoblinType}\r\n{PortDisplayLocation(observation.AreaKey)}\r\n{observation.Source}\r\n{observation.Reason}";
        }

        private sealed record PortGoblinTrackerAreaResolution(
            GoblinAreaResolution Area,
            bool Blocked,
            string SuppressionReason,
            string BestName,
            double BestConfidence,
            string SecondName,
            double SecondConfidence,
            double Delta,
            string AmbiguityGroup,
            string DisambiguationReason);

        private void PortLogSessionSummary(bool exportMarkdownSummary = true)
        {
            TimeSpan runtime = sessionStartTime == default ? TimeSpan.Zero : DateTime.Now - sessionStartTime;
            DiagnosticsSessionSnapshot diagnosticsSnapshot = DebugManager.Session.Snapshot(DateTime.Now);
            AppLogger.Info("========== Session Summary ==========");
            AppLogger.Info($"Games Created: {sessionGamesCreated}");
            AppLogger.Info($"Teleports Completed: {sessionTeleportsCompleted}");
            AppLogger.Info($"Blocked Teleports: {sessionBlockedTeleports}");
            AppLogger.Info($"Failures: {sessionFailures}");
            AppLogger.Info($"Runtime: {runtime:hh\\:mm\\:ss}");
            AppLogger.Info(
                "Diagnostics Counters: " +
                $"TeleportsAttempted={diagnosticsSnapshot.TeleportsAttempted}; " +
                $"TeleportsConfirmed={diagnosticsSnapshot.TeleportsConfirmed}; " +
                $"TeleportFailuresOrTimeouts={diagnosticsSnapshot.TeleportFailuresOrTimeouts}; " +
                $"StartGameFailures={diagnosticsSnapshot.StartGameFailures}; " +
                $"BattleNetLaunchFailures={diagnosticsSnapshot.BattleNetLaunchFailures}; " +
                $"RepairFailures={diagnosticsSnapshot.RepairFailures}; " +
                $"SalvageFailures={diagnosticsSnapshot.SalvageFailures}; " +
                $"StashFailures={diagnosticsSnapshot.StashFailures}; " +
                $"WorkflowCancellations={diagnosticsSnapshot.WorkflowCancellations}; " +
                $"UnexpectedExceptions={diagnosticsSnapshot.UnexpectedExceptions}; " +
                $"CombatActiveTime={diagnosticsSnapshot.CombatActiveTime:hh\\:mm\\:ss}");
            AppLogger.Info("Goblin Tracker");
            AppLogger.Info("--------------");
            AppLogger.Info($"Goblins Found: {diagnosticsSnapshot.GoblinCount}");
            AppLogger.Info($"Counted Area Keys: {diagnosticsSnapshot.CountedGoblinAreaCount}");
            AppLogger.Info($"Last Counted Area: {PortDisplayLocation(diagnosticsSnapshot.LastCountedGoblinAreaKey)}");
            AppLogger.Info($"Active Combat Time: {diagnosticsSnapshot.GoblinActiveCombatTime:hh\\:mm\\:ss}");
            AppLogger.Info($"GPH: {diagnosticsSnapshot.GoblinsPerHour:0.00}");
            AppLogger.Info("Goblin Observations");
            AppLogger.Info("-------------------");
            AppLogger.Info($"Goblin Observations: {diagnosticsSnapshot.GoblinObservationCount}");
            AppLogger.Info($"Journal Observations: {diagnosticsSnapshot.JournalObservationCount}");
            AppLogger.Info($"Minimap Observations: {diagnosticsSnapshot.MinimapObservationCount}");
            AppLogger.Info($"Eligible Observations: {diagnosticsSnapshot.EligibleObservationCount}");
            AppLogger.Info($"Blocked Observations: {diagnosticsSnapshot.BlockedObservationCount}");
            AppLogger.Info($"Duplicate Observations: {diagnosticsSnapshot.DuplicateObservationCount}");
            AppLogger.Info("Goblin Evidence");
            AppLogger.Info("---------------");
            AppLogger.Info($"Events Detected: {diagnosticsSnapshot.GoblinEvidenceEventCount}");
            AppLogger.Info($"Last Evidence: {diagnosticsSnapshot.LastGoblinEvidenceType}");
            AppLogger.Info($"Last Confidence: {diagnosticsSnapshot.LastGoblinEvidenceConfidence:0.00}");
            AppLogger.Info($"Last Evidence Time: {(diagnosticsSnapshot.LastGoblinEvidenceTime.HasValue ? diagnosticsSnapshot.LastGoblinEvidenceTime.Value.ToString("HH:mm:ss") : "--")}");
            AppLogger.Info($"Evidence Screenshot Folder: {DebugManager.GoblinEvidenceDirectory}");
            AppLogger.Info("=====================================");
            if (!exportMarkdownSummary || portApplicationClosing)
            {
                AppLogger.Info(
                    "SessionSummaryExportSkipped: " +
                    $"reason={(portApplicationClosing ? "AppClosing" : "DisabledByCaller")}; " +
                    $"exportMarkdownSummary={exportMarkdownSummary}; " +
                    $"appClosing={portApplicationClosing}");
                return;
            }

            DebugManager.ExportSessionSummary(new SessionSummaryContext(
                AppLogger.CurrentLogFilePath,
                portDiagnosticLatestPackagePath,
                portDiagnosticLatestScreenshotPath,
                DebugManager.FindLatestScreenshotPath(),
                portDiagnosticLatestFailureScreenshotType,
                portLastWorkflowStep,
                diagnosticsSnapshot.LastKnownIssue));
        }

        private void PortWriteSessionMetadata(bool logSuccess = true)
        {
            try
            {
                string metadataPath = DebugManager.SessionInfoPath;
                if (portApplicationClosing)
                {
                    AppLogger.Info($"Session metadata skipped: reason=AppClosing; path={metadataPath}");
                    return;
                }

                DiagnosticsSessionSnapshot snapshot = DebugManager.Session.Snapshot(DateTime.Now);
                string[] lines =
                [
                    $"SessionStartLocal={sessionStartTime:O}",
                    $"SessionStartUtc={sessionStartTime.ToUniversalTime():O}",
                    $"GoblinCount={snapshot.GoblinCount}",
                    $"GoblinFoundRecordCount={snapshot.GoblinFoundRecordCount}",
                    $"CountedGoblinAreaCount={snapshot.CountedGoblinAreaCount}",
                    $"LastCountedGoblinAreaKey={snapshot.LastCountedGoblinAreaKey}",
                    $"ActiveCombatTime={snapshot.GoblinActiveCombatTime:hh\\:mm\\:ss}",
                    $"ActiveCombatTimeSeconds={(long)snapshot.GoblinActiveCombatTime.TotalSeconds}",
                    $"CombatStartTimeLocal={(snapshot.GoblinCombatStartTime.HasValue ? snapshot.GoblinCombatStartTime.Value.ToString("O") : "")}",
                    $"CombatStartTimeUtc={(snapshot.GoblinCombatStartTime.HasValue ? snapshot.GoblinCombatStartTime.Value.ToUniversalTime().ToString("O") : "")}",
                    $"GPH={snapshot.GoblinsPerHour:0.00}",
                    $"GoblinObservationCount={snapshot.GoblinObservationCount}",
                    $"JournalObservationCount={snapshot.JournalObservationCount}",
                    $"MinimapObservationCount={snapshot.MinimapObservationCount}",
                    $"EligibleObservationCount={snapshot.EligibleObservationCount}",
                    $"BlockedObservationCount={snapshot.BlockedObservationCount}",
                    $"DuplicateObservationCount={snapshot.DuplicateObservationCount}",
                    $"LastGoblinObservationSource={snapshot.LastGoblinObservation?.Source ?? ""}",
                    $"LastGoblinObservationType={snapshot.LastGoblinObservation?.GoblinType ?? ""}",
                    $"LastGoblinObservationAreaKey={snapshot.LastGoblinObservation?.AreaKey ?? ""}",
                    $"LastGoblinObservationWouldCount={(snapshot.LastGoblinObservation != null ? snapshot.LastGoblinObservation.WouldCount.ToString() : "")}",
                    $"LastGoblinObservationReason={snapshot.LastGoblinObservation?.Reason ?? ""}",
                    $"GoblinEvidenceEventCount={snapshot.GoblinEvidenceEventCount}",
                    $"LastGoblinEvidenceType={(snapshot.GoblinEvidenceEventCount > 0 ? snapshot.LastGoblinEvidenceType.ToString() : "None")}",
                    $"LastGoblinEvidenceConfidence={snapshot.LastGoblinEvidenceConfidence:0.00}",
                    $"LastGoblinEvidenceTimeLocal={(snapshot.LastGoblinEvidenceTime.HasValue ? snapshot.LastGoblinEvidenceTime.Value.ToString("O") : "")}",
                    $"LastGoblinEvidenceScreenshotPath={snapshot.LastGoblinEvidenceScreenshotPath}",
                    $"GoblinEvidenceScreenshotFolder={DebugManager.GoblinEvidenceDirectory}",
                    $"ProcessId={Environment.ProcessId}",
                    $"BaseDirectory={AppDomain.CurrentDomain.BaseDirectory}"
                ];
                File.WriteAllLines(metadataPath, lines);
                if (logSuccess)
                {
                    AppLogger.Info($"Session metadata written: {metadataPath}; sessionStartLocal={sessionStartTime:O}; goblins={snapshot.GoblinCount}; activeCombatTime={snapshot.GoblinActiveCombatTime:hh\\:mm\\:ss}; gph={snapshot.GoblinsPerHour:0.00}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Session metadata write failed.", ex);
            }
        }

        private string PortCaptureFailureScreenshot(string failureType, string workflow = "Workflow")
        {
            portDiagnosticLatestFailureScreenshotType = string.IsNullOrWhiteSpace(failureType) ? "Unknown" : failureType;
            DebugManager.Session.SetLastKnownIssue($"{workflow}: {portDiagnosticLatestFailureScreenshotType}");
            if (portApplicationClosing)
            {
                AppLogger.Info($"Failure screenshot skipped: type={portDiagnosticLatestFailureScreenshotType}; workflow={PortLogField(workflow)}; reason=AppClosing");
                return "";
            }

            CaptureDebugScreenshot(workflow, portDiagnosticLatestFailureScreenshotType);
            PortScreenshotPair pair = PortCaptureDiagnosticScreenshotPair("Failure", workflow, portDiagnosticLatestFailureScreenshotType);
            if (!string.IsNullOrWhiteSpace(pair.DiabloPath) || !string.IsNullOrWhiteSpace(pair.AppPath))
            {
                string path = !string.IsNullOrWhiteSpace(pair.DiabloPath) ? pair.DiabloPath : pair.AppPath;
                DebugManager.RecordDebugScreenshotPath(path);
                AppLogger.Info($"Failure screenshot saved: type={portDiagnosticLatestFailureScreenshotType}; path={path}");
            }

            return pair.DiabloPath;
        }

        private PortScreenshotPair PortCaptureSuccessScreenshot(string workflow, string action)
        {
            if (!AppSettings.Debug.EnableSuccessScreenshots)
            {
                AppLogger.Info("Success screenshot skipped (EnableSuccessScreenshots=false)");
                return new PortScreenshotPair("", "");
            }

            return PortCaptureDiagnosticScreenshotPair("Success", workflow, action);
        }

        private PortScreenshotPair PortCaptureCombatDiagnosticScreenshot(string action)
        {
            return PortCaptureDiagnosticScreenshotPair(CombatDiagnosticNames.DemonHunterNoClickSuppressionOutcome, CombatDiagnosticNames.DemonHunterNoClickSuppressionWorkflow, action);
        }

        private string PortCaptureDebugScreenshot(string reason)
        {
            try
            {
                if (portApplicationClosing)
                {
                    AppLogger.Info($"Debug screenshot skipped: app closing; reason={reason}");
                    return "";
                }

                if (!DebugManager.ShouldCaptureDebugEvidence(chkKeepDebugScreenshots == null || chkKeepDebugScreenshots.Checked))
                {
                    string skipReason = AppSettings.Debug.EnableDebugScreenshots
                        ? "disabled by Keep Debug Screenshots setting"
                        : "disabled by AppSettings";
                    AppLogger.Info($"Debug screenshot skipped: {skipReason}; reason={reason}");
                    return "";
                }

                IntPtr diabloWindow = FindDiabloWindow();
                RECT rect;
                if (diabloWindow != IntPtr.Zero && PortTryGetDiabloClientScreenRect(diabloWindow, reason, out rect))
                {
                    AppLogger.Info($"Debug screenshot capturing Diablo client: reason={reason}");
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
                    AppLogger.Info($"Debug screenshot capturing virtual screen fallback: reason={reason}; bounds={screen.Left},{screen.Top},{screen.Width},{screen.Height}");
                }

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                if (width <= 0 || height <= 0)
                {
                    AppLogger.Info($"Debug screenshot skipped: capture rectangle invalid; reason={reason}");
                    return "";
                }

                string screenshotDirectory = DebugManager.ScreenshotsDirectory;
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
                portDiagnosticLatestScreenshotPath = path;
                DebugManager.RecordDebugScreenshotPath(path);
                AppLogger.Info($"Debug screenshot saved: {path}");
                return path;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Debug screenshot capture failed: {reason}", ex);
                return "";
            }
        }

        private PortScreenshotPair PortCaptureDiagnosticScreenshotPair(string outcome, string workflow, string action)
        {
            try
            {
                if (!DebugManager.ShouldCaptureDebugEvidence(chkKeepDebugScreenshots == null || chkKeepDebugScreenshots.Checked))
                {
                    string skipReason = AppSettings.Debug.EnableDebugScreenshots
                        ? "disabled by Keep Debug Screenshots setting"
                        : "disabled by AppSettings";
                    AppLogger.Info($"Diagnostic screenshot pair skipped: {skipReason}; outcome={outcome}; workflow={workflow}; action={action}");
                    return new PortScreenshotPair("", "");
                }

                string screenshotDirectory = DebugManager.ScreenshotsDirectory;
                Directory.CreateDirectory(screenshotDirectory);

                DateTime timestamp = DateTime.Now;
                string safeOutcome = PortSafeScreenshotName(outcome, "Debug");
                string safeWorkflow = PortSafeScreenshotName(workflow, "Workflow");
                string safeAction = PortSafeScreenshotName(action, "Action");
                string filePrefix = $"{timestamp:yyyy-MM-dd_HHmmss_fff}_{safeOutcome}_{safeWorkflow}_{safeAction}";

                string diabloPath = Path.Combine(screenshotDirectory, $"{filePrefix}_Diablo.png");
                string appPath = Path.Combine(screenshotDirectory, $"{filePrefix}_App.png");

                string savedDiabloPath = PortCaptureDiabloScreenshotToFile(diabloPath, $"{outcome}:{workflow}:{action}");
                string savedAppPath = PortCaptureAppScreenshotToFile(appPath, $"{outcome}:{workflow}:{action}");

                string latestPath = !string.IsNullOrWhiteSpace(savedDiabloPath) ? savedDiabloPath : savedAppPath;
                if (!string.IsNullOrWhiteSpace(latestPath))
                {
                    portDiagnosticLatestScreenshotPath = latestPath;
                    DebugManager.RecordDebugScreenshotPath(latestPath);
                }

                AppLogger.Info($"Diagnostic screenshot pair saved: timestamp={timestamp:yyyy-MM-dd HH:mm:ss.fff}; outcome={safeOutcome}; workflow={safeWorkflow}; action={safeAction}; diablo={PortDisplayLocation(savedDiabloPath)}; app={PortDisplayLocation(savedAppPath)}");
                return new PortScreenshotPair(savedDiabloPath, savedAppPath);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Diagnostic screenshot pair capture failed: outcome={outcome}; workflow={workflow}; action={action}", ex);
                return new PortScreenshotPair("", "");
            }
        }

        private string PortCaptureDiabloScreenshotToFile(string path, string reason)
        {
            IntPtr diabloWindow = FindDiabloWindow();
            RECT rect;
            if (diabloWindow != IntPtr.Zero && PortTryGetDiabloClientScreenRect(diabloWindow, reason, out rect))
            {
                AppLogger.Info($"Diagnostic screenshot capturing Diablo client: reason={reason}");
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
                AppLogger.Info($"Diagnostic screenshot capturing virtual screen fallback for Diablo evidence: reason={reason}; bounds={screen.Left},{screen.Top},{screen.Width},{screen.Height}");
            }

            return PortCaptureScreenRectangleToFile(rect, path, reason);
        }

        private string PortCaptureAppScreenshotToFile(string path, string reason)
        {
            if (InvokeRequired)
            {
                return (string)Invoke(new Func<string>(() => PortCaptureAppScreenshotToFile(path, reason)));
            }

            Rectangle bounds = Bounds;
            IntPtr appHandle = Handle;
            IntPtr foregroundWindow = GetForegroundWindow();
            bool visible = Visible && IsWindowVisible(appHandle);
            bool minimized = WindowState == FormWindowState.Minimized || IsIconic(appHandle);
            bool foreground = foregroundWindow == appHandle;
            bool possiblyOccluded = visible && !minimized && !foreground;
            RECT rect = new()
            {
                Left = bounds.Left,
                Top = bounds.Top,
                Right = bounds.Right,
                Bottom = bounds.Bottom,
            };

            AppLogger.Info($"Diagnostic screenshot capturing app window: reason={reason}; visible={visible}; minimized={minimized}; foreground={foreground}; possiblyOccluded={possiblyOccluded}; bounds={bounds.Left},{bounds.Top},{bounds.Width},{bounds.Height}; foregroundWindow=0x{foregroundWindow.ToInt64():X}; appWindow=0x{appHandle.ToInt64():X}");
            if (!visible || minimized || possiblyOccluded)
            {
                AppLogger.Info($"Diagnostic screenshot app window warning: reason={reason}; mayCaptureAnotherWindow=true; visible={visible}; minimized={minimized}; possiblyOccluded={possiblyOccluded}; bounds={bounds.Left},{bounds.Top},{bounds.Width},{bounds.Height}");
            }

            return PortCaptureScreenRectangleToFile(rect, path, reason);
        }

        private string PortCaptureScreenRectangleToFile(RECT rect, string path, string reason)
        {
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0)
            {
                AppLogger.Info($"Diagnostic screenshot skipped: capture rectangle invalid; reason={reason}; path={path}");
                return "";
            }

            using Bitmap screenshot = new(width, height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, screenshot.Size);
            }

            screenshot.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            return path;
        }

        private static string PortSafeScreenshotName(string value, string fallback)
        {
            string safe = string.Join("_", (value ?? "").Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            safe = safe.Replace(" ", "");
            return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
        }

        private bool PortTryGetDiabloClientScreenRect(IntPtr diabloWindow, string reason, out RECT rect)
        {
            rect = new RECT();
            GetWindowThreadProcessId(diabloWindow, out uint processId);
            try
            {
                using System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById((int)processId);
                if (!process.ProcessName.Equals("Diablo III", StringComparison.OrdinalIgnoreCase) &&
                    !process.ProcessName.Equals("Diablo III64", StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Info($"Debug screenshot skipped Diablo client: handle is not Diablo; process={process.ProcessName}; reason={reason}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Debug screenshot skipped Diablo client: process verification failed; reason={reason}", ex);
                return false;
            }

            if (!GetClientRect(diabloWindow, out RECT clientRect))
            {
                AppLogger.Info($"Debug screenshot skipped Diablo client: client rectangle unavailable; reason={reason}");
                return false;
            }

            DrawingPoint clientTopLeft = new(clientRect.Left, clientRect.Top);
            if (!ClientToScreen(diabloWindow, ref clientTopLeft))
            {
                AppLogger.Info($"Debug screenshot skipped Diablo client: client origin unavailable; reason={reason}");
                return false;
            }

            rect = new RECT
            {
                Left = clientTopLeft.X,
                Top = clientTopLeft.Y,
                Right = clientTopLeft.X + (clientRect.Right - clientRect.Left),
                Bottom = clientTopLeft.Y + (clientRect.Bottom - clientRect.Top),
            };
            return true;
        }

        private void PortCleanupOldDebugScreenshots(int retentionDays)
        {
            try
            {
                string screenshotDirectory = DebugManager.ScreenshotsDirectory;
                if (!Directory.Exists(screenshotDirectory))
                {
                    AppLogger.Info($"Screenshot retention cleanup complete: deleted=0, retentionDays={retentionDays}");
                    return;
                }

                DateTime cutoff = DateTime.Now.AddDays(-retentionDays);
                int deleted = 0;
                foreach (string file in Directory.GetFiles(screenshotDirectory, "*.png"))
                {
                    try
                    {
                        DateTime lastWrite = File.GetLastWriteTime(file);
                        if (lastWrite >= sessionScreenshotRetentionStartTime || lastWrite >= cutoff)
                        {
                            continue;
                        }

                        File.Delete(file);
                        deleted++;
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"Failed to delete old screenshot: {file}", ex);
                    }
                }

                AppLogger.Info($"Screenshot retention cleanup complete: deleted={deleted}, retentionDays={retentionDays}");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Screenshot retention cleanup failed.", ex);
            }
        }

        private sealed record PortScreenshotPair(string DiabloPath, string AppPath);
    }
}

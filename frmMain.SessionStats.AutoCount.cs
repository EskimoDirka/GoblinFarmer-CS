using System.Globalization;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private bool PortTryRecordAutomaticGoblinCount(
            GoblinObservationRecord observation,
            GoblinAreaResolution area,
            string evidenceSignature,
            string evidenceImagePath,
            IReadOnlyList<ImageRecognitionSampleCandidate>? rankedSamples,
            string evidenceNotes = "")
        {
            string areaKey = PortDisplayLocation(area.AreaKey);
            string displayLocation = PortDisplayLocation(area.DisplayLocation);
            string source = "AutomaticObservation";
            DateTime nowUtc = DateTime.UtcNow;
            bool autoCountingEnabled = PortGoblinAutomaticCountingEnabled();
            double minimapAutoCountMinimumConfidence = PortAutomaticGoblinMinimapCountMinimumConfidenceFor(observation.GoblinType);
            bool minimapAutoCountConfidencePending = string.Equals(observation.Source, "Minimap", StringComparison.OrdinalIgnoreCase) &&
                observation.EvidenceConfidence > 0 &&
                observation.EvidenceConfidence < minimapAutoCountMinimumConfidence;
            double evidenceAgeSeconds = Math.Max(0, (nowUtc - observation.TimestampUtc).TotalSeconds);
            string autoEvidenceKey = PortGoblinAutoCountEvidenceKey(evidenceSignature, observation);
            string globalEvidenceKey = PortGoblinAutoCountGlobalEvidenceKey(evidenceSignature, observation.Source, observation.GoblinType);
            string autoEncounterKey = PortGoblinAutoCountEncounterKey(observation);
            GoblinAreaDuplicateGuardResult guardResult = new(observation.WouldCount, observation.CurrentAreaCount, observation.AreaLimit);
            string suppressionReason = "";
            string encounterSuppressionMatch = "";
            string reliabilityReason = "";
            string evidenceReliability = "";
            bool evidenceReliabilityAllowsCount = false;
            int total = 0;
            PortGoblinAutoCountEvidenceState? evidenceState = null;
            PortGoblinAutoCountEncounterState? encounterState = null;
            double evidenceFirstSeenAgeSeconds = 0;
            double encounterAgeSeconds = -1;
            string encounterAreaKey = "";
            bool refreshEncounterLastSeen = false;
            bool pfMultiCountDuplicateBypass = false;
            string pfMultiCountDuplicateBypassReason = "";
            double pfMultiCountDuplicateElapsedSeconds = -1;
            double autoArmedAgeSeconds = portGoblinAutomaticCountingArmedAtUtc == DateTime.MinValue
                ? -1
                : Math.Max(0, (nowUtc - portGoblinAutomaticCountingArmedAtUtc).TotalSeconds);
            int totalGoblinCountBefore = DebugManager.Session.Snapshot(DateTime.Now).GoblinCount;
            const double MinimapAreaChangedConfidenceMargin = 0.03;
            string currentAreaAtAcceptance = PortResolvedAreaKey(portLastConfirmedLocation);
            string staleArea = PortGoblinEvidenceNoteValue(evidenceNotes, "staleArea");
            string titleResolverOverride = PortGoblinEvidenceNoteValue(evidenceNotes, "TitleResolverOverride");
            bool evidenceAreaDiffersFromCurrent = !string.IsNullOrWhiteSpace(area.AreaKey) &&
                !string.IsNullOrWhiteSpace(currentAreaAtAcceptance) &&
                !GoblinAreaResolver.NormalizedKey(area.AreaKey).Equals(
                    GoblinAreaResolver.NormalizedKey(currentAreaAtAcceptance),
                    StringComparison.OrdinalIgnoreCase);

            lock (portGoblinTrackerLock)
            {
                if (string.IsNullOrWhiteSpace(autoEvidenceKey))
                {
                    suppressionReason = "EvidenceSignatureMissing";
                }
                else
                {
                    if (!portGoblinAutoCountEvidenceBySignature.TryGetValue(autoEvidenceKey, out evidenceState))
                    {
                        evidenceState = new PortGoblinAutoCountEvidenceState(
                            nowUtc,
                            nowUtc,
                            false,
                            area.AreaKey,
                            observation.GoblinType,
                            observation.Source);
                    }
                    else
                    {
                        evidenceState = evidenceState with
                        {
                            LastSeenUtc = nowUtc,
                            AreaKey = string.IsNullOrWhiteSpace(evidenceState.AreaKey) ? area.AreaKey : evidenceState.AreaKey,
                            GoblinType = observation.GoblinType,
                            Source = observation.Source,
                        };
                    }

                    portGoblinAutoCountEvidenceBySignature[autoEvidenceKey] = evidenceState;
                    evidenceFirstSeenAgeSeconds = Math.Max(0, (nowUtc - evidenceState.FirstSeenUtc).TotalSeconds);
                }

                if (!string.IsNullOrWhiteSpace(autoEncounterKey) &&
                    portGoblinAutoCountEncounterByGoblinType.TryGetValue(autoEncounterKey, out encounterState))
                {
                    encounterAgeSeconds = Math.Max(0, (nowUtc - encounterState.CountedUtc).TotalSeconds);
                    encounterAreaKey = encounterState.AreaKey;
                }

                if (!autoCountingEnabled)
                {
                    suppressionReason = "AutomaticCountingDisabled";
                }
                else if (string.IsNullOrWhiteSpace(suppressionReason) &&
                    (portGoblinAutomaticCountingArmedAtUtc == DateTime.MinValue ||
                    evidenceState!.FirstSeenUtc < portGoblinAutomaticCountingArmedAtUtc))
                {
                    suppressionReason = "EvidenceSeenBeforeAutoCountEnabled";
                }
                else if (string.IsNullOrWhiteSpace(suppressionReason) &&
                    evidenceState!.Counted)
                {
                    GoblinAreaDuplicateGuardResult pfGuardResult = portGoblinAreaDuplicateGuard.Peek(area.AreaKey);
                    if (encounterState != null &&
                        GoblinPandemoniumMultiCountDuplicatePolicy.ShouldBypass(
                            observation.Source,
                            area.AreaKey,
                            pfGuardResult.AreaCount,
                            pfGuardResult.AreaLimit,
                            encounterState.AreaKey,
                            encounterState.CountedUtc,
                            nowUtc,
                            globalEvidenceKey,
                            observation.EvidenceConfidence,
                            minimapAutoCountMinimumConfidence,
                            evidenceFirstSeenAgeSeconds,
                            portCombatRunning,
                            out pfMultiCountDuplicateBypassReason,
                            out pfMultiCountDuplicateElapsedSeconds))
                    {
                        pfMultiCountDuplicateBypass = true;
                        guardResult = pfGuardResult;
                        encounterSuppressionMatch = "PfMultiCountDuplicateBypass";
                    }
                    else
                    {
                        suppressionReason = "EvidenceAlreadyAutoCounted";
                        encounterSuppressionMatch = pfMultiCountDuplicateBypassReason;
                    }
                }
                else if (string.IsNullOrWhiteSpace(suppressionReason) &&
                    !pfMultiCountDuplicateBypass &&
                    PortShouldSuppressEncounterAlreadyAutoCounted(observation, area, globalEvidenceKey, encounterState, nowUtc, out encounterSuppressionMatch))
                {
                    GoblinAreaDuplicateGuardResult pfGuardResult = portGoblinAreaDuplicateGuard.Peek(area.AreaKey);
                    if (encounterState != null &&
                        GoblinPandemoniumMultiCountDuplicatePolicy.ShouldBypass(
                            observation.Source,
                            area.AreaKey,
                            pfGuardResult.AreaCount,
                            pfGuardResult.AreaLimit,
                            encounterState.AreaKey,
                            encounterState.CountedUtc,
                            nowUtc,
                            globalEvidenceKey,
                            observation.EvidenceConfidence,
                            minimapAutoCountMinimumConfidence,
                            evidenceFirstSeenAgeSeconds,
                            portCombatRunning,
                            out pfMultiCountDuplicateBypassReason,
                            out pfMultiCountDuplicateElapsedSeconds))
                    {
                        pfMultiCountDuplicateBypass = true;
                        guardResult = pfGuardResult;
                        encounterSuppressionMatch = "PfMultiCountDuplicateBypass";
                    }
                    else if (PortShouldBypassFreshCrossAreaJournalDuplicateSuppression(
                        observation,
                        area.AreaKey,
                        globalEvidenceKey,
                        encounterState,
                        encounterSuppressionMatch,
                        evidenceFirstSeenAgeSeconds))
                    {
                        encounterSuppressionMatch = $"{encounterSuppressionMatch};FreshCrossAreaJournalDuplicateBypass";
                    }
                    else
                    {
                        suppressionReason = "EncounterAlreadyAutoCounted";
                        encounterSuppressionMatch = string.IsNullOrWhiteSpace(pfMultiCountDuplicateBypassReason)
                            ? encounterSuppressionMatch
                            : $"{encounterSuppressionMatch};{pfMultiCountDuplicateBypassReason}";
                        refreshEncounterLastSeen = GoblinAutoCountEncounterSuppressionPolicy.ShouldRefreshEncounterLastSeenAfterSuppression(
                            observation.Source,
                            area.AreaKey,
                            encounterState!.AreaKey);
                    }
                }
                else if (string.IsNullOrWhiteSpace(suppressionReason) &&
                    minimapAutoCountConfidencePending)
                {
                    suppressionReason = "MinimapConfidencePendingJournal";
                }
                else if (string.IsNullOrWhiteSpace(suppressionReason) &&
                    observation.Source.Equals("Minimap", StringComparison.OrdinalIgnoreCase) &&
                    evidenceAreaDiffersFromCurrent &&
                    observation.EvidenceConfidence > 0 &&
                    observation.EvidenceConfidence <= minimapAutoCountMinimumConfidence + MinimapAreaChangedConfidenceMargin)
                {
                    suppressionReason = "MinimapAreaChangedLowMargin";
                }
                else if (string.IsNullOrWhiteSpace(suppressionReason) &&
                    !(evidenceReliabilityAllowsCount = PortTryApplyRecentMinimapJournalConfirmationReliability(
                        observation,
                        area.AreaKey,
                        autoEvidenceKey,
                        nowUtc,
                        out reliabilityReason,
                        out evidenceReliability) ||
                    GoblinAutoCountEvidenceReliabilityPolicy.AllowsAutomaticCount(
                        observation.Source,
                        autoEvidenceKey,
                        evidenceFirstSeenAgeSeconds,
                        portCombatRunning,
                        observation.EvidenceConfidence,
                        out reliabilityReason,
                        out evidenceReliability)))
                {
                    suppressionReason = reliabilityReason;
                }
                else if (string.IsNullOrWhiteSpace(suppressionReason) &&
                    !GoblinJournalFreshnessPolicy.IsFresh(evidenceState!.FirstSeenUtc, nowUtc, GoblinJournalEvidenceFreshWindow))
                {
                    suppressionReason = "StaleEvidence";
                }
                else if (string.IsNullOrWhiteSpace(suppressionReason) &&
                    !observation.WouldCount &&
                    !PortObservationPendingJournalPromotedByReliability(observation, evidenceReliabilityAllowsCount, evidenceReliability))
                {
                    suppressionReason = string.IsNullOrWhiteSpace(observation.Reason)
                        ? "ObservationNotEligible"
                        : observation.Reason;
                }
                else if (!area.Resolved)
                {
                    suppressionReason = "AreaUnresolved";
                }
                else if (GoblinManualCountBlockList.IsBlocked(area.AreaKey))
                {
                    suppressionReason = "BlockedArea";
                    guardResult = new(false, 0, 0);
                }
                else if (!portGoblinAreaDuplicateGuard.TryAccept(area.AreaKey, out guardResult))
                {
                    suppressionReason = guardResult.AreaLimit > 1 ? "AreaLimitReached" : "AreaAlreadyCounted";
                    refreshEncounterLastSeen = encounterState != null &&
                        GoblinAutoCountEncounterSuppressionPolicy.ShouldRefreshEncounterLastSeenAfterAreaAlreadyCounted(
                            observation.Source,
                            area.AreaKey,
                            encounterState.AreaKey);
                }
                else
                {
                    GoblinFoundRecord countedRecord = new(
                        area.AreaKey,
                        area.DisplayLocation,
                        observation.GoblinType,
                        source,
                        nowUtc,
                        true,
                        "");
                    total = DebugManager.Session.RecordGoblinFound(countedRecord);
                    portGoblinAutoCountEvidenceBySignature[autoEvidenceKey] = evidenceState! with
                    {
                        Counted = true,
                        LastSeenUtc = nowUtc,
                        AreaKey = area.AreaKey,
                        GoblinType = observation.GoblinType,
                        Source = observation.Source,
                    };
                    if (!string.IsNullOrWhiteSpace(autoEncounterKey))
                    {
                        portGoblinAutoCountEncounterByGoblinType[autoEncounterKey] = new PortGoblinAutoCountEncounterState(
                            nowUtc,
                            nowUtc,
                            area.AreaKey,
                            observation.GoblinType,
                            observation.Source,
                            globalEvidenceKey);
                    }
                }

                if (!string.IsNullOrWhiteSpace(suppressionReason) &&
                    encounterState != null &&
                    !string.IsNullOrWhiteSpace(autoEncounterKey) &&
                    (refreshEncounterLastSeen || suppressionReason.Equals("EvidenceAlreadyAutoCounted", StringComparison.OrdinalIgnoreCase)))
                {
                    encounterState = encounterState with
                    {
                        LastSeenUtc = nowUtc,
                        GoblinType = observation.GoblinType,
                        Source = observation.Source,
                        EvidenceKey = string.IsNullOrWhiteSpace(globalEvidenceKey)
                            ? encounterState.EvidenceKey
                            : globalEvidenceKey,
                    };
                    portGoblinAutoCountEncounterByGoblinType[autoEncounterKey] = encounterState;
                }
            }

            bool counted = string.IsNullOrWhiteSpace(suppressionReason);
            int areaCountBefore = counted ? Math.Max(0, guardResult.AreaCount - 1) : guardResult.AreaCount;
            string firstSeenArea = evidenceState == null || string.IsNullOrWhiteSpace(evidenceState.AreaKey)
                ? area.AreaKey
                : evidenceState.AreaKey;
            bool areaChangedDuringPendingEvidence = !string.IsNullOrWhiteSpace(firstSeenArea) &&
                !string.IsNullOrWhiteSpace(currentAreaAtAcceptance) &&
                !string.Equals(firstSeenArea, currentAreaAtAcceptance, StringComparison.OrdinalIgnoreCase);
            string evidenceHash = PortGoblinEvidenceHash(autoEvidenceKey);
            if (suppressionReason.Equals("MinimapAreaChangedLowMargin", StringComparison.OrdinalIgnoreCase))
            {
                PortRememberSuppressedMinimapAreaAnchor(
                    observation,
                    area,
                    currentAreaAtAcceptance,
                    suppressionReason,
                    nowUtc,
                    evidenceHash);
            }

            if (PortGoblinDecisionTraceEnabled())
            {
                GoblinDecisionTraceRecord trace = GoblinDecisionTracePolicy.Create(
                    nowUtc,
                    "Live",
                    observation.Source,
                    Path.GetFileName(evidenceImagePath),
                    evidenceImagePath,
                    area.RawLocation,
                    area.AreaKey,
                    observation.GoblinType,
                    PortShortEvidenceSignature(autoEvidenceKey),
                    evidenceAgeSeconds,
                    evidenceFirstSeenAgeSeconds,
                    autoCountingEnabled,
                    AppSettings.GoblinTracker.EnableObservationMode,
                    suppressionReason,
                    counted,
                    areaCountBefore,
                    guardResult.AreaLimit,
                    totalGoblinCountBefore);
                PortLogGoblinDecisionTrace(trace);
                if (PortShouldWriteGoblinDecisionBundle(trace, out string decisionBundleSkipReason))
                {
                    PortWriteGoblinDecisionBundle(trace);
                }
                else
                {
                    AppLogger.Info(
                        "GoblinDecisionBundleSkipped: " +
                        $"correlationId={PortLogField(trace.CorrelationId)}; " +
                        $"decision={PortLogField(trace.Decision)}; " +
                        $"reason={PortLogField(trace.Reason)}; " +
                        $"skipReason={PortLogField(decisionBundleSkipReason)}; " +
                        $"source={PortLogField(trace.Source)}; " +
                        $"goblinType={PortLogField(trace.GoblinType)}; " +
                        $"areaKey={PortLogField(trace.AreaKey)}");
                }
            }

            if (!string.IsNullOrWhiteSpace(suppressionReason))
            {
                if (GoblinPandemoniumMultiCountDuplicatePolicy.IsPandemoniumFortressTwoCountArea(area.AreaKey) &&
                    (suppressionReason.Equals("EvidenceAlreadyAutoCounted", StringComparison.OrdinalIgnoreCase) ||
                    suppressionReason.Equals("EncounterAlreadyAutoCounted", StringComparison.OrdinalIgnoreCase)))
                {
                    AppLogger.Info(
                        "GoblinTracker: PfMultiCountDuplicateBypassSkipped " +
                        $"areaKey={areaKey} " +
                        $"areaCount={guardResult.AreaCount} " +
                        $"areaLimit={guardResult.AreaLimit} " +
                        $"elapsedSinceLastAcceptedSeconds={pfMultiCountDuplicateElapsedSeconds:0.0} " +
                        $"reason={PortLogField(pfMultiCountDuplicateBypassReason)} " +
                        $"suppressionReason={PortLogField(suppressionReason)} " +
                        $"source={PortLogField(observation.Source)} " +
                        $"goblinType={PortLogField(observation.GoblinType)} " +
                        $"evidenceHash={PortGoblinEvidenceHash(autoEvidenceKey)} " +
                        $"evidenceSignature={PortLogField(PortShortEvidenceSignature(autoEvidenceKey))}");
                }

                string eventName = autoCountingEnabled
                    ? "GoblinAutoCountSuppressed"
                    : "GoblinAutoCountSkippedDisabled";
                AppLogger.Info($"GoblinTracker: {eventName} source={PortLogField(observation.Source)} goblinType={PortLogField(observation.GoblinType)} areaKey={areaKey} displayLocation={displayLocation} firstSeenArea={PortLogField(firstSeenArea)} currentAreaAtDetection={PortLogField(currentAreaAtAcceptance)} currentAreaAtAcceptance={PortLogField(currentAreaAtAcceptance)} staleArea={PortLogField(staleArea)} acceptedArea=None notificationDisplayArea=None titleResolverOverride={PortLogField(titleResolverOverride)} notificationFreshnessState=Suppressed areaChangedDuringPendingEvidence={areaChangedDuringPendingEvidence} areaCount={guardResult.AreaCount} areaLimit={guardResult.AreaLimit} reason={PortLogField(suppressionReason)} evidenceReliability={PortLogField(evidenceReliability)} reliabilityReason={PortLogField(reliabilityReason)} evidenceAgeSeconds={evidenceAgeSeconds:0.0} evidenceFirstSeenAgeSeconds={evidenceFirstSeenAgeSeconds:0.0} evidenceFirstSeenUtc={(evidenceState == null ? "Unknown" : evidenceState.FirstSeenUtc.ToString("O"))} evidenceLastSeenUtc={(evidenceState == null ? "Unknown" : evidenceState.LastSeenUtc.ToString("O"))} encounterAgeSeconds={encounterAgeSeconds:0.0} encounterAreaKey={PortLogField(PortDisplayLocation(encounterAreaKey))} encounterCountedUtc={(encounterState == null ? "Unknown" : encounterState.CountedUtc.ToString("O"))} encounterLastSeenUtc={(encounterState == null ? "Unknown" : encounterState.LastSeenUtc.ToString("O"))} encounterMatch={PortLogField(encounterSuppressionMatch)} autoArmedAgeSeconds={autoArmedAgeSeconds:0.0} evidenceConfidence={observation.EvidenceConfidence:0.000} minimapAutoCountMinConfidence={minimapAutoCountMinimumConfidence:0.000} evidenceHash={evidenceHash} evidenceSignature={PortLogField(PortShortEvidenceSignature(autoEvidenceKey))} enableObservationMode={AppSettings.GoblinTracker.EnableObservationMode} enableAutomaticCounting={AppSettings.GoblinTracker.EnableAutomaticCounting}");
                PortWriteGoblinTrackerJsonEvent(
                    eventName,
                    new Dictionary<string, object?>
                    {
                        ["source"] = observation.Source,
                        ["goblinType"] = observation.GoblinType,
                        ["areaKey"] = areaKey,
                        ["displayLocation"] = displayLocation,
                        ["firstSeenArea"] = firstSeenArea,
                        ["currentAreaAtDetection"] = currentAreaAtAcceptance,
                        ["currentAreaAtAcceptance"] = currentAreaAtAcceptance,
                        ["staleArea"] = staleArea,
                        ["acceptedArea"] = null,
                        ["notificationDisplayArea"] = null,
                        ["titleResolverOverride"] = titleResolverOverride,
                        ["notificationFreshnessState"] = "Suppressed",
                        ["areaChangedDuringPendingEvidence"] = areaChangedDuringPendingEvidence,
                        ["areaCount"] = guardResult.AreaCount,
                        ["areaLimit"] = guardResult.AreaLimit,
                        ["reason"] = suppressionReason,
                        ["evidenceReliability"] = evidenceReliability,
                        ["reliabilityReason"] = reliabilityReason,
                        ["evidenceAgeSeconds"] = evidenceAgeSeconds,
                        ["evidenceFirstSeenAgeSeconds"] = evidenceFirstSeenAgeSeconds,
                        ["encounterAgeSeconds"] = encounterAgeSeconds,
                        ["encounterAreaKey"] = PortDisplayLocation(encounterAreaKey),
                        ["encounterMatch"] = encounterSuppressionMatch,
                        ["autoArmedAgeSeconds"] = autoArmedAgeSeconds,
                        ["evidenceConfidence"] = observation.EvidenceConfidence,
                        ["evidenceHash"] = evidenceHash,
                        ["enableObservationMode"] = AppSettings.GoblinTracker.EnableObservationMode,
                        ["enableAutomaticCounting"] = AppSettings.GoblinTracker.EnableAutomaticCounting,
                    });
                return false;
            }

            if (pfMultiCountDuplicateBypass)
            {
                AppLogger.Info(
                    "GoblinTracker: PfMultiCountDuplicateBypass " +
                    $"areaKey={areaKey} " +
                    $"areaCount={guardResult.AreaCount} " +
                    $"areaLimit={guardResult.AreaLimit} " +
                    $"elapsedSinceLastAcceptedSeconds={pfMultiCountDuplicateElapsedSeconds:0.0} " +
                    $"source={PortLogField(observation.Source)} " +
                    $"goblinType={PortLogField(observation.GoblinType)} " +
                    $"evidenceHash={PortGoblinEvidenceHash(autoEvidenceKey)} " +
                    $"evidenceSignature={PortLogField(PortShortEvidenceSignature(autoEvidenceKey))}");
            }

            if (string.IsNullOrWhiteSpace(evidenceReliability))
            {
                GoblinAutoCountEvidenceReliabilityPolicy.AllowsAutomaticCount(
                    observation.Source,
                    autoEvidenceKey,
                    evidenceFirstSeenAgeSeconds,
                    portCombatRunning,
                    observation.EvidenceConfidence,
                    out reliabilityReason,
                    out evidenceReliability);
            }

            AppLogger.Info($"GoblinTracker: GoblinAutoCountAccepted source={PortLogField(observation.Source)} goblinType={PortLogField(observation.GoblinType)} areaKey={areaKey} displayLocation={displayLocation} firstSeenArea={PortLogField(firstSeenArea)} currentAreaAtDetection={PortLogField(currentAreaAtAcceptance)} currentAreaAtAcceptance={PortLogField(currentAreaAtAcceptance)} staleArea={PortLogField(staleArea)} acceptedArea={PortLogField(areaKey)} notificationDisplayArea={PortLogField(displayLocation)} titleResolverOverride={PortLogField(titleResolverOverride)} notificationFreshnessState=Current areaChangedDuringPendingEvidence={areaChangedDuringPendingEvidence} areaCount={guardResult.AreaCount} areaLimit={guardResult.AreaLimit} reason=Eligible evidenceReliability={PortLogField(evidenceReliability)} reliabilityReason={PortLogField(reliabilityReason)} evidenceAgeSeconds={evidenceAgeSeconds:0.0} evidenceFirstSeenAgeSeconds={evidenceFirstSeenAgeSeconds:0.0} evidenceFirstSeenUtc={(evidenceState == null ? "Unknown" : evidenceState.FirstSeenUtc.ToString("O"))} evidenceLastSeenUtc={(evidenceState == null ? "Unknown" : evidenceState.LastSeenUtc.ToString("O"))} encounterAgeSeconds={encounterAgeSeconds:0.0} encounterMatch={PortLogField(encounterSuppressionMatch)} autoArmedAgeSeconds={autoArmedAgeSeconds:0.0} evidenceConfidence={observation.EvidenceConfidence:0.000} minimapAutoCountMinConfidence={minimapAutoCountMinimumConfidence:0.000} evidenceHash={evidenceHash} evidenceSignature={PortLogField(PortShortEvidenceSignature(autoEvidenceKey))} total={total} enableObservationMode={AppSettings.GoblinTracker.EnableObservationMode} enableAutomaticCounting={AppSettings.GoblinTracker.EnableAutomaticCounting}");
            AppLogger.Info($"GoblinTracker: GoblinCountAccepted areaKey={areaKey} areaCount={guardResult.AreaCount} areaLimit={guardResult.AreaLimit} blockListStatus=Allowed countResult=Accepted rawLocation='{PortLogField(PortDisplayLocation(area.RawLocation))}' displayLocation='{PortLogField(displayLocation)}' type='{PortLogField(observation.GoblinType)}' source='{PortLogField(source)}' evidenceSource='{PortLogField(observation.Source)}' evidenceHash={evidenceHash} total={total}");
            DateTime countAcceptedUtc = nowUtc;
            AppLogger.Info(
                "GoblinLatencyTrace: " +
                "stage=CountAccepted; " +
                $"evidenceDetectedUtc={observation.TimestampUtc:O}; " +
                $"countAcceptedUtc={countAcceptedUtc:O}; " +
                $"detectionToCountMs={Math.Max(0, (countAcceptedUtc - observation.TimestampUtc).TotalMilliseconds):0.0}; " +
                $"source={PortLogField(observation.Source)}; " +
                $"goblinType={PortLogField(observation.GoblinType)}; " +
                $"areaKey={PortLogField(areaKey)}; " +
                $"evidenceReliability={PortLogField(evidenceReliability)}; " +
                $"reason=Eligible; " +
                $"total={total}");
            PortWriteGoblinTrackerJsonEvent(
                "GoblinAutoCountAccepted",
                new Dictionary<string, object?>
                {
                    ["source"] = observation.Source,
                    ["goblinType"] = observation.GoblinType,
                    ["areaKey"] = areaKey,
                    ["displayLocation"] = displayLocation,
                    ["firstSeenArea"] = firstSeenArea,
                    ["currentAreaAtDetection"] = currentAreaAtAcceptance,
                    ["currentAreaAtAcceptance"] = currentAreaAtAcceptance,
                    ["staleArea"] = staleArea,
                    ["acceptedArea"] = areaKey,
                    ["notificationDisplayArea"] = displayLocation,
                    ["titleResolverOverride"] = titleResolverOverride,
                    ["notificationFreshnessState"] = "Current",
                    ["areaChangedDuringPendingEvidence"] = areaChangedDuringPendingEvidence,
                    ["areaCount"] = guardResult.AreaCount,
                    ["areaLimit"] = guardResult.AreaLimit,
                    ["reason"] = "Eligible",
                    ["evidenceReliability"] = evidenceReliability,
                    ["reliabilityReason"] = reliabilityReason,
                    ["evidenceAgeSeconds"] = evidenceAgeSeconds,
                    ["evidenceFirstSeenAgeSeconds"] = evidenceFirstSeenAgeSeconds,
                    ["encounterAgeSeconds"] = encounterAgeSeconds,
                    ["encounterMatch"] = encounterSuppressionMatch,
                    ["autoArmedAgeSeconds"] = autoArmedAgeSeconds,
                    ["evidenceConfidence"] = observation.EvidenceConfidence,
                    ["evidenceHash"] = evidenceHash,
                    ["total"] = total,
                    ["enableObservationMode"] = AppSettings.GoblinTracker.EnableObservationMode,
                    ["enableAutomaticCounting"] = AppSettings.GoblinTracker.EnableAutomaticCounting,
                });
            PortPublishAcceptedGoblinCountObservation(area, observation.GoblinType, observation.Source, "AutomaticCountAccepted", guardResult);
            PortCaptureAcceptedGoblinEvidenceBestSamples(
                observation,
                area,
                rankedSamples,
                nowUtc,
                autoEvidenceKey,
                evidenceReliability,
                reliabilityReason,
                total);
            if (total <= 0)
            {
                AppLogger.Info($"GoblinTracker: AutoCountNotificationSkipped reason=InvalidTotal source={PortLogField(observation.Source)} goblinType={PortLogField(observation.GoblinType)} areaKey={areaKey} total={total} evidenceHash={PortGoblinEvidenceHash(autoEvidenceKey)}");
                PortWriteSessionMetadata(logSuccess: false);
                PortUpdateGoblinTrackerStats();
                return true;
            }

            DateTime notificationQueuedUtc = DateTime.UtcNow;
            if (GoblinTypeNormalizer.Normalize(observation.GoblinType).Equals("Rainbow Goblin", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    System.Media.SystemSounds.Exclamation.Play();
                }
                catch (Exception ex)
                {
                    AppLogger.Info($"Rainbow goblin alert sound failed: {ex.Message}");
                }

                PortShowSplash(
                    $"RAINBOW GOBLIN!\r\n{displayLocation}\r\nTotal: {total}",
                    7000,
                    "GoblinAutoCount",
                    observation.TimestampUtc,
                    countAcceptedUtc,
                    notificationQueuedUtc,
                    observation.GoblinType,
                    areaKey,
                    observation.Source,
                    firstSeenArea,
                    areaKey,
                    currentAreaAtAcceptance,
                    evidenceHash,
                    "Current");
            }
            else
            {
                PortShowSplash(
                    $"Goblin auto-counted\r\n{displayLocation}\r\nType: {observation.GoblinType}\r\nTotal: {total}",
                    5000,
                    "GoblinAutoCount",
                    observation.TimestampUtc,
                    countAcceptedUtc,
                    notificationQueuedUtc,
                    observation.GoblinType,
                    areaKey,
                    observation.Source,
                    firstSeenArea,
                    areaKey,
                    currentAreaAtAcceptance,
                    evidenceHash,
                    "Current");
            }
            PortQueueGoblinEncounterDebugCapture(source, observation.Source, observation.GoblinType, areaKey, displayLocation, total);
            PortWriteSessionMetadata(logSuccess: false);
            PortUpdateGoblinTrackerStats();
            return true;
        }

        private void PortCaptureAcceptedGoblinEvidenceBestSamples(
            GoblinObservationRecord observation,
            GoblinAreaResolution area,
            IReadOnlyList<ImageRecognitionSampleCandidate>? rankedSamples,
            DateTime acceptedUtc,
            string autoEvidenceKey,
            string evidenceReliability,
            string reliabilityReason,
            int total)
        {
            if (!AppSettings.ImageRecognition.CaptureAcceptedTopCandidates ||
                (!AppSettings.IsVsDebugProfile && !AppSettings.Debug.DebugMode))
            {
                return;
            }

            string evidenceHash = PortGoblinEvidenceHash(autoEvidenceKey);
            string actionId = $"goblin-{acceptedUtc:yyyyMMddHHmmssfff}-{evidenceHash}";
            bool promotionEnabled = AppSettings.GoblinTracker.PromoteBestGoblinEvidenceImage &&
                (AppSettings.IsVsDebugProfile || AppSettings.Debug.DebugMode);
            ImageRecognitionBestSamplePromoter.CaptureSelectAndPromote(new ImageRecognitionSamplePromotionRequest(
                "GoblinEvidence",
                actionId,
                acceptedUtc.ToLocalTime().ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                DebugManager.GoblinEvidenceAcceptedCandidatesDirectory,
                Path.Combine(PortGoblinEvidenceTemplateDirectory(), "Promoted"),
                CaptureEnabled: true,
                PromotionEnabled: promotionEnabled,
                TopCandidateCount: 3,
                RetentionCount: AppSettings.ImageRecognition.TopCandidateRetentionCount,
                Candidates: rankedSamples ?? [],
                Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["GoblinType"] = observation.GoblinType,
                    ["DetectedArea"] = area.AreaKey,
                    ["DisplayLocation"] = area.DisplayLocation,
                    ["EvidenceSource"] = observation.Source,
                    ["AcceptedReason"] = "Eligible",
                    ["EvidenceHash"] = evidenceHash,
                    ["EvidenceReliability"] = evidenceReliability,
                    ["ReliabilityReason"] = reliabilityReason,
                    ["Total"] = total.ToString(CultureInfo.InvariantCulture),
                }));
        }

        private bool PortTryApplyRecentMinimapJournalConfirmationReliability(
            GoblinObservationRecord observation,
            string areaKey,
            string evidenceSignature,
            DateTime nowUtc,
            out string reliabilityReason,
            out string evidenceReliability)
        {
            reliabilityReason = "";
            evidenceReliability = "";
            if (!PortJournalEngagedHasRecentMinimapConfirmation(
                observation,
                areaKey,
                evidenceSignature,
                nowUtc,
                out GoblinObservationRecord recentMinimapConfirmation,
                out double recentMinimapConfirmationAgeSeconds))
            {
                return false;
            }

            evidenceReliability = "JournalEngagedRecentMinimapConfirmation";
            AppLogger.Info(
                "GoblinTracker: JournalEngagedRecentMinimapConfirmation " +
                $"areaKey={PortLogField(PortDisplayLocation(areaKey))} " +
                $"source={PortLogField(observation.Source)} " +
                $"goblinType={PortLogField(observation.GoblinType)} " +
                $"journalConfidence={observation.EvidenceConfidence:0.000} " +
                $"recentMinimapConfidence={recentMinimapConfirmation.EvidenceConfidence:0.000} " +
                $"recentMinimapAgeSeconds={recentMinimapConfirmationAgeSeconds:0.0} " +
                $"recentMinimapWindowSeconds={PortAutomaticGoblinRecentMinimapJournalConfirmationWindow.TotalSeconds:0} " +
                $"recentMinimapMinConfidence={PortAutomaticGoblinRecentMinimapJournalConfirmationMinimumConfidence:0.000} " +
                "continuesThroughDuplicateGuard=True");
            return true;
        }

        private static bool PortObservationPendingJournalPromotedByReliability(
            GoblinObservationRecord observation,
            bool evidenceReliabilityAllowsCount,
            string evidenceReliability)
        {
            return evidenceReliabilityAllowsCount &&
                observation.Source.Contains("Journal", StringComparison.OrdinalIgnoreCase) &&
                observation.Reason.Equals(GoblinAutoCountEvidenceReliabilityPolicy.JournalPendingKilledOrMinimapConfirmation, StringComparison.OrdinalIgnoreCase) &&
                (evidenceReliability.Equals(GoblinAutoCountEvidenceReliabilityPolicy.JournalEngagedHighConfidenceFreshCombat, StringComparison.OrdinalIgnoreCase) ||
                evidenceReliability.Equals(GoblinAutoCountEvidenceReliabilityPolicy.JournalEngagedSustainedActiveCombat, StringComparison.OrdinalIgnoreCase) ||
                evidenceReliability.Equals("JournalEngagedRecentMinimapConfirmation", StringComparison.OrdinalIgnoreCase));
        }

        private bool PortJournalEngagedHasRecentMinimapConfirmation(
            GoblinObservationRecord observation,
            string areaKey,
            string evidenceSignature,
            DateTime nowUtc,
            out GoblinObservationRecord recentMinimapConfirmation,
            out double recentMinimapConfirmationAgeSeconds)
        {
            recentMinimapConfirmation = default!;
            recentMinimapConfirmationAgeSeconds = -1;
            if (!observation.Source.Contains("Journal", StringComparison.OrdinalIgnoreCase) ||
                !evidenceSignature.Contains("Kind=JournalEngaged", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return PortTryGetRecentMinimapJournalConfirmation(
                observation.GoblinType,
                areaKey,
                nowUtc,
                out recentMinimapConfirmation,
                out recentMinimapConfirmationAgeSeconds);
        }

        private void PortLogGoblinDecisionTrace(GoblinDecisionTraceRecord trace)
        {
            AppLogger.Info(GoblinDecisionTracePolicy.ToLogLine(trace));
        }

        private bool PortShouldWriteGoblinDecisionBundle(GoblinDecisionTraceRecord trace, out string skipReason)
        {
            skipReason = "";
            if (trace.NotificationShown || trace.Decision.Equals("Count", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string throttleKey = PortGoblinDecisionBundleThrottleKey(trace);
            DateTime nowUtc = DateTime.UtcNow;
            lock (portGoblinTrackerLock)
            {
                if (portGoblinDecisionBundleLastSavedByKey.TryGetValue(throttleKey, out DateTime lastSavedUtc) &&
                    nowUtc - lastSavedUtc < PortGoblinDecisionBundleSuppressionThrottleWindow)
                {
                    skipReason = "ThrottledDuplicateDecision";
                    return false;
                }

                portGoblinDecisionBundleLastSavedByKey[throttleKey] = nowUtc;
            }

            return true;
        }

        private static string PortGoblinDecisionBundleThrottleKey(GoblinDecisionTraceRecord trace)
        {
            return string.Join("|",
                trace.Decision,
                trace.Reason,
                trace.Source,
                GoblinTypeNormalizer.Normalize(trace.GoblinType),
                GoblinAreaResolver.NormalizedKey(trace.AreaKey),
                PortGoblinEvidenceHash(trace.EvidenceSignature));
        }

        private void PortWriteGoblinDecisionBundle(GoblinDecisionTraceRecord trace)
        {
            try
            {
                string root = Path.Combine(DebugManager.GoblinEvidenceDirectory, "DecisionBundles");
                string safeCorrelationId = System.Text.RegularExpressions.Regex.Replace(trace.CorrelationId, @"[^A-Za-z0-9_.-]+", "_");
                if (string.IsNullOrWhiteSpace(safeCorrelationId))
                {
                    safeCorrelationId = $"gdt-{DateTime.Now:yyyyMMddHHmmssfff}";
                }

                string bundleDirectory = Path.Combine(root, safeCorrelationId);
                Directory.CreateDirectory(bundleDirectory);
                DateTime createdLocal = DateTime.Now;
                string replayPrefix = $"decision_{safeCorrelationId}";
                string tracePath = Path.Combine(bundleDirectory, "decision-trace.txt");
                string metadataPath = Path.Combine(bundleDirectory, $"{replayPrefix}_Metadata.txt");
                string minimapPath = Path.Combine(bundleDirectory, $"{replayPrefix}_Minimap.png");
                string journalPath = Path.Combine(bundleDirectory, $"{replayPrefix}_Journal.png");
                string savedMinimapPath = PortCaptureGoblinEncounterRegionCrop("DecisionBundleMinimap", PortGoblinEvidenceMinimapRegion(), minimapPath, createdLocal);
                string savedJournalPath = PortCaptureGoblinEncounterRegionCrop("DecisionBundleJournal", PortGoblinEvidenceJournalRegion(), journalPath, createdLocal);

                File.WriteAllLines(tracePath,
                [
                    GoblinDecisionTracePolicy.ToLogLine(trace),
                    $"createdLocal={createdLocal:O}",
                    $"createdUtc={createdLocal.ToUniversalTime():O}",
                    $"originalEvidencePath={trace.ImagePath}",
                    $"metadataPath={metadataPath}",
                    $"minimapPath={savedMinimapPath}",
                    $"journalPath={savedJournalPath}",
                    "fullImageCopied=False",
                    "fullImagePolicy=DisabledByDefault",
                ]);

                File.WriteAllLines(metadataPath,
                [
                    "Goblin Decision Bundle Capture",
                    "Purpose=Replay-ready decision evidence; full-size evidence images are disabled by default",
                    $"CreatedLocal={createdLocal:O}",
                    $"CreatedUtc={createdLocal.ToUniversalTime():O}",
                    $"CorrelationId={trace.CorrelationId}",
                    $"Mode={trace.Mode}",
                    $"Source={trace.Source}",
                    $"GoblinType={trace.GoblinType}",
                    $"AreaKey={trace.AreaKey}",
                    $"DisplayLocation={trace.AreaKey}",
                    $"AreaRaw={trace.AreaRaw}",
                    $"Decision={trace.Decision}",
                    $"Reason={trace.Reason}",
                    $"Counted={trace.NotificationShown}",
                    $"EvidenceSignature={trace.EvidenceSignature}",
                    $"EvidenceAgeSeconds={trace.EvidenceAgeSeconds:0.000}",
                    $"EvidenceFirstSeenAgeSeconds={trace.EvidenceFirstSeenAgeSeconds:0.000}",
                    $"AreaCountBefore={trace.AreaCountBefore}",
                    $"AreaLimit={trace.AreaLimit}",
                    $"TotalBefore={trace.TotalGoblinCountBefore}",
                    $"OriginalEvidencePath={trace.ImagePath}",
                    $"MinimapPath={savedMinimapPath}",
                    $"JournalPath={savedJournalPath}",
                    $"MinimapReferenceRegion={FormatRectangle(PortGoblinEvidenceMinimapRegion())}",
                    $"JournalReferenceRegion={FormatRectangle(PortGoblinEvidenceJournalRegion())}",
                    "FullImageCopied=False",
                    "FullImagePolicy=DisabledByDefault",
                ]);

                bool replayReady = !string.IsNullOrWhiteSpace(savedMinimapPath) || !string.IsNullOrWhiteSpace(savedJournalPath);
                AppLogger.Info(
                    "GoblinDecisionBundleSaved: " +
                    $"correlationId={PortLogField(trace.CorrelationId)}; " +
                    $"bundleDirectory={PortLogField(bundleDirectory)}; " +
                    $"tracePath={PortLogField(tracePath)}; " +
                    $"metadataPath={PortLogField(metadataPath)}; " +
                    $"minimapPath={PortLogField(savedMinimapPath)}; " +
                    $"journalPath={PortLogField(savedJournalPath)}; " +
                    "imageCopied=False; " +
                    "fullImagePolicy=DisabledByDefault; " +
                    $"replayReady={replayReady}; " +
                    $"sourceImagePath={PortLogField(trace.ImagePath)}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Goblin decision bundle failed: correlationId={trace.CorrelationId}", ex);
            }
        }

        private bool PortShouldSuppressEncounterAlreadyAutoCounted(
            GoblinObservationRecord observation,
            GoblinAreaResolution area,
            string globalEvidenceKey,
            PortGoblinAutoCountEncounterState? encounterState,
            DateTime nowUtc,
            out string matchReason)
        {
            return PortShouldSuppressEncounterAlreadyAutoCounted(
                observation.Source,
                observation.GoblinType,
                area.AreaKey,
                globalEvidenceKey,
                encounterState,
                nowUtc,
                out matchReason);
        }

        private bool PortShouldSuppressEncounterAlreadyAutoCounted(
            string source,
            string goblinType,
            string areaKey,
            string globalEvidenceKey,
            PortGoblinAutoCountEncounterState? encounterState,
            DateTime nowUtc,
            out string matchReason)
        {
            matchReason = "";
            if (encounterState == null ||
                string.IsNullOrWhiteSpace(areaKey) ||
                !GoblinTypeNormalizer.Normalize(encounterState.GoblinType).Equals(GoblinTypeNormalizer.Normalize(goblinType), StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(encounterState.AreaKey))
            {
                return false;
            }

            return GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress(
                source,
                goblinType,
                areaKey,
                globalEvidenceKey,
                encounterState.GoblinType,
                encounterState.AreaKey,
                encounterState.Source,
                encounterState.EvidenceKey,
                encounterState.CountedUtc,
                encounterState.LastSeenUtc,
                nowUtc,
                PortAutomaticGoblinJournalEncounterSuppressWindow,
                PortAutomaticGoblinSourceVariantSuppressWindow,
                out matchReason);
        }

        private static bool PortShouldBypassFreshCrossAreaJournalDuplicateSuppression(
            GoblinObservationRecord observation,
            string areaKey,
            string globalEvidenceKey,
            PortGoblinAutoCountEncounterState? encounterState,
            string matchReason,
            double evidenceFirstSeenAgeSeconds)
        {
            if (encounterState == null ||
                string.IsNullOrWhiteSpace(areaKey) ||
                string.IsNullOrWhiteSpace(encounterState.AreaKey) ||
                !PortNormalizeGoblinObservationSource(observation.Source).Equals("Journal", StringComparison.OrdinalIgnoreCase) ||
                !GoblinTypeNormalizer.Normalize(encounterState.GoblinType).Equals(GoblinTypeNormalizer.Normalize(observation.GoblinType), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (GoblinAreaResolver.NormalizedKey(areaKey).Equals(
                GoblinAreaResolver.NormalizedKey(encounterState.AreaKey),
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool sameVisibleJournalRowSuppression = matchReason.StartsWith("JournalLineBucket", StringComparison.OrdinalIgnoreCase) ||
                matchReason.StartsWith("SameEvidenceKey", StringComparison.OrdinalIgnoreCase);
            if (!sameVisibleJournalRowSuppression ||
                !globalEvidenceKey.Contains("JournalEncounter", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            const double minimumNewAreaEvidenceAgeSeconds = 1.0;
            const double maximumFreshBypassAgeSeconds = 12.0;
            const double minimumFreshJournalConfidence = 0.95;
            return evidenceFirstSeenAgeSeconds >= minimumNewAreaEvidenceAgeSeconds &&
                evidenceFirstSeenAgeSeconds <= maximumFreshBypassAgeSeconds &&
                observation.EvidenceConfidence >= minimumFreshJournalConfidence;
        }

        private static bool PortIsGoblinObservationEvidenceSource(string source)
        {
            string normalizedSource = PortNormalizeGoblinObservationSource(source);
            return normalizedSource.Equals("Journal", StringComparison.OrdinalIgnoreCase) ||
                normalizedSource.Equals("Minimap", StringComparison.OrdinalIgnoreCase);
        }

        private static bool PortJournalEvidenceBucketsMatch(string currentEvidenceKey, string countedEvidenceKey, out int currentBucket, out int countedBucket)
        {
            return GoblinAutoCountEncounterSuppressionPolicy.JournalEvidenceBucketsMatch(
                currentEvidenceKey,
                countedEvidenceKey,
                out currentBucket,
                out countedBucket);
        }

        private static bool PortTryParseJournalEvidenceLineBucket(string evidenceKey, out int lineBucket)
        {
            lineBucket = -1;
            if (string.IsNullOrWhiteSpace(evidenceKey))
            {
                return false;
            }

            foreach (string part in evidenceKey.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                const string prefix = "LineBucket=";
                if (part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(part[prefix.Length..], out int parsed))
                {
                    lineBucket = parsed;
                    return true;
                }
            }

            return false;
        }

        private static string PortGoblinEvidenceHash(string evidenceKey)
        {
            if (string.IsNullOrWhiteSpace(evidenceKey))
            {
                return "Unknown";
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(evidenceKey);
            byte[] hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToHexString(hash, 0, Math.Min(6, hash.Length));
        }

        private string PortGoblinAutoCountEvidenceKey(string evidenceSignature, GoblinObservationRecord observation)
        {
            string normalizedSignature = string.IsNullOrWhiteSpace(evidenceSignature)
                ? ""
                : evidenceSignature.Trim();
            if (string.IsNullOrWhiteSpace(normalizedSignature))
            {
                normalizedSignature = string.Join("|",
                    observation.Source,
                    observation.GoblinType,
                    observation.AreaKey,
                    observation.TimestampUtc.Ticks);
            }

            return string.Join("|",
                PortNormalizeGoblinObservationSource(observation.Source),
                GoblinTypeNormalizer.Normalize(observation.GoblinType),
                GoblinAreaResolver.NormalizedKey(observation.AreaKey),
                normalizedSignature);
        }

        private string PortGoblinAutoCountGlobalEvidenceKey(string evidenceSignature, string source, string goblinType)
        {
            string normalizedSignature = string.IsNullOrWhiteSpace(evidenceSignature)
                ? ""
                : evidenceSignature.Trim();
            if (string.IsNullOrWhiteSpace(normalizedSignature))
            {
                return "";
            }

            return string.Join("|",
                PortNormalizeGoblinObservationSource(source),
                GoblinTypeNormalizer.Normalize(goblinType),
                normalizedSignature);
        }

        private string PortGoblinAutoCountEncounterKey(GoblinObservationRecord observation)
        {
            return PortGoblinAutoCountEncounterKey(observation.GoblinType);
        }

        private string PortGoblinAutoCountEncounterKey(string goblinType)
        {
            string normalizedGoblinType = GoblinTypeNormalizer.Normalize(goblinType);
            return string.IsNullOrWhiteSpace(normalizedGoblinType) || normalizedGoblinType.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
                ? ""
                : normalizedGoblinType;
        }

        private static double PortAutomaticGoblinMinimapCountMinimumConfidenceFor(string goblinType)
        {
            string normalized = GoblinTypeNormalizer.Normalize(goblinType);
            return normalized.Equals("Gilded Baron", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Malevolent Tormentor", StringComparison.OrdinalIgnoreCase)
                ? PortAutomaticGoblinAmbiguousMinimapCountMinimumConfidence
                : PortAutomaticGoblinMinimapCountMinimumConfidence;
        }

        private static string PortShortEvidenceSignature(string evidenceSignature)
        {
            if (string.IsNullOrWhiteSpace(evidenceSignature))
            {
                return "";
            }

            return evidenceSignature.Length <= 160
                ? evidenceSignature
                : evidenceSignature[..160];
        }

        private void PortResetGoblinAutoCountEvidenceState(string reason)
        {
            int cleared;
            int encountersCleared;
            int decisionBundleThrottleCleared;
            lock (portGoblinTrackerLock)
            {
                cleared = portGoblinAutoCountEvidenceBySignature.Count;
                encountersCleared = portGoblinAutoCountEncounterByGoblinType.Count;
                decisionBundleThrottleCleared = portGoblinDecisionBundleLastSavedByKey.Count;
                portGoblinAutoCountEvidenceBySignature.Clear();
                portGoblinAutoCountEncounterByGoblinType.Clear();
                portGoblinDecisionBundleLastSavedByKey.Clear();
            }

            AppLogger.Info($"GoblinTracker: Auto-count evidence state reset reason='{PortLogField(reason)}' clearedEvidenceSignatures={cleared} clearedEncounterSignatures={encountersCleared} clearedDecisionBundleThrottleKeys={decisionBundleThrottleCleared}");
        }

        private void PortSetGoblinAutomaticCountingArmedState(string reason)
        {
            DateTime armedAtUtc = PortGoblinAutomaticCountingEnabled()
                ? DateTime.UtcNow
                : DateTime.MinValue;
            lock (portGoblinTrackerLock)
            {
                portGoblinAutomaticCountingArmedAtUtc = armedAtUtc;
            }

            AppLogger.Info(
                "GoblinTracker: Automatic counting armed state changed: " +
                $"reason={PortLogField(reason)}; " +
                $"enableObservationMode={AppSettings.GoblinTracker.EnableObservationMode}; " +
                $"enableAutomaticCounting={AppSettings.GoblinTracker.EnableAutomaticCounting}; " +
                $"automaticCountingEnabled={PortGoblinAutomaticCountingEnabled()}; " +
                $"armedAtUtc={(armedAtUtc == DateTime.MinValue ? "Disabled" : armedAtUtc.ToString("O"))}");
        }
    }
}

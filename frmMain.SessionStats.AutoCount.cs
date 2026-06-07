namespace GoblinFarmer
{
    public partial class frmMain
    {
        private bool PortTryRecordAutomaticGoblinCount(
            GoblinObservationRecord observation,
            GoblinAreaResolution area,
            string evidenceSignature,
            string evidenceImagePath)
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
            int total = 0;
            PortGoblinAutoCountEvidenceState? evidenceState = null;
            PortGoblinAutoCountEncounterState? encounterState = null;
            double evidenceFirstSeenAgeSeconds = 0;
            double encounterAgeSeconds = -1;
            string encounterAreaKey = "";
            double autoArmedAgeSeconds = portGoblinAutomaticCountingArmedAtUtc == DateTime.MinValue
                ? -1
                : Math.Max(0, (nowUtc - portGoblinAutomaticCountingArmedAtUtc).TotalSeconds);
            int totalGoblinCountBefore = DebugManager.Session.Snapshot(DateTime.Now).GoblinCount;

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
                    encounterState = encounterState with
                    {
                        LastSeenUtc = nowUtc,
                        GoblinType = observation.GoblinType,
                        Source = observation.Source,
                    };
                    portGoblinAutoCountEncounterByGoblinType[autoEncounterKey] = encounterState;
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
                    suppressionReason = "EvidenceAlreadyAutoCounted";
                }
                else if (string.IsNullOrWhiteSpace(suppressionReason) &&
                    PortShouldSuppressEncounterAlreadyAutoCounted(observation, area, globalEvidenceKey, encounterState, nowUtc, out encounterSuppressionMatch))
                {
                    suppressionReason = "EncounterAlreadyAutoCounted";
                }
                else if (string.IsNullOrWhiteSpace(suppressionReason) &&
                    minimapAutoCountConfidencePending)
                {
                    suppressionReason = "MinimapConfidencePendingJournal";
                }
                else if (string.IsNullOrWhiteSpace(suppressionReason) &&
                    !GoblinJournalFreshnessPolicy.IsFresh(evidenceState!.FirstSeenUtc, nowUtc, GoblinJournalEvidenceFreshWindow))
                {
                    suppressionReason = "StaleEvidence";
                }
                else if (string.IsNullOrWhiteSpace(suppressionReason) && !observation.WouldCount)
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
            }

            bool counted = string.IsNullOrWhiteSpace(suppressionReason);
            int areaCountBefore = counted ? Math.Max(0, guardResult.AreaCount - 1) : guardResult.AreaCount;
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
                PortWriteGoblinDecisionBundle(trace);
            }

            if (!string.IsNullOrWhiteSpace(suppressionReason))
            {
                string eventName = autoCountingEnabled
                    ? "GoblinAutoCountSuppressed"
                    : "GoblinAutoCountSkippedDisabled";
                AppLogger.Info($"GoblinTracker: {eventName} source={PortLogField(observation.Source)} goblinType={PortLogField(observation.GoblinType)} areaKey={areaKey} displayLocation={displayLocation} areaCount={guardResult.AreaCount} areaLimit={guardResult.AreaLimit} reason={PortLogField(suppressionReason)} evidenceAgeSeconds={evidenceAgeSeconds:0.0} evidenceFirstSeenAgeSeconds={evidenceFirstSeenAgeSeconds:0.0} evidenceFirstSeenUtc={(evidenceState == null ? "Unknown" : evidenceState.FirstSeenUtc.ToString("O"))} evidenceLastSeenUtc={(evidenceState == null ? "Unknown" : evidenceState.LastSeenUtc.ToString("O"))} encounterAgeSeconds={encounterAgeSeconds:0.0} encounterAreaKey={PortLogField(PortDisplayLocation(encounterAreaKey))} encounterCountedUtc={(encounterState == null ? "Unknown" : encounterState.CountedUtc.ToString("O"))} encounterLastSeenUtc={(encounterState == null ? "Unknown" : encounterState.LastSeenUtc.ToString("O"))} encounterMatch={PortLogField(encounterSuppressionMatch)} autoArmedAgeSeconds={autoArmedAgeSeconds:0.0} evidenceConfidence={observation.EvidenceConfidence:0.000} minimapAutoCountMinConfidence={minimapAutoCountMinimumConfidence:0.000} evidenceHash={PortGoblinEvidenceHash(autoEvidenceKey)} evidenceSignature={PortLogField(PortShortEvidenceSignature(autoEvidenceKey))} enableObservationMode={AppSettings.GoblinTracker.EnableObservationMode} enableAutomaticCounting={AppSettings.GoblinTracker.EnableAutomaticCounting}");
                PortWriteGoblinTrackerJsonEvent(
                    eventName,
                    new Dictionary<string, object?>
                    {
                        ["source"] = observation.Source,
                        ["goblinType"] = observation.GoblinType,
                        ["areaKey"] = areaKey,
                        ["displayLocation"] = displayLocation,
                        ["areaCount"] = guardResult.AreaCount,
                        ["areaLimit"] = guardResult.AreaLimit,
                        ["reason"] = suppressionReason,
                        ["evidenceAgeSeconds"] = evidenceAgeSeconds,
                        ["evidenceFirstSeenAgeSeconds"] = evidenceFirstSeenAgeSeconds,
                        ["encounterAgeSeconds"] = encounterAgeSeconds,
                        ["encounterAreaKey"] = PortDisplayLocation(encounterAreaKey),
                        ["encounterMatch"] = encounterSuppressionMatch,
                        ["autoArmedAgeSeconds"] = autoArmedAgeSeconds,
                        ["evidenceConfidence"] = observation.EvidenceConfidence,
                        ["evidenceHash"] = PortGoblinEvidenceHash(autoEvidenceKey),
                        ["enableObservationMode"] = AppSettings.GoblinTracker.EnableObservationMode,
                        ["enableAutomaticCounting"] = AppSettings.GoblinTracker.EnableAutomaticCounting,
                    });
                return false;
            }

            AppLogger.Info($"GoblinTracker: GoblinAutoCountAccepted source={PortLogField(observation.Source)} goblinType={PortLogField(observation.GoblinType)} areaKey={areaKey} displayLocation={displayLocation} areaCount={guardResult.AreaCount} areaLimit={guardResult.AreaLimit} reason=Eligible evidenceAgeSeconds={evidenceAgeSeconds:0.0} evidenceFirstSeenAgeSeconds={evidenceFirstSeenAgeSeconds:0.0} evidenceFirstSeenUtc={(evidenceState == null ? "Unknown" : evidenceState.FirstSeenUtc.ToString("O"))} evidenceLastSeenUtc={(evidenceState == null ? "Unknown" : evidenceState.LastSeenUtc.ToString("O"))} encounterAgeSeconds={encounterAgeSeconds:0.0} encounterMatch={PortLogField(encounterSuppressionMatch)} autoArmedAgeSeconds={autoArmedAgeSeconds:0.0} evidenceConfidence={observation.EvidenceConfidence:0.000} minimapAutoCountMinConfidence={minimapAutoCountMinimumConfidence:0.000} evidenceHash={PortGoblinEvidenceHash(autoEvidenceKey)} evidenceSignature={PortLogField(PortShortEvidenceSignature(autoEvidenceKey))} total={total} enableObservationMode={AppSettings.GoblinTracker.EnableObservationMode} enableAutomaticCounting={AppSettings.GoblinTracker.EnableAutomaticCounting}");
            AppLogger.Info($"GoblinTracker: GoblinCountAccepted areaKey={areaKey} areaCount={guardResult.AreaCount} areaLimit={guardResult.AreaLimit} blockListStatus=Allowed countResult=Accepted rawLocation='{PortLogField(PortDisplayLocation(area.RawLocation))}' displayLocation='{PortLogField(displayLocation)}' type='{PortLogField(observation.GoblinType)}' source='{PortLogField(source)}' evidenceSource='{PortLogField(observation.Source)}' evidenceHash={PortGoblinEvidenceHash(autoEvidenceKey)} total={total}");
            PortWriteGoblinTrackerJsonEvent(
                "GoblinAutoCountAccepted",
                new Dictionary<string, object?>
                {
                    ["source"] = observation.Source,
                    ["goblinType"] = observation.GoblinType,
                    ["areaKey"] = areaKey,
                    ["displayLocation"] = displayLocation,
                    ["areaCount"] = guardResult.AreaCount,
                    ["areaLimit"] = guardResult.AreaLimit,
                    ["reason"] = "Eligible",
                    ["evidenceAgeSeconds"] = evidenceAgeSeconds,
                    ["evidenceFirstSeenAgeSeconds"] = evidenceFirstSeenAgeSeconds,
                    ["encounterAgeSeconds"] = encounterAgeSeconds,
                    ["encounterMatch"] = encounterSuppressionMatch,
                    ["autoArmedAgeSeconds"] = autoArmedAgeSeconds,
                    ["evidenceConfidence"] = observation.EvidenceConfidence,
                    ["evidenceHash"] = PortGoblinEvidenceHash(autoEvidenceKey),
                    ["total"] = total,
                    ["enableObservationMode"] = AppSettings.GoblinTracker.EnableObservationMode,
                    ["enableAutomaticCounting"] = AppSettings.GoblinTracker.EnableAutomaticCounting,
                });
            PortShowSplash($"Goblin auto-counted\r\n{displayLocation}\r\nType: {observation.GoblinType}\r\nTotal: {total}", 5000);
            PortQueueGoblinEncounterDebugCapture(source, observation.Source, observation.GoblinType, areaKey, displayLocation, total);
            PortWriteSessionMetadata(logSuccess: false);
            PortUpdateGoblinTrackerStats();
            return true;
        }

        private void PortLogGoblinDecisionTrace(GoblinDecisionTraceRecord trace)
        {
            AppLogger.Info(GoblinDecisionTracePolicy.ToLogLine(trace));
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
                string tracePath = Path.Combine(bundleDirectory, "decision-trace.txt");
                File.WriteAllLines(tracePath,
                [
                    GoblinDecisionTracePolicy.ToLogLine(trace),
                    $"createdLocal={DateTime.Now:O}",
                    $"sourceImagePath={trace.ImagePath}",
                ]);

                bool imageCopied = false;
                if (!string.IsNullOrWhiteSpace(trace.ImagePath) && File.Exists(trace.ImagePath))
                {
                    string extension = Path.GetExtension(trace.ImagePath);
                    if (string.IsNullOrWhiteSpace(extension))
                    {
                        extension = ".png";
                    }

                    string imageDestination = Path.Combine(bundleDirectory, $"evidence{extension}");
                    File.Copy(trace.ImagePath, imageDestination, overwrite: true);
                    imageCopied = true;
                }

                AppLogger.Info($"GoblinDecisionBundleSaved: correlationId={PortLogField(trace.CorrelationId)}; bundleDirectory={PortLogField(bundleDirectory)}; tracePath={PortLogField(tracePath)}; imageCopied={imageCopied}; sourceImagePath={PortLogField(trace.ImagePath)}");
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

            TimeSpan encounterAge = nowUtc - encounterState.CountedUtc;
            if (encounterAge > PortAutomaticGoblinJournalEncounterSuppressWindow)
            {
                return false;
            }

            string normalizedSource = PortNormalizeGoblinObservationSource(source);
            string normalizedEncounterSource = PortNormalizeGoblinObservationSource(encounterState.Source);
            if (!PortIsGoblinObservationEvidenceSource(normalizedSource) ||
                !PortIsGoblinObservationEvidenceSource(normalizedEncounterSource))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(globalEvidenceKey) &&
                !string.IsNullOrWhiteSpace(encounterState.EvidenceKey) &&
                string.Equals(encounterState.EvidenceKey, globalEvidenceKey, StringComparison.OrdinalIgnoreCase))
            {
                matchReason = "SameEvidenceKey";
                return true;
            }

            if (normalizedSource.Equals("Journal", StringComparison.OrdinalIgnoreCase) &&
                normalizedEncounterSource.Equals("Journal", StringComparison.OrdinalIgnoreCase) &&
                PortJournalEvidenceBucketsMatch(globalEvidenceKey, encounterState.EvidenceKey, out int currentBucket, out int countedBucket))
            {
                matchReason = $"JournalLineBucket:{currentBucket}->{countedBucket}";
                return true;
            }

            if (encounterAge <= TimeSpan.FromSeconds(20) &&
                (normalizedSource.Equals("Journal", StringComparison.OrdinalIgnoreCase) ||
                normalizedEncounterSource.Equals("Journal", StringComparison.OrdinalIgnoreCase) ||
                !normalizedSource.Equals(normalizedEncounterSource, StringComparison.OrdinalIgnoreCase)))
            {
                matchReason = $"RecentSourceVariant:{normalizedEncounterSource}->{normalizedSource}";
                return true;
            }

            return false;
        }

        private static bool PortIsGoblinObservationEvidenceSource(string source)
        {
            string normalizedSource = PortNormalizeGoblinObservationSource(source);
            return normalizedSource.Equals("Journal", StringComparison.OrdinalIgnoreCase) ||
                normalizedSource.Equals("Minimap", StringComparison.OrdinalIgnoreCase);
        }

        private static bool PortJournalEvidenceBucketsMatch(string currentEvidenceKey, string countedEvidenceKey, out int currentBucket, out int countedBucket)
        {
            currentBucket = -1;
            countedBucket = -1;
            if (!PortTryParseJournalEvidenceLineBucket(currentEvidenceKey, out currentBucket) ||
                !PortTryParseJournalEvidenceLineBucket(countedEvidenceKey, out countedBucket))
            {
                return false;
            }

            return Math.Abs(currentBucket - countedBucket) <= 2;
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
            lock (portGoblinTrackerLock)
            {
                cleared = portGoblinAutoCountEvidenceBySignature.Count;
                encountersCleared = portGoblinAutoCountEncounterByGoblinType.Count;
                portGoblinAutoCountEvidenceBySignature.Clear();
                portGoblinAutoCountEncounterByGoblinType.Clear();
            }

            AppLogger.Info($"GoblinTracker: Auto-count evidence state reset reason='{PortLogField(reason)}' clearedEvidenceSignatures={cleared} clearedEncounterSignatures={encountersCleared}");
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

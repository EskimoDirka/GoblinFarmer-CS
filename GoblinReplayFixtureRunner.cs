using System.Drawing;
using System.Globalization;

namespace GoblinFarmer
{
    internal sealed record GoblinReplayFixture(
        string Name,
        string? JournalPath,
        string? MinimapPath);

    internal sealed record GoblinReplayFixtureCandidate(
        string Source,
        string GoblinType,
        GoblinEvidenceType EvidenceType,
        GoblinEvidenceTemplateKind EvidenceKind,
        string TemplateName,
        string TemplatePath,
        double Confidence,
        double Threshold,
        Point MatchPoint,
        Point ScreenMatchPoint,
        Size TemplateSize,
        bool PassedThreshold);

    internal sealed record GoblinReplayFixtureRunResult(
        string FixtureName,
        string TemplateDirectory,
        IReadOnlyList<GoblinReplayFixtureCandidate> Candidates,
        IReadOnlyList<GoblinEvidenceInvalidTemplate> InvalidTemplates,
        IReadOnlyList<string> LogMessages)
    {
        public bool CandidateFound => Candidates.Any(candidate => candidate.PassedThreshold);
    }

    internal sealed record GoblinReplayFixtureStep(
        string Name,
        GoblinReplayFixture Fixture,
        string AreaKey,
        DateTime TimestampUtc);

    internal sealed record GoblinReplayFixtureStepResult(
        string ScenarioName,
        string StepName,
        string AreaKey,
        string FrameSource,
        bool CandidateFound,
        string CandidateResult,
        string Source,
        string GoblinType,
        string EvidenceSignature,
        string CountDecision,
        string Reason,
        string FreshnessReason,
        bool Counted);

    internal sealed record GoblinReplayFixtureScenarioResult(
        string ScenarioName,
        IReadOnlyList<GoblinReplayFixtureStepResult> Steps,
        IReadOnlyList<string> LogMessages);

    internal sealed record GoblinReplayCaptureFolderStep(
        string Name,
        string CaptureFolderPath,
        string? AreaKeyOverride = null,
        DateTime? TimestampUtcOverride = null);

    internal sealed record GoblinReplayCaptureFolderLoadResult(
        string StepName,
        string CaptureFolderPath,
        bool Loaded,
        string Reason,
        string FixtureName,
        string? JournalPath,
        string? MinimapPath,
        string AreaKey,
        DateTime TimestampUtc,
        IReadOnlyDictionary<string, string> Metadata);

    internal sealed record GoblinReplayCaptureFolderScenarioResult(
        string ScenarioName,
        IReadOnlyList<GoblinReplayCaptureFolderLoadResult> CaptureLoads,
        GoblinReplayFixtureScenarioResult FixtureScenarioResult,
        IReadOnlyList<string> LogMessages)
    {
        public IReadOnlyList<GoblinReplayFixtureStepResult> Steps => FixtureScenarioResult.Steps;
    }

    internal static class GoblinReplayFixtureRunner
    {
        private static readonly TimeSpan ReplayJournalFreshnessWindow = TimeSpan.FromSeconds(45);

        public static GoblinReplayFixtureRunResult RunExplicitFixtureForHarness(
            GoblinReplayFixture fixture,
            string templateDirectory,
            Action<string>? log = null,
            Action<IGoblinEvidenceFrameSource?>? setFrameSourceForReplay = null)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            ArgumentException.ThrowIfNullOrWhiteSpace(templateDirectory);

            List<string> logMessages = [];
            List<GoblinReplayFixtureCandidate> candidates = [];
            GoblinEvidenceTemplateCatalog catalog = GoblinEvidenceTemplateRequirements.DiscoverTemplates(templateDirectory);
            FixtureGoblinEvidenceFrameSource? fixtureFrameSource = null;

            void Emit(string eventName, string details)
            {
                string message = $"{eventName}: mode=ExplicitOnDemand; fixture={LogField(fixture.Name)}; {details}";
                logMessages.Add(message);
                log?.Invoke(message);
                AppLogger.Info(message);
            }

            Emit(
                "GoblinReplayFixtureRunStarted",
                $"templateDirectory={LogField(templateDirectory)}; journalPath={LogField(fixture.JournalPath)}; minimapPath={LogField(fixture.MinimapPath)}; templateCount={catalog.Templates.Count}; invalidTemplateCount={catalog.InvalidTemplates.Count}");

            try
            {
                fixtureFrameSource = FixtureGoblinEvidenceFrameSource.FromJournalAndMinimap(fixture.JournalPath, fixture.MinimapPath);
                setFrameSourceForReplay?.Invoke(fixtureFrameSource);

                foreach (IGrouping<string, GoblinEvidenceTemplateRequirement> sourceGroup in catalog.Templates
                    .GroupBy(template => template.Source, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(group => string.Equals(group.Key, "MinimapCandidate", StringComparison.OrdinalIgnoreCase) ? 0 : 1))
                {
                    string source = sourceGroup.Key;
                    string? sourcePath = SourcePath(fixture, source);
                    if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                    {
                        Emit(
                            "GoblinReplayFixtureSourceSkipped",
                            $"source={LogField(source)}; reason=MissingFixtureFrame; framePath={LogField(sourcePath)}");
                        continue;
                    }

                    IReadOnlyList<GoblinEvidenceTemplateRequirement> templates = sourceGroup.ToList();
                    GoblinEvidenceReplayCandidate? replayCandidate = GoblinEvidenceFrameTemplateMatcher.DetectBestCandidate(
                        fixtureFrameSource,
                        templates,
                        template => Path.Combine(templateDirectory, template.FileName),
                        RegionForSource(source),
                        "ExplicitGoblinReplayFixture");

                    if (replayCandidate == null)
                    {
                        Emit(
                            "GoblinReplayFixtureSourceResult",
                            $"source={LogField(source)}; result=NoCandidate; templateCount={templates.Count}; framePath={LogField(sourcePath)}");
                        continue;
                    }

                    GoblinReplayFixtureCandidate candidate = ToFixtureCandidate(replayCandidate);
                    candidates.Add(candidate);
                    Emit(
                        "GoblinReplayFixtureCandidate",
                        $"source={LogField(candidate.Source)}; goblinType={LogField(candidate.GoblinType)}; evidenceKind={candidate.EvidenceKind}; result={(candidate.PassedThreshold ? "Found" : "BelowThreshold")}; confidence={candidate.Confidence.ToString("0.000", CultureInfo.InvariantCulture)}; threshold={candidate.Threshold.ToString("0.000", CultureInfo.InvariantCulture)}; templateName={LogField(candidate.TemplateName)}; matchPoint={candidate.MatchPoint.X},{candidate.MatchPoint.Y}; screenMatchPoint={candidate.ScreenMatchPoint.X},{candidate.ScreenMatchPoint.Y}");
                }

                Emit(
                    "GoblinReplayFixtureRunCompleted",
                    $"candidateCount={candidates.Count}; passedCandidateCount={candidates.Count(candidate => candidate.PassedThreshold)}; invalidTemplateCount={catalog.InvalidTemplates.Count}");
            }
            finally
            {
                setFrameSourceForReplay?.Invoke(null);
                fixtureFrameSource = null;
                Emit("GoblinReplayFixtureFrameSourceRestored", "target=LiveDefault; restored=True");
            }

            return new GoblinReplayFixtureRunResult(
                fixture.Name,
                templateDirectory,
                candidates,
                catalog.InvalidTemplates,
                logMessages);
        }

        public static GoblinReplayFixtureScenarioResult RunExplicitScenarioForHarness(
            string scenarioName,
            IReadOnlyList<GoblinReplayFixtureStep> steps,
            string templateDirectory,
            Action<string>? log = null,
            Action<IGoblinEvidenceFrameSource?>? setFrameSourceForReplay = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);
            ArgumentNullException.ThrowIfNull(steps);
            ArgumentException.ThrowIfNullOrWhiteSpace(templateDirectory);

            List<GoblinReplayFixtureStepResult> stepResults = [];
            List<string> logMessages = [];
            Dictionary<string, ReplayEvidenceState> evidenceBySignature = new(StringComparer.OrdinalIgnoreCase);
            GoblinAreaDuplicateGuard duplicateGuard = new();

            void Emit(string eventName, string details)
            {
                string message = $"{eventName}: mode=ExplicitOnDemand; scenario={LogField(scenarioName)}; {details}";
                logMessages.Add(message);
                log?.Invoke(message);
                AppLogger.Info(message);
            }

            Emit(
                "GoblinReplayFixtureScenarioStarted",
                $"stepCount={steps.Count}; templateDirectory={LogField(templateDirectory)}");

            foreach (GoblinReplayFixtureStep step in steps)
            {
                GoblinReplayFixtureRunResult fixtureResult = RunExplicitFixtureForHarness(
                    step.Fixture,
                    templateDirectory,
                    message =>
                    {
                        logMessages.Add(message);
                        log?.Invoke(message);
                    },
                    setFrameSourceForReplay);

                GoblinReplayFixtureCandidate? candidate = SelectScenarioCandidate(fixtureResult.Candidates);
                GoblinReplayFixtureStepResult stepResult = EvaluateStep(
                    scenarioName,
                    step,
                    candidate,
                    evidenceBySignature,
                    duplicateGuard);
                stepResults.Add(stepResult);

                Emit(
                    "GoblinReplayFixtureStepResult",
                    $"step={LogField(step.Name)}; frameSource={LogField(stepResult.FrameSource)}; areaKey={LogField(stepResult.AreaKey)}; candidateResult={LogField(stepResult.CandidateResult)}; source={LogField(stepResult.Source)}; goblinType={LogField(stepResult.GoblinType)}; evidenceSignature={LogField(stepResult.EvidenceSignature)}; countDecision={LogField(stepResult.CountDecision)}; reason={LogField(stepResult.Reason)}; staleFreshReason={LogField(stepResult.FreshnessReason)}; counted={stepResult.Counted}");
            }

            Emit(
                "GoblinReplayFixtureScenarioCompleted",
                $"stepCount={stepResults.Count}; countedSteps={stepResults.Count(step => step.Counted)}; suppressedSteps={stepResults.Count(step => !step.Counted)}");

            return new GoblinReplayFixtureScenarioResult(scenarioName, stepResults, logMessages);
        }

        public static GoblinReplayCaptureFolderScenarioResult RunExplicitCaptureFoldersForHarness(
            string scenarioName,
            IReadOnlyList<GoblinReplayCaptureFolderStep> captureSteps,
            string templateDirectory,
            Action<string>? log = null,
            Action<IGoblinEvidenceFrameSource?>? setFrameSourceForReplay = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);
            ArgumentNullException.ThrowIfNull(captureSteps);
            ArgumentException.ThrowIfNullOrWhiteSpace(templateDirectory);

            List<string> logMessages = [];
            List<GoblinReplayCaptureFolderLoadResult> captureLoads = [];
            List<GoblinReplayFixtureStep> fixtureSteps = [];

            void Emit(string eventName, string details)
            {
                string message = $"{eventName}: mode=ExplicitOnDemand; scenario={LogField(scenarioName)}; {details}";
                logMessages.Add(message);
                log?.Invoke(message);
                AppLogger.Info(message);
            }

            Emit(
                "GoblinReplayCaptureFolderScenarioStarted",
                $"captureStepCount={captureSteps.Count}; templateDirectory={LogField(templateDirectory)}");

            foreach (GoblinReplayCaptureFolderStep captureStep in captureSteps)
            {
                GoblinReplayCaptureFolderLoadResult loadResult = LoadExplicitCaptureFolderForHarness(captureStep, Emit);
                captureLoads.Add(loadResult);
                if (!loadResult.Loaded)
                {
                    continue;
                }

                fixtureSteps.Add(new GoblinReplayFixtureStep(
                    loadResult.StepName,
                    new GoblinReplayFixture(loadResult.FixtureName, loadResult.JournalPath, loadResult.MinimapPath),
                    loadResult.AreaKey,
                    loadResult.TimestampUtc));
            }

            GoblinReplayFixtureScenarioResult fixtureScenarioResult = fixtureSteps.Count == 0
                ? new GoblinReplayFixtureScenarioResult(scenarioName, [], [])
                : RunExplicitScenarioForHarness(
                    scenarioName,
                    fixtureSteps,
                    templateDirectory,
                    message =>
                    {
                        logMessages.Add(message);
                        log?.Invoke(message);
                    },
                    setFrameSourceForReplay);

            Emit(
                "GoblinReplayCaptureFolderScenarioCompleted",
                $"loadedSteps={captureLoads.Count(load => load.Loaded)}; skippedSteps={captureLoads.Count(load => !load.Loaded)}; replaySteps={fixtureScenarioResult.Steps.Count}");

            return new GoblinReplayCaptureFolderScenarioResult(
                scenarioName,
                captureLoads,
                fixtureScenarioResult,
                logMessages);
        }

        private static GoblinReplayCaptureFolderLoadResult LoadExplicitCaptureFolderForHarness(
            GoblinReplayCaptureFolderStep captureStep,
            Action<string, string> emit)
        {
            string stepName = string.IsNullOrWhiteSpace(captureStep.Name)
                ? "Unnamed capture step"
                : captureStep.Name.Trim();
            string captureFolderPath = captureStep.CaptureFolderPath ?? "";
            if (string.IsNullOrWhiteSpace(captureFolderPath) || !Directory.Exists(captureFolderPath))
            {
                GoblinReplayCaptureFolderLoadResult missing = EmptyCaptureLoad(
                    stepName,
                    captureFolderPath,
                    "CaptureFolderMissing",
                    captureStep.AreaKeyOverride,
                    captureStep.TimestampUtcOverride);
                emit(
                    "GoblinReplayCaptureFolderSkipped",
                    $"step={LogField(stepName)}; captureFolder={LogField(captureFolderPath)}; reason={missing.Reason}");
                return missing;
            }

            string root = Path.GetFullPath(captureFolderPath);
            Dictionary<string, string> metadata = ReadCaptureMetadata(root);
            string? metadataPath = LatestCaptureMetadataPath(root);
            string? prefix = !string.IsNullOrWhiteSpace(metadataPath)
                ? metadataPath[..^"_Metadata.txt".Length]
                : LatestCapturePrefix(root);
            string? journalPath = ResolveCaptureImagePath(root, prefix, metadata, "JournalPath", "_Journal.png");
            string? minimapPath = ResolveCaptureImagePath(root, prefix, metadata, "MinimapPath", "_Minimap.png");
            if (string.IsNullOrWhiteSpace(journalPath) && string.IsNullOrWhiteSpace(minimapPath))
            {
                GoblinReplayCaptureFolderLoadResult empty = EmptyCaptureLoad(
                    stepName,
                    root,
                    "NoJournalOrMinimapFrame",
                    captureStep.AreaKeyOverride,
                    captureStep.TimestampUtcOverride,
                    metadata);
                emit(
                    "GoblinReplayCaptureFolderSkipped",
                    $"step={LogField(stepName)}; captureFolder={LogField(root)}; reason={empty.Reason}; metadataPath={LogField(metadataPath)}");
                return empty;
            }

            string areaKey = ResolveCaptureAreaKey(captureStep.AreaKeyOverride, metadata);
            DateTime timestampUtc = captureStep.TimestampUtcOverride ??
                ResolveCaptureTimestampUtc(metadata, metadataPath, journalPath, minimapPath);
            string fixtureName = string.IsNullOrWhiteSpace(prefix)
                ? new DirectoryInfo(root).Name
                : Path.GetFileName(prefix);

            GoblinReplayCaptureFolderLoadResult loaded = new(
                stepName,
                root,
                true,
                "Loaded",
                fixtureName,
                journalPath,
                minimapPath,
                areaKey,
                timestampUtc,
                metadata);
            emit(
                "GoblinReplayCaptureFolderLoaded",
                $"step={LogField(stepName)}; captureFolder={LogField(root)}; fixture={LogField(fixtureName)}; areaKey={LogField(areaKey)}; timestampUtc={timestampUtc:O}; journalPath={LogField(journalPath)}; minimapPath={LogField(minimapPath)}; metadataPath={LogField(metadataPath)}");
            return loaded;
        }

        private static GoblinReplayFixtureCandidate ToFixtureCandidate(GoblinEvidenceReplayCandidate replayCandidate)
        {
            GoblinEvidenceTemplateRequirement template = replayCandidate.Template;
            GoblinEvidenceTemplateMatch match = replayCandidate.Match;
            return new GoblinReplayFixtureCandidate(
                template.Source,
                template.GoblinType,
                template.Type,
                template.Kind,
                template.FileName,
                replayCandidate.TemplatePath,
                match.Confidence,
                template.Threshold,
                match.MatchPoint,
                match.ScreenMatchPoint,
                match.TemplateSize,
                replayCandidate.PassedThreshold);
        }

        private static GoblinReplayFixtureStepResult EvaluateStep(
            string scenarioName,
            GoblinReplayFixtureStep step,
            GoblinReplayFixtureCandidate? candidate,
            Dictionary<string, ReplayEvidenceState> evidenceBySignature,
            GoblinAreaDuplicateGuard duplicateGuard)
        {
            if (candidate == null)
            {
                return SuppressedStep(
                    scenarioName,
                    step,
                    "None",
                    "NoCandidate",
                    "",
                    "Unknown",
                    "",
                    "NoCandidate",
                    "NoCandidate");
            }

            if (!candidate.PassedThreshold)
            {
                return SuppressedStep(
                    scenarioName,
                    step,
                    "Fixture",
                    "BelowThreshold",
                    candidate.Source,
                    candidate.GoblinType,
                    EvidenceSignature(candidate),
                    "BelowThreshold",
                    "BelowThreshold");
            }

            string evidenceSignature = EvidenceSignature(candidate);
            if (!evidenceBySignature.TryGetValue(evidenceSignature, out ReplayEvidenceState? state))
            {
                state = new ReplayEvidenceState(
                    step.TimestampUtc,
                    step.TimestampUtc,
                    step.AreaKey,
                    candidate.GoblinType,
                    candidate.Source,
                    false,
                    "");
            }
            else
            {
                state = state with
                {
                    LastSeenUtc = step.TimestampUtc,
                    GoblinType = candidate.GoblinType,
                    Source = candidate.Source,
                };
            }

            evidenceBySignature[evidenceSignature] = state;
            string freshnessReason = FreshnessReason(candidate, state, step);
            if (!freshnessReason.Equals("Fresh", StringComparison.OrdinalIgnoreCase))
            {
                string suppressionReason = SuppressionReasonForFreshness(freshnessReason);
                return SuppressedStep(
                    scenarioName,
                    step,
                    "Fixture",
                    "Found",
                    candidate.Source,
                    candidate.GoblinType,
                    evidenceSignature,
                    suppressionReason,
                    freshnessReason);
            }

            string reason = "";
            if (state.Counted)
            {
                reason = string.Equals(state.CountedAreaKey, step.AreaKey, StringComparison.OrdinalIgnoreCase)
                    ? "EvidenceAlreadyAutoCounted"
                    : "EncounterAlreadyAutoCounted";
            }
            else if (GoblinManualCountBlockList.IsBlocked(step.AreaKey))
            {
                reason = "BlockedArea";
            }
            else if (!duplicateGuard.TryAccept(step.AreaKey, out GoblinAreaDuplicateGuardResult guardResult))
            {
                reason = guardResult.AreaLimit > 1 ? "AreaLimitReached" : "AreaAlreadyCounted";
            }
            else
            {
                evidenceBySignature[evidenceSignature] = state with
                {
                    Counted = true,
                    CountedAreaKey = step.AreaKey,
                };

                return new GoblinReplayFixtureStepResult(
                    scenarioName,
                    step.Name,
                    step.AreaKey,
                    "Fixture",
                    true,
                    "Found",
                    candidate.Source,
                    candidate.GoblinType,
                    evidenceSignature,
                    GoblinDecisionTracePolicy.DecisionForReason("", counted: true),
                    "Eligible",
                    freshnessReason,
                    true);
            }

            return SuppressedStep(
                scenarioName,
                step,
                "Fixture",
                "Found",
                candidate.Source,
                candidate.GoblinType,
                evidenceSignature,
                reason,
                freshnessReason);
        }

        private static GoblinReplayFixtureStepResult SuppressedStep(
            string scenarioName,
            GoblinReplayFixtureStep step,
            string frameSource,
            string candidateResult,
            string source,
            string goblinType,
            string evidenceSignature,
            string reason,
            string freshnessReason)
        {
            return new GoblinReplayFixtureStepResult(
                scenarioName,
                step.Name,
                step.AreaKey,
                frameSource,
                candidateResult.Equals("Found", StringComparison.OrdinalIgnoreCase),
                candidateResult,
                source,
                goblinType,
                evidenceSignature,
                GoblinDecisionTracePolicy.DecisionForReason(reason, counted: false),
                reason,
                freshnessReason,
                false);
        }

        private static string FreshnessReason(
            GoblinReplayFixtureCandidate candidate,
            ReplayEvidenceState state,
            GoblinReplayFixtureStep step)
        {
            if (candidate.Source.Equals("JournalCandidate", StringComparison.OrdinalIgnoreCase) &&
                !GoblinJournalEvidencePolicy.AppearsInActiveFeed(ToMatch(candidate)))
            {
                return "JournalCandidateIgnoredHistoryRow";
            }

            if (candidate.EvidenceKind == GoblinEvidenceTemplateKind.JournalEngaged ||
                candidate.EvidenceKind == GoblinEvidenceTemplateKind.JournalEngagedAndKilled)
            {
                bool areaChanged = !string.IsNullOrWhiteSpace(state.FirstSeenAreaKey) &&
                    !string.IsNullOrWhiteSpace(step.AreaKey) &&
                    !string.Equals(state.FirstSeenAreaKey, step.AreaKey, StringComparison.OrdinalIgnoreCase);
                if (areaChanged)
                {
                    return "JournalEngagedIgnoredAreaChanged";
                }

                return GoblinJournalFreshnessPolicy.EngagedIsFresh(
                    state.FirstSeenUtc,
                    state.FirstSeenAreaKey,
                    step.AreaKey,
                    step.TimestampUtc,
                    ReplayJournalFreshnessWindow)
                    ? "Fresh"
                    : "JournalEngagedIgnoredStale";
            }

            if (candidate.EvidenceKind == GoblinEvidenceTemplateKind.JournalKilled)
            {
                GoblinJournalKilledState killedState = new(
                    state.GoblinType,
                    state.FirstSeenAreaKey,
                    state.FirstSeenUtc,
                    state.LastSeenUtc);
                return GoblinJournalFreshnessPolicy.KilledIsFresh(
                    killedState,
                    step.AreaKey,
                    step.TimestampUtc,
                    ReplayJournalFreshnessWindow)
                    ? "Fresh"
                    : "JournalKilledIgnoredStale";
            }

            return GoblinJournalFreshnessPolicy.IsFresh(state.FirstSeenUtc, step.TimestampUtc, ReplayJournalFreshnessWindow)
                ? "Fresh"
                : "StaleEvidence";
        }

        private static string SuppressionReasonForFreshness(string freshnessReason)
        {
            if (freshnessReason.Contains("Stale", StringComparison.OrdinalIgnoreCase) ||
                freshnessReason.Contains("AreaChanged", StringComparison.OrdinalIgnoreCase))
            {
                return "StaleEvidence";
            }

            return freshnessReason;
        }

        private static GoblinReplayFixtureCandidate? SelectScenarioCandidate(IReadOnlyList<GoblinReplayFixtureCandidate> candidates)
        {
            return candidates
                .Where(candidate => candidate.PassedThreshold)
                .OrderBy(candidate => string.Equals(candidate.Source, "JournalCandidate", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenByDescending(candidate => candidate.Confidence)
                .FirstOrDefault();
        }

        private static string EvidenceSignature(GoblinReplayFixtureCandidate candidate)
        {
            GoblinEvidenceTemplateRequirement template = new(
                candidate.EvidenceType,
                candidate.Source,
                candidate.TemplateName,
                candidate.Threshold,
                candidate.GoblinType,
                candidate.EvidenceKind);
            GoblinEvidenceTemplateMatch match = ToMatch(candidate);
            if (candidate.Source.Equals("JournalCandidate", StringComparison.OrdinalIgnoreCase))
            {
                return GoblinJournalEvidencePolicy.LineSignature(template, candidate.GoblinType, match);
            }

            return string.Join("|",
                candidate.Source,
                GoblinTypeNormalizer.Normalize(candidate.GoblinType),
                candidate.TemplateName,
                $"Match={candidate.MatchPoint.X},{candidate.MatchPoint.Y}");
        }

        private static GoblinEvidenceTemplateMatch ToMatch(GoblinReplayFixtureCandidate candidate)
        {
            return new GoblinEvidenceTemplateMatch(
                candidate.Confidence,
                candidate.MatchPoint,
                candidate.ScreenMatchPoint,
                candidate.TemplateSize);
        }

        private static Rectangle RegionForSource(string source)
        {
            return string.Equals(source, "MinimapCandidate", StringComparison.OrdinalIgnoreCase)
                ? GoblinEvidenceScanRegions.MinimapReferenceRegion
                : GoblinEvidenceScanRegions.JournalReferenceRegion;
        }

        private static string? SourcePath(GoblinReplayFixture fixture, string source)
        {
            return string.Equals(source, "MinimapCandidate", StringComparison.OrdinalIgnoreCase)
                ? fixture.MinimapPath
                : fixture.JournalPath;
        }

        private static GoblinReplayCaptureFolderLoadResult EmptyCaptureLoad(
            string stepName,
            string captureFolderPath,
            string reason,
            string? areaKeyOverride,
            DateTime? timestampUtcOverride,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            return new GoblinReplayCaptureFolderLoadResult(
                stepName,
                captureFolderPath,
                false,
                reason,
                "",
                null,
                null,
                ResolveCaptureAreaKey(areaKeyOverride, metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)),
                timestampUtcOverride ?? DateTime.UtcNow,
                metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        private static Dictionary<string, string> ReadCaptureMetadata(string captureFolderPath)
        {
            Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase);
            string? metadataPath = LatestCaptureMetadataPath(captureFolderPath);
            if (string.IsNullOrWhiteSpace(metadataPath) || !File.Exists(metadataPath))
            {
                return metadata;
            }

            foreach (string line in File.ReadLines(metadataPath))
            {
                int separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                string key = line[..separator].Trim();
                string value = line[(separator + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    metadata[key] = value;
                }
            }

            return metadata;
        }

        private static string? LatestCaptureMetadataPath(string captureFolderPath)
        {
            return Directory
                .EnumerateFiles(captureFolderPath, "*_Metadata.txt", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ThenByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        private static string? LatestCapturePrefix(string captureFolderPath)
        {
            return Directory
                .EnumerateFiles(captureFolderPath, "*.png", SearchOption.TopDirectoryOnly)
                .Where(path =>
                    path.EndsWith("_Journal.png", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith("_Minimap.png", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ThenByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .Select(path => path.EndsWith("_Journal.png", StringComparison.OrdinalIgnoreCase)
                    ? path[..^"_Journal.png".Length]
                    : path[..^"_Minimap.png".Length])
                .FirstOrDefault();
        }

        private static string? ResolveCaptureImagePath(
            string captureFolderPath,
            string? prefix,
            IReadOnlyDictionary<string, string> metadata,
            string metadataKey,
            string suffix)
        {
            if (metadata.TryGetValue(metadataKey, out string? metadataPath))
            {
                string resolvedMetadataPath = ResolveCaptureMetadataPath(captureFolderPath, metadataPath);
                if (File.Exists(resolvedMetadataPath))
                {
                    return resolvedMetadataPath;
                }
            }

            if (!string.IsNullOrWhiteSpace(prefix))
            {
                string siblingPath = $"{prefix}{suffix}";
                if (File.Exists(siblingPath))
                {
                    return siblingPath;
                }
            }

            return Directory
                .EnumerateFiles(captureFolderPath, $"*{suffix}", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ThenByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        private static string ResolveCaptureMetadataPath(string captureFolderPath, string metadataPath)
        {
            if (Path.IsPathRooted(metadataPath))
            {
                if (File.Exists(metadataPath))
                {
                    return metadataPath;
                }

                return Path.Combine(captureFolderPath, Path.GetFileName(metadataPath));
            }

            return Path.Combine(captureFolderPath, metadataPath);
        }

        private static string ResolveCaptureAreaKey(string? areaKeyOverride, IReadOnlyDictionary<string, string> metadata)
        {
            string rawArea = !string.IsNullOrWhiteSpace(areaKeyOverride)
                ? areaKeyOverride!
                : MetadataValue(metadata, "AreaKey", "CurrentArea", "DisplayLocation", "Area");
            return string.IsNullOrWhiteSpace(rawArea)
                ? ""
                : GoblinAreaResolver.Resolve(rawArea).AreaKey;
        }

        private static DateTime ResolveCaptureTimestampUtc(
            IReadOnlyDictionary<string, string> metadata,
            string? metadataPath,
            string? journalPath,
            string? minimapPath)
        {
            string rawTimestamp = MetadataValue(metadata, "CreatedUtc", "TimestampUtc", "Timestamp");
            if (DateTime.TryParse(
                rawTimestamp,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsed))
            {
                return parsed.ToUniversalTime();
            }

            string? fallbackPath = new[] { metadataPath, journalPath, minimapPath }
                .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
            return string.IsNullOrWhiteSpace(fallbackPath)
                ? DateTime.UtcNow
                : File.GetLastWriteTimeUtc(fallbackPath);
        }

        private static string MetadataValue(IReadOnlyDictionary<string, string> metadata, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (metadata.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return "";
        }

        private static string LogField(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "None";
            }

            return value.Replace('\r', ' ').Replace('\n', ' ').Replace(';', ',');
        }

        private sealed record ReplayEvidenceState(
            DateTime FirstSeenUtc,
            DateTime LastSeenUtc,
            string FirstSeenAreaKey,
            string GoblinType,
            string Source,
            bool Counted,
            string CountedAreaKey);
    }
}

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

    internal enum GoblinReplayTemplateScenarioStepKind
    {
        Scan,
        Wait,
        ResetStats,
        NewGame,
    }

    internal sealed record GoblinReplayTemplateScenarioStep(
        string Name,
        GoblinReplayTemplateScenarioStepKind Kind,
        string AreaKey = "",
        string? LocationTemplateName = null,
        string? JournalTemplateName = null,
        string? MinimapTemplateName = null,
        int JournalLineBucket = 10,
        int AdvanceSeconds = 1,
        DateTime? TimestampUtc = null);

    internal sealed record GoblinReplayTemplateScenarioManifestLoadResult(
        string ScenarioPath,
        bool Loaded,
        string Reason,
        string ScenarioName,
        IReadOnlyList<GoblinReplayTemplateScenarioStep> Steps,
        IReadOnlyList<string> Errors);

    internal sealed record GoblinReplayCaptureFolderStep(
        string Name,
        string CaptureFolderPath,
        string? AreaKeyOverride = null,
        DateTime? TimestampUtcOverride = null);

    internal enum GoblinReplayCaptureInputKind
    {
        CaptureFolder,
        MetadataFile,
        CapturePrefix,
        DecisionBundle,
    }

    internal sealed record GoblinReplayCaptureInputStep(
        string Name,
        string InputPath,
        GoblinReplayCaptureInputKind Kind,
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
        private static readonly TimeSpan ReplayNewGameCarryoverSuppressWindow = TimeSpan.FromSeconds(20);

        public static GoblinReplayFixtureRunResult RunExplicitFixtureForHarness(
            GoblinReplayFixture fixture,
            string templateDirectory,
            Action<string>? log = null,
            Action<IGoblinEvidenceFrameSource?>? setFrameSourceForReplay = null,
            bool writeAppLog = true)
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
                if (writeAppLog)
                {
                    AppLogger.Info(message);
                }
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
            Action<IGoblinEvidenceFrameSource?>? setFrameSourceForReplay = null,
            bool writeAppLog = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);
            ArgumentNullException.ThrowIfNull(steps);
            ArgumentException.ThrowIfNullOrWhiteSpace(templateDirectory);

            List<GoblinReplayFixtureStepResult> stepResults = [];
            List<string> logMessages = [];
            Dictionary<string, ReplayEvidenceState> evidenceBySignature = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ReplayEncounterState> encounterByGoblinType = new(StringComparer.OrdinalIgnoreCase);
            GoblinAreaDuplicateGuard duplicateGuard = new();

            void Emit(string eventName, string details)
            {
                string message = $"{eventName}: mode=ExplicitOnDemand; scenario={LogField(scenarioName)}; {details}";
                logMessages.Add(message);
                log?.Invoke(message);
                if (writeAppLog)
                {
                    AppLogger.Info(message);
                }
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
                    setFrameSourceForReplay,
                    writeAppLog);

                GoblinReplayFixtureCandidate? candidate = SelectScenarioCandidate(fixtureResult.Candidates);
                GoblinReplayFixtureStepResult stepResult = EvaluateStep(
                    scenarioName,
                    step,
                    candidate,
                    evidenceBySignature,
                    encounterByGoblinType,
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

        public static GoblinReplayFixtureScenarioResult RunExplicitTemplateScenarioForHarness(
            string scenarioName,
            IReadOnlyList<GoblinReplayTemplateScenarioStep> steps,
            string templateDirectory,
            Action<string>? log = null,
            Action<IGoblinEvidenceFrameSource?>? setFrameSourceForReplay = null,
            bool writeAppLog = true,
            DateTime? startUtc = null,
            string? currentLocationTemplateDirectory = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);
            ArgumentNullException.ThrowIfNull(steps);
            ArgumentException.ThrowIfNullOrWhiteSpace(templateDirectory);

            List<GoblinReplayFixtureStepResult> stepResults = [];
            List<string> logMessages = [];
            Dictionary<string, ReplayEvidenceState> evidenceBySignature = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ReplayEncounterState> encounterByGoblinType = new(StringComparer.OrdinalIgnoreCase);
            List<ReplayResetCarryoverState> resetCarryoverSuppressions = [];
            List<ReplayStaleVisibleLineState> staleVisibleLineSuppressions = [];
            GoblinAreaDuplicateGuard duplicateGuard = new();
            string tempRoot = Path.Combine(Path.GetTempPath(), $"GoblinFarmerReplayScenario_{Guid.NewGuid():N}");
            DateTime cursorUtc = startUtc ?? DateTime.UtcNow;
            DateTime newGameCarryoverSuppressUntilUtc = DateTime.MinValue;
            string resolvedCurrentLocationTemplateDirectory = string.IsNullOrWhiteSpace(currentLocationTemplateDirectory)
                ? Path.Combine(Path.GetDirectoryName(templateDirectory) ?? "", "Current Location")
                : currentLocationTemplateDirectory!;

            void Emit(string eventName, string details)
            {
                string message = $"{eventName}: mode=ExplicitOnDemand; scenario={LogField(scenarioName)}; {details}";
                logMessages.Add(message);
                log?.Invoke(message);
                if (writeAppLog)
                {
                    AppLogger.Info(message);
                }
            }

            Emit(
                "GoblinReplayTemplateScenarioStarted",
                $"stepCount={steps.Count}; templateDirectory={LogField(templateDirectory)}; tempRoot={LogField(tempRoot)}");

            try
            {
                Directory.CreateDirectory(tempRoot);
                for (int index = 0; index < steps.Count; index++)
                {
                    GoblinReplayTemplateScenarioStep step = steps[index];
                    DateTime stepUtc = step.TimestampUtc ?? cursorUtc;
                    string resolvedStepAreaKey = ResolveTemplateScenarioAreaKey(
                        step,
                        resolvedCurrentLocationTemplateDirectory,
                        Emit);

                    if (step.Kind != GoblinReplayTemplateScenarioStepKind.Scan)
                    {
                        int remembered = 0;
                        int clearedEvidence = 0;
                        int clearedEncounters = 0;
                        int clearedAreas = 0;
                        string reason = step.Kind.ToString();

                        if (step.Kind == GoblinReplayTemplateScenarioStepKind.NewGame ||
                            step.Kind == GoblinReplayTemplateScenarioStepKind.ResetStats)
                        {
                            remembered = RememberReplayResetCarryoverSuppressions(
                                evidenceBySignature,
                                resetCarryoverSuppressions,
                                stepUtc,
                                reason);
                            clearedEvidence = evidenceBySignature.Count;
                            clearedEncounters = encounterByGoblinType.Count;
                            clearedAreas = duplicateGuard.Reset();
                            evidenceBySignature.Clear();
                            encounterByGoblinType.Clear();
                            staleVisibleLineSuppressions.Clear();
                            newGameCarryoverSuppressUntilUtc = step.Kind == GoblinReplayTemplateScenarioStepKind.NewGame
                                ? stepUtc + ReplayNewGameCarryoverSuppressWindow
                                : DateTime.MinValue;
                        }

                        GoblinReplayFixtureStepResult actionResult = new(
                            scenarioName,
                            step.Name,
                            resolvedStepAreaKey,
                            "ScenarioAction",
                            false,
                            step.Kind.ToString(),
                            "",
                            "Unknown",
                            "",
                            "Action",
                            reason,
                            "ScenarioAction",
                            false);
                        stepResults.Add(actionResult);
                        Emit(
                            "GoblinReplayTemplateScenarioAction",
                            $"step={LogField(step.Name)}; action={step.Kind}; rememberedResetCarryover={remembered}; clearedEvidence={clearedEvidence}; clearedEncounters={clearedEncounters}; clearedAreaKeys={clearedAreas}");
                        cursorUtc = stepUtc.AddSeconds(Math.Max(0, step.AdvanceSeconds));
                        continue;
                    }

                    GoblinReplayFixture fixture = CreateTemplateScenarioFixture(
                        step,
                        templateDirectory,
                        tempRoot,
                        index,
                        Emit);
                    GoblinReplayFixtureRunResult fixtureResult = RunExplicitFixtureForHarness(
                        fixture,
                        templateDirectory,
                        message =>
                        {
                            logMessages.Add(message);
                            log?.Invoke(message);
                        },
                        setFrameSourceForReplay,
                        writeAppLog);
                    GoblinReplayFixtureCandidate? candidate = SelectScenarioCandidate(fixtureResult.Candidates);
                    GoblinReplayFixtureStep fixtureStep = new(
                        step.Name,
                        fixture,
                        resolvedStepAreaKey,
                        stepUtc);
                    GoblinReplayFixtureStepResult stepResult = EvaluateStep(
                        scenarioName,
                        fixtureStep,
                        candidate,
                        evidenceBySignature,
                        encounterByGoblinType,
                        duplicateGuard,
                        resetCarryoverSuppressions,
                        staleVisibleLineSuppressions,
                        newGameCarryoverSuppressUntilUtc);
                    stepResults.Add(stepResult);
                    Emit(
                        "GoblinReplayTemplateScenarioStepResult",
                        $"step={LogField(step.Name)}; frameSource={LogField(stepResult.FrameSource)}; areaKey={LogField(stepResult.AreaKey)}; candidateResult={LogField(stepResult.CandidateResult)}; source={LogField(stepResult.Source)}; goblinType={LogField(stepResult.GoblinType)}; evidenceSignature={LogField(stepResult.EvidenceSignature)}; countDecision={LogField(stepResult.CountDecision)}; reason={LogField(stepResult.Reason)}; staleFreshReason={LogField(stepResult.FreshnessReason)}; counted={stepResult.Counted}");
                    cursorUtc = stepUtc.AddSeconds(Math.Max(0, step.AdvanceSeconds));
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }

            Emit(
                "GoblinReplayTemplateScenarioCompleted",
                $"stepCount={stepResults.Count}; countedSteps={stepResults.Count(step => step.Counted)}; suppressedSteps={stepResults.Count(step => !step.Counted)}");

            return new GoblinReplayFixtureScenarioResult(scenarioName, stepResults, logMessages);
        }

        public static GoblinReplayTemplateScenarioManifestLoadResult LoadExplicitTemplateScenarioManifestForHarness(string scenarioPath)
        {
            if (string.IsNullOrWhiteSpace(scenarioPath) || !File.Exists(scenarioPath))
            {
                return new GoblinReplayTemplateScenarioManifestLoadResult(
                    scenarioPath ?? "",
                    false,
                    "ScenarioFileMissing",
                    "",
                    [],
                    [$"Scenario file does not exist: {scenarioPath}"]);
            }

            string fullPath = Path.GetFullPath(scenarioPath);
            string scenarioName = Path.GetFileNameWithoutExtension(fullPath);
            List<GoblinReplayTemplateScenarioStep> steps = [];
            List<string> errors = [];
            int lineNumber = 0;
            foreach (string rawLine in File.ReadLines(fullPath))
            {
                lineNumber++;
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                int separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    errors.Add($"Line {lineNumber}: expected Key=Value.");
                    continue;
                }

                string key = line[..separator].Trim();
                string value = line[(separator + 1)..].Trim();
                if (key.Equals("Scenario", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        scenarioName = value;
                    }

                    continue;
                }

                if (!key.Equals("Step", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Line {lineNumber}: unsupported key '{key}'.");
                    continue;
                }

                if (TryParseTemplateScenarioStep(value, out GoblinReplayTemplateScenarioStep? step, out string error))
                {
                    steps.Add(step!);
                }
                else
                {
                    errors.Add($"Line {lineNumber}: {error}");
                }
            }

            if (errors.Count > 0)
            {
                return new GoblinReplayTemplateScenarioManifestLoadResult(
                    fullPath,
                    false,
                    "ScenarioParseError",
                    scenarioName,
                    steps,
                    errors);
            }

            if (steps.Count == 0)
            {
                return new GoblinReplayTemplateScenarioManifestLoadResult(
                    fullPath,
                    false,
                    "ScenarioHasNoSteps",
                    scenarioName,
                    steps,
                    ["Scenario contains no Step= entries."]);
            }

            return new GoblinReplayTemplateScenarioManifestLoadResult(
                fullPath,
                true,
                "Loaded",
                scenarioName,
                steps,
                []);
        }

        public static GoblinReplayCaptureFolderScenarioResult RunExplicitCaptureFoldersForHarness(
            string scenarioName,
            IReadOnlyList<GoblinReplayCaptureFolderStep> captureSteps,
            string templateDirectory,
            Action<string>? log = null,
            Action<IGoblinEvidenceFrameSource?>? setFrameSourceForReplay = null,
            bool writeAppLog = true)
        {
            ArgumentNullException.ThrowIfNull(captureSteps);
            return RunExplicitCaptureInputsForHarness(
                scenarioName,
                captureSteps
                    .Select(step => new GoblinReplayCaptureInputStep(
                        step.Name,
                        step.CaptureFolderPath,
                        GoblinReplayCaptureInputKind.CaptureFolder,
                        step.AreaKeyOverride,
                        step.TimestampUtcOverride))
                    .ToList(),
                templateDirectory,
                log,
                setFrameSourceForReplay,
                writeAppLog);
        }

        public static GoblinReplayCaptureFolderScenarioResult RunExplicitMetadataFilesForHarness(
            string scenarioName,
            IReadOnlyList<GoblinReplayCaptureFolderStep> metadataSteps,
            string templateDirectory,
            Action<string>? log = null,
            Action<IGoblinEvidenceFrameSource?>? setFrameSourceForReplay = null,
            bool writeAppLog = true)
        {
            ArgumentNullException.ThrowIfNull(metadataSteps);
            return RunExplicitCaptureInputsForHarness(
                scenarioName,
                metadataSteps
                    .Select(step => new GoblinReplayCaptureInputStep(
                        step.Name,
                        step.CaptureFolderPath,
                        GoblinReplayCaptureInputKind.MetadataFile,
                        step.AreaKeyOverride,
                        step.TimestampUtcOverride))
                    .ToList(),
                templateDirectory,
                log,
                setFrameSourceForReplay,
                writeAppLog);
        }

        public static GoblinReplayCaptureFolderScenarioResult RunExplicitCapturePrefixesForHarness(
            string scenarioName,
            IReadOnlyList<GoblinReplayCaptureFolderStep> prefixSteps,
            string templateDirectory,
            Action<string>? log = null,
            Action<IGoblinEvidenceFrameSource?>? setFrameSourceForReplay = null,
            bool writeAppLog = true)
        {
            ArgumentNullException.ThrowIfNull(prefixSteps);
            return RunExplicitCaptureInputsForHarness(
                scenarioName,
                prefixSteps
                    .Select(step => new GoblinReplayCaptureInputStep(
                        step.Name,
                        step.CaptureFolderPath,
                        GoblinReplayCaptureInputKind.CapturePrefix,
                        step.AreaKeyOverride,
                        step.TimestampUtcOverride))
                    .ToList(),
                templateDirectory,
                log,
                setFrameSourceForReplay,
                writeAppLog);
        }

        public static GoblinReplayCaptureFolderScenarioResult RunExplicitDecisionBundlesForHarness(
            string scenarioName,
            IReadOnlyList<GoblinReplayCaptureFolderStep> decisionBundleSteps,
            string templateDirectory,
            Action<string>? log = null,
            Action<IGoblinEvidenceFrameSource?>? setFrameSourceForReplay = null,
            bool writeAppLog = true)
        {
            ArgumentNullException.ThrowIfNull(decisionBundleSteps);
            return RunExplicitCaptureInputsForHarness(
                scenarioName,
                decisionBundleSteps
                    .Select(step => new GoblinReplayCaptureInputStep(
                        step.Name,
                        step.CaptureFolderPath,
                        GoblinReplayCaptureInputKind.DecisionBundle,
                        step.AreaKeyOverride,
                        step.TimestampUtcOverride))
                    .ToList(),
                templateDirectory,
                log,
                setFrameSourceForReplay,
                writeAppLog);
        }

        public static GoblinReplayCaptureFolderScenarioResult RunExplicitCaptureInputsForHarness(
            string scenarioName,
            IReadOnlyList<GoblinReplayCaptureInputStep> captureSteps,
            string templateDirectory,
            Action<string>? log = null,
            Action<IGoblinEvidenceFrameSource?>? setFrameSourceForReplay = null,
            bool writeAppLog = true)
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
                if (writeAppLog)
                {
                    AppLogger.Info(message);
                }
            }

            Emit(
                "GoblinReplayCaptureInputScenarioStarted",
                $"captureStepCount={captureSteps.Count}; templateDirectory={LogField(templateDirectory)}");

            foreach (GoblinReplayCaptureInputStep captureStep in captureSteps)
            {
                GoblinReplayCaptureFolderLoadResult loadResult = LoadExplicitCaptureInputForHarness(captureStep, Emit);
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
                    setFrameSourceForReplay,
                    writeAppLog);

            Emit(
                "GoblinReplayCaptureInputScenarioCompleted",
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
            return LoadExplicitCaptureInputForHarness(
                new GoblinReplayCaptureInputStep(
                    captureStep.Name,
                    captureStep.CaptureFolderPath,
                    GoblinReplayCaptureInputKind.CaptureFolder,
                    captureStep.AreaKeyOverride,
                    captureStep.TimestampUtcOverride),
                emit);
        }

        private static GoblinReplayCaptureFolderLoadResult LoadExplicitCaptureInputForHarness(
            GoblinReplayCaptureInputStep captureStep,
            Action<string, string> emit)
        {
            return captureStep.Kind switch
            {
                GoblinReplayCaptureInputKind.MetadataFile => LoadExplicitMetadataFileForHarness(captureStep, emit),
                GoblinReplayCaptureInputKind.CapturePrefix => LoadExplicitCapturePrefixForHarness(captureStep, emit),
                GoblinReplayCaptureInputKind.DecisionBundle => LoadExplicitDecisionBundleForHarness(captureStep, emit),
                _ => LoadExplicitCaptureFolderForHarnessCore(captureStep, emit),
            };
        }

        private static GoblinReplayCaptureFolderLoadResult LoadExplicitCaptureFolderForHarnessCore(
            GoblinReplayCaptureInputStep captureStep,
            Action<string, string> emit)
        {
            string stepName = string.IsNullOrWhiteSpace(captureStep.Name)
                ? "Unnamed capture step"
                : captureStep.Name.Trim();
            string captureFolderPath = captureStep.InputPath ?? "";
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

        private static GoblinReplayCaptureFolderLoadResult LoadExplicitMetadataFileForHarness(
            GoblinReplayCaptureInputStep captureStep,
            Action<string, string> emit)
        {
            string stepName = string.IsNullOrWhiteSpace(captureStep.Name)
                ? "Unnamed metadata step"
                : captureStep.Name.Trim();
            string metadataPath = captureStep.InputPath ?? "";
            if (string.IsNullOrWhiteSpace(metadataPath) || !File.Exists(metadataPath))
            {
                GoblinReplayCaptureFolderLoadResult missing = EmptyCaptureLoad(
                    stepName,
                    metadataPath,
                    "MetadataFileMissing",
                    captureStep.AreaKeyOverride,
                    captureStep.TimestampUtcOverride);
                emit(
                    "GoblinReplayCaptureMetadataSkipped",
                    $"step={LogField(stepName)}; metadataPath={LogField(metadataPath)}; reason={missing.Reason}");
                return missing;
            }

            return LoadExplicitCapturePrefixForHarness(
                captureStep with { InputPath = PrefixFromMetadataPath(Path.GetFullPath(metadataPath)) },
                emit,
                explicitMetadataPath: Path.GetFullPath(metadataPath));
        }

        private static GoblinReplayCaptureFolderLoadResult LoadExplicitCapturePrefixForHarness(
            GoblinReplayCaptureInputStep captureStep,
            Action<string, string> emit,
            string? explicitMetadataPath = null)
        {
            string stepName = string.IsNullOrWhiteSpace(captureStep.Name)
                ? "Unnamed prefix step"
                : captureStep.Name.Trim();
            string prefix = captureStep.InputPath ?? "";
            if (string.IsNullOrWhiteSpace(prefix))
            {
                GoblinReplayCaptureFolderLoadResult missingPrefix = EmptyCaptureLoad(
                    stepName,
                    prefix,
                    "CapturePrefixMissing",
                    captureStep.AreaKeyOverride,
                    captureStep.TimestampUtcOverride);
                emit(
                    "GoblinReplayCapturePrefixSkipped",
                    $"step={LogField(stepName)}; prefix={LogField(prefix)}; reason={missingPrefix.Reason}");
                return missingPrefix;
            }

            prefix = Path.GetFullPath(prefix);
            string root = Path.GetDirectoryName(prefix) ?? "";
            string metadataPath = explicitMetadataPath ?? $"{prefix}_Metadata.txt";
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                GoblinReplayCaptureFolderLoadResult missingFolder = EmptyCaptureLoad(
                    stepName,
                    prefix,
                    "CaptureFolderMissing",
                    captureStep.AreaKeyOverride,
                    captureStep.TimestampUtcOverride);
                emit(
                    "GoblinReplayCapturePrefixSkipped",
                    $"step={LogField(stepName)}; prefix={LogField(prefix)}; reason={missingFolder.Reason}");
                return missingFolder;
            }

            if (!File.Exists(metadataPath))
            {
                GoblinReplayCaptureFolderLoadResult missingMetadata = EmptyCaptureLoad(
                    stepName,
                    prefix,
                    "MetadataFileMissing",
                    captureStep.AreaKeyOverride,
                    captureStep.TimestampUtcOverride);
                emit(
                    "GoblinReplayCapturePrefixSkipped",
                    $"step={LogField(stepName)}; prefix={LogField(prefix)}; metadataPath={LogField(metadataPath)}; reason={missingMetadata.Reason}");
                return missingMetadata;
            }

            Dictionary<string, string> metadata = ReadCaptureMetadataFile(metadataPath);
            string? journalPath = ResolveCaptureImagePath(root, prefix, metadata, "JournalPath", "_Journal.png");
            string? minimapPath = ResolveCaptureImagePath(root, prefix, metadata, "MinimapPath", "_Minimap.png");
            if (string.IsNullOrWhiteSpace(journalPath) && string.IsNullOrWhiteSpace(minimapPath))
            {
                GoblinReplayCaptureFolderLoadResult noFrames = EmptyCaptureLoad(
                    stepName,
                    prefix,
                    "NoJournalOrMinimapFrame",
                    captureStep.AreaKeyOverride,
                    captureStep.TimestampUtcOverride,
                    metadata);
                emit(
                    "GoblinReplayCapturePrefixSkipped",
                    $"step={LogField(stepName)}; prefix={LogField(prefix)}; metadataPath={LogField(metadataPath)}; reason={noFrames.Reason}");
                return noFrames;
            }

            string areaKey = ResolveCaptureAreaKey(captureStep.AreaKeyOverride, metadata);
            DateTime timestampUtc = captureStep.TimestampUtcOverride ??
                ResolveCaptureTimestampUtc(metadata, metadataPath, journalPath, minimapPath);
            string fixtureName = Path.GetFileName(prefix);
            GoblinReplayCaptureFolderLoadResult loaded = new(
                stepName,
                prefix,
                true,
                "Loaded",
                fixtureName,
                journalPath,
                minimapPath,
                areaKey,
                timestampUtc,
                metadata);
            emit(
                "GoblinReplayCapturePrefixLoaded",
                $"step={LogField(stepName)}; prefix={LogField(prefix)}; fixture={LogField(fixtureName)}; areaKey={LogField(areaKey)}; timestampUtc={timestampUtc:O}; journalPath={LogField(journalPath)}; minimapPath={LogField(minimapPath)}; metadataPath={LogField(metadataPath)}");
            return loaded;
        }

        private static GoblinReplayCaptureFolderLoadResult LoadExplicitDecisionBundleForHarness(
            GoblinReplayCaptureInputStep captureStep,
            Action<string, string> emit)
        {
            string stepName = string.IsNullOrWhiteSpace(captureStep.Name)
                ? "Unnamed decision bundle step"
                : captureStep.Name.Trim();
            string bundlePath = captureStep.InputPath ?? "";
            if (string.IsNullOrWhiteSpace(bundlePath) || !Directory.Exists(bundlePath))
            {
                GoblinReplayCaptureFolderLoadResult missing = EmptyCaptureLoad(
                    stepName,
                    bundlePath,
                    "DecisionBundleMissing",
                    captureStep.AreaKeyOverride,
                    captureStep.TimestampUtcOverride);
                emit(
                    "GoblinReplayDecisionBundleSkipped",
                    $"step={LogField(stepName)}; bundlePath={LogField(bundlePath)}; reason={missing.Reason}");
                return missing;
            }

            string root = Path.GetFullPath(bundlePath);
            Dictionary<string, string> metadata = ReadDecisionBundleMetadata(root);
            string? tracePath = DecisionBundleTracePath(root);
            string? evidencePath = DecisionBundleEvidencePath(root);
            metadata["DecisionBundlePath"] = root;
            if (!string.IsNullOrWhiteSpace(tracePath))
            {
                metadata["DecisionTracePath"] = tracePath;
            }

            if (!string.IsNullOrWhiteSpace(evidencePath))
            {
                metadata["EvidencePath"] = evidencePath;
            }

            string? replayPrefix = ResolveDecisionBundleReplayPrefix(root, metadata);
            if (!string.IsNullOrWhiteSpace(replayPrefix))
            {
                GoblinReplayCaptureFolderLoadResult replayLoaded = LoadExplicitCapturePrefixForHarness(
                    captureStep with { InputPath = replayPrefix, Kind = GoblinReplayCaptureInputKind.CapturePrefix },
                    emit);
                if (replayLoaded.Loaded)
                {
                    emit(
                        "GoblinReplayDecisionBundleLoaded",
                        $"step={LogField(stepName)}; bundlePath={LogField(root)}; replayPrefix={LogField(replayPrefix)}; reason=ResolvedReplayCapturePrefix");
                    return replayLoaded with
                    {
                        CaptureFolderPath = root,
                        Reason = "LoadedFromDecisionBundle",
                    };
                }
            }

            GoblinReplayCaptureFolderLoadResult notReplayable = EmptyCaptureLoad(
                stepName,
                root,
                "DecisionBundleMissingReplayFrames",
                captureStep.AreaKeyOverride ?? MetadataValue(metadata, "areaKey", "AreaKey"),
                captureStep.TimestampUtcOverride,
                metadata);
            emit(
                "GoblinReplayDecisionBundleSkipped",
                $"step={LogField(stepName)}; bundlePath={LogField(root)}; reason={notReplayable.Reason}; tracePath={LogField(tracePath)}; evidencePath={LogField(evidencePath)}; availableFiles={LogField(string.Join(",", Directory.EnumerateFiles(root).Select(Path.GetFileName)))}; explanation={LogField("Replay-ready decision bundles contain decision-trace.txt plus local *_Metadata.txt, *_Journal.png, and *_Minimap.png frames. Older bundles may contain only fullscreen evidence.png; those are useful for manual review but cannot replay until crop frames or a capture prefix are available.")}");
            return notReplayable;
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

        private static bool TryParseTemplateScenarioStep(
            string value,
            out GoblinReplayTemplateScenarioStep? step,
            out string error)
        {
            step = null;
            error = "";
            string[] parts = value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                error = "Step must use 'Name|Action|Key=Value'.";
                return false;
            }

            string name = parts[0];
            if (!Enum.TryParse(parts[1], ignoreCase: true, out GoblinReplayTemplateScenarioStepKind kind))
            {
                error = $"Unsupported step action '{parts[1]}'.";
                return false;
            }

            Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase);
            for (int index = 2; index < parts.Length; index++)
            {
                int separator = parts[index].IndexOf('=');
                if (separator <= 0)
                {
                    error = $"Step option '{parts[index]}' must use Key=Value.";
                    return false;
                }

                options[parts[index][..separator].Trim()] = parts[index][(separator + 1)..].Trim();
            }

            string areaKey = MetadataValue(options, "Area", "AreaKey", "CurrentArea");
            string locationTemplateName = MetadataValue(options, "Location", "LocationTemplate", "CurrentLocationTemplate");
            string journalTemplateName = MetadataValue(options, "Journal", "JournalTemplate");
            string minimapTemplateName = MetadataValue(options, "Minimap", "MinimapTemplate");
            int journalLineBucket = ParseIntOption(options, 10, "JournalLineBucket", "LineBucket", "Bucket");
            int advanceSeconds = ParseIntOption(options, 1, "AdvanceSeconds", "Advance", "Seconds");
            DateTime? timestampUtc = null;
            string timestampValue = MetadataValue(options, "TimestampUtc", "Timestamp");
            if (!string.IsNullOrWhiteSpace(timestampValue) &&
                DateTime.TryParse(
                    timestampValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTime parsedTimestamp))
            {
                timestampUtc = parsedTimestamp.ToUniversalTime();
            }

            if (kind == GoblinReplayTemplateScenarioStepKind.Scan &&
                string.IsNullOrWhiteSpace(journalTemplateName) &&
                string.IsNullOrWhiteSpace(minimapTemplateName))
            {
                error = "Scan steps require Journal=... and/or Minimap=....";
                return false;
            }

            step = new GoblinReplayTemplateScenarioStep(
                name,
                kind,
                areaKey,
                string.IsNullOrWhiteSpace(locationTemplateName) ? null : locationTemplateName,
                string.IsNullOrWhiteSpace(journalTemplateName) ? null : journalTemplateName,
                string.IsNullOrWhiteSpace(minimapTemplateName) ? null : minimapTemplateName,
                journalLineBucket,
                advanceSeconds,
                timestampUtc);
            return true;
        }

        private static int ParseIntOption(IReadOnlyDictionary<string, string> options, int defaultValue, params string[] keys)
        {
            string rawValue = MetadataValue(options, keys);
            return int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : defaultValue;
        }

        private static GoblinReplayFixture CreateTemplateScenarioFixture(
            GoblinReplayTemplateScenarioStep step,
            string templateDirectory,
            string tempRoot,
            int stepIndex,
            Action<string, string> emit)
        {
            string? journalFramePath = CreateTemplateScenarioSourceFrame(
                step.JournalTemplateName,
                "JournalCandidate",
                templateDirectory,
                tempRoot,
                stepIndex,
                step.JournalLineBucket,
                emit);
            string? minimapFramePath = CreateTemplateScenarioSourceFrame(
                step.MinimapTemplateName,
                "MinimapCandidate",
                templateDirectory,
                tempRoot,
                stepIndex,
                step.JournalLineBucket,
                emit);
            return new GoblinReplayFixture(step.Name, journalFramePath, minimapFramePath);
        }

        private static string ResolveTemplateScenarioAreaKey(
            GoblinReplayTemplateScenarioStep step,
            string currentLocationTemplateDirectory,
            Action<string, string> emit)
        {
            if (!string.IsNullOrWhiteSpace(step.AreaKey) || string.IsNullOrWhiteSpace(step.LocationTemplateName))
            {
                return GoblinAreaResolver.Resolve(step.AreaKey).AreaKey;
            }

            string locationTemplatePath = ResolveTemplateScenarioLocationPath(
                step.LocationTemplateName!,
                currentLocationTemplateDirectory);
            if (string.IsNullOrWhiteSpace(locationTemplatePath) || !File.Exists(locationTemplatePath))
            {
                emit(
                    "GoblinReplayTemplateScenarioLocationMissing",
                    $"step={LogField(step.Name)}; locationTemplate={LogField(step.LocationTemplateName)}; currentLocationTemplateDirectory={LogField(currentLocationTemplateDirectory)}");
                return "";
            }

            Dictionary<string, string> templates = CurrentLocationImageResolver.DiscoverTemplatePaths(currentLocationTemplateDirectory);
            using Bitmap locationFrame = new(locationTemplatePath);
            CurrentLocationImageResolverResult result = CurrentLocationImageResolver.DetectFromBitmap(
                locationFrame,
                templates,
                0.82);
            string resolvedAreaKey = GoblinAreaResolver.Resolve(result.Detected).AreaKey;
            emit(
                "GoblinReplayTemplateScenarioLocationResolved",
                $"step={LogField(step.Name)}; locationTemplate={LogField(Path.GetFileName(locationTemplatePath))}; detected={LogField(result.Detected)}; areaKey={LogField(resolvedAreaKey)}; best={LogField(result.BestName)}; bestConfidence={result.BestConfidence.ToString("0.000", CultureInfo.InvariantCulture)}; second={LogField(result.SecondName)}; secondConfidence={result.SecondConfidence.ToString("0.000", CultureInfo.InvariantCulture)}; templateCount={result.TemplateCount}");
            return resolvedAreaKey;
        }

        private static string ResolveTemplateScenarioLocationPath(string locationTemplateName, string currentLocationTemplateDirectory)
        {
            if (string.IsNullOrWhiteSpace(locationTemplateName))
            {
                return "";
            }

            if (Path.IsPathRooted(locationTemplateName) && File.Exists(locationTemplateName))
            {
                return locationTemplateName;
            }

            string fileName = Path.GetFileName(locationTemplateName);
            string directPath = Path.Combine(currentLocationTemplateDirectory, fileName);
            if (File.Exists(directPath))
            {
                return directPath;
            }

            if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                string pngPath = Path.Combine(currentLocationTemplateDirectory, $"{fileName}.png");
                if (File.Exists(pngPath))
                {
                    return pngPath;
                }
            }

            string normalizedKey = CurrentLocationImageResolver.LocationKey(Path.GetFileNameWithoutExtension(fileName));
            return Directory.Exists(currentLocationTemplateDirectory)
                ? Directory
                    .EnumerateFiles(currentLocationTemplateDirectory, "*.png", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(path => CurrentLocationImageResolver.LocationKey(Path.GetFileNameWithoutExtension(path)).Equals(normalizedKey, StringComparison.OrdinalIgnoreCase)) ?? ""
                : "";
        }

        private static string? CreateTemplateScenarioSourceFrame(
            string? templateName,
            string source,
            string templateDirectory,
            string tempRoot,
            int stepIndex,
            int journalLineBucket,
            Action<string, string> emit)
        {
            if (string.IsNullOrWhiteSpace(templateName))
            {
                return null;
            }

            string fileName = Path.GetFileName(templateName);
            string templatePath = Path.Combine(templateDirectory, fileName);
            if (!File.Exists(templatePath))
            {
                emit(
                    "GoblinReplayTemplateScenarioTemplateMissing",
                    $"stepIndex={stepIndex}; source={LogField(source)}; templateName={LogField(fileName)}; templatePath={LogField(templatePath)}");
                return null;
            }

            Rectangle sourceRegion = RegionForSource(source);
            using Bitmap template = new(templatePath);
            int frameWidth = Math.Max(sourceRegion.Width, template.Width + 2);
            int frameHeight = Math.Max(sourceRegion.Height, template.Height + 2);
            Point matchPoint = source.Equals("MinimapCandidate", StringComparison.OrdinalIgnoreCase)
                ? new Point(
                    Math.Max(0, (frameWidth - template.Width) / 2),
                    Math.Max(0, (frameHeight - template.Height) / 2))
                : new Point(
                    Math.Min(30, Math.Max(0, frameWidth - template.Width - 1)),
                    Math.Min(Math.Max(0, journalLineBucket * 32 + 4), Math.Max(0, frameHeight - template.Height - 1)));
            string framePath = Path.Combine(
                tempRoot,
                $"{stepIndex:000}_{source}_{SanitizeFileName(fileName)}");
            using Bitmap frame = new(frameWidth, frameHeight);
            using Graphics graphics = Graphics.FromImage(frame);
            graphics.Clear(Color.Black);
            graphics.DrawImageUnscaled(template, matchPoint);
            frame.Save(framePath);
            emit(
                "GoblinReplayTemplateScenarioFrameCreated",
                $"stepIndex={stepIndex}; source={LogField(source)}; templateName={LogField(fileName)}; framePath={LogField(framePath)}; matchPoint={matchPoint.X},{matchPoint.Y}; frameSize={frameWidth}x{frameHeight}");
            return framePath;
        }

        private static string SanitizeFileName(string value)
        {
            string sanitized = value;
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(invalid, '_');
            }

            return sanitized;
        }

        private static int RememberReplayResetCarryoverSuppressions(
            Dictionary<string, ReplayEvidenceState> evidenceBySignature,
            List<ReplayResetCarryoverState> resetCarryoverSuppressions,
            DateTime nowUtc,
            string reason)
        {
            ExpireReplayResetCarryoverSuppressions(resetCarryoverSuppressions, nowUtc);
            int remembered = 0;
            foreach (KeyValuePair<string, ReplayEvidenceState> pair in evidenceBySignature)
            {
                if (!pair.Value.Source.Equals("JournalCandidate", StringComparison.OrdinalIgnoreCase) ||
                    !GoblinJournalFreshnessPolicy.StaleSuppressionActive(pair.Value.LastSeenUtc, nowUtc, ReplayJournalFreshnessWindow))
                {
                    continue;
                }

                resetCarryoverSuppressions.RemoveAll(state =>
                    state.Signature.Equals(pair.Key, StringComparison.OrdinalIgnoreCase));
                resetCarryoverSuppressions.Add(new ReplayResetCarryoverState(
                    pair.Key,
                    nowUtc,
                    pair.Value.LastSeenUtc,
                    reason));
                remembered++;
            }

            return remembered;
        }

        private static bool TrySuppressReplayResetCarryover(
            string signature,
            DateTime nowUtc,
            List<ReplayResetCarryoverState>? resetCarryoverSuppressions,
            out string freshnessReason)
        {
            freshnessReason = "";
            if (resetCarryoverSuppressions == null || resetCarryoverSuppressions.Count == 0)
            {
                return false;
            }

            ExpireReplayResetCarryoverSuppressions(resetCarryoverSuppressions, nowUtc);
            for (int index = 0; index < resetCarryoverSuppressions.Count; index++)
            {
                ReplayResetCarryoverState state = resetCarryoverSuppressions[index];
                if (!GoblinJournalEvidencePolicy.SameVisibleLineFamily(
                    signature,
                    state.Signature,
                    out int currentBucket,
                    out int previousBucket))
                {
                    continue;
                }

                resetCarryoverSuppressions[index] = state with { LastSeenUtc = nowUtc };
                freshnessReason = $"JournalCandidateIgnoredResetCarryover:{currentBucket}->{previousBucket}";
                return true;
            }

            return false;
        }

        private static void ExpireReplayResetCarryoverSuppressions(
            List<ReplayResetCarryoverState> resetCarryoverSuppressions,
            DateTime nowUtc)
        {
            resetCarryoverSuppressions.RemoveAll(state =>
                !GoblinJournalFreshnessPolicy.StaleSuppressionActive(state.LastSeenUtc, nowUtc, ReplayJournalFreshnessWindow));
        }

        private static GoblinReplayFixtureStepResult EvaluateStep(
            string scenarioName,
            GoblinReplayFixtureStep step,
            GoblinReplayFixtureCandidate? candidate,
            Dictionary<string, ReplayEvidenceState> evidenceBySignature,
            Dictionary<string, ReplayEncounterState> encounterByGoblinType,
            GoblinAreaDuplicateGuard duplicateGuard,
            List<ReplayResetCarryoverState>? resetCarryoverSuppressions = null,
            List<ReplayStaleVisibleLineState>? staleVisibleLineSuppressions = null,
            DateTime? newGameCarryoverSuppressUntilUtc = null)
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
            bool journalCandidate = candidate.Source.Equals("JournalCandidate", StringComparison.OrdinalIgnoreCase);
            if (candidate.Source.Equals("JournalCandidate", StringComparison.OrdinalIgnoreCase) &&
                TrySuppressReplayResetCarryover(
                    evidenceSignature,
                    step.TimestampUtc,
                    resetCarryoverSuppressions,
                    out string resetCarryoverFreshnessReason))
            {
                return SuppressedStep(
                    scenarioName,
                    step,
                    "Fixture",
                    "Found",
                    candidate.Source,
                    candidate.GoblinType,
                    evidenceSignature,
                    "JournalCandidateIgnoredResetCarryover",
                    resetCarryoverFreshnessReason);
            }

            if (journalCandidate &&
                newGameCarryoverSuppressUntilUtc.HasValue &&
                step.TimestampUtc < newGameCarryoverSuppressUntilUtc.Value)
            {
                RememberReplayStaleVisibleLine(staleVisibleLineSuppressions, evidenceSignature, step.TimestampUtc);
                return SuppressedStep(
                    scenarioName,
                    step,
                    "Fixture",
                    "Found",
                    candidate.Source,
                    candidate.GoblinType,
                    evidenceSignature,
                    "JournalCandidateIgnoredNewGameCarryoverWindow",
                    $"JournalCandidateIgnoredNewGameCarryoverWindow:{Math.Max(0, (newGameCarryoverSuppressUntilUtc.Value - step.TimestampUtc).TotalSeconds):0.0}s");
            }

            if (journalCandidate &&
                TrySuppressReplayStaleVisibleLine(
                    evidenceSignature,
                    step.TimestampUtc,
                    staleVisibleLineSuppressions,
                    out string staleVisibleLineFreshnessReason))
            {
                return SuppressedStep(
                    scenarioName,
                    step,
                    "Fixture",
                    "Found",
                    candidate.Source,
                    candidate.GoblinType,
                    evidenceSignature,
                    "JournalCandidateIgnoredStaleVisibleLine",
                    staleVisibleLineFreshnessReason);
            }

            string encounterKey = GoblinTypeNormalizer.Normalize(candidate.GoblinType);
            if (encounterKey.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                encounterKey = "";
            }

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
            encounterByGoblinType.TryGetValue(encounterKey, out ReplayEncounterState? encounterState);
            string freshnessReason = FreshnessReason(candidate, state, step);
            if (!freshnessReason.Equals("Fresh", StringComparison.OrdinalIgnoreCase))
            {
                if (journalCandidate &&
                    (freshnessReason.Contains("Stale", StringComparison.OrdinalIgnoreCase) ||
                    freshnessReason.Contains("AreaChanged", StringComparison.OrdinalIgnoreCase)))
                {
                    RememberReplayStaleVisibleLine(staleVisibleLineSuppressions, evidenceSignature, step.TimestampUtc);
                }

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
            bool refreshEncounterLastSeen = false;
            bool pfMultiCountDuplicateBypass = false;
            if (state.Counted)
            {
                GoblinAreaDuplicateGuardResult pfGuardResult = duplicateGuard.Peek(step.AreaKey);
                pfMultiCountDuplicateBypass = encounterState != null &&
                    GoblinPandemoniumMultiCountDuplicatePolicy.ShouldBypass(
                        candidate.Source,
                        step.AreaKey,
                        pfGuardResult.AreaCount,
                        pfGuardResult.AreaLimit,
                        encounterState.AreaKey,
                        encounterState.CountedUtc,
                        step.TimestampUtc,
                        evidenceSignature,
                        candidate.Confidence,
                        ReplayAutomaticMinimapCountMinimumConfidenceFor(candidate.GoblinType),
                        Math.Max(0, (step.TimestampUtc - state.FirstSeenUtc).TotalSeconds),
                        combatActive: true,
                        out _,
                        out _);
                if (!pfMultiCountDuplicateBypass)
                {
                    reason = string.Equals(state.CountedAreaKey, step.AreaKey, StringComparison.OrdinalIgnoreCase)
                        ? "EvidenceAlreadyAutoCounted"
                        : "EncounterAlreadyAutoCounted";
                    refreshEncounterLastSeen = encounterState != null &&
                        GoblinAutoCountEncounterSuppressionPolicy.ShouldRefreshEncounterLastSeenAfterSuppression(
                            candidate.Source,
                            step.AreaKey,
                            encounterState.AreaKey);
                }
            }
            if (string.IsNullOrWhiteSpace(reason) &&
                !pfMultiCountDuplicateBypass &&
                encounterState != null &&
                GoblinAutoCountEncounterSuppressionPolicy.ShouldSuppress(
                    candidate.Source,
                    candidate.GoblinType,
                    step.AreaKey,
                    evidenceSignature,
                    encounterState.GoblinType,
                    encounterState.AreaKey,
                    encounterState.Source,
                    encounterState.EvidenceKey,
                    encounterState.CountedUtc,
                    encounterState.LastSeenUtc,
                    step.TimestampUtc,
                    TimeSpan.FromMinutes(10),
                    TimeSpan.FromSeconds(45),
                    out _))
            {
                GoblinAreaDuplicateGuardResult pfGuardResult = duplicateGuard.Peek(step.AreaKey);
                pfMultiCountDuplicateBypass = GoblinPandemoniumMultiCountDuplicatePolicy.ShouldBypass(
                    candidate.Source,
                    step.AreaKey,
                    pfGuardResult.AreaCount,
                    pfGuardResult.AreaLimit,
                    encounterState.AreaKey,
                    encounterState.CountedUtc,
                    step.TimestampUtc,
                    evidenceSignature,
                    candidate.Confidence,
                    ReplayAutomaticMinimapCountMinimumConfidenceFor(candidate.GoblinType),
                    Math.Max(0, (step.TimestampUtc - state.FirstSeenUtc).TotalSeconds),
                    combatActive: true,
                    out _,
                    out _);
                if (!pfMultiCountDuplicateBypass)
                {
                    reason = "EncounterAlreadyAutoCounted";
                    refreshEncounterLastSeen = GoblinAutoCountEncounterSuppressionPolicy.ShouldRefreshEncounterLastSeenAfterSuppression(
                        candidate.Source,
                        step.AreaKey,
                        encounterState.AreaKey);
                }
            }
            if (string.IsNullOrWhiteSpace(reason))
            {
                if (!GoblinAutoCountEvidenceReliabilityPolicy.AllowsAutomaticCount(
                    candidate.Source,
                    evidenceSignature,
                    Math.Max(0, (step.TimestampUtc - state.FirstSeenUtc).TotalSeconds),
                    combatActive: true,
                    out string reliabilityReason,
                    out _))
                {
                    reason = reliabilityReason;
                }
                else if (GoblinManualCountBlockList.IsBlocked(step.AreaKey))
                {
                    reason = "BlockedArea";
                }
                else if (!duplicateGuard.TryAccept(step.AreaKey, out GoblinAreaDuplicateGuardResult guardResult))
                {
                    reason = guardResult.AreaLimit > 1 ? "AreaLimitReached" : "AreaAlreadyCounted";
                    refreshEncounterLastSeen = encounterState != null &&
                        GoblinAutoCountEncounterSuppressionPolicy.ShouldRefreshEncounterLastSeenAfterAreaAlreadyCounted(
                            candidate.Source,
                            step.AreaKey,
                            encounterState.AreaKey);
                }
                else
                {
                    evidenceBySignature[evidenceSignature] = state with
                    {
                        Counted = true,
                        CountedAreaKey = step.AreaKey,
                    };
                    if (!string.IsNullOrWhiteSpace(encounterKey))
                    {
                        encounterByGoblinType[encounterKey] = new ReplayEncounterState(
                            step.TimestampUtc,
                            step.TimestampUtc,
                            step.AreaKey,
                            candidate.GoblinType,
                            candidate.Source,
                            evidenceSignature);
                    }

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
            }

            if (refreshEncounterLastSeen &&
                !string.IsNullOrWhiteSpace(encounterKey) &&
                encounterState != null)
            {
                encounterByGoblinType[encounterKey] = encounterState with
                {
                    LastSeenUtc = step.TimestampUtc,
                    GoblinType = candidate.GoblinType,
                    Source = candidate.Source,
                    EvidenceKey = evidenceSignature,
                };
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

        private static void RememberReplayStaleVisibleLine(
            List<ReplayStaleVisibleLineState>? staleVisibleLineSuppressions,
            string signature,
            DateTime nowUtc)
        {
            if (staleVisibleLineSuppressions == null || string.IsNullOrWhiteSpace(signature))
            {
                return;
            }

            staleVisibleLineSuppressions.RemoveAll(state =>
                state.Signature.Equals(signature, StringComparison.OrdinalIgnoreCase) ||
                !GoblinJournalFreshnessPolicy.StaleSuppressionActive(state.LastSeenUtc, nowUtc, ReplayJournalFreshnessWindow));
            staleVisibleLineSuppressions.Add(new ReplayStaleVisibleLineState(signature, nowUtc, nowUtc));
        }

        private static bool TrySuppressReplayStaleVisibleLine(
            string signature,
            DateTime nowUtc,
            List<ReplayStaleVisibleLineState>? staleVisibleLineSuppressions,
            out string freshnessReason)
        {
            freshnessReason = "";
            if (staleVisibleLineSuppressions == null || staleVisibleLineSuppressions.Count == 0)
            {
                return false;
            }

            for (int index = staleVisibleLineSuppressions.Count - 1; index >= 0; index--)
            {
                ReplayStaleVisibleLineState state = staleVisibleLineSuppressions[index];
                if (!GoblinJournalFreshnessPolicy.StaleSuppressionActive(state.LastSeenUtc, nowUtc, ReplayJournalFreshnessWindow))
                {
                    staleVisibleLineSuppressions.RemoveAt(index);
                    continue;
                }

                if (!GoblinJournalEvidencePolicy.SameVisibleGoblinLine(signature, state.Signature, out int currentBucket, out int staleBucket))
                {
                    continue;
                }

                staleVisibleLineSuppressions[index] = state with { LastSeenUtc = nowUtc };
                freshnessReason = $"JournalCandidateIgnoredStaleVisibleLine:{currentBucket}->{staleBucket}";
                return true;
            }

            return false;
        }

        private static GoblinReplayFixtureCandidate? SelectScenarioCandidate(IReadOnlyList<GoblinReplayFixtureCandidate> candidates)
        {
            List<GoblinReplayFixtureCandidate> passedCandidates = candidates
                .Where(candidate => candidate.PassedThreshold)
                .ToList();
            GoblinReplayFixtureCandidate? journalCandidate = passedCandidates
                .Where(candidate => candidate.Source.Equals("JournalCandidate", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(candidate => candidate.Confidence)
                .FirstOrDefault();
            GoblinReplayFixtureCandidate? strongMinimapCandidate = passedCandidates
                .Where(IsStrongReplayMinimapCandidate)
                .OrderByDescending(candidate => candidate.Confidence)
                .FirstOrDefault();

            if (journalCandidate != null &&
                IsReplayPendingJournalEngagedCandidate(journalCandidate) &&
                strongMinimapCandidate != null)
            {
                return strongMinimapCandidate;
            }

            return journalCandidate ?? passedCandidates
                .OrderBy(candidate => string.Equals(candidate.Source, "JournalCandidate", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenByDescending(candidate => candidate.Confidence)
                .FirstOrDefault();
        }

        private static bool IsReplayPendingJournalEngagedCandidate(GoblinReplayFixtureCandidate candidate)
        {
            return candidate.Source.Equals("JournalCandidate", StringComparison.OrdinalIgnoreCase) &&
                candidate.EvidenceType == GoblinEvidenceType.JournalEncounter &&
                candidate.EvidenceKind == GoblinEvidenceTemplateKind.JournalEngaged;
        }

        private static bool IsStrongReplayMinimapCandidate(GoblinReplayFixtureCandidate candidate)
        {
            return candidate.Source.Equals("MinimapCandidate", StringComparison.OrdinalIgnoreCase) &&
                candidate.Confidence >= ReplayAutomaticMinimapCountMinimumConfidenceFor(candidate.GoblinType);
        }

        private static double ReplayAutomaticMinimapCountMinimumConfidenceFor(string goblinType)
        {
            string normalized = GoblinTypeNormalizer.Normalize(goblinType);
            return normalized.Equals("Gilded Baron", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Malevolent Tormentor", StringComparison.OrdinalIgnoreCase)
                ? 0.90
                : 0.85;
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

            return ReadCaptureMetadataFile(metadataPath);
        }

        private static Dictionary<string, string> ReadCaptureMetadataFile(string metadataPath)
        {
            Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase);
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

        private static Dictionary<string, string> ReadDecisionBundleMetadata(string bundlePath)
        {
            Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase);
            string? tracePath = DecisionBundleTracePath(bundlePath);
            if (string.IsNullOrWhiteSpace(tracePath) || !File.Exists(tracePath))
            {
                return metadata;
            }

            foreach (string line in File.ReadLines(tracePath))
            {
                if (line.StartsWith("GoblinDecisionTrace:", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string part in line["GoblinDecisionTrace:".Length..].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        int separator = part.IndexOf('=');
                        if (separator <= 0)
                        {
                            continue;
                        }

                        string key = part[..separator].Trim();
                        string value = part[(separator + 1)..].Trim();
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            metadata[key] = value;
                        }
                    }

                    continue;
                }

                int normalSeparator = line.IndexOf('=');
                if (normalSeparator <= 0)
                {
                    continue;
                }

                string normalKey = line[..normalSeparator].Trim();
                string normalValue = line[(normalSeparator + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(normalKey))
                {
                    metadata[normalKey] = normalValue;
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

        private static string PrefixFromMetadataPath(string metadataPath)
        {
            string fullPath = Path.GetFullPath(metadataPath);
            return fullPath.EndsWith("_Metadata.txt", StringComparison.OrdinalIgnoreCase)
                ? fullPath[..^"_Metadata.txt".Length]
                : Path.Combine(Path.GetDirectoryName(fullPath) ?? "", Path.GetFileNameWithoutExtension(fullPath));
        }

        private static string? DecisionBundleTracePath(string bundlePath)
        {
            string candidate = Path.Combine(bundlePath, "decision-trace.txt");
            return File.Exists(candidate)
                ? candidate
                : Directory.EnumerateFiles(bundlePath, "*trace*.txt", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .ThenByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
        }

        private static string? DecisionBundleEvidencePath(string bundlePath)
        {
            string candidate = Path.Combine(bundlePath, "evidence.png");
            return File.Exists(candidate)
                ? candidate
                : Directory.EnumerateFiles(bundlePath, "evidence.*", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .ThenByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
        }

        private static string? ResolveDecisionBundleReplayPrefix(string bundlePath, IReadOnlyDictionary<string, string> metadata)
        {
            string? localPrefix = Directory
                .EnumerateFiles(bundlePath, "*_Metadata.txt", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ThenByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .Select(PrefixFromMetadataPath)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(localPrefix))
            {
                return localPrefix;
            }

            string sourceImagePath = MetadataValue(metadata, "sourceImagePath", "imagePath");
            if (string.IsNullOrWhiteSpace(sourceImagePath))
            {
                return null;
            }

            string resolvedSourceImagePath = ResolveCaptureMetadataPath(bundlePath, sourceImagePath);
            if (!File.Exists(resolvedSourceImagePath))
            {
                return null;
            }

            if (resolvedSourceImagePath.EndsWith("_Journal.png", StringComparison.OrdinalIgnoreCase))
            {
                return resolvedSourceImagePath[..^"_Journal.png".Length];
            }

            if (resolvedSourceImagePath.EndsWith("_Minimap.png", StringComparison.OrdinalIgnoreCase))
            {
                return resolvedSourceImagePath[..^"_Minimap.png".Length];
            }

            return null;
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

        private sealed record ReplayEncounterState(
            DateTime CountedUtc,
            DateTime LastSeenUtc,
            string AreaKey,
            string GoblinType,
            string Source,
            string EvidenceKey);

        private sealed record ReplayResetCarryoverState(
            string Signature,
            DateTime ResetUtc,
            DateTime LastSeenUtc,
            string ResetReason);

        private sealed record ReplayStaleVisibleLineState(
            string Signature,
            DateTime FirstSuppressedUtc,
            DateTime LastSeenUtc);
    }
}

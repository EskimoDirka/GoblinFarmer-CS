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

    internal static class GoblinReplayFixtureRunner
    {
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

        private static string LogField(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "None";
            }

            return value.Replace('\r', ' ').Replace('\n', ' ').Replace(';', ',');
        }
    }
}

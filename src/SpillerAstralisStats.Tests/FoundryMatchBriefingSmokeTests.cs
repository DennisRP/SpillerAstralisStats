using Microsoft.Extensions.Options;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Configuration;
using SpillerAstralisStats.Infrastructure.Foundry;
using Xunit.Abstractions;

namespace SpillerAstralisStats.Tests;

public sealed class FoundryMatchBriefingSmokeTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Generates_a_valid_Danish_briefing_when_explicitly_enabled()
    {
        var configuration = FoundrySmokeTestConfiguration.Load(
            Path.Combine(AppContext.BaseDirectory, "local.settings.json"));
        if (!configuration.IsEnabled)
        {
            return;
        }

        var options = new FoundryOptions
        {
            Endpoint = configuration.Endpoint ?? string.Empty,
            DeploymentName = configuration.DeploymentName ?? string.Empty
        };
        if (!options.TryValidate(out var failure))
        {
            throw new InvalidOperationException($"Foundry smoke test configuration is invalid: {failure}");
        }

        var client = new FoundryMatchBriefingClient(Options.Create(options));
        var generator = new MatchBriefingGenerator(client);

        var result = await generator.GenerateAsync(FixedGrounding(), CancellationToken.None);

        MatchBriefingValidator.Validate(result.Briefing);
        Assert.Equal(MatchBriefingContract.PromptVersion, result.PromptVersion);
        Assert.Equal(MatchBriefingContract.SchemaVersion, result.SchemaVersion);
        output.WriteLine($"Headline: {result.Briefing.Headline}");
        output.WriteLine($"Summary: {result.Briefing.Summary}");
        output.WriteLine($"Key points: {string.Join(" | ", result.Briefing.KeyPoints)}");
        output.WriteLine($"Prompt version: {result.PromptVersion}");
        output.WriteLine($"Schema version: {result.SchemaVersion}");
        output.WriteLine($"Grounding version: {result.Grounding.GroundingVersion}");
        output.WriteLine($"Target: Astralis vs {result.Grounding.Target.OpponentName} at {result.Grounding.Target.ScheduledAt:O}");
        output.WriteLine($"Evidence IDs: {string.Join(", ", result.Grounding.EvidenceMatchProviderIds)}");
    }

    private static MatchFactsSnapshot FixedFacts()
    {
        var latestMeeting = new MatchFact(
            1001,
            new DateTimeOffset(2026, 8, 20, 18, 0, 0, TimeSpan.Zero),
            456,
            "G2",
            MatchOutcome.Loss,
            1,
            2,
            3,
            null,
            null,
            null,
            false);
        var recentWin = new MatchFact(
            1002,
            new DateTimeOffset(2026, 8, 24, 18, 0, 0, TimeSpan.Zero),
            789,
            "Vitality",
            MatchOutcome.Win,
            2,
            0,
            3,
            null,
            null,
            null,
            true);

        return new MatchFactsSnapshot(
            [recentWin, latestMeeting],
            new RecentFormFacts([MatchOutcome.Win, MatchOutcome.Loss], 1, 1, 0, 50m),
            new HeadToHeadFacts(456, "G2", 1, 0, 1, 0, [latestMeeting], latestMeeting));
    }

    private static GroundedMatchBriefingContext FixedGrounding()
    {
        var facts = FixedFacts();
        return new GroundedMatchBriefingContext(
            GroundedMatchBriefingBuilder.GroundingVersion,
            new GroundedMatchTarget(2001, new DateTimeOffset(2026, 8, 28, 18, 0, 0, TimeSpan.Zero), 456, "G2", null, null, null),
            MatchBriefingInputMapper.Map(facts),
            new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero),
            [1001, 1002]);
    }
}

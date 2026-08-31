using System.Text.Json;
using SpillerAstralisStats.Application;

namespace SpillerAstralisStats.Tests;

public sealed class MatchBriefingGeneratorTests
{
    [Fact]
    public async Task Sends_exact_versioned_contract_and_returns_validated_result()
    {
        var client = new StubClient(new MatchBriefingModelResponse(
            """{"headline":"Astralis møder G2 igen","summary":"Historikken viser tætte opgør.","keyPoints":["G2 vandt det seneste møde 2-1."]}""",
            null));
        var generator = new MatchBriefingGenerator(client);

        var result = await generator.GenerateAsync(Grounding(), CancellationToken.None);

        Assert.Equal(MatchBriefingContract.DeveloperInstruction, client.Request?.Instructions);
        Assert.Equal(MatchBriefingContract.SchemaName, client.Request?.SchemaName);
        Assert.Equal(MatchBriefingContract.JsonSchema, client.Request?.JsonSchema);
        Assert.Equal(MatchBriefingContract.PromptVersion, result.PromptVersion);
        Assert.Equal(MatchBriefingContract.SchemaVersion, result.SchemaVersion);
        Assert.Equal(GroundedMatchBriefingBuilder.GroundingVersion, result.Grounding.GroundingVersion);
        Assert.Equal("Astralis møder G2 igen", result.Briefing.Headline);
        Assert.Equal("Historikken viser tætte opgør.", result.Briefing.Summary);
        Assert.Equal(new[] { "G2 vandt det seneste møde 2-1." }, result.Briefing.KeyPoints);

        using var input = JsonDocument.Parse(Assert.IsType<string>(client.Request?.InputJson));
        Assert.Equal(GroundedMatchBriefingBuilder.GroundingVersion, input.RootElement.GetProperty("groundingVersion").GetString());
        Assert.Equal(999, input.RootElement.GetProperty("target").GetProperty("providerId").GetInt64());
        Assert.Equal(3, input.RootElement.GetProperty("evidence").GetProperty("recentForm").GetProperty("wins").GetInt32());
        Assert.Equal(2, input.RootElement.GetProperty("evidence").GetProperty("recentForm").GetProperty("losses").GetInt32());
        Assert.Equal(456, input.RootElement.GetProperty("evidence").GetProperty("headToHead").GetProperty("opponentTeamId").GetInt64());
        Assert.Equal("loss", input.RootElement.GetProperty("evidence").GetProperty("headToHead").GetProperty("latestMeeting").GetProperty("outcome").GetString());
        Assert.Equal(42, Assert.Single(input.RootElement.GetProperty("evidenceMatchProviderIds").EnumerateArray()).GetInt64());
    }

    [Fact]
    public async Task Rejects_provider_refusal_without_a_partial_result()
    {
        var generator = new MatchBriefingGenerator(new StubClient(new MatchBriefingModelResponse(null, "Cannot comply")));

        var exception = await Assert.ThrowsAsync<MatchBriefingRefusalException>(
            () => generator.GenerateAsync(Grounding(), CancellationToken.None));

        Assert.Contains("Cannot comply", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"headline\":\"H\",\"summary\":\"S\",\"keyPoints\":[\"K\"],\"extra\":true}")]
    [InlineData("{\"summary\":\"S\",\"keyPoints\":[\"K\"]}")]
    [InlineData("{\"headline\":\" \",\"summary\":\"S\",\"keyPoints\":[\"K\"]}")]
    public async Task Rejects_malformed_schema_incompatible_or_semantically_invalid_output(string output)
    {
        var generator = new MatchBriefingGenerator(new StubClient(new MatchBriefingModelResponse(output, null)));

        await Assert.ThrowsAsync<MatchBriefingOutputException>(
            () => generator.GenerateAsync(Grounding(), CancellationToken.None));
    }

    [Fact]
    public async Task Rejects_empty_output()
    {
        var generator = new MatchBriefingGenerator(new StubClient(new MatchBriefingModelResponse(" ", null)));

        await Assert.ThrowsAsync<MatchBriefingOutputException>(
            () => generator.GenerateAsync(Grounding(), CancellationToken.None));
    }

    [Fact]
    public async Task Propagates_cancellation_to_the_provider()
    {
        var client = new CancellationClient();
        var generator = new MatchBriefingGenerator(client);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => generator.GenerateAsync(Grounding(), cancellation.Token));

        Assert.Equal(cancellation.Token, client.ReceivedToken);
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    private static MatchFactsSnapshot Facts()
    {
        var match = new MatchFact(
            42,
            new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero),
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

        return new MatchFactsSnapshot(
            [match],
            new RecentFormFacts([MatchOutcome.Win, MatchOutcome.Win, MatchOutcome.Win, MatchOutcome.Loss, MatchOutcome.Loss], 3, 2, 0, 60m),
            new HeadToHeadFacts(456, "G2", 1, 0, 1, 0, [match], match));
    }

    private static GroundedMatchBriefingContext Grounding()
    {
        var facts = Facts();
        return new GroundedMatchBriefingContext(
            GroundedMatchBriefingBuilder.GroundingVersion,
            new GroundedMatchTarget(999, new DateTimeOffset(2026, 9, 1, 18, 0, 0, TimeSpan.Zero), 456, "G2", null, null, null),
            MatchBriefingInputMapper.Map(facts),
            new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero),
            [42]);
    }

    private sealed class StubClient(MatchBriefingModelResponse response) : IMatchBriefingClient
    {
        public MatchBriefingModelRequest? Request { get; private set; }

        public Task<MatchBriefingModelResponse> GenerateAsync(
            MatchBriefingModelRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(response);
        }
    }

    private sealed class CancellationClient : IMatchBriefingClient
    {
        public CancellationToken ReceivedToken { get; private set; }

        public async Task<MatchBriefingModelResponse> GenerateAsync(
            MatchBriefingModelRequest request,
            CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable.");
        }
    }
}

using SpillerAstralisStats.Application;
using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Tests;

public sealed class GroundedMatchBriefingBuilderTests
{
    private static readonly DateTimeOffset ReferenceTime = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Selects_the_next_target_and_builds_ordered_provenance_from_historical_facts()
    {
        var context = GroundedMatchBriefingBuilder.Build(new GroundedMatchBriefingRequest(
            [Finished(30, 456, ReferenceTime.AddDays(-1), 456), Finished(10, 789, ReferenceTime.AddDays(-2), 3209), Finished(20, 456, ReferenceTime.AddDays(-3), 3209)],
            [Upcoming(200, 456, ReferenceTime.AddDays(2)), Upcoming(100, 456, ReferenceTime.AddDays(2)), Upcoming(300, 789, ReferenceTime.AddDays(3))],
            3,
            ReferenceTime));

        var result = Assert.IsType<GroundedMatchBriefingContext>(context);
        Assert.Equal(100, result.Target.ProviderId);
        Assert.Equal(456, result.Target.OpponentTeamId);
        Assert.Equal(GroundedMatchBriefingBuilder.GroundingVersion, result.GroundingVersion);
        Assert.Equal(ReferenceTime, result.ReferenceTime);
        Assert.Equal([10L, 20L, 30L], result.EvidenceMatchProviderIds);
        Assert.Equal(2, result.Evidence.HeadToHead.Meetings);
        Assert.Equal(1, result.Evidence.HeadToHead.AstralisWins);
        Assert.Equal(1, result.Evidence.HeadToHead.OpponentWins);
    }

    [Fact]
    public void Ignores_ineligible_candidates_and_returns_no_context()
    {
        var context = GroundedMatchBriefingBuilder.Build(new GroundedMatchBriefingRequest(
            [],
            [Upcoming(1, 456, ReferenceTime.AddDays(1), "canceled"), Upcoming(2, 456, ReferenceTime.AddDays(1), null), Upcoming(3, null, ReferenceTime.AddDays(1))],
            5,
            ReferenceTime));

        Assert.Null(context);
    }

    [Fact]
    public void Rejects_a_non_positive_recent_match_count()
    {
        var request = new GroundedMatchBriefingRequest([], [Upcoming(1, 456, ReferenceTime)], 0, ReferenceTime);

        Assert.Throws<ArgumentOutOfRangeException>(() => GroundedMatchBriefingBuilder.Build(request));
    }

    [Fact]
    public void Retains_an_empty_head_to_head_when_there_is_no_previous_meeting()
    {
        var context = GroundedMatchBriefingBuilder.Build(new GroundedMatchBriefingRequest(
            [Finished(10, 789, ReferenceTime.AddDays(-1), 3209)],
            [Upcoming(1, 456, ReferenceTime)],
            5,
            ReferenceTime));

        var result = Assert.IsType<GroundedMatchBriefingContext>(context);
        Assert.Equal(0, result.Evidence.HeadToHead.Meetings);
        Assert.Null(result.Evidence.HeadToHead.LatestMeeting);
    }

    [Fact]
    public async Task No_target_does_not_invoke_the_model()
    {
        var client = new RecordingClient();
        var service = new GroundedMatchBriefingService(() => new MatchBriefingGenerator(client));

        var result = await service.GenerateAsync(new GroundedMatchBriefingRequest([], [], 5, ReferenceTime), CancellationToken.None);

        Assert.Null(result);
        Assert.False(client.WasCalled);
    }

    private static MatchRecord Upcoming(long id, long? opponentId, DateTimeOffset scheduledAt, string? status = "not_started") =>
        new(id, scheduledAt, null, null, status, null,
            opponentId is null
                ? [new OpponentRecord(3209, "Astralis")]
                : [new OpponentRecord(3209, "Astralis"), new OpponentRecord(opponentId, "Opponent")],
            [], 3, "best_of", null, null, null, [], null);

    private static MatchRecord Finished(long id, long opponentId, DateTimeOffset beginAt, long winnerId) =>
        new(id, beginAt, beginAt, beginAt.AddHours(1), "finished", winnerId,
            [new OpponentRecord(3209, "Astralis"), new OpponentRecord(opponentId, "Opponent")],
            [new ScoreRecord(3209, winnerId == 3209 ? 2 : 1), new ScoreRecord(opponentId, winnerId == opponentId ? 2 : 1)],
            3, "best_of", null, null, null, [], null);

    private sealed class RecordingClient : IMatchBriefingClient
    {
        public bool WasCalled { get; private set; }

        public Task<MatchBriefingModelResponse> GenerateAsync(MatchBriefingModelRequest request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            throw new InvalidOperationException("The model must not be called without grounding.");
        }
    }
}

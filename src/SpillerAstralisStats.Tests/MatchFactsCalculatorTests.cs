using SpillerAstralisStats.Application;
using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Tests;

public sealed class MatchFactsCalculatorTests
{
    private static readonly DateTimeOffset Newest = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Rejects_invalid_request_without_a_result()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Calculate([], 0, 456));
        Assert.Throws<ArgumentOutOfRangeException>(() => Calculate([], 5, 3209));
        Assert.Throws<ArgumentOutOfRangeException>(() => Calculate([], 5, 0));
    }

    [Fact]
    public void Filters_deduplicates_and_orders_eligible_matches()
    {
        var matches = new[]
        {
            Match(1, Newest.AddDays(-1)),
            Match(2, Newest, winner: 456),
            Match(2, Newest, winner: 3209),
            Match(3, Newest, status: "running"),
            Match(4, Newest, includeAstralis: false),
            Match(5, Newest, hasTimestamp: false),
            Match(6, Newest, opponent: null)
        };

        var result = Calculate(matches, 10, 456);

        Assert.Equal(new long[] { 2, 1 }, result.RecentMatches.Select(match => match.ProviderId));

        var limitedResult = Calculate(matches, 1, 456);
        Assert.Equal(2, Assert.Single(limitedResult.RecentMatches).ProviderId);
    }

    [Fact]
    public void Calculates_recent_form_and_excludes_unknown_from_percentage()
    {
        var matches = new[]
        {
            Match(1, Newest, winner: 3209, astralisScore: 2, opponentScore: 0),
            Match(2, Newest.AddDays(-1), winner: 456, astralisScore: 1, opponentScore: 2),
            Match(3, Newest.AddDays(-2), winner: null)
        };

        var form = Calculate(matches, 5, 456).RecentForm;

        Assert.Equal(new[] { MatchOutcome.Win, MatchOutcome.Loss, MatchOutcome.Unknown }, form.Outcomes);
        Assert.Equal(1, form.Wins);
        Assert.Equal(1, form.Losses);
        Assert.Equal(1, form.Unknown);
        Assert.Equal(50.0m, form.WinPercentage);
    }

    [Fact]
    public void Returns_null_percentage_when_all_outcomes_are_unknown()
    {
        var form = Calculate([Match(1, Newest, winner: null)], 5, 456).RecentForm;

        Assert.Null(form.WinPercentage);
        Assert.Equal(1, form.Unknown);
    }

    [Fact]
    public void Calculates_head_to_head_for_only_the_requested_opponent()
    {
        var matches = new[]
        {
            Match(1, Newest, winner: 3209),
            Match(2, Newest.AddDays(-1), winner: 456),
            Match(3, Newest.AddDays(-2), opponent: 789, winner: 789)
        };

        var headToHead = Calculate(matches, 5, 456).HeadToHead;

        Assert.Equal(2, headToHead.Meetings);
        Assert.Equal(1, headToHead.AstralisWins);
        Assert.Equal(1, headToHead.OpponentWins);
        Assert.Equal(0, headToHead.Unknown);
        Assert.Equal(new long[] { 1, 2 }, headToHead.Matches.Select(match => match.ProviderId));
        Assert.Equal(1, headToHead.LatestMeeting?.ProviderId);
    }

    [Fact]
    public void Returns_empty_head_to_head_when_no_previous_meeting_exists()
    {
        var headToHead = Calculate([Match(1, Newest, opponent: 789)], 5, 456).HeadToHead;

        Assert.Equal(0, headToHead.Meetings);
        Assert.Empty(headToHead.Matches);
        Assert.Null(headToHead.LatestMeeting);
    }

    [Fact]
    public void Retains_details_and_derives_sweep_from_complete_scores()
    {
        var match = Match(1, Newest, winner: 3209, astralisScore: 2, opponentScore: 0) with
        {
            NumberOfGames = 3,
            League = new CompetitionRecord(7, "League", "A", "DK", null),
            Tournament = new CompetitionRecord(8, "Event", "S", "SE", "2,000 Dollar")
        };

        var fact = Calculate([match], 5, 456).RecentMatches[0];

        Assert.Equal(2, fact.AstralisScore);
        Assert.Equal(0, fact.OpponentScore);
        Assert.Equal(3, fact.NumberOfGames);
        Assert.Equal("League", fact.League?.Name);
        Assert.Equal("Event", fact.Tournament?.Name);
        Assert.True(fact.IsSweep);
    }

    [Fact]
    public void Leaves_sweep_unavailable_when_scores_are_incomplete()
    {
        var fact = Calculate([Match(1, Newest, winner: 3209)], 5, 456).RecentMatches[0];

        Assert.Null(fact.AstralisScore);
        Assert.Null(fact.OpponentScore);
        Assert.Null(fact.IsSweep);
    }

    private static MatchFactsSnapshot Calculate(IReadOnlyList<MatchRecord> matches, int count, long opponent) =>
        MatchFactsCalculator.Calculate(matches, new MatchFactsRequest(count, opponent));

    private static MatchRecord Match(
        long id,
        DateTimeOffset timestamp,
        long? opponent = 456,
        long? winner = 3209,
        string status = "finished",
        bool includeAstralis = true,
        bool hasTimestamp = true,
        int? astralisScore = null,
        int? opponentScore = null) =>
        new(
            id,
            hasTimestamp ? timestamp : null,
            null,
            null,
            status,
            winner,
            [
                ..(includeAstralis ? new[] { new OpponentRecord(3209, "Astralis") } : Array.Empty<OpponentRecord>()),
                ..(opponent is null ? Array.Empty<OpponentRecord>() : new[] { new OpponentRecord(opponent, "Opponent") })
            ],
            [
                ..(astralisScore is null ? Array.Empty<ScoreRecord>() : new[] { new ScoreRecord(3209, astralisScore) }),
                ..(opponentScore is null || opponent is null ? Array.Empty<ScoreRecord>() : new[] { new ScoreRecord(opponent, opponentScore) })
            ],
            null,
            "best_of",
            null,
            null,
            null,
            [],
            null);
}

using SpillerAstralisStats.Application;
using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Tests;

public sealed class MatchBriefingInputMapperTests
{
    [Fact]
    public void Maps_existing_facts_without_recalculating_values()
    {
        var timestamp = new DateTimeOffset(2026, 8, 20, 12, 30, 0, TimeSpan.FromHours(2));
        var competition = new CompetitionRecord(17, "IEM Cologne", "S", "DE", "1,000,000 USD");
        var match = new MatchFact(
            42,
            timestamp,
            456,
            "G2",
            MatchOutcome.Loss,
            1,
            2,
            3,
            competition,
            null,
            competition,
            false);
        var source = new MatchFactsSnapshot(
            [match],
            new RecentFormFacts([MatchOutcome.Loss], 7, 5, 2, 58.3m),
            new HeadToHeadFacts(456, "G2", 9, 3, 5, 1, [match], match));

        var result = MatchBriefingInputMapper.Map(source);

        var recentMatch = Assert.Single(result.RecentMatches);
        Assert.Equal(42, recentMatch.ProviderId);
        Assert.Equal(timestamp, recentMatch.RelevantAt);
        Assert.Equal(456, recentMatch.OpponentTeamId);
        Assert.Equal("G2", recentMatch.OpponentName);
        Assert.Equal("loss", recentMatch.Outcome);
        Assert.Equal(1, recentMatch.AstralisScore);
        Assert.Equal(2, recentMatch.OpponentScore);
        Assert.Equal(3, recentMatch.NumberOfGames);
        Assert.False(recentMatch.IsSweep);
        Assert.Equal(competition.ProviderId, recentMatch.League?.ProviderId);
        Assert.Equal(competition.Name, recentMatch.League?.Name);
        Assert.Equal(competition.Tier, recentMatch.League?.Tier);
        Assert.Equal(competition.Country, recentMatch.League?.Country);
        Assert.Equal(competition.PrizePool, recentMatch.League?.PrizePool);
        Assert.Null(recentMatch.Serie);
        Assert.Equal(competition, ToCompetitionRecord(recentMatch.Tournament));

        Assert.Equal(new[] { "loss" }, result.RecentForm.Outcomes);
        Assert.Equal(7, result.RecentForm.Wins);
        Assert.Equal(5, result.RecentForm.Losses);
        Assert.Equal(2, result.RecentForm.Unknown);
        Assert.Equal(58.3m, result.RecentForm.WinPercentage);

        Assert.Equal(456, result.HeadToHead.OpponentTeamId);
        Assert.Equal("G2", result.HeadToHead.OpponentName);
        Assert.Equal(9, result.HeadToHead.Meetings);
        Assert.Equal(3, result.HeadToHead.AstralisWins);
        Assert.Equal(5, result.HeadToHead.OpponentWins);
        Assert.Equal(1, result.HeadToHead.Unknown);
        Assert.Single(result.HeadToHead.Matches);
        Assert.Equal(42, result.HeadToHead.LatestMeeting?.ProviderId);
    }

    [Fact]
    public void Rejects_null_facts()
    {
        Assert.Throws<ArgumentNullException>(() => MatchBriefingInputMapper.Map(null!));
    }

    private static CompetitionRecord? ToCompetitionRecord(MatchBriefingCompetitionFacts? source) => source is null
        ? null
        : new CompetitionRecord(source.ProviderId, source.Name, source.Tier, source.Country, source.PrizePool);
}

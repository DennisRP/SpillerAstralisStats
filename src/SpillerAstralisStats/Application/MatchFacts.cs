using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Application;

internal sealed record MatchFactsRequest(int RecentMatchCount, long OpponentTeamId)
{
    public void Validate()
    {
        if (RecentMatchCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RecentMatchCount), "The recent match count must be positive.");
        }

        if (OpponentTeamId <= 0 || OpponentTeamId == MatchFactsCalculator.AstralisTeamId)
        {
            throw new ArgumentOutOfRangeException(nameof(OpponentTeamId), "The opponent team ID must be positive and different from Astralis.");
        }
    }
}

internal enum MatchOutcome
{
    Unknown,
    Win,
    Loss
}

internal sealed record MatchFact(
    long ProviderId,
    DateTimeOffset RelevantAt,
    long OpponentTeamId,
    string? OpponentName,
    MatchOutcome Outcome,
    int? AstralisScore,
    int? OpponentScore,
    int? NumberOfGames,
    CompetitionRecord? League,
    CompetitionRecord? Serie,
    CompetitionRecord? Tournament,
    bool? IsSweep);

internal sealed record RecentFormFacts(
    IReadOnlyList<MatchOutcome> Outcomes,
    int Wins,
    int Losses,
    int Unknown,
    decimal? WinPercentage);

internal sealed record HeadToHeadFacts(
    long OpponentTeamId,
    string? OpponentName,
    int Meetings,
    int AstralisWins,
    int OpponentWins,
    int Unknown,
    IReadOnlyList<MatchFact> Matches,
    MatchFact? LatestMeeting);

internal sealed record MatchFactsSnapshot(
    IReadOnlyList<MatchFact> RecentMatches,
    RecentFormFacts RecentForm,
    HeadToHeadFacts HeadToHead);

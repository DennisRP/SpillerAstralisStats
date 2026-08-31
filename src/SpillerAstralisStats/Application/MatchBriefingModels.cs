using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Application;

internal sealed record MatchBriefingInput(
    IReadOnlyList<MatchBriefingMatchFacts> RecentMatches,
    MatchBriefingRecentFormFacts RecentForm,
    MatchBriefingHeadToHeadFacts HeadToHead);

internal sealed record MatchBriefingMatchFacts(
    long ProviderId,
    DateTimeOffset RelevantAt,
    long OpponentTeamId,
    string? OpponentName,
    string Outcome,
    int? AstralisScore,
    int? OpponentScore,
    int? NumberOfGames,
    MatchBriefingCompetitionFacts? League,
    MatchBriefingCompetitionFacts? Serie,
    MatchBriefingCompetitionFacts? Tournament,
    bool? IsSweep);

internal sealed record MatchBriefingRecentFormFacts(
    IReadOnlyList<string> Outcomes,
    int Wins,
    int Losses,
    int Unknown,
    decimal? WinPercentage);

internal sealed record MatchBriefingHeadToHeadFacts(
    long OpponentTeamId,
    string? OpponentName,
    int Meetings,
    int AstralisWins,
    int OpponentWins,
    int Unknown,
    IReadOnlyList<MatchBriefingMatchFacts> Matches,
    MatchBriefingMatchFacts? LatestMeeting);

internal sealed record MatchBriefingCompetitionFacts(
    long? ProviderId,
    string? Name,
    string? Tier,
    string? Country,
    string? PrizePool);

public sealed record MatchBriefing(
    string Headline,
    string Summary,
    IReadOnlyList<string> KeyPoints);

public sealed record MatchBriefingResult(
    MatchBriefing Briefing,
    string PromptVersion,
    string SchemaVersion);

internal static class MatchBriefingInputMapper
{
    public static MatchBriefingInput Map(MatchFactsSnapshot source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new MatchBriefingInput(
            source.RecentMatches.Select(MapMatch).ToArray(),
            new MatchBriefingRecentFormFacts(
                source.RecentForm.Outcomes.Select(MapOutcome).ToArray(),
                source.RecentForm.Wins,
                source.RecentForm.Losses,
                source.RecentForm.Unknown,
                source.RecentForm.WinPercentage),
            new MatchBriefingHeadToHeadFacts(
                source.HeadToHead.OpponentTeamId,
                source.HeadToHead.OpponentName,
                source.HeadToHead.Meetings,
                source.HeadToHead.AstralisWins,
                source.HeadToHead.OpponentWins,
                source.HeadToHead.Unknown,
                source.HeadToHead.Matches.Select(MapMatch).ToArray(),
                source.HeadToHead.LatestMeeting is null ? null : MapMatch(source.HeadToHead.LatestMeeting)));
    }

    private static MatchBriefingMatchFacts MapMatch(MatchFact source) => new(
        source.ProviderId,
        source.RelevantAt,
        source.OpponentTeamId,
        source.OpponentName,
        MapOutcome(source.Outcome),
        source.AstralisScore,
        source.OpponentScore,
        source.NumberOfGames,
        MapCompetition(source.League),
        MapCompetition(source.Serie),
        MapCompetition(source.Tournament),
        source.IsSweep);

    private static string MapOutcome(MatchOutcome outcome) => outcome.ToString().ToLowerInvariant();

    private static MatchBriefingCompetitionFacts? MapCompetition(CompetitionRecord? source) => source is null
        ? null
        : new MatchBriefingCompetitionFacts(
            source.ProviderId,
            source.Name,
            source.Tier,
            source.Country,
            source.PrizePool);
}

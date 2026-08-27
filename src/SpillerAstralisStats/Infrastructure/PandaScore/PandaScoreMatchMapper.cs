using System.Globalization;
using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Infrastructure.PandaScore;

internal static class PandaScoreMatchMapper
{
    public static MatchRecord Map(PandaScoreMatchDto source) => new(
        source.Id,
        ParseDate(source.ScheduledAt),
        ParseDate(source.BeginAt),
        ParseDate(source.EndAt),
        source.Status,
        source.WinnerId,
        (source.Opponents ?? []).Select(x => new OpponentRecord(x.Opponent?.Id, x.Opponent?.Name)).ToArray(),
        (source.Results ?? []).Select(x => new ScoreRecord(x.TeamId, x.Score)).ToArray(),
        source.NumberOfGames,
        source.MatchType,
        MapCompetition(source.League),
        MapCompetition(source.Serie),
        MapCompetition(source.Tournament),
        (source.Games ?? []).Select(x => new GameRecord(ParseDate(x.BeginAt), ParseDate(x.EndAt), x.Length, x.Winner?.Id, x.Status)).ToArray(),
        source.Rescheduled);

    private static CompetitionRecord? MapCompetition(PandaScoreCompetitionDto? source) => source is null
        ? null
        : new CompetitionRecord(source.Id, source.Name, source.Tier, source.Country, source.PrizePool);

    private static DateTimeOffset? ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
}

namespace SpillerAstralisStats.Domain;

public sealed record MatchRecord(
    long ProviderId,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? BeginAt,
    DateTimeOffset? EndAt,
    string? Status,
    long? WinnerTeamId,
    IReadOnlyList<OpponentRecord> Opponents,
    IReadOnlyList<ScoreRecord> Scores,
    int? NumberOfGames,
    string? MatchType,
    CompetitionRecord? League,
    CompetitionRecord? Serie,
    CompetitionRecord? Tournament,
    IReadOnlyList<GameRecord> Games,
    bool? Rescheduled);

public sealed record OpponentRecord(long? TeamId, string? Name);

public sealed record ScoreRecord(long? TeamId, int? Score);

public sealed record GameRecord(
    DateTimeOffset? BeginAt,
    DateTimeOffset? EndAt,
    int? DurationSeconds,
    long? WinnerTeamId,
    string? Status);

public sealed record CompetitionRecord(
    long? ProviderId,
    string? Name,
    string? Tier,
    string? Country,
    string? PrizePool);

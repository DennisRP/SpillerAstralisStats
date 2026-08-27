using System.Text.Json.Serialization;

namespace SpillerAstralisStats.Infrastructure.PandaScore;

internal sealed class PandaScoreMatchDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("scheduled_at")] public string? ScheduledAt { get; set; }
    [JsonPropertyName("begin_at")] public string? BeginAt { get; set; }
    [JsonPropertyName("end_at")] public string? EndAt { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("winner_id")] public long? WinnerId { get; set; }
    [JsonPropertyName("opponents")] public List<PandaScoreOpponentDto>? Opponents { get; set; }
    [JsonPropertyName("results")] public List<PandaScoreScoreDto>? Results { get; set; }
    [JsonPropertyName("number_of_games")] public int? NumberOfGames { get; set; }
    [JsonPropertyName("match_type")] public string? MatchType { get; set; }
    [JsonPropertyName("league")] public PandaScoreCompetitionDto? League { get; set; }
    [JsonPropertyName("serie")] public PandaScoreCompetitionDto? Serie { get; set; }
    [JsonPropertyName("tournament")] public PandaScoreCompetitionDto? Tournament { get; set; }
    [JsonPropertyName("games")] public List<PandaScoreGameDto>? Games { get; set; }
    [JsonPropertyName("rescheduled")] public bool? Rescheduled { get; set; }
}

internal sealed class PandaScoreOpponentDto
{
    [JsonPropertyName("opponent")] public PandaScoreTeamDto? Opponent { get; set; }
}

internal sealed class PandaScoreTeamDto
{
    [JsonPropertyName("id")] public long? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
}

internal sealed class PandaScoreScoreDto
{
    [JsonPropertyName("team_id")] public long? TeamId { get; set; }
    [JsonPropertyName("score")] public int? Score { get; set; }
}

internal sealed class PandaScoreGameDto
{
    [JsonPropertyName("begin_at")] public string? BeginAt { get; set; }
    [JsonPropertyName("end_at")] public string? EndAt { get; set; }
    [JsonPropertyName("length")] public int? Length { get; set; }
    [JsonPropertyName("winner")] public PandaScoreTeamDto? Winner { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
}

internal sealed class PandaScoreCompetitionDto
{
    [JsonPropertyName("id")] public long? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("tier")] public string? Tier { get; set; }
    [JsonPropertyName("country")] public string? Country { get; set; }
    [JsonPropertyName("prizepool")] public string? PrizePool { get; set; }
}

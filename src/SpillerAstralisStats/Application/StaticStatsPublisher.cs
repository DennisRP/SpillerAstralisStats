using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SpillerAstralisStats.Configuration;
using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Application;

public sealed class StaticStatsPublisher(IOptions<StaticStatsOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task<StaticStatsPublication> PublishAsync(
        IReadOnlyList<MatchRecord> source,
        StaticBriefingAsset briefing,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(briefing);

        var settings = options.Value;
        if (!settings.TryValidate(out var failure))
        {
            throw new InvalidOperationException(failure);
        }

        var snapshot = CreateSnapshot(source, settings.RecentMatchCount);
        var outputPath = Path.GetFullPath(settings.OutputPath);
        var parentPath = Directory.GetParent(outputPath)?.FullName
            ?? throw new InvalidOperationException("Static stats output path has no parent directory.");
        Directory.CreateDirectory(parentPath);

        var stagingPath = $"{outputPath}.staging-{Guid.NewGuid():N}";
        var backupPath = $"{outputPath}.backup-{Guid.NewGuid():N}";
        try
        {
            Directory.CreateDirectory(stagingPath);
            await File.WriteAllTextAsync(
                Path.Combine(stagingPath, "matches.json"),
                JsonSerializer.Serialize(snapshot.Matches, JsonOptions),
                cancellationToken);
            await File.WriteAllTextAsync(
                Path.Combine(stagingPath, "stats.json"),
                JsonSerializer.Serialize(snapshot.Stats, JsonOptions),
                cancellationToken);
            await File.WriteAllTextAsync(
                Path.Combine(stagingPath, "briefing.json"),
                JsonSerializer.Serialize(briefing, JsonOptions),
                cancellationToken);

            if (Directory.Exists(outputPath))
            {
                Directory.Move(outputPath, backupPath);
            }

            Directory.Move(stagingPath, outputPath);
            if (Directory.Exists(backupPath))
            {
                Directory.Delete(backupPath, true);
            }
        }
        catch
        {
            if (Directory.Exists(outputPath) && Directory.Exists(backupPath))
            {
                Directory.Delete(outputPath, true);
            }

            if (!Directory.Exists(outputPath) && Directory.Exists(backupPath))
            {
                Directory.Move(backupPath, outputPath);
            }

            throw;
        }
        finally
        {
            if (Directory.Exists(stagingPath))
            {
                Directory.Delete(stagingPath, true);
            }
        }

        return new StaticStatsPublication(snapshot.Matches.Count, snapshot.Stats.HeadToHead.Count);
    }

    public Task<StaticStatsPublication> PublishAsync(
        IReadOnlyList<MatchRecord> source,
        CancellationToken cancellationToken) =>
        PublishAsync(source, StaticBriefingAsset.Unavailable(StaticBriefingUnavailableReason.NoEligibleTarget), cancellationToken);

    private static StaticStatsSnapshot CreateSnapshot(IReadOnlyList<MatchRecord> source, int recentMatchCount)
    {
        var opponentIds = source
            .SelectMany(match => match.Opponents)
            .Where(opponent => opponent.TeamId is > 0 and not MatchFactsCalculator.AstralisTeamId)
            .Select(opponent => opponent.TeamId!.Value)
            .Distinct()
            .Order()
            .ToArray();

        var factsByOpponent = opponentIds
            .Select(opponentId => MatchFactsCalculator.Calculate(source, new MatchFactsRequest(recentMatchCount, opponentId)))
            .ToArray();
        IReadOnlyList<MatchFact> allFacts = opponentIds.Length == 0
            ? []
            : MatchFactsCalculator.Calculate(source, new MatchFactsRequest(int.MaxValue, opponentIds[0])).RecentMatches;
        var recentForm = factsByOpponent.FirstOrDefault()?.RecentForm ??
            new RecentFormFacts([], 0, 0, 0, null);

        return new StaticStatsSnapshot(
            allFacts.Select(ToMatchDto).ToArray(),
            new StaticStatsDto(
                ToFormDto(recentForm),
                factsByOpponent.Select(facts => ToHeadToHeadDto(facts.HeadToHead)).ToArray()));
    }

    private static StaticMatchDto ToMatchDto(MatchFact fact) => new(
        fact.ProviderId,
        fact.RelevantAt,
        fact.OpponentTeamId,
        fact.OpponentName,
        fact.Outcome.ToString().ToLowerInvariant(),
        fact.AstralisScore,
        fact.OpponentScore,
        fact.NumberOfGames,
        fact.League,
        fact.Serie,
        fact.Tournament,
        fact.IsSweep);

    private static StaticRecentFormDto ToFormDto(RecentFormFacts form) => new(
        form.Outcomes.Select(outcome => outcome.ToString().ToLowerInvariant()).ToArray(),
        form.Wins,
        form.Losses,
        form.Unknown,
        form.WinPercentage);

    private static StaticHeadToHeadDto ToHeadToHeadDto(HeadToHeadFacts facts) => new(
        facts.OpponentTeamId,
        facts.OpponentName,
        facts.Meetings,
        facts.AstralisWins,
        facts.OpponentWins,
        facts.Unknown,
        facts.Matches.Select(ToMatchDto).ToArray(),
        facts.LatestMeeting is null ? null : ToMatchDto(facts.LatestMeeting));
}

public sealed record StaticStatsPublication(int MatchCount, int OpponentSummaryCount);

public enum StaticBriefingUnavailableReason
{
    NoEligibleTarget,
    GenerationFailed
}

public sealed record StaticBriefingAsset(
    string Availability,
    MatchBriefingResult? Briefing,
    string? UnavailableReason)
{
    public const string Available = "available";
    public const string UnavailableState = "unavailable";

    public static StaticBriefingAsset FromAvailable(MatchBriefingResult briefing)
    {
        ArgumentNullException.ThrowIfNull(briefing);
        return new(Available, briefing, null);
    }

    public static StaticBriefingAsset Unavailable(StaticBriefingUnavailableReason reason) =>
        new(UnavailableState, null, reason switch
        {
            StaticBriefingUnavailableReason.NoEligibleTarget => "noEligibleTarget",
            StaticBriefingUnavailableReason.GenerationFailed => "generationFailed",
            _ => throw new ArgumentOutOfRangeException(nameof(reason))
        });
}

internal sealed record StaticStatsSnapshot(IReadOnlyList<StaticMatchDto> Matches, StaticStatsDto Stats);

internal sealed record StaticMatchDto(
    long ProviderId,
    DateTimeOffset RelevantAt,
    long OpponentTeamId,
    string? OpponentName,
    string Outcome,
    int? AstralisScore,
    int? OpponentScore,
    int? NumberOfGames,
    CompetitionRecord? League,
    CompetitionRecord? Serie,
    CompetitionRecord? Tournament,
    bool? IsSweep);

internal sealed record StaticStatsDto(StaticRecentFormDto RecentForm, IReadOnlyList<StaticHeadToHeadDto> HeadToHead);

internal sealed record StaticRecentFormDto(
    IReadOnlyList<string> Outcomes,
    int Wins,
    int Losses,
    int Unknown,
    decimal? WinPercentage);

internal sealed record StaticHeadToHeadDto(
    long OpponentTeamId,
    string? OpponentName,
    int Meetings,
    int AstralisWins,
    int OpponentWins,
    int Unknown,
    IReadOnlyList<StaticMatchDto> Matches,
    StaticMatchDto? LatestMeeting);

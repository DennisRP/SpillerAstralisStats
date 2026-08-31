using Microsoft.Extensions.Options;
using SpillerAstralisStats.Configuration;
using SpillerAstralisStats.Domain;
using SpillerAstralisStats.Infrastructure.PandaScore;

namespace SpillerAstralisStats.Application;

internal sealed class PandaScoreMatchIngestionService(
    PandaScoreMatchClient client,
    IOptions<PandaScoreOptions> options,
    TimeProvider timeProvider) : IPandaScoreMatchIngestionService
{
    public async Task<PandaScoreIngestionResult> IngestAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.TryValidate(out var configurationFailure))
        {
            return PandaScoreIngestionResult.Failure(IngestionFailureCategory.Configuration, configurationFailure, settings.HistoryLookbackMonths);
        }

        IReadOnlyList<PandaScoreMatchDto> providerMatches;
        try
        {
            providerMatches = await client.GetAllMatchesAsync(cancellationToken);
        }
        catch (PandaScoreResponseException exception)
        {
            return PandaScoreIngestionResult.Failure(IngestionFailureCategory.ProviderResponse, exception.Message, settings.HistoryLookbackMonths);
        }
        catch (PandaScorePayloadException exception)
        {
            return PandaScoreIngestionResult.Failure(IngestionFailureCategory.Payload, exception.Message, settings.HistoryLookbackMonths);
        }
        catch (HttpRequestException exception)
        {
            return PandaScoreIngestionResult.Failure(IngestionFailureCategory.Transport, exception.Message, settings.HistoryLookbackMonths);
        }

        var now = timeProvider.GetUtcNow();
        var start = now.AddMonths(-settings.HistoryLookbackMonths);
        var mappedMatches = providerMatches
            .Select(PandaScoreMatchMapper.Map)
            .ToArray();
        var upcomingMatches = mappedMatches
            .Where(match => IsUpcomingCandidate(match, now))
            .OrderBy(match => match.ScheduledAt)
            .ThenBy(match => match.ProviderId)
            .ToArray();
        var matches = mappedMatches
            .Where(match => IsInWindow(match, start, now) && !IsUpcomingCandidate(match, now))
            .OrderByDescending(match => match.BeginAt ?? match.ScheduledAt)
            .ToArray();

        return PandaScoreIngestionResult.Success(matches, upcomingMatches, settings.HistoryLookbackMonths);
    }

    private static bool IsInWindow(MatchRecord match, DateTimeOffset start, DateTimeOffset end)
    {
        var relevantTime = match.BeginAt ?? match.ScheduledAt;
        return relevantTime is not null && relevantTime >= start && relevantTime <= end;
    }

    private static bool IsUpcomingCandidate(MatchRecord match, DateTimeOffset now) =>
        match.ScheduledAt is { } scheduledAt
        && scheduledAt >= now
        && string.Equals(match.Status, "not_started", StringComparison.OrdinalIgnoreCase)
        && match.Opponents.Any(opponent => opponent.TeamId == MatchFactsCalculator.AstralisTeamId)
        && match.Opponents.Count(opponent => opponent.TeamId is > 0 and not MatchFactsCalculator.AstralisTeamId) == 1;
}

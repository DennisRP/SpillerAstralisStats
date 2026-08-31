using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Application;

public sealed record GroundedMatchBriefingRequest(
    IReadOnlyList<MatchRecord> HistoricalMatches,
    IReadOnlyList<MatchRecord> UpcomingMatches,
    int RecentMatchCount,
    DateTimeOffset ReferenceTime)
{
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(HistoricalMatches);
        ArgumentNullException.ThrowIfNull(UpcomingMatches);

        if (RecentMatchCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RecentMatchCount), "The recent match count must be positive.");
        }
    }
}

public sealed record GroundedMatchBriefingContext(
    string GroundingVersion,
    GroundedMatchTarget Target,
    MatchBriefingInput Evidence,
    DateTimeOffset ReferenceTime,
    IReadOnlyList<long> EvidenceMatchProviderIds);

public sealed record GroundedMatchTarget(
    long ProviderId,
    DateTimeOffset ScheduledAt,
    long OpponentTeamId,
    string? OpponentName,
    MatchBriefingCompetitionFacts? League,
    MatchBriefingCompetitionFacts? Serie,
    MatchBriefingCompetitionFacts? Tournament);

public static class GroundedMatchBriefingBuilder
{
    public const string GroundingVersion = "grounded-match-briefing-v1";

    public static GroundedMatchBriefingContext? Build(GroundedMatchBriefingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var target = request.UpcomingMatches
            .Where(match => IsEligibleTarget(match, request.ReferenceTime))
            .OrderBy(match => match.ScheduledAt)
            .ThenBy(match => match.ProviderId)
            .FirstOrDefault();
        if (target is null)
        {
            return null;
        }

        var opponent = target.Opponents.Single(opponent => opponent.TeamId is > 0 and not MatchFactsCalculator.AstralisTeamId);
        var facts = MatchFactsCalculator.Calculate(
            request.HistoricalMatches,
            new MatchFactsRequest(request.RecentMatchCount, opponent.TeamId!.Value));
        var evidenceIds = facts.RecentMatches
            .Concat(facts.HeadToHead.Matches)
            .Select(match => match.ProviderId)
            .Distinct()
            .Order()
            .ToArray();

        return new GroundedMatchBriefingContext(
            GroundingVersion,
            new GroundedMatchTarget(
                target.ProviderId,
                target.ScheduledAt!.Value,
                opponent.TeamId.Value,
                opponent.Name,
                MatchBriefingInputMapper.MapCompetition(target.League),
                MatchBriefingInputMapper.MapCompetition(target.Serie),
                MatchBriefingInputMapper.MapCompetition(target.Tournament)),
            MatchBriefingInputMapper.Map(facts),
            request.ReferenceTime,
            evidenceIds);
    }

    private static bool IsEligibleTarget(MatchRecord match, DateTimeOffset referenceTime) =>
        match.ProviderId > 0
        && match.ScheduledAt is { } scheduledAt
        && scheduledAt >= referenceTime
        && string.Equals(match.Status, "not_started", StringComparison.OrdinalIgnoreCase)
        && match.Opponents.Any(opponent => opponent.TeamId == MatchFactsCalculator.AstralisTeamId)
        && match.Opponents.Count(opponent => opponent.TeamId is > 0 and not MatchFactsCalculator.AstralisTeamId) == 1;
}

internal sealed class GroundedMatchBriefingService(MatchBriefingGenerator generator)
{
    public Task<MatchBriefingResult?> GenerateAsync(
        GroundedMatchBriefingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grounding = GroundedMatchBriefingBuilder.Build(request);
        return grounding is null
            ? Task.FromResult<MatchBriefingResult?>(null)
            : GenerateAsync(grounding, cancellationToken);
    }

    private async Task<MatchBriefingResult?> GenerateAsync(
        GroundedMatchBriefingContext grounding,
        CancellationToken cancellationToken) =>
        await generator.GenerateAsync(grounding, cancellationToken);
}

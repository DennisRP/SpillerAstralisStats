using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Configuration;

namespace SpillerAstralisStats;

public sealed class DailyStatsFunction(
    ILogger<DailyStatsFunction> logger,
    IPandaScoreMatchIngestionService ingestionService,
    StaticStatsPublisher staticStatsPublisher,
    IGroundedMatchBriefingService groundedMatchBriefingService,
    IOptions<StaticStatsOptions> staticStatsOptions,
    TimeProvider timeProvider)
{
    [Function(nameof(DailyStatsFunction))]
    public async Task Run(
        [TimerTrigger("0 37 13 * * *", RunOnStartup = true)] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Hello World");

        var result = await ingestionService.IngestAsync(cancellationToken);
        if (result.Succeeded)
        {
            logger.LogInformation(
                "PandaScore ingestion succeeded for {LookbackMonths}-month window with {MatchCount} matches.",
                result.LookbackMonths,
                result.Matches.Count);
            var briefing = await GenerateBriefingAssetAsync(result, cancellationToken);
            var publication = await staticStatsPublisher.PublishAsync(result.Matches, briefing, cancellationToken);
            logger.LogInformation(
                "Static stats data published with {MatchCount} matches and {OpponentSummaryCount} opponent summaries.",
                publication.MatchCount,
                publication.OpponentSummaryCount);
        }
        else
        {
            logger.LogError(
                "PandaScore ingestion failed ({FailureCategory}): {FailureMessage}",
                result.FailureCategory,
                result.FailureMessage);
        }
    }

    private async Task<StaticBriefingAsset> GenerateBriefingAssetAsync(
        PandaScoreIngestionResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            var briefing = await groundedMatchBriefingService.GenerateAsync(
                new GroundedMatchBriefingRequest(
                    result.Matches,
                    result.UpcomingMatches,
                    staticStatsOptions.Value.RecentMatchCount,
                    timeProvider.GetUtcNow()),
                cancellationToken);
            if (briefing is null)
            {
                logger.LogInformation("Grounded briefing is unavailable ({BriefingReason}).", "noEligibleTarget");
                return StaticBriefingAsset.Unavailable(StaticBriefingUnavailableReason.NoEligibleTarget);
            }

            logger.LogInformation("Grounded briefing is available.");
            return StaticBriefingAsset.FromAvailable(briefing);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            logger.LogWarning("Grounded briefing is unavailable ({BriefingReason}).", "generationFailed");
            return StaticBriefingAsset.Unavailable(StaticBriefingUnavailableReason.GenerationFailed);
        }
    }
}

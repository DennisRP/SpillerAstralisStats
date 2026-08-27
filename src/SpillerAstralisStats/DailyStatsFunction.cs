using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SpillerAstralisStats.Application;

namespace SpillerAstralisStats;

public sealed class DailyStatsFunction(
    ILogger<DailyStatsFunction> logger,
    IPandaScoreMatchIngestionService ingestionService,
    StaticStatsPublisher staticStatsPublisher)
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
            var publication = await staticStatsPublisher.PublishAsync(result.Matches, cancellationToken);
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
}

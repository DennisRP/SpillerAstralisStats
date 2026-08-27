using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SpillerAstralisStats;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Tests;

public sealed class DailyStatsFunctionTests
{
    [Fact]
    public async Task Run_logs_greeting_and_ingestion_summary()
    {
        var logger = new TestLogger<DailyStatsFunction>();
        var ingestion = new StubIngestionService(PandaScoreIngestionResult.Success(
            [new MatchRecord(1, null, null, null, "finished", null, [], [], null, null, null, null, null, [], null)], 12));
        var function = new DailyStatsFunction(logger, ingestion);

        await function.Run(null!, CancellationToken.None);

        Assert.Contains("Hello World", logger.Messages);
        Assert.Contains(logger.Messages, message => message.Contains("12-month window with 1 matches", StringComparison.Ordinal));
        Assert.Equal(CancellationToken.None, ingestion.ReceivedCancellationToken);
    }

    [Fact]
    public void Run_uses_the_expected_timer_configuration()
    {
        var method = typeof(DailyStatsFunction).GetMethod(nameof(DailyStatsFunction.Run));
        var timerTrigger = method?.GetParameters()[0].GetCustomAttribute<TimerTriggerAttribute>();

        Assert.NotNull(timerTrigger);
        Assert.Equal("0 37 13 * * *", timerTrigger!.Schedule);
        Assert.True(timerTrigger.RunOnStartup);
    }

    [Fact]
    public async Task Run_logs_failure_without_provider_payload_or_credential()
    {
        var logger = new TestLogger<DailyStatsFunction>();
        var ingestion = new StubIngestionService(PandaScoreIngestionResult.Failure(
            IngestionFailureCategory.ProviderResponse, "HTTP 500", 12));
        var function = new DailyStatsFunction(logger, ingestion);

        await function.Run(null!, CancellationToken.None);

        Assert.DoesNotContain(logger.Messages, message => message.Contains("secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("Authorization", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Messages, message => message.Contains("HTTP 500", StringComparison.Ordinal));
    }

    private sealed class StubIngestionService(PandaScoreIngestionResult result) : IPandaScoreMatchIngestionService
    {
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<PandaScoreIngestionResult> IngestAsync(CancellationToken cancellationToken)
        {
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(result);
        }
    }
}

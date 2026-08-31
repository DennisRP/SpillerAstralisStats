using System.Reflection;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SpillerAstralisStats;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Configuration;
using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Tests;

public sealed class DailyStatsFunctionTests
{
    [Fact]
    public async Task Run_logs_greeting_and_ingestion_summary()
    {
        var logger = new TestLogger<DailyStatsFunction>();
        var historical = new MatchRecord(1, new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero), null, null, "finished", 3209, [new OpponentRecord(3209, "Astralis"), new OpponentRecord(456, "G2")], [new ScoreRecord(3209, 2), new ScoreRecord(456, 0)], 3, "best_of", null, null, null, [], null);
        var upcoming = new MatchRecord(2, new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero), null, null, "not_started", null, [new OpponentRecord(3209, "Astralis"), new OpponentRecord(456, "G2")], [], 3, "best_of", null, null, null, [], null);
        var ingestion = new StubIngestionService(PandaScoreIngestionResult.Success([historical], [upcoming], 12));
        var outputPath = Path.Combine(Path.GetTempPath(), $"spiller-function-test-{Guid.NewGuid():N}");
        var function = new DailyStatsFunction(logger, ingestion, Publisher(outputPath));

        try
        {
            await function.Run(null!, CancellationToken.None);
            using var matches = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputPath, "matches.json")));
            Assert.Equal(1, Assert.Single(matches.RootElement.EnumerateArray()).GetProperty("providerId").GetInt64());
        }
        finally { if (Directory.Exists(outputPath)) Directory.Delete(outputPath, true); }

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
        var function = new DailyStatsFunction(logger, ingestion, Publisher(Path.Combine(Path.GetTempPath(), $"spiller-function-test-{Guid.NewGuid():N}")));

        await function.Run(null!, CancellationToken.None);

        Assert.DoesNotContain(logger.Messages, message => message.Contains("secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("Authorization", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Messages, message => message.Contains("HTTP 500", StringComparison.Ordinal));
    }

    private static StaticStatsPublisher Publisher(string outputPath) =>
        new(Microsoft.Extensions.Options.Options.Create(new StaticStatsOptions { OutputPath = outputPath }));

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

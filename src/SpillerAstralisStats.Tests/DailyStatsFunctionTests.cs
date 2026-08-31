using System.Reflection;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;
using SpillerAstralisStats;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Configuration;
using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Tests;

public sealed class DailyStatsFunctionTests
{
    private static readonly DateTimeOffset ReferenceTime = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Run_publishes_the_validated_grounded_briefing_without_recalculating_it()
    {
        var logger = new TestLogger<DailyStatsFunction>();
        var historical = Finished(1, 456, ReferenceTime.AddDays(-1));
        var upcoming = Upcoming(2, 456, ReferenceTime.AddDays(2));
        var ingestion = new StubIngestionService(PandaScoreIngestionResult.Success([historical], [upcoming], 12));
        var groundedService = new StubGroundedBriefingService(Briefing());
        var outputPath = Path.Combine(Path.GetTempPath(), $"spiller-function-test-{Guid.NewGuid():N}");

        try
        {
            await Function(logger, ingestion, outputPath, groundedService).Run(null!, CancellationToken.None);
            using var asset = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputPath, "briefing.json")));
            Assert.Equal("available", asset.RootElement.GetProperty("availability").GetString());
            Assert.Equal("Astralis møder G2", asset.RootElement.GetProperty("briefing").GetProperty("briefing").GetProperty("headline").GetString());
            Assert.Equal(999, asset.RootElement.GetProperty("briefing").GetProperty("grounding").GetProperty("target").GetProperty("providerId").GetInt64());
            Assert.True(File.Exists(Path.Combine(outputPath, "matches.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "stats.json")));
        }
        finally { if (Directory.Exists(outputPath)) Directory.Delete(outputPath, true); }

        var request = Assert.IsType<GroundedMatchBriefingRequest>(groundedService.ReceivedRequest);
        Assert.Same(ingestion.Result.Matches, request.HistoricalMatches);
        Assert.Same(ingestion.Result.UpcomingMatches, request.UpcomingMatches);
        Assert.Equal(5, request.RecentMatchCount);
        Assert.Equal(ReferenceTime, request.ReferenceTime);
        Assert.Contains(logger.Messages, message => message.Contains("Grounded briefing is available", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true, "noEligibleTarget")]
    [InlineData(false, "generationFailed")]
    public async Task Run_publishes_an_unavailable_briefing_without_failing_historical_generation(bool noTarget, string reason)
    {
        var logger = new TestLogger<DailyStatsFunction>();
        var ingestion = new StubIngestionService(PandaScoreIngestionResult.Success([Finished(1, 456, ReferenceTime.AddDays(-1))], [], 12));
        var briefingService = noTarget
            ? new StubGroundedBriefingService((MatchBriefingResult?)null)
            : new StubGroundedBriefingService(new InvalidOperationException("provider response must not be published"));
        var outputPath = Path.Combine(Path.GetTempPath(), $"spiller-function-test-{Guid.NewGuid():N}");

        try
        {
            await Function(logger, ingestion, outputPath, briefingService).Run(null!, CancellationToken.None);
            using var asset = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputPath, "briefing.json")));
            Assert.Equal("unavailable", asset.RootElement.GetProperty("availability").GetString());
            Assert.Equal(reason, asset.RootElement.GetProperty("unavailableReason").GetString());
            Assert.False(asset.RootElement.TryGetProperty("briefing", out _));
            Assert.True(File.Exists(Path.Combine(outputPath, "matches.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "stats.json")));
        }
        finally { if (Directory.Exists(outputPath)) Directory.Delete(outputPath, true); }

        Assert.Contains(logger.Messages, message => message.Contains(reason, StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("provider response", StringComparison.Ordinal));
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
        var ingestion = new StubIngestionService(PandaScoreIngestionResult.Failure(IngestionFailureCategory.ProviderResponse, "HTTP 500", 12));
        await Function(logger, ingestion, Path.Combine(Path.GetTempPath(), $"spiller-function-test-{Guid.NewGuid():N}"), new StubGroundedBriefingService((MatchBriefingResult?)null)).Run(null!, CancellationToken.None);
        Assert.DoesNotContain(logger.Messages, message => message.Contains("secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("Authorization", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Messages, message => message.Contains("HTTP 500", StringComparison.Ordinal));
    }

    private static DailyStatsFunction Function(TestLogger<DailyStatsFunction> logger, StubIngestionService ingestion, string outputPath, IGroundedMatchBriefingService briefingService) =>
        new(logger, ingestion, new StaticStatsPublisher(Options.Create(new StaticStatsOptions { OutputPath = outputPath })), briefingService,
            Options.Create(new StaticStatsOptions { OutputPath = outputPath, RecentMatchCount = 5 }), new FixedTimeProvider(ReferenceTime));

    private static MatchBriefingResult Briefing() => new(
        new MatchBriefing("Astralis møder G2", "En valideret briefing.", ["Et nøglepunkt."]), "prompt-v1", "schema-v1",
        new GroundedMatchBriefingContext("grounding-v1", new GroundedMatchTarget(999, ReferenceTime.AddDays(2), 456, "G2", null, null, null), new MatchBriefingInput([], new MatchBriefingRecentFormFacts([], 0, 0, 0, null), new MatchBriefingHeadToHeadFacts(456, "G2", 0, 0, 0, 0, [], null)), ReferenceTime, []));

    private static MatchRecord Finished(long id, long opponentId, DateTimeOffset at) => new(id, at, at, at.AddHours(1), "finished", 3209, [new OpponentRecord(3209, "Astralis"), new OpponentRecord(opponentId, "G2")], [new ScoreRecord(3209, 2), new ScoreRecord(opponentId, 0)], 3, "best_of", null, null, null, [], null);
    private static MatchRecord Upcoming(long id, long opponentId, DateTimeOffset at) => new(id, at, null, null, "not_started", null, [new OpponentRecord(3209, "Astralis"), new OpponentRecord(opponentId, "G2")], [], 3, "best_of", null, null, null, [], null);

    private sealed class StubIngestionService(PandaScoreIngestionResult result) : IPandaScoreMatchIngestionService
    {
        public PandaScoreIngestionResult Result { get; } = result;
        public Task<PandaScoreIngestionResult> IngestAsync(CancellationToken cancellationToken) => Task.FromResult(Result);
    }

    private sealed class StubGroundedBriefingService : IGroundedMatchBriefingService
    {
        private readonly MatchBriefingResult? result;
        private readonly Exception? exception;
        public StubGroundedBriefingService(MatchBriefingResult? result) => this.result = result;
        public StubGroundedBriefingService(Exception exception) => this.exception = exception;
        public GroundedMatchBriefingRequest? ReceivedRequest { get; private set; }
        public Task<MatchBriefingResult?> GenerateAsync(GroundedMatchBriefingRequest request, CancellationToken cancellationToken)
        {
            ReceivedRequest = request;
            if (exception is not null) throw exception;
            return Task.FromResult(result);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Configuration;
using SpillerAstralisStats.Domain;
using SpillerAstralisStats.Infrastructure.PandaScore;

namespace SpillerAstralisStats.Tests;

public sealed class PandaScoreMatchIngestionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Ingests_pages_maps_fields_and_filters_history()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal("Bearer test-secret", request.Headers.Authorization?.ToString());
            var query = request.RequestUri?.Query;
            return query == "?page=1&per_page=100"
                ? Response("[{\"id\":1,\"begin_at\":\"2026-08-20T12:00:00Z\",\"status\":\"finished\",\"winner_id\":3209,\"opponents\":[{\"opponent\":{\"id\":456,\"name\":\"G2\"}}],\"results\":[{\"team_id\":3209,\"score\":2}],\"number_of_games\":3,\"league\":{\"id\":7,\"name\":\"League\"},\"tournament\":{\"name\":\"Event\",\"prizepool\":\"2,000,000 United States Dollar\"}}]")
                : query == "?page=2&per_page=100"
                    ? Response("[{\"id\":1,\"begin_at\":\"2026-08-20T12:00:00Z\"},{\"id\":2,\"scheduled_at\":\"2026-08-27T12:00:00Z\"},{\"id\":3},{\"id\":4,\"begin_at\":\"2025-01-01T12:00:00Z\"}]")
                    : Response("[]");
        });
        var options = Options(new PandaScoreOptions { ApiKey = "test-secret" });
        var client = new PandaScoreMatchClient(new HttpClient(handler) { BaseAddress = new("https://api.pandascore.co/") }, options);
        var service = new PandaScoreMatchIngestionService(client, options, new FixedTimeProvider(Now));

        var result = await service.IngestAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        var match = Assert.Single(result.Matches);
        Assert.Equal(1, match.ProviderId);
        Assert.Equal("G2", Assert.Single(match.Opponents).Name);
        Assert.Equal(2, Assert.Single(match.Scores).Score);
        Assert.Equal("2,000,000 United States Dollar", match.Tournament?.PrizePool);
    }

    [Fact]
    public async Task Missing_key_does_not_send_request()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("request should not be sent"));
        var options = Options(new PandaScoreOptions());
        var client = new PandaScoreMatchClient(new HttpClient(handler) { BaseAddress = new("https://api.pandascore.co/") }, options);
        var service = new PandaScoreMatchIngestionService(client, options, new FixedTimeProvider(Now));

        var result = await service.IngestAsync(CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(IngestionFailureCategory.Configuration, result.FailureCategory);
    }

    [Fact]
    public async Task Invalid_lookback_does_not_send_request()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("request should not be sent"));
        var options = Options(new PandaScoreOptions { ApiKey = "secret", HistoryLookbackMonths = 5 });
        var client = new PandaScoreMatchClient(new HttpClient(handler) { BaseAddress = new("https://api.pandascore.co/") }, options);
        var service = new PandaScoreMatchIngestionService(client, options, new FixedTimeProvider(Now));

        var result = await service.IngestAsync(CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(IngestionFailureCategory.Configuration, result.FailureCategory);
    }

    [Fact]
    public async Task Non_success_response_is_categorized()
    {
        var options = Options(new PandaScoreOptions { ApiKey = "secret" });
        var client = new PandaScoreMatchClient(new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError))) { BaseAddress = new("https://api.pandascore.co/") }, options);
        var service = new PandaScoreMatchIngestionService(client, options, new FixedTimeProvider(Now));

        var result = await service.IngestAsync(CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(IngestionFailureCategory.ProviderResponse, result.FailureCategory);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task Malformed_payload_is_categorized_without_partial_data()
    {
        var options = Options(new PandaScoreOptions { ApiKey = "secret" });
        var client = new PandaScoreMatchClient(new HttpClient(new StubHttpMessageHandler(_ => Response("not-json"))) { BaseAddress = new("https://api.pandascore.co/") }, options);
        var service = new PandaScoreMatchIngestionService(client, options, new FixedTimeProvider(Now));

        var result = await service.IngestAsync(CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(IngestionFailureCategory.Payload, result.FailureCategory);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task Transport_failure_is_categorized()
    {
        var options = Options(new PandaScoreOptions { ApiKey = "secret" });
        var client = new PandaScoreMatchClient(new HttpClient(new StubHttpMessageHandler(_ => throw new HttpRequestException("network unavailable"))) { BaseAddress = new("https://api.pandascore.co/") }, options);
        var service = new PandaScoreMatchIngestionService(client, options, new FixedTimeProvider(Now));

        var result = await service.IngestAsync(CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(IngestionFailureCategory.Transport, result.FailureCategory);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task Cancellation_is_propagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var options = Options(new PandaScoreOptions { ApiKey = "secret" });
        var client = new PandaScoreMatchClient(new HttpClient(new StubHttpMessageHandler(_ => throw new OperationCanceledException(cancellation.Token))) { BaseAddress = new("https://api.pandascore.co/") }, options);
        var service = new PandaScoreMatchIngestionService(client, options, new FixedTimeProvider(Now));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.IngestAsync(cancellation.Token));
    }

    private static IOptions<PandaScoreOptions> Options(PandaScoreOptions options) => Microsoft.Extensions.Options.Options.Create(options);

    private static HttpResponseMessage Response(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}

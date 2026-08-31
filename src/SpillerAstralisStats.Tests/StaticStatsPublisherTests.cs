using System.Text.Json;
using Microsoft.Extensions.Options;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Configuration;
using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Tests;

public sealed class StaticStatsPublisherTests
{
    [Fact]
    public async Task Publishes_all_json_assets_from_completed_history()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"spiller-stats-{Guid.NewGuid():N}");
        try
        {
            var publisher = new StaticStatsPublisher(Options.Create(new StaticStatsOptions { OutputPath = outputPath }));
            var publication = await publisher.PublishAsync([Match(1, 3209)], CancellationToken.None);

            Assert.Equal(1, publication.MatchCount);
            Assert.Equal(1, publication.OpponentSummaryCount);
            Assert.True(File.Exists(Path.Combine(outputPath, "matches.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "stats.json")));
            Assert.True(File.Exists(Path.Combine(outputPath, "briefing.json")));

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputPath, "matches.json")));
            var match = Assert.Single(document.RootElement.EnumerateArray().ToArray());
            Assert.Equal("win", match.GetProperty("outcome").GetString());
            Assert.False(match.TryGetProperty("league", out _));

            using var briefing = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputPath, "briefing.json")));
            Assert.Equal("unavailable", briefing.RootElement.GetProperty("availability").GetString());
            Assert.Equal("noEligibleTarget", briefing.RootElement.GetProperty("unavailableReason").GetString());
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
        }
    }

    [Fact]
    public void Briefing_asset_serialization_only_contains_the_safe_available_or_unavailable_contract()
    {
        var available = StaticBriefingAsset.FromAvailable(new MatchBriefingResult(
            new MatchBriefing("Overskrift", "Opsummering", ["Punkt"]),
            "prompt-v1",
            "schema-v1",
            new GroundedMatchBriefingContext("grounding-v1", new GroundedMatchTarget(99, DateTimeOffset.UtcNow, 456, "G2", null, null, null), new MatchBriefingInput([], new MatchBriefingRecentFormFacts([], 0, 0, 0, null), new MatchBriefingHeadToHeadFacts(456, "G2", 0, 0, 0, 0, [], null)), DateTimeOffset.UtcNow, [])));
        var unavailable = StaticBriefingAsset.Unavailable(StaticBriefingUnavailableReason.GenerationFailed);

        var availableJson = JsonSerializer.Serialize(available, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var unavailableJson = JsonSerializer.Serialize(unavailable, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Contains("grounding", availableJson, StringComparison.Ordinal);
        Assert.DoesNotContain("exception", unavailableJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", unavailableJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("briefing", unavailableJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("generationFailed", unavailableJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Keeps_existing_generation_when_publication_fails()
    {
        var parentPath = Path.Combine(Path.GetTempPath(), $"spiller-stats-{Guid.NewGuid():N}");
        var outputPath = Path.Combine(parentPath, "data");
        try
        {
            var publisher = new StaticStatsPublisher(Options.Create(new StaticStatsOptions { OutputPath = outputPath }));
            await publisher.PublishAsync([Match(1, 3209)], CancellationToken.None);
            var previousMatches = await File.ReadAllTextAsync(Path.Combine(outputPath, "matches.json"));
            var previousStats = await File.ReadAllTextAsync(Path.Combine(outputPath, "stats.json"));
            var previousBriefing = await File.ReadAllTextAsync(Path.Combine(outputPath, "briefing.json"));
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => publisher.PublishAsync([Match(2, 3209)], cancellation.Token));

            Assert.Equal(previousMatches, await File.ReadAllTextAsync(Path.Combine(outputPath, "matches.json")));
            Assert.Equal(previousStats, await File.ReadAllTextAsync(Path.Combine(outputPath, "stats.json")));
            Assert.Equal(previousBriefing, await File.ReadAllTextAsync(Path.Combine(outputPath, "briefing.json")));
            Assert.Empty(Directory.GetDirectories(parentPath, "data.staging-*"));
        }
        finally
        {
            if (Directory.Exists(parentPath))
            {
                Directory.Delete(parentPath, true);
            }
        }
    }

    private static MatchRecord Match(long id, long winner) => new(
        id,
        new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero),
        null,
        null,
        "finished",
        winner,
        [new OpponentRecord(3209, "Astralis"), new OpponentRecord(456, "G2")],
        [new ScoreRecord(3209, 2), new ScoreRecord(456, 0)],
        3,
        "best_of",
        null,
        null,
        null,
        [],
        null);
}

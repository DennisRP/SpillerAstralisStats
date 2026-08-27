using System.Text.Json;
using Microsoft.Extensions.Options;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Configuration;
using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Tests;

public sealed class StaticStatsPublisherTests
{
    [Fact]
    public async Task Publishes_both_json_assets_from_completed_history()
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

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputPath, "matches.json")));
            var match = Assert.Single(document.RootElement.EnumerateArray().ToArray());
            Assert.Equal("win", match.GetProperty("outcome").GetString());
            Assert.False(match.TryGetProperty("league", out _));
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
    public async Task Keeps_existing_generation_when_publication_fails()
    {
        var parentPath = Path.Combine(Path.GetTempPath(), $"spiller-stats-{Guid.NewGuid():N}");
        var outputPath = Path.Combine(parentPath, "data");
        Directory.CreateDirectory(parentPath);
        await File.WriteAllTextAsync(outputPath, "existing-generation");
        try
        {
            var publisher = new StaticStatsPublisher(Options.Create(new StaticStatsOptions { OutputPath = outputPath }));

            await Assert.ThrowsAnyAsync<IOException>(() => publisher.PublishAsync([Match(1, 3209)], CancellationToken.None));

            Assert.Equal("existing-generation", await File.ReadAllTextAsync(outputPath));
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

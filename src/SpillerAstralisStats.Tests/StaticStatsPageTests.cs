using System.Text.Json;

namespace SpillerAstralisStats.Tests;

public sealed class StaticStatsPageTests
{
    [Fact]
    public void Static_page_contains_local_assets_and_required_states()
    {
        var pageRoot = FindPageRoot();
        var html = File.ReadAllText(Path.Combine(pageRoot, "index.html"));
        var script = File.ReadAllText(Path.Combine(pageRoot, "app.js"));
        var styles = File.ReadAllText(Path.Combine(pageRoot, "styles.css"));

        Assert.Contains("./styles.css", html);
        Assert.Contains("./app.js", html);
        Assert.Contains("id=\"loading-state\"", html);
        Assert.Contains("id=\"empty-state\"", html);
        Assert.Contains("id=\"error-state\"", html);
        Assert.Contains("./data/matches.json", script);
        Assert.Contains("./data/stats.json", script);
        Assert.DoesNotContain("https://", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@media (max-width: 700px)", styles);
    }

    [Fact]
    public async Task Static_data_contract_contains_the_fields_consumed_by_page()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"spiller-page-contract-{Guid.NewGuid():N}");
        try
        {
            var publisher = new Application.StaticStatsPublisher(
                Microsoft.Extensions.Options.Options.Create(new Configuration.StaticStatsOptions { OutputPath = outputPath }));
            await publisher.PublishAsync(
                [new Domain.MatchRecord(
                    1,
                    new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero),
                    null,
                    null,
                    "finished",
                    3209,
                    [new Domain.OpponentRecord(3209, "Astralis"), new Domain.OpponentRecord(456, "G2")],
                    [new Domain.ScoreRecord(3209, 2), new Domain.ScoreRecord(456, 0)],
                    3,
                    "best_of",
                    null,
                    null,
                    null,
                    [],
                    null)],
                CancellationToken.None);

            using var matches = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputPath, "matches.json")));
            using var stats = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputPath, "stats.json")));
            var match = Assert.Single(matches.RootElement.EnumerateArray().ToArray());

            Assert.True(match.TryGetProperty("relevantAt", out _));
            Assert.True(match.TryGetProperty("opponentName", out _));
            Assert.True(match.TryGetProperty("outcome", out _));
            Assert.True(stats.RootElement.TryGetProperty("recentForm", out var form));
            Assert.True(form.TryGetProperty("outcomes", out _));
            Assert.True(stats.RootElement.TryGetProperty("headToHead", out _));
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
        }
    }

    private static string FindPageRoot()
    {
        foreach (var startingPoint in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startingPoint);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "dist", "stats");
                if (File.Exists(Path.Combine(candidate, "index.html")))
                {
                    return candidate;
                }
                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not find the dist/stats static page.");
    }
}

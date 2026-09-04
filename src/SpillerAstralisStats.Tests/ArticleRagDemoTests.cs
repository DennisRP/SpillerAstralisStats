using System.Text.Json;
using SpillerAstralisStats.Application;

namespace SpillerAstralisStats.Tests;

public sealed class ArticleRagDemoTests
{
    [Fact]
    public async Task Produces_a_deterministic_network_free_evidence_and_failure_demo()
    {
        var first = await ArticleRagDemoRunner.RunAsync(FixtureDirectory, EvaluationPath);
        var second = await ArticleRagDemoRunner.RunAsync(FixtureDirectory, EvaluationPath);
        var trace = ArticleRagDemoTraceFormatter.Format(first);

        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Equal(2, first.Documents.Count);
        Assert.Equal(3, first.Chunks.Count);
        Assert.Equal(1d, first.EvaluationReport.AverageRecallAt3);
        Assert.Equal(1d, first.EvaluationReport.MeanReciprocalRank);
        Assert.Equal("available", first.SupportedEvidence.Generation.Answer.State);
        Assert.Equal(["synthetic-astralis-g2-recap#chunk-02"], first.SupportedEvidence.Generation.Answer.CitationChunkIds);
        Assert.Contains(first.SupportedEvidence.Generation.Answer.CitationChunkIds.Single(), first.SupportedEvidence.Generation.Context.Chunks.Select(chunk => chunk.ChunkId));
        Assert.Equal("retrievalInsufficientContext", first.RetrievalInsufficientContext.State);
        Assert.Equal(0, first.RetrievalInsufficientContext.GeneratorCallCount);
        Assert.Equal("modelSelectedInsufficientContext", first.ModelSelectedInsufficientContext.State);
        Assert.Equal(1, first.ModelSelectedInsufficientContext.GeneratorCallCount);
        Assert.Equal("generationValidationFailure", first.GenerationFailure.State);
        Assert.Equal(1, first.GenerationFailure.GeneratorCallCount);
        Assert.DoesNotContain("unretrieved#chunk-01", trace, StringComparison.Ordinal);
        Assert.DoesNotContain("Foundry", trace, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PandaScore", trace, StringComparison.OrdinalIgnoreCase);

        AssertStageOrder(trace,
            "=== 1. SYNTHETIC CORPUS ===",
            "=== 2. RETRIEVAL EVALUATION ===",
            "=== 3. SUPPORTED ANSWER (DETERMINISTIC FAKE MODEL) ===",
            "=== 4. RETRIEVAL-LEVEL INSUFFICIENT CONTEXT ===",
            "=== 5. MODEL-SELECTED INSUFFICIENT CONTEXT (DETERMINISTIC FAKE MODEL) ===",
            "=== 6. GENERATION VALIDATION FAILURE (DETERMINISTIC FAKE MODEL) ===");
        Assert.Contains("Citation membership: validated against supplied context.", trace, StringComparison.Ordinal);
        Assert.Contains("article-rag-prompt-v1", trace, StringComparison.Ordinal);
        Assert.Contains("article-bm25-v1", trace, StringComparison.Ordinal);
    }

    private static string FixtureDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Articles");
    private static string EvaluationPath => Path.Combine(FixtureDirectory, "evaluations.json");

    private static void AssertStageOrder(string trace, params string[] headings)
    {
        var previous = -1;
        foreach (var heading in headings)
        {
            var current = trace.IndexOf(heading, StringComparison.Ordinal);
            Assert.True(current > previous, $"Expected '{heading}' after the previous demo stage.");
            previous = current;
        }
    }
}

using System.Text.Json;
using SpillerAstralisStats.Application;

namespace SpillerAstralisStats.Tests;

public sealed class ArticleRagCorpusTests
{
    [Fact]
    public void Loads_redistributable_fixture_and_retains_provenance()
    {
        var result = ArticleCorpusLoader.Load(FixtureDirectory);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Equal(2, result.Documents.Count);
        var document = result.Documents.Single(item => item.DocumentId == "synthetic-astralis-g2-recap");
        Assert.Equal("synthetic-astralis-g2-recap", document.DocumentId);
        Assert.Equal("en", document.Language);
        Assert.Equal(new[] { "astralis", "g2", "fixture" }, document.Tags);
        Assert.Equal("https://example.test/astralis-g2-recap", document.SourceUrl.ToString().TrimEnd('/'));
    }

    [Fact]
    public void Rejects_an_invalid_corpus_without_returning_partial_documents()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "valid.md"), ValidDocument("valid"));
        File.WriteAllText(Path.Combine(directory.Path, "invalid.md"), "---\ndocumentId: Invalid\n---\n");

        var result = ArticleCorpusLoader.Load(directory.Path);

        Assert.False(result.IsValid);
        Assert.Empty(result.Documents);
        Assert.Contains(result.Errors, error => error.SourcePath.EndsWith("invalid.md", StringComparison.Ordinal));
    }

    [Fact]
    public void Chunks_are_deterministic_and_preserve_complete_long_paragraphs()
    {
        var document = new ArticleDocument("article", "Title", "Source", new Uri("https://example.test/a"), new DateOnly(2026, 8, 1), "en", ["fixture"], "# Heading\n\n" + string.Join(' ', Enumerable.Repeat("word", 320)), "memory.md");

        var first = ArticleChunker.Chunk(document);
        var second = ArticleChunker.Chunk(document);

        var chunk = Assert.Single(first);
        Assert.Equal("article#chunk-01", chunk.ChunkId);
        Assert.Equal("Heading", chunk.Heading);
        Assert.True(ArticleChunker.CountWords(chunk.Text) > ArticleChunker.TargetMaximumWords);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
    }

    [Fact]
    public void Retrieval_is_deterministic_filters_zero_scores_and_exposes_trace()
    {
        var chunks = new[]
        {
            Chunk("a#chunk-01", "Astralis G2 recap", "G2 won a close series."),
            Chunk("b#chunk-01", "Other", "Astralis prepared for a match."),
            Chunk("c#chunk-01", "Tie", "G2 match.")
        };
        var retriever = new ArticleBm25Retriever();

        var results = retriever.Search(chunks, "G2", 3);

        Assert.Equal(new[] { "a#chunk-01", "c#chunk-01" }, results.Select(result => result.Chunk.ChunkId).Order());
        Assert.All(results, result => Assert.True(result.Score > 0));
        Assert.Empty(retriever.Search(chunks, "unmatched", 3));
        Assert.Throws<ArgumentException>(() => retriever.Search(chunks, " ", 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => retriever.Search(chunks, "G2", 0));
        var trace = ArticleRetrievalTraceFormatter.Format("G2", results);
        Assert.Contains("a#chunk-01", trace, StringComparison.Ordinal);
        Assert.Contains("score=", trace, StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluates_positive_and_unsupported_queries_without_external_calls()
    {
        var corpus = ArticleCorpusLoader.Load(FixtureDirectory);
        var chunks = corpus.Documents.SelectMany(ArticleChunker.Chunk).ToArray();
        var evaluations = ArticleRagEvaluationLoader.Load(Path.Combine(FixtureDirectory, "evaluations.json"), chunks);

        Assert.True(evaluations.IsValid, string.Join(Environment.NewLine, evaluations.Errors));
        var report = ArticleRagEvaluator.Evaluate(chunks, evaluations.Cases, new ArticleBm25Retriever());

        Assert.Equal(2, report.Results.Count);
        Assert.Equal(1d, report.AverageRecallAt3);
        Assert.Equal(1d, report.MeanReciprocalRank);
        Assert.True(report.Results.Single(result => !result.EvaluationCase.ExpectsSupport).UnsupportedQueryRejected);
    }

    private static string FixtureDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Articles");

    private static string ValidDocument(string documentId) => $$"""
        ---
        documentId: {{documentId}}
        title: Valid title
        source: Test
        sourceUrl: https://example.test/{{documentId}}
        publishedAt: 2026-08-20
        language: en
        tags: [fixture]
        ---

        A valid body.
        """;

    private static ArticleChunk Chunk(string chunkId, string title, string text)
    {
        var documentId = chunkId.Split('#')[0];
        return new ArticleChunk(documentId, chunkId, title, "Test", new Uri("https://example.test/" + documentId), new DateOnly(2026, 8, 1), "en", ["fixture"], null, text, ArticleRagVersions.Chunker);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SpillerAstralisStatsTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }
        public string Path { get; }
        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }
}

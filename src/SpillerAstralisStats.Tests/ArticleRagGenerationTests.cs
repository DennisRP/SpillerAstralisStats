using System.Text.Json;
using SpillerAstralisStats.Application;

namespace SpillerAstralisStats.Tests;

public sealed class ArticleRagGenerationTests
{
    [Fact]
    public async Task Generates_a_cited_answer_from_only_the_serialized_context()
    {
        var client = new StubClient("""{"state":"available","answer":"Den syntetiske artikel beskriver en tæt serie.","citationChunkIds":["article#chunk-01"]}""");
        var generator = new ArticleRagGenerator(client);
        var context = Context();

        var result = await generator.GenerateAsync(context, CancellationToken.None);

        Assert.Equal("available", result.Answer.State);
        Assert.Equal(new[] { "article#chunk-01" }, result.Answer.CitationChunkIds);
        Assert.Equal(ArticleRagContract.PromptVersion, result.PromptVersion);
        Assert.Equal(ArticleRagContract.SchemaVersion, result.SchemaVersion);
        Assert.Equal(ArticleRagContract.DeveloperInstruction, client.Request?.Instructions);
        using var input = JsonDocument.Parse(Assert.IsType<string>(client.Request?.InputJson));
        Assert.Equal("article#chunk-01", input.RootElement.GetProperty("chunks")[0].GetProperty("chunkId").GetString());
        Assert.DoesNotContain("unretrieved", client.Request?.InputJson, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"state":"available","answer":"Text","citationChunkIds":["unretrieved#chunk-01"]}""")]
    [InlineData("""{"state":"available","answer":" ","citationChunkIds":["article#chunk-01"]}""")]
    [InlineData("""{"state":"insufficientContext","answer":"Text","citationChunkIds":[]}""")]
    [InlineData("""{"state":"available","answer":"Text","citationChunkIds":["article#chunk-01","article#chunk-01"]}""")]
    public async Task Rejects_invalid_citations_and_state_combinations(string output)
    {
        var generator = new ArticleRagGenerator(new StubClient(output));

        await Assert.ThrowsAsync<ArticleRagGenerationException>(() => generator.GenerateAsync(Context(), CancellationToken.None));
    }

    [Fact]
    public async Task Does_not_call_the_model_when_retrieval_has_no_positive_evidence()
    {
        var client = new StubClient("unreachable");
        var service = new ArticleRagService(new ArticleBm25Retriever(), new ArticleRagGenerator(client));
        var chunks = new[] { Chunk("article#chunk-01", "Astralis recap") };

        var result = await service.AnswerAsync(chunks, "xylophonic nebula", 3, CancellationToken.None);

        Assert.Equal("insufficientContext", result.State);
        Assert.Null(result.Context);
        Assert.Null(result.Generation);
        Assert.Null(client.Request);
    }

    [Fact]
    public async Task Retains_a_model_selected_insufficient_context_result()
    {
        var service = new ArticleRagService(new ArticleBm25Retriever(), new ArticleRagGenerator(new StubClient("""{"state":"insufficientContext","answer":null,"citationChunkIds":[]}""")));

        var result = await service.AnswerAsync([Chunk("article#chunk-01", "Astralis recap")], "Astralis", 3, CancellationToken.None);

        Assert.Equal("insufficientContext", result.State);
        Assert.NotNull(result.Context);
        Assert.NotNull(result.Generation);
    }

    private static ArticleRagContext Context() => ArticleRagContextBuilder.Create("What happened?", [new ArticleRetrievalResult(Chunk("article#chunk-01", "Synthetic recap"), 1, 1.2, ArticleRagVersions.Retriever, ArticleRagVersions.Tokenizer)]);

    private static ArticleChunk Chunk(string chunkId, string title) => new("article", chunkId, title, "Test", new Uri("https://example.test/article"), new DateOnly(2026, 8, 20), "en", ["fixture"], "Heading", "Astralis played a synthetic series.", ArticleRagVersions.Chunker);

    private sealed class StubClient(string output) : IStructuredOutputClient
    {
        public StructuredOutputRequest? Request { get; private set; }
        public Task<StructuredOutputResponse> GenerateAsync(StructuredOutputRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new StructuredOutputResponse(output, null));
        }
    }
}

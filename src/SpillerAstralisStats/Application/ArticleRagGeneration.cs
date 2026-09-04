using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpillerAstralisStats.Application;

public sealed record ArticleRagContextChunk(
    int Rank,
    double RetrievalScore,
    string DocumentId,
    string ChunkId,
    string Title,
    Uri SourceUrl,
    DateOnly PublishedAt,
    string Language,
    string Heading,
    string Text,
    string ChunkerVersion,
    string RetrieverVersion);

public sealed record ArticleRagContext(string ContextVersion, string Question, IReadOnlyList<ArticleRagContextChunk> Chunks);

public sealed record ArticleRagAnswer(string State, string? Answer, IReadOnlyList<string> CitationChunkIds);
public sealed record ArticleRagGenerationResult(ArticleRagAnswer Answer, string PromptVersion, string SchemaVersion, ArticleRagContext Context);
public sealed record ArticleRagOperationResult(string State, ArticleRagContext? Context, ArticleRagGenerationResult? Generation);

public interface IArticleRagService
{
    Task<ArticleRagOperationResult> AnswerAsync(
        IReadOnlyList<ArticleChunk> chunks,
        string question,
        int topK,
        CancellationToken cancellationToken);
}

public static class ArticleRagContextBuilder
{
    public static ArticleRagContext Create(string question, IReadOnlyList<ArticleRetrievalResult> results)
    {
        if (string.IsNullOrWhiteSpace(question)) throw new ArgumentException("Question is required.", nameof(question));
        ArgumentNullException.ThrowIfNull(results);
        return new ArticleRagContext(
            ArticleRagVersions.Context,
            question,
            results.Select(result => new ArticleRagContextChunk(
                result.Rank,
                result.Score,
                result.Chunk.DocumentId,
                result.Chunk.ChunkId,
                result.Chunk.Title,
                result.Chunk.SourceUrl,
                result.Chunk.PublishedAt,
                result.Chunk.Language,
                result.Chunk.Heading ?? string.Empty,
                result.Chunk.Text,
                result.Chunk.ChunkerVersion,
                result.RetrieverVersion)).ToArray());
    }
}

internal static class ArticleRagContract
{
    public const string PromptVersion = "article-rag-prompt-v1";
    public const string SchemaVersion = "article-rag-schema-v1";
    public const string SchemaName = "article_rag_answer";
    public const int MaxAnswerLength = 1_200;
    public const int MaxCitations = 5;

    public const string DeveloperInstruction = """
        Write a concise Danish editorial answer using only the supplied article chunks.
        Do not use outside knowledge, calculate match statistics, invent facts, or make predictions.
        If the chunks do not support an answer, return insufficientContext with no answer and no citations.
        For an available answer, cite one or more supplied chunk IDs and omit every unsupported claim.
        """;

    public const string JsonSchema = """
        {
          "type":"object",
          "additionalProperties":false,
          "required":["state","answer","citationChunkIds"],
          "properties":{
            "state":{"type":"string","enum":["available","insufficientContext"]},
            "answer":{"type":["string","null"],"maxLength":1200},
            "citationChunkIds":{"type":"array","items":{"type":"string"},"maxItems":5}
          }
        }
        """;
}

internal static class ArticleRagValidator
{
    public static void Validate(ArticleRagAnswer answer, ArticleRagContext context)
    {
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(context);
        if (answer.State == "insufficientContext")
        {
            if (answer.Answer is not null || answer.CitationChunkIds.Count != 0) throw new ArticleRagValidationException("Insufficient context cannot include an answer or citations.");
            return;
        }

        if (answer.State != "available") throw new ArticleRagValidationException("State must be available or insufficientContext.");
        if (string.IsNullOrWhiteSpace(answer.Answer) || answer.Answer.Length > ArticleRagContract.MaxAnswerLength) throw new ArticleRagValidationException("Available answer text is invalid.");
        if (answer.CitationChunkIds.Count is 0 or > ArticleRagContract.MaxCitations || answer.CitationChunkIds.Any(string.IsNullOrWhiteSpace)) throw new ArticleRagValidationException("Available answers require bounded non-empty citations.");
        if (answer.CitationChunkIds.Distinct(StringComparer.Ordinal).Count() != answer.CitationChunkIds.Count) throw new ArticleRagValidationException("Citations must be unique.");
        var supplied = context.Chunks.Select(chunk => chunk.ChunkId).ToHashSet(StringComparer.Ordinal);
        if (answer.CitationChunkIds.Any(citation => !supplied.Contains(citation))) throw new ArticleRagValidationException("Every citation must refer to a supplied chunk.");
    }
}

internal sealed class ArticleRagGenerator(IStructuredOutputClient client)
{
    private static readonly JsonSerializerOptions InputOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions OutputOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public async Task<ArticleRagGenerationResult> GenerateAsync(ArticleRagContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var response = await client.GenerateAsync(new StructuredOutputRequest(
            ArticleRagContract.DeveloperInstruction,
            JsonSerializer.Serialize(context, InputOptions),
            ArticleRagContract.SchemaName,
            ArticleRagContract.JsonSchema), cancellationToken);
        if (!string.IsNullOrWhiteSpace(response.Refusal)) throw new ArticleRagGenerationException("The model refused to generate an article answer.");
        if (string.IsNullOrWhiteSpace(response.OutputText)) throw new ArticleRagGenerationException("The model returned no article answer.");

        try
        {
            var answer = JsonSerializer.Deserialize<ArticleRagAnswer>(response.OutputText, OutputOptions) ?? throw new ArticleRagGenerationException("The model returned a null article answer.");
            ArticleRagValidator.Validate(answer, context);
            return new ArticleRagGenerationResult(answer, ArticleRagContract.PromptVersion, ArticleRagContract.SchemaVersion, context);
        }
        catch (JsonException exception)
        {
            throw new ArticleRagGenerationException("The model returned malformed or schema-incompatible article JSON.", exception);
        }
        catch (ArticleRagValidationException exception)
        {
            throw new ArticleRagGenerationException("The model returned an invalid article answer.", exception);
        }
    }
}

internal sealed class ArticleRagService(ArticleBm25Retriever retriever, ArticleRagGenerator generator) : IArticleRagService
{
    public async Task<ArticleRagOperationResult> AnswerAsync(IReadOnlyList<ArticleChunk> chunks, string question, int topK, CancellationToken cancellationToken)
    {
        var retrieved = retriever.Search(chunks, question, topK);
        if (retrieved.Count == 0) return new ArticleRagOperationResult("insufficientContext", null, null);
        var context = ArticleRagContextBuilder.Create(question, retrieved);
        var result = await generator.GenerateAsync(context, cancellationToken);
        return new ArticleRagOperationResult(result.Answer.State, context, result);
    }
}

public sealed class ArticleRagGenerationException : Exception
{
    public ArticleRagGenerationException(string message) : base(message) { }
    public ArticleRagGenerationException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class ArticleRagValidationException(string message) : Exception(message);

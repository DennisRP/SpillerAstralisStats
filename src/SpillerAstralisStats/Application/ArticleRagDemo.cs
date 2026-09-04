using System.Text;
using System.Text.Json;

namespace SpillerAstralisStats.Application;

public sealed record ArticleRagDemoSupportedEvidence(
    string Question,
    IReadOnlyList<ArticleRetrievalResult> Retrieved,
    ArticleRagGenerationResult Generation);

public sealed record ArticleRagDemoOutcome(
    string Question,
    IReadOnlyList<ArticleRetrievalResult> Retrieved,
    string State,
    string Detail,
    int GeneratorCallCount);

public sealed record ArticleRagDemoResult(
    IReadOnlyList<ArticleDocument> Documents,
    IReadOnlyList<ArticleChunk> Chunks,
    ArticleRagEvaluationReport EvaluationReport,
    ArticleRagDemoSupportedEvidence SupportedEvidence,
    ArticleRagDemoOutcome RetrievalInsufficientContext,
    ArticleRagDemoOutcome ModelSelectedInsufficientContext,
    ArticleRagDemoOutcome GenerationFailure);

public static class ArticleRagDemoRunner
{
    private const string SupportedQueryId = "synthetic-g2-recap";
    private const string UnsupportedQueryId = "unsupported";
    private const string ModelInsufficientContextQuestion = "Does the synthetic recap provide player statistics?";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<ArticleRagDemoResult> RunAsync(
        string contentDirectory,
        string evaluationPath,
        CancellationToken cancellationToken = default)
    {
        var corpus = ArticleCorpusLoader.Load(contentDirectory);
        if (!corpus.IsValid)
        {
            throw new InvalidOperationException(FormatErrors(corpus.Errors));
        }

        var chunks = corpus.Documents.SelectMany(ArticleChunker.Chunk).ToArray();
        var evaluations = ArticleRagEvaluationLoader.Load(evaluationPath, chunks);
        if (!evaluations.IsValid)
        {
            throw new InvalidOperationException(FormatErrors(evaluations.Errors));
        }

        var retriever = new ArticleBm25Retriever();
        var evaluationReport = ArticleRagEvaluator.Evaluate(chunks, evaluations.Cases, retriever);
        var supportedEvaluation = evaluations.Cases.Single(item => item.QueryId == SupportedQueryId);
        var supportedRetrieved = retriever.Search(chunks, supportedEvaluation.Question, 3);
        var citedChunk = supportedRetrieved.Single(result => supportedEvaluation.RelevantChunkIds.Contains(result.Chunk.ChunkId, StringComparer.Ordinal));
        var supportedContext = ArticleRagContextBuilder.Create(supportedEvaluation.Question, supportedRetrieved);
        var supportedClient = new FixedStructuredOutputClient(SerializeAnswer(
            new ArticleRagAnswer(
                "available",
                "Den syntetiske artikel siger, at G2 vandt serien.",
                [citedChunk.Chunk.ChunkId])));
        var supportedGeneration = await new ArticleRagGenerator(supportedClient)
            .GenerateAsync(supportedContext, cancellationToken);

        var unsupportedEvaluation = evaluations.Cases.Single(item => item.QueryId == UnsupportedQueryId);
        var retrievalClient = new FixedStructuredOutputClient("unreachable");
        var retrievalResult = await new ArticleRagService(retriever, new ArticleRagGenerator(retrievalClient))
            .AnswerAsync(chunks, unsupportedEvaluation.Question, 3, cancellationToken);
        if (retrievalResult.State != "insufficientContext" || retrievalClient.CallCount != 0)
        {
            throw new InvalidOperationException("The retrieval insufficient-context demonstration unexpectedly invoked generation.");
        }

        var modelRetrieved = retriever.Search(chunks, ModelInsufficientContextQuestion, 3);
        if (modelRetrieved.Count == 0)
        {
            throw new InvalidOperationException("The model-selected insufficient-context demonstration requires lexical retrieval results.");
        }

        var modelClient = new FixedStructuredOutputClient(SerializeAnswer(
            new ArticleRagAnswer("insufficientContext", null, [])));
        var modelResult = await new ArticleRagService(retriever, new ArticleRagGenerator(modelClient))
            .AnswerAsync(chunks, ModelInsufficientContextQuestion, 3, cancellationToken);
        if (modelResult.State != "insufficientContext" || modelClient.CallCount != 1)
        {
            throw new InvalidOperationException("The model-selected insufficient-context demonstration did not produce the expected result.");
        }

        var invalidCitationClient = new FixedStructuredOutputClient(SerializeAnswer(
            new ArticleRagAnswer("available", "Dette svar må ikke accepteres.", ["unretrieved#chunk-01"])));
        string generationFailure;
        try
        {
            await new ArticleRagGenerator(invalidCitationClient).GenerateAsync(supportedContext, cancellationToken);
            throw new InvalidOperationException("The invalid-citation demonstration unexpectedly succeeded.");
        }
        catch (ArticleRagGenerationException exception)
        {
            generationFailure = exception.Message;
        }

        return new ArticleRagDemoResult(
            corpus.Documents,
            chunks,
            evaluationReport,
            new ArticleRagDemoSupportedEvidence(supportedEvaluation.Question, supportedRetrieved, supportedGeneration),
            new ArticleRagDemoOutcome(
                unsupportedEvaluation.Question,
                [],
                "retrievalInsufficientContext",
                "No positive-scoring lexical evidence was retrieved; generation was not invoked.",
                retrievalClient.CallCount),
            new ArticleRagDemoOutcome(
                ModelInsufficientContextQuestion,
                modelRetrieved,
                "modelSelectedInsufficientContext",
                "Retrieved passages do not support the requested claim, so the deterministic fake model declined to answer.",
                modelClient.CallCount),
            new ArticleRagDemoOutcome(
                supportedEvaluation.Question,
                supportedRetrieved,
                "generationValidationFailure",
                generationFailure,
                invalidCitationClient.CallCount));
    }

    private static string SerializeAnswer(ArticleRagAnswer answer) => JsonSerializer.Serialize(answer, JsonOptions);

    private static string FormatErrors(IEnumerable<CorpusValidationError> errors) => string.Join(
        Environment.NewLine,
        errors.Select(error => $"{error.SourcePath} [{error.Field}]: {error.Message}"));

    private sealed class FixedStructuredOutputClient(string output) : IStructuredOutputClient
    {
        public int CallCount { get; private set; }

        public Task<StructuredOutputResponse> GenerateAsync(StructuredOutputRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new StructuredOutputResponse(output, null));
        }
    }
}

public static class ArticleRagDemoTraceFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static string Format(ArticleRagDemoResult demo)
    {
        ArgumentNullException.ThrowIfNull(demo);
        var builder = new StringBuilder();
        builder.AppendLine("=== 1. SYNTHETIC CORPUS ===");
        builder.AppendLine($"Documents: {demo.Documents.Count}; chunks: {demo.Chunks.Count}");
        foreach (var document in demo.Documents)
        {
            builder.AppendLine($"Document: {document.DocumentId} | {document.Title}");
        }
        foreach (var chunk in demo.Chunks)
        {
            builder.AppendLine($"Chunk: {chunk.ChunkId} | heading={chunk.Heading ?? "<none>"}");
        }

        builder.AppendLine();
        builder.AppendLine("=== 2. RETRIEVAL EVALUATION ===");
        foreach (var result in demo.EvaluationReport.Results)
        {
            builder.AppendLine(ArticleRetrievalTraceFormatter.Format(result.EvaluationCase.Question, result.Results));
            builder.AppendLine(result.EvaluationCase.ExpectsSupport
                ? $"Recall@3: {result.RecallAt3:F3}; reciprocal rank: {result.ReciprocalRank:F3}"
                : $"Unsupported-query rejection: {result.UnsupportedQueryRejected}");
        }
        builder.AppendLine($"Average Recall@3: {demo.EvaluationReport.AverageRecallAt3:F3}");
        builder.AppendLine($"Mean reciprocal rank: {demo.EvaluationReport.MeanReciprocalRank:F3}");

        builder.AppendLine();
        builder.AppendLine("=== 3. SUPPORTED ANSWER (DETERMINISTIC FAKE MODEL) ===");
        builder.AppendLine(ArticleRetrievalTraceFormatter.Format(demo.SupportedEvidence.Question, demo.SupportedEvidence.Retrieved));
        builder.AppendLine("Exact model context:");
        builder.AppendLine(JsonSerializer.Serialize(demo.SupportedEvidence.Generation.Context, JsonOptions));
        builder.AppendLine($"Answer: {demo.SupportedEvidence.Generation.Answer.Answer}");
        builder.AppendLine($"Citations: {string.Join(", ", demo.SupportedEvidence.Generation.Answer.CitationChunkIds)}");
        builder.AppendLine($"Citation membership: validated against supplied context.");
        builder.AppendLine($"Versions: prompt={demo.SupportedEvidence.Generation.PromptVersion}; schema={demo.SupportedEvidence.Generation.SchemaVersion}; chunker={ArticleRagVersions.Chunker}; retriever={ArticleRagVersions.Retriever}; tokenizer={ArticleRagVersions.Tokenizer}");

        AppendOutcome(builder, "=== 4. RETRIEVAL-LEVEL INSUFFICIENT CONTEXT ===", demo.RetrievalInsufficientContext);
        AppendOutcome(builder, "=== 5. MODEL-SELECTED INSUFFICIENT CONTEXT (DETERMINISTIC FAKE MODEL) ===", demo.ModelSelectedInsufficientContext);
        AppendOutcome(builder, "=== 6. GENERATION VALIDATION FAILURE (DETERMINISTIC FAKE MODEL) ===", demo.GenerationFailure);
        return builder.ToString();
    }

    private static void AppendOutcome(StringBuilder builder, string heading, ArticleRagDemoOutcome outcome)
    {
        builder.AppendLine();
        builder.AppendLine(heading);
        if (outcome.Retrieved.Count > 0)
        {
            builder.AppendLine(ArticleRetrievalTraceFormatter.Format(outcome.Question, outcome.Retrieved));
        }
        else
        {
            builder.AppendLine($"Question: {outcome.Question}");
        }
        builder.AppendLine($"State: {outcome.State}");
        builder.AppendLine($"Detail: {outcome.Detail}");
        builder.AppendLine($"Fake model calls: {outcome.GeneratorCallCount}");
    }
}

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Configuration;
using SpillerAstralisStats.Infrastructure.Foundry;

if (args.Length >= 2 && args[0] == "corpus" && args[1] == "build")
{
    return BuildCorpus(args);
}

if (args.Length >= 2 && args[0] == "article" && args[1] == "answer")
{
    return await AnswerArticleAsync(args);
}

Console.Error.WriteLine("Usage:");
Console.Error.WriteLine("  corpus build [--content <directory>] [--evaluations <file>] [--skip-evaluations]");
Console.Error.WriteLine("  article answer --question <question> [--content <directory>] [--top-k <count>] [--use-foundry]");
return 2;

static int BuildCorpus(IReadOnlyList<string> arguments)
{
    var contentDirectory = GetOption(arguments, "--content") ?? DefaultContentDirectory();
    var evaluationPath = GetOption(arguments, "--evaluations") ?? Path.Combine(Directory.GetCurrentDirectory(), "content", "rag", "evaluations.local.json");
    var corpus = ArticleCorpusLoader.Load(contentDirectory);
    if (!corpus.IsValid)
    {
        PrintErrors(corpus.Errors);
        return 1;
    }

    var chunks = corpus.Documents.SelectMany(ArticleChunker.Chunk).ToArray();
    Console.WriteLine($"Validated {corpus.Documents.Count} document(s) and generated {chunks.Length} chunk(s).");
    foreach (var chunk in chunks)
    {
        Console.WriteLine($"{chunk.ChunkId} ({ArticleChunker.CountWords(chunk.Text)} words) heading={chunk.Heading ?? "<none>"}");
    }

    if (arguments.Contains("--skip-evaluations", StringComparer.Ordinal))
    {
        return 0;
    }

    var evaluations = ArticleRagEvaluationLoader.Load(evaluationPath, chunks);
    if (!evaluations.IsValid)
    {
        PrintErrors(evaluations.Errors);
        return 1;
    }

    var report = ArticleRagEvaluator.Evaluate(chunks, evaluations.Cases, new ArticleBm25Retriever());
    foreach (var result in report.Results)
    {
        Console.WriteLine();
        Console.WriteLine(ArticleRetrievalTraceFormatter.Format(result.EvaluationCase.Question, result.Results));
        if (result.EvaluationCase.ExpectsSupport)
        {
            Console.WriteLine($"Recall@3: {result.RecallAt3:F3}; reciprocal rank: {result.ReciprocalRank:F3}");
        }
        else
        {
            Console.WriteLine($"Unsupported-query rejection: {result.UnsupportedQueryRejected}");
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Average Recall@3: {report.AverageRecallAt3?.ToString("F3") ?? "n/a"}");
    Console.WriteLine($"Mean reciprocal rank: {report.MeanReciprocalRank?.ToString("F3") ?? "n/a"}");
    return 0;
}

static async Task<int> AnswerArticleAsync(IReadOnlyList<string> arguments)
{
    var question = GetOption(arguments, "--question");
    if (string.IsNullOrWhiteSpace(question))
    {
        Console.Error.WriteLine("The --question option is required.");
        return 2;
    }

    if (!TryGetTopK(GetOption(arguments, "--top-k"), out var topK))
    {
        Console.Error.WriteLine("The --top-k option must be a positive integer.");
        return 2;
    }

    var corpus = ArticleCorpusLoader.Load(GetOption(arguments, "--content") ?? DefaultContentDirectory());
    if (!corpus.IsValid)
    {
        PrintErrors(corpus.Errors);
        return 1;
    }

    var chunks = corpus.Documents.SelectMany(ArticleChunker.Chunk).ToArray();
    var retrieved = new ArticleBm25Retriever().Search(chunks, question, topK);
    Console.WriteLine(ArticleRetrievalTraceFormatter.Format(question, retrieved));
    if (retrieved.Count == 0)
    {
        Console.WriteLine("Result: insufficientContext. No positive-scoring evidence was found, so Foundry was not called.");
        return 0;
    }

    var context = ArticleRagContextBuilder.Create(question, retrieved);
    Console.WriteLine("Exact model context:");
    Console.WriteLine(JsonSerializer.Serialize(context, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));

    if (!arguments.Contains("--use-foundry", StringComparer.Ordinal))
    {
        Console.WriteLine("Preview complete. Add --use-foundry to make the explicit opt-in live model call.");
        return 0;
    }

    try
    {
        var services = new ServiceCollection();
        services.AddArticleRag(LoadFoundryOptions());
        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IArticleRagService>();
        var result = await service.AnswerAsync(chunks, question, topK, CancellationToken.None);

        if (result.State == "insufficientContext")
        {
            Console.WriteLine("Model result: insufficientContext.");
            return 0;
        }

        var generation = result.Generation!;
        Console.WriteLine("Validated model result:");
        Console.WriteLine(generation.Answer.Answer);
        Console.WriteLine($"Citations: {string.Join(", ", generation.Answer.CitationChunkIds)}");
        Console.WriteLine($"Prompt: {generation.PromptVersion}; schema: {generation.SchemaVersion}");
        Console.WriteLine("Citation membership: validated against the supplied context.");
        return 0;
    }
    catch (Exception exception) when (exception is ArticleRagGenerationException or InvalidOperationException)
    {
        Console.Error.WriteLine($"Generation failed: {exception.Message}");
        return 1;
    }
}

static string DefaultContentDirectory() => Path.Combine(Directory.GetCurrentDirectory(), "content", "rag", "articles");

static string? GetOption(IReadOnlyList<string> arguments, string name)
{
    var index = arguments.ToList().IndexOf(name);
    return index >= 0 && index + 1 < arguments.Count ? arguments[index + 1] : null;
}

static bool TryGetTopK(string? value, out int topK)
{
    if (value is null)
    {
        topK = 3;
        return true;
    }

    return int.TryParse(value, out topK) && topK > 0;
}

static FoundryOptions LoadFoundryOptions()
{
    var localSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "SpillerAstralisStats", "local.settings.json");
    var localValues = ReadLocalValues(localSettingsPath);
    return new FoundryOptions
    {
        Endpoint = GetValue("Foundry__Endpoint") ?? string.Empty,
        DeploymentName = GetValue("Foundry__DeploymentName") ?? string.Empty
    };

    string? GetValue(string name)
    {
        var environmentValue = Environment.GetEnvironmentVariable(name);
        return !string.IsNullOrWhiteSpace(environmentValue) ? environmentValue : localValues.GetValueOrDefault(name);
    }
}

static IReadOnlyDictionary<string, string?> ReadLocalValues(string localSettingsPath)
{
    if (!File.Exists(localSettingsPath))
    {
        return new Dictionary<string, string?>();
    }

    try
    {
        using var document = JsonDocument.Parse(File.ReadAllText(localSettingsPath));
        if (!document.RootElement.TryGetProperty("Values", out var values) || values.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string?>();
        }

        return values.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null,
            StringComparer.Ordinal);
    }
    catch (JsonException exception)
    {
        throw new InvalidOperationException($"Foundry settings file '{localSettingsPath}' contains invalid JSON.", exception);
    }
}

static void PrintErrors(IEnumerable<CorpusValidationError> errors)
{
    foreach (var error in errors)
    {
        Console.Error.WriteLine($"{error.SourcePath} [{error.Field}]: {error.Message}");
    }
}

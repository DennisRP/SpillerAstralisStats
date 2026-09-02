using SpillerAstralisStats.Application;

if (args.Length == 0 || args[0] != "corpus" || args.ElementAtOrDefault(1) != "build")
{
    Console.Error.WriteLine("Usage: corpus build [--content <directory>] [--evaluations <file>] [--skip-evaluations]");
    return 2;
}

var contentDirectory = GetOption(args, "--content") ?? Path.Combine(Directory.GetCurrentDirectory(), "content", "rag", "articles");
var evaluationPath = GetOption(args, "--evaluations") ?? Path.Combine(Directory.GetCurrentDirectory(), "content", "rag", "evaluations.local.json");
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

if (args.Contains("--skip-evaluations", StringComparer.Ordinal))
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

static string? GetOption(IReadOnlyList<string> arguments, string name)
{
    var index = arguments.ToList().IndexOf(name);
    return index >= 0 && index + 1 < arguments.Count ? arguments[index + 1] : null;
}

static void PrintErrors(IEnumerable<CorpusValidationError> errors)
{
    foreach (var error in errors)
    {
        Console.Error.WriteLine($"{error.SourcePath} [{error.Field}]: {error.Message}");
    }
}

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SpillerAstralisStats.Application;

public static class ArticleRagVersions
{
    public const string Chunker = "article-chunker-v1";
    public const string Tokenizer = "article-tokenizer-v1";
    public const string Retriever = "article-bm25-v1";
    public const string Context = "article-rag-context-v1";
}

public sealed record ArticleDocument(
    string DocumentId,
    string Title,
    string Source,
    Uri SourceUrl,
    DateOnly PublishedAt,
    string Language,
    IReadOnlyList<string> Tags,
    string MarkdownBody,
    string SourcePath);

public sealed record ArticleChunk(
    string DocumentId,
    string ChunkId,
    string Title,
    string Source,
    Uri SourceUrl,
    DateOnly PublishedAt,
    string Language,
    IReadOnlyList<string> Tags,
    string? Heading,
    string Text,
    string ChunkerVersion);

public sealed record CorpusValidationError(string SourcePath, string Field, string Message);

public sealed record ArticleCorpusLoadResult(
    IReadOnlyList<ArticleDocument> Documents,
    IReadOnlyList<CorpusValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public static class ArticleCorpusLoader
{
    private static readonly HashSet<string> AllowedFields = new(StringComparer.Ordinal)
    {
        "documentId", "title", "source", "sourceUrl", "publishedAt", "language", "tags"
    };

    private static readonly Regex DocumentIdPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant);
    private static readonly Regex TagPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant);

    public static ArticleCorpusLoadResult Load(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        if (!Directory.Exists(directoryPath))
        {
            return new ArticleCorpusLoadResult([], [new CorpusValidationError(directoryPath, "directory", "The corpus directory does not exist.")]);
        }

        var documents = new List<ArticleDocument>();
        var errors = new List<CorpusValidationError>();
        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.md", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.Ordinal))
        {
            ParseFile(file, documents, errors);
        }

        foreach (var group in documents.GroupBy(document => document.DocumentId, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            foreach (var duplicate in group)
            {
                errors.Add(new CorpusValidationError(duplicate.SourcePath, "documentId", $"Duplicate document ID '{group.Key}'."));
            }
        }

        return errors.Count == 0
            ? new ArticleCorpusLoadResult(documents, [])
            : new ArticleCorpusLoadResult([], errors.OrderBy(error => error.SourcePath, StringComparer.Ordinal).ThenBy(error => error.Field, StringComparer.Ordinal).ToArray());
    }

    private static void ParseFile(string path, ICollection<ArticleDocument> documents, ICollection<CorpusValidationError> errors)
    {
        string content;
        try
        {
            content = File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n", StringComparison.Ordinal);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            errors.Add(new CorpusValidationError(path, "file", exception.Message));
            return;
        }

        if (!content.StartsWith("---\n", StringComparison.Ordinal))
        {
            errors.Add(new CorpusValidationError(path, "frontMatter", "Front matter must start with a delimiter on the first line."));
            return;
        }

        var closingDelimiter = content.IndexOf("\n---\n", StringComparison.Ordinal);
        if (closingDelimiter < 0)
        {
            errors.Add(new CorpusValidationError(path, "frontMatter", "Front matter must end with a delimiter."));
            return;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var frontMatter = content[4..closingDelimiter];
        foreach (var line in frontMatter.Split('\n'))
        {
            var separator = line.IndexOf(':');
            if (separator <= 0)
            {
                errors.Add(new CorpusValidationError(path, "frontMatter", $"Invalid front-matter line '{line}'."));
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (!AllowedFields.Contains(key))
            {
                errors.Add(new CorpusValidationError(path, key, "Unknown metadata field."));
            }
            else if (!values.TryAdd(key, value))
            {
                errors.Add(new CorpusValidationError(path, key, "Metadata field occurs more than once."));
            }
        }

        var body = content[(closingDelimiter + 5)..].Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            errors.Add(new CorpusValidationError(path, "body", "Article body is required."));
        }

        foreach (var field in AllowedFields)
        {
            if (!values.ContainsKey(field) || string.IsNullOrWhiteSpace(values.GetValueOrDefault(field)))
            {
                errors.Add(new CorpusValidationError(path, field, "Metadata field is required."));
            }
        }

        if (errors.Any(error => error.SourcePath == path))
        {
            return;
        }

        var documentId = values["documentId"];
        if (!DocumentIdPattern.IsMatch(documentId))
        {
            errors.Add(new CorpusValidationError(path, "documentId", "Document ID must be lowercase kebab-case."));
        }

        if (!Uri.TryCreate(values["sourceUrl"], UriKind.Absolute, out var sourceUrl) || (sourceUrl.Scheme != Uri.UriSchemeHttp && sourceUrl.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add(new CorpusValidationError(path, "sourceUrl", "Source URL must be an absolute HTTP or HTTPS URL."));
        }

        if (!DateOnly.TryParseExact(values["publishedAt"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var publishedAt))
        {
            errors.Add(new CorpusValidationError(path, "publishedAt", "Publication date must use yyyy-MM-dd."));
        }

        var language = values["language"];
        if (language is not ("en" or "da"))
        {
            errors.Add(new CorpusValidationError(path, "language", "Language must be 'en' or 'da'."));
        }

        var tags = ParseTags(values["tags"], path, errors);
        if (errors.Any(error => error.SourcePath == path))
        {
            return;
        }

        documents.Add(new ArticleDocument(documentId, values["title"], values["source"], sourceUrl!, publishedAt, language, tags, body, path));
    }

    private static IReadOnlyList<string> ParseTags(string rawTags, string path, ICollection<CorpusValidationError> errors)
    {
        if (!rawTags.StartsWith("[", StringComparison.Ordinal) || !rawTags.EndsWith("]", StringComparison.Ordinal))
        {
            errors.Add(new CorpusValidationError(path, "tags", "Tags must be an inline list."));
            return [];
        }

        var tags = rawTags[1..^1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tags.Length == 0 || tags.Any(tag => !TagPattern.IsMatch(tag)) || tags.Distinct(StringComparer.Ordinal).Count() != tags.Length)
        {
            errors.Add(new CorpusValidationError(path, "tags", "Tags must be unique lowercase kebab-case values."));
            return [];
        }

        return tags;
    }
}

public static class ArticleChunker
{
    public const int TargetMinimumWords = 150;
    public const int TargetMaximumWords = 300;
    private static readonly Regex WordPattern = new("[\\p{L}\\p{Nd}]+", RegexOptions.CultureInvariant);

    public static IReadOnlyList<ArticleChunk> Chunk(ArticleDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var sections = ParseSections(document.MarkdownBody);
        var chunks = new List<ArticleChunk>();
        var paragraphBuffer = new List<string>();
        var currentHeading = (string?)null;
        var wordCount = 0;

        void Flush(bool mergeShortFinalFragment = false)
        {
            if (paragraphBuffer.Count == 0)
            {
                return;
            }

            var text = string.Join("\n\n", paragraphBuffer);
            if (mergeShortFinalFragment && wordCount < TargetMinimumWords && chunks.Count > 0 && string.Equals(chunks[^1].Heading, currentHeading, StringComparison.Ordinal))
            {
                chunks[^1] = chunks[^1] with { Text = $"{chunks[^1].Text}\n\n{text}" };
                paragraphBuffer.Clear();
                wordCount = 0;
                return;
            }

            var ordinal = chunks.Count + 1;
            chunks.Add(new ArticleChunk(
                document.DocumentId,
                $"{document.DocumentId}#chunk-{ordinal:D2}",
                document.Title,
                document.Source,
                document.SourceUrl,
                document.PublishedAt,
                document.Language,
                document.Tags,
                currentHeading,
                text,
                ArticleRagVersions.Chunker));
            paragraphBuffer.Clear();
            wordCount = 0;
        }

        foreach (var section in sections)
        {
            if (section.IsHeading)
            {
                Flush();
                currentHeading = section.Text;
                continue;
            }

            var paragraphWords = CountWords(section.Text);
            if (paragraphBuffer.Count > 0 && wordCount >= TargetMinimumWords && wordCount + paragraphWords > TargetMaximumWords)
            {
                Flush();
            }

            paragraphBuffer.Add(section.Text);
            wordCount += paragraphWords;
        }

        Flush(mergeShortFinalFragment: true);
        return chunks;
    }

    public static int CountWords(string text) => WordPattern.Matches(text).Count;

    private static IReadOnlyList<MarkdownSection> ParseSections(string body)
    {
        var sections = new List<MarkdownSection>();
        var paragraphLines = new List<string>();

        void FlushParagraph()
        {
            if (paragraphLines.Count > 0)
            {
                sections.Add(new MarkdownSection(false, string.Join("\n", paragraphLines).Trim()));
                paragraphLines.Clear();
            }
        }

        foreach (var rawLine in body.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (line.StartsWith('#'))
            {
                var heading = line.TrimStart('#').Trim();
                if (heading.Length > 0)
                {
                    FlushParagraph();
                    sections.Add(new MarkdownSection(true, heading));
                    continue;
                }
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph();
            }
            else
            {
                paragraphLines.Add(line);
            }
        }

        FlushParagraph();
        return sections;
    }

    private sealed record MarkdownSection(bool IsHeading, string Text);
}

public static class ArticleTokenizer
{
    private static readonly Regex TokenPattern = new("[\\p{L}\\p{Nd}]+", RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> Tokenize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return TokenPattern.Matches(text).Select(match => match.Value.ToLowerInvariant()).ToArray();
    }
}

public sealed record ArticleRetrievalResult(ArticleChunk Chunk, int Rank, double Score, string RetrieverVersion, string TokenizerVersion);

public sealed class ArticleBm25Retriever
{
    private const double K1 = 1.2;
    private const double B = 0.75;

    public IReadOnlyList<ArticleRetrievalResult> Search(IReadOnlyList<ArticleChunk> chunks, string query, int topK)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query is required.", nameof(query));
        if (topK <= 0) throw new ArgumentOutOfRangeException(nameof(topK), "Top-K must be positive.");

        var queryTerms = ArticleTokenizer.Tokenize(query).Distinct(StringComparer.Ordinal).ToArray();
        if (queryTerms.Length == 0 || chunks.Count == 0) return [];

        var documents = chunks.Select(chunk => new IndexedChunk(chunk, ArticleTokenizer.Tokenize($"{chunk.Title} {chunk.Heading} {chunk.Text}"))).ToArray();
        var averageLength = documents.Average(document => document.Terms.Count);
        var documentFrequency = queryTerms.ToDictionary(
            term => term,
            term => documents.Count(document => document.Terms.Contains(term, StringComparer.Ordinal)),
            StringComparer.Ordinal);

        return documents
            .Select(document => new { document.Chunk, Score = Score(document.Terms, queryTerms, documentFrequency, documents.Length, averageLength) })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Chunk.ChunkId, StringComparer.Ordinal)
            .Take(topK)
            .Select((result, index) => new ArticleRetrievalResult(result.Chunk, index + 1, result.Score, ArticleRagVersions.Retriever, ArticleRagVersions.Tokenizer))
            .ToArray();
    }

    private static double Score(IReadOnlyList<string> documentTerms, IReadOnlyList<string> queryTerms, IReadOnlyDictionary<string, int> documentFrequency, int documentCount, double averageLength)
    {
        var score = 0d;
        foreach (var term in queryTerms)
        {
            var frequency = documentTerms.Count(candidate => string.Equals(candidate, term, StringComparison.Ordinal));
            if (frequency == 0) continue;
            var idf = Math.Log(1 + ((documentCount - documentFrequency[term] + 0.5) / (documentFrequency[term] + 0.5)));
            var normalizedLength = 1 - B + (B * documentTerms.Count / averageLength);
            score += idf * ((frequency * (K1 + 1)) / (frequency + (K1 * normalizedLength)));
        }

        return score;
    }

    private sealed record IndexedChunk(ArticleChunk Chunk, IReadOnlyList<string> Terms);
}

public static class ArticleRetrievalTraceFormatter
{
    public static string Format(string query, IReadOnlyList<ArticleRetrievalResult> results)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(results);
        var builder = new StringBuilder();
        builder.AppendLine($"Question: {query}");
        builder.AppendLine($"Retriever: {ArticleRagVersions.Retriever}; tokenizer: {ArticleRagVersions.Tokenizer}");
        foreach (var result in results)
        {
            builder.AppendLine($"#{result.Rank} {result.Chunk.ChunkId} score={result.Score:F6}");
            builder.AppendLine($"Source: {result.Chunk.Title} | {result.Chunk.SourceUrl}");
            builder.AppendLine(result.Chunk.Text);
        }

        return builder.ToString();
    }
}

public sealed record ArticleRagEvaluationCase(
    string QueryId,
    string Question,
    bool ExpectsSupport,
    IReadOnlyList<string> RelevantDocumentIds,
    IReadOnlyList<string> RelevantChunkIds);

public sealed record ArticleRagEvaluationLoadResult(IReadOnlyList<ArticleRagEvaluationCase> Cases, IReadOnlyList<CorpusValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public static class ArticleRagEvaluationLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    public static ArticleRagEvaluationLoadResult Load(string path, IReadOnlyList<ArticleChunk> chunks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(chunks);
        if (!File.Exists(path)) return new ArticleRagEvaluationLoadResult([], [new CorpusValidationError(path, "file", "Evaluation file does not exist.")]);

        EvaluationDto[]? values;
        try { values = JsonSerializer.Deserialize<EvaluationDto[]>(File.ReadAllText(path, Encoding.UTF8), Options); }
        catch (JsonException exception) { return new ArticleRagEvaluationLoadResult([], [new CorpusValidationError(path, "json", exception.Message)]); }

        if (values is null) return new ArticleRagEvaluationLoadResult([], [new CorpusValidationError(path, "json", "Evaluation collection cannot be null.")]);
        var errors = new List<CorpusValidationError>();
        var cases = new List<ArticleRagEvaluationCase>();
        var documentIds = chunks.Select(chunk => chunk.DocumentId).ToHashSet(StringComparer.Ordinal);
        var chunkIds = chunks.Select(chunk => chunk.ChunkId).ToHashSet(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var queryId = value.QueryId?.Trim() ?? string.Empty;
            var question = value.Question?.Trim() ?? string.Empty;
            var relevantDocuments = value.RelevantDocumentIds ?? [];
            var relevantChunks = value.RelevantChunkIds ?? [];
            if (!DocumentIdPattern.IsMatch(queryId)) errors.Add(new CorpusValidationError(path, "queryId", "Query ID must be lowercase kebab-case."));
            if (question.Length == 0) errors.Add(new CorpusValidationError(path, "question", "Question is required."));
            if (value.ExpectsSupport != (relevantDocuments.Count + relevantChunks.Count > 0)) errors.Add(new CorpusValidationError(path, "expectsSupport", "Support expectation must match presence of relevance judgments."));
            if (relevantDocuments.Any(id => !documentIds.Contains(id))) errors.Add(new CorpusValidationError(path, "relevantDocumentIds", "A relevant document ID is absent from the corpus."));
            if (relevantChunks.Any(id => !chunkIds.Contains(id))) errors.Add(new CorpusValidationError(path, "relevantChunkIds", "A relevant chunk ID is absent from the corpus."));
            cases.Add(new ArticleRagEvaluationCase(queryId, question, value.ExpectsSupport, relevantDocuments, relevantChunks));
        }

        if (cases.GroupBy(item => item.QueryId, StringComparer.Ordinal).Any(group => group.Count() > 1)) errors.Add(new CorpusValidationError(path, "queryId", "Query IDs must be unique."));
        return errors.Count == 0 ? new ArticleRagEvaluationLoadResult(cases, []) : new ArticleRagEvaluationLoadResult([], errors);
    }

    private static readonly Regex DocumentIdPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant);
    private sealed record EvaluationDto(string? QueryId, string? Question, bool ExpectsSupport, IReadOnlyList<string>? RelevantDocumentIds, IReadOnlyList<string>? RelevantChunkIds);
}

public sealed record ArticleRagEvaluationResult(ArticleRagEvaluationCase EvaluationCase, IReadOnlyList<ArticleRetrievalResult> Results, double? RecallAt3, double? ReciprocalRank, bool? UnsupportedQueryRejected);
public sealed record ArticleRagEvaluationReport(IReadOnlyList<ArticleRagEvaluationResult> Results, double? AverageRecallAt3, double? MeanReciprocalRank);

public static class ArticleRagEvaluator
{
    public static ArticleRagEvaluationReport Evaluate(IReadOnlyList<ArticleChunk> chunks, IReadOnlyList<ArticleRagEvaluationCase> cases, ArticleBm25Retriever retriever)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(retriever);
        var results = new List<ArticleRagEvaluationResult>();
        foreach (var evaluationCase in cases)
        {
            var retrieved = retriever.Search(chunks, evaluationCase.Question, 3);
            if (!evaluationCase.ExpectsSupport)
            {
                results.Add(new ArticleRagEvaluationResult(evaluationCase, retrieved, null, null, retrieved.Count == 0));
                continue;
            }

            var relevant = evaluationCase.RelevantDocumentIds.Concat(evaluationCase.RelevantChunkIds).ToHashSet(StringComparer.Ordinal);
            var matchedJudgments = evaluationCase.RelevantDocumentIds.Count(documentId => retrieved.Any(result => result.Chunk.DocumentId == documentId))
                + evaluationCase.RelevantChunkIds.Count(chunkId => retrieved.Any(result => result.Chunk.ChunkId == chunkId));
            var recall = (double)matchedJudgments / relevant.Count;
            var first = retrieved.FirstOrDefault(result => relevant.Contains(result.Chunk.DocumentId) || relevant.Contains(result.Chunk.ChunkId));
            results.Add(new ArticleRagEvaluationResult(evaluationCase, retrieved, recall, first is null ? 0 : 1d / first.Rank, null));
        }

        var supported = results.Where(result => result.EvaluationCase.ExpectsSupport).ToArray();
        return new ArticleRagEvaluationReport(results, supported.Length == 0 ? null : supported.Average(result => result.RecallAt3!.Value), supported.Length == 0 ? null : supported.Average(result => result.ReciprocalRank!.Value));
    }
}

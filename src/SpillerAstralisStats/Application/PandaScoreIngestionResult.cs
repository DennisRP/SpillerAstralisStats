using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Application;

public enum IngestionFailureCategory
{
    None,
    Configuration,
    Transport,
    ProviderResponse,
    Payload
}

public sealed record PandaScoreIngestionResult(
    bool Succeeded,
    IReadOnlyList<MatchRecord> Matches,
    int LookbackMonths,
    IngestionFailureCategory FailureCategory = IngestionFailureCategory.None,
    string? FailureMessage = null)
{
    public static PandaScoreIngestionResult Success(IReadOnlyList<MatchRecord> matches, int lookbackMonths) =>
        new(true, matches, lookbackMonths);

    public static PandaScoreIngestionResult Failure(IngestionFailureCategory category, string message, int lookbackMonths) =>
        new(false, [], lookbackMonths, category, message);
}

public interface IPandaScoreMatchIngestionService
{
    Task<PandaScoreIngestionResult> IngestAsync(CancellationToken cancellationToken);
}

namespace SpillerAstralisStats.Configuration;

public sealed class PandaScoreOptions
{
    public const string SectionName = "PandaScore";

    public string BaseUrl { get; set; } = "https://api.pandascore.co/";

    public string? ApiKey { get; set; }

    public int HistoryLookbackMonths { get; set; } = 12;

    public bool TryValidate(out string failure)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            failure = "PandaScore API key is not configured.";
            return false;
        }

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
        {
            failure = "PandaScore base URL is invalid.";
            return false;
        }

        if (HistoryLookbackMonths is < 6 or > 12)
        {
            failure = "PandaScore history lookback must be between 6 and 12 months.";
            return false;
        }

        failure = string.Empty;
        return true;
    }
}

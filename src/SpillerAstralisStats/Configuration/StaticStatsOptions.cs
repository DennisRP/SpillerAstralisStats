namespace SpillerAstralisStats.Configuration;

public sealed class StaticStatsOptions
{
    public const string SectionName = "StaticStats";

    public string OutputPath { get; set; } = "dist/stats/data";

    public int RecentMatchCount { get; set; } = 5;

    public bool TryValidate(out string failure)
    {
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            failure = "Static stats output path is not configured.";
            return false;
        }

        if (RecentMatchCount <= 0)
        {
            failure = "Static stats recent match count must be positive.";
            return false;
        }

        failure = string.Empty;
        return true;
    }
}

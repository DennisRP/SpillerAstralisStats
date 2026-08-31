namespace SpillerAstralisStats.Configuration;

public sealed class FoundryOptions
{
    public const string SectionName = "Foundry";

    public string Endpoint { get; set; } = string.Empty;

    public string DeploymentName { get; set; } = string.Empty;

    public bool TryValidate(out string failure)
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            failure = "Foundry endpoint is required.";
            return false;
        }

        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
        {
            failure = "Foundry endpoint must be an absolute HTTPS URI.";
            return false;
        }

        if (!endpoint.AbsolutePath.EndsWith("/openai/v1/", StringComparison.OrdinalIgnoreCase))
        {
            failure = "Foundry endpoint must end with /openai/v1/.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(DeploymentName))
        {
            failure = "Foundry deployment name is required.";
            return false;
        }

        failure = string.Empty;
        return true;
    }
}

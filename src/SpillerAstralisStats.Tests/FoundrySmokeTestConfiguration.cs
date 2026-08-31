using System.Text.Json;

namespace SpillerAstralisStats.Tests;

internal sealed record FoundrySmokeTestConfiguration(
    string? RunSmokeTest,
    string? Endpoint,
    string? DeploymentName)
{
    public bool IsEnabled => string.Equals(
        RunSmokeTest,
        "true",
        StringComparison.OrdinalIgnoreCase);

    public static FoundrySmokeTestConfiguration Load(string localSettingsPath) =>
        Load(localSettingsPath, Environment.GetEnvironmentVariable);

    internal static FoundrySmokeTestConfiguration Load(
        string localSettingsPath,
        Func<string, string?> getEnvironmentVariable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localSettingsPath);
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        var localValues = ReadLocalValues(localSettingsPath);

        return new FoundrySmokeTestConfiguration(
            GetValue("RUN_FOUNDRY_SMOKE_TEST"),
            GetValue("Foundry__Endpoint"),
            GetValue("Foundry__DeploymentName"));

        string? GetValue(string name)
        {
            var environmentValue = getEnvironmentVariable(name);
            return !string.IsNullOrWhiteSpace(environmentValue)
                ? environmentValue
                : localValues.GetValueOrDefault(name);
        }
    }

    private static IReadOnlyDictionary<string, string?> ReadLocalValues(string localSettingsPath)
    {
        if (!File.Exists(localSettingsPath))
        {
            return new Dictionary<string, string?>();
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(localSettingsPath));
            if (!document.RootElement.TryGetProperty("Values", out var values) ||
                values.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string?>();
            }

            return values.EnumerateObject().ToDictionary(
                property => property.Name,
                property => property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : null,
                StringComparer.Ordinal);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Foundry smoke test settings file '{localSettingsPath}' contains invalid JSON.",
                exception);
        }
    }
}

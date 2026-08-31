using SpillerAstralisStats.Configuration;

namespace SpillerAstralisStats.Tests;

public sealed class FoundryOptionsTests
{
    [Fact]
    public void Accepts_valid_openai_v1_configuration()
    {
        var options = new FoundryOptions
        {
            Endpoint = "https://example-resource.openai.azure.com/openai/v1/",
            DeploymentName = "briefing-model"
        };

        Assert.True(options.TryValidate(out var failure));
        Assert.Empty(failure);
    }

    [Theory]
    [InlineData(null, "briefing-model", "endpoint is required")]
    [InlineData("", "briefing-model", "endpoint is required")]
    [InlineData("not-a-uri", "briefing-model", "absolute HTTPS URI")]
    [InlineData("http://example.openai.azure.com/openai/v1/", "briefing-model", "absolute HTTPS URI")]
    [InlineData("https://example.openai.azure.com/", "briefing-model", "end with /openai/v1/")]
    [InlineData("https://example.openai.azure.com/openai/v1/", null, "deployment name is required")]
    [InlineData("https://example.openai.azure.com/openai/v1/", "", "deployment name is required")]
    public void Rejects_invalid_configuration(string? endpoint, string? deploymentName, string expectedFailure)
    {
        var options = new FoundryOptions
        {
            Endpoint = endpoint!,
            DeploymentName = deploymentName!
        };

        Assert.False(options.TryValidate(out var failure));
        Assert.Contains(expectedFailure, failure, StringComparison.OrdinalIgnoreCase);
    }
}

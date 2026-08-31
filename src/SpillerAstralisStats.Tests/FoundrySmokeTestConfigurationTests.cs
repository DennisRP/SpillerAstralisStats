namespace SpillerAstralisStats.Tests;

public sealed class FoundrySmokeTestConfigurationTests
{
    [Fact]
    public void Uses_local_settings_values_when_environment_values_are_absent()
    {
        var settingsPath = WriteLocalSettings();

        try
        {
            var configuration = FoundrySmokeTestConfiguration.Load(settingsPath, _ => null);

            Assert.True(configuration.IsEnabled);
            Assert.Equal("https://local.openai.azure.com/openai/v1/", configuration.Endpoint);
            Assert.Equal("local-deployment", configuration.DeploymentName);
        }
        finally
        {
            File.Delete(settingsPath);
        }
    }

    [Fact]
    public void Environment_values_override_local_settings_values()
    {
        var settingsPath = WriteLocalSettings();
        var environment = new Dictionary<string, string?>
        {
            ["RUN_FOUNDRY_SMOKE_TEST"] = "false",
            ["Foundry__Endpoint"] = "https://environment.openai.azure.com/openai/v1/",
            ["Foundry__DeploymentName"] = "environment-deployment"
        };

        try
        {
            var configuration = FoundrySmokeTestConfiguration.Load(
                settingsPath,
                name => environment.GetValueOrDefault(name));

            Assert.False(configuration.IsEnabled);
            Assert.Equal("https://environment.openai.azure.com/openai/v1/", configuration.Endpoint);
            Assert.Equal("environment-deployment", configuration.DeploymentName);
        }
        finally
        {
            File.Delete(settingsPath);
        }
    }

    [Fact]
    public void Missing_local_settings_and_environment_values_leave_smoke_test_disabled()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.settings.json");

        var configuration = FoundrySmokeTestConfiguration.Load(missingPath, _ => null);

        Assert.False(configuration.IsEnabled);
        Assert.Null(configuration.Endpoint);
        Assert.Null(configuration.DeploymentName);
    }

    private static string WriteLocalSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.settings.json");
        File.WriteAllText(path, """
            {
              "Values": {
                "RUN_FOUNDRY_SMOKE_TEST": "true",
                "Foundry__Endpoint": "https://local.openai.azure.com/openai/v1/",
                "Foundry__DeploymentName": "local-deployment"
              }
            }
            """);
        return path;
    }
}

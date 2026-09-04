using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Infrastructure.Foundry;

namespace SpillerAstralisStats.Tests;

public sealed class FoundryServiceCollectionExtensionsTests
{
    [Fact]
    public void Registration_does_not_require_Foundry_configuration_until_briefing_is_resolved()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddMatchBriefing(configuration);
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<MatchBriefingGenerator>());
        Assert.Equal("Foundry endpoint is required.", exception.Message);
    }

    [Fact]
    public void Valid_configuration_resolves_the_briefing_generator_without_a_network_call()
    {
        var values = new Dictionary<string, string?>
        {
            [$"{SpillerAstralisStats.Configuration.FoundryOptions.SectionName}:Endpoint"] = "https://example-resource.openai.azure.com/openai/v1/",
            [$"{SpillerAstralisStats.Configuration.FoundryOptions.SectionName}:DeploymentName"] = "briefing-deployment"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();

        services.AddMatchBriefing(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<MatchBriefingGenerator>());
    }

    [Fact]
    public void Explicit_article_RAG_registration_resolves_without_a_network_call()
    {
        var services = new ServiceCollection();

        services.AddArticleRag(new SpillerAstralisStats.Configuration.FoundryOptions
        {
            Endpoint = "https://example-resource.openai.azure.com/openai/v1/",
            DeploymentName = "article-rag-deployment"
        });
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IArticleRagService>());
    }

    [Fact]
    public void Daily_function_depends_on_the_grounded_briefing_boundary_not_the_model_client()
    {
        var constructor = Assert.Single(typeof(DailyStatsFunction).GetConstructors());

        Assert.DoesNotContain(
            constructor.GetParameters(),
            parameter => parameter.ParameterType == typeof(MatchBriefingGenerator)
                || parameter.ParameterType == typeof(IMatchBriefingClient));
        Assert.Contains(constructor.GetParameters(), parameter => parameter.ParameterType == typeof(IGroundedMatchBriefingService));
    }
}

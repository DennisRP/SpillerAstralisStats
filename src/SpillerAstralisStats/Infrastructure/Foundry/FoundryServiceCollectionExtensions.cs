using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Configuration;

namespace SpillerAstralisStats.Infrastructure.Foundry;

public static class FoundryServiceCollectionExtensions
{
    public static IServiceCollection AddMatchBriefing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FoundryOptions>()
            .Bind(configuration.GetSection(FoundryOptions.SectionName));
        AddArticleRagServices(services);
        services.AddSingleton<IMatchBriefingClient, MatchBriefingClientAdapter>();
        services.AddSingleton<MatchBriefingGenerator>();
        services.AddSingleton<IGroundedMatchBriefingService>(serviceProvider =>
            new GroundedMatchBriefingService(() => serviceProvider.GetRequiredService<MatchBriefingGenerator>()));
        return services;
    }

    public static IServiceCollection AddArticleRag(
        this IServiceCollection services,
        FoundryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        services.AddSingleton<IOptions<FoundryOptions>>(Options.Create(options));
        AddArticleRagServices(services);
        return services;
    }

    private static void AddArticleRagServices(IServiceCollection services)
    {
        services.AddSingleton<IStructuredOutputClient, FoundryMatchBriefingClient>();
        services.AddSingleton<ArticleBm25Retriever>();
        services.AddSingleton<ArticleRagGenerator>();
        services.AddSingleton<IArticleRagService, ArticleRagService>();
    }
}

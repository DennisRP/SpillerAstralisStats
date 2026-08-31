using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Configuration;

namespace SpillerAstralisStats.Infrastructure.Foundry;

internal static class FoundryServiceCollectionExtensions
{
    public static IServiceCollection AddMatchBriefing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FoundryOptions>()
            .Bind(configuration.GetSection(FoundryOptions.SectionName));
        services.AddSingleton<IMatchBriefingClient, FoundryMatchBriefingClient>();
        services.AddSingleton<MatchBriefingGenerator>();
        services.AddSingleton<IGroundedMatchBriefingService>(serviceProvider =>
            new GroundedMatchBriefingService(() => serviceProvider.GetRequiredService<MatchBriefingGenerator>()));
        return services;
    }
}

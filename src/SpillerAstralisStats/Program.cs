using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Configuration;
using SpillerAstralisStats.Infrastructure.Foundry;
using SpillerAstralisStats.Infrastructure.PandaScore;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddOptions<PandaScoreOptions>()
            .BindConfiguration(PandaScoreOptions.SectionName);
        services.AddOptions<StaticStatsOptions>()
            .BindConfiguration(StaticStatsOptions.SectionName);
        services.AddMatchBriefing(context.Configuration);
        services.AddHttpClient<PandaScoreMatchClient>((serviceProvider, client) =>
        {
            client.BaseAddress = new Uri(
                serviceProvider.GetRequiredService<IOptions<PandaScoreOptions>>().Value.BaseUrl);
        });
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IPandaScoreMatchIngestionService, PandaScoreMatchIngestionService>();
        services.AddSingleton<StaticStatsPublisher>();
    })
    .Build();

host.Run();

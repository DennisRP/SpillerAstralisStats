using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SpillerAstralisStats;

public sealed class DailyStatsFunction(ILogger<DailyStatsFunction> logger)
{
    [Function(nameof(DailyStatsFunction))]
    public void Run(
        [TimerTrigger("0 37 13 * * *", RunOnStartup = true)] TimerInfo timer)
    {
        logger.LogInformation("Hello World");
    }
}

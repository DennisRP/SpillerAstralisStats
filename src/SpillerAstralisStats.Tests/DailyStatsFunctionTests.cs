using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SpillerAstralisStats;

namespace SpillerAstralisStats.Tests;

public sealed class DailyStatsFunctionTests
{
    [Fact]
    public void Run_logs_the_initial_greeting()
    {
        var logger = new TestLogger<DailyStatsFunction>();
        var function = new DailyStatsFunction(logger);

        function.Run(null!);

        Assert.Single(logger.Messages);
        Assert.Equal("Hello World", logger.Messages[0]);
    }

    [Fact]
    public void Run_uses_the_expected_timer_configuration()
    {
        var method = typeof(DailyStatsFunction).GetMethod(nameof(DailyStatsFunction.Run));
        var timerTrigger = method?.GetParameters()[0].GetCustomAttribute<TimerTriggerAttribute>();

        Assert.NotNull(timerTrigger);
        Assert.Equal("0 37 13 * * *", timerTrigger!.Schedule);
        Assert.True(timerTrigger.RunOnStartup);
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}

using Microsoft.Extensions.Logging;

namespace NLWebNet.Tests;

/// <summary>
/// Simple test logger implementation for unit tests.
/// </summary>
/// <typeparam name="T">The type being logged.</typeparam>
public class TestLogger<T> : ILogger<T>
{
    private readonly LogLevel _minLogLevel;

    public TestLogger(LogLevel minLogLevel = LogLevel.Debug)
    {
        _minLogLevel = minLogLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLogLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        // In a real test, you might want to capture log messages for assertions
        Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
    }
}

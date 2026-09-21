namespace DotSettings;

/// <summary>
/// Sink pluggable para logs do bootstrap/refresh de env (sem ILogger).
/// </summary>
public static class EnvBootstrapLog
{
    private static readonly Lock Gate = new();
    private static Action<EnvLogLevel, DateTimeOffset, string> _writer = DefaultWriter;

    /// <summary>Sink atual (somente leitura; altere via <see cref="SetWriter"/>).</summary>
    public static Action<EnvLogLevel, DateTimeOffset, string> Writer
    {
        get
        {
            lock (Gate)
            {
                return _writer;
            }
        }
    }

    public static void SetWriter(Action<EnvLogLevel, DateTimeOffset, string> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        lock (Gate)
        {
            _writer = writer;
        }
    }

    public static void ResetWriter()
    {
        lock (Gate)
        {
            _writer = DefaultWriter;
        }
    }

    public static void Write(EnvLogLevel level, string message)
    {
        Action<EnvLogLevel, DateTimeOffset, string> sink;
        lock (Gate)
        {
            sink = _writer;
        }

        sink(level, DateTimeOffset.UtcNow, message);
    }

    private static void DefaultWriter(EnvLogLevel level, DateTimeOffset timestamp, string message)
    {
        Console.WriteLine($"[{timestamp:O}] [{level}] {message}");
    }
}

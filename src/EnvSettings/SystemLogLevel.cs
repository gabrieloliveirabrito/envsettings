namespace EnvSettings;

/// <summary>
/// Níveis de override de logging da aplicação (ex.: chave <c>LOG_LEVELS</c>),
/// alinhados a níveis comuns como Serilog <c>LogEventLevel</c>.
/// Independente de <see cref="EnvLogLevel"/> (somente bootstrap desta biblioteca).
/// </summary>
public enum SystemLogLevel
{
    Verbose = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5,
}

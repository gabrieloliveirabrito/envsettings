namespace EnvSettings;

/// <summary>
/// Níveis apenas do <see cref="EnvBootstrapLog"/> (bootstrap desta biblioteca).
/// Não usar para logs da aplicação — ver <see cref="SystemLogLevel"/>.
/// </summary>
public enum EnvLogLevel
{
    Verbose = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5,
}

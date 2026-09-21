namespace DotSettings;

/// <summary>
/// Falha ao carregar ou converter variável de ambiente obrigatória.
/// </summary>
public sealed class EnvConfigurationException : Exception
{
    public EnvConfigurationException(string message)
        : base(message)
    {
    }

    public EnvConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

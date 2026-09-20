namespace EnvSettings;

/// <summary>
/// Resolve valores: mapa do arquivo .env (não vazio) e fallback para <see cref="Environment"/>.
/// Snapshot imutável após construção; trocado atomicamente no refresh.
/// </summary>
public sealed class EnvValueSource
{
    private readonly IReadOnlyDictionary<string, string> _fileValues;

    public EnvValueSource(IReadOnlyDictionary<string, string>? fileValues = null)
    {
        _fileValues = fileValues ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Retorna o valor do .env se presente e não vazio; senão a variável de ambiente do processo; senão null.
    /// </summary>
    public string? Get(string key)
    {
        if (_fileValues.TryGetValue(key, out var fromFile) && !string.IsNullOrEmpty(fromFile))
        {
            return fromFile;
        }

        var fromEnv = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrEmpty(fromEnv) ? null : fromEnv;
    }
}

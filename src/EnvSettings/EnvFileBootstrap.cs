using DotEnv.Core;

namespace EnvSettings;

/// <summary>
/// Localiza e carrega o arquivo .env (DotEnv.Core). Em container usa apenas o ambiente do processo.
/// </summary>
public static class EnvFileBootstrap
{
    private static readonly Lock Gate = new();

    public const int DefaultMaxParentLevels = 8;

    /// <summary>
    /// Sobe até <paramref name="maxParentLevels"/> pastas a partir de <paramref name="startDirectory"/> até achar <c>.env</c>.
    /// </summary>
    public static string? FindEnvFile(string startDirectory, int maxParentLevels = DefaultMaxParentLevels)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            return null;
        }

        var dir = new DirectoryInfo(startDirectory);
        var remaining = Math.Max(0, maxParentLevels);

        while (dir is not null && remaining >= 0)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
            remaining--;
        }

        return null;
    }

    public static bool IsRunningInContainer()
    {
        var value = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Carrega o .env no processo (quando aplicável) e devolve o mapa arquivo→valor para <see cref="EnvValueSource"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, string> LoadFileMap(string? envFilePath)
    {
        lock (Gate)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);

            if (IsRunningInContainer() || string.IsNullOrEmpty(envFilePath))
            {
                var loader = new EnvLoader();
                loader.AllowOverwriteExistingVars();
                loader.Load();
                return map;
            }

            if (!File.Exists(envFilePath))
            {
                throw new EnvConfigurationException($".env file not found: {envFilePath}");
            }

            ParseEnvFileInto(envFilePath, map);

            var envDirectory = Path.GetDirectoryName(envFilePath);
            if (!string.IsNullOrEmpty(envDirectory))
            {
                var tunnelPath = Path.Combine(envDirectory, ".env.tunnel");
                if (File.Exists(tunnelPath))
                {
                    ParseEnvFileInto(tunnelPath, map);
                }
            }

            var dotenvLoader = new EnvLoader();
            dotenvLoader.AllowOverwriteExistingVars();
            dotenvLoader.AddEnvFile(envFilePath, optional: false);
            if (!string.IsNullOrEmpty(envDirectory))
            {
                var tunnelPath = Path.Combine(envDirectory, ".env.tunnel");
                if (File.Exists(tunnelPath))
                {
                    dotenvLoader.AddEnvFile(tunnelPath, optional: false);
                }
            }

            dotenvLoader.Load();

            // DotEnv.Core pode manter "# comment" no valor; o mapa limpo manda no process env.
            foreach (var (key, value) in map)
            {
                Environment.SetEnvironmentVariable(key, value);
            }

            return map;
        }
    }

    private static void ParseEnvFileInto(string path, Dictionary<string, string> map)
    {
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (trimmed.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed["export ".Length..].TrimStart();
            }

            var eq = trimmed.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            var key = trimmed[..eq].Trim();
            var value = NormalizeEnvValue(trimmed[(eq + 1)..]);
            if (key.Length > 0)
            {
                map[key] = value;
            }
        }
    }

    /// <summary>
    /// Remove comentário inline (<c># …</c>) fora de aspas e aspas externas.
    /// Ex.: <c>123 # canal</c> → <c>123</c>; <c>"a # b"</c> → <c>a # b</c>;
    /// <c>VALUE         #</c> → <c>VALUE</c> (espaços antes do # não ficam no valor).
    /// </summary>
    internal static string NormalizeEnvValue(string raw)
    {
        var value = StripUnquotedInlineComment(raw).Trim();
        if (value.Length == 0)
        {
            return value;
        }

        if (value.Length >= 2 &&
            ((value.StartsWith('"') && value.EndsWith('"')) ||
             (value.StartsWith('\'') && value.EndsWith('\''))))
        {
            return value[1..^1];
        }

        return value;
    }

    private static string StripUnquotedInlineComment(string value)
    {
        var inSingle = false;
        var inDouble = false;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '\'' && !inDouble)
            {
                inSingle = !inSingle;
                continue;
            }

            if (c == '"' && !inSingle)
            {
                inDouble = !inDouble;
                continue;
            }

            if (c == '#' && !inSingle && !inDouble)
            {
                return value[..i].TrimEnd();
            }
        }

        return value;
    }
}

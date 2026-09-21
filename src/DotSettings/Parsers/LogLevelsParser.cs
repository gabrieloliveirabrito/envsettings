using System.Collections.ObjectModel;

namespace DotSettings;

/// <summary>Parseia <c>LOG_LEVELS</c> no formato <c>Key:Warning;Other:Info</c>.</summary>
public static class LogLevelsParser
{
    public static IReadOnlyDictionary<string, SystemLogLevel> Parse(string raw, string envKeyName = "LOG_LEVELS")
    {
        var map = new Dictionary<string, SystemLogLevel>(StringComparer.OrdinalIgnoreCase);
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
        {
            return new ReadOnlyDictionary<string, SystemLogLevel>(map);
        }

        foreach (var segment in trimmed.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colon = segment.IndexOf(':');
            if (colon <= 0 || colon >= segment.Length - 1)
            {
                throw new EnvConfigurationException(
                    $"Invalid {envKeyName} segment '{segment}'. Expected SourceContext:Level.");
            }

            var key = segment[..colon].Trim();
            var levelRaw = segment[(colon + 1)..].Trim();
            if (key.Length == 0 || levelRaw.Length == 0)
            {
                throw new EnvConfigurationException(
                    $"Invalid {envKeyName} segment '{segment}'. Expected SourceContext:Level.");
            }

            if (!TryParseLevel(levelRaw, out var level))
            {
                throw new EnvConfigurationException(
                    $"Invalid log level '{levelRaw}' in {envKeyName} for '{key}'.");
            }

            map[key] = level;
        }

        return new ReadOnlyDictionary<string, SystemLogLevel>(map);
    }

    public static bool TryParseLevel(string raw, out SystemLogLevel level)
    {
        if (string.Equals(raw, "Info", StringComparison.OrdinalIgnoreCase))
        {
            level = SystemLogLevel.Information;
            return true;
        }

        return Enum.TryParse(raw, ignoreCase: true, out level);
    }
}

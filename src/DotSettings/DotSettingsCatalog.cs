using System.Collections.Concurrent;

namespace DotSettings;

/// <summary>
/// Catálogo thread-safe das instâncias de settings carregadas.
/// </summary>
public static class DotSettingsCatalog
{
    private static readonly ConcurrentDictionary<Type, object> ByType = new();

    public static TSettings GetSettings<TSettings>()
        where TSettings : class
    {
        if (!ByType.TryGetValue(typeof(TSettings), out var instance))
        {
            throw new InvalidOperationException(
                $"{typeof(TSettings).Name} has not been loaded. Call Env.Load first.");
        }

        return (TSettings)instance;
    }

    public static bool TryGetSettings<TSettings>(out TSettings? settings)
        where TSettings : class
    {
        if (ByType.TryGetValue(typeof(TSettings), out var instance))
        {
            settings = (TSettings)instance;
            return true;
        }

        settings = null;
        return false;
    }

    public static IReadOnlyDictionary<Type, object> Snapshot() =>
        new Dictionary<Type, object>(ByType);

    internal static void Register(object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var type = instance.GetType();
        ByType[type] = instance;
        DotSettingsAssign.AssignShared(type, instance);
    }

    internal static void Clear()
    {
        ByType.Clear();
    }

    internal static IEnumerable<object> AllInstances() => ByType.Values;
}

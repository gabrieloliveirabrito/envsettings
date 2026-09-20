namespace EnvSettings;

/// <summary>
/// Base CRTP: cada settings herda <c>EnvSettings&lt;TSelf&gt;</c> e expõe <see cref="Shared"/>.
/// </summary>
public abstract class EnvSettings<TSelf>
    where TSelf : EnvSettings<TSelf>, new()
{
    private static TSelf? _shared;

    /// <summary>Instância carregada por <see cref="Env.Load{THost}"/> (mesma do catálogo).</summary>
    public static TSelf Shared
    {
        get
        {
            var instance = Volatile.Read(ref _shared);
            if (instance is null)
            {
                throw new InvalidOperationException(
                    $"{typeof(TSelf).Name} has not been loaded. Call Env.Load first.");
            }

            return instance;
        }
    }

    internal static void AssignShared(TSelf instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        Volatile.Write(ref _shared, instance);
    }

    internal static void ClearShared() => Volatile.Write(ref _shared, null);
}

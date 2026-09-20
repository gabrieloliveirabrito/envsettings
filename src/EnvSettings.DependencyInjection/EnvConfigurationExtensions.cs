using Microsoft.Extensions.DependencyInjection;

namespace EnvSettings;

public static class EnvConfigurationExtensions
{
    /// <summary>
    /// Registra todas as instâncias do <see cref="EnvSettingsCatalog"/> como singleton.
    /// Requer <see cref="Env.Load{THost}"/> prévio.
    /// </summary>
    public static IServiceCollection AddEnvSettings(this IServiceCollection services)
    {
        foreach (var (type, instance) in EnvSettingsCatalog.Snapshot())
        {
            services.AddSingleton(type, instance);
        }

        return services;
    }
}

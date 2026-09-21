using Microsoft.Extensions.DependencyInjection;

namespace DotSettings;

public static class EnvConfigurationExtensions
{
    /// <summary>
    /// Registra todas as instâncias do <see cref="DotSettingsCatalog"/> como singleton.
    /// Requer <see cref="Env.Load{THost}"/> prévio.
    /// </summary>
    public static IServiceCollection AddDotSettings(this IServiceCollection services)
    {
        foreach (var (type, instance) in DotSettingsCatalog.Snapshot())
        {
            services.AddSingleton(type, instance);
        }

        return services;
    }
}

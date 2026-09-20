using System.Reflection;

namespace EnvSettings;

/// <summary>
/// API estática de load/refresh do grafo de settings (thread-safe).
/// </summary>
public static class Env
{
    private static readonly Lock Gate = new();
    private static LoadState? _state;

    public sealed class LoadOptions
    {
        public string? RootPath { get; init; }

        public int MaxParentLevels { get; init; } = EnvFileBootstrap.DefaultMaxParentLevels;

        /// <summary>Logs informativos de bootstrap (path, environment-only).</summary>
        public bool LogToConsole { get; init; }
    }

    private sealed class LoadState
    {
        public required Type HostType { get; init; }
        public required object HostInstance { get; init; }
        public required string? EnvFilePath { get; set; }
        public required string? RootPath { get; init; }
        public required int MaxParentLevels { get; init; }
        public required bool LogToConsole { get; init; }
        public required Delegate? PostBind { get; init; }
        public required EnvValueSource ValueSource { get; set; }
    }

    /// <summary>
    /// Carrega o host e todas as <see cref="EnvSubConfigurationAttribute"/> no catálogo / Shared.
    /// Configuração inválida lança <see cref="EnvConfigurationException"/>.
    /// </summary>
    public static THost Load<THost>(
        LoadOptions? options = null,
        Action<THost>? postBind = null)
        where THost : class, new()
    {
        options ??= new LoadOptions();

        lock (Gate)
        {
            if (_state is not null)
            {
                throw new InvalidOperationException(
                    "Env settings already loaded. Call Refresh to reload, or ResetForTesting in tests.");
            }

            var envFilePath = ResolveEnvFilePath(options);
            if (options.LogToConsole)
            {
                EnvBootstrapLog.Write(
                    EnvLogLevel.Information,
                    envFilePath is null
                        ? "EnvSettings: loading from process Environment only (no .env file)."
                        : $"EnvSettings: loading from .env at '{envFilePath}'.");
            }

            var fileMap = EnvFileBootstrap.LoadFileMap(envFilePath);
            var source = new EnvValueSource(fileMap);
            var host = new THost();
            EnvConfigurationBinder.BindObject(host, source, registerInstances: true);
            postBind?.Invoke(host);

            _state = new LoadState
            {
                HostType = typeof(THost),
                HostInstance = host,
                EnvFilePath = envFilePath,
                RootPath = options.RootPath,
                MaxParentLevels = options.MaxParentLevels,
                LogToConsole = options.LogToConsole,
                PostBind = postBind,
                ValueSource = source,
            };

            if (options.LogToConsole)
            {
                var count = EnvSettingsCatalog.Snapshot().Count;
                EnvBootstrapLog.Write(
                    EnvLogLevel.Information,
                    $"EnvSettings: bound {count} settings type(s) successfully.");
            }

            return host;
        }
    }

    /// <summary>
    /// Revalida em scratch; se inválido, loga e aborta. Se válido, aplica in-place nas instâncias Shared.
    /// </summary>
    public static bool Refresh()
    {
        lock (Gate)
        {
            if (_state is null)
            {
                EnvBootstrapLog.Write(
                    EnvLogLevel.Warning,
                    "Env.Refresh called before Load; ignored.");
                return false;
            }

            try
            {
                var envFilePath = _state.EnvFilePath;
                if (!string.IsNullOrEmpty(_state.RootPath))
                {
                    envFilePath = EnvFileBootstrap.FindEnvFile(_state.RootPath, _state.MaxParentLevels)
                        ?? envFilePath;
                }

                var fileMap = EnvFileBootstrap.LoadFileMap(envFilePath);
                var source = new EnvValueSource(fileMap);
                var scratch = Activator.CreateInstance(_state.HostType)
                    ?? throw new EnvConfigurationException($"Could not create {_state.HostType.Name}.");

                EnvConfigurationBinder.BindObject(scratch, source, registerInstances: false);
                InvokePostBind(scratch, _state.PostBind);

                EnvConfigurationBinder.CopyEnvKeyedProperties(scratch, _state.HostInstance);
                InvokePostBind(_state.HostInstance, _state.PostBind);

                _state.EnvFilePath = envFilePath;
                _state.ValueSource = source;

                EnvBootstrapLog.Write(EnvLogLevel.Information, "EnvSettings: refresh applied successfully.");
                return true;
            }
            catch (Exception ex)
            {
                EnvBootstrapLog.Write(
                    EnvLogLevel.Error,
                    $"EnvSettings: refresh aborted; shared settings unchanged. {ex}");
                return false;
            }
        }
    }

    /// <summary>Limpa catálogo e Shared (apenas testes).</summary>
    public static void ResetForTesting()
    {
        lock (Gate)
        {
            foreach (var type in EnvSettingsCatalog.Snapshot().Keys)
            {
                EnvSettingsAssign.ClearShared(type);
            }

            EnvSettingsCatalog.Clear();
            _state = null;
        }
    }

    /// <summary>Registra instâncias já montadas (ambiente Testing) sem ler .env.</summary>
    public static void LoadPrepared<THost>(THost host, Action<THost>? postBind = null)
        where THost : class
    {
        ArgumentNullException.ThrowIfNull(host);
        lock (Gate)
        {
            if (_state is not null)
            {
                ResetForTestingUnlocked();
            }

            RegisterTree(host);
            postBind?.Invoke(host);

            _state = new LoadState
            {
                HostType = typeof(THost),
                HostInstance = host,
                EnvFilePath = null,
                RootPath = null,
                MaxParentLevels = EnvFileBootstrap.DefaultMaxParentLevels,
                LogToConsole = false,
                PostBind = postBind,
                ValueSource = new EnvValueSource(),
            };
        }
    }

    private static void ResetForTestingUnlocked()
    {
        foreach (var type in EnvSettingsCatalog.Snapshot().Keys)
        {
            EnvSettingsAssign.ClearShared(type);
        }

        EnvSettingsCatalog.Clear();
        _state = null;
    }

    private static void RegisterTree(object node)
    {
        EnvSettingsCatalog.Register(node);
        foreach (var property in node.GetType().GetProperties(
                     BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<EnvSubConfigurationAttribute>() is null)
            {
                continue;
            }

            var child = property.GetValue(node);
            if (child is not null)
            {
                RegisterTree(child);
            }
        }
    }

    private static string? ResolveEnvFilePath(LoadOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RootPath))
        {
            return null;
        }

        return EnvFileBootstrap.FindEnvFile(options.RootPath, options.MaxParentLevels);
    }

    private static void InvokePostBind(object host, Delegate? postBind)
    {
        if (postBind is null)
        {
            return;
        }

        postBind.DynamicInvoke(host);
    }
}

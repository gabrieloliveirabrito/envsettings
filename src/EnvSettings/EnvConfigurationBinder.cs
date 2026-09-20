using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

namespace EnvSettings;

/// <summary>
/// Liga classes de configuração ao <see cref="EnvValueSource"/> via reflection,
/// <see cref="EnvKeyAttribute"/> e <see cref="EnvSubConfigurationAttribute"/>.
/// </summary>
public static class EnvConfigurationBinder
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    public static void BindObject(object target, EnvValueSource source, bool registerInstances)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        var type = target.GetType();
        if (registerInstances)
        {
            EnvSettingsCatalog.Register(target);
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite && property.GetCustomAttribute<EnvSubConfigurationAttribute>() is null)
            {
                continue;
            }

            if (property.GetCustomAttribute<EnvKeyAttribute>() is { } envKey)
            {
                BindLeaf(target, property, envKey, source);
                continue;
            }

            if (property.GetCustomAttribute<EnvSubConfigurationAttribute>() is not null)
            {
                BindSubConfiguration(target, property, source, registerInstances);
            }
        }
    }

    /// <summary>Copia propriedades <see cref="EnvKeyAttribute"/> de <paramref name="from"/> para <paramref name="to"/> (mesma forma de árvore).</summary>
    public static void CopyEnvKeyedProperties(object from, object to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        if (from.GetType() != to.GetType())
        {
            throw new ArgumentException("Source and target must be the same type.");
        }

        foreach (var property in from.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<EnvKeyAttribute>() is not null && property.CanRead && property.CanWrite)
            {
                property.SetValue(to, property.GetValue(from));
                continue;
            }

            if (property.GetCustomAttribute<EnvSubConfigurationAttribute>() is null)
            {
                continue;
            }

            var fromChild = property.GetValue(from);
            var toChild = property.GetValue(to);
            if (fromChild is null || toChild is null)
            {
                continue;
            }

            CopyEnvKeyedProperties(fromChild, toChild);
        }
    }

    private static void BindSubConfiguration(
        object parent,
        PropertyInfo property,
        EnvValueSource source,
        bool registerInstances)
    {
        var child = property.GetValue(parent);
        if (child is null)
        {
            child = Activator.CreateInstance(property.PropertyType)
                ?? throw new EnvConfigurationException(
                    $"Could not create instance of {property.PropertyType.Name} for {parent.GetType().Name}.{property.Name}.");
            if (property.CanWrite)
            {
                property.SetValue(parent, child);
            }
        }

        BindObject(child, source, registerInstances);
    }

    private static void BindLeaf(object target, PropertyInfo property, EnvKeyAttribute envKey, EnvValueSource source)
    {
        if (!property.CanWrite)
        {
            return;
        }

        var raw = source.Get(envKey.Name);
        var optional = IsOptionalProperty(property);

        if (raw is null)
        {
            if (!optional)
            {
                throw new EnvConfigurationException(
                    $"Environment variable '{envKey.Name}' is required for {target.GetType().Name}.{property.Name}.");
            }

            return;
        }

        try
        {
            var converted = ConvertValue(raw, property.PropertyType, envKey.Name);
            property.SetValue(target, converted);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException or EnvConfigurationException)
        {
            if (ex is EnvConfigurationException)
            {
                throw;
            }

            throw new EnvConfigurationException(
                $"Invalid value for '{envKey.Name}' ({target.GetType().Name}.{property.Name}): {ex.Message}",
                ex);
        }
    }

    private static bool IsOptionalProperty(PropertyInfo property)
    {
        if (NullabilityContext.Create(property).WriteState == NullabilityState.Nullable)
        {
            return true;
        }

        return Nullable.GetUnderlyingType(property.PropertyType) is not null;
    }

    private static object? ConvertValue(string raw, Type targetType, string envKeyName)
    {
        if (IsSystemLogLevelOverridesDictionary(targetType))
        {
            return LogLevelsParser.Parse(raw, envKeyName);
        }

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(string))
        {
            return Unquote(raw);
        }

        if (underlying == typeof(bool))
        {
            return bool.Parse(raw);
        }

        if (underlying == typeof(int))
        {
            return int.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        if (underlying == typeof(long))
        {
            return long.Parse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        if (underlying.IsEnum)
        {
            return Enum.Parse(underlying, raw, ignoreCase: true);
        }

        return Convert.ChangeType(raw, underlying, CultureInfo.InvariantCulture);
    }

    private static bool IsSystemLogLevelOverridesDictionary(Type targetType)
    {
        if (!targetType.IsGenericType)
        {
            return false;
        }

        var genericArgs = targetType.GetGenericArguments();
        if (genericArgs.Length != 2 || genericArgs[0] != typeof(string) || genericArgs[1] != typeof(SystemLogLevel))
        {
            return false;
        }

        return typeof(IReadOnlyDictionary<string, SystemLogLevel>).IsAssignableFrom(targetType)
            || targetType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)
            || targetType.GetGenericTypeDefinition() == typeof(ReadOnlyDictionary<,>)
            || targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value.StartsWith('"') && value.EndsWith('"')) ||
             (value.StartsWith('\'') && value.EndsWith('\''))))
        {
            return value[1..^1];
        }

        return value;
    }
}

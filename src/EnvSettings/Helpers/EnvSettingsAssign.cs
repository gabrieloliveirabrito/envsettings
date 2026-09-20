using System.Reflection;

namespace EnvSettings;

/// <summary>Reflection helper para assign de <see cref="EnvSettings{TSelf}.Shared"/>.</summary>
internal static class EnvSettingsAssign
{
    public static void AssignShared(Type concreteType, object instance)
    {
        var envBase = FindEnvSettingsBase(concreteType);
        if (envBase is null)
        {
            return;
        }

        var method = envBase.GetMethod(
            "AssignShared",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [envBase.GetGenericArguments()[0]],
            modifiers: null);

        method?.Invoke(null, [instance]);
    }

    public static void ClearShared(Type concreteType)
    {
        var envBase = FindEnvSettingsBase(concreteType);
        if (envBase is null)
        {
            return;
        }

        var method = envBase.GetMethod("ClearShared", BindingFlags.NonPublic | BindingFlags.Static);
        method?.Invoke(null, null);
    }

    private static Type? FindEnvSettingsBase(Type concreteType)
    {
        var current = concreteType;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(EnvSettings<>))
            {
                return current;
            }

            current = current.BaseType;
        }

        return null;
    }
}

using System.Reflection;

namespace DotSettings;

/// <summary>Reflection helper para assign de <see cref="DotSettings{TSelf}.Shared"/>.</summary>
internal static class DotSettingsAssign
{
    public static void AssignShared(Type concreteType, object instance)
    {
        var envBase = FindDotSettingsBase(concreteType);
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
        var envBase = FindDotSettingsBase(concreteType);
        if (envBase is null)
        {
            return;
        }

        var method = envBase.GetMethod("ClearShared", BindingFlags.NonPublic | BindingFlags.Static);
        method?.Invoke(null, null);
    }

    private static Type? FindDotSettingsBase(Type concreteType)
    {
        var current = concreteType;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(DotSettings<>))
            {
                return current;
            }

            current = current.BaseType;
        }

        return null;
    }
}

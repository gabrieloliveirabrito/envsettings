using System.Linq.Expressions;
using System.Reflection;

namespace EnvSettings;

/// <summary>Resolve o nome da variável de ambiente a partir de <see cref="EnvKeyAttribute"/>.</summary>
public static class EnvKeyHelper
{
    public static string GetVariableName<TSettings>(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        var property = typeof(TSettings).GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property is null)
        {
            throw new ArgumentException(
                $"Property '{propertyName}' was not found on {typeof(TSettings).Name}.",
                nameof(propertyName));
        }

        var envKey = property.GetCustomAttribute<EnvKeyAttribute>()
            ?? throw new ArgumentException(
                $"Property '{typeof(TSettings).Name}.{property.Name}' has no [EnvKey].",
                nameof(propertyName));

        return envKey.Name;
    }

    public static string GetVariableName<TSettings>(Expression<Func<TSettings, object?>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);
        var member = propertyExpression.Body switch
        {
            MemberExpression m => m,
            UnaryExpression { Operand: MemberExpression m } => m,
            _ => throw new ArgumentException("Expression must be a property access.", nameof(propertyExpression)),
        };

        if (member.Member is not PropertyInfo)
        {
            throw new ArgumentException("Expression must be a property access.", nameof(propertyExpression));
        }

        return GetVariableName<TSettings>(member.Member.Name);
    }
}

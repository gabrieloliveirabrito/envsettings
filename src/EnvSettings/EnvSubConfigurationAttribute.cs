namespace EnvSettings;

/// <summary>
/// Marca uma propriedade aninhada de configuração. Só estas propriedades entram na recursão do binder.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EnvSubConfigurationAttribute : Attribute;

namespace DotSettings;

/// <summary>
/// Mapeia a propriedade para uma chave do .env. Apenas propriedades anotadas são ligadas.
/// Propriedades nullable (<c>string?</c>, <c>int?</c>, …) aceitam ausência da chave; demais exigem valor.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EnvKeyAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

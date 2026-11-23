namespace Fdw.Data.Serialization.Converters;

/// <summary>The JSON shape of an <see cref="Fdw.Data.Abstractions.IFieldType"/>, read as a typed value rather than property by property.</summary>
internal sealed class FieldTypeWire
{
    /// <summary>Gets or sets the type's name.</summary>
    public string? TypeName { get; set; }

    /// <summary>Gets or sets the assembly-qualified or loadable name of the CLR type.</summary>
    public string? ClrType { get; set; }
}

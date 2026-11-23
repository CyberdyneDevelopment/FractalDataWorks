using Fdw.Data.Abstractions;

namespace Fdw.Data.Serialization.Converters;

/// <summary>
/// The JSON shape of an <see cref="IField"/>, read as a typed value rather than property by property. A property the
/// shape does not name is skipped by the reader, as the property-by-property read skipped it.
/// </summary>
internal sealed class FieldWire
{
    /// <summary>Gets or sets the field's name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the field's type; read by <see cref="FieldTypeConverter"/>.</summary>
    public IFieldType? FieldType { get; set; }

    /// <summary>Gets or sets the name of the field's datum type; null when the field was never typed.</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets whether the field accepts null.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets whether the field is an identity column.</summary>
    public bool IsIdentity { get; set; }

    /// <summary>Gets or sets whether the field is computed.</summary>
    public bool IsComputed { get; set; }

    /// <summary>Gets or sets whether the system provides the field's value.</summary>
    public bool IsSystemProvided { get; set; }

    /// <summary>Gets or sets whether the field has a default.</summary>
    public bool HasDefault { get; set; }

    /// <summary>Gets or sets the field's description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the field's type-system id.</summary>
    public string? TypeSystemId { get; set; }

    /// <summary>Gets or sets the field's converter type id.</summary>
    public int? ConverterTypeId { get; set; }
}

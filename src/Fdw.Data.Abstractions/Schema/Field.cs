using System.Collections.Generic;
using Fdw.Schema;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Implementation of field metadata with type and role information.
/// </summary>
public sealed class Field : IField
{
    /// <summary>
    /// Gets or initializes the field name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes the field type.
    /// </summary>
    public required IFieldType FieldType { get; init; }

    /// <summary>
    /// Gets or initializes the field role (from PropertyRoles TypeCollection).
    /// </summary>
    public required IPropertyRole Role { get; init; }

    /// <summary>
    /// Gets or initializes whether the field is nullable.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// Gets whether the field is required (inverse of IsNullable).
    /// </summary>
    /// <remarks>
    /// Implements IPropertyDefinition.IsRequired.
    /// </remarks>
    public bool IsRequired => !IsNullable;

    /// <summary>
    /// Gets or initializes the field description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or initializes optional metadata associated with this field.
    /// </summary>
    /// <remarks>
    /// Implements IPropertyDefinition.Metadata.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets or initializes the type system identifier (e.g., "MsSql", "JsonSchema").
    /// </summary>
    public string? TypeSystemId { get; init; }

    /// <summary>
    /// Gets or initializes the converter ID within the type system.
    /// </summary>
    public int? ConverterTypeId { get; init; }

    /// <summary>
    /// Gets or initializes whether this field is an identity/auto-increment column.
    /// </summary>
    public bool IsIdentity { get; init; }

    /// <summary>
    /// Gets or initializes whether this field is a computed column.
    /// </summary>
    public bool IsComputed { get; init; }

    /// <summary>
    /// Gets or initializes whether this field's value is provided by the system.
    /// </summary>
    public bool IsSystemProvided { get; init; }
}

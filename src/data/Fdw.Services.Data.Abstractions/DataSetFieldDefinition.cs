using System;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Describes a single field (column) within a DataSet schema.
/// </summary>
public sealed record DataSetFieldDefinition
{
    /// <summary>Gets the identifier of the DataSet this field belongs to.</summary>
    public Guid DataSetId { get; init; }

    /// <summary>Gets the name of the field as it appears in the DataSet schema.</summary>
    public string FieldName { get; init; } = string.Empty;

    /// <summary>Gets the scalar type name for this field (e.g., "String", "Int32", "Decimal").</summary>
    public string ScalarTypeName { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether this field accepts null values.</summary>
    public bool IsNullable { get; init; }

    /// <summary>Gets the ordinal position of this field within the DataSet.</summary>
    public int Ordinal { get; init; }

    /// <summary>Gets an optional human-readable description of this field's purpose.</summary>
    public string? Description { get; init; }
}

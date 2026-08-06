using System.Collections.Generic;
using Fdw.Schema;
using Fdw.Schema.Properties;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents a single field/column with type and role metadata.
/// </summary>
/// <remarks>
/// Extends IPropertyDefinition from Schema.Abstractions for unified property handling.
/// The Role property is inherited and typed as IPropertyRole.
/// </remarks>
public interface IField : IPropertyDefinition
{
    // Name property inherited from IPropertyDefinition
    // Role property inherited from IPropertyDefinition (IPropertyRole type)
    // IsRequired property inherited from IPropertyDefinition (inverse of IsNullable)
    // Description property inherited from IPropertyDefinition
    // Metadata property inherited from IPropertyDefinition

    /// <summary>
    /// Field type (can be simple, array, or object with recursive nesting).
    /// </summary>
    IFieldType FieldType { get; }

    /// <summary>
    /// Whether this field can be null.
    /// </summary>
    /// <remarks>
    /// This is the inverse of IPropertyDefinition.IsRequired.
    /// IsRequired == !IsNullable
    /// </remarks>
    bool IsNullable { get; }

    /// <summary>
    /// Type system identifier (e.g., "MsSql", "JsonSchema").
    /// </summary>
    string? TypeSystemId { get; }

    /// <summary>
    /// Converter ID within the type system.
    /// </summary>
    int? ConverterTypeId { get; }

    /// <summary>
    /// Whether this field is an identity/auto-increment column.
    /// </summary>
    bool IsIdentity { get; }

    /// <summary>
    /// Whether this field is a computed column.
    /// </summary>
    bool IsComputed { get; }

    /// <summary>
    /// Whether this field's value is provided by the system (DEFAULT, IDENTITY, computed, server-generated).
    /// Translators skip system-provided fields on INSERT — the source system provides the value.
    /// </summary>
    /// <remarks>
    /// Why: Superset of IsIdentity and IsComputed. Also covers DEFAULT constraints (e.g., NEWSEQUENTIALID(),
    /// SYSDATETIMEOFFSET()) and any source-system-generated values (REST server timestamps, etc.).
    /// </remarks>
    bool IsSystemProvided { get; }
}

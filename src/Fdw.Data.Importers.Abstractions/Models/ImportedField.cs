using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.SchemaImporters.Abstractions.Models;

/// <summary>
/// Imported Field (column, property, etc.).
/// </summary>
/// <ExcludedFromCoverage>DTO with init-only properties</ExcludedFromCoverage>
[ExcludeFromCodeCoverage]
public sealed class ImportedField
{
    /// <summary>
    /// Field name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Source type name (e.g., "nvarchar", "integer", "string").
    /// </summary>
    public string SourceType { get; init; } = string.Empty;

    /// <summary>
    /// Mapped .NET CLR type.
    /// </summary>
    public Type DotNetType { get; init; } = typeof(object);

    /// <summary>
    /// Whether the field is nullable.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// The key type name for this field, or null if the field is not a key.
    /// </summary>
    /// <remarks>
    /// Why: Replaces the IsPrimaryKey boolean — key identity is now expressed via
    /// KeyType name ("Surrogate", "Natural", "Foreign", "Join") and persisted as
    /// DataContainerKeyField entries rather than a flag on the field itself.
    /// </remarks>
    public string? KeyType { get; init; }

    /// <summary>
    /// Whether this field is an identity/auto-increment column.
    /// </summary>
    public bool IsIdentity { get; init; }

    /// <summary>
    /// Whether this field is required (NOT NULL).
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Maximum length for string/binary types.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Precision for decimal types.
    /// </summary>
    public int? Precision { get; init; }

    /// <summary>
    /// Scale for decimal types.
    /// </summary>
    public int? Scale { get; init; }

    /// <summary>
    /// Default value for this field.
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Additional field metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);
}

#pragma warning disable CS1591
using System;

namespace Fdw.Schema.Properties;

/// <summary>
/// Represents a logical field with .NET type information and data mapping.
/// Use for data transformation, ETL operations, and business logic.
/// </summary>
public interface IFieldDefinition : IPropertyDefinition
{
    /// <summary>
    /// The .NET type of the field (e.g., typeof(string), typeof(int)).
    /// </summary>
    Type ClrType { get; }

    /// <summary>
    /// Name of the source column/property this field maps to.
    /// Null if field name matches source name.
    /// </summary>
    string? SourceMapping { get; }

    /// <summary>
    /// Name of the calculator to compute this field's value.
    /// Null if not a calculated field.
    /// </summary>
    string? Calculator { get; }

    /// <summary>
    /// Name of the transformer to apply to this field's value.
    /// Null if no transformation needed.
    /// </summary>
    string? Transformer { get; }

    /// <summary>
    /// Format string for parsing/formatting the value.
    /// </summary>
    string? Format { get; }
}

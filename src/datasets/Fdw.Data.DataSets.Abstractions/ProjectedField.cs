using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Represents a field in a SELECT projection.
/// </summary>
public sealed class ProjectedField
{
    /// <summary>
    /// Gets the source field name from the dataset.
    /// </summary>
    public string SourceField { get; init; } = string.Empty;

    /// <summary>
    /// Gets the alias for this field in the result.
    /// </summary>
    public string Alias { get; init; } = string.Empty;

    /// <summary>
    /// Gets the field type.
    /// </summary>
    public Type FieldType { get; init; } = typeof(object);
}
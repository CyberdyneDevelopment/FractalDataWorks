using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One column of a table input: its name and the kind of value it holds.
/// Rows live in <c>transform.BuiltTransformationConfigurationTableInputColumn</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class BuiltTransformationTableInputColumnConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and name.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the table input this column belongs to.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value type: Number, Flag or Text.</summary>
    public string ValueType { get; set; } = string.Empty;

    /// <summary>Gets or sets the column's position within its table.</summary>
    public int ExecutionOrder { get; set; }
}

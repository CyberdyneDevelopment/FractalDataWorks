using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// A total or a lookup formulas can read by name: Sum, Count, Min, Max, Average over a column of a table, or a Lookup of a value in another table.
/// Rows live in <c>transform.BuiltTransformationConfigurationDerived</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class BuiltTransformationDerivedConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and name.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name formulas read the value under.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind: Sum, Count, Min, Max, Average or Lookup.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the table the value is taken from.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column that is totalled, or, for a lookup, the column whose value is taken.</summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>Gets or sets a lookup's key column in the searched table; null for a total.</summary>
    public string? KeyColumn { get; set; }

    /// <summary>Gets or sets the name of the current row's value a lookup searches for; null for a total.</summary>
    public string? MatchField { get; set; }

    /// <summary>Gets or sets the value's position among the derived values.</summary>
    public int ExecutionOrder { get; set; }
}

using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One column whose values define a group, for an output made of one row per group.
/// Rows live in <c>transform.BuiltTransformationConfigurationTableOutputGroupColumn</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class BuiltTransformationTableOutputGroupColumnConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and name.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the table output this group column belongs to.</summary>
    public string OutputName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column that defines the group.</summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column's position among the group columns.</summary>
    public int Ordinal { get; set; }
}

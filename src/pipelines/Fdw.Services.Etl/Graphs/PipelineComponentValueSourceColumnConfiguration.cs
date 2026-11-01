using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// One ordering column of a sequenced value source of a destination (FDW-1636): the rows of a batch are numbered in the order of
/// these columns.
/// </summary>
/// <remarks>Cascade child of a value source, in <c>pipe.PipelineComponentValueSourceColumn</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineComponentValueSourceColumnConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the position of the column in the ordering.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the column's name.</summary>
    public string ColumnName { get; set; } = string.Empty;
}

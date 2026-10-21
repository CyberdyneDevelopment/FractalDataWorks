using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// One column of a component setting that is an ordered list of columns (FDW-1396): a join or lookup key, or a destination's key,
/// compare or write column.
/// </summary>
/// <remarks>Cascade child of a component, in <c>pipe.PipelineComponentColumn</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineComponentColumnConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the setting the column belongs to.</summary>
    public string SettingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the position of the column within its setting.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the column's name.</summary>
    public string ColumnName { get; set; } = string.Empty;
}

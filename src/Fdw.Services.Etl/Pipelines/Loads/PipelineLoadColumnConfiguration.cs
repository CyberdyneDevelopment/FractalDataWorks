using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// One column of a named list on a load step or a value source: a match key, a compared column, a written column
/// or a sequence ordering column (FDW-1108). Persisted in <c>pipe.PipelineLoadColumn</c>; cascade child of
/// <see cref="PipelineLoadStepConfiguration"/> (key <c>PipelineLoadStepRowId</c>) and of
/// <see cref="PipelineLoadValueSourceConfiguration"/> (key <c>PipelineLoadValueSourceRowId</c>), the way
/// <c>PipelineTransformConfiguration</c> hangs from either engine.
/// </summary>
/// <remarks>
/// Why: implements <see cref="IGenericConfiguration"/> so the configuration cascade-save discovers and persists the
/// collections that hold it when the parent is saved. Without this interface the collection is silently skipped.
/// <see cref="Name"/> carries the column's name, because the interface requires one.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineLoadColumnConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for display and binding.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the column, resolved against <see cref="PipelineLoadColumnRoles"/>.
    /// </summary>
    [ValuesFrom(typeof(PipelineLoadColumnRoles))]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the destination column.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the position among the columns of the same role on the same owner.
    /// </summary>
    public int Ordinal { get; set; }
}

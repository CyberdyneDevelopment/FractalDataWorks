using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// One column of a named list on a load step or a value source: a match key, a compared column, a written column
/// or a sequence ordering column (FDW-1108). The graph destination builders make these in memory for
/// <see cref="PipelineLoadStepConfiguration"/> and <see cref="PipelineLoadValueSourceConfiguration"/>; nothing stores one.
/// </summary>
/// <remarks>
/// <see cref="Name"/> carries the column's name.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class PipelineLoadColumnConfiguration
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

using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Configuration for a single sort field within a Sort transform. Cascade child of
/// <see cref="PipelineTransformConfiguration"/> (pipe.PipelineTransformSortField), ordered by <see cref="Ordinal"/>:
/// the lowest ordinal is the primary sort key.
/// </summary>
/// <remarks>
/// Why: implements <see cref="IGenericConfiguration"/> (mirroring <see cref="PipelineTransformGroupByFieldConfiguration"/>)
/// so the configuration cascade-save discovers and persists this collection when the parent transform is saved.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineTransformSortFieldConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for display/binding.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field name to sort by.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the direction: a <see cref="Fdw.Data.Abstractions.SortDirections"/> option name (Ascending or Descending). Stated, never
    /// assumed.
    /// </summary>
    [ValuesFrom(typeof(SortDirections))]
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordinal position among the sort fields on the same transform.
    /// </summary>
    public int Ordinal { get; set; }
}

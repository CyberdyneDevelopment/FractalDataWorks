using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Configuration for a single brought-across column within a Lookup transform. Cascade child of
/// <see cref="PipelineTransformConfiguration"/> (pipe.LookupOperationConfiguration). One row per
/// output column — the lookup connection/keys are shared across the rows for the same transform.
/// </summary>
/// <remarks>
/// Why: implements <see cref="IGenericConfiguration"/> (mirroring <see cref="PipelineTransformFieldMappingConfiguration"/>)
/// so the configuration cascade-save discovers and persists this collection when the parent transform is
/// saved. Without this interface the Lookups collection would be silently skipped.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Pipeline")]
public sealed partial class PipelineTransformLookupConfiguration : IGenericConfiguration
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
    /// Gets or sets the logical FK to the parent transform (column pipe.LookupOperationConfiguration.PipelineTransformId).
    /// </summary>
    /// <remarks>
    /// Why: set by the configuration cascade-save to the parent transform's logical Id; the configuration
    /// save translator resolves the physical PipelineTransformRowId via FK subquery on insert.
    /// </remarks>
    public Guid PipelineTransformId { get; set; }

    /// <inheritdoc/>
    public string SectionName => string.Empty;

    /// <inheritdoc/>
    public string ServiceType => "Pipeline";

    /// <inheritdoc/>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the lookup connection name.
    /// </summary>
    public string LookupConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lookup data set name.
    /// </summary>
    public string LookupDataSet { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lookup key field.
    /// </summary>
    public string LookupKeyField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source key field to match against.
    /// </summary>
    public string SourceKeyField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional output field prefix.
    /// </summary>
    public string? OutputFieldPrefix { get; set; }

    /// <summary>
    /// Gets or sets the field in the lookup source to bring across as this row's output column.
    /// </summary>
    public string LookupValueField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the join type, resolved against <see cref="LookupJoinTypes"/>.
    /// </summary>
    [ValuesFrom(typeof(LookupJoinTypes))]
    public string JoinType { get; set; } = string.Empty;
}

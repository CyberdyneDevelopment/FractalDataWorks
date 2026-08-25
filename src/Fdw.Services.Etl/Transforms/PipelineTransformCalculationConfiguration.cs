using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Configuration for a single computed column within a Calculate transform. Cascade child of
/// <see cref="PipelineTransformConfiguration"/> (pipe.CalculationOperationConfiguration).
/// </summary>
/// <remarks>
/// Why: implements <see cref="IGenericConfiguration"/> (mirroring <see cref="PipelineTransformFieldMappingConfiguration"/>)
/// so the configuration cascade-save discovers and persists this collection when the parent transform is
/// saved. Without this interface the Calculations collection would be silently skipped.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Pipeline")]
public sealed partial class PipelineTransformCalculationConfiguration : IGenericConfiguration
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
    /// Gets or sets the logical FK to the parent transform (column pipe.CalculationOperationConfiguration.PipelineTransformId).
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
    /// Gets or sets the output field name.
    /// </summary>
    public string OutputField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the calculation expression.
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution order within the calculation.
    /// </summary>
    public int ExecutionOrder { get; set; }
}

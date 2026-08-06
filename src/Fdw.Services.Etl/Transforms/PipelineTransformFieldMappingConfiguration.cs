using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Configuration for a field mapping within a Map transform.
/// </summary>
/// <remarks>
/// Why: implements <see cref="IGenericConfiguration"/> (in addition to <see cref="IFieldMapping"/>) so the
/// configuration cascade-save discovers and persists field-mapping rows when an operation
/// (<see cref="PipelineTransformConfiguration"/>) is saved. The cascade walks IGenericConfiguration child
/// collections; without this interface the FieldMappings collection would be silently skipped.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Pipeline")]
public sealed partial class PipelineTransformFieldMappingConfiguration : IFieldMapping, IGenericConfiguration
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
    /// Gets or sets the logical FK to the parent operation (column pipe.PipelineTransformFieldMapping.PipelineTransformId).
    /// </summary>
    /// <remarks>
    /// Why: set by the configuration cascade-save to the parent operation's logical Id; the configuration
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
    /// Gets or sets the source field name.
    /// </summary>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination field name.
    /// </summary>
    public string DestinationField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional transform expression.
    /// </summary>
    public string? TransformExpression { get; set; }

    /// <summary>
    /// Gets or sets the default value when source is null.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the target data type.
    /// </summary>
    public string? TargetType { get; set; }

    /// <summary>
    /// Gets or sets whether this mapping is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets whether this mapping is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

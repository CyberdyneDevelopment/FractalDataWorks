using System;
using System.Collections.Generic;
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
    /// Gets or sets the source field name.
    /// </summary>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination field name.
    /// </summary>
    public string DestinationField { get; set; } = string.Empty;

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

    /// <summary>
    /// Gets or sets the transformer chain of this mapping: the named Transformations its value goes through, in
    /// <see cref="PipelineTransformFieldMappingTransformationConfiguration.Ordinal"/> order. Cascade child
    /// (pipe.PipelineTransformFieldMappingTransformation). Empty when the mapping states none.
    /// </summary>
    /// <remarks>
    /// Why this holds ids and not runnable steps: a step names a Transformation by id, and turning that id into a transformation
    /// type and its parameters needs the Transformation domain, which a configuration row cannot reach. The run resolves the
    /// chain when it starts (<see cref="FieldTransformChains"/>) and hands the result to the Map stage through its context.
    /// </remarks>
    public IList<PipelineTransformFieldMappingTransformationConfiguration> TransformationChain { get; set; } = [];
}

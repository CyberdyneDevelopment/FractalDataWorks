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

    /// <summary>
    /// Gets or sets the transformer chain of this mapping: the named Transformations its value goes through, in
    /// <see cref="PipelineTransformFieldMappingTransformationConfiguration.Ordinal"/> order. Cascade child
    /// (pipe.PipelineTransformFieldMappingTransformation). Empty when the mapping states none.
    /// </summary>
    /// <remarks>
    /// Why this is not <see cref="Transforms"/>: a step names a Transformation by id, and turning that id into a transformation
    /// type and its parameters needs the Transformation domain, which a configuration row cannot reach. The run resolves the
    /// chain when it starts (<see cref="FieldTransformChains"/>) and hands the result to the Map stage through its context.
    /// </remarks>
    public IList<PipelineTransformFieldMappingTransformationConfiguration> TransformationChain { get; set; } = [];

    /// <inheritdoc/>
    /// <remarks>
    /// Why this projects <see cref="TransformExpression"/> instead of reading its own rows: a pipeline
    /// field mapping stores ONE transform name and no parameter values, while a dataset field mapping
    /// stores an ordered chain with parameters. This reports what is actually stored - a one-step chain
    /// when a name is set, and no chain when it is not. It never invents parameters: a transform that
    /// needs them and is reached from here is reported as an error rather than run unconfigured, which
    /// is the behaviour this replaces. A mapping that states a chain of named Transformations
    /// (<see cref="TransformationChain"/>) is run from the chain the run resolved, not from this projection.
    /// </remarks>
    public IReadOnlyList<IFieldMappingTransform> Transforms =>
        string.IsNullOrEmpty(TransformExpression)
            ? []
            : [new FieldMappingTransform(TransformExpression, 0)];
}

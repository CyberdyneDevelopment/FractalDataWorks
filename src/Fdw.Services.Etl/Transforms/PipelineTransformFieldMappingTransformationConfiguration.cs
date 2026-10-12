using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// One step of the transformer chain of a field mapping in a Map stage: the named Transformation it applies and its
/// position. Cascade child of <see cref="PipelineTransformFieldMappingConfiguration"/>
/// (pipe.PipelineTransformFieldMappingTransformation), ordered by <see cref="Ordinal"/>: the mapped value goes through the
/// steps in that order, before the TargetType conversion.
/// </summary>
/// <remarks>
/// Why implements <see cref="IGenericConfiguration"/> (mirroring <see cref="PipelineTransformCheckConfiguration"/>): the
/// configuration cascade-save discovers and persists this collection when the field mapping is saved, and the cascade read
/// resolves it through its ConfigurationCommands option (see PipelineTransformFieldMappingTransformationConfigurationCommand).
/// Why only keys and order (a factless fact, like transform.FieldMappingTransformation): what the transformation does, and
/// its parameters, live on the Transformation itself. A step whose Transformation no longer resolves fails the run naming it.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineTransformFieldMappingTransformationConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the id of the Transformation (the transform.Transformation domain) this step applies.
    /// </summary>
    public Guid TransformationId { get; set; }

    /// <summary>
    /// Gets or sets the position among the steps of the same field mapping, counted from 1.
    /// </summary>
    public int Ordinal { get; set; }
}

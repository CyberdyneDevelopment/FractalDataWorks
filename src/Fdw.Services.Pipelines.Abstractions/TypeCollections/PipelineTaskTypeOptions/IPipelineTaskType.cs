using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Services.Transformations.Abstractions;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineTaskTypeOptions;

/// <summary>
/// Represents a pipeline task category type that defines the role a task node plays in the
/// designer canvas and at runtime.
/// </summary>
/// <remarks>
/// Each task type carries a <see cref="ConfigurationFields"/> list that drives the properties
/// panel in the builder. For Source/Destination tasks the relevant fields come instead from the
/// bound connection's <c>IDataQueryType.ConfigurationFields</c>; for Transform tasks they come
/// from the bound <c>ITransformationType.ConfigurationFields</c>. Use <see cref="ConfigurationFields"/>
/// only for task-type-specific fields that are not supplied by the connection or transformation.
/// </remarks>
public interface IPipelineTaskType : ITypeOption<int, PipelineTaskTypeBase>
{
    /// <summary>
    /// Gets the configuration-field descriptors this task type contributes to the properties
    /// panel. Empty list means the task relies entirely on its bound connection's or
    /// transformation's fields.
    /// </summary>
    IReadOnlyList<TransformFieldDescriptor> ConfigurationFields { get; }
}

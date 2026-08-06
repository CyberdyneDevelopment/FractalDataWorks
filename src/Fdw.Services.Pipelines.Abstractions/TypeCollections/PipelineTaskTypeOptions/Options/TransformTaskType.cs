using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineTaskTypeOptions.Options;

/// <summary>
/// Pipeline task type for in-flight data transformation.
/// </summary>
/// <remarks>
/// Transform tasks delegate their configuration fields to the bound
/// <c>ITransformationType.ConfigurationFields</c>. No task-type-level fields are declared here.
/// </remarks>
[TypeOption(typeof(PipelineTaskTypes), "Transform")]
[ExcludeFromCodeCoverage]
public sealed class TransformTaskType : PipelineTaskTypeBase
{
    /// <summary>Initializes a new instance of <see cref="TransformTaskType"/>.</summary>
    public TransformTaskType()
        : base(id: 3, name: "Transform")
    {
    }
}

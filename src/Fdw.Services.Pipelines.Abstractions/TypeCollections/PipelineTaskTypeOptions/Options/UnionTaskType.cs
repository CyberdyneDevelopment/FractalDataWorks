using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineTaskTypeOptions.Options;

/// <summary>
/// Pipeline task type for merging multiple upstream data streams into one.
/// </summary>
/// <remarks>
/// Wave 0a: no configuration fields declared. Fields will be added in a later wave.
/// </remarks>
[TypeOption(typeof(PipelineTaskTypes), "Union")]
[ExcludeFromCodeCoverage]
public sealed class UnionTaskType : PipelineTaskTypeBase
{
    /// <summary>Initializes a new instance of <see cref="UnionTaskType"/>.</summary>
    public UnionTaskType()
        : base(id: 6, name: "Union")
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineTaskTypeOptions.Options;

/// <summary>
/// Pipeline task type for iterating over a collection of items.
/// </summary>
/// <remarks>
/// Wave 0a: no configuration fields declared. Fields will be added in a later wave.
/// </remarks>
[TypeOption(typeof(PipelineTaskTypes), "Loop")]
[ExcludeFromCodeCoverage]
public sealed class LoopTaskType : PipelineTaskTypeBase
{
    /// <summary>Initializes a new instance of <see cref="LoopTaskType"/>.</summary>
    public LoopTaskType()
        : base(id: 7, name: "Loop")
    {
    }
}

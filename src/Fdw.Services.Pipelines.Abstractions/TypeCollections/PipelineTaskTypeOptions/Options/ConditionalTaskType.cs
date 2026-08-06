using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineTaskTypeOptions.Options;

/// <summary>
/// Pipeline task type for branching the data flow based on a condition.
/// </summary>
/// <remarks>
/// Wave 0a: no configuration fields declared. Fields will be added in a later wave
/// once the conditional expression model is defined.
/// </remarks>
[TypeOption(typeof(PipelineTaskTypes), "Conditional")]
[ExcludeFromCodeCoverage]
public sealed class ConditionalTaskType : PipelineTaskTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ConditionalTaskType"/>.</summary>
    public ConditionalTaskType()
        : base(id: 5, name: "Conditional")
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineTaskTypeOptions.Options;

/// <summary>
/// Pipeline task type for catching and handling errors from upstream tasks.
/// </summary>
/// <remarks>
/// Wave 0a: no configuration fields declared. Fields will be added in a later wave.
/// </remarks>
[TypeOption(typeof(PipelineTaskTypes), "ErrorHandler")]
[ExcludeFromCodeCoverage]
public sealed class ErrorHandlerTaskType : PipelineTaskTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ErrorHandlerTaskType"/>.</summary>
    public ErrorHandlerTaskType()
        : base(id: 8, name: "ErrorHandler")
    {
    }
}

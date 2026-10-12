using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// The pipeline types a stage kind can run on (FDW-1120 phase 4).
/// </summary>
/// <remarks>
/// The rule is data: it is read from the kind, not written into the validator or the console, and it is listed in
/// <c>/type-collections/TransformTypes/values</c> as <c>pipelineTypes</c> so a form can stop offering a kind on a pipeline type
/// that cannot run it. A stage of a kind that lists pipeline types is refused when it is saved on a pipeline of any other type.
/// </remarks>
public interface IStagePipelineTypesShape
{
    /// <summary>
    /// Gets the names of the pipeline types (as a pipeline's <c>pipelineType</c> states them, for example BatchCopy) the kind
    /// runs on, or null when it runs on every pipeline type.
    /// </summary>
    IReadOnlyList<string>? PipelineTypes { get; }
}

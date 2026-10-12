using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// One path of a Branch stage in a create or update request: a name, the condition that routes a record to it, and the stages
/// a routed record passes through (FDW-1120).
/// </summary>
public class CreatePipelinePathRequest : ITransformPathSpec
{
    /// <summary>Gets or sets the path's name, unique within its Branch and free of '.'.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the condition a record must meet to take this path, in the pipeline's condition language.</summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>Gets or sets the stages of the path, in order; null or empty passes a routed record through.</summary>
    public IList<CreatePipelineTransformRequest>? Stages { get; set; }

    IReadOnlyList<ITransformOperationSpec> ITransformPathSpec.Stages => Stages is null ? [] : [.. Stages.Cast<ITransformOperationSpec>()];
}

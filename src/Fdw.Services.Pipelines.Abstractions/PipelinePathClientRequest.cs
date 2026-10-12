using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// One path of a Branch stage as the client contract states it: a name, the condition that routes a record to it, and the
/// stages a routed record passes through (FDW-1120).
/// </summary>
public sealed class PipelinePathClientRequest
{
    /// <summary>Gets or sets the path's name, unique within its Branch and free of '.'.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the condition a record must meet to take this path, in the pipeline's condition language.</summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>Gets or sets the stages of the path, in order; null or empty passes a routed record through.</summary>
    public IList<PipelineTransformClientRequest>? Stages { get; set; }
}

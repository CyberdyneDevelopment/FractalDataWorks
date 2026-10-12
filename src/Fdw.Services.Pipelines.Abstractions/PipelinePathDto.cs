using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// One path of a Branch stage as a pipeline's detail reads it back: its name, its condition and its stages, in the order they run.
/// </summary>
public class PipelinePathDto
{
    /// <summary>Gets or sets the path's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the condition a record must meet to take the path.</summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>Gets or sets the stages of the path, in execution order; empty when the path passes its records through.</summary>
    public IList<PipelineTransformDto> Stages { get; set; } = [];
}

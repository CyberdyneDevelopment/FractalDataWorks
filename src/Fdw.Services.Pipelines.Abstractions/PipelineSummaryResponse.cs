using System;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Summary information for a pipeline, used in list views.
/// </summary>
public class PipelineSummaryResponse
{
    /// <summary>
    /// Gets or sets the pipeline's durable logical identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline type (e.g., "BatchCopy", "Streaming").
    /// </summary>
    public string PipelineType { get; set; } = string.Empty;
}

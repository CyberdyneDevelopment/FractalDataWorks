using System;
using System.Collections.Generic;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Hubs;

/// <summary>
/// The components of one pipeline run in flight as the live view lists them (FDW-3778): each component's stage, start, end,
/// success and row count. Counts only; no row values are ever carried.
/// </summary>
public sealed class PipelineComponentsUpdate
{
    /// <summary>Gets or sets the pipeline name.</summary>
    public required string PipelineName { get; set; }

    /// <summary>Gets or sets the execution ID.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the run's components, in the order the live view lists them.</summary>
    public List<PipelineRunComponentResponse> Components { get; set; } = [];

    /// <summary>Gets or sets what every link between the components has carried so far.</summary>
    public List<PipelineRunLinkResponse> Links { get; set; } = [];

    /// <summary>Gets or sets when the list was read.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

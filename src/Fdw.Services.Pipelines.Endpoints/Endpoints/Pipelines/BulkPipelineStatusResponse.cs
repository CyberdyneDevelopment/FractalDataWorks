using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Response for bulk pipeline status.
/// </summary>
public class BulkPipelineStatusResponse
{
    /// <summary>
    /// Gets or sets the pipeline statuses.
    /// </summary>
    public IList<PipelineStatusInfo> Pipelines { get; set; } = [];
}

using System;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Summary DTO for a pipeline, used in list views.
/// </summary>
public class PipelineSummaryResponse : ResourceSummary
{
    /// <summary>Gets or sets the pipeline unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the pipeline type name (e.g., "BatchCopy", "Streaming").</summary>
    public required string PipelineType { get; set; }
}

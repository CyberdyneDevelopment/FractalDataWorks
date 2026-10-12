using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Summary DTO for a pipeline, used in list views.
/// </summary>
public class PipelineSummaryResponse
{
    /// <summary>Gets or sets the pipeline unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the pipeline name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the pipeline type name (e.g., "BatchCopy", "Streaming").</summary>
    public required string PipelineType { get; set; }
}

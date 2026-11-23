using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>The request that names a pipeline whose offers are listed (FDW-6135).</summary>
public sealed class PipelineOffersRequest
{
    /// <summary>Gets or sets the pipeline.</summary>
    public Guid PipelineId { get; set; }
}

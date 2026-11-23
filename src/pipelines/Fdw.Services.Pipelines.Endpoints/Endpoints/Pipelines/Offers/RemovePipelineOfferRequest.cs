using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>The request that removes one offer of a pipeline (FDW-6135).</summary>
public sealed class RemovePipelineOfferRequest
{
    /// <summary>Gets or sets the pipeline.</summary>
    public Guid PipelineId { get; set; }

    /// <summary>Gets or sets the offer.</summary>
    public Guid OfferId { get; set; }
}

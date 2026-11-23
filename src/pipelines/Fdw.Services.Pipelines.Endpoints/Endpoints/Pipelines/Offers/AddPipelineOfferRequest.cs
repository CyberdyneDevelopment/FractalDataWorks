using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>The request that offers a pipeline (FDW-6135): to the whole tenant when it names no person and no role, to one person, or to one role; never to both.</summary>
public sealed class AddPipelineOfferRequest
{
    /// <summary>Gets or sets the pipeline.</summary>
    public Guid PipelineId { get; set; }

    /// <summary>Gets or sets the person it is offered to.</summary>
    public Guid? PersonId { get; set; }

    /// <summary>Gets or sets the role whose holders it is offered to.</summary>
    public string? RoleName { get; set; }
}

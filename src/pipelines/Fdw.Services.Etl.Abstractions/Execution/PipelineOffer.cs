using System;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>One offer of a pipeline (FDW-6135): to the whole tenant (no person and no role), to one person, or to the holders of one role.</summary>
public sealed class PipelineOffer
{
    /// <summary>Gets or sets the offer's id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the pipeline offered.</summary>
    public Guid PipelineId { get; set; }

    /// <summary>Gets or sets the person it is offered to; empty with the role empty means the whole tenant.</summary>
    public Guid? PersonId { get; set; }

    /// <summary>Gets or sets the role whose holders it is offered to.</summary>
    public string? RoleName { get; set; }
}

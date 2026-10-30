using System;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// One pipeline run in the ETL host's queue: in flight, or waiting for its turn (FDW-3097).
/// </summary>
public class PipelineRunQueueItemResponse
{
    /// <summary>Gets or sets the execution record of the run.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the pipeline the run is of.</summary>
    public Guid PipelineId { get; set; }

    /// <summary>Gets or sets the pipeline's name.</summary>
    public required string PipelineName { get; set; }

    /// <summary>Gets or sets the tenant the run belongs to, or <see langword="null"/> for a run that belongs to none.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets when the host accepted the run.</summary>
    public DateTimeOffset QueuedAt { get; set; }

    /// <summary>Gets or sets when the run started, or <see langword="null"/> for a run that has not.</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>Gets or sets the name of the <c>RunWaitReasons</c> option that says why a waiting run cannot start now, or <see langword="null"/> for a run in flight.</summary>
    public string? WaitReason { get; set; }

    /// <summary>Gets or sets the limit that holds a run back, for a server, tenant or pipeline limit.</summary>
    public int? Limit { get; set; }

    /// <summary>Gets or sets the run that holds this one back, when one does. A caller who does not administer the platform is told of a run of their own tenant only.</summary>
    public Guid? BlockedByExecutionId { get; set; }

    /// <summary>Gets or sets the table both runs write, for a shared table held back by a run the caller is told of.</summary>
    public string? Table { get; set; }

    /// <summary>Gets or sets when a held run joins the queue, for a run its pipeline holds past its limit until a set time (FDW-3129).</summary>
    public DateTimeOffset? HeldUntil { get; set; }
}

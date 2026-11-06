using System.Collections.Generic;
using System;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Request to trigger a pipeline job.
/// </summary>
public class TriggerPipelineRequest
{
    /// <summary>
    /// Gets or sets the name of the item (pipeline) to execute. Leave it empty when <see cref="Id"/> names the
    /// pipeline; a request names it one way, not both.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical id of the pipeline to execute (FDW-1088). A schedule relays the id it stores, so
    /// the run resolves exactly that pipeline and never another tenant's of the same name.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the trigger source identifier.
    /// </summary>
    public string? TriggerSource { get; set; }

    /// <summary>
    /// Gets or sets the key of one fire (FDW-4317): the schedule's Id and the time the fire was due, the same on every retry of that fire, so the ETL host answers a retry with the run the
    /// first call started. Required when the trigger source is one that requires it (a scheduled fire); a source that does not must send none.
    /// </summary>
    public string? TriggerKey { get; set; }

    /// <summary>
    /// Gets or sets the time the fire was due (FDW-4317), the time <see cref="TriggerKey"/> names. The ETL host remembers a key only for the retention its setting gives and
    /// refuses a fire due before that. Sent with the key.
    /// </summary>
    public DateTimeOffset? TriggerKeyFireTime { get; set; }

    /// <summary>
    /// Gets or sets the schedule name that triggered this execution.
    /// </summary>
    public string? ScheduleName { get; set; }

    /// <summary>
    /// Gets or sets the Id of the schedule that triggered this execution (FDW-3975): the schedule's domain Id, the one the thresholds attached to the schedule name it by.
    /// The scheduler reads the schedule before it triggers, so it has the Id in hand. Null for an execution no schedule started.
    /// </summary>
    public Guid? ScheduleId { get; set; }

    /// <summary>
    /// Gets or sets the tenant the triggering schedule belongs to, if known
    /// (<c>ScheduleConfiguration.TenantId</c>). Relayed to the ETL server so the dispatched execution's
    /// RLS SESSION_CONTEXT is scoped correctly for background runs with no caller ClaimsPrincipal.
    /// </summary>
    /// <remarks>
    /// Why the receiving endpoint must not trust this blindly for every caller: an authenticated
    /// per-tenant caller's own token claim always wins over this field (see
    /// <c>UnifiedTriggerEndpoint.TriggerPipeline</c>) — this value is a legitimate relay path only for
    /// callers whose own token carries no tenant scope (e.g. the scheduler's service-account credential,
    /// which spans every tenant's schedules).
    /// </remarks>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the roles the run acts with (FDW-1161), by name. Required: a run names at least one, only roles the caller holds
    /// in the tenant it acts in, and the roles must be able to reach everything the pipeline reads and writes. A schedule relays the
    /// roles its creator chose; a pipeline carries none of its own.
    /// </summary>
    public IList<string>? RunRoles { get; set; }

    /// <summary>
    /// Gets or sets the run parameter values (contract item 3): each name must be a parameter the pipeline declares, each
    /// required parameter must be given, and each value is parsed by the declared type.
    /// </summary>
    public IList<PipelineParameterValue>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets how many rows each source may read in a test run (FDW-1676). When set, the run is a test run: each source stops
    /// after this many rows, no destination is written and no step with an outside effect runs. When null, the run is a real run.
    /// </summary>
    public int? TestRowsPerSource { get; set; }

    /// <summary>
    /// Gets or sets how long the run may take once it starts running, in seconds (FDW-3805): the triggering schedule's stated timeout. When the time has passed the run is
    /// cancelled and ends with the result code TimedOut. Null means the caller states no time limit and the run has none; a number stated must be positive.
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

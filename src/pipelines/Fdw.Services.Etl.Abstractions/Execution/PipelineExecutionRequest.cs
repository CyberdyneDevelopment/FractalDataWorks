using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Work item enqueued by endpoints and dequeued by the background execution service.
/// </summary>
/// <remarks>
/// Why a class with <c>required</c> properties instead of a record: the request is a mutable
/// work item where the execution ID comes from IExecutionTracker before being enqueued.
/// <c>required</c> enforces construction correctness without a constructor with many parameters.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class PipelineExecutionRequest
{
    /// <summary>
    /// Gets the execution tracking ID created by IExecutionTracker before enqueue.
    /// </summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Gets the id of the pipeline to execute: the pipeline the trigger resolved in its caller's view.
    /// </summary>
    /// <remarks>
    /// Why the id and not the name: a pipeline name is unique only within a tenant, and the worker reads
    /// with no caller of its own, so a name could resolve to another tenant's pipeline. The id names the
    /// one pipeline the trigger was allowed to see (FDW-1087).
    /// </remarks>
    public required Guid PipelineId { get; init; }

    /// <summary>
    /// Gets the name of the pipeline to execute, for logs, status broadcasts and execution records.
    /// The worker never resolves the pipeline by it.
    /// </summary>
    public required string PipelineName { get; init; }

    /// <summary>
    /// Gets the source that triggered this execution (e.g., "Api", "Scheduler").
    /// </summary>
    public required string TriggerSource { get; init; }

    /// <summary>
    /// Gets the name of the schedule that triggered this execution.
    /// Null for manual/ad-hoc executions.
    /// </summary>
    public string? ScheduleName { get; init; }

    /// <summary>
    /// Gets the Id of the schedule that triggered this execution (FDW-3975): the schedule's domain Id, which the thresholds attached to the schedule are found by.
    /// Null for manual/ad-hoc executions. A scheduled execution carries both the name and the Id.
    /// </summary>
    public Guid? ScheduleId { get; init; }

    /// <summary>
    /// Gets the tenant this execution belongs to, if known. Sourced from
    /// <c>ScheduleConfiguration.TenantId</c> for scheduled triggers, or from the triggering caller's own
    /// <c>IAuthenticationContext.ActiveTenantId</c> for API-triggered executions. Null when no tenant
    /// scope applies (e.g. a system-wide, cross-tenant, or single-tenant deployment).
    /// </summary>
    /// <remarks>
    /// Why this must ride on the request (not be resolved later): the background executor that
    /// dequeues this request runs on a completely separate async flow from whatever enqueued it (an
    /// HTTP request handler, or the scheduler's dispatch loop) — no ambient/AsyncLocal context spans
    /// that boundary, nor should it. The execution's tenant scope must be carried explicitly as data.
    /// </remarks>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Gets the roles this run acts with (FDW-1161): named by whoever started it -- the caller of a manual trigger, the creator
    /// of a schedule -- and judged, when the run was accepted, for what the pipeline reads and writes.
    /// </summary>
    /// <remarks>
    /// Why on the request and not recorded on the pipeline: the roles are a property of the run, not of the pipeline. The same
    /// pipeline run by two people, or by a schedule, can act with different roles, and a pipeline carries none of its own.
    /// </remarks>
    public required IReadOnlyList<string> RunRoles { get; init; }

    /// <summary>
    /// Gets the run parameters of this execution, by name, already read as the type the pipeline declares for each (the
    /// trigger and the schedule resolve them through <c>PipelineRunParameterValues.Resolve</c>, which refuses an undeclared
    /// name, a missing required one and an unreadable value). Null when none were supplied.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }

    /// <summary>
    /// Gets how many rows each source may read when this execution is a test run (FDW-1676). When set, the run reads at most this many
    /// rows from each source and writes nothing; null means a real run.
    /// </summary>
    public int? TestRowsPerSource { get; init; }

    /// <summary>
    /// Gets how long this execution may take once it starts running, in seconds (FDW-3805): the timeout the triggering schedule states. The host cancels the run when the
    /// time has passed, and it ends with the result code TimedOut. Null means no time limit is stated and the run has none.
    /// </summary>
    public int? TimeoutSeconds { get; init; }

    /// <summary>
    /// The longest timeout a request may state, in seconds, about 49 days: the longest delay the timer that stops the run can be given. A longer time is refused where
    /// the request is accepted, naming this limit, and never cut short.
    /// </summary>
    public const int MaxTimeoutSeconds = 4_294_967;

    /// <summary>
    /// Gets when a run that was held back (DelayedStart) is due, for a run the host takes up again after a restart (FDW-3216); none for a run that is
    /// not recovered, whose hold the scheduler works out itself.
    /// </summary>
    public DateTimeOffset? HeldUntil { get; init; }
}

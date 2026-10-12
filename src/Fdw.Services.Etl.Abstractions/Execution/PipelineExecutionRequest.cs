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
}

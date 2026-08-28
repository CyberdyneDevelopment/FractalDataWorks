using System;

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
    /// Gets the name of the pipeline to execute.
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
}

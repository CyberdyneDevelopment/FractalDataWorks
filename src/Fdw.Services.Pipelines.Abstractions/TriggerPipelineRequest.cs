using System;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Request to trigger a pipeline job.
/// </summary>
public class TriggerPipelineRequest
{
    /// <summary>
    /// Gets or sets the name of the item (pipeline) to execute.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trigger source identifier.
    /// </summary>
    public string? TriggerSource { get; set; }

    /// <summary>
    /// Gets or sets the schedule name that triggered this execution.
    /// </summary>
    public string? ScheduleName { get; set; }

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
}

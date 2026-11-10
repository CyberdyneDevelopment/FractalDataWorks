using System;
using System.Collections.Generic;

namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>
/// Represents an escalation policy configuration.
/// </summary>
public interface IEscalationPolicy
{
    /// <summary>
    /// Gets the unique identifier for this policy.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the policy name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether this policy is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the workflow ID this policy is scoped to, if any.
    /// </summary>
    Guid? WorkflowId { get; }

    /// <summary>
    /// Gets the schedule ID this policy is scoped to, if any (FDW-4814).
    /// </summary>
    Guid? ScheduleId { get; }

    /// <summary>
    /// Gets the maximum escalation level.
    /// </summary>
    int MaxEscalationLevel { get; }

    /// <summary>
    /// Gets the cooldown period in minutes between escalations.
    /// </summary>
    int CooldownMinutes { get; }

    /// <summary>
    /// Gets how many times a failed escalation notification is retried. The caller states it; there is no default (FDW-3823).
    /// </summary>
    int MaxRetries { get; }

    /// <summary>
    /// Gets the seconds between retries of a failed escalation notification. The caller states it; there is no default (FDW-3823).
    /// </summary>
    int RetryDelaySeconds { get; }

    /// <summary>
    /// Gets the escalation levels.
    /// </summary>
    IReadOnlyList<IEscalationLevel> Levels { get; }
}

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
    /// Gets the maximum escalation level.
    /// </summary>
    int MaxEscalationLevel { get; }

    /// <summary>
    /// Gets the cooldown period in minutes between escalations.
    /// </summary>
    int CooldownMinutes { get; }

    /// <summary>
    /// Gets the escalation levels.
    /// </summary>
    IReadOnlyList<IEscalationLevel> Levels { get; }
}

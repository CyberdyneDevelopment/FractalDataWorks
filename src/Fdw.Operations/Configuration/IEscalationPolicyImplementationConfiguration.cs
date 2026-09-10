using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Operations.Configuration;

/// <summary>The contract every EscalationPolicy implementation carries.</summary>
public interface IEscalationPolicyImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid EscalationPolicyId { get; set; }

    /// <summary>Gets or sets the EscalationPolicy Description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the EscalationPolicy IsEnabled.</summary>
    bool IsEnabled { get; set; }

    /// <summary>Gets or sets the EscalationPolicy ItemType.</summary>
    string? ItemType { get; set; }

    /// <summary>Gets or sets the EscalationPolicy WorkflowId.</summary>
    Guid? WorkflowId { get; set; }

    /// <summary>Gets or sets the EscalationPolicy ScheduleId.</summary>
    Guid? ScheduleId { get; set; }

    /// <summary>Gets or sets the EscalationPolicy MaxEscalationLevel.</summary>
    int MaxEscalationLevel { get; set; }

    /// <summary>Gets or sets the EscalationPolicy CooldownMinutes.</summary>
    int CooldownMinutes { get; set; }

    /// <summary>Gets or sets the EscalationPolicy ConditionExpression.</summary>
    string? ConditionExpression { get; set; }

    /// <summary>Gets or sets the EscalationPolicy MaxRetries.</summary>
    int MaxRetries { get; set; }

    /// <summary>Gets or sets the EscalationPolicy RetryDelaySeconds.</summary>
    int RetryDelaySeconds { get; set; }

    /// <summary>Gets or sets the EscalationPolicy Levels.</summary>
    IList<EscalationLevelConfiguration> Levels { get; set; }
}

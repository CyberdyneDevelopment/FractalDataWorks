using Fdw.Configuration;
using Fdw.Configuration.Abstractions;
using Fdw.Data;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>The contract every NotificationRule implementation carries.</summary>
public interface INotificationRuleImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid NotificationRuleId { get; set; }

    /// <summary>Gets or sets the NotificationRule Description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the NotificationRule IsEnabled.</summary>
    bool IsEnabled { get; set; }

    /// <summary>Gets or sets the NotificationRule ScheduleId.</summary>
    Guid? ScheduleId { get; set; }

    /// <summary>Gets or sets the NotificationRule PipelineId.</summary>
    Guid? PipelineId { get; set; }

    /// <summary>Gets or sets the NotificationRule WorkflowId.</summary>
    Guid? WorkflowId { get; set; }

    /// <summary>Gets or sets the NotificationRule ConditionOperator.</summary>
    string ConditionOperator { get; set; }

    /// <summary>Gets or sets the NotificationRule NotificationServiceType.</summary>
    string NotificationServiceType { get; set; }

    /// <summary>Gets or sets the NotificationRule NotificationServiceName.</summary>
    string NotificationServiceName { get; set; }

    /// <summary>Gets or sets the NotificationRule Template.</summary>
    string? Template { get; set; }

    /// <summary>Gets or sets the NotificationRule Severity.</summary>
    string Severity { get; set; }

    /// <summary>Gets or sets the NotificationRule CooldownMinutes.</summary>
    int? CooldownMinutes { get; set; }

    /// <summary>Gets or sets the NotificationRule LastNotificationSent.</summary>
    DateTimeOffset? LastNotificationSent { get; set; }
}

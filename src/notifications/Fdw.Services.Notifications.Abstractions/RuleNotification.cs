using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// One notification to send through a named notification rule: the rule chooses the channel and severity, the caller
/// supplies the words and the scope the rule is matched against.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RuleNotification
{
    /// <summary>Gets the name of the notification rule to send through.</summary>
    public required string RuleName { get; init; }

    /// <summary>Gets the subject line the rule's channel carries.</summary>
    public required string Subject { get; init; }

    /// <summary>Gets the message body, sent exactly as given.</summary>
    public required string Message { get; init; }

    /// <summary>Gets the pipeline the notification is about, matched against the rule's pipeline scope; null when none.</summary>
    public Guid? PipelineId { get; init; }

    /// <summary>Gets the schedule the notification is about, matched against the rule's schedule scope; null when none.</summary>
    public Guid? ScheduleId { get; init; }

    /// <summary>Gets the workflow the notification is about, matched against the rule's workflow scope; null when none.</summary>
    public Guid? WorkflowId { get; init; }

    /// <summary>Gets the metadata the channel carries with the notification.</summary>
    public required IReadOnlyDictionary<string, object?> Metadata { get; init; }

    /// <summary>Gets the correlation id the channel carries; null when the notification has none.</summary>
    public string? CorrelationId { get; init; }
}

using System.Collections.Generic;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Workflow notification settings.
/// </summary>
public interface IWorkflowNotificationSettings
{
    /// <summary>
    /// Gets whether to notify on workflow start.
    /// </summary>
    bool NotifyOnStart { get; }

    /// <summary>
    /// Gets whether to notify on workflow completion.
    /// </summary>
    bool NotifyOnCompletion { get; }

    /// <summary>
    /// Gets whether to notify on workflow failure.
    /// </summary>
    bool NotifyOnFailure { get; }

    /// <summary>
    /// Gets the notification channels.
    /// </summary>
    IReadOnlyList<string> Channels { get; }
}
using System;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// An activity item for the recent activity feed.
/// </summary>
public interface IActivityItem
{
    /// <summary>
    /// Gets the activity timestamp.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Gets the activity type (e.g., "ConfigChanged", "PipelineRun", "Error").
    /// </summary>
    string ActivityType { get; }

    /// <summary>
    /// Gets the activity message.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the severity level.
    /// </summary>
    IActivitySeverity Severity { get; }

    /// <summary>
    /// Gets the icon for this activity type.
    /// </summary>
    string? Icon { get; }

    /// <summary>
    /// Gets the user who performed the action (if applicable).
    /// </summary>
    string? User { get; }

    /// <summary>
    /// Gets the navigation target for more details.
    /// </summary>
    string? NavigationTarget { get; }
}
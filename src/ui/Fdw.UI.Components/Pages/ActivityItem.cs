using System;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of an activity item.
/// </summary>
public sealed class ActivityItem : IActivityItem
{
    /// <inheritdoc />
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public string ActivityType { get; set; } = "";

    /// <inheritdoc />
    public string Message { get; set; } = "";

    /// <inheritdoc />
    public IActivitySeverity Severity { get; set; } = ActivitySeverities.Info;

    /// <inheritdoc />
    public string? Icon { get; set; }

    /// <inheritdoc />
    public string? User { get; set; }

    /// <inheritdoc />
    public string? NavigationTarget { get; set; }

    /// <summary>
    /// Creates an info activity.
    /// </summary>
    public static ActivityItem Info(string message, string? user = null) =>
        new() { ActivityType = "Info", Message = message, Severity = ActivitySeverities.Info, User = user };

    /// <summary>
    /// Creates a success activity.
    /// </summary>
    public static ActivityItem Success(string message, string? user = null) =>
        new() { ActivityType = "Success", Message = message, Severity = ActivitySeverities.Success, User = user };

    /// <summary>
    /// Creates a warning activity.
    /// </summary>
    public static ActivityItem Warning(string message, string? user = null) =>
        new() { ActivityType = "Warning", Message = message, Severity = ActivitySeverities.Warning, User = user };

    /// <summary>
    /// Creates an error activity.
    /// </summary>
    public static ActivityItem Error(string message, string? user = null) =>
        new() { ActivityType = "Error", Message = message, Severity = ActivitySeverities.Error, User = user };
}
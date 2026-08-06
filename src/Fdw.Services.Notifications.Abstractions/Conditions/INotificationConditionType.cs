using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Interface for notification condition types with evaluation behavior.
/// </summary>
public interface INotificationConditionType : ITypeOption<int, NotificationConditionTypeBase>
{
    /// <summary>
    /// Gets the MudBlazor icon name for this condition type.
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Gets the MudBlazor color for this condition type.
    /// </summary>
    string Color { get; }

    /// <summary>
    /// Evaluates whether this condition is met.
    /// </summary>
    /// <param name="context">The notification evaluation context.</param>
    /// <returns>Success with true if condition is met, false otherwise.</returns>
    IGenericResult<bool> Evaluate(NotificationContext context);
}

using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Base class for notification condition types.
/// </summary>
public abstract class NotificationConditionTypeBase : TypeOptionBase<NotificationConditionTypeBase>, INotificationConditionType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationConditionTypeBase"/> class.
    /// </summary>
    protected NotificationConditionTypeBase(
        string name,
        string icon,
        string color)
        : base(name)
    {
        Icon = icon;
        Color = color;
    }

    /// <inheritdoc />
    public string Icon { get; }

    /// <inheritdoc />
    public string Color { get; }

    /// <inheritdoc />
    public abstract IGenericResult<bool> Evaluate(NotificationContext context);
}

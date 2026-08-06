using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Base class for notification condition types.
/// </summary>
public abstract class NotificationConditionTypeBase : TypeOptionBase<int, NotificationConditionTypeBase>, INotificationConditionType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationConditionTypeBase"/> class.
    /// </summary>
    protected NotificationConditionTypeBase(
        int id,
        string name,
        string icon,
        string color)
        : base(id, name)
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

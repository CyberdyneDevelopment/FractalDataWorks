using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>The base of the options of <see cref="NotificationPayloadTypes"/>.</summary>
public abstract class NotificationPayloadTypeBase : TypeOptionBase<NotificationPayloadTypeBase>, INotificationPayloadType
{
    /// <summary>Initializes a new instance of the <see cref="NotificationPayloadTypeBase"/> class.</summary>
    /// <param name="name">The payload type's name.</param>
    /// <param name="description">What a payload of the type is.</param>
    /// <param name="fields">The fields it declares.</param>
    protected NotificationPayloadTypeBase(string name, string description, IReadOnlyList<PayloadField> fields)
        : base(name, name, description, nameof(NotificationPayloadTypes))
    {
        Fields = fields;
    }

    /// <inheritdoc />
    public IReadOnlyList<PayloadField> Fields { get; }
}

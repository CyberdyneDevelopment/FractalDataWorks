using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>A type of payload a pipeline may send through a notification rule (FDW-6138): its name and the fields it declares.</summary>
public interface INotificationPayloadType : ITypeOption<int, NotificationPayloadTypeBase>
{
    /// <summary>Gets the fields a payload of this type states.</summary>
    IReadOnlyList<PayloadField> Fields { get; }
}

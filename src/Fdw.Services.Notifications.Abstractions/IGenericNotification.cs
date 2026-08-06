using System;
using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Base interface for notification services in the Fdw framework.
/// All notification implementations must implement this interface.
/// </summary>
public interface IGenericNotification : IDisposable, IServiceOption
{
    /// <summary>
    /// Gets the notification channel this service handles.
    /// </summary>
    INotificationChannel Channel { get; }
}

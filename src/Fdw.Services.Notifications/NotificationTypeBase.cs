using System;
using Fdw.ServiceTypes;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Notifications;

/// <summary>
/// Base class for notification service type definitions.
/// Provides notification-specific metadata and typed provider support.
/// </summary>
/// <typeparam name="TService">The notification service interface type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating service instances.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the service.</typeparam>
public abstract class NotificationTypeBase<TService, TFactory, TConfiguration>
    : ServiceTypeBase<TService, TFactory, TConfiguration>,
      INotificationType
    where TService : IPlatformNotification
    where TFactory : INotificationFactory<TService, TConfiguration>
    where TConfiguration : class, INotificationImplementationConfiguration
{
    private readonly string _channelName;

    /// <summary>
    /// Gets the notification channel this type handles.
    /// </summary>
    public INotificationChannel Channel => NotificationChannels.ByName(_channelName);

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationTypeBase{TService, TFactory, TConfiguration}"/> class.
    /// </summary>
    /// <param name="name">The name of this notification type.</param>
    /// <param name="channelName">The name of the notification channel.</param>
    /// <param name="displayName">The display name for UI.</param>
    /// <param name="description">Description of the notification type.</param>
    /// <param name="defaultContainerName">The default container name for this notification type.</param>
    protected NotificationTypeBase(
        string name,
        string channelName,
        string displayName,
        string description,
        string defaultContainerName = "")
        : base(
            name,
            $"Notifications:{name}",
            displayName,
            description,
            "Notifications",
            defaultDataStoreName: "ConfigurationDb",
            defaultPathName: "notify",
            defaultContainerName: defaultContainerName)
    {
        _channelName = channelName;
    }

}

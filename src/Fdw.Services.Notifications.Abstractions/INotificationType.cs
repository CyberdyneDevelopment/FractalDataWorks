using Fdw.Configuration;
using System;
using System.Collections.Generic;
using Fdw.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Generic interface for notification service type definitions.
/// Defines the contract for notification service type implementations that integrate
/// with the service framework's dependency injection and configuration systems.
/// </summary>
/// <typeparam name="TService">The notification service interface type.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the notification service.</typeparam>
/// <typeparam name="TFactory">The factory type for creating notification service instances.</typeparam>
public interface INotificationType<TService, TConfiguration, TFactory>
    : IServiceType<Guid, TService, TFactory, TConfiguration>, INotificationType
    where TService : IGenericService
    where TConfiguration : IGenericConfiguration
    where TFactory : IServiceFactory<TService, TConfiguration>
{
    // Notification-specific methods and properties inherited from INotificationType
}

/// <summary>
/// Non-generic interface for notification service types.
/// Provides a common base for all notification types regardless of generic parameters.
/// </summary>
public interface INotificationType : IServiceType
{
    /// <summary>
    /// Gets the notification channel this type handles.
    /// </summary>
    INotificationChannel Channel { get; }

    /// <summary>Gets the name a person sees for this type.</summary>
    /// <remarks>Implemented by <c>TypeOptionBase.DisplayName</c>; declared here so callers holding the interface can show it.</remarks>
    string DisplayName { get; }

    /// <summary>Gets what this type is for, in a sentence.</summary>
    /// <remarks>Implemented by <c>TypeOptionBase.Description</c>; declared here so callers holding the interface can show it.</remarks>
    string Description { get; }

    /// <summary>
    /// Gets the settings a notification of this type needs beyond the ones every notification has.
    /// </summary>
    IReadOnlyList<NotificationSettingField> Settings { get; }

    /// <summary>
    /// Makes a configuration of this type with the defaults of the type, for a notification being made.
    /// </summary>
    /// <returns>A new configuration, not yet saved.</returns>
    INotificationImplementationConfiguration NewConfiguration();
}

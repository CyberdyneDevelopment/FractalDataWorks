using Fdw.Configuration;
using System;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// Registers this option's implementation provider factory with the domain provider being
    /// constructed, using the constructing scope's own <paramref name="serviceProvider"/>.
    /// </summary>
    /// <remarks>
    /// Called from <c>NotificationServiceTypes</c>'s own <c>AddScoped</c> factory — once per domain
    /// provider instance, not once at startup — so the closure this hands to the domain
    /// provider's <c>Register</c> always resolves against whichever scope actually constructed
    /// THIS instance (root at startup, a real request's own scope for a real request), never a
    /// reference captured from a different one. The prior approach registered from each option's
    /// <c>Initialize</c>, which only ever runs once against the root container — so every scope
    /// but root's own found this domain's factory registry empty.
    /// </remarks>
    /// <param name="domainProvider">The domain provider instance being constructed.</param>
    /// <param name="serviceProvider">The service provider that constructed it — root at startup, a request's own scope for a request.</param>
    /// <param name="logger">The logger to report the outcome to.</param>
    /// <returns>Success, or a structured failure. The base no-op returns success for options with nothing to contribute.</returns>
    IGenericResult RegisterImplementationProvider(INotificationServiceProvider domainProvider, IServiceProvider serviceProvider, ILogger logger);
}

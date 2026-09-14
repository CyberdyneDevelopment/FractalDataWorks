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
    /// Sets the body this option runs to register itself with the domain service provider and/or
    /// the domain configuration provider, when the caller has one to hand it.
    /// </summary>
    /// <remarks>
    /// An overload of <see cref="ServiceTypeBase{TService,TFactory,TConfiguration}.Registration"/> —
    /// same gerund, same call-and-store shape, an additional signature rather than a new phase or a
    /// new method name. Set from the option's own constructor exactly like its DI-wiring
    /// <c>Registration((builder, loggerFactory) => ...)</c> call; invoked by
    /// <c>NotificationServiceTypes</c>'s own <c>AddScoped</c> factory via <see cref="Register"/>,
    /// once per domain provider instance it constructs — not once at startup — so whichever scope
    /// actually builds an instance (root at startup, a real request's own scope for a real request)
    /// is the same scope whose <c>IServiceProvider</c> the closure resolves against. Registering
    /// from each option's <c>Initialize</c> would run only once against the root container, so
    /// every scope but root's own would find this domain's factory registry empty.
    /// </remarks>
    /// <param name="method">
    /// The body this option runs. Receives the constructing scope's <c>IServiceProvider</c>, the
    /// domain service provider instance being constructed (or null if none is being constructed),
    /// the domain configuration provider (or null if none is available), and the logger the caller
    /// already built (with its own <c>NullLogger</c> fallback already applied) — not re-resolved
    /// here, so every option logs through the same logger instance the domain provider's own
    /// construction used.
    /// </param>
    void Registration(Func<IServiceProvider, INotificationServiceProvider?, INotificationConfigurationProvider?, ILogger<INotificationType>, IGenericResult> method);

    /// <summary>
    /// Runs the body set by the <see cref="Registration"/> overload above, registering this option
    /// with whichever of <paramref name="domainProvider"/>/<paramref name="domainConfigurationProvider"/>
    /// were supplied.
    /// </summary>
    /// <param name="serviceProvider">The scope actually constructing whatever called this — root at startup, a request's own scope for a request.</param>
    /// <param name="domainProvider">The domain service provider instance being constructed, or null.</param>
    /// <param name="domainConfigurationProvider">The domain configuration provider, or null.</param>
    /// <param name="logger">The logger to report the outcome to — the caller's own, already built with its <c>NullLogger</c> fallback applied.</param>
    /// <returns>Success, or a structured failure. The base no-op returns success for options with nothing to contribute.</returns>
    IGenericResult Register(IServiceProvider serviceProvider, INotificationServiceProvider? domainProvider, INotificationConfigurationProvider? domainConfigurationProvider, ILogger<INotificationType> logger);
}

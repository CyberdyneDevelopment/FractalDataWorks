using System;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            defaultDataStoreName: "PlatformConfiguration",
            defaultPathName: "notify",
            defaultContainerName: defaultContainerName)
    {
        _channelName = channelName;
    }

    // ── Domain-provider/domain-configuration-provider registration ─────────────────────────────
    // An overload of Registration/Register, not a new phase and not a new method name. Stored and
    // invoked exactly like the DI-wiring Registration(Func<IHostApplicationBuilder, ...>) overload
    // already inherited from ServiceTypeBase -- this is simply a second signature of the same verb,
    // distinguished by its parameter types.

    private Func<IServiceProvider, INotificationServiceProvider?, INotificationConfigurationProvider?, ILogger<INotificationType>, IGenericResult> _domainRegistrationMethod
        = static (_, _, _, _) => GenericResult.Success();

    /// <inheritdoc/>
    public void Registration(Func<IServiceProvider, INotificationServiceProvider?, INotificationConfigurationProvider?, ILogger<INotificationType>, IGenericResult> method)
    {
        _domainRegistrationMethod = method;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Base default: an option that never called the overload above reports success and leaves
    /// both providers untouched.
    /// </remarks>
    public IGenericResult Register(IServiceProvider serviceProvider, INotificationServiceProvider? domainProvider, INotificationConfigurationProvider? domainConfigurationProvider, ILogger<INotificationType> logger)
        => _domainRegistrationMethod(serviceProvider, domainProvider, domainConfigurationProvider, logger);
}

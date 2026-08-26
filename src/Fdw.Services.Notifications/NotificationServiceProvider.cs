using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications;

/// <summary>
/// The notification domain's service provider.
/// </summary>
/// <remarks>
/// Inherits the whole resolution path from
/// <see cref="PlatformServiceProviderBase{TService, TConfiguration, TFactory, TConfigurationProvider}"/>
/// and overrides nothing — notifications resolve exactly the way the platform does. A domain that
/// needs different behaviour overrides the virtual member here rather than reaching into the base.
/// </remarks>
/// <remarks>
/// Closed over <see cref="INotificationConfiguration"/> rather than the configuration class:
/// <c>IServiceConfigurationProvider&lt;T&gt;</c> is invariant, so a base closed over the class cannot
/// satisfy an interface closed over the contract. The concrete class is named at the option and
/// factory level, where the typed body actually matters.
/// </remarks>
public sealed class NotificationServiceProvider
    : PlatformServiceProviderBase<
        IPlatformNotification,
        INotificationConfiguration,
        INotificationFactory<IPlatformNotification, INotificationConfiguration>,
        IServiceConfigurationProvider<INotificationConfiguration>>,
      INotificationServiceProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationServiceProvider"/> class.
    /// </summary>
    /// <param name="services">The container this provider resolves its factories from.</param>
    /// <param name="logger">The logger for this provider.</param>
    public NotificationServiceProvider(IServiceProvider services, ILogger<NotificationServiceProvider> logger)
        : base(services, logger ?? NullLogger<NotificationServiceProvider>.Instance)
    {
    }
}

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
/// <see cref="DomainServiceProviderBase{TService, TConfiguration, TFactory, TConfigurationProvider}"/>
/// and overrides nothing — notifications resolve exactly the way the platform does. A domain that
/// needs different behaviour overrides the virtual member here rather than reaching into the base.
/// </remarks>
/// <remarks>
/// Closed over <see cref="INotificationImplementationConfiguration"/> rather than the configuration class:
/// <c>IServiceConfigurationProvider&lt;T&gt;</c> is invariant, so a base closed over the class cannot
/// satisfy an interface closed over the contract. The concrete class is named at the option and
/// factory level, where the typed body actually matters.
/// </remarks>
public sealed class NotificationServiceProvider
    : DomainServiceProviderBase<
        IPlatformNotification,
        INotificationImplementationConfiguration,
        INotificationFactory<IPlatformNotification, INotificationImplementationConfiguration>,
        INotificationConfigurationProvider>,
      INotificationServiceProvider
{
    /// <summary>Gets how this provider reads the configuration it builds a service from.</summary>
    /// <remarks>A notification service is built from the configuration (its channel, its settings) and shows no description, and nothing saves the configuration back (FDW-7351).</remarks>
    protected override ConfigurationReadOptions ReadOptions => ConfigurationReadOptions.WithoutFilledMembers;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationServiceProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger for this provider.</param>
    public NotificationServiceProvider(ILogger<NotificationServiceProvider> logger)
        : base(logger ?? NullLogger<NotificationServiceProvider>.Instance)
    {
    }
}

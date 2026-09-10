using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Commands;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications;

/// <summary>Supplies the Notification configuration.</summary>
public sealed class NotificationConfigurationProvider
    : DomainConfigurationProviderBase<INotificationImplementationConfiguration>,
      INotificationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="NotificationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public NotificationConfigurationProvider(
        ILogger<NotificationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "notify", "Notification")
    {
    }
}

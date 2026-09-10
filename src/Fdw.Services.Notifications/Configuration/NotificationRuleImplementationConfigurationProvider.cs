using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>Supplies the NotificationRule implementation's own configuration.</summary>
public sealed class NotificationRuleImplementationConfigurationProvider
    : ImplementationProviderBase<NotificationRuleImplementationConfiguration, INotificationRuleImplementationConfiguration>,
      INotificationRuleImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="NotificationRuleImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public NotificationRuleImplementationConfigurationProvider(
        ILogger<NotificationRuleImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "notify", "NotificationRuleImplementation")
    {
    }
}

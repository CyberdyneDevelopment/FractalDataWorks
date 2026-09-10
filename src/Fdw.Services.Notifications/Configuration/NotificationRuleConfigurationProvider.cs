using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>Supplies the configured NotificationRule members.</summary>
public sealed class NotificationRuleConfigurationProvider
    : DomainConfigurationProviderBase<INotificationRuleImplementationConfiguration>,
      INotificationRuleConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="NotificationRuleConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public NotificationRuleConfigurationProvider(
        ILogger<NotificationRuleConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "notify", "NotificationRule")
    {
    }
}

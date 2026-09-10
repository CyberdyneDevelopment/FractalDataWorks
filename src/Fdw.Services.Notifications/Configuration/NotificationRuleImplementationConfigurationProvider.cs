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
    public NotificationRuleImplementationConfigurationProvider(
        ILogger<NotificationRuleImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "notify", "NotificationRuleImplementation")
    {
    }
}

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
    public NotificationRuleConfigurationProvider(
        ILogger<NotificationRuleConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "notify", "NotificationRule")
    {
    }
}

using Fdw.Services.Abstractions;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>Supplies the configured NotificationRule members.</summary>
public interface INotificationRuleConfigurationProvider
    : IDomainConfigurationProvider<INotificationRuleImplementationConfiguration>
{
}

using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>Supplies the configured NotificationRule members.</summary>
public interface INotificationRuleConfigurationProvider
    : IDomainConfigurationProvider<INotificationRuleImplementationConfiguration>,
      IDomainRecordProvider<NotificationRuleDomainConfiguration>
{
}

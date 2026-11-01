using Fdw.Services.Abstractions;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>Supplies the NotificationRule implementation's own configuration to the domain that registers it.</summary>
public interface INotificationRuleImplementationConfigurationProvider
    : IImplementationConfigurationProvider<INotificationRuleImplementationConfiguration>
{
}

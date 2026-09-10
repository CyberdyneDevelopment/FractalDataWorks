using Fdw.Services.Abstractions;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Resolves configured notification channels and routes each to the implementation provider that owns it.
/// </summary>
public interface INotificationConfigurationProvider
    : IDomainConfigurationProvider<INotificationImplementationConfiguration>
{
}

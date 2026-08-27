using Fdw.Configuration;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// One configured notification channel — the domain record, naming which implementation it is and
/// holding that implementation's own configuration.
/// </summary>
public interface INotificationConfiguration
    : IPlatformServiceConfiguration<INotificationImplementationConfiguration>
{
}

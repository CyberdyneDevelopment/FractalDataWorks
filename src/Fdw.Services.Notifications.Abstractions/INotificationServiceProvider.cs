using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Resolves notification services by configuration name or id.
/// </summary>
/// <remarks>
/// The domain's name for the platform contract, rather than a bare
/// <c>IPlatformServiceProvider&lt;IPlatformNotification, INotificationImplementationConfiguration&gt;</c> at every
/// injection site. It narrows the contract at the configuration arity, so the registration and
/// typed-configuration overloads stay reachable through it.
/// </remarks>
public interface INotificationServiceProvider
    : IPlatformServiceProvider<IPlatformNotification, INotificationImplementationConfiguration>
{
}

using Fdw.Configuration;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// The configuration contract every notification channel binds to.
/// </summary>
/// <remarks>
/// Lives here rather than beside the class because a contract in this package cannot name a type in
/// the core package — the dependency runs the other way. Declaring the interface here is what lets
/// <see cref="INotificationServiceProvider"/> name its configuration at all.
/// </remarks>
public interface INotificationConfiguration : IImplementationConfiguration
{
}

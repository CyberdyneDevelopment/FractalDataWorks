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
public interface INotificationImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets whether the notification is enabled. Stored on the domain row; every implementation declares it.</summary>
    bool IsEnabled { get; set; }

    /// <summary>Gets or sets the name of the secret manager that holds the notification's credentials, or null when it needs none.</summary>
    string? SecretManagerName { get; set; }

    /// <summary>Gets or sets the key of the notification's credentials in the secret manager, or null when it needs none.</summary>
    string? SecretKeyName { get; set; }

    /// <summary>Gets or sets what the notification is for, kept on the domain row.</summary>
    string? Description { get; set; }
}

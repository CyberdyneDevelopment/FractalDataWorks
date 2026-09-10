using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications;

/// <summary>
/// Parent (header) configuration class for all notification types.
/// Generates the parent table <c>notify.Notification</c> which holds identity fields only.
/// </summary>
/// <remarks>
/// <para>
/// Follows the polymorphic configuration pattern: this parent holds identity-only fields.
/// Runtime fields (Lifetime, IsEnabled, SecretManagerName, SecretKeyName, channel-specific
/// settings) live on typed-body configuration classes (EmailNotificationConfiguration, etc.).
/// </para>
/// <para>
/// This class serves two purposes:
/// <list type="bullet">
/// <item><description>As a header configuration for <c>IOptionsSnapshot&lt;List&lt;NotificationConfiguration&gt;&gt;</c> lookups</description></item>
/// <item><description>As the base class for type-specific configurations (EmailNotificationConfiguration, etc.)</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Notification")]
public partial class NotificationConfiguration : DomainConfigurationBase, INotificationConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public NotificationConfiguration() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="implementation">The service option type (e.g., "Email", "Teams", "Webhook").</param>
    protected NotificationConfiguration(string? implementation)
    {
        Implementation = implementation;
    }








}

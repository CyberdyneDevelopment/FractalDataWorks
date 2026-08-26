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
public partial class NotificationConfiguration : INotificationConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public NotificationConfiguration() : this("Notification", null, "Notifications")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="serviceType">The service type (domain) - always "Notification".</param>
    /// <param name="serviceOptionType">The service option type (e.g., "Email", "Teams", "Webhook").</param>
    /// <param name="sectionName">The configuration section name for binding.</param>
    protected NotificationConfiguration(string serviceType, string? serviceOptionType, string sectionName)
    {
        ServiceType = serviceType;
        ServiceOptionType = serviceOptionType;
        SectionName = sectionName;
    }

    /// <summary>
    /// Gets or sets the unique identifier for this notification.
    /// </summary>
    // Why: No Guid.NewGuid() default — DB owns identity assignment. A random default would
    // propagate to typed-body lookups via WHERE [NotificationId] = @parentId, causing misses.
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this notification for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section name for configuration binding.
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Gets or sets the service type (domain) - always "Notification" for this configuration.
    /// </summary>
    public string ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the service option type (e.g., "Email", "Sms", "Push").
    /// </summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets the notification type name. Alias for <see cref="ServiceOptionType"/>.
    /// </summary>
    public string? NotificationType => ServiceOptionType;

    /// <summary>
    /// Gets or sets the optional description of this notification channel.
    /// </summary>
    public string? Description { get; set; }

}

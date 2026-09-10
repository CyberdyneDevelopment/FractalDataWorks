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

    /// <summary>
    /// Gets or sets the unique identifier for this notification.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this notification for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the domain this record belongs to.</summary>
    public string Domain => "Notification";

    /// <summary>
    /// Gets or sets the service option type (e.g., "Email", "Sms", "Push").
    /// </summary>
    public string? Implementation { get; set; }

    /// <summary>
    /// Gets the notification type name. Alias for <see cref="Implementation"/>.
    /// </summary>
    public string? NotificationType => Implementation;

    /// <summary>
    /// Gets or sets the optional description of this notification channel.
    /// </summary>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public INotificationImplementationConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// The non-generic view of <see cref="Configuration"/>. The platform service provider reads the
    /// implementation without naming this domain's implementation contract; netstandard2.0 rules out
    /// a default interface implementation, so each domain record states it.
    /// </remarks>
    IGenericConfiguration? IDomainConfiguration.ImplementationConfiguration => Configuration;

}

using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications;

namespace Fdw.Services.Notifications.System;

/// <summary>
/// Configuration for system/in-app notification services.
/// Bridges the notification service domain to the in-system messaging framework.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Notification",
    ServiceType = "System")]
public sealed partial class SystemNotificationConfiguration : NotificationConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemNotificationConfiguration"/> class.
    /// </summary>
    public SystemNotificationConfiguration() : base("Notification", "System", "Notifications:System")
    {
    }

    // ========================================
    // Runtime fields (not on parent header)
    // Why: Polymorphic configuration pattern — parent is identity-only.
    // These fields are specific to the typed body and read by the factory at service construction time.
    // ========================================

    /// <summary>
    /// Gets or sets the service lifetime for DI registration.
    /// </summary>
    public IServiceLifetime Lifetime { get; set; } = ServiceLifetimes.Transient;

    /// <summary>
    /// Gets or sets whether this notification channel is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the secret manager to use for retrieving secrets.
    /// </summary>
    public string? SecretManagerName { get; set; }

    /// <summary>
    /// Gets or sets the key name within the secret manager to retrieve.
    /// </summary>
    public string? SecretKeyName { get; set; }

    // ========================================
    // System-specific fields
    // ========================================

    /// <summary>
    /// Gets or sets the default message severity for system notifications.
    /// Defaults to Info.
    /// </summary>
    public string DefaultSeverity { get; set; } = "Info";

    /// <summary>
    /// Gets or sets the default message type for system notifications.
    /// Defaults to Notification.
    /// </summary>
    public string DefaultMessageType { get; set; } = "Notification";
}

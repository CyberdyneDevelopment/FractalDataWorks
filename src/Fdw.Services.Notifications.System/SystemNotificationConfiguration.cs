using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Abstractions;
using Fdw.Services.Notifications;

using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.System;

/// <summary>
/// Configuration for system/in-app notification services.
/// Bridges the notification service domain to the in-system messaging framework.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Notification",
    ServiceType = "System")]
public sealed partial class SystemNotificationConfiguration : INotificationImplementationConfiguration
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning notification's durable id.</summary>
    public Guid NotificationId { get; set; }

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

using System;
using Fdw.Configuration;
using Fdw.Services.Abstractions;

#pragma warning disable CA1822 // expression-bodied properties implement IGenericConfiguration interface contract

namespace Fdw.Services.Notifications.Abstractions.Configuration;

/// <summary>
/// Configuration for Microsoft Teams notifications.
/// </summary>
public sealed class TeamsConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this configuration instance.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the logical name of this configuration.
    /// </summary>
    public string Name { get; set; } = "Teams";

    /// <summary>
    /// Gets the configuration section name used to locate this block in the configuration source.
    /// </summary>
    public string SectionName => "Teams";

    /// <summary>
    /// Gets the service type (domain) - always "Notification" for this configuration.
    /// </summary>
    public string ServiceType => "Notification";

    /// <summary>
    /// Gets the service option type (implementation variant) this configuration is for.
    /// Alias for <see cref="NotificationType"/>.
    /// </summary>
    public string? ServiceOptionType => NotificationType;

    /// <summary>
    /// Gets or sets the notification type identifier for this configuration.
    /// </summary>
    public string NotificationType { get; set; } = "Teams";

    /// <summary>
    /// Gets or sets an optional human-readable description of this configuration.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the service lifetime used when registering the notification service in the DI container.
    /// </summary>
    public IServiceLifetime Lifetime { get; set; } = ServiceLifetimes.Transient;

    /// <summary>
    /// Gets or sets a value indicating whether this notification configuration is active.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the secret manager used to resolve the webhook secret.
    /// </summary>
    public string? SecretManagerName { get; set; }

    /// <summary>
    /// Gets or sets the key name within the secret manager that holds the webhook secret value.
    /// </summary>
    public string? SecretKeyName { get; set; }

    /// <summary>
    /// Gets or sets the default webhook URL used when no per-request URL is provided.
    /// </summary>
    public string? DefaultWebhookUrl { get; set; }

    /// <summary>
    /// Gets or sets the HTTP request timeout in seconds for outbound Teams webhook calls.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether notifications are sent as Adaptive Cards rather than plain message text.
    /// </summary>
    public bool UseAdaptiveCards { get; set; } = true;

}

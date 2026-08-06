using System;
using System.Collections.Generic;
using Fdw.Configuration;
using Fdw.Services.Abstractions;

#pragma warning disable CA1822 // expression-bodied properties implement IGenericConfiguration interface contract

namespace Fdw.Services.Notifications.Abstractions.Configuration;

/// <summary>
/// Configuration for webhook notifications.
/// </summary>
public sealed class WebhookConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this webhook configuration.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display name of this webhook configuration.
    /// </summary>
    public string Name { get; set; } = "Webhook";

    /// <summary>
    /// Gets the configuration section name — always "Webhook" for this configuration.
    /// </summary>
    public string SectionName => "Webhook";

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
    /// Gets or sets the notification type — always "Webhook" for this configuration.
    /// </summary>
    public string NotificationType { get; set; } = "Webhook";

    /// <summary>
    /// Gets or sets an optional human-readable description of this webhook configuration.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the service lifetime for webhook notification instances.
    /// </summary>
    public IServiceLifetime Lifetime { get; set; } = ServiceLifetimes.Transient;

    /// <summary>
    /// Gets or sets whether this webhook configuration is active and will be used to dispatch notifications.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the secret manager used to resolve the webhook secret.
    /// </summary>
    public string? SecretManagerName { get; set; }

    /// <summary>
    /// Gets or sets the key name within the secret manager that holds the webhook signing secret or auth token.
    /// </summary>
    public string? SecretKeyName { get; set; }

    /// <summary>
    /// Gets or sets the fallback URL used when a notification does not supply an explicit target URL.
    /// </summary>
    public string? DefaultWebhookUrl { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of seconds to wait for an HTTP response before the request is cancelled.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the HTTP method used when dispatching webhook requests (e.g., "POST", "PUT").
    /// </summary>
    public string HttpMethod { get; set; } = "POST";

    /// <summary>
    /// Gets or sets additional HTTP headers included on every outbound webhook request.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Headers { get; set; }

}

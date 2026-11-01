using System;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>The answer to making a new secret for a webhook trigger: the secret, which is shown only here. The old one stops working.</summary>
public class WebhookSecretPayload
{
    /// <summary>Gets or sets the trigger's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the new secret. It cannot be read again.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Gets or sets the first characters of the new secret.</summary>
    public string SecretPrefix { get; set; } = string.Empty;

    /// <summary>Gets or sets when the new secret was made.</summary>
    public DateTimeOffset SecretCreatedAt { get; set; }
}

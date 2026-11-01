namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>The answer to making a webhook trigger: the trigger and its secret, which is shown only here.</summary>
public class WebhookCreatedPayload : WebhookPayload
{
    /// <summary>Gets or sets the secret the caller sends in the <c>X-Webhook-Secret</c> header. It cannot be read again.</summary>
    public string Secret { get; set; } = string.Empty;
}

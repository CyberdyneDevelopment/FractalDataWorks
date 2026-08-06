using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// Invalid webhook URL.
/// </summary>
[TypeOption(typeof(NotificationResultCodes), "InvalidWebhookUrl", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidWebhookUrlCode : NotificationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidWebhookUrlCode"/> class.
    /// </summary>
    public InvalidWebhookUrlCode()
        : base(21002, "InvalidWebhookUrl",
            ResultSeverities.ByName("Error"),
            "Invalid webhook URL: {WebhookUrl}",
            isRetryable: false)
    {
    }
}
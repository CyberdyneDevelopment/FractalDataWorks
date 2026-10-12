using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// At least one webhook URL is required, or configure a default webhook URL.
/// </summary>
[TypeOption(typeof(NotificationResultCodes), "NoWebhookUrl", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoWebhookUrlCode : NotificationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoWebhookUrlCode"/> class.
    /// </summary>
    public NoWebhookUrlCode()
        : base(21001, "NoWebhookUrl",
            ResultSeverities.ByName("Error"),
            "At least one webhook URL is required, or configure a default webhook URL",
            isRetryable: false)
    {
    }
}
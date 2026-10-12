using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// Notification channel not found or not available.
/// </summary>
[TypeOption(typeof(NotificationResultCodes), "ChannelNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ChannelNotFoundCode : NotificationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelNotFoundCode"/> class.
    /// </summary>
    public ChannelNotFoundCode()
        : base(30000, "ChannelNotFound",
            ResultSeverities.ByName("Error"),
            "Notification channel '{ChannelName}' not found or not available",
            isRetryable: false)
    {
    }
}
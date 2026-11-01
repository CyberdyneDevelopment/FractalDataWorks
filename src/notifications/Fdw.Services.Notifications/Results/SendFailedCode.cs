using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// Send operation failed.
/// </summary>
[TypeOption(typeof(NotificationResultCodes), "SendFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SendFailedCode : NotificationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendFailedCode"/> class.
    /// </summary>
    public SendFailedCode()
        : base(70000, "SendFailed",
            ResultSeverities.ByName("Error"),
            "Send operation failed: {Message}",
            isRetryable: true)
    {
    }
}
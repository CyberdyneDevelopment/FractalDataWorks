using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// Use Send() method for notification requests.
/// </summary>
[TypeOption(typeof(NotificationResultCodes), "UseSendMethod", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UseSendMethodCode : NotificationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UseSendMethodCode"/> class.
    /// </summary>
    public UseSendMethodCode()
        : base(91000, "UseSendMethod",
            ResultSeverities.ByName("Error"),
            "Use Send() method for notification requests",
            isRetryable: false)
    {
    }
}
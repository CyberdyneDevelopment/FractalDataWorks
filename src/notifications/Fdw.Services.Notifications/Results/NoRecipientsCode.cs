using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// At least one recipient is required.
/// </summary>
[TypeOption(typeof(NotificationResultCodes), "NoRecipients", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoRecipientsCode : NotificationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoRecipientsCode"/> class.
    /// </summary>
    public NoRecipientsCode()
        : base(21000, "NoRecipients",
            ResultSeverities.ByName("Error"),
            "At least one recipient is required",
            isRetryable: false)
    {
    }
}
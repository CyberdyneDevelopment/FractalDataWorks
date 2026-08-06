using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// Invalid email address.
/// </summary>
[TypeOption(typeof(NotificationResultCodes), "InvalidEmailAddress", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidEmailAddressCode : NotificationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidEmailAddressCode"/> class.
    /// </summary>
    public InvalidEmailAddressCode()
        : base(20001, "InvalidEmailAddress",
            ResultSeverities.ByName("Error"),
            "Invalid email address: {EmailAddress}",
            isRetryable: false)
    {
    }
}
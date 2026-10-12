using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// A notification rule carries a severity that maps to no notification priority.
/// </summary>
[TypeOption(typeof(NotificationResultCodes), "RuleSeverityInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RuleSeverityInvalidCode : NotificationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuleSeverityInvalidCode"/> class.
    /// </summary>
    public RuleSeverityInvalidCode()
        : base(21004, "RuleSeverityInvalid",
            ResultSeverities.ByName("Error"),
            "Notification rule '{RuleName}' has invalid severity '{Severity}'",
            isRetryable: false)
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// A notification rule of the given name is not configured.
/// </summary>
/// <remarks>
/// The message names the rule and nothing else: the rule lookup's own reason is written to the log, so a lookup that gives
/// none no longer leaves empty parentheses at the end of the sentence (FDW-1145).
/// </remarks>
[TypeOption(typeof(NotificationResultCodes), "RuleNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RuleNotFoundCode : NotificationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuleNotFoundCode"/> class.
    /// </summary>
    public RuleNotFoundCode()
        : base(30001, "RuleNotFound",
            ResultSeverities.ByName("Error"),
            "Notification rule '{RuleName}' was not found",
            isRetryable: false)
    {
    }
}

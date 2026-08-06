using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// Result type mismatch.
/// </summary>
[TypeOption(typeof(NotificationResultCodes), "UnexpectedResultType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnexpectedResultTypeCode : NotificationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedResultTypeCode"/> class.
    /// </summary>
    public UnexpectedResultTypeCode()
        : base(90000, "UnexpectedResultType",
            ResultSeverities.ByName("Error"),
            "Result is not of expected type {ExpectedType}",
            isRetryable: false)
    {
    }
}
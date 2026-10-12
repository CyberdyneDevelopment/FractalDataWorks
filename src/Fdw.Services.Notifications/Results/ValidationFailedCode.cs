using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Results;

/// <summary>
/// Notification request validation failed.
/// </summary>
[TypeOption(typeof(NotificationResultCodes), "ValidationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ValidationFailedCode : NotificationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFailedCode"/> class.
    /// </summary>
    public ValidationFailedCode()
        : base(21003, "ValidationFailed",
            ResultSeverities.ByName("Error"),
            "Validation failed: {Message}",
            isRetryable: false)
    {
    }
}
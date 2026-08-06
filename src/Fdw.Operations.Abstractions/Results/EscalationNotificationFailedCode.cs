using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Failed to send escalation notification.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "EscalationNotificationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EscalationNotificationFailedCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationNotificationFailedCode"/> class.
    /// </summary>
    public EscalationNotificationFailedCode()
        : base(
            71000,
            "EscalationNotificationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to send escalation notification: {Error}",
            isRetryable: true)
    {
    }
}
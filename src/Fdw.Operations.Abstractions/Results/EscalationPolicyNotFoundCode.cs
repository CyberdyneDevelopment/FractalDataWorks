using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// No escalation policy found.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "EscalationPolicyNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EscalationPolicyNotFoundCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationPolicyNotFoundCode"/> class.
    /// </summary>
    public EscalationPolicyNotFoundCode()
        : base(
            31000,
            "EscalationPolicyNotFound",
            ResultSeverities.ByName("Warning"),
            "No escalation policy found for '{ExecutionItemId}'",
            isRetryable: false)
    {
    }
}
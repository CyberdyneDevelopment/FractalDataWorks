using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Escalation suppressed by override.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "EscalationSuppressed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EscalationSuppressedCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationSuppressedCode"/> class.
    /// </summary>
    public EscalationSuppressedCode()
        : base(
            11001,
            "EscalationSuppressed",
            ResultSeverities.ByName("Information"),
            "Escalation suppressed by override",
            isRetryable: false)
    {
    }
}
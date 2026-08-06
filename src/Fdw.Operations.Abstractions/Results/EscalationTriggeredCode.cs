using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Escalation triggered at specified level.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "EscalationTriggered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EscalationTriggeredCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationTriggeredCode"/> class.
    /// </summary>
    public EscalationTriggeredCode()
        : base(
            11002,
            "EscalationTriggered",
            ResultSeverities.ByName("Warning"),
            "Escalation triggered at level {Level}",
            isRetryable: false)
    {
    }
}
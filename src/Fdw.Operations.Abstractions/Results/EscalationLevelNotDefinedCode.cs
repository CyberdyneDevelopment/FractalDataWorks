using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Escalation level not defined in policy.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "EscalationLevelNotDefined", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EscalationLevelNotDefinedCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationLevelNotDefinedCode"/> class.
    /// </summary>
    public EscalationLevelNotDefinedCode()
        : base(
            60001,
            "EscalationLevelNotDefined",
            ResultSeverities.ByName("Error"),
            "Escalation level {Level} not defined in policy",
            isRetryable: false)
    {
    }
}
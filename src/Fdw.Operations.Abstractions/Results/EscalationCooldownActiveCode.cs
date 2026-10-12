using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Escalation is in cooldown period.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "EscalationCooldownActive", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EscalationCooldownActiveCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationCooldownActiveCode"/> class.
    /// </summary>
    public EscalationCooldownActiveCode()
        : base(
            11000,
            "EscalationCooldownActive",
            ResultSeverities.ByName("Information"),
            "Escalation in cooldown until {CooldownEnd}",
            isRetryable: false)
    {
    }
}
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>The {Escalation} consequence of threshold '{Threshold}' needs an escalation policy and the rule names none</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdPolicyMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdPolicyMissingCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdPolicyMissingCode"/> class.</summary>
    public ThresholdPolicyMissingCode()
        : base(
            20004,
            "ThresholdPolicyMissing",
            ResultSeverities.ByName("Error"),
            "The {Escalation} consequence of threshold '{Threshold}' needs an escalation policy and the rule names none",
            isRetryable: false)
    {
    }
}

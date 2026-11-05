using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>Rule {Rule} of threshold '{Threshold}' is not valid: {Reason}</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdRuleInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdRuleInvalidCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdRuleInvalidCode"/> class.</summary>
    public ThresholdRuleInvalidCode()
        : base(
            20005,
            "ThresholdRuleInvalid",
            ResultSeverities.ByName("Error"),
            "Rule {Rule} of threshold '{Threshold}' is not valid: {Reason}",
            isRetryable: false)
    {
    }
}

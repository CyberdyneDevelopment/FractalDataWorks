using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>The {Escalation} consequence of threshold '{Threshold}' acts on a run, but the subject reported none</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdRunMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdRunMissingCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdRunMissingCode"/> class.</summary>
    public ThresholdRunMissingCode()
        : base(
            20003,
            "ThresholdRunMissing",
            ResultSeverities.ByName("Error"),
            "The {Escalation} consequence of threshold '{Threshold}' acts on a run, but the subject reported none",
            isRetryable: false)
    {
    }
}

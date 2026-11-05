using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>The host registered no {Service}, so the {Escalation} consequence of threshold '{Threshold}' cannot act</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdServiceMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdServiceMissingCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdServiceMissingCode"/> class.</summary>
    public ThresholdServiceMissingCode()
        : base(
            60002,
            "ThresholdServiceMissing",
            ResultSeverities.ByName("Error"),
            "The host registered no {Service}, so the {Escalation} consequence of threshold '{Threshold}' cannot act",
            isRetryable: false)
    {
    }
}

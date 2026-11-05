using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>{Kind} '{Subject}' does not have threshold '{Threshold}' attached.</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdLinkAbsent", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdLinkAbsentCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdLinkAbsentCode"/> class.</summary>
    public ThresholdLinkAbsentCode()
        : base(
            31004,
            "ThresholdLinkAbsent",
            ResultSeverities.ByName("Error"),
            "{Kind} '{Subject}' does not have threshold '{Threshold}' attached",
            isRetryable: false)
    {
    }
}

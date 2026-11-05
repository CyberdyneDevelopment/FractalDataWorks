using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>Threshold '{Threshold}' has the type '{Type}', which is not one of the threshold types</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdTypeUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdTypeUnknownCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdTypeUnknownCode"/> class.</summary>
    public ThresholdTypeUnknownCode()
        : base(
            20006,
            "ThresholdTypeUnknown",
            ResultSeverities.ByName("Error"),
            "Threshold '{Threshold}' has the type '{Type}', which is not one of the threshold types",
            isRetryable: false)
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>Threshold '{Threshold}' does not exist.</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdNotFoundCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdNotFoundCode"/> class.</summary>
    public ThresholdNotFoundCode()
        : base(
            31003,
            "ThresholdNotFound",
            ResultSeverities.ByName("Error"),
            "Threshold '{Threshold}' does not exist",
            isRetryable: false)
    {
    }
}

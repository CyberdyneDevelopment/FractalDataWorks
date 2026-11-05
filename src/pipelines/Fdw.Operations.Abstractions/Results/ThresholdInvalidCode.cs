using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>Threshold '{Threshold}' is not valid: {Reason}</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdInvalidCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdInvalidCode"/> class.</summary>
    public ThresholdInvalidCode()
        : base(
            20012,
            "ThresholdInvalid",
            ResultSeverities.ByName("Error"),
            "Threshold '{Threshold}' is not valid: {Reason}",
            isRetryable: false)
    {
    }
}

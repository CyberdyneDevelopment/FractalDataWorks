using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>Threshold '{Threshold}' has a parameter that cannot be used: {Reason}.</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdParameterInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdParameterInvalidCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdParameterInvalidCode"/> class.</summary>
    public ThresholdParameterInvalidCode()
        : base(
            20013,
            "ThresholdParameterInvalid",
            ResultSeverities.ByName("Error"),
            "Threshold '{Threshold}' has a parameter that cannot be used: {Reason}",
            isRetryable: false)
    {
    }
}

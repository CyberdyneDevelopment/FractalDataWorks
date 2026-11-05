using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>The host has no {Service}, so the notification rules of a Notify consequence cannot be sent.</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdNotifierUnavailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdNotifierUnavailableCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdNotifierUnavailableCode"/> class.</summary>
    public ThresholdNotifierUnavailableCode()
        : base(
            20009,
            "ThresholdNotifierUnavailable",
            ResultSeverities.ByName("Error"),
            "The host has no {Service}, so the notification rules of a Notify consequence cannot be sent",
            isRetryable: false)
    {
    }
}

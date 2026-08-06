using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Metrics are not implemented for this transformation.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "MetricsNotImplemented")]
[ExcludeFromCodeCoverage]
public sealed class MetricsNotImplementedCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsNotImplementedCode"/> class.
    /// </summary>
    public MetricsNotImplementedCode()
        : base(90005, "MetricsNotImplemented",
            ResultSeverities.ByName("Warning"),
            "Metrics not implemented",
            isRetryable: false)
    {
    }
}
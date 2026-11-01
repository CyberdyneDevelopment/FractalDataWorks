using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.ResultCodes;

/// <summary>
/// The requested calculation entity was not found.
/// </summary>
[TypeOption(typeof(CalculationEntityResultCodes), "CalculationNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CalculationNotFoundCode : CalculationEntityResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationNotFoundCode"/> class.
    /// </summary>
    public CalculationNotFoundCode()
        : base(
            30000,
            "CalculationNotFound",
            ResultSeverities.ByName("Error"),
            "Calculation entity '{Name}' was not found",
            isRetryable: false)
    {
    }
}

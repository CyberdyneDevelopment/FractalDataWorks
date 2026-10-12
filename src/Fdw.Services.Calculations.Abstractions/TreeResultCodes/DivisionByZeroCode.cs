using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A value was divided by zero.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "DivisionByZero", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DivisionByZeroCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DivisionByZeroCode"/> class.
    /// </summary>
    public DivisionByZeroCode()
        : base(
            22000,
            "DivisionByZero",
            ResultSeverities.ByName("Error"),
            "Result '{result}' failed at {node}: the divisor is zero. {reason}",
            isRetryable: false)
    {
    }
}

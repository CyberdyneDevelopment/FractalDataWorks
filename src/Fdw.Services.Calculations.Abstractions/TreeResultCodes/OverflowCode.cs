using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// An arithmetic operation exceeded the range of a decimal.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "Overflow", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OverflowCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OverflowCode"/> class.
    /// </summary>
    public OverflowCode()
        : base(
            22004,
            "Overflow",
            ResultSeverities.ByName("Error"),
            "Result '{result}' failed at {node}: the arithmetic overflowed. {reason}",
            isRetryable: false)
    {
    }
}

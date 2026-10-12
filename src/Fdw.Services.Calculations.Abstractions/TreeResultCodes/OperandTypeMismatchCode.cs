using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// An operand is a number where a condition is needed, or the reverse.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "OperandTypeMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OperandTypeMismatchCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperandTypeMismatchCode"/> class.
    /// </summary>
    public OperandTypeMismatchCode()
        : base(
            22013,
            "OperandTypeMismatch",
            ResultSeverities.ByName("Error"),
            "Calculation tree refused at {node}: an operand has the wrong type. {reason}",
            isRetryable: false)
    {
    }
}

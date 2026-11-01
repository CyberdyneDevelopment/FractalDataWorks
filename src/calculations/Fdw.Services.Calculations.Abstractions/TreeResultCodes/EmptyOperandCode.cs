using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A number was needed but the operand was empty; wrap it in Coalesce to state the value to use.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "EmptyOperand", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EmptyOperandCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyOperandCode"/> class.
    /// </summary>
    public EmptyOperandCode()
        : base(
            22001,
            "EmptyOperand",
            ResultSeverities.ByName("Error"),
            "Result '{result}' failed at {node}: an operand is empty. {reason}",
            isRetryable: false)
    {
    }
}

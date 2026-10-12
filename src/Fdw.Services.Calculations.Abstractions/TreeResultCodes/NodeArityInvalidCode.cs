using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A tree node has fewer or more operands than its operator allows.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "NodeArityInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NodeArityInvalidCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeArityInvalidCode"/> class.
    /// </summary>
    public NodeArityInvalidCode()
        : base(
            22011,
            "NodeArityInvalid",
            ResultSeverities.ByName("Error"),
            "Calculation tree refused at {node}: wrong number of operands. {reason}",
            isRetryable: false)
    {
    }
}

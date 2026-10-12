using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A tree node names an operator that is not a CalculationNodeTypes option.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "NodeOperatorUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NodeOperatorUnknownCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeOperatorUnknownCode"/> class.
    /// </summary>
    public NodeOperatorUnknownCode()
        : base(
            22010,
            "NodeOperatorUnknown",
            ResultSeverities.ByName("Error"),
            "Calculation tree refused at {node}: the operator is unknown. {reason}",
            isRetryable: false)
    {
    }
}

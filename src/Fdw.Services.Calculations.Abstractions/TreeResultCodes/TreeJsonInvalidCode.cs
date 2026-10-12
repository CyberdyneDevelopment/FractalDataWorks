using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// The JSON text is not a calculation tree.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "TreeJsonInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TreeJsonInvalidCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TreeJsonInvalidCode"/> class.
    /// </summary>
    public TreeJsonInvalidCode()
        : base(
            22018,
            "TreeJsonInvalid",
            ResultSeverities.ByName("Error"),
            "Calculation tree refused at {node}: the tree JSON is invalid. {reason}",
            isRetryable: false)
    {
    }
}

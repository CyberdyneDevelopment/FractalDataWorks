using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A result name equals a field the same list reads, so the result would shadow the input.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "ResultShadowsField", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ResultShadowsFieldCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultShadowsFieldCode"/> class.
    /// </summary>
    public ResultShadowsFieldCode()
        : base(
            22016,
            "ResultShadowsField",
            ResultSeverities.ByName("Error"),
            "Calculation tree refused at {node}: a result has the name of an input field the list reads. {reason}",
            isRetryable: false)
    {
    }
}

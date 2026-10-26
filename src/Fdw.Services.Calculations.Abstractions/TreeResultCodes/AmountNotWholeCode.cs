using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// An amount of time was not a whole number, or was too large to add.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "AmountNotWhole", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AmountNotWholeCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AmountNotWholeCode"/> class.
    /// </summary>
    public AmountNotWholeCode()
        : base(
            22030,
            "AmountNotWhole",
            ResultSeverities.ByName("Error"),
            "Result '{result}' failed at {node}: an amount of time must be a whole number. {reason}",
            isRetryable: false)
    {
    }
}

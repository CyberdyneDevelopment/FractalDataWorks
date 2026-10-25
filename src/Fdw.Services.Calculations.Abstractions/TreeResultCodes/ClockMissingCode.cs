using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A calculation read the current time and the evaluation was given no clock.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "ClockMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ClockMissingCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClockMissingCode"/> class.
    /// </summary>
    public ClockMissingCode()
        : base(
            22029,
            "ClockMissing",
            ResultSeverities.ByName("Error"),
            "Result '{result}' failed at {node}: the evaluation was given no clock, so there is no current time. {reason}",
            isRetryable: false)
    {
    }
}

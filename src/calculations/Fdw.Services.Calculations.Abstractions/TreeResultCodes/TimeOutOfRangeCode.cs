using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A date-time calculation produced a moment outside the range that can be represented.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "TimeOutOfRange", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TimeOutOfRangeCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeOutOfRangeCode"/> class.
    /// </summary>
    public TimeOutOfRangeCode()
        : base(
            22031,
            "TimeOutOfRange",
            ResultSeverities.ByName("Error"),
            "Result '{result}' failed at {node}: the date-time is outside the range that can be represented. {reason}",
            isRetryable: false)
    {
    }
}

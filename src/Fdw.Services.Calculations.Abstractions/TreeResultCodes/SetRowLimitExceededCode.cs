using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>A calculation set would write more rows than the stated limit.</summary>
[TypeOption(typeof(CalculationTreeResultCodes), "SetRowLimitExceeded", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetRowLimitExceededCode : CalculationTreeResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="SetRowLimitExceededCode"/> class.</summary>
    public SetRowLimitExceededCode()
        : base(
            22021,
            "SetRowLimitExceeded",
            ResultSeverities.ByName("Error"),
            "Output '{output}' refused: it would exceed the row limit. {reason}",
            isRetryable: false)
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>A value a total reads is not a number.</summary>
[TypeOption(typeof(CalculationTreeResultCodes), "SetValueNotNumeric", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetValueNotNumericCode : CalculationTreeResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="SetValueNotNumericCode"/> class.</summary>
    public SetValueNotNumericCode()
        : base(
            22028,
            "SetValueNotNumeric",
            ResultSeverities.ByName("Error"),
            "Output '{output}' refused: a value to total is not a number. {reason}",
            isRetryable: false)
    {
    }
}

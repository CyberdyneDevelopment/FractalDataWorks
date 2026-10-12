using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A field the tree reads as a number is absent or holds something other than a decimal or integer value.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "FieldNotNumeric", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldNotNumericCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldNotNumericCode"/> class.
    /// </summary>
    public FieldNotNumericCode()
        : base(
            22005,
            "FieldNotNumeric",
            ResultSeverities.ByName("Error"),
            "Result '{result}' failed at {node}: the field is not usable as a number. {reason}",
            isRetryable: false)
    {
    }
}

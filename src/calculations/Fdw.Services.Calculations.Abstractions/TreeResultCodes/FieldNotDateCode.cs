using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A field the tree reads as a date is absent, or holds something other than a date: a date-time with a time of day, or a value of
/// another type.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "FieldNotDate", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldNotDateCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldNotDateCode"/> class.
    /// </summary>
    public FieldNotDateCode()
        : base(
            22032,
            "FieldNotDate",
            ResultSeverities.ByName("Error"),
            "Result '{result}' failed at {node}: the field is not usable as a date. {reason}",
            isRetryable: false)
    {
    }
}

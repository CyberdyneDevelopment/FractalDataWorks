using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A field the tree reads as a true/false flag is absent or holds something other than a boolean, 0 or 1.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "FieldNotFlag", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldNotFlagCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldNotFlagCode"/> class.
    /// </summary>
    public FieldNotFlagCode()
        : base(
            22006,
            "FieldNotFlag",
            ResultSeverities.ByName("Error"),
            "Result '{result}' failed at {node}: the field is not usable as a flag. {reason}",
            isRetryable: false)
    {
    }
}

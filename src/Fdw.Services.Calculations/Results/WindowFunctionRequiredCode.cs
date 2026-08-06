using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.ResultCodes;

namespace Fdw.Services.Calculations;

/// <summary>
/// A Windowed calculation entity requires both a target field and a window function name.
/// Either <c>TargetField</c> or <c>WindowFunction</c> was empty or whitespace only.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CalculationEntityResultCodes), "WindowFunctionRequired")]
public sealed class WindowFunctionRequiredCode : CalculationEntityResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="WindowFunctionRequiredCode"/>.
    /// </summary>
    public WindowFunctionRequiredCode()
        : base(
            21001,
            "WindowFunctionRequired",
            ResultSeverities.ByName("Error"),
            "TargetField and WindowFunction are both required and must not be empty for a Windowed calculation entity.",
            isRetryable: false)
    {
    }
}

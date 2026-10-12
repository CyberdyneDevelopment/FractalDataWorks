using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A Guard node found its condition false.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "GuardFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GuardFailedCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuardFailedCode"/> class.
    /// </summary>
    public GuardFailedCode()
        : base(
            22003,
            "GuardFailed",
            ResultSeverities.ByName("Error"),
            "Result '{result}' failed at {node}: its guard condition is false. {reason}",
            isRetryable: false)
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// A Require node found its operand empty.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "RequiredValueEmpty", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RequiredValueEmptyCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredValueEmptyCode"/> class.
    /// </summary>
    public RequiredValueEmptyCode()
        : base(
            22002,
            "RequiredValueEmpty",
            ResultSeverities.ByName("Error"),
            "Result '{result}' failed at {node}: a required value is empty. {reason}",
            isRetryable: false)
    {
    }
}

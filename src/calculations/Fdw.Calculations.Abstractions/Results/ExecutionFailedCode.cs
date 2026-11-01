using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Execution failed.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "ExecutionFailed")]
[ExcludeFromCodeCoverage]
public sealed class ExecutionFailedCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionFailedCode"/> class.
    /// </summary>
    public ExecutionFailedCode()
        : base(91000, "ExecutionFailed",
            ResultSeverities.ByName("Error"),
            "Execution failed: {Error}",
            isRetryable: false)
    {
    }
}
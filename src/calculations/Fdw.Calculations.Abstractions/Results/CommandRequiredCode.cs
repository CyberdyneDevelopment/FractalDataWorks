using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Data command is required for query execution.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "CommandRequired")]
[ExcludeFromCodeCoverage]
public sealed class CommandRequiredCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandRequiredCode"/> class.
    /// </summary>
    public CommandRequiredCode()
        : base(21001, "CommandRequired",
            ResultSeverities.ByName("Error"),
            "Command is required",
            isRetryable: false)
    {
    }
}
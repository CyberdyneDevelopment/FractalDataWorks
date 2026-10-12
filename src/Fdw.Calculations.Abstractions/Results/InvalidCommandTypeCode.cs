using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Command must be an ITransformationRequest.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "InvalidCommandType")]
[ExcludeFromCodeCoverage]
public sealed class InvalidCommandTypeCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCommandTypeCode"/> class.
    /// </summary>
    public InvalidCommandTypeCode()
        : base(21004, "InvalidCommandType",
            ResultSeverities.ByName("Error"),
            "Command must be ITransformationRequest",
            isRetryable: false)
    {
    }
}
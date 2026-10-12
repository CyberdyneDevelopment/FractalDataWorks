using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// The result list is empty, a result has no name, or the limits are not positive.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "ResultListInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ResultListInvalidCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultListInvalidCode"/> class.
    /// </summary>
    public ResultListInvalidCode()
        : base(
            22019,
            "ResultListInvalid",
            ResultSeverities.ByName("Error"),
            "Calculation tree refused at {node}: the result list is invalid. {reason}",
            isRetryable: false)
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// The result list or a tree is larger than the stated limits allow.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "LimitExceeded", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LimitExceededCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LimitExceededCode"/> class.
    /// </summary>
    public LimitExceededCode()
        : base(
            22017,
            "LimitExceeded",
            ResultSeverities.ByName("Error"),
            "Calculation tree refused at {node}: a stated limit is exceeded. {reason}",
            isRetryable: false)
    {
    }
}

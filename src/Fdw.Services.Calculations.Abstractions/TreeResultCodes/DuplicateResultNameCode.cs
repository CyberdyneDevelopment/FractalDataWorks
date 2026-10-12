using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// Two results in one list have the same name.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "DuplicateResultName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DuplicateResultNameCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateResultNameCode"/> class.
    /// </summary>
    public DuplicateResultNameCode()
        : base(
            22015,
            "DuplicateResultName",
            ResultSeverities.ByName("Error"),
            "Calculation tree refused at {node}: two results share a name. {reason}",
            isRetryable: false)
    {
    }
}

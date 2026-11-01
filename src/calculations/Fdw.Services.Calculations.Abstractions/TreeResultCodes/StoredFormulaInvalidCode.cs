using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// The stored rows of a built calculation do not form a calculation tree.
/// </summary>
[TypeOption(typeof(CalculationTreeResultCodes), "StoredFormulaInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StoredFormulaInvalidCode : CalculationTreeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoredFormulaInvalidCode"/> class.
    /// </summary>
    public StoredFormulaInvalidCode()
        : base(
            22020,
            "StoredFormulaInvalid",
            ResultSeverities.ByName("Error"),
            "Stored calculation refused at {node}: the formula rows are invalid. {reason}",
            isRetryable: false)
    {
    }
}

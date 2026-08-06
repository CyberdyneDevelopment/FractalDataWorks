using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.ResultCodes;

namespace Fdw.Services.Calculations;

/// <summary>
/// A Formula calculation entity requires a non-empty formula body.
/// The <c>FormulaBody</c> property was empty or whitespace only.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CalculationEntityResultCodes), "FormulaBodyRequired")]
public sealed class FormulaBodyRequiredCode : CalculationEntityResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="FormulaBodyRequiredCode"/>.
    /// </summary>
    public FormulaBodyRequiredCode()
        : base(
            20000,
            "FormulaBodyRequired",
            ResultSeverities.ByName("Error"),
            "FormulaBody is required and must not be empty for a Formula calculation entity.",
            isRetryable: false)
    {
    }
}

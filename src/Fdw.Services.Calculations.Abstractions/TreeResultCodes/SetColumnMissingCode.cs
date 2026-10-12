using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>A row lacks a column a calculation set needs.</summary>
[TypeOption(typeof(CalculationTreeResultCodes), "SetColumnMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetColumnMissingCode : CalculationTreeResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="SetColumnMissingCode"/> class.</summary>
    public SetColumnMissingCode()
        : base(
            22023,
            "SetColumnMissing",
            ResultSeverities.ByName("Error"),
            "Output '{output}' refused: a row has no value for a column it needs. {reason}",
            isRetryable: false)
    {
    }
}

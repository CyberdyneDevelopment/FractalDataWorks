using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>A calculation set reads a table the run did not supply.</summary>
[TypeOption(typeof(CalculationTreeResultCodes), "SetTableMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetTableMissingCode : CalculationTreeResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="SetTableMissingCode"/> class.</summary>
    public SetTableMissingCode()
        : base(
            22022,
            "SetTableMissing",
            ResultSeverities.ByName("Error"),
            "Output '{output}' refused: a table it reads was not supplied. {reason}",
            isRetryable: false)
    {
    }
}

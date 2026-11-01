using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>A lookup found no row for its key.</summary>
[TypeOption(typeof(CalculationTreeResultCodes), "SetLookupKeyNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetLookupKeyNotFoundCode : CalculationTreeResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="SetLookupKeyNotFoundCode"/> class.</summary>
    public SetLookupKeyNotFoundCode()
        : base(
            22024,
            "SetLookupKeyNotFound",
            ResultSeverities.ByName("Error"),
            "Output '{output}' refused: a lookup found no matching row. {reason}",
            isRetryable: false)
    {
    }
}

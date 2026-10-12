using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>A lookup table has two rows for one key.</summary>
[TypeOption(typeof(CalculationTreeResultCodes), "SetLookupKeyAmbiguous", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetLookupKeyAmbiguousCode : CalculationTreeResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="SetLookupKeyAmbiguousCode"/> class.</summary>
    public SetLookupKeyAmbiguousCode()
        : base(
            22025,
            "SetLookupKeyAmbiguous",
            ResultSeverities.ByName("Error"),
            "Output '{output}' refused: two rows share a lookup key. {reason}",
            isRetryable: false)
    {
    }
}

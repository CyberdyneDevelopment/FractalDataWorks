using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Chain ID is required before building.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "ChainIdRequired")]
[ExcludeFromCodeCoverage]
public sealed class ChainIdRequiredCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainIdRequiredCode"/> class.
    /// </summary>
    public ChainIdRequiredCode()
        : base(20000, "ChainIdRequired",
            ResultSeverities.ByName("Error"),
            "Chain ID is required. Call WithId() before Build().",
            isRetryable: false)
    {
    }
}
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Chain name is required before building.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "ChainNameRequired")]
[ExcludeFromCodeCoverage]
public sealed class ChainNameRequiredCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainNameRequiredCode"/> class.
    /// </summary>
    public ChainNameRequiredCode()
        : base(21000, "ChainNameRequired",
            ResultSeverities.ByName("Error"),
            "Chain name is required. Call WithName() before Build().",
            isRetryable: false)
    {
    }
}
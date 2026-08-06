using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Transformation engine not supported for this provider.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "EngineNotSupported")]
[ExcludeFromCodeCoverage]
public sealed class EngineNotSupportedCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EngineNotSupportedCode"/> class.
    /// </summary>
    public EngineNotSupportedCode()
        : base(61000, "EngineNotSupported",
            ResultSeverities.ByName("Error"),
            "Aggregation calculations do not require engines. Use Transform() directly.",
            isRetryable: false)
    {
    }
}
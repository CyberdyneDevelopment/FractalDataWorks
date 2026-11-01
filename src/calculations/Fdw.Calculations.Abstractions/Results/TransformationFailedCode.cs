using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Transformation execution failed.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "TransformationFailed")]
[ExcludeFromCodeCoverage]
public sealed class TransformationFailedCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransformationFailedCode"/> class.
    /// </summary>
    public TransformationFailedCode()
        : base(91002, "TransformationFailed",
            ResultSeverities.ByName("Error"),
            "Transformation failed: {Error}",
            isRetryable: false)
    {
    }
}
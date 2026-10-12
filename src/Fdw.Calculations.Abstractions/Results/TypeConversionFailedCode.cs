using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Cannot convert provider to requested type.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "TypeConversionFailed")]
[ExcludeFromCodeCoverage]
public sealed class TypeConversionFailedCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeConversionFailedCode"/> class.
    /// </summary>
    public TypeConversionFailedCode()
        : base(90002, "TypeConversionFailed",
            ResultSeverities.ByName("Error"),
            "Cannot convert {SourceType} to {TargetType}",
            isRetryable: false)
    {
    }
}
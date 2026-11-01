using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Configuration must be of the expected type.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "InvalidConfigurationType")]
[ExcludeFromCodeCoverage]
public sealed class InvalidConfigurationTypeCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationTypeCode"/> class.
    /// </summary>
    public InvalidConfigurationTypeCode()
        : base(21005, "InvalidConfigurationType",
            ResultSeverities.ByName("Error"),
            "Configuration must be CalculationTransformationConfiguration",
            isRetryable: false)
    {
    }
}
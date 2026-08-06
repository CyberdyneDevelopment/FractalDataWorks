using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.ResultCodes;

/// <summary>
/// The provided configuration type does not match the expected type for this calculation entity.
/// </summary>
[TypeOption(typeof(CalculationEntityResultCodes), "ConfigurationTypeMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConfigurationTypeMismatchCode : CalculationEntityResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationTypeMismatchCode"/> class.
    /// </summary>
    public ConfigurationTypeMismatchCode()
        : base(
            21000,
            "ConfigurationTypeMismatch",
            ResultSeverities.ByName("Error"),
            "Configuration must be of the expected type for this calculation entity",
            isRetryable: false)
    {
    }
}

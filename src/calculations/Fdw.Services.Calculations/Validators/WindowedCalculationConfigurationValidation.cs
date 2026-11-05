using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Calculations.Validators;

/// <summary>States that <see cref="WindowedCalculationConfiguration"/> is checked by <see cref="WindowedCalculationConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "WindowedCalculationConfiguration")]
public sealed class WindowedCalculationConfigurationValidation : ValidatedConfiguration<WindowedCalculationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WindowedCalculationConfigurationValidation"/> class.</summary>
    public WindowedCalculationConfigurationValidation()
        : base("WindowedCalculationConfiguration", new WindowedCalculationConfigurationValidator())
    {
    }
}

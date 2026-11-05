using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Calculations.Validators;

/// <summary>States that <see cref="CalculationStepConfigurationBase"/> (and what derives from or implements it) is checked by <see cref="CalculationStepConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "CalculationStepConfigurationBase")]
public sealed class CalculationStepConfigurationValidation : ValidatedConfiguration<CalculationStepConfigurationBase>
{
    /// <summary>Initializes a new instance of the <see cref="CalculationStepConfigurationValidation"/> class.</summary>
    public CalculationStepConfigurationValidation()
        : base("CalculationStepConfigurationBase", new CalculationStepConfigurationValidator())
    {
    }
}

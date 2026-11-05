using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Calculations.Validators;

/// <summary>States that <see cref="CalculationStepOperandConfigurationBase"/> (and what derives from or implements it) is checked by <see cref="CalculationStepOperandConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "CalculationStepOperandConfigurationBase")]
public sealed class CalculationStepOperandConfigurationValidation : ValidatedConfiguration<CalculationStepOperandConfigurationBase>
{
    /// <summary>Initializes a new instance of the <see cref="CalculationStepOperandConfigurationValidation"/> class.</summary>
    public CalculationStepOperandConfigurationValidation()
        : base("CalculationStepOperandConfigurationBase", new CalculationStepOperandConfigurationValidator())
    {
    }
}

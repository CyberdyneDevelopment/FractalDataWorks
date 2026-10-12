using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.FormulaCalculationStepField child table.</summary>
[TypeOption(typeof(ConfigurationCommands), "FormulaCalculationStepField")]
public sealed class FormulaCalculationStepFieldConfigurationCommand : ConfigurationCommandBase<FormulaCalculationStepFieldConfiguration>
{
    /// <inheritdoc/>
    public FormulaCalculationStepFieldConfigurationCommand() : base("FormulaCalculationStepField") { }
}

using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.FormulaCalculationStepOperand child table.</summary>
[TypeOption(typeof(ConfigurationCommands), "FormulaCalculationStepOperand")]
public sealed class FormulaCalculationStepOperandConfigurationCommand : ConfigurationCommandBase<FormulaCalculationStepOperandConfiguration>
{
    /// <inheritdoc/>
    public FormulaCalculationStepOperandConfigurationCommand() : base("FormulaCalculationStepOperand") { }
}

using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.CalculationStepOperand grandchild table.</summary>
[TypeOption(typeof(ConfigurationCommands), "CalculationStepOperand")]
public sealed class CalculationStepOperandConfigurationCommand : ConfigurationCommandBase<CalculationStepOperandConfiguration>
{
    /// <inheritdoc/>
    public CalculationStepOperandConfigurationCommand() : base("CalculationStepOperand") { }
}

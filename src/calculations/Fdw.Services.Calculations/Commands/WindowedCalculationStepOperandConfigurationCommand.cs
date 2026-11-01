using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.WindowedCalculationStepOperand child table.</summary>
[TypeOption(typeof(ConfigurationCommands), "WindowedCalculationStepOperand")]
public sealed class WindowedCalculationStepOperandConfigurationCommand : ConfigurationCommandBase<WindowedCalculationStepOperandConfiguration>
{
    /// <inheritdoc/>
    public WindowedCalculationStepOperandConfigurationCommand() : base("WindowedCalculationStepOperand") { }
}

using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.WindowedCalculationStepOperand child table.</summary>
[TypeOption(typeof(ConfigurationCommands), "WindowedCalculationStepOperand")]
public sealed class WindowedCalculationStepOperandConfigurationCommand : ConfigurationCommandBase<WindowedCalculationStepOperandConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WindowedCalculationStepOperandConfigurationCommand"/> class.</summary>
    public WindowedCalculationStepOperandConfigurationCommand() : base("WindowedCalculationStepOperand") { }
}

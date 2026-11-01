using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.WindowedCalculationStepField child table.</summary>
[TypeOption(typeof(ConfigurationCommands), "WindowedCalculationStepField")]
public sealed class WindowedCalculationStepFieldConfigurationCommand : ConfigurationCommandBase<WindowedCalculationStepFieldConfiguration>
{
    /// <inheritdoc/>
    public WindowedCalculationStepFieldConfigurationCommand() : base("WindowedCalculationStepField") { }
}

using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.WindowedCalculationInput child table.</summary>
[TypeOption(typeof(ConfigurationCommands), "WindowedCalculationInput")]
public sealed class WindowedCalculationInputConfigurationCommand : ConfigurationCommandBase<WindowedCalculationInputConfiguration>
{
    /// <inheritdoc/>
    public WindowedCalculationInputConfigurationCommand() : base("WindowedCalculationInput") { }
}

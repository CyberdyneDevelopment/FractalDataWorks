using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.WindowedCalculationConfiguration typed-body table.</summary>
[TypeOption(typeof(ConfigurationCommands), "WindowedCalculationConfiguration")]
public sealed class WindowedCalculationConfigurationCommand : ConfigurationCommandBase<WindowedCalculationConfiguration>
{
    /// <inheritdoc/>
    public WindowedCalculationConfigurationCommand() : base("WindowedCalculationConfiguration") { }
}

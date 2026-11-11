using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.WindowedCalculationConfiguration typed-body table.</summary>
[TypeOption(typeof(ConfigurationCommands), "WindowedCalculationConfiguration")]
public sealed class WindowedCalculationConfigurationCommand : ConfigurationCommandBase<WindowedCalculationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WindowedCalculationConfigurationCommand"/> class.</summary>
    public WindowedCalculationConfigurationCommand() : base("WindowedCalculationConfiguration") { }
}

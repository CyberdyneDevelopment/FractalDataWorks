using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.WindowedCalculation typed-body table.</summary>
[TypeOption(typeof(ConfigurationCommands), "WindowedCalculation")]
public sealed class WindowedCalculationConfigurationCommand : ConfigurationCommandBase<WindowedCalculationConfiguration>
{
    /// <inheritdoc/>
    public WindowedCalculationConfigurationCommand() : base("WindowedCalculation") { }
}

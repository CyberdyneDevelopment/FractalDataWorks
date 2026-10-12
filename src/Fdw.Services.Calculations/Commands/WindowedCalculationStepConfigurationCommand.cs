using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.WindowedCalculationStep child table.</summary>
[TypeOption(typeof(ConfigurationCommands), "WindowedCalculationStep")]
public sealed class WindowedCalculationStepConfigurationCommand : ConfigurationCommandBase<WindowedCalculationStepConfiguration>
{
    /// <inheritdoc/>
    public WindowedCalculationStepConfigurationCommand() : base("WindowedCalculationStep") { }
}

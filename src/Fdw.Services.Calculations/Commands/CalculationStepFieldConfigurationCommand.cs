using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.CalculationStepField grandchild table.</summary>
[TypeOption(typeof(ConfigurationCommands), "CalculationStepField")]
public sealed class CalculationStepFieldConfigurationCommand : ConfigurationCommandBase<CalculationStepFieldConfiguration>
{
    /// <inheritdoc/>
    public CalculationStepFieldConfigurationCommand() : base("CalculationStepField") { }
}

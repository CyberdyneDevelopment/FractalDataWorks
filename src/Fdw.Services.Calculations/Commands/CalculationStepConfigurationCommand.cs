using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.CalculationStep child table.</summary>
[TypeOption(typeof(ConfigurationCommands), "CalculationStep")]
public sealed class CalculationStepConfigurationCommand : ConfigurationCommandBase<CalculationStepConfiguration>
{
    /// <inheritdoc/>
    public CalculationStepConfigurationCommand() : base("CalculationStep") { }
}

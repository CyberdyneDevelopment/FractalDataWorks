using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.CalculationEntityInput child table.</summary>
[TypeOption(typeof(ConfigurationCommands), "CalculationEntityInput")]
public sealed class CalculationEntityInputConfigurationCommand : ConfigurationCommandBase<CalculationEntityInputRecord>
{
    /// <inheritdoc/>
    public CalculationEntityInputConfigurationCommand() : base("CalculationEntityInput") { }
}

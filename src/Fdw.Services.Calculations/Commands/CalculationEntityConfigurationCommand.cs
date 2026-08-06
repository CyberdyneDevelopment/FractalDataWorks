using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the CalculationEntity configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "CalculationEntity")]
public sealed class CalculationEntityConfigurationCommand : ConfigurationCommandBase<CalculationEntityConfiguration>
{
    /// <inheritdoc/>
    public CalculationEntityConfigurationCommand() : base("CalculationEntity") { }
}

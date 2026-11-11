using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the CalculationEntity configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "CalculationEntity")]
public sealed class CalculationEntityConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CalculationEntityConfigurationCommand"/> class.</summary>
    public CalculationEntityConfigurationCommand() : base("CalculationEntity") { }
}

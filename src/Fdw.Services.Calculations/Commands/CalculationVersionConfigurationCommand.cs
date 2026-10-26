using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>The command that reads and writes the versions of calculations held at places (FDW-2560).</summary>
[TypeOption(typeof(ConfigurationCommands), "CalculationVersion")]
public sealed class CalculationVersionConfigurationCommand : ConfigurationCommandBase<CalculationVersionImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="CalculationVersionConfigurationCommand"/> class.</summary>
    public CalculationVersionConfigurationCommand() : base("CalculationVersion") { }
}

using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calculation cache configuration.</summary>
[TypeOption(typeof(ConfigurationCommands), "CalculationCache")]
public sealed class CalculationCacheConfigurationCommand : ConfigurationCommandBase<CalculationCacheConfiguration>
{
    /// <inheritdoc/>
    public CalculationCacheConfigurationCommand() : base("CalculationCache") { }
}

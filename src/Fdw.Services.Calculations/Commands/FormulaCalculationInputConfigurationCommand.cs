using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.FormulaCalculationInput child table.</summary>
[TypeOption(typeof(ConfigurationCommands), "FormulaCalculationInput")]
public sealed class FormulaCalculationInputConfigurationCommand : ConfigurationCommandBase<FormulaCalculationInputConfiguration>
{
    /// <inheritdoc/>
    public FormulaCalculationInputConfigurationCommand() : base("FormulaCalculationInput") { }
}

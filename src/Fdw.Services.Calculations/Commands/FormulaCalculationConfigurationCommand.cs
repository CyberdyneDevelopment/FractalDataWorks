using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.FormulaCalculationConfiguration typed-body table.</summary>
[TypeOption(typeof(ConfigurationCommands), "FormulaCalculationConfiguration")]
public sealed class FormulaCalculationConfigurationCommand : ConfigurationCommandBase<FormulaCalculationConfiguration>
{
    /// <inheritdoc/>
    public FormulaCalculationConfigurationCommand() : base("FormulaCalculationConfiguration") { }
}

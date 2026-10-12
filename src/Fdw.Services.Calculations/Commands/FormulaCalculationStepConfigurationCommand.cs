using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Calculations.Commands;

/// <summary>ConfigurationCommands TypeOption for the calc.FormulaCalculationStep child table.</summary>
[TypeOption(typeof(ConfigurationCommands), "FormulaCalculationStep")]
public sealed class FormulaCalculationStepConfigurationCommand : ConfigurationCommandBase<FormulaCalculationStepConfiguration>
{
    /// <inheritdoc/>
    public FormulaCalculationStepConfigurationCommand() : base("FormulaCalculationStep") { }
}

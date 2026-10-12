using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// A step of a <see cref="FormulaCalculationConfiguration"/>, held in <c>calc.FormulaCalculationStep</c> by the
/// implementation's RowId.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Calculation")]
public partial class FormulaCalculationStepConfiguration : CalculationStepConfigurationBase, ICalculationStepConfiguration
{
    /// <summary>
    /// Gets or sets the fields referenced by this step (GROUP BY and ORDER BY roles).
    /// Composed from <c>calc.FormulaCalculationStepField</c> by the keystone cascade. Callers filter by
    /// <see cref="CalculationStepFieldConfigurationBase.StepFieldRole"/>.
    /// </summary>
#pragma warning disable MA0016
    public List<FormulaCalculationStepFieldConfiguration> Fields { get; set; } = [];

    /// <summary>
    /// Gets or sets the operands bound to this step. Composed from <c>calc.FormulaCalculationStepOperand</c>
    /// by the keystone cascade; cascade-saved on insert.
    /// </summary>
    public List<FormulaCalculationStepOperandConfiguration> Operands { get; set; } = [];
#pragma warning restore MA0016

    IReadOnlyList<CalculationStepFieldConfigurationBase> ICalculationStepConfiguration.Fields => Fields;

    IReadOnlyList<CalculationStepOperandConfigurationBase> ICalculationStepConfiguration.Operands => Operands;
}

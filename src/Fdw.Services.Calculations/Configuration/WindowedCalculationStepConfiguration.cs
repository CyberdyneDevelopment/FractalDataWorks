using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// A step of a <see cref="WindowedCalculationConfiguration"/>, held in <c>calc.WindowedCalculationStep</c> by the
/// implementation's RowId.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Calculation")]
public partial class WindowedCalculationStepConfiguration : CalculationStepConfigurationBase, ICalculationStepConfiguration
{
    /// <summary>
    /// Gets or sets the fields referenced by this step (GROUP BY and ORDER BY roles).
    /// Composed from <c>calc.WindowedCalculationStepField</c> by the keystone cascade. Callers filter by
    /// <see cref="CalculationStepFieldConfigurationBase.StepFieldRole"/>.
    /// </summary>
#pragma warning disable MA0016
    public List<WindowedCalculationStepFieldConfiguration> Fields { get; set; } = [];

    /// <summary>
    /// Gets or sets the operands bound to this step. Composed from <c>calc.WindowedCalculationStepOperand</c>
    /// by the keystone cascade; cascade-saved on insert.
    /// </summary>
    public List<WindowedCalculationStepOperandConfiguration> Operands { get; set; } = [];
#pragma warning restore MA0016

    IReadOnlyList<CalculationStepFieldConfigurationBase> ICalculationStepConfiguration.Fields => Fields;

    IReadOnlyList<CalculationStepOperandConfigurationBase> ICalculationStepConfiguration.Operands => Operands;
}

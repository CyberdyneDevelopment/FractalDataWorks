using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// An operand bound to a <see cref="FormulaCalculationStepConfiguration"/>, held in
/// <c>calc.FormulaCalculationStepOperand</c> by the step's RowId.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Calculation")]
public partial class FormulaCalculationStepOperandConfiguration : CalculationStepOperandConfigurationBase
{
}

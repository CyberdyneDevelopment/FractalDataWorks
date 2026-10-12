using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// A field a <see cref="FormulaCalculationStepConfiguration"/> groups or orders by, held in
/// <c>calc.FormulaCalculationStepField</c> by the step's RowId.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "CalculationStepField")]
public partial class FormulaCalculationStepFieldConfiguration : CalculationStepFieldConfigurationBase
{
}

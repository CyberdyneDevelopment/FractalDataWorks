using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// An input of a <see cref="FormulaCalculationConfiguration"/>, held in <c>calc.FormulaCalculationInput</c> by the
/// implementation's RowId.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class FormulaCalculationInputConfiguration : CalculationInputConfigurationBase
{
}

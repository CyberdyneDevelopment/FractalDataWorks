using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// A field a <see cref="WindowedCalculationStepConfiguration"/> groups or orders by, held in
/// <c>calc.WindowedCalculationStepField</c> by the step's RowId.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "CalculationStepField")]
public partial class WindowedCalculationStepFieldConfiguration : CalculationStepFieldConfigurationBase
{
}

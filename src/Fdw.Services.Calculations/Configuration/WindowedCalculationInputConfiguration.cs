using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// An input of a <see cref="WindowedCalculationConfiguration"/>, held in <c>calc.WindowedCalculationInput</c> by the
/// implementation's RowId.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class WindowedCalculationInputConfiguration : CalculationInputConfigurationBase
{
}

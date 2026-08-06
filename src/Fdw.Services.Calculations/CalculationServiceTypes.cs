using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Calculations;

/// <summary>
/// ServiceTypeCollection for calculation domain service types.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(CalculationServiceTypeBase),
    typeof(ICalculationServiceType),
    typeof(CalculationServiceTypes),
    ServiceCategory = "Calculation",
    RestrictToCurrentCompilation = true)]
public partial class CalculationServiceTypes : ServiceTypeCollectionBase<CalculationServiceTypeBase, ICalculationServiceType>
{
}

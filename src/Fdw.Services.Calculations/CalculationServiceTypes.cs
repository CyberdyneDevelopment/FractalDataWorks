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
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Quality;

/// <summary>
/// ServiceTypeCollection for quality domain service types.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(QualityServiceTypeBase),
    typeof(IQualityServiceType),
    typeof(QualityServiceTypes),
    ServiceCategory = "Quality",
    RestrictToCurrentCompilation = true)]
public partial class QualityServiceTypes : ServiceTypeCollectionBase<QualityServiceTypeBase, IQualityServiceType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

}

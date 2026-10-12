using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Data.Abstractions;

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
    public static string ConfigurationConnection => PlatformConnection.Name;

}

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

    /// <summary>
    /// The connection this domain's operational rows (the executions of its rules) live in. The host must set it; there is no default.
    /// </summary>
    /// <remarks>
    /// Deliberately without an initializer, as <c>EtlPipelineTypes.OperationalConnection</c> is: an operational store is a row
    /// INSIDE the platform configuration store, so a default would name a store the application merely hopes exists. The
    /// sites that record or read an execution report it when it is unset (FDW-1363).
    /// </remarks>
    public static string? OperationalConnection { get; set; }
}

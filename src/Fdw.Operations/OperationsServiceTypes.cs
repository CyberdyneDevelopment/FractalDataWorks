using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Operations;

/// <summary>
/// ServiceTypeCollection for operations domain service types.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(OperationsServiceTypeBase),
    typeof(IOperationsServiceType),
    typeof(OperationsServiceTypes),
    ServiceCategory = "Operations",
    RestrictToCurrentCompilation = true)]
public partial class OperationsServiceTypes : ServiceTypeCollectionBase<OperationsServiceTypeBase, IOperationsServiceType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

    /// <summary>
    /// The connection this domain's operational rows live in. The host must set it; there is no default.
    /// </summary>
    /// <remarks>
    /// Deliberately without an initializer, unlike <see cref="ConfigurationConnection"/>. That one may
    /// default because <c>PlatformConfiguration</c> is declared in <c>configurationSchema.json</c> and
    /// is therefore known before any row is read. An operational store is a row INSIDE that store, so a
    /// default here would name a store the application merely hopes exists — the absence the
    /// no-fallbacks rule exists to catch, rather than the ConfigurationConnection case it resembles.
    /// The Registration phase fails loud when this is unset.
    /// </remarks>
    public static string? OperationalConnection { get; set; }

}

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

}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Workspaces;

/// <summary>
/// ServiceTypeCollection for the place domain: the scope tree of tenant, organization and workspace, and the service that says which places are
/// above a place (FDW-2538).
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(PlaceServiceTypeBase),
    typeof(IPlaceServiceType),
    typeof(PlaceServiceTypes),
    ServiceCategory = "Places",
    RestrictToCurrentCompilation = true)]
public partial class PlaceServiceTypes : ServiceTypeCollectionBase<PlaceServiceTypeBase, IPlaceServiceType>
{
}

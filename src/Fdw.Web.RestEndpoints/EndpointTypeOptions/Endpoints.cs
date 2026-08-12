using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Web.RestEndpoints.EndpointTypeOptions;

/// <summary>
/// The parent of every endpoints collection.
/// </summary>
/// <remarks>
/// The level above a resource. ScheduleEndpoints holds the endpoints over schedules; this holds ScheduleEndpoints and its siblings, so there is a name for
/// "every one of these the application serves" that is not a list somebody maintains.
///
/// Its ServiceCategory is what puts it in PlatformServices and names the generated accessor, so
/// <c>PlatformServices.Endpoints</c> reaches all of them without going through any one domain's service
/// type. That is the level that was missing: a tag every the endpoints over one resource shares belongs on that resource's
/// collection, and something they all share belongs here. Before this, both had to be repeated per
/// member or hoisted into a host that owns neither.
///
/// TBase is the interface rather than a class because a non-generic base cannot be inserted:
/// each resource collection already derives a closed TypeCollectionBase, and that base slot is taken.
/// The interface is the only type they all are.
///
/// A resource collection joins by naming this one on its own attribute - TypeOption plus
/// TypeOptionName - so the relationship is declared beside the collection it describes rather than
/// centrally, where it drifts. This collection declares nothing about its members.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(IEndpointTypeCollection),
    typeof(IEndpointTypeCollection),
    typeof(Endpoints),
    ServiceCategory = "Endpoints")]
public partial class Endpoints : ServiceTypeCollectionBase<IEndpointTypeCollection, IEndpointTypeCollection>
{
}

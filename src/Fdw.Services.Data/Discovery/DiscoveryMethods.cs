using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Discovery;

/// <summary>
/// TypeCollection of container discovery methods.
/// Each entry defines a strategy for discovering containers within a data store.
///
/// The source generator populates ByName(), ById(), All(), NotFound() at compile time.
///
/// Usage:
///   var method = DiscoveryMethods.ByName("Auto");
///   bool canAutoDiscover = method.SupportsAutoDiscovery;
/// </summary>
[TypeCollection(typeof(DiscoveryMethodBase), typeof(IDiscoveryMethod), typeof(DiscoveryMethods))]
[ExcludeFromCodeCoverage]
public abstract partial class DiscoveryMethods
    : TypeCollectionBase<DiscoveryMethodBase, IDiscoveryMethod>
{
}

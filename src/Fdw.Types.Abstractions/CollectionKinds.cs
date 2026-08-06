using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Types;

/// <summary>
/// TypeCollection for the kinds of TypeCollections.
/// </summary>
/// <remarks>
/// Kinds:
/// <list type="bullet">
/// <item>Immutable - Standard TypeCollection (compile-time fixed)</item>
/// <item>Mutable - MutableTypeCollection (runtime registration supported)</item>
/// <item>Instance - TypeInstanceCollection (pre-created instances instead of types)</item>
/// <item>Service - ServiceTypeCollection (factory and configuration support)</item>
/// <item>MutableService - MutableServiceTypeCollection (runtime registration)</item>
/// <item>ServiceInstance - ServiceTypeInstanceCollection (pre-created service instances)</item>
/// </list>
/// </remarks>
[TypeCollection(typeof(CollectionKindBase), typeof(ICollectionKind), typeof(CollectionKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class CollectionKinds : TypeCollectionBase<CollectionKindBase, ICollectionKind>
{
    // Source generator creates:
    // - All() method
    // - ById() method
    // - ByName() method
    // - Static properties for each [TypeOption]
}

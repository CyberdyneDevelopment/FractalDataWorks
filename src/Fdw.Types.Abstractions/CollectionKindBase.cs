using Fdw.Collections;

namespace Fdw.Types;

/// <summary>
/// Base class for CollectionKind type options.
/// </summary>
public abstract class CollectionKindBase : TypeOptionBase<CollectionKindBase>, ICollectionKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionKindBase"/> class.
    /// </summary>
    /// <param name="name">The name of this collection kind.</param>
    protected CollectionKindBase(string name) : base(name)
    {
    }
}

using Fdw.Collections;

namespace Fdw.Types;

/// <summary>
/// Base class for CollectionKind type options.
/// </summary>
public abstract class CollectionKindBase : TypeOptionBase<int, CollectionKindBase>, ICollectionKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionKindBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this collection kind.</param>
    /// <param name="name">The name of this collection kind.</param>
    protected CollectionKindBase(int id, string name) : base(id, name)
    {
    }
}

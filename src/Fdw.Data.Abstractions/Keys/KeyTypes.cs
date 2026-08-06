using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// TypeCollection for key types that define field roles in key relationships.
/// </summary>
/// <remarks>
/// Why: Replaces string-based KeyType and the IsPrimaryKey flag on fields.
/// Each TypeOption carries behavioral flags that translators read to determine
/// how to handle the key: generate constraints, build WHERE clauses, resolve
/// FK subqueries, or discover join paths.
/// </remarks>
[TypeCollection(typeof(KeyTypeBase), typeof(IKeyType), typeof(KeyTypes), RestrictToCurrentCompilation = false)]
public abstract partial class KeyTypes : TypeCollectionBase<KeyTypeBase, IKeyType>
{
}

using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>The kinds of owner a data configuration rule has: an organization or a workspace (FDW-6454).</summary>
[TypeCollection(typeof(DataOptionOwnerKindBase), typeof(IDataOptionOwnerKind), typeof(DataOptionOwnerKinds))]
public abstract partial class DataOptionOwnerKinds : TypeCollectionBase<DataOptionOwnerKindBase, IDataOptionOwnerKind>
{
}

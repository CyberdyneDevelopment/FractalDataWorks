using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>The kinds of value a data configuration rule holds: a literal or a parameter a run supplies (FDW-6454).</summary>
[TypeCollection(typeof(DataOptionValueKindBase), typeof(IDataOptionValueKind), typeof(DataOptionValueKinds))]
public abstract partial class DataOptionValueKinds : TypeCollectionBase<DataOptionValueKindBase, IDataOptionValueKind>
{
}

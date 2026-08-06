using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.DataDestination;

/// <summary>
/// Collection of data destination kind types.
/// </summary>
[TypeCollection(typeof(DataDestinationKindBase), typeof(IDataDestinationKind), typeof(DataDestinationKinds))]
public abstract partial class DataDestinationKinds : TypeCollectionBase<DataDestinationKindBase, IDataDestinationKind>
{
}

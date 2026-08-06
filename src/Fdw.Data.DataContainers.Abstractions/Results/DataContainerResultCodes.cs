using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataContainers.Abstractions.Results;

/// <summary>
/// TypeCollection for DataContainer result codes.
/// EventId range: 4100-4199 (Data.DataContainers domain)
/// </summary>
[TypeCollection(typeof(DataContainerResultCodeBase), typeof(IResultCode), typeof(DataContainerResultCodes))]
public abstract partial class DataContainerResultCodes : TypeCollectionBase<DataContainerResultCodeBase, IResultCode>
{
}
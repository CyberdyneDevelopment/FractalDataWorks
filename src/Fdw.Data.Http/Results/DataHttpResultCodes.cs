using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Http.Results;

/// <summary>
/// TypeCollection for Data.Http result codes.
/// EventId range: 5670-5689 (within Data domain)
/// </summary>
[TypeCollection(typeof(DataHttpResultCodeBase), typeof(IResultCode), typeof(DataHttpResultCodes))]
public abstract partial class DataHttpResultCodes : TypeCollectionBase<DataHttpResultCodeBase, IResultCode>
{
}
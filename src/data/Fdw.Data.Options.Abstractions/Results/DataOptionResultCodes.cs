using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Options.Results;

/// <summary>
/// TypeCollection for data option result codes. A code's number is its Id and its EventId, and its Code is
/// <c>DATAOPTION-{number}</c>.
/// </summary>
[TypeCollection(typeof(DataOptionResultCodeBase), typeof(IResultCode), typeof(DataOptionResultCodes))]
public abstract partial class DataOptionResultCodes : TypeCollectionBase<DataOptionResultCodeBase, IResultCode>
{
}

using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// TypeCollection for DataStores domain result codes (category 2 / Validation — bad input to
/// DataLocation/DataPath addressing).
/// </summary>
[TypeCollection(typeof(DataStoresResultCodeBase), typeof(IResultCode), typeof(DataStoresResultCodes))]
public abstract partial class DataStoresResultCodes : TypeCollectionBase<DataStoresResultCodeBase, IResultCode>
{
}

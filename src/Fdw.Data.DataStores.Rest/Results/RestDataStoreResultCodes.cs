using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataStores.Rest.Results;

/// <summary>
/// TypeCollection for REST DataStore result codes.
/// Codes use categorized numbers (Id == EventId == number, Code == "REST-{number}").
/// </summary>
[TypeCollection(typeof(RestDataStoreResultCodeBase), typeof(IResultCode), typeof(RestDataStoreResultCodes))]
public abstract partial class RestDataStoreResultCodes : TypeCollectionBase<RestDataStoreResultCodeBase, IResultCode>
{
}


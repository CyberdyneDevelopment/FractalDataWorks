using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Web.RestEndpoints.ApiReference.Results;

/// <summary>
/// TypeCollection for the API reference's result codes.
/// Codes use the categorized-number scheme: Id == EventId == number, Code == "APIREF-{number}".
/// </summary>
[TypeCollection(typeof(ApiReferenceResultCodeBase), typeof(IResultCode), typeof(ApiReferenceResultCodes))]
public abstract partial class ApiReferenceResultCodes : TypeCollectionBase<ApiReferenceResultCodeBase, IResultCode>
{
}

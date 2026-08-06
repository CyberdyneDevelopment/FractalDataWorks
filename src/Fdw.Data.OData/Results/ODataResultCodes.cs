using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.OData.Results;

/// <summary>
/// TypeCollection for REST data translator result codes.
/// Codes use categorized numbers (Id == EventId == number, Code == "REST-{number}").
/// </summary>
[TypeCollection(typeof(RestDataResultCodeBase), typeof(IResultCode), typeof(ODataResultCodes))]
public abstract partial class ODataResultCodes : TypeCollectionBase<RestDataResultCodeBase, IResultCode>
{
}
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.OpenApi.Results;

/// <summary>
/// TypeCollection for OpenAPI translator result codes.
/// EventId range: 4350-4399 (within Data.OpenApi)
/// </summary>
[TypeCollection(typeof(OpenApiResultCodeBase), typeof(IResultCode), typeof(OpenApiResultCodes))]
public abstract partial class OpenApiResultCodes : TypeCollectionBase<OpenApiResultCodeBase, IResultCode>
{
}
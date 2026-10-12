using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// TypeCollection for HTTP connection result codes.
/// EventId range: 5300-5399 (within Connections 5000-5999)
/// </summary>
[TypeCollection(typeof(HttpResultCodeBase), typeof(IResultCode), typeof(HttpResultCodes))]
public abstract partial class HttpResultCodes : TypeCollectionBase<HttpResultCodeBase, IResultCode>
{
}
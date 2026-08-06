using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// TypeCollection for MsSql connection result codes.
/// EventId range: 5100-5199 (within Connections 5000-5999)
/// </summary>
[TypeCollection(typeof(MsSqlResultCodeBase), typeof(IResultCode), typeof(MsSqlResultCodes))]
public abstract partial class MsSqlResultCodes : TypeCollectionBase<MsSqlResultCodeBase, IResultCode>
{
}

using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// TypeCollection for MsSql Connection result codes.
/// EventId range: 5200-5299 (within Services.Connections.MsSql domain)
/// </summary>
[TypeCollection(typeof(MsSqlConnectionResultCodeBase), typeof(IResultCode), typeof(MsSqlConnectionResultCodes))]
public abstract partial class MsSqlConnectionResultCodes : TypeCollectionBase<MsSqlConnectionResultCodeBase, IResultCode>
{
}

using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>Result codes raised by the connections domain.</summary>
[TypeCollection(typeof(ConnectionsResultCodeBase), typeof(IResultCode), typeof(ConnectionsResultCodes))]
public abstract partial class ConnectionsResultCodes : TypeCollectionBase<ConnectionsResultCodeBase, IResultCode>
{
}

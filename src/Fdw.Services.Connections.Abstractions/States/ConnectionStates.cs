using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Collection of connection states.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ConnectionStateBase), typeof(IConnectionState), typeof(ConnectionStates))]
public abstract partial class ConnectionStates : TypeCollectionBase<ConnectionStateBase, IConnectionState>
{

}
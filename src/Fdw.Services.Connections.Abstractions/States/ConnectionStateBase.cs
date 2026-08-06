using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Base class for connection states in the Fdw framework.
/// </summary>
/// <remarks>
/// Connection states help track the lifecycle of external connections and enable
/// proper connection management, pooling, and error handling throughout the framework.
/// </remarks>
public abstract class ConnectionStateBase : TypeOptionBase<int, ConnectionStateBase>, IConnectionState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionStateBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this connection state.</param>
    /// <param name="name">The name of this connection state.</param>
    protected ConnectionStateBase(int id, string name) : base(id, name)
    {
    }
}
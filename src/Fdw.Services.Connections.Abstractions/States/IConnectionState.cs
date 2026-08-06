using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Interface for connection states.
/// </summary>
public interface IConnectionState : ITypeOption<int, ConnectionStateBase>
{
}
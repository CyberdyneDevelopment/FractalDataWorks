using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.TUI.Management.Services;

/// <summary>
/// Tracks the Fdw instances this TUI can talk to, and which one is currently connected.
/// </summary>
/// <remarks>
/// The saved-instance list is deliberately client-side state — the endpoint and credential you point
/// the tool at, the way a CLI keeps profiles locally. It is NOT the Fdw Connections domain: those are
/// configuration records reached over the API through <c>ConnectionApiClient</c>.
/// </remarks>
public interface IConnectionManager
{
    /// <summary>
    /// Gets the current connection status.
    /// </summary>
    ConnectionStatus GetStatus();

    /// <summary>
    /// Gets the instance currently connected to, or <c>null</c> when disconnected.
    /// </summary>
    /// <remarks>
    /// The API-client plumbing reads this: the access-token provider takes its credential from here and
    /// the routing handler takes the base address from here, so switching instances at runtime needs no
    /// container rebuild.
    /// </remarks>
    SavedConnection? GetCurrentConnection();

    /// <summary>
    /// Gets a list of saved connections.
    /// </summary>
    IReadOnlyList<SavedConnection> GetSavedConnections();

    /// <summary>
    /// Saves a connection for later use.
    /// </summary>
    void SaveConnection(SavedConnection connection);

    /// <summary>
    /// Removes a saved connection.
    /// </summary>
    void RemoveConnection(string name);

    /// <summary>
    /// Connects to a Fdw instance, verifying it is reachable and that the credential is accepted.
    /// </summary>
    Task<ConnectionResult> Connect(SavedConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the current instance.
    /// </summary>
    void Disconnect();
}

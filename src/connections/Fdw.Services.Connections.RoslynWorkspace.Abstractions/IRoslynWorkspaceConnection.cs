using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions;

/// <summary>
/// Marker interface for a RoslynWorkspace connection.
/// Exposes the solution path, mode, and the typed primitive client.
/// The underlying <c>IRoslynWorkspace</c> is held internally — not exposed publicly.
/// </summary>
public interface IRoslynWorkspaceConnection : IGenericConnection
{
    /// <summary>
    /// Gets the typed primitive client for symbol and graph operations.
    /// Connectors call this directly per the §1.1 canary experiment.
    /// </summary>
    IRoslynWorkspaceClient Client { get; }

    /// <summary>
    /// Gets the solution file path that this connection was opened from.
    /// </summary>
    string SolutionPath { get; }

    /// <summary>
    /// Gets the operating mode — Live (workspace resident) or Snapshot (load-query-dispose).
    /// </summary>
    RoslynWorkspaceModeBase Mode { get; }
}

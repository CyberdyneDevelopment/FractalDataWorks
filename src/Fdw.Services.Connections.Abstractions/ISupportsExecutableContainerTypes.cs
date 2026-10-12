using System.Collections.Generic;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Opt-in capability interface for connection types whose translator can execute a container: the container types
/// the Execute command can run against.
/// </summary>
/// <remarks>
/// A connection type lists the Execute capability in its supported commands only when it implements this interface
/// with at least one container type. What executing means for a type (a stored procedure on SQL Server, an endpoint
/// for HTTP) is decided only by that connection type's translator; callers just offer the containers of these types.
/// </remarks>
public interface ISupportsExecutableContainerTypes
{
    /// <summary>
    /// Gets the container type names the Execute command can run against on this connection type.
    /// </summary>
    IReadOnlyList<string> ExecutableContainerTypes { get; }
}

using System.Collections.Generic;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Opt-in capability interface for connection types that declare which storage container types they support.
/// </summary>
/// <remarks>
/// Connection types implement this interface to enumerate the container kinds (e.g. Table, View, Function)
/// that are accessible via the connection. Consistent with the <c>ISupportsCalculationPushdown</c> capability pattern.
/// </remarks>
public interface ISupportsContainerTypes
{
    /// <summary>
    /// Gets the container type names (e.g. "Table", "View", "StoredProcedure") supported by this connection type.
    /// </summary>
    IReadOnlyList<string> SupportedContainerTypes { get; }
}

using System.Collections.Generic;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Opt-in capability interface for connection types that declare which wire protocols they support.
/// </summary>
/// <remarks>
/// Connection types implement this interface to enumerate the protocol names a caller may select
/// (e.g. "Rest", "GraphQL", "Soap11" for an HTTP connection). Consistent with the
/// <c>ISupportsCalculationPushdown</c> capability pattern.
/// </remarks>
public interface ISupportsProtocols
{
    /// <summary>
    /// Gets the protocol names supported by this connection type.
    /// </summary>
    IReadOnlyList<string> SupportedProtocols { get; }
}

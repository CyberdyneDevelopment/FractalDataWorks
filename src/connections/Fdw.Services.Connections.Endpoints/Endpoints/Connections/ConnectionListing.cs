using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// A connection of a listed page with its disabled state (FDW-4290): the page's states are read together, so each row is
/// handed on with its own.
/// </summary>
/// <param name="Connection">The connection, read as a record.</param>
/// <param name="Disabled">The state when the connection is disabled; nothing when it is not.</param>
public sealed record ConnectionListing(IConnectionImplementationConfiguration Connection, ConnectionDisabledRecord? Disabled);

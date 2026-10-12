using System;
using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Typed lookup request for a connection — identifies the connection being requested by
/// logical Id and/or Name. Used by new lookup surfaces so a connection request is
/// distinguishable by type from other domain requests (e.g. a DataStore request).
/// </summary>
/// <param name="Id">The connection's logical Id, or null when requesting by name.</param>
/// <param name="Name">The connection's name, or null when requesting by Id.</param>
/// <remarks>
/// A request with neither Id nor Name is invalid — consumers fail loud with a structured
/// result; there is no fallback resolution.
/// </remarks>
public sealed record ConnectionRequest(Guid? Id, string? Name) : ITypeRequest<Guid, ConnectionRequest>
{
    object? ITypeRequest.Id => Id;
}

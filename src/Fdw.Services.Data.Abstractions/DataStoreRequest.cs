using System;
using Fdw.Collections;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Typed lookup request for a data store — identifies the store being requested by
/// logical Id and/or Name. Used by new lookup surfaces so a data store request is
/// distinguishable by type from other domain requests (e.g. a Connection request).
/// </summary>
/// <param name="Id">The data store's logical Id, or null when requesting by name.</param>
/// <param name="Name">The data store's name, or null when requesting by Id.</param>
/// <remarks>
/// A request with neither Id nor Name is invalid — consumers fail loud with a structured
/// result; there is no fallback resolution.
/// </remarks>
public sealed record DataStoreRequest(Guid? Id, string? Name) : ITypeRequest<Guid, DataStoreRequest>
{
    object? ITypeRequest.Id => Id;
}

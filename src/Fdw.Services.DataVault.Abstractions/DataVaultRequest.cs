using System;
using Fdw.Collections;

namespace Fdw.Services.DataVault.Abstractions;

/// <summary>
/// Typed lookup request for a data vault — identifies the vault being requested by
/// logical Id and/or Name. Used so a vault request is distinguishable by type from
/// other domain requests (e.g. a Connection or DataStore request).
/// </summary>
/// <param name="Id">The vault's logical Id, or null when requesting by name.</param>
/// <param name="Name">The vault's name, or null when requesting by Id.</param>
/// <remarks>
/// A request with neither Id nor Name is invalid — consumers fail loud with a structured
/// result; there is no fallback resolution.
/// </remarks>
public sealed record DataVaultRequest(Guid? Id, string? Name) : ITypeRequest<Guid, DataVaultRequest>
{
    object? ITypeRequest.Id => Id;
}

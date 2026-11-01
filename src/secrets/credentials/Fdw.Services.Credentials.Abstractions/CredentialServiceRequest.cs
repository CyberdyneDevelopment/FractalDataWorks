using System;
using Fdw.Collections;

namespace Fdw.Services.Credentials.Abstractions;

/// <summary>
/// Typed lookup request for a credential service — identifies the service being requested by
/// logical Id and/or Name. Used so a credential service request is distinguishable by type from
/// other domain requests (e.g. a DataVault or SecretManager request).
/// </summary>
/// <param name="Id">The service's logical Id, or null when requesting by name.</param>
/// <param name="Name">The service's name, or null when requesting by Id.</param>
/// <remarks>
/// A request with neither Id nor Name is invalid — consumers fail loud with a structured
/// result; there is no fallback resolution.
/// </remarks>
public sealed record CredentialServiceRequest(Guid? Id, string? Name) : ITypeRequest<Guid, CredentialServiceRequest>
{
    object? ITypeRequest.Id => Id;
}

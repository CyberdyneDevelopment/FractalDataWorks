using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Generic base endpoint for listing available secret manager types from the source-generated collection.
/// </summary>
public abstract class ListSecretManagerTypesEndpointBase : CrudListEndpointBase<SecretManagerTypeDto>
{
    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "secret-manager-types";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "secret-managers:read";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List available secret manager types";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns all secret manager types registered via source-generated ServiceTypeCollections.";

    /// <summary>Loads all registered secret manager types as summary DTOs.</summary>
    protected override Task<IGenericResult<List<SecretManagerTypeDto>>> LoadItems(CancellationToken ct)
    {
        var items = SecretManagerTypes.All()
            .Select(kvp => MapToDto(kvp.Value))
            .ToList();
        return Task.FromResult(GenericResult<List<SecretManagerTypeDto>>.Success(items));
    }

    /// <summary>Maps a single secret manager type to a DTO.</summary>
    protected virtual SecretManagerTypeDto MapToDto(ISecretManagerType secretManagerType)
    {
        return new SecretManagerTypeDto
        {
            Name = secretManagerType.Name,
            Description = $"Secret manager type: {secretManagerType.Name}"
        };
    }
}

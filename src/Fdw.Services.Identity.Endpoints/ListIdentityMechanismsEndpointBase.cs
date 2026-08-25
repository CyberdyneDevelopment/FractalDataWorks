using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Identity.Abstractions;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Identity.Endpoints;

/// <summary>
/// Lists the managed-identity mechanisms registered in this deployment.
/// </summary>
/// <remarks>
/// Read from the source-generated collection rather than a hand-kept list, so a mechanism added by
/// referencing a package appears here without this endpoint changing. A hardcoded list would close a
/// set the framework deliberately keeps open.
/// </remarks>
public abstract class ListIdentityMechanismsEndpointBase : CrudListEndpointBase<IdentityMechanismDto>
{
    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "identity-mechanisms";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "identities:read";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List available managed identity mechanisms";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns every managed identity mechanism registered via source-generated ServiceTypeCollections.";

    /// <summary>Loads all registered identity mechanisms.</summary>
    /// <param name="ct">Propagated cancellation token.</param>
    /// <returns>The registered mechanisms.</returns>
    protected override Task<IGenericResult<List<IdentityMechanismDto>>> LoadItems(CancellationToken ct)
        => Task.FromResult(GenericResult<List<IdentityMechanismDto>>.Success(
            IdentityServiceTypes.All().Select(kvp => MapToDto(kvp.Value)).ToList()));

    /// <summary>Maps a single mechanism to a DTO.</summary>
    /// <param name="mechanism">The mechanism to map.</param>
    /// <returns>The DTO.</returns>
    protected virtual IdentityMechanismDto MapToDto(IIdentityServiceType mechanism)
        => new() { Name = mechanism.Name, Description = $"Managed identity mechanism: {mechanism.Name}" };
}

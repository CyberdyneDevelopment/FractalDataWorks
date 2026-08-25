using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Identity.Endpoints;

/// <summary>
/// Lists the managed identities configured in this deployment.
/// </summary>
/// <remarks>
/// Returns no credential and no token — see <see cref="IdentitySummaryResponse"/>.
/// </remarks>
public abstract class ListIdentitiesEndpointBase : CrudListEndpointBase<IdentitySummaryResponse>
{
    /// <summary>Gets the configuration provider identities are read through.</summary>
    protected abstract IServiceConfigurationProvider<IdentityServiceConfiguration> Identities { get; }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "identities";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "identities:read";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List configured managed identities";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns every configured managed identity. Never returns credentials or tokens.";

    /// <summary>Loads all configured identities.</summary>
    /// <param name="ct">Propagated cancellation token.</param>
    /// <returns>The configured identities.</returns>
    protected override async Task<IGenericResult<List<IdentitySummaryResponse>>> LoadItems(CancellationToken ct)
    {
        var configured = await Identities.Get(ct).ConfigureAwait(false);
        return configured.IsSuccess && configured.Value is { } identities
            ? GenericResult<List<IdentitySummaryResponse>>.Success(identities.Select(MapToSummary).ToList())
            : configured.ToNewResult<List<IdentitySummaryResponse>>();
    }

    /// <summary>Maps a configuration header to its summary view.</summary>
    /// <param name="configuration">The identity configuration.</param>
    /// <returns>The summary.</returns>
    protected virtual IdentitySummaryResponse MapToSummary(IdentityServiceConfiguration configuration)
        => new()
        {
            Id = configuration.Id,
            Name = configuration.Name,
            Mechanism = configuration.ServiceOptionType,
            Description = configuration.Description,
        };
}

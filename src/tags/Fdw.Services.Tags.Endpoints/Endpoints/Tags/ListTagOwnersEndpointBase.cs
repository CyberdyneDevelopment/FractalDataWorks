using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Tags.Endpoints;

/// <summary>
/// Lists the owners a tag can be given to: the tenant the caller acts in, and the organizations and workspaces the tenant-scoped providers
/// return (FDW-4100). It does not filter by the caller's own membership. The kinds themselves are the <c>TagOwnerKinds</c> collection, which the
/// type-collections route lists.
/// </summary>
/// <remarks>Why one route and not a console rule: a console that names the kinds and finds the owners itself repeats what the platform knows.</remarks>
public abstract class ListTagOwnersEndpointBase : CrudListEndpointBase<TagOwnerResponse>
{
    private readonly ITagOwners _owners;

    /// <summary>Initializes a new instance of the <see cref="ListTagOwnersEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="owners">The service that lists the owners.</param>
    protected ListTagOwnersEndpointBase(ILogger<ListTagOwnersEndpointBase> logger, ITagOwners owners)
        : base(logger)
    {
        _owners = owners;
    }

    /// <inheritdoc />
    protected override string ResourceName => "tags";

    /// <inheritdoc />
    protected override string Route => "/tags/owners";

    /// <inheritdoc />
    protected override string EndpointSummary => "List the owners a tag can be given to";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<TagOwnerResponse>>> LoadItems(CancellationToken ct)
    {
        var owners = await _owners.List(ct).ConfigureAwait(false);
        return owners.IsSuccess && owners.Value is { } all
            ? GenericResult<List<TagOwnerResponse>>.Success([.. all.Select(owner => new TagOwnerResponse { Kind = owner.Kind, Id = owner.Id, Name = owner.Name })])
            : owners.ToNewResult<List<TagOwnerResponse>>();
    }
}

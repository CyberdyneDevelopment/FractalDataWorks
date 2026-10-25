using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Workspaces;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Endpoint that lists the versions of a glossary term at the places the caller may read, nearest the caller first (FDW-2331). The place is the workspace
/// the caller names, else the organization they act in, else their tenant; the term itself is the tenant's version, and the nearest version is the one that applies.
/// </summary>
public abstract class ListGlossaryTermVersionsEndpointBase : Endpoint<GlossaryTermVersionsRequest, GlossaryTermVersionsResponse>
{
    private readonly GlossaryTermVersionServices _services;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IPlaceNames _names;

    /// <summary>Initializes a new instance of the <see cref="ListGlossaryTermVersionsEndpointBase"/> class.</summary>
    /// <param name="terms">Reads the glossary terms.</param>
    /// <param name="versions">Reads and writes the versions of terms held at places.</param>
    /// <param name="ancestry">Says which places are at or above a place.</param>
    /// <param name="authority">Says whether the caller holds a right at a place.</param>
    /// <param name="authContext">Says who is signed in and the place they act in.</param>
    /// <param name="names">Says the name of an organization or a workspace, which labels a version held there.</param>
    protected ListGlossaryTermVersionsEndpointBase(
        IGlossaryTermConfigurationProvider terms,
        IGlossaryTermVersionConfigurationProvider versions,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IAuthenticationContextAccessor authContext,
        IPlaceNames names)
    {
        _services = new GlossaryTermVersionServices(terms, versions, ancestry, authority);
        _authContext = authContext;
        _names = names ?? throw new System.ArgumentNullException(nameof(names));
    }

    /// <summary>Gets the authorization policy and scope right required for read operations.</summary>
    protected virtual string ReadPolicy => "datastores:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/catalog/glossary/{Id}/versions");
        Policies(ReadPolicy);
        Summary(s => s.Summary = "List the versions of a glossary term at the places the caller may read");
    }

    /// <summary>Lists the versions, or answers why they cannot be listed.</summary>
    public override async Task HandleAsync(GlossaryTermVersionsRequest req, CancellationToken ct)
    {
        if (CallerPlaces.Of(_authContext, req.WorkspaceId) is not { } origin)
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        var listed = await GlossaryTermVersionList.Run(_services, _names, req.Id, origin, ReadPolicy, Logger, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is not { } response)
        {
            await Send.ResultAsync(GlossaryCategoryLinks.Failure(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }
}

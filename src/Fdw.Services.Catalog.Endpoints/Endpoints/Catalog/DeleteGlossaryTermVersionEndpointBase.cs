using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Quality;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Endpoint that deletes the version of a glossary term held at an organization or a workspace (FDW-2331). The term itself is not touched.
/// </summary>
public abstract class DeleteGlossaryTermVersionEndpointBase : Endpoint<GlossaryTermVersionPlaceRequest>
{
    private readonly GlossaryTermVersionServices _services;
    private readonly IAuthenticationContextAccessor _authContext;

    /// <summary>Initializes a new instance of the <see cref="DeleteGlossaryTermVersionEndpointBase"/> class.</summary>
    /// <param name="terms">Reads the glossary terms.</param>
    /// <param name="versions">Reads and writes the versions of terms held at places.</param>
    /// <param name="ancestry">Says which places are at or above a place.</param>
    /// <param name="authority">Says whether the caller holds a right at a place.</param>
    /// <param name="authContext">Says who is signed in and the place they act in.</param>
    protected DeleteGlossaryTermVersionEndpointBase(
        IGlossaryTermConfigurationProvider terms,
        IGlossaryTermVersionConfigurationProvider versions,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IAuthenticationContextAccessor authContext)
    {
        _services = new GlossaryTermVersionServices(terms, versions, ancestry, authority);
        _authContext = authContext;
    }

    /// <summary>Gets the authorization policy and scope right required for write operations.</summary>
    protected virtual string WritePolicy => "datastores:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Delete("/catalog/glossary/{Id}/versions/{PlaceKind}/{PlaceId}");
        Policies(WritePolicy);
        Summary(s => s.Summary = "Delete a version of a glossary term from an organization or workspace");
    }

    /// <summary>Deletes the version and answers 204, or answers why it cannot be deleted.</summary>
    public override async Task HandleAsync(GlossaryTermVersionPlaceRequest req, CancellationToken ct)
    {
        if (_authContext.Current is not { IsAuthenticated: true, ActiveTenantId: { } tenantId })
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        var deleted = await GlossaryTermVersionWrite.Delete(_services, req, tenantId, WritePolicy, Logger, ct).ConfigureAwait(false);
        if (deleted.IsFailure)
        {
            await Send.ResultAsync(GlossaryCategoryLinks.Failure(deleted, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}

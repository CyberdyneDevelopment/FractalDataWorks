using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Calculations;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Endpoint that lists the versions of a calculation at the places the caller may read, nearest the caller first (FDW-2560). The place is the workspace
/// the caller names, else the organization they act in, else their tenant; the calculation itself is the tenant's version, and the nearest version is the
/// one that applies.
/// </summary>
public abstract class ListCalculationVersionsEndpointBase : Endpoint<CalculationVersionsRequest, CalculationVersionsResponse>
{
    private readonly CalculationVersionServices _services;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IPlaceNames _names;

    /// <summary>Initializes a new instance of the <see cref="ListCalculationVersionsEndpointBase"/> class.</summary>
    /// <param name="calculations">Reads the calculations.</param>
    /// <param name="versions">Reads and writes the versions of calculations held at places.</param>
    /// <param name="ancestry">Says which places are at or above a place.</param>
    /// <param name="authority">Says whether the caller holds a right at a place.</param>
    /// <param name="authContext">Says who is signed in and the place they act in.</param>
    /// <param name="names">Says the name of an organization or a workspace, which labels a version held there.</param>
    protected ListCalculationVersionsEndpointBase(
        ICalculationConfigurationProvider calculations,
        ICalculationVersionConfigurationProvider versions,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IAuthenticationContextAccessor authContext,
        IPlaceNames names)
    {
        _services = new CalculationVersionServices(calculations, versions, ancestry, authority);
        _authContext = authContext;
        _names = names ?? throw new System.ArgumentNullException(nameof(names));
    }

    /// <summary>Gets the authorization policy and scope right required for read operations.</summary>
    protected virtual string ReadPolicy => "calculation-entities:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/calculation-entities/{Id}/versions");
        Policies(ReadPolicy);
        Summary(s => s.Summary = "List the versions of a calculation at the places the caller may read");
    }

    /// <summary>Lists the versions, or answers why they cannot be listed.</summary>
    public override async Task HandleAsync(CalculationVersionsRequest req, CancellationToken ct)
    {
        if (CallerPlaces.Of(_authContext, req.WorkspaceId) is not { } origin)
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        var listed = await CalculationVersionList.Run(_services, _names, req.Id, origin, ReadPolicy, Logger, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is not { } response)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }
}

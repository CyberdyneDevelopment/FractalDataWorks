using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Calculations;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Endpoint that deletes the version of a calculation held at an organization or a workspace (FDW-2560). The calculation itself is not touched.
/// </summary>
public abstract class DeleteCalculationVersionEndpointBase : Endpoint<CalculationVersionPlaceRequest>
{
    private readonly CalculationVersionServices _services;
    private readonly IAuthenticationContextAccessor _authContext;

    /// <summary>Initializes a new instance of the <see cref="DeleteCalculationVersionEndpointBase"/> class.</summary>
    /// <param name="calculations">Reads the calculations.</param>
    /// <param name="versions">Reads and writes the versions of calculations held at places.</param>
    /// <param name="ancestry">Says which places are at or above a place.</param>
    /// <param name="authority">Says whether the caller holds a right at a place.</param>
    /// <param name="authContext">Says who is signed in and the place they act in.</param>
    protected DeleteCalculationVersionEndpointBase(
        ICalculationConfigurationProvider calculations,
        ICalculationVersionConfigurationProvider versions,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IAuthenticationContextAccessor authContext)
    {
        _services = new CalculationVersionServices(calculations, versions, ancestry, authority);
        _authContext = authContext;
    }

    /// <summary>Gets the authorization policy and scope right required for write operations.</summary>
    protected virtual string WritePolicy => "calculation-entities:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Delete("/calculation-entities/{Id}/versions/{PlaceKind}/{PlaceId}");
        Policies(WritePolicy);
        Summary(s => s.Summary = "Delete a version of a calculation from an organization or workspace");
    }

    /// <summary>Deletes the version and answers 204, or answers why it cannot be deleted.</summary>
    public override async Task HandleAsync(CalculationVersionPlaceRequest req, CancellationToken ct)
    {
        if (_authContext.Current is not { IsAuthenticated: true, ActiveTenantId: { } tenantId })
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        var deleted = await CalculationVersionWrite.Delete(_services, req, tenantId, WritePolicy, Logger, ct).ConfigureAwait(false);
        if (deleted.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(deleted, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}

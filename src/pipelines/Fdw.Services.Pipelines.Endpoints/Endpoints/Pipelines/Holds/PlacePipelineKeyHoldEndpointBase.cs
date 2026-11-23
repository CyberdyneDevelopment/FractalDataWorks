using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Execution.Claims;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Pipelines.Runs;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Places a hold on a key (FDW-6939): <c>POST /pipeline-keys/{Key}/hold</c>. A hold stops the waiting runs on that key (one conversation) from starting until it is released, and survives a restart.
/// </summary>
/// <remarks>
/// The person is the signed-in caller and the tenant the caller's own active tenant (its token's claim: the ETL host resolves no tenant per request, so ITenantContext is null there), never read from the request: who placed a hold is a fact about the caller. The permission is <c>pipelines:hold</c>, its own and not
/// <c>pipelines:execute</c>, so a deployment can give holding to other people than starting and cancelling runs. A key that is already held is refused naming who holds it.
/// </remarks>
public abstract class PlacePipelineKeyHoldEndpointBase : Endpoint<PlacePipelineKeyHoldRequest, PipelineKeyHold>
{
    private readonly IPipelineKeyHolds _holds;
    private readonly IAuthenticationContextAccessor _authentication;

    /// <summary>Initializes a new instance of the <see cref="PlacePipelineKeyHoldEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="holds">Places the hold.</param>
    /// <param name="authentication">Says who the caller is.</param>
    protected PlacePipelineKeyHoldEndpointBase(ILogger logger, IPipelineKeyHolds holds, IAuthenticationContextAccessor authentication)
    {
        EndpointLogger = logger;
        _holds = holds;
        _authentication = authentication;
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/pipeline-keys/{Key}/hold");
#if DEVELOP
        AllowAnonymous();
#else
        // Why its own permission: holding a key is a different right from starting and cancelling runs, so a deployment gives it to other people.
        Policies("pipelines:hold");
#endif
        Summary(s =>
        {
            s.Summary = "Hold a key";
            s.Description = "Stops the waiting runs on a key from starting until the hold is released. The hold is the signed-in person's, in their tenant, and states why. A key that is already held is refused naming who holds it.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(PlacePipelineKeyHoldRequest req, CancellationToken ct)
    {
        if (RunPerson.Of(_authentication.Current) is not { } person)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(PipelineRunClaimLog.HoldCallerIsNoPerson(EndpointLogger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        if (RunTenant.Of(_authentication.Current) is not { } tenant)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(PipelineRunClaimLog.HoldCallerHasNoTenant(EndpointLogger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var placed = await _holds.Place(tenant, req.Key, person, req.Reason, ct).ConfigureAwait(false);
        if (placed.IsFailure || placed.Value is not { } hold)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(placed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(hold, ct).ConfigureAwait(false);
    }
}

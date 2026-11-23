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
/// Releases the hold on a key (FDW-6939): <c>DELETE /pipeline-keys/{Key}/hold</c>, which lets the waiting runs the hold was stopping start, in the order they asked.
/// </summary>
/// <remarks>
/// Any caller with <c>pipelines:hold</c> in the tenant may release a hold, not only the person who placed it (no ownership rule in 1.6.0). A key that is not held is refused.
/// </remarks>
public abstract class ReleasePipelineKeyHoldEndpointBase : Endpoint<ReleasePipelineKeyHoldRequest, PipelineKeyHoldReleased>
{
    private readonly IPipelineKeyHolds _holds;
    private readonly IAuthenticationContextAccessor _authentication;

    /// <summary>Initializes a new instance of the <see cref="ReleasePipelineKeyHoldEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="holds">Releases the hold.</param>
    /// <param name="authentication">Says who the caller is.</param>
    protected ReleasePipelineKeyHoldEndpointBase(ILogger logger, IPipelineKeyHolds holds, IAuthenticationContextAccessor authentication)
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
        Delete("/pipeline-keys/{Key}/hold");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:hold");
#endif
        Summary(s =>
        {
            s.Summary = "Release the hold on a key";
            s.Description = "Lets the waiting runs the hold was stopping start, in the order they asked. Any caller with the permission in the tenant may release it. A key that is not held is refused.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ReleasePipelineKeyHoldRequest req, CancellationToken ct)
    {
        if (RunPerson.Of(_authentication.Current) is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(PipelineRunClaimLog.HoldCallerIsNoPerson(EndpointLogger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        if (RunTenant.Of(_authentication.Current) is not { } tenant)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(PipelineRunClaimLog.HoldCallerHasNoTenant(EndpointLogger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var released = await _holds.Release(tenant, req.Key, ct).ConfigureAwait(false);
        if (released.IsFailure || released.Value is not { } answer)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(released, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(answer, ct).ConfigureAwait(false);
    }
}

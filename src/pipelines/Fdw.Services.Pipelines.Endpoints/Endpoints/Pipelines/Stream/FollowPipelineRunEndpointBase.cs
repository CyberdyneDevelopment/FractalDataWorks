using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Pipelines.Runs;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Follows a run (FDW-6140): <c>GET /pipeline-runs/{ExecutionId}/stream?after=N</c> answers the pieces of the run after the sequence the person last received: each text a step wrote, each step's start and end, each
/// step skipped. A client that dropped asks again from the last sequence it has and gets exactly what follows, once and in order.
/// </summary>
/// <remarks>
/// The person is the signed-in caller and the tenant the caller's own active tenant, never read from the request. Only the person the pieces are addressed to reads them (and whoever the host's audience names): another
/// person gets <c>PIPERUNSTREAM-40850</c> and no piece. The permission is <c>pipelines:read</c>.
/// </remarks>
public abstract class FollowPipelineRunEndpointBase : Endpoint<FollowPipelineRunRequest, FollowPipelineRunResponse>
{
    private readonly IRunStream _stream;
    private readonly IAuthenticationContextAccessor _authentication;

    /// <summary>Initializes a new instance of the <see cref="FollowPipelineRunEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="stream">Reads the pieces.</param>
    /// <param name="authentication">Says who the caller is.</param>
    protected FollowPipelineRunEndpointBase(ILogger logger, IRunStream stream, IAuthenticationContextAccessor authentication)
    {
        EndpointLogger = logger;
        _stream = stream;
        _authentication = authentication;
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/pipeline-runs/{ExecutionId}/stream");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Follow a run";
            s.Description = "The pieces of the run after the sequence last received, in order: each text a step wrote, each step's start and end, each step skipped. Only the person the run is for reads them. Ask again from the last sequence after a dropped connection.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(FollowPipelineRunRequest req, CancellationToken ct)
    {
        if (RunPerson.Of(_authentication.Current) is not { } person)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(RunStreamLog.CallerIsNoPerson(EndpointLogger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        if (RunTenant.Of(_authentication.Current) is not { } tenant)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(RunStreamLog.CallerHasNoTenant(EndpointLogger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var read = await _stream.ReadAfter(tenant, person, req.ExecutionId, req.After, req.Take, ct).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } pieces)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(read, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new FollowPipelineRunResponse { Pieces = pieces, Last = pieces.Count > 0 ? pieces.Max(piece => piece.Sequence) : req.After }, ct).ConfigureAwait(false);
    }
}

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.Http.Abstractions.EndPoints;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>What the two map endpoints share: the route's policy, and sending a stream whose status code is fixed by its first byte.</summary>
/// <typeparam name="TRequest">The endpoint's request.</typeparam>
/// <remarks>
/// The map spans data stores, pipelines and data sets, and no single resource permission covers all three. It is gated by
/// <c>pipelines:read</c>, as the dataflow graph it replaces was, and a workspace scope is gated again by whether the
/// caller may view that workspace.
/// </remarks>
public abstract class MapEndpointBase<TRequest> : Endpoint<TRequest, StreamedMapResponse>
    where TRequest : notnull
{
    /// <summary>Initializes a new instance of the <see cref="MapEndpointBase{TRequest}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    private protected MapEndpointBase(ILogger logger)
    {
        MapLogger = logger ?? NullLogger.Instance;
    }

    /// <summary>Gets the logger.</summary>
    private protected ILogger MapLogger { get; }

    /// <summary>Configures the route, policy and streaming marker every map endpoint has.</summary>
    /// <param name="route">The route.</param>
    /// <param name="summary">The OpenAPI summary.</param>
    /// <param name="description">The OpenAPI description.</param>
    private protected void ConfigureMap(string route, string summary, string description)
    {
        Get(route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Options(builder => builder.WithMetadata(StreamingResponseMetadata.Instance));
        Summary(s =>
        {
            s.Summary = summary;
            s.Description = description;
        });
        Description(x => x.WithTags("map"));
    }

    /// <summary>Opens a stream and sends it, or sends the failure that stopped it opening.</summary>
    /// <param name="open">Opens the stream: reads whatever must be known before the first byte is sent.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="maxNodes">The most nodes the caller wants sent, or nothing for no cap; honoured in the line form only.</param>
    /// <remarks>
    /// A request that names <c>application/x-ndjson</c> in <c>Accept</c> gets the line form (<see cref="MapNdjsonWriter"/>); any
    /// other request gets the single JSON object it always got.
    /// </remarks>
    private protected async Task SendStream(
        Func<CancellationToken, Task<IGenericResult<MapStream>>> open, CancellationToken ct, int? maxNodes = null)
    {
        var started = Stopwatch.GetTimestamp();
        EndpointLogger.ExecutingEndpoint(MapLogger, GetType().Name, typeof(TRequest).Name);
        try
        {
            var opened = await open(ct).ConfigureAwait(false);
            if (opened.IsFailure)
            {
                await SendError(opened, ct).ConfigureAwait(false);
                return;
            }

            var stream = opened.Value!;
            if (MapNdjsonWriter.Wanted(HttpContext.Request))
            {
                // Asks a buffering proxy (nginx) to pass each line on as it is written.
                HttpContext.Response.Headers["X-Accel-Buffering"] = "no";
                HttpContext.Response.ContentType = MapNdjsonWriter.MediaType;
                await MapNdjsonWriter.Write(HttpContext.Response.Body, stream, () => Error(stream.State.Outcome), maxNodes, ct).ConfigureAwait(false);
            }
            else
            {
                await Send.ResultAsync(TypedResults.Json(new StreamedMapResponse(stream, () => Error(stream.State.Outcome)))).ConfigureAwait(false);
            }

            EndpointLogger.EndpointExecuted(MapLogger, GetType().Name, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var msg = EndpointLogger.EndpointError(MapLogger, ex, GetType().Name);

            // Once the first byte is out the status code is fixed; a cut connection is what tells the client the
            // map is incomplete, where a closed body would read as a whole one.
            if (HttpContext.Response.HasStarted)
            {
                HttpContext.Abort();
                return;
            }

            await SendError(GenericResult.Failure(msg), ct).ConfigureAwait(false);
        }
    }

    /// <summary>The refusal for a request the map cannot draw.</summary>
    /// <typeparam name="T">The value type of the result.</typeparam>
    /// <param name="reason">The part of the request at fault.</param>
    private protected IGenericResult<T> Invalid<T>(string reason)
        => GenericResult<T>.Failure(
            WorkspacesResultCodes.ByName("MapRequestInvalid"), MapLogger, ResultDetails.Create("reason", reason));

    private ProblemDetails? Error(IGenericResult outcome)
        => outcome.IsFailure ? MapProblemDetails.From(outcome, HttpContext) : null;

    private async Task SendError(IGenericResult result, CancellationToken ct)
    {
        await Send.ResultAsync(MapProblemDetails.From(result, HttpContext)).ConfigureAwait(false);
    }
}

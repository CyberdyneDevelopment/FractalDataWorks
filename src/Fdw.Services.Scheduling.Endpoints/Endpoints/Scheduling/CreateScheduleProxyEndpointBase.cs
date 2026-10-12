using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Models;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Base endpoint for proxying schedule creation requests to SchedulerServer.
/// </summary>
/// <remarks>
/// The request names its pipeline by id. This host resolves it with the caller's own identity -- the pipeline
/// client carries the caller's token -- and relays the id and the name it resolved; the scheduler stores them
/// without looking again (FDW-1088).
/// </remarks>
public abstract class CreateScheduleProxyEndpointBase : Endpoint<ProxyCreateScheduleRequest, CreateScheduleClientResponse>
{
    private readonly IScheduleClient _client;
    private readonly IPipelineClient _pipelines;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="CreateScheduleProxyEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="client">The schedule client.</param>
    /// <param name="pipelines">The pipeline client, carrying the caller's identity.</param>
    protected CreateScheduleProxyEndpointBase(ILogger logger, IScheduleClient client, IPipelineClient pipelines)
    {
        EndpointLogger = logger;
        _client = client;
        _pipelines = pipelines;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/schedules");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("schedules:write");
#endif
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (summary, tags, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(ProxyCreateScheduleRequest req, CancellationToken ct)
    {
        ScheduleProxyEndpointLog.ProxyRequest(EndpointLogger, "POST", "Scheduler", "schedules");

        var zone = ScheduleTimeZone.Check(req.TimeZoneId, EndpointLogger);
        if (zone.IsFailure)
        {
            var (zoneStatus, zoneRefusal) = ResultHttpStatusMapper.Map(zone, HttpContext);
            await Send.ResultAsync(TypedResults.Json(zoneRefusal, statusCode: zoneStatus)).ConfigureAwait(false);
            return;
        }

        var pipeline = await SchedulePipeline.Resolve(_pipelines, req.Name, req.PipelineId, EndpointLogger, ct).ConfigureAwait(false);
        if (pipeline.IsFailure)
        {
            var (refusedStatus, refusal) = ResultHttpStatusMapper.Map(pipeline, HttpContext);
            await Send.ResultAsync(TypedResults.Json(refusal, statusCode: refusedStatus)).ConfigureAwait(false);
            return;
        }

        var request = new CreateScheduleClientRequest
        {
            Name = req.Name,
            PipelineId = pipeline.Value!.Id,
            PipelineName = pipeline.Value.Name,
            SchedulerType = req.SchedulerType,
            CronExpression = req.CronExpression,
            IntervalSeconds = req.IntervalSeconds,
            OneTimeDateTime = req.OneTimeDateTime,
            EventName = req.EventName,
            TimeZoneId = req.TimeZoneId,
            IsEnabled = req.IsEnabled,
            Parameters = req.Parameters,
            RunRoles = req.RunRoles,
        };

        var result = await _client.CreateSchedule(request, ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            ScheduleProxyEndpointLog.ProxyFailed(EndpointLogger, "Scheduler", result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        ScheduleProxyEndpointLog.ProxyResponse(EndpointLogger, "Scheduler", 201);
        await Send.ResponseAsync(result.Value!, 201, ct).ConfigureAwait(false);
    }
}

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Base endpoint for proxying schedule creation requests to SchedulerServer.
/// </summary>
public abstract class CreateScheduleProxyEndpointBase : Endpoint<ProxyCreateScheduleRequest, CreateScheduleClientResponse>
{
    private readonly IScheduleClient _client;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateScheduleProxyEndpointBase"/> class.
    /// </summary>
    protected CreateScheduleProxyEndpointBase(IScheduleClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/proxy/schedules");
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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        ScheduleProxyEndpointLog.ProxyRequest(EndpointLogger, "POST", "Scheduler", "schedules");

        var request = new CreateScheduleClientRequest
        {
            Name = req.Name,
            PipelineName = req.PipelineName,
            SchedulerType = req.SchedulerType,
            CronExpression = req.CronExpression ?? string.Empty,
            IntervalSeconds = req.IntervalSeconds,
            OneTimeDateTime = req.OneTimeDateTime,
            EventName = req.EventName,
            TimeZoneId = req.TimeZoneId,
            IsEnabled = req.IsEnabled
        };

        var result = await _client.CreateSchedule(request, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var errorMessage = result.CurrentMessage ?? "Proxy request failed";
            ScheduleProxyEndpointLog.ProxyFailed(EndpointLogger, "Scheduler", errorMessage);
            AddError("Failed to proxy request to SchedulerServer");
            await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
            return;
        }

        ScheduleProxyEndpointLog.ProxyResponse(EndpointLogger, "Scheduler", 201);
        await Send.ResponseAsync(result.Value!, 201, ct).ConfigureAwait(false);
    }
}

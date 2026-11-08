using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Base endpoint for proxying schedule list requests to SchedulerServer.
/// </summary>
public abstract class GetSchedulesProxyEndpointBase : EndpointWithoutRequest<IReadOnlyList<ScheduleInfoDto>>
{
    /// <summary>Initializes a new instance of the <see cref="GetSchedulesProxyEndpointBase"/> class.</summary>
        private readonly IScheduleClient _client;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="GetSchedulesProxyEndpointBase"/> class.</summary>
    protected GetSchedulesProxyEndpointBase(ILogger logger, IScheduleClient client)
    {
        EndpointLogger = logger;
        _client = client;
    }


    /// <inheritdoc />
    public override void Configure()
    {
        Get("/schedules");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("schedules:read");
#endif
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (summary, tags, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        
        ScheduleProxyEndpointLog.ProxyRequest(EndpointLogger, "GET", "Scheduler", "schedules");

        // ?tag= is forwarded as sent, so the scheduler host that holds the tags answers it; it is never dropped here (FDW-3816).
        var result = await _client.List(HttpContext.Request.Query["tag"].Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!).ToList(), ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var errorMessage = result.FailureReason().Message;
            ScheduleProxyEndpointLog.ProxyFailed(EndpointLogger, "Scheduler", errorMessage);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(ScheduleProxyEndpointLog.GetSchedulesProxyFailedToProxyRequestTo(EndpointLogger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        ScheduleProxyEndpointLog.ProxyResponse(EndpointLogger, "Scheduler", 200);
        await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
    }
}

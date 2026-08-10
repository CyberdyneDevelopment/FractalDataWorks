using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Base endpoint for proxying schedule list requests to SchedulerServer.
/// </summary>
public abstract class GetSchedulesProxyEndpointBase : EndpointWithoutRequest<IReadOnlyList<ScheduleInfoDto>>
{
    private readonly IScheduleClient _client;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSchedulesProxyEndpointBase"/> class.
    /// </summary>
    protected GetSchedulesProxyEndpointBase(IScheduleClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/proxy/schedules");
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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        ScheduleProxyEndpointLog.ProxyRequest(EndpointLogger, "GET", "Scheduler", "schedules");

        var result = await _client.List(ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var errorMessage = result.CurrentMessage ?? "Proxy request failed";
            ScheduleProxyEndpointLog.ProxyFailed(EndpointLogger, "Scheduler", errorMessage);
            AddError("Failed to proxy request to SchedulerServer");
            await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
            return;
        }

        ScheduleProxyEndpointLog.ProxyResponse(EndpointLogger, "Scheduler", 200);
        await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
    }
}

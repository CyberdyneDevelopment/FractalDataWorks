using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Base endpoint for proxying ETL job trigger requests.
/// </summary>
public abstract class TriggerEtlJobProxyEndpointBase : Endpoint<ProxyTriggerEtlRequest, TriggerPipelineResponse>
{
    private readonly IPipelineJobClient _client;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerEtlJobProxyEndpointBase"/> class.
    /// </summary>
    protected TriggerEtlJobProxyEndpointBase(IPipelineJobClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/proxy/etl/trigger");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:execute");
#endif
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (summary, tags, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(ProxyTriggerEtlRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        ProxyEndpointLog.ProxyRequest(EndpointLogger, "POST", "Etl", "etl/trigger");

        var request = new TriggerPipelineRequest
        {
            Name = req.PipelineName,
            TriggerSource = req.TriggerSource
        };

        var result = await _client.Trigger(request, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var errorMessage = result.CurrentMessage ?? "Proxy request failed";
            ProxyEndpointLog.ProxyFailed(EndpointLogger, "Etl", errorMessage);
            AddError("Failed to proxy request to EtlServer");
            await Send.ErrorsAsync(502, ct).ConfigureAwait(false);
            return;
        }

        ProxyEndpointLog.ProxyResponse(EndpointLogger, "Etl", 202);
        await Send.ResponseAsync(result.Value!, 202, ct).ConfigureAwait(false);
    }
}

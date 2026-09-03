using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Base endpoint for receiving ETL webhook completion callbacks.
/// </summary>
public abstract class EtlWebhookEndpointBase : Endpoint<EtlWebhookRequest, EtlWebhookResponse>
{
    /// <summary>Initializes a new instance of the <see cref="EtlWebhookEndpointBase"/> class.</summary>
    protected EtlWebhookEndpointBase(ILogger logger)
    {
        EndpointLogger = logger;
    }

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/proxy/etl/webhook/completion");
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
    public override async Task HandleAsync(EtlWebhookRequest req, CancellationToken ct)
    {
        
        if (string.IsNullOrWhiteSpace(req.ExecutionId))
        {
            ProxyEndpointLog.EtlWebhookUnknownExecution(EndpointLogger, req.ExecutionId);
            await Send.ResponseAsync(new EtlWebhookResponse
            {
                Acknowledged = false,
                ExecutionId = req.ExecutionId
            }, 400, ct).ConfigureAwait(false);
            return;
        }

        ProxyEndpointLog.EtlWebhookReceived(EndpointLogger, req.ExecutionId, req.Status);

        await Send.OkAsync(new EtlWebhookResponse
        {
            Acknowledged = true,
            ExecutionId = req.ExecutionId
        }, ct).ConfigureAwait(false);
    }
}

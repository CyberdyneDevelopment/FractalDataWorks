using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Webhooks;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Reads a webhook trigger by name (FDW-1884). The answer never carries the secret or its hash.</summary>
public abstract class GetWebhookEndpointBase : CrudGetEndpointBase<WebhookNameRequest, WebhookPayload>
{
    private readonly WebhookTriggers _triggers;

    /// <summary>Initializes a new instance of the <see cref="GetWebhookEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored webhook triggers.</param>
    /// <param name="pipelines">The stored pipelines.</param>
    /// <param name="grants">Expands the roles a caller names.</param>
    protected GetWebhookEndpointBase(
        ILogger<GetWebhookEndpointBase> logger,
        WebhookTriggerConfigurationProvider provider,
        PipelineServiceConfigurationProvider pipelines,
        IRoleGrants grants) : base(logger)
    {
        _triggers = new WebhookTriggers(provider, pipelines, new RunRoleAuthority(grants), logger);
    }

    /// <inheritdoc />
    protected override string ResourceName => "webhooks";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(WebhookNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<WebhookPayload?>> FindByIdentifier(WebhookNameRequest request, CancellationToken ct) =>
        _triggers.FindPayload(request.Name, ct);
}

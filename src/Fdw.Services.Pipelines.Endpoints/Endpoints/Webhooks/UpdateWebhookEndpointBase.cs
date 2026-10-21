using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Webhooks;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Replaces a webhook trigger's pipeline, state, description, run roles and mappings (FDW-1884). Its secret is kept.</summary>
public abstract class UpdateWebhookEndpointBase : CrudUpdateEndpointBase<WebhookPayload, WebhookPayload>
{
    private readonly WebhookTriggers _triggers;

    /// <summary>Initializes a new instance of the <see cref="UpdateWebhookEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored webhook triggers.</param>
    /// <param name="pipelines">The stored pipelines.</param>
    /// <param name="grants">Expands the roles a caller names.</param>
    protected UpdateWebhookEndpointBase(
        ILogger<UpdateWebhookEndpointBase> logger,
        WebhookTriggerConfigurationProvider provider,
        PipelineServiceConfigurationProvider pipelines,
        IRoleGrants grants) : base(logger)
    {
        _triggers = new WebhookTriggers(provider, pipelines, new RunRoleAuthority(grants), logger);
    }

    /// <inheritdoc />
    protected override string ResourceName => "webhooks";

    /// <inheritdoc />
    protected override string Route => "/webhooks/{Name}";

    /// <inheritdoc />
    protected override HttpVerb UpdateVerb => HttpVerb.PUT;

    /// <inheritdoc />
    protected override string EndpointSummary => "Replace a webhook trigger";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Replaces the pipeline, state, description, run roles and mappings of a webhook trigger with those in the body. The secret is kept; make a new one with POST /webhooks/{name}/secret. The trigger is refused whole when it cannot be saved.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(WebhookPayload request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<WebhookPayload?>> FindForUpdate(WebhookPayload request, CancellationToken ct) =>
        _triggers.FindPayload(request.Name, ct);

    /// <inheritdoc />
    protected override Task<IGenericResult<WebhookPayload>> Update(WebhookPayload request, WebhookPayload existing, CancellationToken ct) =>
        _triggers.Replace(new ClaimsPrincipalAuthenticationContext(User), request, ct);
}

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

/// <summary>Makes a webhook trigger (FDW-1884) and answers with its secret, which is shown only in this answer.</summary>
public abstract class CreateWebhookEndpointBase : CrudCreateEndpointBase<WebhookPayload, WebhookCreatedPayload>
{
    private readonly WebhookTriggers _triggers;

    /// <summary>Initializes a new instance of the <see cref="CreateWebhookEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored webhook triggers.</param>
    /// <param name="pipelines">The stored pipelines.</param>
    /// <param name="grants">Expands the roles a caller names.</param>
    protected CreateWebhookEndpointBase(
        ILogger<CreateWebhookEndpointBase> logger,
        WebhookTriggerConfigurationProvider provider,
        PipelineServiceConfigurationProvider pipelines,
        IRoleGrants grants) : base(logger)
    {
        _triggers = new WebhookTriggers(provider, pipelines, new RunRoleAuthority(grants), logger);
    }

    /// <inheritdoc />
    protected override string ResourceName => "webhooks";

    /// <inheritdoc />
    protected override string EndpointSummary => "Make a webhook trigger";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Makes an address an outside system calls, with a secret in the X-Webhook-Secret header, to start a pipeline. The answer carries the secret, which cannot be read again. The trigger is refused whole, naming each problem, when it cannot be saved.";

    /// <inheritdoc />
    protected override string GetResourceName(WebhookPayload request) => request.Name;

    /// <inheritdoc />
    protected override string DuplicateMessage(WebhookPayload request) => $"A webhook named '{request.Name}' already exists";

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(WebhookPayload request, CancellationToken ct)
    {
        var existing = await _triggers.Find(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existing.IsSuccess && existing.Value != null);
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<WebhookCreatedPayload>> Create(WebhookPayload request, CancellationToken ct) =>
        _triggers.Create(new ClaimsPrincipalAuthenticationContext(User), request, ct);

    /// <inheritdoc />
    protected override Task SendCreatedResponse(WebhookCreatedPayload detail, CancellationToken ct) => Send.ResponseAsync(detail, 201, ct);
}

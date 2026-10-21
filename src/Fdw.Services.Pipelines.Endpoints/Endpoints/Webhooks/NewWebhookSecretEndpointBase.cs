using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Webhooks;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Makes a new secret for a webhook trigger (FDW-1884) and answers with it, once. The old secret stops working at once. Holding the
/// write policy is what allows it: a secret that leaked is replaced without touching the rest of the trigger.
/// </summary>
public abstract class NewWebhookSecretEndpointBase : Endpoint<WebhookNameRequest, WebhookSecretPayload>
{
    private readonly WebhookTriggers _triggers;

    /// <summary>Initializes a new instance of the <see cref="NewWebhookSecretEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The stored webhook triggers.</param>
    /// <param name="pipelines">The stored pipelines.</param>
    /// <param name="grants">Expands the roles a caller names.</param>
    protected NewWebhookSecretEndpointBase(
        ILogger<NewWebhookSecretEndpointBase> logger,
        WebhookTriggerConfigurationProvider provider,
        PipelineServiceConfigurationProvider pipelines,
        IRoleGrants grants)
    {
        _triggers = new WebhookTriggers(provider, pipelines, new RunRoleAuthority(grants), logger ?? throw new ArgumentNullException(nameof(logger)));
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/webhooks/{Name}/secret");
        DontThrowIfValidationFails();
#if DEVELOP
        AllowAnonymous();
#else
        Policies("webhooks:write");
#endif
        Summary(s =>
        {
            s.Summary = "Make a new secret for a webhook trigger";
            s.Description = "Makes a new secret and answers with it, once. The old secret stops working at once.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(WebhookNameRequest req, CancellationToken ct)
    {
        var made = await _triggers.NewSecret(req.Name, ct).ConfigureAwait(false);
        if (made.IsSuccess && made.Value is { } payload)
        {
            await Send.OkAsync(payload, ct).ConfigureAwait(false);
            return;
        }

        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(made, HttpContext);
        await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
    }
}

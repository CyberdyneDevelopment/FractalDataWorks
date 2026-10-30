using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Webhooks;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Fdw.Services.Pipelines.Webhooks;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Makes, changes and reads webhook triggers for the endpoints that serve them (FDW-1884): finds the pipeline, judges the run roles,
/// makes the secret and saves through the domain provider, then reads the trigger back as the API answers it.
/// </summary>
public sealed class WebhookTriggers
{
    private readonly WebhookTriggerConfigurationProvider _provider;
    private readonly PipelineServiceConfigurationProvider _pipelines;
    private readonly RunRoleAuthority _roles;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="WebhookTriggers"/> class.</summary>
    /// <param name="provider">The stored webhook triggers.</param>
    /// <param name="pipelines">The stored pipelines.</param>
    /// <param name="roles">Judges the run roles a caller names.</param>
    /// <param name="logger">The logger.</param>
    public WebhookTriggers(
        WebhookTriggerConfigurationProvider provider, PipelineServiceConfigurationProvider pipelines, RunRoleAuthority roles, ILogger logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _pipelines = pipelines ?? throw new ArgumentNullException(nameof(pipelines));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Finds a trigger by name.</summary>
    /// <param name="name">The trigger's name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The trigger as it is stored, or null when nothing has that name; or the failure of the read.</returns>
    public async Task<IGenericResult<WebhookTriggerConfiguration?>> Find(string name, CancellationToken ct)
    {
        var read = await _provider.Get(name, ct).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<WebhookTriggerConfiguration?>();
        }

        return read.Value switch
        {
            null => GenericResult<WebhookTriggerConfiguration?>.Success(null),
            WebhookTriggerConfiguration trigger => GenericResult<WebhookTriggerConfiguration?>.Success(trigger),
            var other => GenericResult<WebhookTriggerConfiguration?>.Failure(WebhookEndpointLog.NotAWebhookTrigger(_logger, name, other.Implementation)),
        };
    }

    /// <summary>Finds a trigger by name as the API answers it.</summary>
    /// <param name="name">The trigger's name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The payload, or null when nothing has that name; or the failure of the read.</returns>
    public async Task<IGenericResult<WebhookPayload?>> FindPayload(string name, CancellationToken ct)
    {
        var found = await Find(name, ct).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return found.ToNewResult<WebhookPayload?>();
        }

        return GenericResult<WebhookPayload?>.Success(found.Value is { } trigger ? WebhookPayloadWriter.Write(trigger) : null);
    }

    /// <summary>Makes a trigger with a new secret.</summary>
    /// <param name="caller">Who is making it; the run roles must be ones they hold.</param>
    /// <param name="request">The trigger in the request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The trigger and its secret, which is shown only here; or the refusal.</returns>
    public async Task<IGenericResult<WebhookCreatedPayload>> Create(IAuthenticationContext caller, WebhookPayload request, CancellationToken ct)
    {
        var (secret, hash, prefix) = WebhookSecret.Create();
        var saved = await Save(caller, request, Guid.CreateVersion7(), new WebhookSecretHash(hash, prefix, DateTimeOffset.UtcNow), mayCreate: true, ct)
            .ConfigureAwait(false);
        if (saved.IsFailure || saved.Value is not { } payload)
        {
            return saved.ToNewResult<WebhookCreatedPayload>();
        }

        WebhookEndpointLog.Made(_logger, payload.Name, payload.Pipeline, prefix);
        return GenericResult<WebhookCreatedPayload>.Success(new WebhookCreatedPayload
        {
            Name = payload.Name,
            Pipeline = payload.Pipeline,
            Enabled = payload.Enabled,
            Description = payload.Description,
            RunRoles = payload.RunRoles,
            Parameters = payload.Parameters,
            SecretPrefix = payload.SecretPrefix,
            SecretCreatedAt = payload.SecretCreatedAt,
            Secret = secret,
        });
    }

    /// <summary>Replaces a trigger's pipeline, state, description, roles and mappings, keeping its secret.</summary>
    /// <param name="caller">Who is changing it; the run roles must be ones they hold.</param>
    /// <param name="request">The trigger in the request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The trigger as saved, or the refusal.</returns>
    public async Task<IGenericResult<WebhookPayload>> Replace(IAuthenticationContext caller, WebhookPayload request, CancellationToken ct)
    {
        var original = await Find(request.Name, ct).ConfigureAwait(false);
        if (original.IsFailure || original.Value is not { } current)
        {
            return original.IsFailure
                ? original.ToNewResult<WebhookPayload>()
                : WebhookRefusal.Of<WebhookPayload>($"no webhook trigger is named '{request.Name}'");
        }

        return await Save(caller, request, current.Id, new WebhookSecretHash(current.SecretHash, current.SecretPrefix, current.SecretCreatedAt), mayCreate: false, ct)
            .ConfigureAwait(false);
    }

    /// <summary>Makes a new secret for a trigger. The old one stops working at once.</summary>
    /// <param name="name">The trigger's name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The new secret, which is shown only here; or the refusal when nothing has that name.</returns>
    public async Task<IGenericResult<WebhookSecretPayload>> NewSecret(string name, CancellationToken ct)
    {
        var original = await Find(name, ct).ConfigureAwait(false);
        if (original.IsFailure || original.Value is not { } trigger)
        {
            return original.IsFailure
                ? original.ToNewResult<WebhookSecretPayload>()
                : WebhookRefusal.Of<WebhookSecretPayload>($"no webhook trigger is named '{name}'");
        }

        var (secret, hash, prefix) = WebhookSecret.Create();
        trigger.SecretHash = hash;
        trigger.SecretPrefix = prefix;
        trigger.SecretCreatedAt = DateTimeOffset.UtcNow;
        var saved = await _provider
            .Update(trigger, WebhookPayloadReader.DomainName, WebhookPayloadReader.ImplementationName, trigger.Name, ct)
            .ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.ToNewResult<WebhookSecretPayload>();
        }

        WebhookEndpointLog.NewSecretMade(_logger, trigger.Name, prefix);
        return GenericResult<WebhookSecretPayload>.Success(new WebhookSecretPayload
        {
            Name = trigger.Name, Secret = secret, SecretPrefix = prefix, SecretCreatedAt = trigger.SecretCreatedAt,
        });
    }

    // Only a create may make the trigger; a replace of a trigger removed since it was read fails instead of bringing it back.
    private async Task<IGenericResult<WebhookPayload>> Save(
        IAuthenticationContext caller, WebhookPayload request, Guid id, WebhookSecretHash secret, bool mayCreate, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pipeline = await _pipelines.Get(request.Pipeline, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (pipeline.IsFailure || pipeline.Value is not { } found)
        {
            return WebhookRefusal.Of<WebhookPayload>($"the pipeline '{request.Pipeline}' does not exist");
        }

        var roles = await _roles.Authorize(caller, found.Name, request.RunRoles, _logger, ct).ConfigureAwait(false);
        if (roles.IsFailure || roles.Value is not { } judged)
        {
            return roles.ToNewResult<WebhookPayload>();
        }

        var trigger = WebhookPayloadReader.Read(request, id, found, judged, secret);
        if (trigger.IsFailure || trigger.Value is not { } checkedTrigger)
        {
            return trigger.ToNewResult<WebhookPayload>();
        }

        var saved = mayCreate
            ? await _provider
                .Save(checkedTrigger, WebhookPayloadReader.DomainName, WebhookPayloadReader.ImplementationName, checkedTrigger.Name, ct)
                .ConfigureAwait(false)
            : await _provider
                .Update(checkedTrigger, WebhookPayloadReader.DomainName, WebhookPayloadReader.ImplementationName, checkedTrigger.Name, ct)
                .ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.ToNewResult<WebhookPayload>();
        }

        var back = await FindPayload(checkedTrigger.Name, ct).ConfigureAwait(false);
        if (back.IsFailure)
        {
            return back.ToNewResult<WebhookPayload>();
        }

        return back.Value is { } payload
            ? GenericResult<WebhookPayload>.Success(payload)
            : GenericResult<WebhookPayload>.Failure(WebhookEndpointLog.SavedButUnreadable(_logger, checkedTrigger.Name));
    }
}

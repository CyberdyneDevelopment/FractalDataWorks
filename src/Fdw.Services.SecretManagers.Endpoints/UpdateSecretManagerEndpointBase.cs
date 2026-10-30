using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Base endpoint for updating an existing secret manager configuration.
/// Route: PUT /secret-managers/{Name}
/// </summary>
public abstract class UpdateSecretManagerEndpointBase : Endpoint<UpdateSecretManagerRequest, SecretManagerDetailResponse>
{
    private readonly ISecretManagerConfigurationProvider _configProvider;
    private readonly ILogger<UpdateSecretManagerEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSecretManagerEndpointBase"/> class.
    /// </summary>
    protected UpdateSecretManagerEndpointBase(
        ISecretManagerConfigurationProvider configProvider,
        ILogger<UpdateSecretManagerEndpointBase> logger)
    {
        _configProvider = configProvider;
        _logger = logger ?? NullLogger<UpdateSecretManagerEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Patch("/secret-managers/{Name}");
        // Secret managers are the platform's, owned by the system tenant: only a platform administrator lists, reads
        // or changes one. A tenant never touches a secret manager or a secret; a connection's credentials are set
        // through the connection (FDW-1086).
        Policies(AuthConstants.PlatformAdminPolicy);
        Summary(s =>
        {
            s.Summary = "Update secret manager";
            s.Description = "Changes the settings of an existing secret manager. Send the settings to change as a JSON object under 'configuration'; the members left out keep their values. A setting the secret manager's type does not take is refused, naming it and listing those it takes.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add Tags or other endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc/>
    public override async Task HandleAsync(UpdateSecretManagerRequest req, CancellationToken ct)
    {
        try
        {
            SecretManagerEndpointLog.UpdatingSecretManager(_logger, req.Name);

            var existingResult = await _configProvider.Get(req.Name, ct).ConfigureAwait(false);
            var existing = existingResult.IsSuccess ? existingResult.Value : null;

            if (existing == null)
            {
                SecretManagerEndpointLog.SecretManagerNotFound(_logger, req.Name);
                await HttpContext.WriteNotFound("SecretManager", req.Name, ct).ConfigureAwait(false);
                return;
            }

            // The implementation the domain row names is the secret manager's type, and its settings are what this route changes: the sent
            // members are laid over the stored ones and saved. The domain row's Description and Environment are not written by a save of the
            // implementation, so this route does not take them (FDW-2890).
            var changed = SecretManagerSettings.Read(existing.Implementation, req.Configuration, existing, _logger);
            if (changed.IsFailure || changed.Value is null)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(changed, HttpContext)).ConfigureAwait(false);
                return;
            }

            changed.Value.Id = existing.Id;
            var saveResult = await _configProvider.Update(changed.Value, "SecretManager", existing.Implementation, existing.Name, ct).ConfigureAwait(false);
            if (saveResult.IsFailure)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(saveResult, HttpContext)).ConfigureAwait(false);
                return;
            }

            SecretManagerEndpointLog.SecretManagerUpdated(_logger, req.Name);

            var detail = new SecretManagerDetailResponse
            {
                Id = existing.Id,
                Name = existing.Name,
                SecretManagerType = existing.Implementation,
                Implementation = existing.Implementation
            };

            await Send.OkAsync(detail, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SecretManagerEndpointLog.UnexpectedError(_logger, ex);
            throw;
        }
    }
}

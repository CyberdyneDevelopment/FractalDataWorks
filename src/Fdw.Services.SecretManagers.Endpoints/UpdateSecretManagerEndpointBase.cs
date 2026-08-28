using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Base endpoint for updating an existing secret manager configuration.
/// Route: PUT /secret-managers/{Name}
/// </summary>
public abstract class UpdateSecretManagerEndpointBase : Endpoint<UpdateSecretManagerRequest, SecretManagerDetailResponse>
{
    private readonly SecretManagerConfigurationProvider _configProvider;
    private readonly ILogger<UpdateSecretManagerEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSecretManagerEndpointBase"/> class.
    /// </summary>
    protected UpdateSecretManagerEndpointBase(
        SecretManagerConfigurationProvider configProvider,
        ILogger<UpdateSecretManagerEndpointBase> logger)
    {
        _configProvider = configProvider;
        _logger = logger ?? NullLogger<UpdateSecretManagerEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Patch("/secret-managers/{Name}");
        Policies("secretmanagers:delete");
        Summary(s =>
        {
            s.Summary = "Update secret manager";
            s.Description = "Updates an existing secret manager configuration.";
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

            existing.Description = req.Description ?? existing.Description;
            existing.Environment = req.Environment;

            var saveResult = await _configProvider.Save(existing, ct).ConfigureAwait(false);
            if (saveResult.IsFailure)
            {
                SecretManagerEndpointLog.UpdateFailed(_logger, saveResult.CurrentMessage ?? "Unknown error");
                AddError(saveResult.CurrentMessage ?? "Failed to update secret manager configuration");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            SecretManagerEndpointLog.SecretManagerUpdated(_logger, req.Name);

            var detail = new SecretManagerDetailResponse
            {
                Id = existing.Id,
                Name = existing.Name,
                SecretManagerType = existing.SecretManagerType,
                Description = existing.Description,
                ServiceOptionType = existing.ServiceOptionType
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

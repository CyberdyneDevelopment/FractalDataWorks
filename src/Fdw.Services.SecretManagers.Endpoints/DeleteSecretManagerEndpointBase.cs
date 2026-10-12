using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Base endpoint for deleting a secret manager configuration.
/// Route: DELETE /secret-managers/{Name}
/// </summary>
public abstract class DeleteSecretManagerEndpointBase : Endpoint<DeleteSecretManagerRequest>
{
    private readonly ISecretManagerConfigurationProvider _configProvider;
    private readonly ILogger<DeleteSecretManagerEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSecretManagerEndpointBase"/> class.
    /// </summary>
    protected DeleteSecretManagerEndpointBase(
        ISecretManagerConfigurationProvider configProvider,
        ILogger<DeleteSecretManagerEndpointBase> logger)
    {
        _configProvider = configProvider;
        _logger = logger ?? NullLogger<DeleteSecretManagerEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete("/secret-managers/{Name}");
        // Secret managers are the platform's, owned by the system tenant: only a platform administrator lists, reads
        // or changes one. A tenant never touches a secret manager or a secret; a connection's credentials are set
        // through the connection (FDW-1086).
        Policies(AuthConstants.PlatformAdminPolicy);
        Summary(s =>
        {
            s.Summary = "Delete secret manager";
            s.Description = "Deletes a secret manager configuration by name.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add Tags or other endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc/>
    public override async Task HandleAsync(DeleteSecretManagerRequest req, CancellationToken ct)
    {
        try
        {
            SecretManagerEndpointLog.DeletingSecretManager(_logger, req.Name);

            var existingResult = await _configProvider.Get(req.Name, ct).ConfigureAwait(false);
            var existing = existingResult.IsSuccess ? existingResult.Value : null;

            if (existing == null)
            {
                SecretManagerEndpointLog.SecretManagerNotFound(_logger, req.Name);
                await HttpContext.WriteNotFound("SecretManager", req.Name, ct).ConfigureAwait(false);
                return;
            }

            var deleteResult = await _configProvider.Delete(existing.Id, ct).ConfigureAwait(false);
            if (deleteResult.IsFailure)
            {
                SecretManagerEndpointLog.DeleteFailed(_logger, deleteResult.CurrentMessage ?? "Unknown error");
                AddError(deleteResult.CurrentMessage ?? "Failed to delete secret manager configuration");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            SecretManagerEndpointLog.SecretManagerDeleted(_logger, req.Name);
            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SecretManagerEndpointLog.UnexpectedError(_logger, ex);
            throw;
        }
    }
}

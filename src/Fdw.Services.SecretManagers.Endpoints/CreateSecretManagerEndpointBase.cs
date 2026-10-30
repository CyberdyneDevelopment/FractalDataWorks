using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Base endpoint for creating a new secret manager configuration.
/// Route: POST /secret-managers
/// </summary>
public abstract class CreateSecretManagerEndpointBase : Endpoint<CreateSecretManagerRequest, SecretManagerDetailResponse>
{
    private readonly ISecretManagerConfigurationProvider _configProvider;
    private readonly ILogger<CreateSecretManagerEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSecretManagerEndpointBase"/> class.
    /// </summary>
    protected CreateSecretManagerEndpointBase(
        ISecretManagerConfigurationProvider configProvider,
        ILogger<CreateSecretManagerEndpointBase> logger)
    {
        _configProvider = configProvider;
        _logger = logger ?? NullLogger<CreateSecretManagerEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/secret-managers");
        // Secret managers are the platform's, owned by the system tenant: only a platform administrator lists, reads
        // or changes one. A tenant never touches a secret manager or a secret; a connection's credentials are set
        // through the connection (FDW-1086).
        Policies(AuthConstants.PlatformAdminPolicy);
        Summary(s =>
        {
            s.Summary = "Create secret manager";
            s.Description = "Creates a new secret manager configuration.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add Tags or other endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc/>
    public override async Task HandleAsync(CreateSecretManagerRequest req, CancellationToken ct)
    {
        try
        {
            SecretManagerEndpointLog.CreatingSecretManager(_logger, req.Name, req.SecretManagerType);

            if (string.IsNullOrWhiteSpace(req.Name))
            {
                AddError("Name is required");
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(req.SecretManagerType))
            {
                AddError("SecretManagerType is required");
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            var existingResult = await _configProvider.Get(req.Name, ct).ConfigureAwait(false);
            var existing = existingResult.IsSuccess ? existingResult.Value : null;

            if (existing != null)
            {
                SecretManagerEndpointLog.SecretManagerAlreadyExists(_logger, req.Name);
                AddError($"A secret manager with name '{req.Name}' already exists");
                await Send.ErrorsAsync(409, ct).ConfigureAwait(false);
                return;
            }

            var domainConfigurationId = Guid.NewGuid();
            var bodyResult = SecretManagerSettings.Read(req.SecretManagerType, req.Configuration, null, _logger);
            if (bodyResult.IsFailure || bodyResult.Value is null)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(bodyResult, HttpContext)).ConfigureAwait(false);
                return;
            }

            // The settings reader already produced the implementation this row is, so it is what gets
            // saved. The domain save takes a name and the implementation, no description: the request does not take one.
            var config = bodyResult.Value;
            config.Id = domainConfigurationId;

            var saveResult = await _configProvider.Save(config, "SecretManager", req.SecretManagerType, req.Name, ct).ConfigureAwait(false);
            if (saveResult.IsFailure)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(saveResult, HttpContext)).ConfigureAwait(false);
                return;
            }

            SecretManagerEndpointLog.SecretManagerCreated(_logger, req.Name);

            var detail = new SecretManagerDetailResponse
            {
                Id = config.Id,
                Name = req.Name,
                SecretManagerType = req.SecretManagerType,
                Implementation = req.SecretManagerType
            };

            await Send.ResponseAsync(detail, 201, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SecretManagerEndpointLog.UnexpectedError(_logger, ex);
            throw;
        }
    }
}

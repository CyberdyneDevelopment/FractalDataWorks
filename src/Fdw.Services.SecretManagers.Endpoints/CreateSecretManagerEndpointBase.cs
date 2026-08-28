using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Base endpoint for creating a new secret manager configuration.
/// Route: POST /secret-managers
/// </summary>
public abstract class CreateSecretManagerEndpointBase : Endpoint<CreateSecretManagerRequest, SecretManagerDetailResponse>
{
    private readonly SecretManagerConfigurationProvider _configProvider;
    private readonly ILogger<CreateSecretManagerEndpointBase> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSecretManagerEndpointBase"/> class.
    /// </summary>
    protected CreateSecretManagerEndpointBase(
        SecretManagerConfigurationProvider configProvider,
        ILogger<CreateSecretManagerEndpointBase> logger)
    {
        _configProvider = configProvider;
        _logger = logger ?? NullLogger<CreateSecretManagerEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/secret-managers");
        Policies("secretmanagers:delete");
        Summary(s =>
        {
            s.Summary = "Create secret manager";
            s.Description = "Creates a new secret manager configuration.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add Tags or other endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    private static (ISecretManagerImplementationConfiguration? Body, string? Error) BuildTypedBody(
        CreateSecretManagerRequest req, Guid domainConfigurationId)
    {
        var option = SecretManagerTypes.ByName(req.SecretManagerType);
        if (option is null)
            return (null, $"Unknown SecretManagerType: '{req.SecretManagerType}'");

        var typedType = option.ConfigurationType;

        ISecretManagerImplementationConfiguration? typedBody;
        try
        {
            typedBody = (ISecretManagerImplementationConfiguration?)req.Configuration!.Value.Deserialize(typedType, JsonOptions);
        }
        catch (JsonException jex)
        {
            return (null, $"Configuration body could not be parsed as {typedType.Name}: {jex.Message}");
        }
        if (typedBody is null)
            return (null, $"Configuration body could not be parsed as {typedType.Name}");

        if (typedBody.Id == Guid.Empty)
            typedBody.Id = Guid.CreateVersion7();
        typedBody.SecretManagerId = domainConfigurationId;

        return (typedBody, null);
    }

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
            var bodyResult = BuildTypedBody(req, domainConfigurationId);
            if (bodyResult.Error is not null)
            {
                AddError(bodyResult.Error);
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            var config = new SecretManagerConfiguration
            {
                Id = domainConfigurationId,
                Name = req.Name,
                ServiceOptionType = req.SecretManagerType,
                Description = req.Description,
                Environment = req.Environment,
                Configuration = bodyResult.Body
            };

            var saveResult = await _configProvider.Save(config, ct).ConfigureAwait(false);
            if (saveResult.IsFailure)
            {
                SecretManagerEndpointLog.SaveFailed(_logger, saveResult.CurrentMessage ?? "Unknown error");
                AddError(saveResult.CurrentMessage ?? "Failed to save secret manager configuration");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            SecretManagerEndpointLog.SecretManagerCreated(_logger, req.Name);

            var detail = new SecretManagerDetailResponse
            {
                Id = config.Id,
                Name = config.Name,
                SecretManagerType = config.SecretManagerType,
                Description = config.Description,
                ServiceOptionType = config.ServiceOptionType
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

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
/// Base endpoint for getting a specific secret manager by name.
/// Route: GET /secret-managers/{Name}
/// </summary>
public abstract class GetSecretManagerEndpointBase : Endpoint<GetSecretManagerRequest, SecretManagerDetailResponse>
{
    private readonly SecretManagerConfigurationProvider _configProvider;
    private readonly ILogger<GetSecretManagerEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSecretManagerEndpointBase"/> class.
    /// </summary>
    protected GetSecretManagerEndpointBase(
        SecretManagerConfigurationProvider configProvider,
        ILogger<GetSecretManagerEndpointBase> logger)
    {
        _configProvider = configProvider;
        _logger = logger ?? NullLogger<GetSecretManagerEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/secret-managers/{Name}");
        Policies("secretmanagers:read");
        Summary(s =>
        {
            s.Summary = "Get secret manager";
            s.Description = "Returns detail information for a specific secret manager configuration.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add Tags or other endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc/>
    public override async Task HandleAsync(GetSecretManagerRequest req, CancellationToken ct)
    {
        SecretManagerEndpointLog.GettingSecretManager(_logger, req.Name);

        var getResult = await _configProvider.Get(req.Name, ct).ConfigureAwait(false);
        var config = getResult.IsSuccess ? getResult.Value : null;

        if (config == null)
        {
            SecretManagerEndpointLog.SecretManagerNotFound(_logger, req.Name);
            await HttpContext.WriteNotFound("SecretManager", req.Name, ct).ConfigureAwait(false);
            return;
        }

        var detail = new SecretManagerDetailResponse
        {
            Id = config.Id,
            Name = config.Name,
            SecretManagerType = config.SecretManagerType,
            Description = config.Description,
            ServiceOptionType = config.ServiceOptionType
        };

        await Send.OkAsync(detail, ct).ConfigureAwait(false);
    }
}

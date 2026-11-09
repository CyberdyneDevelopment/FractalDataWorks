using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Configuration.Endpoints.Logging;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

namespace Fdw.Configuration.Endpoints;

/// <summary>
/// Base endpoint for getting a specific configuration instance with all values.
/// </summary>
public abstract class GetConfigurationInstanceEndpointBase : Endpoint<GetConfigurationInstanceRequest, ConfigurationInstanceDetailResponse>
{
    private static readonly HashSet<string> SystemFields = new(StringComparer.OrdinalIgnoreCase)
        { "Id", "Name", "Type", "ServiceType", "CreatedAt", "ModifiedAt" };

    private readonly IConfigurationGatewayProvider _configurationGateways;

    private static string PlatformConfigurationConnection => PlatformConnection.Name;

    // The platform's own rows are read through the configuration gateway onto that store, never the request-facing
    // Main gateway (FDW-1068): Main runs whatever data store a request names, and these rows are not a request's to name.
    private IDataGateway Gateway => _configurationGateways.Get(PlatformConfigurationConnection) is { IsSuccess: true, Value: { } gateway }
        ? gateway
        : throw new InvalidOperationException(
            $"No configuration gateway is available for connection '{PlatformConfigurationConnection}'. It must be declared in the host's configurationSchema.json.");
    private readonly IConfigurationContainerLookup _containerLookup;
    private readonly ILogger<GetConfigurationInstanceEndpointBase> _logger;

    /// <summary>Initializes a new instance of the endpoint.</summary>
    protected GetConfigurationInstanceEndpointBase(
        IConfigurationGatewayProvider configurationGateways,
        IConfigurationContainerLookup containerLookup,
        ILogger<GetConfigurationInstanceEndpointBase> logger)
    {
        _configurationGateways = configurationGateways ?? throw new ArgumentNullException(nameof(configurationGateways));
        _containerLookup = containerLookup;
        _logger = logger ?? NullLogger<GetConfigurationInstanceEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/configuration/instances/{Category}/{Name}");
        Policies("configurations:read");
        Summary(s =>
        {
            s.Summary = "Get configuration instance details";
            s.Description = "Returns detailed configuration values for a specific instance.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(GetConfigurationInstanceRequest req, CancellationToken ct)
    {
        ConfigurationEndpointLog.GettingInstance(_logger, req.Category, req.Name);

        var containers = _containerLookup.ByCategory(req.Category);
        if (containers.Count == 0)
        {
            ConfigurationEndpointLog.NoCategoryTypes(_logger, req.Category);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(ConfigurationEndpointLog.GetConfigurationInstanceNotFound(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        ConfigurationInstanceDetailResponse? detail;
        try
        {
            detail = await FindInstanceDetail(req, containers, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ConfigurationEndpointLog.TableQueryFailed(_logger, ex, req.Category, $"instance {req.Name}");
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(ConfigurationEndpointLog.GetConfigurationInstanceFaulted(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        if (detail is not null)
        {
            await Send.OkAsync(detail, ct).ConfigureAwait(false);
            return;
        }

        ConfigurationEndpointLog.InstanceNotFound(_logger, req.Name, req.Category);
        await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(ConfigurationEndpointLog.GetConfigurationInstanceNotFound2(_logger)), HttpContext)).ConfigureAwait(false);
    }

    private async Task<ConfigurationInstanceDetailResponse?> FindInstanceDetail(
        GetConfigurationInstanceRequest req,
        IReadOnlyList<IDataContainer> containers,
        CancellationToken ct)
    {
        foreach (var container in containers)
        {
            var command = DataQuery.From<Dictionary<string, object?>>(PlatformConfigurationConnection, container.Parent.Name, container.Name)
                .Where("Name", req.Name)
                .Build();

            try
            {
                var result = await Gateway.Execute<IEnumerable<Dictionary<string, object?>>>(command, ct)
                    .ConfigureAwait(false);

                if (!result.IsSuccess || result.Value?.Any() != true)
                {
                    continue;
                }

                return MapToDetail(result.Value.First(), container, req.Name, req.Category);
            }
            catch (Exception ex)
            {
                ConfigurationEndpointLog.TableQueryFailed(_logger, ex, container.Name, $"instance {req.Name}");
                throw;
            }
        }

        return null;
    }

    private ConfigurationInstanceDetailResponse MapToDetail(
        Dictionary<string, object?> record,
        IDataContainer container,
        string requestedName,
        string category)
    {
        // Every stored instance has an Id, a Name and a CreatedAt. A row without one is broken data: it fails here,
        // and is never returned with an invented value standing in for the missing column.
        if (!record.TryGetValue("Id", out var idValue) || idValue is not Guid id)
            throw RowIncomplete(container, requestedName, "Id");

        if (!record.TryGetValue("Name", out var nameValue) || nameValue?.ToString() is not { } name)
            throw RowIncomplete(container, requestedName, "Name");

        if (!record.TryGetValue("CreatedAt", out var createdValue) || createdValue is not DateTime createdAt)
            throw RowIncomplete(container, requestedName, "CreatedAt");

        var serviceType = record.TryGetValue("Type", out var typeValue)
            ? typeValue?.ToString()
            : null;
        var modifiedAt = record.TryGetValue("ModifiedAt", out var modifiedValue) && modifiedValue is DateTime modified
            ? (DateTime?)modified : null;

        return new ConfigurationInstanceDetailResponse
        {
            Id = id,
            Name = name,
            ServiceType = serviceType,
            Category = category,
            Values = ExtractValues(record),
            CreatedAt = createdAt,
            ModifiedAt = modifiedAt
        };
    }

    private InvalidOperationException RowIncomplete(IDataContainer container, string requestedName, string column) =>
        new(ConfigurationEndpointLog.InstanceRowIncomplete(_logger, requestedName, container.Name, column).Message);

    private static Dictionary<string, object?> ExtractValues(Dictionary<string, object?> record)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in record)
        {
            if (SystemFields.Contains(kvp.Key))
            {
                continue;
            }

            var isSecret = IsSecretProperty(kvp.Key);
            values[kvp.Key] = isSecret && kvp.Value is not null ? "********" : kvp.Value;
        }

        return values;
    }

    private static bool IsSecretProperty(string? propertyName)
    {
        if (propertyName is null)
        {
            return false;
        }

        return propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("ApiKey", StringComparison.OrdinalIgnoreCase);
    }
}

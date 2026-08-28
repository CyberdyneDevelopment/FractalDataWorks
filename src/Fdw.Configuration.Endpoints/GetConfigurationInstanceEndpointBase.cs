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

namespace Fdw.Configuration.Endpoints;

/// <summary>
/// Base endpoint for getting a specific configuration instance with all values.
/// </summary>
public abstract class GetConfigurationInstanceEndpointBase : Endpoint<GetConfigurationInstanceRequest, ConfigurationInstanceDetailResponse>
{
    private static readonly HashSet<string> SystemFields = new(StringComparer.OrdinalIgnoreCase)
        { "Id", "Name", "Type", "ServiceType", "CreatedAt", "ModifiedAt" };

    private readonly IDataGateway _dataGateway;
    private readonly IConfigurationContainerLookup _containerLookup;
    private readonly ILogger<GetConfigurationInstanceEndpointBase> _logger;

    /// <summary>Initializes a new instance of the endpoint.</summary>
    protected GetConfigurationInstanceEndpointBase(
        IDataGateway dataGateway,
        IConfigurationContainerLookup containerLookup,
        ILogger<GetConfigurationInstanceEndpointBase> logger)
    {
        _dataGateway = dataGateway;
        _containerLookup = containerLookup;
        _logger = logger;
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
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
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
            AddError("An error occurred while retrieving the configuration instance");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (detail is not null)
        {
            await Send.OkAsync(detail, ct).ConfigureAwait(false);
            return;
        }

        ConfigurationEndpointLog.InstanceNotFound(_logger, req.Name, req.Category);
        await Send.NotFoundAsync(ct).ConfigureAwait(false);
    }

    private async Task<ConfigurationInstanceDetailResponse?> FindInstanceDetail(
        GetConfigurationInstanceRequest req,
        IReadOnlyList<IDataContainer> containers,
        CancellationToken ct)
    {
        foreach (var container in containers)
        {
            var command = DataQuery.From<Dictionary<string, object?>>("PlatformConfiguration", container.Parent.Name, container.Name)
                .Where("Name", req.Name)
                .Build();

            try
            {
                var result = await _dataGateway.Execute<IEnumerable<Dictionary<string, object?>>>(command, ct)
                    .ConfigureAwait(false);

                if (!result.IsSuccess || result.Value?.Any() != true)
                {
                    continue;
                }

                return MapToDetail(result.Value.First(), container, req.Category);
            }
            catch (Exception ex)
            {
                ConfigurationEndpointLog.TableQueryFailed(_logger, ex, container.Name, $"instance {req.Name}");
                throw;
            }
        }

        return null;
    }

    private static ConfigurationInstanceDetailResponse MapToDetail(
        Dictionary<string, object?> record,
        IDataContainer container,
        string category)
    {
        var id = record.TryGetValue("Id", out var idValue) && idValue is Guid guidId
            ? guidId : Guid.Empty;
        var name = record.TryGetValue("Name", out var nameValue)
            ? nameValue?.ToString() ?? string.Empty : string.Empty;
        var serviceType = record.TryGetValue("Type", out var typeValue)
            ? typeValue?.ToString() ?? container.Name
            : container.Name;
        var createdAt = record.TryGetValue("CreatedAt", out var createdValue) && createdValue is DateTime created
            ? created : DateTime.UtcNow;
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

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
/// Base endpoint for listing configuration instances with optional category filter.
/// </summary>
public abstract class ListConfigurationInstancesEndpointBase : Endpoint<ListConfigurationInstancesRequest, List<ConfigurationInstanceSummaryResponse>>
{
    private readonly IConfigurationGatewayProvider _configurationGateways;

    private static string PlatformConfigurationConnection => PlatformConnection.Name;

    // The platform's own rows are read through the configuration gateway onto that store, never the request-facing
    // Main gateway (FDW-1068): Main runs whatever data store a request names, and these rows are not a request's to name.
    private IDataGateway Gateway => _configurationGateways.Get(PlatformConfigurationConnection) is { IsSuccess: true, Value: { } gateway }
        ? gateway
        : throw new InvalidOperationException(
            $"No configuration gateway is available for connection '{PlatformConfigurationConnection}'. It must be declared in the host's configurationSchema.json.");
    private readonly IConfigurationContainerLookup _containerLookup;
    private readonly ILogger<ListConfigurationInstancesEndpointBase> _logger;

    /// <summary>Initializes a new instance of the endpoint.</summary>
    protected ListConfigurationInstancesEndpointBase(
        IConfigurationGatewayProvider configurationGateways,
        IConfigurationContainerLookup containerLookup,
        ILogger<ListConfigurationInstancesEndpointBase> logger)
    {
        _configurationGateways = configurationGateways ?? throw new ArgumentNullException(nameof(configurationGateways));
        _containerLookup = containerLookup;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/configuration/instances");
        Policies("configurations:read");
        Summary(s =>
        {
            s.Summary = "List configuration instances";
            s.Description = "Returns all configuration instances, optionally filtered by category.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListConfigurationInstancesRequest req, CancellationToken ct)
    {
        ConfigurationEndpointLog.ListingInstances(_logger, req.Category ?? "none");

        var containers = string.IsNullOrEmpty(req.Category)
            ? _containerLookup.All()
            : _containerLookup.ByCategory(req.Category);

        var tableGroups = containers
            .GroupBy(c => $"{c.Parent.Name}.{c.Name}", StringComparer.Ordinal)
            .ToList();

        List<ConfigurationInstanceSummaryResponse> instances;
        try
        {
            instances = await QueryInstances(tableGroups, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ConfigurationEndpointLog.TableQueryFailed(_logger, ex, "configuration", "listing instances");
            AddError("An error occurred while retrieving configuration instances");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        ConfigurationEndpointLog.InstancesRetrieved(_logger, instances.Count);
        await Send.OkAsync(
            instances.OrderBy(i => i.Category, StringComparer.Ordinal)
                .ThenBy(i => i.Name, StringComparer.Ordinal).ToList(), ct).ConfigureAwait(false);
    }

    private async Task<List<ConfigurationInstanceSummaryResponse>> QueryInstances(
        List<IGrouping<string, IDataContainer>> tableGroups,
        CancellationToken ct)
    {
        var instances = new List<ConfigurationInstanceSummaryResponse>();

        foreach (var group in tableGroups)
        {
            var container = group.First();

            try
            {
                var command = DataQuery.From<ConfigurationRecord>(PlatformConfigurationConnection, container.Parent.Name, container.Name)
                    .Build();

                var result = await Gateway.Execute<IEnumerable<ConfigurationRecord>>(command, ct)
                    .ConfigureAwait(false);

                if (result.IsSuccess && result.Value != null)
                {
                    foreach (var record in result.Value)
                    {
                        instances.Add(MapToSummary(record, container));
                    }
                }
            }
            catch (Exception ex)
            {
                ConfigurationEndpointLog.TableQueryFailed(_logger, ex, container.Name, "configuration instances");
                throw;
            }
        }

        return instances;
    }

    private static ConfigurationInstanceSummaryResponse MapToSummary(ConfigurationRecord record, IDataContainer container)
    {
        var serviceType = record.Type ?? container.Name;

        return new ConfigurationInstanceSummaryResponse
        {
            Id = record.Id,
            Name = record.Name,
            ServiceType = serviceType,
            Category = container.Parent.Name,
            CreatedAt = record.CreatedAt,
            ModifiedAt = record.ModifiedAt
        };
    }
}

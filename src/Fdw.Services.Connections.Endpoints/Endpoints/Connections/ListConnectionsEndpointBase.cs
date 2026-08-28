using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Generic base endpoint for listing all configured connections.
/// Reads from the connection configuration provider (dual-source: ctrl + cfg),
/// so newly created connections appear without requiring a server restart.
/// </summary>
public abstract class ListConnectionsEndpointBase : CrudListEndpointBase<ConnectionSummaryDto>
{
    private readonly ConnectionConfigurationProvider _configProvider;

    /// <inheritdoc />
    protected ListConnectionsEndpointBase(ConnectionConfigurationProvider configProvider)
    {
        _configProvider = configProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "connections";

    /// <summary>Loads connection configurations filtered to the caller's permitted scope.</summary>
    protected override async Task<IGenericResult<List<ConnectionSummaryDto>>> LoadItems(CancellationToken ct)
    {
        var allResult = await _configProvider.Get(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess)
        {
            return allResult.ToNewResult<List<ConnectionSummaryDto>>();
        }

        var items = (allResult.Value ?? (IReadOnlyList<ConnectionConfiguration>)[])
            .Where(config => !string.IsNullOrWhiteSpace(config.Name)
                && !string.IsNullOrWhiteSpace(config.ServiceOptionType)
                && !string.Equals(config.ServiceOptionType, "Connection", StringComparison.OrdinalIgnoreCase))
            .GroupBy(config => config.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(MapToSummary)
            .ToList();

        return GenericResult<List<ConnectionSummaryDto>>.Success(items);
    }

    /// <summary>Maps a single connection configuration to a summary DTO.</summary>
    protected virtual ConnectionSummaryDto MapToSummary(ConnectionConfiguration config)
    {
        return new ConnectionSummaryDto
        {
            Id = config.Id,
            Name = config.Name,
            ConnectionType = config.ConnectionType ?? config.ServiceOptionType ?? "Unknown",
        };
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Lists the latest health check result for every connection — a dashboard summary, as opposed to
/// <see cref="GetConnectionHealthEndpointBase"/>'s per-connection history.
/// </summary>
public abstract class ListConnectionHealthEndpointBase : CrudListEndpointBase<ConnectionHealthSummaryDto>
{
    private readonly IConnectionHealthService _healthService;

    /// <inheritdoc />
    protected ListConnectionHealthEndpointBase(ILogger<ListConnectionHealthEndpointBase> logger, IConnectionHealthService healthService) : base(logger)
    {
        _healthService = healthService;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "connections";

    /// <summary>Routes at <c>/connections/health</c> rather than the default <c>/connections</c>,
    /// which is already <see cref="ListConnectionsEndpointBase"/>'s route.</summary>
    protected override string Route => "/connections/health";

    /// <summary>Loads the latest health status for every connection.</summary>
    protected override async Task<IGenericResult<List<ConnectionHealthSummaryDto>>> LoadItems(CancellationToken ct)
    {
        var allResult = await _healthService.GetAllCurrent(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess)
            return allResult.ToNewResult<List<ConnectionHealthSummaryDto>>();

        var items = (allResult.Value ?? [])
            .Select(MapToSummary)
            .ToList();

        return GenericResult<List<ConnectionHealthSummaryDto>>.Success(items);
    }

    /// <summary>Maps one current-health record to its DTO.</summary>
    protected virtual ConnectionHealthSummaryDto MapToSummary(ConnectionHealthCurrentRecord record) => new()
    {
        ConnectionId = record.ConnectionId,
        ConnectionName = record.ConnectionName,
        Status = record.Status,
        IsHealthy = record.IsHealthy,
        ResponseTimeMs = record.ResponseTimeMs,
        ErrorMessage = record.ErrorMessage,
        LastCheckedAt = record.LastCheckedAt,
    };
}

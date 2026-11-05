using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Retires the level of thresholds on waiting runs an owner states when the owner is deleted (FDW-3161, FDW-3975), so no level is left naming a pipeline or a schedule that
/// is gone.
/// </summary>
public sealed class WaitThresholdOwnerRelease
{
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly ILogger _logger;
    private readonly WaitThresholdRows _rows;

    /// <summary>Initializes a new instance of the <see cref="WaitThresholdOwnerRelease"/> class.</summary>
    /// <param name="gateways">Reaches the configuration containers.</param>
    /// <param name="logger">The logger.</param>
    public WaitThresholdOwnerRelease(IConfigurationGatewayProvider gateways, ILogger? logger = null)
    {
        _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        _logger = logger ?? NullLogger.Instance;
        _rows = new WaitThresholdRows(_logger);
    }

    /// <summary>Retires the level of a pipeline that is deleted.</summary>
    /// <param name="name">The pipeline's name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or the failure that left the level in place.</returns>
    public Task<IGenericResult> ReleasePipeline(string name, CancellationToken cancellationToken = default) =>
        Release(WaitThresholdOwners.Pipeline, name, cancellationToken);

    /// <summary>Retires the level of a schedule that is deleted.</summary>
    /// <param name="name">The schedule's name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or the failure that left the level in place.</returns>
    public Task<IGenericResult> ReleaseSchedule(string name, CancellationToken cancellationToken = default) =>
        Release(WaitThresholdOwners.Schedule, name, cancellationToken);

    private async Task<IGenericResult> Release(string kind, string name, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var gateway = _gateways.Get(EtlPipelineTypes.ConfigurationConnection);
        if (gateway.IsFailure || gateway.Value is null)
        {
            return gateway;
        }

        var levels = await _rows.Levels(gateway.Value, cancellationToken).ConfigureAwait(false);
        if (levels.IsFailure || levels.Value is null)
        {
            return levels;
        }

        var owned = levels.Value.Where(row => WaitThresholdRows.Owns(kind, name, row.OwnerKind, row.OwnerName)).Select(row => row.Id).ToList();
        var retired = await WaitThresholdRows.Retire(gateway.Value, WaitThresholdRows.LevelContainer, owned, cancellationToken).ConfigureAwait(false);
        return retired.IsFailure ? GenericResult.Failure(WaitThresholdLog.NotReleased(_logger, string.Concat(kind, ":", name), retired.CurrentMessage)) : retired;
    }
}

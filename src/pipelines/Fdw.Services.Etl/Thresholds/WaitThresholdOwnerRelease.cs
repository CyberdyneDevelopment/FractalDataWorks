using System;
using System.Collections.Generic;
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
/// Removes the level of thresholds on waiting runs of a pipeline or a schedule that is being deleted (FDW-3271), so no level names an owner that is gone: such a level fails
/// every run at the queue, and a new owner of the same name would inherit it. A pipeline that a threshold of ANOTHER owner counts is not released and the failure names that
/// threshold and its owner, because the threshold would be left in a state a save refuses to create and would guard nothing. It needs only the configuration gateway, so the
/// scheduler host, which does not run the queue, builds it for the schedule delete the way the ETL host does for the pipeline delete.
/// </summary>
public sealed class WaitThresholdOwnerRelease
{
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly ILogger _logger;
    private readonly WaitThresholdRows _rows;

    /// <summary>Initializes a new instance of the <see cref="WaitThresholdOwnerRelease"/> class.</summary>
    /// <param name="gateways">Reaches the configuration database.</param>
    /// <param name="logger">The logger.</param>
    public WaitThresholdOwnerRelease(IConfigurationGatewayProvider gateways, ILogger? logger = null)
    {
        _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        _logger = logger ?? NullLogger.Instance;
        _rows = new WaitThresholdRows(_logger);
    }

    /// <summary>Removes the level of a pipeline that is about to be deleted; fails, removing nothing, while a threshold of another owner counts it.</summary>
    /// <param name="name">The pipeline name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the pipeline has no level or its level is removed; a failure naming what stopped it.</returns>
    public Task<IGenericResult> ReleasePipeline(string name, CancellationToken cancellationToken = default) =>
        Release(WaitThresholdOwners.Pipeline, name, cancellationToken);

    /// <summary>Removes the level of a schedule that is about to be deleted.</summary>
    /// <param name="name">The schedule name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the schedule has no level or its level is removed; a failure naming what stopped it.</returns>
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

        var tables = await _rows.Tables(gateway.Value, cancellationToken).ConfigureAwait(false);
        if (tables.IsFailure || tables.Value is null)
        {
            return tables;
        }

        var counting = Counting(kind, name, tables.Value.Thresholds);
        if (counting is not null)
        {
            return GenericResult.Failure(WaitThresholdLog.PipelineStillCounted(_logger, name, counting.Name, OwnerText(counting)));
        }

        var thresholds = tables.Value.Thresholds.Where(row => WaitThresholdRows.Owns(kind, name, row.OwnerKind, row.OwnerName)).ToList();
        var rules = tables.Value.Rules.Where(rule => thresholds.Any(row => row.Id == rule.WaitThresholdId)).Select(rule => rule.Id).ToList();
        var levels = tables.Value.Levels.Where(row => WaitThresholdRows.Owns(kind, name, row.OwnerKind, row.OwnerName)).Select(row => row.Id).ToList();

        var retired = await WaitThresholdRows.Retire(gateway.Value, WaitThresholdRows.RuleContainer, rules, cancellationToken).ConfigureAwait(false);
        retired = retired.IsFailure ? retired : await WaitThresholdRows.Retire(gateway.Value, WaitThresholdRows.ThresholdContainer, [.. thresholds.Select(row => row.Id)], cancellationToken).ConfigureAwait(false);
        retired = retired.IsFailure ? retired : await WaitThresholdRows.Retire(gateway.Value, WaitThresholdRows.LevelContainer, levels, cancellationToken).ConfigureAwait(false);
        return retired.IsFailure ? GenericResult.Failure(WaitThresholdLog.NotReleased(_logger, string.Concat(kind, ":", name), retired.CurrentMessage)) : retired;
    }

    // Only a pipeline can be counted; the threshold that counts it and is not the pipeline's own.
    private static WaitThresholdRecord? Counting(string kind, string name, IReadOnlyList<WaitThresholdRecord> thresholds) =>
        string.Equals(kind, WaitThresholdOwners.Pipeline, StringComparison.Ordinal)
            ? thresholds.FirstOrDefault(row => string.Equals(row.CountedPipeline, name, StringComparison.OrdinalIgnoreCase) && !WaitThresholdRows.Owns(kind, name, row.OwnerKind, row.OwnerName))
            : null;

    private static string OwnerText(WaitThresholdRecord threshold) =>
        threshold.OwnerName is null ? threshold.OwnerKind : string.Concat(threshold.OwnerKind, " '", threshold.OwnerName, "'");
}

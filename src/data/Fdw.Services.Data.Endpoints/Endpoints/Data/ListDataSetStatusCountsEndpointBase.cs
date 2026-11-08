using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Settings;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Counts the data sets the caller may see by status, for a console that shows how many need attention or are running (FDW-4488).
/// </summary>
/// <remarks>
/// Every status of <c>DataSetStatuses</c> is listed, highest rank first, with the number of data sets that have it, so a status nothing has reads 0 and a status a
/// developer added appears. Variations are not counted: they hold no data of their own and have no status. The writers, their latest runs and the window are read
/// once, as for the list, so the cost does not grow with the number of data sets.
/// </remarks>
public abstract class ListDataSetStatusCountsEndpointBase : CrudListEndpointBase<DataSetStatusCountDto>
{
    private readonly DataSetConfigurationProvider _dataSets;
    private readonly IDataSetWriters _writers;
    private readonly IExecutionTracker _runs;
    private readonly IEffectiveSettingsProvider _settings;
    private readonly ITenantContext _tenant;
    private readonly TimeProvider _time;

    /// <summary>Initializes a new instance of the <see cref="ListDataSetStatusCountsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSets">Reads the data sets as records.</param>
    /// <param name="writers">Which pipelines write each data set.</param>
    /// <param name="runs">Where the latest run of each writer is read.</param>
    /// <param name="settings">Where the status window is read.</param>
    /// <param name="tenant">The caller's tenant, whose own status window applies when it has one.</param>
    /// <param name="time">The clock the status is judged by.</param>
    protected ListDataSetStatusCountsEndpointBase(
        ILogger<ListDataSetStatusCountsEndpointBase> logger,
        DataSetConfigurationProvider dataSets,
        IDataSetWriters writers,
        IExecutionTracker runs,
        IEffectiveSettingsProvider settings,
        ITenantContext tenant,
        TimeProvider time)
        : base(logger)
    {
        _dataSets = dataSets ?? throw new ArgumentNullException(nameof(dataSets));
        _writers = writers ?? throw new ArgumentNullException(nameof(writers));
        _runs = runs ?? throw new ArgumentNullException(nameof(runs));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        _time = time ?? throw new ArgumentNullException(nameof(time));
    }

    /// <summary>Gets the resource name used for route generation.</summary>
    protected override string ResourceName => "dataset-status-counts";

    /// <summary>Gated by "datasets:read": the counts are over the data sets the caller may read.</summary>
    protected override string ReadPolicy => "datasets:read";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<DataSetStatusCountDto>>> LoadItems(CancellationToken ct)
    {
        var statuses = await DataSetStatusLookup.Read(_writers, _runs, _settings, _tenant.TenantId, _time, Logger, ct).ConfigureAwait(false);
        if (statuses.IsFailure || statuses.Value is null)
            return statuses.ToNewResult<List<DataSetStatusCountDto>>();

        var dataSets = await _dataSets.Get(ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        return dataSets.IsFailure
            ? dataSets.ToNewResult<List<DataSetStatusCountDto>>()
            : Count(dataSets.Value!, statuses.Value, Logger);
    }

    /// <summary>Counts the data sets by status.</summary>
    /// <param name="dataSets">Every data set the caller may see.</param>
    /// <param name="statuses">What was read to judge a status.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <returns>Every status, highest rank first, with its number of data sets, or the failure that says no option applies to a data set.</returns>
    internal static IGenericResult<List<DataSetStatusCountDto>> Count(
        IEnumerable<IDataSetImplementationConfiguration> dataSets, DataSetStatusLookup statuses, ILogger logger)
    {
        var counts = DataSetStatuses.All()
            .OrderByDescending(status => status.Rank)
            .ThenBy(status => status.Name, StringComparer.Ordinal)
            .ToDictionary(status => status.Name, _ => 0, StringComparer.Ordinal);
        foreach (var dataSet in dataSets.Where(dataSet => dataSet is not IDataSetVariationImplementationConfiguration))
        {
            var status = statuses.StatusOf(dataSet.Name, logger);
            if (status.IsFailure || status.Value is null)
                return status.ToNewResult<List<DataSetStatusCountDto>>();

            counts[status.Value.Name]++;
        }

        return GenericResult<List<DataSetStatusCountDto>>.Success([.. counts.Select(pair => new DataSetStatusCountDto { Status = pair.Key, Count = pair.Value })]);
    }
}

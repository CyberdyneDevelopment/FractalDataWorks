using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// The status of the data sets the caller may see (FDW-4488): the data set domain's answer, behind <see cref="IDataSetStatuses"/>.
/// </summary>
/// <remarks>
/// <para>
/// It is scoped: the tenant whose status window applies, the pipelines that write the data sets and their latest runs are all the caller's. The pipelines are reached through
/// <see cref="IDataSetWriterRuns"/>, which they implement, so this domain does not depend on them.
/// </para>
/// <para>
/// The window is the setting <c>DataSetStatusWindowHours</c> as it resolves for the caller's tenant. A window nobody stated is a coded failure; nothing assumes one.
/// </para>
/// </remarks>
public sealed class DataSetStatusProvider : IDataSetStatuses
{
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IDataSetWriterRuns _writerRuns;
    private readonly IEffectiveSettingsProvider _settings;
    private readonly ITenantContext _tenant;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DataSetStatusProvider"/> class.</summary>
    /// <param name="dataSets">Reads the data sets as records, for the counts.</param>
    /// <param name="writerRuns">The pipelines that write each data set and their latest runs.</param>
    /// <param name="settings">Where the status window is read.</param>
    /// <param name="tenant">The caller's tenant, whose own status window applies when it has one.</param>
    /// <param name="time">The clock a status is judged by.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    public DataSetStatusProvider(
        IDataSetConfigurationProvider dataSets,
        IDataSetWriterRuns writerRuns,
        IEffectiveSettingsProvider settings,
        ITenantContext tenant,
        TimeProvider time,
        ILogger<DataSetStatusProvider>? logger = null)
    {
        _dataSets = dataSets;
        _writerRuns = writerRuns;
        _settings = settings;
        _tenant = tenant;
        _time = time;
        _logger = logger ?? NullLogger<DataSetStatusProvider>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataSetStatusView>> Read(CancellationToken cancellationToken = default)
    {
        if (!_settings.TryGetEffectiveValue<int>(SettingDefinitions.DataSetStatusWindowHours, out var hours, _tenant.TenantId) || hours <= 0)
        {
            return GenericResult<IDataSetStatusView>.Failure(
                DataSetsResultCodes.ByName("DataSetStatusWindowNotStated"), _logger, ResultDetails.Create("setting", SettingDefinitions.DataSetStatusWindowHours));
        }

        var runs = await _writerRuns.ByDataSet(cancellationToken).ConfigureAwait(false);
        return runs.IsFailure || runs.Value is null
            ? runs.ToNewResult<IDataSetStatusView>()
            : GenericResult<IDataSetStatusView>.Success(new DataSetStatusView(runs.Value, TimeSpan.FromHours(hours), _time.GetUtcNow(), _logger));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<DataSetStatusCount>>> Counts(CancellationToken cancellationToken = default)
    {
        var view = await Read(cancellationToken).ConfigureAwait(false);
        if (view.IsFailure || view.Value is null)
            return view.ToNewResult<IReadOnlyList<DataSetStatusCount>>();

        var dataSets = await _dataSets.Get(ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (dataSets.IsFailure || dataSets.Value is null)
            return dataSets.ToNewResult<IReadOnlyList<DataSetStatusCount>>();

        var statuses = DataSetStatuses.All()
            .OrderByDescending(status => status.Rank)
            .ThenBy(status => status.Name, StringComparer.Ordinal)
            .ToList();
        var counts = statuses.ToDictionary(status => status.Name, _ => 0, StringComparer.Ordinal);
        foreach (var dataSet in dataSets.Value)
        {
            var status = view.Value.StatusOf(dataSet);
            if (status.IsFailure)
                return status.ToNewResult<IReadOnlyList<DataSetStatusCount>>();

            if (status.Value is { } counted)
                counts[counted.Name]++;
        }

        return GenericResult<IReadOnlyList<DataSetStatusCount>>.Success([.. statuses.Select(status => new DataSetStatusCount(status, counts[status.Name]))]);
    }
}

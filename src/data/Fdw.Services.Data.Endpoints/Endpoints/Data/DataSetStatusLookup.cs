using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The status of each data set in a list, read once for the whole request (FDW-4488).
/// </summary>
/// <remarks>
/// A status needs the writers of every data set and the latest run of each writer, so they are read once, in a fixed number of statements, and each data set's status
/// is then looked up: the list costs the same however many data sets it holds. The window is the setting <c>DataSetStatusWindowHours</c> as it resolves for the
/// caller's tenant; nothing assumes one.
/// </remarks>
internal sealed class DataSetStatusLookup
{
    private static readonly IReadOnlyDictionary<string, IExecutionItem> NoRuns = new Dictionary<string, IExecutionItem>(StringComparer.OrdinalIgnoreCase);

    private readonly IReadOnlyDictionary<string, IReadOnlyList<DataSetWriter>> _writers;
    private readonly IReadOnlyDictionary<string, IExecutionItem> _latest;
    private readonly TimeSpan _window;
    private readonly DateTimeOffset _now;

    private DataSetStatusLookup(
        IReadOnlyDictionary<string, IReadOnlyList<DataSetWriter>> writers, IReadOnlyDictionary<string, IExecutionItem> latest, TimeSpan window, DateTimeOffset now)
    {
        _writers = writers;
        _latest = latest;
        _window = window;
        _now = now;
    }

    /// <summary>Reads the writers, their latest runs and the window.</summary>
    /// <param name="writers">Which pipelines write each data set.</param>
    /// <param name="runs">Where the latest run of each pipeline is read.</param>
    /// <param name="settings">Where the window is read.</param>
    /// <param name="tenantId">The caller's tenant, whose own window applies when it has one.</param>
    /// <param name="time">The clock the status is judged by.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lookup, or the failure that names what could not be read.</returns>
    internal static async Task<IGenericResult<DataSetStatusLookup>> Read(
        IDataSetWriters writers, IExecutionTracker runs, IEffectiveSettingsProvider settings, Guid? tenantId, TimeProvider time, ILogger logger, CancellationToken cancellationToken)
    {
        if (!settings.TryGetEffectiveValue<int>(SettingDefinitions.DataSetStatusWindowHours, out var hours, tenantId) || hours <= 0)
        {
            return GenericResult<DataSetStatusLookup>.Failure(
                DataSetsResultCodes.ByName("DataSetStatusWindowNotStated"), logger, ResultDetails.Create("setting", SettingDefinitions.DataSetStatusWindowHours));
        }

        var written = await writers.ByDataSet(cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return written.ToNewResult<DataSetStatusLookup>();

        var pipelines = written.Value!.Values.SelectMany(list => list).Select(writer => writer.PipelineName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (pipelines.Count == 0)
            return GenericResult<DataSetStatusLookup>.Success(new DataSetStatusLookup(written.Value!, NoRuns, TimeSpan.FromHours(hours), time.GetUtcNow()));

        var latest = await runs.GetLatestRuns(pipelines, cancellationToken).ConfigureAwait(false);
        return latest.IsFailure
            ? latest.ToNewResult<DataSetStatusLookup>()
            : GenericResult<DataSetStatusLookup>.Success(new DataSetStatusLookup(written.Value!, latest.Value!, TimeSpan.FromHours(hours), time.GetUtcNow()));
    }

    /// <summary>Works out one data set's status from what was read.</summary>
    /// <param name="dataSetName">The data set's name.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <returns>The status, or the failure that says no option applies.</returns>
    internal IGenericResult<IDataSetStatus> StatusOf(string dataSetName, ILogger logger)
    {
        var runs = _writers.TryGetValue(dataSetName, out var found)
            ? found.Select(writer => new DataSetWriterRun(writer.PipelineName, writer.IsEnabled, _latest.TryGetValue(writer.PipelineName, out var run) ? run : null)).ToList()
            : [];
        return DataSetStatusResolver.Resolve(dataSetName, new DataSetStatusInput(runs, _window, _now), logger);
    }
}

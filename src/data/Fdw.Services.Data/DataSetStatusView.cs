using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// The status of each data set in one request, worked out from what was read once (FDW-4488).
/// </summary>
internal sealed class DataSetStatusView : IDataSetStatusView
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<DataSetWriterRun>> _runs;
    private readonly TimeSpan _window;
    private readonly DateTimeOffset _now;
    private readonly ILogger _logger;

    internal DataSetStatusView(IReadOnlyDictionary<string, IReadOnlyList<DataSetWriterRun>> runs, TimeSpan window, DateTimeOffset now, ILogger logger)
    {
        _runs = runs ?? throw new ArgumentNullException(nameof(runs));
        _window = window;
        _now = now;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public IGenericResult<IDataSetStatus?> StatusOf(IDataSetImplementationConfiguration dataSet)
    {
        ArgumentNullException.ThrowIfNull(dataSet);

        // A variation holds no data of its own (it runs the data set it points at), so no pipeline writes it and it has no status of its own.
        if (dataSet is IDataSetVariationImplementationConfiguration)
            return GenericResult<IDataSetStatus?>.Success(null);

        var resolved = DataSetStatusResolver.Resolve(
            dataSet.Name, new DataSetStatusInput(_runs.TryGetValue(dataSet.Name, out var found) ? found : [], _window, _now), _logger);
        return resolved.IsFailure || resolved.Value is null
            ? resolved.ToNewResult<IDataSetStatus?>()
            : GenericResult<IDataSetStatus?>.Success(resolved.Value);
    }
}

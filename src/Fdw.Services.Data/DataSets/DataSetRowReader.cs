using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// The default <see cref="IDataSetRowReader"/>: resolves the name to a data set or a view of one, reads its rows through the gateway
/// it is handed the way the <c>/data</c> routes read them, and refuses a data set that holds more rows than the cap.
/// </summary>
/// <remarks>
/// <para>
/// Why the gateway is a parameter: the gateway is scoped to a caller, and this reader is registered once, so it takes the
/// caller's gateway on every read and never holds one.
/// </para>
/// <para>
/// Why <see cref="DataSetQuery"/>: it is the one path every route that returns a data set's rows reads it by (FDW-1075), so a
/// view's stored filters, the data set's read gate and the cut to the data set's declared fields are applied here exactly as they are
/// on <c>/data</c>, and the two cannot drift apart. A stored filter that cannot be read refuses, naming the view; the rows are never
/// read unfiltered.
/// </para>
/// </remarks>
public sealed class DataSetRowReader : IDataSetRowReader
{
    private readonly IDataSetResolver _resolver;
    private readonly DataSetQuery _query;
    private readonly ILogger<DataSetRowReader> _logger;

    /// <summary>Initializes a new instance of the <see cref="DataSetRowReader"/> class.</summary>
    /// <param name="resolver">Resolves a name to a data set or a variation of one.</param>
    /// <param name="logger">The logger; a null logger is replaced with <see cref="NullLogger{T}"/>.</param>
    /// <param name="calculations">
    /// Evaluates the calculations a view's filter names, as <c>/data</c> does; null for a host that has none, where a view whose
    /// filter names one is refused.
    /// </param>
    public DataSetRowReader(IDataSetResolver resolver, ILogger<DataSetRowReader>? logger = null, IFilterCalculations? calculations = null)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _logger = logger ?? NullLogger<DataSetRowReader>.Instance;
        _query = new DataSetQuery(_logger, calculations);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<Dictionary<string, object?>>>> Read(
        IDataGateway gateway,
        string dataSetName,
        int rowCap,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataSetName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rowCap);

        var resolved = await _resolver.Resolve(dataSetName, cancellationToken).ConfigureAwait(false);
        if (resolved.IsFailure)
            return resolved.ToNewResult<IReadOnlyList<Dictionary<string, object?>>>();

        if (resolved.Value is not { } named)
            return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(
                DataServiceResultCodes.ByName("DataSetNotFound"), _logger, ResultDetails.Create("DataSetName", dataSetName));

        // Why Take is the cap: the query reads one row past the page, so a data set of exactly the cap is told from one that holds
        // more, and a cut-short read of the second would hand back a calculation's input with rows missing. The view's stored filters
        // are the scope, read by the query after its own checks; one that cannot be read refuses before any row is.
        var page = await _query.Run(
            gateway,
            named.DataSet,
            new DataQueryRequest { Skip = 0, Take = rowCap },
            rowCap,
            rowCap,
            _ => Task.FromResult(StoredViewFilters.Of(named.ViewFilters, named.Name, _logger)),
            cancellationToken).ConfigureAwait(false);
        if (page.IsFailure)
            return page.ToNewResult<IReadOnlyList<Dictionary<string, object?>>>();

        if (page.Value is not { } read)
            return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(
                DataServiceResultCodes.ByName("DataQueryReturnedNoValue"), _logger, ResultDetails.Create("dataSet", named.Name));

        if (read.HasMoreRows)
            return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(
                DataSetRowReaderLog.OverRowCap(_logger, named.Name, rowCap));

        // Why copies: a calculation writes its result column into the rows it is handed, and the page's rows are read-only views.
        var rows = read.Rows.Select(row => new Dictionary<string, object?>(row, StringComparer.Ordinal)).ToList();
        DataSetRowReaderLog.Read(_logger, named.Name, rows.Count);
        return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Success(rows);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;
using Fdw.Web.RestEndpoints.Query;
using Fdw.Web.Http.Abstractions.EndPoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Queries one data set directly: <c>POST /data/{DataSetName}</c>.
/// </summary>
/// <remarks>
/// <para>
/// The data set as it is: the <c>datasets:read</c> permission lets a caller ask, and
/// the data store's row-level security under the caller's session decides which rows they get. When the
/// name is a view of a data set, the rows are the data set's, kept to the rows the stored filter of the view
/// and of every view above it keeps (FDW-2391). A
/// workspace is a layer over the same data set that answers a different view of it
/// (<c>POST /{WorkspaceName}/{DataSetName}</c>); both share the query in
/// <see cref="DataQueryEndpointBase{TRequest, TResponse}"/> (FDW-975).
/// </para>
/// <para>
/// A data set that does not exist answers 404 <c>DataSetNotFound</c>.
/// </para>
/// </remarks>
public abstract class QueryDataSetDataEndpointBase : DataQueryEndpointBase<DataQueryRequest, DataQueryResponse>
{
    private readonly IDataSetResolver _resolver;

    /// <summary>Initializes a new instance of the <see cref="QueryDataSetDataEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="resolver">Resolves the name to a data set, or to a view of one and the stored filters of its chain.</param>
    /// <param name="dataGateways">Supplies the gateway the query is dispatched through.</param>
    /// <param name="calculations">Evaluates the calculations a filter names for its values; null when the host has none.</param>
    protected QueryDataSetDataEndpointBase(
        ILogger<QueryDataSetDataEndpointBase> logger,
        IDataSetResolver resolver,
        IDataGatewayProvider dataGateways,
        IFilterCalculations? calculations = null)
        : base(logger ?? NullLogger<QueryDataSetDataEndpointBase>.Instance, dataGateways, calculations)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/data/{DataSetName}");
        Policies("datasets:read");
        Tags("DataSets");
        // Why the metadata: a caller no page limit applies to is answered by streaming every row, and the buffering
        // middleware would otherwise hold that response to set a Content-Length (FDW-1209).
        Options(builder => builder.WithMetadata(StreamingResponseMetadata.Instance));
        Summary(s =>
        {
            s.Summary = "Query a data set";
            s.Description =
                "Queries one data set directly. The caller must hold datasets:read; the data store's " +
                "row-level security narrows the rows to the caller's session. Filter tree, sort and paging " +
                "travel in the body; take defaults to and is capped by the pagination settings, except for a caller " +
                "authenticated with an API key, who is limited only by the ApiKey pagination settings (0 means no limit, and " +
                "every row is then streamed). An unknown data set answers 404.";
        });
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<IDataSetImplementationConfiguration>> ResolveDataSet(
        DataQueryRequest req, CancellationToken ct)
    {
        // The name may be a data set or a view of one; a view reads the rows of the data set it varies (FDW-2391).
        var resolved = await _resolver.Resolve(req.DataSetName, ct).ConfigureAwait(false);
        if (resolved.IsFailure) return resolved.ToNewResult<IDataSetImplementationConfiguration>();

        return resolved.Value is { } named
            ? GenericResult<IDataSetImplementationConfiguration>.Success(named.DataSet)
            : NotFound<IDataSetImplementationConfiguration>(req.DataSetName);
    }

    /// <inheritdoc />
    /// <remarks>
    /// When the name is a view, the stored filter of every view in its chain, parent first, ANDed with the caller's own. A data set
    /// has none. The name was resolved a moment ago for this same request, so this repeats what the first found, from the providers' cache.
    /// </remarks>
    protected override async Task<IGenericResult<QueryFilterNode?>> ScopeFilter(DataQueryRequest req, CancellationToken ct)
    {
        var resolved = await _resolver.Resolve(req.DataSetName, ct).ConfigureAwait(false);
        if (resolved.IsFailure) return resolved.ToNewResult<QueryFilterNode?>();

        return resolved.Value is { } named
            ? StoredViewFilters.Of(named.ViewFilters, named.Name, Logger)
            : NotFound<QueryFilterNode?>(req.DataSetName);
    }

    private IGenericResult<T> NotFound<T>(string dataSetName)
        => GenericResult<T>.Failure(DataServiceResultCodes.ByName("DataSetNotFound"), Logger, ResultDetails.Create("DataSetName", dataSetName));
}

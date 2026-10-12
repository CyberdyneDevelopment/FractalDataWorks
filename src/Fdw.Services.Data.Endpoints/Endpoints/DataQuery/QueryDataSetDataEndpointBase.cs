using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;
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
/// The data set as it is, unspecialized: the <c>datasets:read</c> permission lets a caller ask, and
/// the data store's row-level security under the caller's session decides which rows they get. A
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
    private readonly IDataSetConfigurationProvider _dataSets;

    /// <summary>Initializes a new instance of the <see cref="QueryDataSetDataEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSets">Resolves the data set and its fields.</param>
    /// <param name="dataGateways">Supplies the gateway the query is dispatched through.</param>
    protected QueryDataSetDataEndpointBase(
        ILogger<QueryDataSetDataEndpointBase> logger,
        IDataSetConfigurationProvider dataSets,
        IDataGatewayProvider dataGateways)
        : base(logger ?? NullLogger<QueryDataSetDataEndpointBase>.Instance, dataGateways)
    {
        _dataSets = dataSets ?? throw new ArgumentNullException(nameof(dataSets));
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
        var dataSet = await _dataSets.Get(req.DataSetName, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet;

        return dataSet.Value is null
            ? GenericResult<IDataSetImplementationConfiguration>.Failure(
                DataServiceResultCodes.ByName("DataSetNotFound"), Logger,
                ResultDetails.Create("DataSetName", req.DataSetName))
            : dataSet;
    }
}

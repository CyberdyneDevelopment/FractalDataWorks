using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Clients.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Etl;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Owns all ConfigurationDb gateway access for the dataflow graph endpoint.
/// Reads DataSet, DataStore, DataSetSource, and Pipeline header rows for graph construction.
/// The endpoint injects this provider — never IConfigurationGateway directly.
/// </summary>
public class DataflowGraphConfigurationProvider
{


    private static string DataStoreName => EtlPipelineTypes.ConfigurationConnection;
    private const string DataPath = "data";
    private const string PipePath = "pipe";
    private const int AllVariations = int.MaxValue - 1;

    private readonly IConfigurationGatewayProvider _gatewayProvider;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IDataSetVariationConfigurationProvider _variations;
    private readonly ICategoryConfigurationProvider _categories;
    private readonly ILogger<DataflowGraphConfigurationProvider> _logger;

    private Task<IGenericResult<IConfigurationGateway>> Gateway()
        => Task.FromResult(_gatewayProvider.Get(DataStoreName));

    /// <summary>
    /// Initializes a new instance of <see cref="DataflowGraphConfigurationProvider"/>.
    /// </summary>
    public DataflowGraphConfigurationProvider(
        IConfigurationGatewayProvider gatewayProvider,
        IDataSetConfigurationProvider dataSets,
        IDataSetVariationConfigurationProvider variations,
        ICategoryConfigurationProvider categories,
        ILogger<DataflowGraphConfigurationProvider>? logger = null)
    {
        _gatewayProvider = gatewayProvider ?? throw new ArgumentNullException(nameof(gatewayProvider));
        _dataSets = dataSets ?? throw new ArgumentNullException(nameof(dataSets));
        _variations = variations ?? throw new ArgumentNullException(nameof(variations));
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        _logger = logger ?? NullLogger<DataflowGraphConfigurationProvider>.Instance;
    }

    /// <summary>
    /// Returns all current DataSet summary rows for graph node construction.
    /// </summary>
    public virtual async Task<IGenericResult<IReadOnlyList<DataSetRecord>>> LoadDataSets(
        CancellationToken cancellationToken = default)
    {
        var command = new QueryCommand<DataSetRecord>();

        var gatewayResult = await Gateway().ConfigureAwait(false);
        if (gatewayResult.IsFailure || gatewayResult.Value is not { } gateway)
            return GenericResult<IReadOnlyList<DataSetRecord>>.Failure(
                DataflowGraphConfigurationProviderLog.GatewayUnavailable(_logger, DataStoreName));

        var result = await gateway.Execute<IEnumerable<DataSetRecord>>(
                command, new DataStoreTarget(DataStoreName, DataPath, "DataSet"), cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            return result.ToNewResult<IReadOnlyList<DataSetRecord>>();

        return GenericResult<IReadOnlyList<DataSetRecord>>.Success(result.Value?.ToList() ?? []);
    }

    /// <summary>
    /// Returns all current DataStore summary rows for graph node construction.
    /// </summary>
    public virtual async Task<IGenericResult<IReadOnlyList<DataStoreRecord>>> LoadDataStores(
        CancellationToken cancellationToken = default)
    {
        var command = new QueryCommand<DataStoreRecord>();

        var gatewayResult = await Gateway().ConfigureAwait(false);
        if (gatewayResult.IsFailure || gatewayResult.Value is not { } gateway)
            return GenericResult<IReadOnlyList<DataStoreRecord>>.Failure(
                DataflowGraphConfigurationProviderLog.GatewayUnavailable(_logger, DataStoreName));

        var result = await gateway.Execute<IEnumerable<DataStoreRecord>>(
                command, new DataStoreTarget(DataStoreName, DataPath, "DataStore"), cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            return result.ToNewResult<IReadOnlyList<DataStoreRecord>>();

        return GenericResult<IReadOnlyList<DataStoreRecord>>.Success(result.Value?.ToList() ?? []);
    }

    /// <summary>
    /// Returns every configured data set with its sources, for graph edge construction.
    /// </summary>
    /// <remarks>
    /// A source row does not name its data set: it hangs from the data set by RowId. The data set
    /// domain composes each data set with its sources, so the edge comes from that aggregate.
    /// </remarks>
    public virtual Task<IGenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>> LoadDataSetSources(
        CancellationToken cancellationToken = default)
        => _dataSets.Get(cancellationToken);

    /// <summary>
    /// Returns the DataSet category tree, read once, so each data set node can name the path of the category it sits under.
    /// </summary>
    public virtual Task<IGenericResult<CategoryTree>> LoadDataSetCategories(CancellationToken cancellationToken = default)
        => CategoryTrees.Load(_categories, DataSetCategoryConfiguration.ImplementationName, _logger, cancellationToken);

    /// <summary>
    /// Returns every data set variation, read as domain rows in one query. A variation is not a data
    /// set: it is its own domain and holds no sources, so the rows alone carry what the graph draws.
    /// </summary>
    public virtual async Task<IGenericResult<IReadOnlyList<DataSetVariationDomainConfiguration>>> LoadVariations(
        CancellationToken cancellationToken = default)
    {
        var page = await _variations.Get(new DomainQuery(null, null, 0, AllVariations, false), cancellationToken)
            .ConfigureAwait(false);
        if (page.IsFailure)
            return page.ToNewResult<IReadOnlyList<DataSetVariationDomainConfiguration>>();

        var rows = new List<DataSetVariationDomainConfiguration>();
        await foreach (var row in page.Value!.Items.WithCancellation(cancellationToken).ConfigureAwait(false))
            rows.Add(row);

        return page.Value.Outcome.IsFailure
            ? page.Value.Outcome.ToNewResult<IReadOnlyList<DataSetVariationDomainConfiguration>>()
            : GenericResult<IReadOnlyList<DataSetVariationDomainConfiguration>>.Success(rows);
    }

    /// <summary>
    /// Returns whether a pipeline with the given name exists (case-insensitive).
    /// Used to validate the optional ?pipelineName filter before graph construction.
    /// </summary>
    public virtual async Task<IGenericResult<bool>> PipelineExists(
        string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new QueryCommand<System.Collections.Generic.Dictionary<string, object?>>();

            var gatewayResult = await Gateway().ConfigureAwait(false);
            if (gatewayResult.IsFailure || gatewayResult.Value is not { } gateway)
                return GenericResult<bool>.Failure(
                    DataflowGraphConfigurationProviderLog.GatewayUnavailable(_logger, DataStoreName));

            var result = await gateway
                .Execute<IEnumerable<System.Collections.Generic.Dictionary<string, object?>>>(
                    command, new DataStoreTarget(DataStoreName, PipePath, "Pipeline"), cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
                return result.ToNewResult<bool>();

            var exists = (result.Value ?? []).Any(p =>
                p.TryGetValue("Name", out var n)
                && n is string s
                && string.Equals(s, name, System.StringComparison.OrdinalIgnoreCase));

            return GenericResult<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                DataflowGraphConfigurationProviderLog.PipelineFilterCheckFailed(_logger, ex));
        }
    }
}

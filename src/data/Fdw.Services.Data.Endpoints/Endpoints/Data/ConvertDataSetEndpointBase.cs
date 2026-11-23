using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Data.Transformations;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>PATCH /datasets/{name}/convert</c> (FDW-6972, FDW-6973): turns a Simple, Compound or Federated data set into a Graph data set with the same Id, name, fields and content, so it
/// edits in the Graph designer. Permissioned like an update of the data set.
/// </summary>
/// <remarks>
/// The endpoint reads the data set, asks <see cref="SimpleToGraph"/> for the Graph configuration (a part with no Graph place refuses the conversion
/// by name), judges it with the Graph data set's own soundness check, and writes it through the domain provider's update, which moves the data set
/// between implementations in one unit (FDW-6806): a failure leaves the Simple data set as it was.
/// </remarks>
public abstract class ConvertDataSetEndpointBase : CrudUpdateEndpointBase<ConvertDataSetRequest, DataSetDetailResponse>
{
    private readonly IDataSetConfigurationProvider _dataSetProvider;
    private readonly ICategoryConfigurationProvider _categories;
    private readonly GraphDataSetSources _sources;
    private readonly FieldMappingTransformationResolver _chains;

    /// <summary>Initializes a new instance of the <see cref="ConvertDataSetEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads and moves the data set.</param>
    /// <param name="categories">Reads the categories a data set sits under, for the answer.</param>
    /// <param name="dataStores">Reads the data stores the converted graph reads, for its soundness check.</param>
    /// <param name="chains">Reads the transformation chains attached to the data set's mappings, which a converted graph cannot carry.</param>
    protected ConvertDataSetEndpointBase(
        ILogger<ConvertDataSetEndpointBase> logger,
        IDataSetConfigurationProvider dataSetProvider,
        ICategoryConfigurationProvider categories,
        IDataStoreConfigurationProvider dataStores,
        FieldMappingTransformationResolver chains) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
        _categories = categories;
        _sources = new GraphDataSetSources(dataSetProvider, dataStores);
        _chains = chains;
    }

    /// <inheritdoc />
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string Route => "/datasets/{Name}/convert";

    /// <inheritdoc />
    protected override string EndpointSummary => "Convert a data set to a Graph data set";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Turns a Simple, Compound or Federated data set into a Graph data set with the same name, fields and content; a part with no Graph place refuses the conversion by name.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(ConvertDataSetRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataSetDetailResponse?>> FindForUpdate(ConvertDataSetRequest request, CancellationToken ct)
    {
        var found = await _dataSetProvider.Get(request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure) return found.ToNewResult<DataSetDetailResponse?>();
        if (found.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, request.Name);
            return GenericResult<DataSetDetailResponse?>.Success((DataSetDetailResponse?)null);
        }

        return GenericResult<DataSetDetailResponse?>.Success(DataSetQueryHelper.MapToDetail(found.Value));
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataSetDetailResponse>> Update(
        ConvertDataSetRequest request, DataSetDetailResponse existing, CancellationToken ct)
    {
        if (request.BatchSize is not { } batchSize)
            return GenericResult<DataSetDetailResponse>.Failure(
                DataSetsResultCodes.ValueRequired, Logger,
                ResultDetails.Create("name", request.Name, "part", "conversion", "partName", request.Name).With("field", "batchSize"));

        var loaded = await _dataSetProvider.Get(request.Name, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (loaded.IsFailure) return loaded.ToNewResult<DataSetDetailResponse>();
        if (loaded.Value is null) return GenericResult<DataSetDetailResponse>.Failure(DataSetEndpointLog.DataSetNotFound(Logger, request.Name));

        var chains = await Chains(loaded.Value, ct).ConfigureAwait(false);
        if (chains.IsFailure) return chains.ToNewResult<DataSetDetailResponse>();

        // A Compound or Federated data set converts its joins and union too (FDW-6973); a Simple one has one source and no input to carry a chain.
        var converted = loaded.Value is IJoinedDataSetConfiguration joined
            ? JoinedToGraph.Convert(joined, batchSize, chains.Value!, Logger)
            : SimpleToGraph.Convert(loaded.Value, batchSize, [.. chains.Value!.Values.SelectMany(bySource => bySource.Keys).Distinct(StringComparer.Ordinal)], Logger);
        if (converted.IsFailure) return converted.ToNewResult<DataSetDetailResponse>();

        var graph = converted.Value!;
        var sound = await GraphDataSetSoundness.Judge(graph, _sources, Logger, ct).ConfigureAwait(false);
        if (sound.IsFailure) return sound.ToNewResult<DataSetDetailResponse>();

        var saved = await _dataSetProvider.Update(graph, "DataSet", graph.Implementation, graph.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            DataSetEndpointLog.DataSetUpdateFailed(Logger, request.Name, "Conversion failed");
            return saved.ToNewResult<DataSetDetailResponse>();
        }

        DataSetEndpointLog.DataSetUpdated(Logger, request.Name);
        var tree = await DataSetCategoryLinks.Tree(_categories, Logger, ct).ConfigureAwait(false);
        return tree.IsFailure || tree.Value is null
            ? tree.ToNewResult<DataSetDetailResponse>()
            : DataSetCategoryLinks.Describe(DataSetQueryHelper.MapToDetail(graph), tree.Value, graph.CategoryId, Logger);
    }

    // Each source's stored chain links, by source name and then by logical field.
    private async Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<AttachedFieldTransformation>>>>> Chains(
        IDataSetImplementationConfiguration dataSet, CancellationToken ct)
    {
        var bySource = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<AttachedFieldTransformation>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in dataSet.Sources)
        {
            var attached = await _chains.Attached(source, ct).ConfigureAwait(false);
            if (attached.IsFailure) return attached.ToNewResult<IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<AttachedFieldTransformation>>>>();
            bySource[source.SourceName] = attached.Value!;
        }

        return GenericResult<IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<AttachedFieldTransformation>>>>.Success(bySource);
    }
}

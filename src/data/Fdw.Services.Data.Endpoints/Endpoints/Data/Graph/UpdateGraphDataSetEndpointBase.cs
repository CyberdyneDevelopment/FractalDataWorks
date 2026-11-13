using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Data.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>PATCH /datasets/graph/{name}</c> (FDW-5287): changes a data set of kind Graph. A member the request leaves out is kept; a list it sends
/// replaces the stored one whole. A new route beside the update route of every other kind, which is untouched.
/// </summary>
/// <remarks>
/// Binding an abstract data set is an update that sends its components and links and an empty list of intended sources: the intention is replaced in
/// place, and the data set is not recreated. A data set of another kind is refused, as the update of every kind refuses to change a data set's kind.
/// </remarks>
public abstract class UpdateGraphDataSetEndpointBase : CrudUpdateEndpointBase<UpdateGraphDataSetRequest, DataSetDetailResponse>
{
    private readonly IDataSetConfigurationProvider _dataSetProvider;
    private readonly ICategoryConfigurationProvider _categories;
    private readonly GraphDataSetSources _sources;

    /// <summary>Initializes a new instance of the <see cref="UpdateGraphDataSetEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads and saves data sets.</param>
    /// <param name="categories">Reads the categories a data set sits under.</param>
    /// <param name="dataStores">Reads the data stores the data set reads or intends to read, for the check made before it is saved.</param>
    protected UpdateGraphDataSetEndpointBase(
        ILogger<UpdateGraphDataSetEndpointBase> logger, IDataSetConfigurationProvider dataSetProvider, ICategoryConfigurationProvider categories,
        IDataStoreConfigurationProvider dataStores) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
        _sources = new GraphDataSetSources(dataSetProvider, dataStores);
        _categories = categories;
    }

    /// <inheritdoc />
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string Route => "/datasets/graph/{Name}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Update a Graph data set";

    /// <inheritdoc />
    protected override string EndpointDescription => "Changes a data set of kind Graph; a member left out is kept, a list sent replaces the stored one.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateGraphDataSetRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataSetDetailResponse?>> FindForUpdate(UpdateGraphDataSetRequest request, CancellationToken ct)
    {
        DataSetEndpointLog.LoadingDataSet(Logger, request.Name, string.Empty);

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
        UpdateGraphDataSetRequest request, DataSetDetailResponse existing, CancellationToken ct)
    {
        DataSetEndpointLog.UpdatingDataSet(Logger, request.Name);

        var loaded = await _dataSetProvider.Get(request.Name, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (loaded.IsFailure) return loaded.ToNewResult<DataSetDetailResponse>();
        if (loaded.Value is null) return GenericResult<DataSetDetailResponse>.Failure(DataSetEndpointLog.DataSetNotFound(Logger, request.Name));

        if (loaded.Value is not GraphDataSetConfiguration graph)
        {
            return GenericResult<DataSetDetailResponse>.Failure(
                DataSetsResultCodes.ImplementationChangeNotAllowed, Logger,
                ResultDetails.Create("name", request.Name, "implementation", loaded.Value.Implementation, "requested", GraphDataSetConfiguration.ImplementationName));
        }

        var checkedFields = CheckFields(request);
        if (checkedFields.IsFailure) return checkedFields.ToNewResult<DataSetDetailResponse>();

        var category = await RequestedCategory(request, graph, ct).ConfigureAwait(false);
        if (category.IsFailure) return category.ToNewResult<DataSetDetailResponse>();

        var applied = await Apply(request, graph, category.Value, GraphDataSetStamp.Saving(User), ct).ConfigureAwait(false);
        if (applied.IsFailure) return applied.ToNewResult<DataSetDetailResponse>();

        // Why the same judgement as the validate route, over the data set as it would be stored: a change validate calls unsound is not saved (FDW-5442).
        var sound = await GraphDataSetSoundness.Judge(graph, _sources, Logger, ct).ConfigureAwait(false);
        if (sound.IsFailure) return sound.ToNewResult<DataSetDetailResponse>();

        var saved = await _dataSetProvider.Update(graph, "DataSet", graph.Implementation, graph.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            DataSetEndpointLog.DataSetUpdateFailed(Logger, request.Name, "Save failed");
            return saved.ToNewResult<DataSetDetailResponse>();
        }

        DataSetEndpointLog.DataSetUpdated(Logger, request.Name);
        var tree = await DataSetCategoryLinks.Tree(_categories, Logger, ct).ConfigureAwait(false);
        return tree.IsFailure || tree.Value is null
            ? tree.ToNewResult<DataSetDetailResponse>()
            : DataSetCategoryLinks.Describe(DataSetQueryHelper.MapToDetail(graph), tree.Value, graph.CategoryId, Logger);
    }

    // The classification of the data set when the PATCH names one, and of each field out when fields are replaced, and each field's types.
    private IGenericResult CheckFields(UpdateGraphDataSetRequest request)
    {
        var fieldClassifications = request.FieldsOut?.Select(f => f.ClassificationId) ?? [];
        return GraphDataSetDefinitions.ValidateFields(
            request.ClassificationId is { } named ? fieldClassifications.Prepend(named) : fieldClassifications, request.FieldsOut, Logger);
    }

    // Sets what the request states: the header, then each list that is sent. A graph is sent whole (components with links), and is checked as sent.
    private async Task<IGenericResult> Apply(UpdateGraphDataSetRequest request, GraphDataSetConfiguration graph, Guid? categoryId, GraphDataSetStamp stamp, CancellationToken ct)
    {
        var values = new DataSetRequestValues(request.Name, Logger);
        var header = ApplyHeader(request, graph, categoryId, values);
        if (header.IsFailure) return header;

        var parts = ApplyGraph(request, graph, values, stamp);
        if (parts.IsFailure) return parts;

        if (request.IntendedSources is not null)
        {
            return await GraphDataSetDefinitions.Intents(graph, request.IntendedSources, _dataSetProvider, stamp, Logger, ct).ConfigureAwait(false);
        }

        // A graph that has components and still holds intended sources is refused: binding sends an empty list, which replaces the intention.
        return graph.Components.Count > 0 && graph.Intents.Count > 0
            ? GenericResult.Failure(DataServiceResultCodes.ByName("GraphDataSetIntentOnBoundGraph"), Logger, ResultDetails.Create("dataSet", graph.Name))
            : GenericResult.Success();
    }

    // Why a stated version goes through the kind's declared default as create's does (FDW-5509): a blank one takes the declared version or is refused, and is never stored blank; a version left out keeps the stored one.
    internal static IGenericResult ApplyHeader(UpdateGraphDataSetRequest request, GraphDataSetConfiguration graph, Guid? categoryId, DataSetRequestValues values)
    {
        graph.CategoryId = categoryId;
        graph.Description = request.Description ?? graph.Description;
        graph.ClassificationId = request.ClassificationId ?? graph.ClassificationId;
        if (request.Version is not null)
        {
            var version = values.DeclaredText(request.Version, GraphDataSetConfiguration.ImplementationName, DataSetDeclaredDefaults.Version, "version");
            if (version.IsFailure) return version;
            graph.Version = version.Value!;
        }

        graph.RecordTypeName = request.RecordTypeName ?? graph.RecordTypeName;
        if (request.Caching is null) return GenericResult.Success();

        var caching = DataSetQueryHelper.MapCaching(request.Caching, graph.Implementation, values);
        if (caching.IsFailure) return caching;
        graph.Caching = caching.Value;
        return GenericResult.Success();
    }

    // The fields out, the graph and where the fields out come from; the last is judged over the merged graph, with what is stored.
    private IGenericResult ApplyGraph(UpdateGraphDataSetRequest request, GraphDataSetConfiguration graph, DataSetRequestValues values, GraphDataSetStamp stamp)
    {
        if (request.FieldsOut is not null && GraphDataSetDefinitions.Fields(graph, request.FieldsOut, values, stamp, Logger) is { IsFailure: true } fields) return fields;

        if ((request.Components is null) != (request.Links is null))
        {
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("GraphDataSetBodyIncomplete"), Logger,
                ResultDetails.Create("dataSet", request.Name, "sent", request.Components is null ? "links" : "components"));
        }

        if (request.Components is not null && request.Links is not null
            && GraphDataSetDefinitions.Body(graph, request.Components, request.Links, Logger) is { IsFailure: true } body) return body;

        return GraphDataSetDefinitions.FieldsOut(graph, request.FieldsOutComponent, request.FieldsOutOutput, Logger);
    }

    // A category left out leaves the stored one; an empty name takes the data set out of its category; a name must be a category of the DataSet type.
    private async Task<IGenericResult<Guid?>> RequestedCategory(UpdateGraphDataSetRequest request, GraphDataSetConfiguration graph, CancellationToken ct)
    {
        if (request.Category is null) return GenericResult<Guid?>.Success(graph.CategoryId);

        var tree = await DataSetCategoryLinks.Tree(_categories, Logger, ct).ConfigureAwait(false);
        return tree.IsFailure || tree.Value is null
            ? tree.ToNewResult<Guid?>()
            : DataSetCategoryLinks.Resolve(tree.Value, request.Category, Logger);
    }
}

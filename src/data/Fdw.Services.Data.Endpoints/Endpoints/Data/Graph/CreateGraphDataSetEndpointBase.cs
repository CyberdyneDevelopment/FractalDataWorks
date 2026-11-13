using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /datasets/graph</c> (FDW-5287): creates a data set of kind Graph from its fields out and its graph. A new route beside the create
/// route of every other kind, which is untouched.
/// </summary>
/// <remarks>
/// A graph with no components is abstract: it saves with its fields out and its intended sources, and reading it is refused until it is bound.
/// A destination, an action or a loop in the graph is refused, each by its own code, before anything is written.
/// </remarks>
public abstract class CreateGraphDataSetEndpointBase : CrudCreateEndpointBase<CreateGraphDataSetRequest, DataSetDetailResponse>
{
    private readonly IDataSetConfigurationProvider _dataSetProvider;
    private readonly ICategoryConfigurationProvider _categories;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly GraphDataSetSources _sources;

    /// <summary>Initializes a new instance of the <see cref="CreateGraphDataSetEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads and saves data sets.</param>
    /// <param name="categories">Reads the categories a data set sits under.</param>
    /// <param name="dataStores">Reads the data stores the data set reads or intends to read, for the check made before it is saved.</param>
    /// <param name="authContext">Names the caller, who owns the data set they create.</param>
    protected CreateGraphDataSetEndpointBase(
        ILogger<CreateGraphDataSetEndpointBase> logger, IDataSetConfigurationProvider dataSetProvider, ICategoryConfigurationProvider categories,
        IDataStoreConfigurationProvider dataStores, IAuthenticationContextAccessor authContext) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
        _sources = new GraphDataSetSources(dataSetProvider, dataStores);
        _categories = categories;
        _authContext = authContext;
    }

    /// <inheritdoc />
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string Route => "/datasets/graph";

    /// <inheritdoc />
    protected override string EndpointSummary => "Create a Graph data set";

    /// <inheritdoc />
    protected override string EndpointDescription => "Creates a data set of kind Graph: its fields out, and a graph of row components and links read by running it.";

    /// <inheritdoc />
    protected override string GetResourceName(CreateGraphDataSetRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(CreateGraphDataSetRequest request, CancellationToken ct)
    {
        var result = await _dataSetProvider.Get(request.Name, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<bool>();

        var exists = result.Value is not null;
        if (exists) DataSetEndpointLog.DataSetAlreadyExists(Logger, request.Name);
        return GenericResult<bool>.Success(exists);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataSetDetailResponse>> Create(CreateGraphDataSetRequest request, CancellationToken ct)
    {
        DataSetEndpointLog.CreatingDataSet(Logger, request.Name);

        var checkedFields = GraphDataSetDefinitions.ValidateFields(request.FieldsOut.Select(f => f.ClassificationId).Prepend(request.ClassificationId), request.FieldsOut, Logger);
        if (checkedFields.IsFailure) return checkedFields.ToNewResult<DataSetDetailResponse>();

        var category = await DataSetCategoryLinks.Requested(_categories, request.Category, Logger, ct).ConfigureAwait(false);
        if (category.IsFailure) return category.ToNewResult<DataSetDetailResponse>();

        // The caller owns the data set they create; a caller that cannot be named is refused, never stored as no owner (FDW-4346).
        if (_authContext.Current is not { } caller || !Guid.TryParse(caller.UserId, out var ownerUserId))
        {
            return GenericResult<DataSetDetailResponse>.Failure(
                DataSetsResultCodes.ByName("OwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "the caller cannot be named"));
        }

        var built = await Build(request, category.Value.Id, ownerUserId, ct).ConfigureAwait(false);
        if (built.IsFailure) return built.ToNewResult<DataSetDetailResponse>();

        var config = built.Value!;

        // Why the same judgement as the validate route: a body validate calls unsound is not saved (FDW-5442).
        var sound = await GraphDataSetSoundness.Judge(config, _sources, Logger, ct).ConfigureAwait(false);
        if (sound.IsFailure) return sound.ToNewResult<DataSetDetailResponse>();
        var saved = await _dataSetProvider.Save(config, "DataSet", config.Implementation, config.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            DataSetEndpointLog.DataSetCreateFailed(Logger, request.Name, "Save failed");
            return saved.ToNewResult<DataSetDetailResponse>();
        }

        DataSetEndpointLog.DataSetCreated(Logger, config.Name);
        return DataSetCategoryLinks.Describe(DataSetQueryHelper.MapToDetail(config), category.Value.Tree, config.CategoryId, Logger);
    }

    private async Task<IGenericResult<GraphDataSetConfiguration>> Build(CreateGraphDataSetRequest request, Guid? categoryId, Guid ownerUserId, CancellationToken ct)
    {
        var values = new DataSetRequestValues(request.Name, Logger);

        // A version the request leaves out is the kind's declared one (FDW-3956).
        var version = values.DeclaredText(request.Version, GraphDataSetConfiguration.ImplementationName, DataSetDeclaredDefaults.Version, "version");
        if (version.IsFailure) return version.ToNewResult<GraphDataSetConfiguration>();

        var caching = DataSetQueryHelper.MapCaching(request.Caching, GraphDataSetConfiguration.ImplementationName, values);
        if (caching.IsFailure) return caching.ToNewResult<GraphDataSetConfiguration>();

        var graph = new GraphDataSetConfiguration
        {
            Name = request.Name,
            Implementation = GraphDataSetConfiguration.ImplementationName,
            Description = request.Description ?? string.Empty,
            ClassificationId = request.ClassificationId,
            CategoryId = categoryId,
            OwnerUserId = ownerUserId,
            Version = version.Value!,
            RecordTypeName = request.RecordTypeName,
            Caching = caching.Value,
        };

        var stated = GraphDataSetDefinitions.Apply(graph, request, values, GraphDataSetStamp.Saving(ownerUserId), Logger);
        if (stated.IsFailure) return stated.ToNewResult<GraphDataSetConfiguration>();

        var intents = await GraphDataSetDefinitions.Intents(graph, request.IntendedSources, _dataSetProvider, GraphDataSetStamp.Saving(ownerUserId), Logger, ct).ConfigureAwait(false);
        return intents.IsFailure ? intents.ToNewResult<GraphDataSetConfiguration>() : GenericResult<GraphDataSetConfiguration>.Success(graph);
    }

    /// <inheritdoc />
    protected override Task SendCreatedResponse(DataSetDetailResponse detail, CancellationToken ct) => Send.ResponseAsync(detail, 201, ct);
}

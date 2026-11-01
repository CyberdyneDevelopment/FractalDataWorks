using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for setting the data classification of one or more paths, containers or fields of a
/// data store (FDW-938, FDW-960).
/// </summary>
/// <remarks>
/// A store's paths can be written only by replacing all of them, and its containers and fields not at
/// all, so a workbench had no way to classify them. This names the items by durable id, sets every one
/// to the level and saves the store once, so a workbench classifying a selection writes one version of
/// the store, not one per item. One item is a selection of one.
/// <para>
/// All or nothing: an id the store does not have refuses the request and nothing is changed.
/// </para>
/// </remarks>
public abstract class ClassifyDataStoreItemsEndpointBase
    : CrudUpdateEndpointBase<ClassifyDataStoreItemsRequest, List<ClassifiedItemResponse>>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <summary>Initializes a new instance of the <see cref="ClassifyDataStoreItemsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">Reads and saves the data store.</param>
    protected ClassifyDataStoreItemsEndpointBase(
        ILogger<ClassifyDataStoreItemsEndpointBase> logger, DataStoreConfigurationProvider dataStoreProvider) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider ?? throw new ArgumentNullException(nameof(dataStoreProvider));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/classifications";

    /// <summary>Returns the store as the resource identifier.</summary>
    protected override string GetResourceIdentifier(ClassifyDataStoreItemsRequest request) => request.Name;

    /// <summary>Finds the named items; a store that does not exist answers 404, an item it lacks refuses the request.</summary>
    protected override async Task<IGenericResult<List<ClassifiedItemResponse>?>> FindForUpdate(
        ClassifyDataStoreItemsRequest request, CancellationToken ct)
    {
        var store = await _dataStoreProvider.Get(request.Name, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (store.IsFailure) return store.ToNewResult<List<ClassifiedItemResponse>?>();
        if (store.Value is null) return GenericResult<List<ClassifiedItemResponse>?>.Success(null);

        var items = Find(store.Value, request);
        return items.IsFailure
            ? items.ToNewResult<List<ClassifiedItemResponse>?>()
            : GenericResult<List<ClassifiedItemResponse>?>.Success(items.Value!.Select(Describe).ToList());
    }

    /// <summary>Refuses a classification that names no level.</summary>
    protected override Task<IGenericResult> ValidateUpdate(
        ClassifyDataStoreItemsRequest request, List<ClassifiedItemResponse> existing, CancellationToken ct)
        => Task.FromResult(DataClassificationValidator.Validate(request.ClassificationId, Logger));

    /// <summary>Sets every named item's classification and saves the store once.</summary>
    /// <remarks>
    /// The items are changed on the composed store before the save, and the composed rows may be the
    /// gateway's cached ones. A save that fails puts every old value back, so the cache is left holding
    /// what is stored rather than what was refused.
    /// </remarks>
    protected override async Task<IGenericResult<List<ClassifiedItemResponse>>> Update(
        ClassifyDataStoreItemsRequest request, List<ClassifiedItemResponse> existing, CancellationToken ct)
    {
        var store = await _dataStoreProvider.Get(request.Name, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (store.IsFailure) return store.ToNewResult<List<ClassifiedItemResponse>>();
        if (store.Value is not { } current)
            return GenericResult<List<ClassifiedItemResponse>>.Failure(
                DataStoreEndpointLog.DataStoreNotFound(Logger, request.Name));

        var items = Find(current, request);
        if (items.IsFailure) return items.ToNewResult<List<ClassifiedItemResponse>>();

        var previous = items.Value!.Select(item => (item, item.ClassificationId())).ToList();
        foreach (var item in items.Value!)
            item.SetClassificationId(request.ClassificationId);

        var saved = await _dataStoreProvider.Update(current, "DataStore", current.Implementation, current.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            foreach (var (item, classificationId) in previous)
                item.SetClassificationId(classificationId);
            return saved.ToNewResult<List<ClassifiedItemResponse>>();
        }

        return GenericResult<List<ClassifiedItemResponse>>.Success(items.Value!.Select(Describe).ToList());
    }

    // Every item the request names, or the failure naming the ones the store does not have. Ids are
    // unique across the store's tree; a repeated id is one item.
    private IGenericResult<IReadOnlyList<StoreItem>> Find(IDataStoreImplementationConfiguration store, ClassifyDataStoreItemsRequest request)
    {
        var wanted = request.ItemIds.Distinct().ToList();
        if (wanted.Count == 0)
            return GenericResult<IReadOnlyList<StoreItem>>.Failure(
                DataStoreEndpointLog.BulkTargetsMissing(Logger, "classify", "path, container or field", store.Name));

        var byId = Items(store).ToDictionary(item => item.Id);
        var missing = wanted.Where(id => !byId.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return GenericResult<IReadOnlyList<StoreItem>>.Failure(
                DataStoreEndpointLog.BulkTargetsNotFound(Logger, "classify", store.Name, "path, container or field", string.Join(", ", missing)));

        return GenericResult<IReadOnlyList<StoreItem>>.Success(wanted.Select(id => byId[id]).ToList());
    }

    private static IEnumerable<StoreItem> Items(IDataStoreImplementationConfiguration store)
    {
        foreach (var path in store.Paths)
        {
            yield return new StoreItem("Path", path.Id, path.Name, () => path.ClassificationId, v => path.ClassificationId = v);
            foreach (var container in path.Containers)
            {
                yield return new StoreItem("Container", container.Id, container.Name, () => container.ClassificationId, v => container.ClassificationId = v);
                foreach (var field in container.Fields)
                    yield return new StoreItem("Field", field.Id, field.Name, () => field.ClassificationId, v => field.ClassificationId = v);
            }
        }
    }

    private static ClassifiedItemResponse Describe(StoreItem item) => new()
    {
        Id = item.Id,
        Kind = item.Kind,
        Name = item.Name,
        ClassificationId = item.ClassificationId()
    };

    private sealed record StoreItem(string Kind, Guid Id, string Name, Func<int> ClassificationId, Action<int> SetClassificationId);
}

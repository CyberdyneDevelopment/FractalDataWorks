using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Etl;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// One organization's pipelines (the ones that carry its id) and what they reach: the data sets they name, and the data
/// stores those data sets are stored in.
/// </summary>
/// <remarks>
/// The pipelines are read before the data sets because they decide which data sets are in scope, and the data sets
/// before the data stores for the same reason. The data sets and data stores are read by name, one at a time, not by
/// scanning every one and keeping a few.
/// </remarks>
internal sealed class OrganizationMapSelection : MapSelection
{
    private readonly Guid _organizationId;
    private readonly ILogger _logger;
    private readonly HashSet<string> _dataSetNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _dataStoreNames = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Initializes a selection of one organization.</summary>
    /// <param name="organizationId">The organization's id, as its pipelines carry it.</param>
    /// <param name="logger">Records what could not be resolved.</param>
    public OrganizationMapSelection(Guid organizationId, ILogger logger)
    {
        _organizationId = organizationId;
        _logger = logger;
    }

    /// <inheritdoc />
    public override MapScopeDto Describe() => new() { Kind = MapScopeKinds.Organization, OrganizationId = _organizationId };

    /// <inheritdoc />
    public override async IAsyncEnumerable<IPipelineImplementationConfiguration> Pipelines(
        MapSources sources, MapState state, [EnumeratorCancellation] CancellationToken ct)
    {
        var all = MapSources.Pages(
            (skip, take, token) => sources.Pipelines.Get(skip, take, ConfigurationLevels.RecordOnly, token), state, ct);

        await foreach (var pipeline in all.WithCancellation(ct).ConfigureAwait(false))
        {
            if (pipeline.OrgId != _organizationId) continue;

            if (pipeline is IEtlPipelineImplementationConfiguration engine)
            {
                NameDataSet(engine.SourceDataSet);
                NameDataSet(engine.DestinationDataSet);
            }

            yield return pipeline;
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<IDataSetImplementationConfiguration> DataSets(
        MapSources sources, MapState state, [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var name in _dataSetNames.ToList())
        {
            var read = await sources.DataSets.Get(name, ConfigurationLevels.Children, ct).ConfigureAwait(false);
            var dataSet = MapSources.Resolve(read, MapKinds.DataSet, name, "a pipeline of the organization", state, _logger);
            if (dataSet is null)
            {
                if (state.Outcome.IsFailure) yield break;
                continue;
            }

            foreach (var source in dataSet.Sources.Where(source => !string.IsNullOrEmpty(source.DataStoreName)))
                _dataStoreNames.Add(source.DataStoreName);

            yield return dataSet;
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<IDataStoreImplementationConfiguration> DataStores(
        MapSources sources, MapState state, [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var name in _dataStoreNames.ToList())
        {
            var read = await sources.DataStores.Get(name, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
            var dataStore = MapSources.Resolve(read, MapKinds.DataStore, name, "a data set of the organization", state, _logger);
            if (dataStore is null)
            {
                if (state.Outcome.IsFailure) yield break;
                continue;
            }

            yield return dataStore;
        }
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>> VariationCounts(
        MapSources sources, CancellationToken ct)
    {
        var all = await DataSetVariationQueries.All(sources.Variations, ct).ConfigureAwait(false);
        return all.IsFailure
            ? all.ToNewResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>()
            : GenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>.Success(MapVariationCounts.Of(all.Value!));
    }

    private void NameDataSet(string? name)
    {
        if (!string.IsNullOrEmpty(name)) _dataSetNames.Add(name);
    }
}

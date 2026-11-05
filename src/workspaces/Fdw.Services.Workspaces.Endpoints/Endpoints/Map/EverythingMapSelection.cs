using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Everything the caller can see: every data store, pipeline and data set, with every variation counted.</summary>
internal sealed class EverythingMapSelection : MapSelection
{
    /// <inheritdoc />
    public override MapScopeDto Describe() => new() { Kind = MapScopeKinds.Everything };

    /// <inheritdoc />
    /// <remarks>
    /// Read whole, a page at a time: the data sets a graph pipeline reads and writes are named in its steps, and a page is read as a set, a statement
    /// for each table, where a read of each pipeline on its own cost several (FDW-3906).
    /// </remarks>
    public override IAsyncEnumerable<IPipelineImplementationConfiguration> Pipelines(MapSources sources, MapState state, CancellationToken ct)
        => MapSources.Pages(
            (skip, take, token) => sources.Pipelines.Get(skip, take, ConfigurationLevels.WholeAggregate, token), state, ct);

    /// <inheritdoc />
    public override IAsyncEnumerable<IDataSetImplementationConfiguration> DataSets(MapSources sources, MapState state, CancellationToken ct)
        => MapSources.Pages(
            (skip, take, token) => sources.DataSets.Get(skip, take, ConfigurationLevels.Children, token), state, ct);

    /// <inheritdoc />
    public override IAsyncEnumerable<IDataStoreImplementationConfiguration> DataStores(MapSources sources, MapState state, CancellationToken ct)
        => MapSources.Pages(
            (skip, take, token) => sources.DataStores.Get(skip, take, ConfigurationLevels.RecordOnly, token), state, ct);

    /// <inheritdoc />
    public override async Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>> VariationCounts(
        MapSources sources, CancellationToken ct)
    {
        var all = await DataSetVariationQueries.All(sources.Variations, ct).ConfigureAwait(false);
        return all.IsFailure
            ? all.ToNewResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>()
            : GenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>.Success(MapVariationCounts.Of(all.Value!));
    }
}

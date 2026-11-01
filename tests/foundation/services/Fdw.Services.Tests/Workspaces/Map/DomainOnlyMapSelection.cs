using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>
/// A scope that can only answer what the domain tables answer: asking it to compose any object throws. A reader that passes
/// against it never loaded the whole snapshot.
/// </summary>
internal sealed class DomainOnlyMapSelection(
    string scopeKind = MapScopeKinds.Everything,
    string? workspace = null,
    Guid? scopeId = null,
    IReadOnlyDictionary<string, IReadOnlyCollection<Guid>>? held = null) : MapSelection
{
    public override Guid? ScopeId => scopeId;

    public override MapScopeDto Describe() => new() { Kind = scopeKind, Workspace = workspace };

    public override IReadOnlyCollection<Guid>? Held(string kind)
        => held is not null && held.TryGetValue(kind, out var ids) ? ids : null;

    public override IAsyncEnumerable<IPipelineImplementationConfiguration> Pipelines(MapSources sources, MapState state, CancellationToken ct)
        => throw new NotSupportedException("a pipeline was composed");

    public override IAsyncEnumerable<IDataSetImplementationConfiguration> DataSets(MapSources sources, MapState state, CancellationToken ct)
        => throw new NotSupportedException("a data set was composed");

    public override IAsyncEnumerable<IDataStoreImplementationConfiguration> DataStores(MapSources sources, MapState state, CancellationToken ct)
        => throw new NotSupportedException("a data store was composed");

    public override Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>> VariationCounts(
        MapSources sources, CancellationToken ct)
        => throw new NotSupportedException("the variations were read");
}

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Reads what the caller can see into a lineage graph, through the same providers and access rules as the map.</summary>
internal sealed class LineageReader
{
    private readonly MapSources _sources;
    private readonly MapScopeSelector _scopes;
    private readonly IFrameworkAuthorizationService _authorization;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="LineageReader"/> class.</summary>
    public LineageReader(
        MapSources sources,
        MapScopeSelector scopes,
        IFrameworkAuthorizationService authorization,
        ILogger logger)
    {
        _sources = sources;
        _scopes = scopes;
        _authorization = authorization;
        _logger = logger;
    }

    /// <summary>Reads everything the caller can see, or one workspace's share of it, into a graph.</summary>
    public async Task<IGenericResult<LineageGraph>> Read(string? workspace, ClaimsPrincipal user, CancellationToken ct)
    {
        var selection = await _scopes.Select(workspace, null, ct).ConfigureAwait(false);
        if (selection.IsFailure) return selection.ToNewResult<LineageGraph>();

        var editing = await MapEditing.Resolve(
            _authorization, new ClaimsPrincipalAuthenticationContext(user), selection.Value!.CanEditScope, ct).ConfigureAwait(false);
        if (editing.IsFailure) return editing.ToNewResult<LineageGraph>();

        return await LineageSnapshot.Load(selection.Value!, _sources, editing.Value!, _logger, ct).ConfigureAwait(false);
    }

    /// <summary>Finds the objects whose names match, from the domain tables of the scope, without reading the scope into a graph.</summary>
    /// <param name="workspace">The workspace to search in, or nothing for everything the caller can see.</param>
    /// <param name="query">What to find and which page.</param>
    /// <param name="ct">The cancellation token.</param>
    public async Task<IGenericResult<LineageSearchPage>> Search(string? workspace, LineageSearchQuery query, CancellationToken ct)
    {
        var selection = await _scopes.Select(workspace, null, ct).ConfigureAwait(false);
        return selection.IsFailure
            ? selection.ToNewResult<LineageSearchPage>()
            : await LineageSearchReader.Search(selection.Value!, _sources, query, _logger, ct).ConfigureAwait(false);
    }

    /// <summary>Reads how many stages each named pipeline has; null for one whose configuration has no stage list.</summary>
    public async Task<IGenericResult<Dictionary<string, int?>>> StageCounts(List<string> pipelineNodeIds, CancellationToken ct)
    {
        var counts = new Dictionary<string, int?>(StringComparer.Ordinal);
        foreach (var nodeId in pipelineNodeIds)
        {
            if (!MapNodeIds.TryParse(nodeId, out _, out var id)) continue;

            var read = await _sources.Pipelines.Get(id, ConfigurationLevels.Children, ct).ConfigureAwait(false);
            if (read.IsFailure) return read.ToNewResult<Dictionary<string, int?>>();

            counts[nodeId] = read.Value is IEtlPipelineImplementationConfiguration engine ? engine.Transforms?.Count : null;
        }

        return GenericResult<Dictionary<string, int?>>.Success(counts);
    }
}

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
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
    /// <param name="workspace">The workspace to read, or nothing for everything the caller can see.</param>
    /// <param name="user">The caller.</param>
    /// <param name="readCategories">Whether each data set names the category it sits under; only the groups ask for it.</param>
    /// <param name="ct">The cancellation token.</param>
    public async Task<IGenericResult<LineageGraph>> Read(string? workspace, ClaimsPrincipal user, bool readCategories, CancellationToken ct)
    {
        var selection = await _scopes.Select(workspace, null, ct).ConfigureAwait(false);
        if (selection.IsFailure) return selection.ToNewResult<LineageGraph>();

        var editing = await MapEditing.Resolve(
            _authorization, new ClaimsPrincipalAuthenticationContext(user), selection.Value!.CanEditScope, ct).ConfigureAwait(false);
        if (editing.IsFailure) return editing.ToNewResult<LineageGraph>();

        return await LineageSnapshot.Load(selection.Value!, _sources, editing.Value!, readCategories, _logger, ct).ConfigureAwait(false);
    }

    /// <summary>Finds the path of the data-set category a request names, or nothing when it names none.</summary>
    /// <param name="name">The category's stable name; left out for none.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The category's path, null when no name was given, or the refusal when no such category exists.</returns>
    public async Task<IGenericResult<string?>> CategoryPath(string? name, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(name)) return GenericResult<string?>.Success(null);

        var tree = await _sources.DataSetCategories(_logger, ct).ConfigureAwait(false);
        if (tree.IsFailure || tree.Value is null) return tree.ToNewResult<string?>();

        var id = CategoryLinks.Resolve(tree.Value, DataSetCategoryConfiguration.ImplementationName, name, _logger);
        if (id.IsFailure || id.Value is not { } found) return id.ToNewResult<string?>();

        return tree.Value.Find(found) is { } node
            ? GenericResult<string?>.Success(node.Path)
            : GenericResult<string?>.Success(null);
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
}

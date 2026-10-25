using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Tests.Catalog;

/// <summary>A source of one kind whose hits a test states; it records the windows it was asked for and places hits under given parents.</summary>
internal sealed class FakeRankedSearch : IRankedSearch
{
    private readonly List<SearchHit> _hits;
    private readonly Dictionary<long, string[]> _parents = [];

    public FakeRankedSearch(string kind, int maxWindow = 1000, params SearchHit[] hits)
    {
        Kind = kind;
        MaxWindow = maxWindow;
        _hits = hits.ToList();
    }

    public string Kind { get; }

    public int MaxWindow { get; }

    /// <summary>Gets the windows searches asked for.</summary>
    public List<int> Windows { get; } = [];

    /// <summary>Gets how many hits were asked to be placed, over all calls.</summary>
    public int Placed { get; private set; }

    /// <summary>Names where a hit sits; a hit with no parents stated cannot be placed.</summary>
    public FakeRankedSearch Under(long key, params string[] parents)
    {
        _parents[key] = parents;
        return this;
    }

    public Task<IGenericResult<SearchPage>> Search(string text, int window, CancellationToken cancellationToken = default)
    {
        Windows.Add(window);
        var matching = _hits
            .Where(hit => text.Length == 0
                || hit.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                || (hit.Description?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
        return Task.FromResult<IGenericResult<SearchPage>>(GenericResult<SearchPage>.Success(new SearchPage(matching.Take(window).ToList(), matching.Count)));
    }

    public Task<IGenericResult<IReadOnlyList<SearchHit>>> Place(IReadOnlyList<SearchHit> hits, CancellationToken cancellationToken = default)
    {
        Placed += hits.Count;
        var placed = hits.Where(hit => _parents.ContainsKey(hit.Key)).Select(hit => hit.Under(_parents[hit.Key])).ToList();
        return Task.FromResult<IGenericResult<IReadOnlyList<SearchHit>>>(GenericResult<IReadOnlyList<SearchHit>>.Success(placed));
    }

    /// <summary>Makes a hit of a field.</summary>
    public static SearchHit Hit(string name, long key, int rank, string? description = null)
        => new(name, description, key, rank, "Parent", key);
}

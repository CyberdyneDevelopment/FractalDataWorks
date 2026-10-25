using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions;

/// <summary>The closest things to a text, in order, and how many things match in all.</summary>
public sealed class SearchPage
{
    /// <summary>Initializes a new instance of the <see cref="SearchPage"/> class.</summary>
    /// <param name="hits">The closest things, up to the window asked for, closest first.</param>
    /// <param name="matches">How many things the text finds in all.</param>
    public SearchPage(IReadOnlyList<SearchHit> hits, int matches)
    {
        Hits = hits;
        Matches = matches;
    }

    /// <summary>Gets the closest things, closest first, with no more than the window asked for.</summary>
    public IReadOnlyList<SearchHit> Hits { get; }

    /// <summary>Gets how many things the text finds in all.</summary>
    public int Matches { get; }
}

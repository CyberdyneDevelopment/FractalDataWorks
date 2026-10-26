using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// One thing a ranked search found: a field of a data set or of a container, or a glossary term, with how close it is to the text and
/// where it sits.
/// </summary>
public sealed class SearchHit
{
    private static readonly IReadOnlyList<string> NoParents = Array.Empty<string>();

    /// <summary>Initializes a new instance of the <see cref="SearchHit"/> class, not yet placed under any names.</summary>
    /// <param name="name">The name of the thing found.</param>
    /// <param name="description">What the thing is described by; none when it has none.</param>
    /// <param name="key">The row's key, which makes the order of things with the same name the same on every request.</param>
    /// <param name="rank">How close the thing is to the text, by <see cref="SearchRank"/>.</param>
    /// <param name="parentTable">The table of the row the thing hangs from, which names its parents; empty for a thing that hangs from none.</param>
    /// <param name="parentKey">The key of the row the thing hangs from.</param>
    /// <param name="owner">The owner the thing records, for the kinds that record one.</param>
    public SearchHit(string name, string? description, long key, int rank, string parentTable, long parentKey, string? owner = null)
        : this(name, description, key, rank, parentTable, parentKey, owner, NoParents)
    {
    }

    private SearchHit(
        string name, string? description, long key, int rank, string parentTable, long parentKey, string? owner, IReadOnlyList<string> parents)
    {
        Name = name;
        Description = description;
        Key = key;
        Rank = rank;
        ParentTable = parentTable;
        ParentKey = parentKey;
        Owner = owner;
        Parents = parents;
    }

    /// <summary>Gets the name of the thing found.</summary>
    public string Name { get; }

    /// <summary>Gets what the thing is described by, or nothing when it has none.</summary>
    public string? Description { get; }

    /// <summary>Gets the row's key.</summary>
    public long Key { get; }

    /// <summary>Gets how close the thing is to the text.</summary>
    public int Rank { get; }

    /// <summary>Gets the table of the row the thing hangs from; empty for a thing that hangs from none.</summary>
    public string ParentTable { get; }

    /// <summary>Gets the key of the row the thing hangs from.</summary>
    public long ParentKey { get; }

    /// <summary>Gets the owner the thing records, or nothing.</summary>
    public string? Owner { get; }

    /// <summary>Gets the names the thing sits under, outermost first; empty until <see cref="IRankedSearch.Place"/> has read them.</summary>
    public IReadOnlyList<string> Parents { get; }

    /// <summary>Gets a copy of this hit that sits under the given names.</summary>
    /// <param name="parents">The names, outermost first.</param>
    /// <returns>The placed hit.</returns>
    public SearchHit Under(IReadOnlyList<string> parents) => new(Name, Description, Key, Rank, ParentTable, ParentKey, Owner, parents);
}

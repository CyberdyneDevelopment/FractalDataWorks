using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>One match a catalog search is choosing a page from: an entry of a kind read whole, or a hit of a kind searched at the store.</summary>
/// <param name="Rank">How close the match is to the text, by <see cref="SearchRank"/>.</param>
/// <param name="Kind">The kind of the match, one of <see cref="CatalogKinds"/>.</param>
/// <param name="Name">The name of the match.</param>
/// <param name="Tie">What settles the order of matches of the same kind and name: where an entry sits, nothing for a hit.</param>
/// <param name="Order">The position of a hit in the answer of its source, which settles the order of hits with the same name.</param>
/// <param name="Entry">The entry, for a kind read whole.</param>
/// <param name="Source">The source that found the hit, for a kind searched at the store.</param>
/// <param name="Hit">The hit, for a kind searched at the store.</param>
internal sealed record CatalogCandidate(
    int Rank, string Kind, string Name, string Tie, int Order, CatalogEntryDto? Entry, IRankedSearch? Source, SearchHit? Hit);

using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions;

/// <summary>One page of the containers of all data stores (FDW-2311).</summary>
/// <param name="Entries">The containers of the page, in container-name order.</param>
/// <param name="HasMore">True when another page follows.</param>
/// <param name="Total">How many containers the condition keeps in all; none when it was not asked for.</param>
public sealed record DataStoreContainerPage(IReadOnlyList<DataStoreContainerEntry> Entries, bool HasMore, int? Total);

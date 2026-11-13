using System;
using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>What a data set's edges are drawn from, kept after the data set itself is let go.</summary>
/// <param name="Id">The data set's logical id.</param>
/// <param name="DataStoreNames">The data stores its sources name.</param>
/// <param name="DerivedFromIds">The data sets its sources name by id.</param>
/// <param name="DerivedFromNames">The data sets it or its sources name by name.</param>
/// <remarks>
/// Edges come after every node has been sent, and a composed data set holds its fields and keys as well as its sources.
/// The map keeps this much of each, so a response over a large estate holds a few strings per data set, not the estate.
/// </remarks>
internal sealed record MapDataSetLinks(
    Guid Id,
    IReadOnlyList<string> DataStoreNames,
    IReadOnlyList<Guid> DerivedFromIds,
    IReadOnlyList<string> DerivedFromNames)
{
    /// <summary>Gets whether the data set is of kind Graph (FDW-5456): the edges it draws are marked, so the groups route can draw its reads and leave other kinds' edges as they were.</summary>
    public bool IsGraph { get; init; }

    /// <summary>Gets the data stores a Graph data set only intends to read (FDW-5296); empty for every other kind.</summary>
    public IReadOnlyList<string> IntendedDataStoreNames { get; init; } = [];

    /// <summary>Gets the ids of the data sets a Graph data set only intends to read, as its intents cite them; empty for every other kind.</summary>
    public IReadOnlyList<Guid> IntendedFromIds { get; init; } = [];

    /// <summary>Gets the names of the data sets a Graph data set only intends to read, as its intents cite them; empty for every other kind.</summary>
    public IReadOnlyList<string> IntendedFromNames { get; init; } = [];
}

using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Connections;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Carries a data store's working areas between the data store routes and the store (FDW-6394): the routes speak names, the store keeps a row for each.
/// </summary>
public static class DataStoreWorkingPaths
{
    /// <summary>Applies the names a request states to the store.</summary>
    /// <param name="target">The configuration about to be saved.</param>
    /// <param name="names">Null means the caller said nothing and the store keeps its working areas; an empty list clears them; names replace them.</param>
    /// <remarks>A row whose name the store already had (compared without regard to case) keeps its Id, so a save that changes nothing about it writes nothing new; a new name has no Id and the save mints one.</remarks>
    public static void Apply(IDataStoreImplementationConfiguration target, IEnumerable<string>? names)
    {
        if (names is null)
        {
            return;
        }

        var kept = target.WorkingPathRows
            .GroupBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        target.WorkingPathRows = [.. names.Select(name => new DataStoreWorkingPathConfiguration
        {
            Id = kept.TryGetValue(name, out var existing) ? existing.Id : default,
            Name = name,
        })];
    }

    /// <summary>States the names of the working areas the store keeps.</summary>
    /// <param name="config">The configuration a response is made from.</param>
    /// <returns>The names, in the order the store holds them; empty when it has none.</returns>
    public static IList<string> Of(IDataStoreImplementationConfiguration config) => [.. config.WorkingPaths];
}

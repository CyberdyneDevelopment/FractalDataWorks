using System.Collections.Generic;

namespace Fdw.Services.Data.DataNodes;

/// <summary>
/// A foreign key a schema discovery read, named by the catalog's own names: what <see cref="DiscoveredContainers"/> resolves to the
/// referenced container once every container of the discovery is known.
/// </summary>
/// <param name="Name">The key's name in the catalog.</param>
/// <param name="Columns">The column pairs of the key, in key order.</param>
/// <param name="ReferencedSchema">The schema of the referenced container, as the catalog names it.</param>
/// <param name="ReferencedTable">The name of the referenced container, as the catalog names it.</param>
public sealed record DiscoveredForeignKeySpec(
    string Name,
    IReadOnlyList<DiscoveredForeignKeySpecColumn> Columns,
    string ReferencedSchema,
    string ReferencedTable);

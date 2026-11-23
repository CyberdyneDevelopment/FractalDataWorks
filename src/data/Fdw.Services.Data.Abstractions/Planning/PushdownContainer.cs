using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// A container as a planner sees it (FDW-6969): the data store record it belongs to, whether that store's connection runs joins, the fields a plain
/// read of it returns and its declared primary key.
/// </summary>
/// <param name="DataStoreId">The id of the data store's configuration record: two containers are in one store when their records are the same one.</param>
/// <param name="JoinsInStore">Whether the store's connection carries a query's joins into the store.</param>
/// <param name="NamedAsStored">Whether the container's name is the name its store keeps it under: a joined statement names a joined table by it.</param>
/// <param name="Returned">The fields a read with no projection returns, in the order the read returns them, each with its stated type.</param>
/// <param name="PrimaryKey">The fields of the container's declared primary key, in key order; empty when it declares none.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record PushdownContainer(Guid DataStoreId, bool JoinsInStore, bool NamedAsStored, IReadOnlyList<PushdownField> Returned, IReadOnlyList<string> PrimaryKey);

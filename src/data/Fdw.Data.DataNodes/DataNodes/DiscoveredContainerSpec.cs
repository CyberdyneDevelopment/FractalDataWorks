using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.DataNodes;

/// <summary>
/// One container a schema discovery read, with the keys the catalog declares on it, before its keys are resolved against the other
/// containers of the discovery.
/// </summary>
/// <param name="Name">The container's object name.</param>
/// <param name="SchemaName">The schema it is in, as the catalog names it.</param>
/// <param name="ContainerType">The container type option; it states the operations the container supports (FDW-6309).</param>
/// <param name="Format">The format its rows are read and written in.</param>
/// <param name="Path">Its physical address.</param>
/// <param name="Fields">Its columns, in column order.</param>
/// <param name="Metadata">Metadata about the container.</param>
/// <param name="PrimaryKeyColumns">The columns of its primary key, in key order; empty when it declares none.</param>
/// <param name="ForeignKeys">The foreign keys declared on it; empty when it declares none or the discovery did not read them.</param>
public sealed record DiscoveredContainerSpec(
    string Name,
    string SchemaName,
    IContainerType ContainerType,
    IFormatType Format,
    IPath Path,
    IReadOnlyList<IField> Fields,
    IReadOnlyDictionary<string, object> Metadata,
    IReadOnlyList<string> PrimaryKeyColumns,
    IReadOnlyList<DiscoveredForeignKeySpec> ForeignKeys);

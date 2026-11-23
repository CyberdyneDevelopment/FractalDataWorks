using System;
using System.Collections.Generic;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Search;

/// <summary>
/// Finds the fields of containers (<c>data.DataContainerField</c>) closest to a text. A field hangs from a container, which hangs from a
/// path, which hangs from the implementation row of a data store, which points at the store's domain row. A field's description is not a column of
/// <c>data.DataContainerField</c> (FDW-6589): the search asks the descriptions table which fields a text finds and what each is described by.
/// </summary>
internal sealed class ContainerFieldSearch : RankedSearchBase
{
    private const string Container = "DataContainerField";

    public ContainerFieldSearch(
        IConfigurationGatewayProvider gateways, string dataStoreName, string pathName, IRowDescriptions descriptions, ILogger<ContainerFieldSearch>? logger = null)
        : base(gateways, dataStoreName, pathName, Container, logger ?? NullLogger<ContainerFieldSearch>.Instance, descriptions)
    {
    }

    /// <inheritdoc />
    public override string Kind => "ContainerField";

    /// <inheritdoc />
    protected override SearchHit ToHit(RankedRow ranked)
    {
        var row = ranked.Row;
        var owner = row.Number("DataContainerRowId") ?? throw new InvalidOperationException($"Field row {ranked.Key} of {Container} hangs from no container");
        return new SearchHit(row.Text("Name") ?? string.Empty, null, ranked.Key, ranked.Rank, "DataContainer", owner);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<RowHop> HopsFrom(string parentTable) =>
    [
        new RowHop("DataContainer", "Name", "DataPathRowId"),
        new RowHop("DataPath", "Name", "DataStoreConfigurationRowId"),
        new RowHop("DataStoreConfiguration", null, "DataStoreRowId"),
        new RowHop("DataStore", "Name", null),
    ];
}

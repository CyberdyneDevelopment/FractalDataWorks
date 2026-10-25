using System;
using System.Collections.Generic;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Search;

/// <summary>
/// Finds the fields of data sets (<c>data.DataSetField</c>) closest to a text. A field hangs from the implementation row of a Simple,
/// Compound or Federated data set, which points at the data set's domain row, which names it.
/// </summary>
internal sealed class DataSetFieldSearch : RankedSearchBase
{
    private const string Container = "DataSetField";

    // The owner columns of a field, one per data set implementation; the column's name without RowId is the owner's container.
    private static readonly string[] OwnerColumns =
    [
        "SimpleDataSetConfigurationRowId",
        "CompoundDataSetConfigurationRowId",
        "FederatedDataSetConfigurationRowId",
    ];

    public DataSetFieldSearch(IConfigurationGatewayProvider gateways, string dataStoreName, string pathName, ILogger<DataSetFieldSearch>? logger = null)
        : base(gateways, dataStoreName, pathName, Container, logger ?? NullLogger<DataSetFieldSearch>.Instance)
    {
    }

    /// <inheritdoc />
    public override string Kind => "DataSetField";

    /// <inheritdoc />
    protected override SearchHit ToHit(RankedRow ranked)
    {
        var row = ranked.Row;
        foreach (var column in OwnerColumns)
        {
            if (row.Number(column) is { } owner)
            {
                return new SearchHit(
                    row.Text("Name") ?? string.Empty, row.Text("Description"), ranked.Key, ranked.Rank, column.Substring(0, column.Length - "RowId".Length), owner);
            }
        }

        throw new InvalidOperationException($"Field row {ranked.Key} of {Container} hangs from none of {string.Join(", ", OwnerColumns)}");
    }

    /// <inheritdoc />
    protected override IReadOnlyList<RowHop> HopsFrom(string parentTable) =>
    [
        new RowHop(parentTable, null, "DataSetRowId"),
        new RowHop("DataSet", "Name", null),
    ];
}

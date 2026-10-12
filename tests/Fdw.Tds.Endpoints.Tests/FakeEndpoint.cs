using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Tests;

/// <summary>
/// One schema, Sales, holding one table, Orders, whose store pages and whose Amount is not filterable, and a
/// second, Events, whose store does not. Rows come back as given: what the store was asked to do is read from <see cref="Commands"/>.
/// </summary>
internal sealed class FakeEndpoint : TdsEndpointBase
{
    private static readonly DateTime Created = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Local);

    public FakeEndpoint()
        : base(null)
    {
    }

    public static SqlTable Orders { get; } = new("Sales", "Orders",
    [
        new SqlTableColumn("Id", 1, typeof(int), "int", isNullable: false, maxLength: null, isKey: true, isQueryable: true, isFilterable: true),
        new SqlTableColumn("Customer", 2, typeof(string), "string", isNullable: true, maxLength: 100, isKey: false, isQueryable: true, isFilterable: true),
        new SqlTableColumn("Amount", 3, typeof(decimal), "decimal", isNullable: true, maxLength: null, isKey: false, isQueryable: true, isFilterable: false),
    ], pagesInStore: true, target: "OrdersSet", Created, Created);

    public static SqlTable Events { get; } = new("Sales", "Events",
    [
        new SqlTableColumn("Name", 1, typeof(string), "string", isNullable: false, maxLength: 50, isKey: false, isQueryable: true, isFilterable: true),
    ], pagesInStore: false, target: "EventsSet", Created, Created);

    public List<QueryCommand<Dictionary<string, object?>>> Commands { get; } = [];

    public List<Dictionary<string, object?>> OrderRows { get; } =
    [
        new() { ["Id"] = 1, ["Customer"] = "Acme", ["Amount"] = 10.50m },
        new() { ["Id"] = 2, ["Customer"] = "Globex", ["Amount"] = 20.25m },
        new() { ["Id"] = 3, ["Customer"] = null, ["Amount"] = null },
    ];

    public override Task<IGenericResult<IReadOnlyList<string>>> Schemas(CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<IReadOnlyList<string>>.Success(["Sales"]));

    public override Task<IGenericResult<IReadOnlyList<SqlTable>>> Tables(string schema, CancellationToken cancellationToken = default)
        => Task.FromResult(string.Equals(schema, "Sales", StringComparison.OrdinalIgnoreCase)
            ? GenericResult<IReadOnlyList<SqlTable>>.Success([Orders, Events])
            : GenericResult<IReadOnlyList<SqlTable>>.Failure(new TableMissingCode()));

    public override Task<IGenericResult<SqlTable>> Table(string schema, string name, CancellationToken cancellationToken = default)
    {
        var table = !string.Equals(schema, "Sales", StringComparison.OrdinalIgnoreCase) ? null
            : string.Equals(name, "Orders", StringComparison.OrdinalIgnoreCase) ? Orders
            : string.Equals(name, "Events", StringComparison.OrdinalIgnoreCase) ? Events
            : null;
        return Task.FromResult(table is null
            ? GenericResult<SqlTable>.Failure(new TableMissingCode())
            : GenericResult<SqlTable>.Success(table));
    }

    public override Task<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> Rows(
        SqlTable table, QueryCommand<Dictionary<string, object?>> command, CancellationToken cancellationToken = default)
    {
        Commands.Add(command);
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows = ReferenceEquals(table, Orders) ? OrderRows : [];
        return Task.FromResult(GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(rows));
    }
}

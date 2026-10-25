using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Search;
using Shouldly;
using Xunit;
using static Fdw.Services.Data.Tests.Search.InMemoryConfigurationStore;

namespace Fdw.Services.Data.Tests.Search;

/// <summary>
/// FDW-2444: the searches for the fields of data sets and of containers find the closest fields at the database and name where a page's
/// fields sit by reading the parents of those fields only.
/// </summary>
public sealed class FieldSearchTests
{
    // The table has one nullable owner column for each data set implementation; a field holds a value in the one that owns it.
    private static readonly string[] OwnerColumns =
    [
        "SimpleDataSetConfigurationRowId",
        "CompoundDataSetConfigurationRowId",
        "FederatedDataSetConfigurationRowId",
    ];

    private static Dictionary<string, object?> DataSetField(int key, string name, string? description, string ownerColumn, int owner)
        => Row([("RowId", key), ("Name", name), ("Description", description), .. OwnerColumns.Select(column => (column, string.Equals(column, ownerColumn, StringComparison.Ordinal) ? (object?)owner : null))]);

    // Three data sets, one of each kind: Orders (Simple), Customers (Compound), Sales (Federated).
    private static (DataSetFieldSearch Search, InMemoryConfigurationStore Store) DataSets()
    {
        var store = new InMemoryConfigurationStore()
            .Holds("DataSetField",
            [
                DataSetField(1, "Amount", "order amount", "SimpleDataSetConfigurationRowId", 11),
                DataSetField(2, "Amount", null, "CompoundDataSetConfigurationRowId", 21),
                DataSetField(3, "AmountDue", null, "FederatedDataSetConfigurationRowId", 31),
                DataSetField(4, "Name", "customer name", "CompoundDataSetConfigurationRowId", 21),
                DataSetField(5, "Gone", "amount lost", "SimpleDataSetConfigurationRowId", 99),
            ])
            .Holds("SimpleDataSetConfiguration", [Row(("RowId", 11), ("DataSetRowId", 101))])
            .Holds("CompoundDataSetConfiguration", [Row(("RowId", 21), ("DataSetRowId", 102))])
            .Holds("FederatedDataSetConfiguration", [Row(("RowId", 31), ("DataSetRowId", 103))])
            .Holds("DataSet", [Row(("RowId", 101), ("Name", "Orders")), Row(("RowId", 102), ("Name", "Customers")), Row(("RowId", 103), ("Name", "Sales"))]);
        return (new DataSetFieldSearch(store.Gateways, "PlatformConfiguration", "data"), store);
    }

    [Fact]
    public async Task TheClosestFieldsComeFirstWithTheirOwnersTable()
    {
        var (search, _) = DataSets();

        var found = await search.Search("amount", 10, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        found.Value!.Hits.Select(hit => (hit.Key, hit.Rank, hit.ParentTable)).ToList().ShouldBe(
        [
            (1L, 0, "SimpleDataSetConfiguration"),
            (2L, 0, "CompoundDataSetConfiguration"),
            (3L, 1, "FederatedDataSetConfiguration"),
            (5L, 3, "SimpleDataSetConfiguration"),
        ]);
        found.Value.Matches.ShouldBe(4);
    }

    [Fact]
    public async Task PlacingNamesTheDataSetEachFieldSitsIn()
    {
        var (search, _) = DataSets();
        var found = await search.Search("amount", 3, TestContext.Current.CancellationToken);

        var placed = await search.Place(found.Value!.Hits, TestContext.Current.CancellationToken);

        placed.IsSuccess.ShouldBeTrue(placed.CurrentMessage);
        placed.Value!.Select(hit => (hit.Key, Parent: string.Join("/", hit.Parents))).ToList().ShouldBe(
            [(1L, "Orders"), (2L, "Customers"), (3L, "Sales")]);
    }

    [Fact]
    public async Task AFieldWhoseParentTheCallerCannotReadIsLeftOutOfThePlacedPage()
    {
        var (search, _) = DataSets();
        var found = await search.Search("amount", 10, TestContext.Current.CancellationToken);

        var placed = await search.Place(found.Value!.Hits, TestContext.Current.CancellationToken);

        placed.Value!.Select(hit => hit.Key).ShouldBe([1L, 2L, 3L]);
    }

    [Fact]
    public async Task PlacingReadsTheParentsOfThePageOnly()
    {
        var (search, store) = DataSets();
        var found = await search.Search("name", 1, TestContext.Current.CancellationToken);
        var before = store.Queries.Count;

        await search.Place(found.Value!.Hits, TestContext.Current.CancellationToken);

        // One field of the Compound kind: its implementation row, then its data set. Nothing else is read.
        store.Queries.Skip(before).ToList().ShouldBe(["CompoundDataSetConfiguration", "DataSet"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public async Task AWindowOutsideTheLimitsIsRefusedBeforeAnythingIsRead(int window)
    {
        var (search, store) = DataSets();

        var found = await search.Search("amount", window, TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        found.CurrentMessage.ShouldNotBeNull().ShouldContain("1000");
        store.Queries.ShouldBeEmpty();
    }

    [Fact]
    public async Task AFieldThatHangsFromNothingFailsTheSearchNamingIt()
    {
        var store = new InMemoryConfigurationStore().Holds("DataSetField", [DataSetField(7, "Orphan", null, string.Empty, 0)]);
        var search = new DataSetFieldSearch(store.Gateways, "PlatformConfiguration", "data");

        var found = await search.Search("orphan", 5, TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        found.CurrentMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task AContainerFieldIsPlacedUnderItsStoreItsPathAndItsContainerOutermostFirst()
    {
        var store = new InMemoryConfigurationStore()
            .Holds("DataContainerField", [Row(("RowId", 1), ("Name", "OrderId"), ("Description", null), ("DataContainerRowId", 5))])
            .Holds("DataContainer", [Row(("RowId", 5), ("Name", "Orders"), ("DataPathRowId", 6)), Row(("RowId", 55), ("Name", "Other"), ("DataPathRowId", 66))])
            .Holds("DataPath", [Row(("RowId", 6), ("Name", "dbo"), ("DataStoreConfigurationRowId", 7))])
            .Holds("DataStoreConfiguration", [Row(("RowId", 7), ("DataStoreRowId", 8))])
            .Holds("DataStore", [Row(("RowId", 8), ("Name", "Warehouse"))]);
        var search = new ContainerFieldSearch(store.Gateways, "PlatformConfiguration", "data");

        var found = await search.Search("orderid", 5, TestContext.Current.CancellationToken);
        var placed = await search.Place(found.Value!.Hits, TestContext.Current.CancellationToken);

        found.Value.Hits.ShouldHaveSingleItem().ParentTable.ShouldBe("DataContainer");
        placed.Value!.ShouldHaveSingleItem().Parents.ShouldBe(["Warehouse", "dbo", "Orders"]);
    }

    [Fact]
    public void TheKindsAreWhatTheCatalogAsksFor()
    {
        var store = new InMemoryConfigurationStore();

        new DataSetFieldSearch(store.Gateways, "PlatformConfiguration", "data").Kind.ShouldBe("DataSetField");
        new ContainerFieldSearch(store.Gateways, "PlatformConfiguration", "data").Kind.ShouldBe("ContainerField");
    }
}

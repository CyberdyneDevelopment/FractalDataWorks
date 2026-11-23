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

    private static Dictionary<string, object?> DataSetField(int key, string name, string ownerColumn, int owner)
        => Row([("RowId", key), ("Id", DescriptionsOfRows.IdOf(key)), ("Name", name), .. OwnerColumns.Select(column => (column, string.Equals(column, ownerColumn, StringComparison.Ordinal) ? (object?)owner : null))]);

    // The descriptions table, which holds the text of the fields (FDW-6208): fields 1, 4 and 5 are described.
    private static DescriptionsOfRows Described() => new(
    [
        new(DescriptionsOfRows.IdOf(1), "order amount"),
        new(DescriptionsOfRows.IdOf(4), "customer name"),
        new(DescriptionsOfRows.IdOf(5), "amount lost"),
    ]);

    // Three data sets, one of each kind: Orders (Simple), Customers (Compound), Sales (Federated).
    private static (DataSetFieldSearch Search, InMemoryConfigurationStore Store) DataSets()
    {
        var store = new InMemoryConfigurationStore()
            .Holds("DataSetField",
            [
                DataSetField(1, "Amount", "SimpleDataSetConfigurationRowId", 11),
                DataSetField(2, "Amount", "CompoundDataSetConfigurationRowId", 21),
                DataSetField(3, "AmountDue", "FederatedDataSetConfigurationRowId", 31),
                DataSetField(4, "Name", "CompoundDataSetConfigurationRowId", 21),
                DataSetField(5, "Gone", "SimpleDataSetConfigurationRowId", 99),
            ])
            .Holds("SimpleDataSetConfiguration", [Row(("RowId", 11), ("DataSetRowId", 101))])
            .Holds("CompoundDataSetConfiguration", [Row(("RowId", 21), ("DataSetRowId", 102))])
            .Holds("FederatedDataSetConfiguration", [Row(("RowId", 31), ("DataSetRowId", 103))])
            .Holds("DataSet", [Row(("RowId", 101), ("Name", "Orders")), Row(("RowId", 102), ("Name", "Customers")), Row(("RowId", 103), ("Name", "Sales"))]);
        return (new DataSetFieldSearch(store.Gateways, "PlatformConfiguration", "data", Described()), store);
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
    public async Task AFieldFoundOnlyByItsDescriptionRanksLastAndCarriesTheTextReadFromTheDescriptionsTable()
    {
        var (search, _) = DataSets();

        var found = await search.Search("lost", 10, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        var hit = found.Value!.Hits.ShouldHaveSingleItem();
        (hit.Key, hit.Rank, hit.Description).ShouldBe((5L, 3, "amount lost"));
        found.Value.Matches.ShouldBe(1);
    }

    [Fact]
    public async Task AFieldFoundByBothItsNameAndItsDescriptionIsCountedOnce()
    {
        var (search, _) = DataSets();

        var found = await search.Search("name", 10, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        found.Value!.Hits.Select(hit => hit.Key).ShouldBe([4L]);
        found.Value.Matches.ShouldBe(1);
    }

    [Fact]
    public async Task APageOfHitsCarriesTheDescriptionOfEachFieldThatHasOne()
    {
        var (search, _) = DataSets();

        var found = await search.Search("amount", 10, TestContext.Current.CancellationToken);

        found.Value!.Hits.Select(hit => (hit.Key, hit.Description)).ToList().ShouldBe(
            [(1L, "order amount"), (2L, null), (3L, null), (5L, "amount lost")]);
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
        var store = new InMemoryConfigurationStore().Holds("DataSetField", [DataSetField(7, "Orphan", string.Empty, 0)]);
        var search = new DataSetFieldSearch(store.Gateways, "PlatformConfiguration", "data", Described());

        var found = await search.Search("orphan", 5, TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        found.CurrentMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task AContainerFieldIsPlacedUnderItsStoreItsPathAndItsContainerOutermostFirst()
    {
        var store = new InMemoryConfigurationStore()
            .Holds("DataContainerField", [Row(("RowId", 1), ("Id", DescriptionsOfRows.IdOf(1)), ("Name", "OrderId"), ("DataContainerRowId", 5))])
            .Holds("DataContainer", [Row(("RowId", 5), ("Name", "Orders"), ("DataPathRowId", 6)), Row(("RowId", 55), ("Name", "Other"), ("DataPathRowId", 66))])
            .Holds("DataPath", [Row(("RowId", 6), ("Name", "dbo"), ("DataStoreConfigurationRowId", 7))])
            .Holds("DataStoreConfiguration", [Row(("RowId", 7), ("DataStoreRowId", 8))])
            .Holds("DataStore", [Row(("RowId", 8), ("Name", "Warehouse"))]);
        var search = new ContainerFieldSearch(store.Gateways, "PlatformConfiguration", "data", new DescriptionsOfRows([]));

        var found = await search.Search("orderid", 5, TestContext.Current.CancellationToken);
        var placed = await search.Place(found.Value!.Hits, TestContext.Current.CancellationToken);

        found.Value.Hits.ShouldHaveSingleItem().ParentTable.ShouldBe("DataContainer");
        placed.Value!.ShouldHaveSingleItem().Parents.ShouldBe(["Warehouse", "dbo", "Orders"]);
    }

    [Fact]
    public void TheKindsAreWhatTheCatalogAsksFor()
    {
        var store = new InMemoryConfigurationStore();

        new DataSetFieldSearch(store.Gateways, "PlatformConfiguration", "data", Described()).Kind.ShouldBe("DataSetField");
        new ContainerFieldSearch(store.Gateways, "PlatformConfiguration", "data", new DescriptionsOfRows([])).Kind.ShouldBe("ContainerField");
    }

    [Fact]
    public async Task AContainerFieldIsFoundByItsDescriptionFromTheDescriptionsTableAndCarriesItsText()
    {
        // FDW-6589: data.DataContainerField has no Description column; the search asks the descriptions table which fields a text finds and what each says.
        var store = new InMemoryConfigurationStore()
            .Holds("DataContainerField",
            [
                Row(("RowId", 1), ("Id", DescriptionsOfRows.IdOf(1)), ("Name", "OrderId"), ("DataContainerRowId", 5)),
                Row(("RowId", 2), ("Id", DescriptionsOfRows.IdOf(2)), ("Name", "Placed"), ("DataContainerRowId", 5)),
            ])
            .Holds("DataContainer", [Row(("RowId", 5), ("Name", "Orders"), ("DataPathRowId", 6))])
            .Holds("DataPath", [Row(("RowId", 6), ("Name", "dbo"), ("DataStoreConfigurationRowId", 7))])
            .Holds("DataStoreConfiguration", [Row(("RowId", 7), ("DataStoreRowId", 8))])
            .Holds("DataStore", [Row(("RowId", 8), ("Name", "Warehouse"))]);
        var described = new DescriptionsOfRows([new(DescriptionsOfRows.IdOf(2), "when the order was placed")]);
        var search = new ContainerFieldSearch(store.Gateways, "PlatformConfiguration", "data", described);

        var found = await search.Search("when the order", 5, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        var hit = found.Value!.Hits.ShouldHaveSingleItem();
        (hit.Key, hit.Description, hit.ParentTable).ShouldBe((2L, "when the order was placed", "DataContainer"));
        described.Asked.ShouldBe(1);
    }
}

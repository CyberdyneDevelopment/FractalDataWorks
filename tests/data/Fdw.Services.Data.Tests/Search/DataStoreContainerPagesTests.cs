using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Search;
using Shouldly;
using Xunit;
using static Fdw.Services.Data.Tests.Search.InMemoryConfigurationStore;

namespace Fdw.Services.Data.Tests.Search;

/// <summary>
/// FDW-2311: the containers of all data stores are read one page at a time, in container-name order, with the names of the path and the store above each,
/// and the rows read for a page do not grow with the number of containers.
/// </summary>
public sealed class DataStoreContainerPagesTests
{
    private static Dictionary<string, object?> Container(int key, string name, string type, int path)
        => Row(("RowId", key), ("Id", Guid.Parse($"00000000-0000-0000-0000-{key:D12}")), ("Name", name), ("TypeId", type), ("DataPathRowId", path), ("ClassificationId", 470168002));

    // Two stores, Sales (key 1) with the path Reports (key 10) and Ops (key 2) with the path Raw (key 20).
    private static InMemoryConfigurationStore Stores(IEnumerable<Dictionary<string, object?>> containers)
        => new InMemoryConfigurationStore()
            .Holds("DataContainer", containers)
            .Holds("DataPath", [Row(("RowId", 10), ("Name", "Reports"), ("DataStoreConfigurationRowId", 100)), Row(("RowId", 20), ("Name", "Raw"), ("DataStoreConfigurationRowId", 200))])
            .Holds("DataStoreConfiguration", [Row(("RowId", 100), ("DataStoreRowId", 1)), Row(("RowId", 200), ("DataStoreRowId", 2))])
            .Holds("DataStore", [Row(("RowId", 1), ("Name", "Sales")), Row(("RowId", 2), ("Name", "Ops"))]);

    private static DataStoreContainerPages PagesOf(InMemoryConfigurationStore store) => new(store.Gateways, "PlatformConfiguration", "data");

    [Fact]
    public async Task APageComesInContainerNameOrderWithThePathAndStoreAboveEachContainer()
    {
        var store = Stores([Container(3, "Orders", "Table", 10), Container(1, "Audit", "File", 20), Container(2, "Customers", "Table", 10)]);

        var read = await PagesOf(store).Page(null, 0, 10, false, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value!.Entries.Select(entry => (entry.ContainerName, entry.PathName, entry.DataStoreName, entry.ContainerType)).ToList().ShouldBe(
        [
            ("Audit", "Raw", "Ops", "File"),
            ("Customers", "Reports", "Sales", "Table"),
            ("Orders", "Reports", "Sales", "Table"),
        ]);
        read.Value.HasMore.ShouldBeFalse();
        read.Value.Total.ShouldBeNull();
    }

    [Fact]
    public async Task ThePageHoldsTakeContainersAndSaysWhetherAnotherPageFollows()
    {
        var store = Stores([Container(1, "A", "Table", 10), Container(2, "B", "Table", 10), Container(3, "C", "Table", 10)]);

        var first = await PagesOf(store).Page(null, 0, 2, false, TestContext.Current.CancellationToken);
        var second = await PagesOf(store).Page(null, 2, 2, false, TestContext.Current.CancellationToken);

        first.Value!.Entries.Select(entry => entry.ContainerName).ToList().ShouldBe(["A", "B"]);
        first.Value.HasMore.ShouldBeTrue();
        second.Value!.Entries.Select(entry => entry.ContainerName).ToList().ShouldBe(["C"]);
        second.Value.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task TheRowsReadForAPageDoNotGrowWithTheNumberOfContainers()
    {
        var store = Stores(Enumerable.Range(1, 5000).Select(key => Container(key, $"Container{key:D5}", "Table", 10)));

        var read = await PagesOf(store).Page(null, 100, 10, false, TestContext.Current.CancellationToken);

        read.Value!.Entries.Count.ShouldBe(10);
        // One more than the page, then the one path, its store configuration and its store: 11 + 1 + 1 + 1.
        store.RowsReturned.ShouldBe(14);
        store.Queries.ShouldBe(["DataContainer", "DataPath", "DataStoreConfiguration", "DataStore"]);
    }

    [Fact]
    public async Task AConditionNarrowsThePageAndTheTotalCountsWhatItKeeps()
    {
        var store = Stores([Container(1, "Orders", "Table", 10), Container(2, "OrderLines", "Table", 10), Container(3, "Customers", "Table", 10)]);
        var condition = new FilterCondition { PropertyName = "Name", Operator = FilterOperators.ByName("Contains"), Value = "order" };

        var read = await PagesOf(store).Page(condition, 0, 1, true, TestContext.Current.CancellationToken);

        read.Value!.Entries.Select(entry => entry.ContainerName).ToList().ShouldBe(["OrderLines"]);
        read.Value.HasMore.ShouldBeTrue();
        read.Value.Total.ShouldBe(2);
    }

    [Fact]
    public async Task AContainerWhoseParentsAreNotHeldIsLeftOutOfThePage()
    {
        var store = Stores([Container(1, "Orders", "Table", 10), Container(2, "Lost", "Table", 99)]);

        var read = await PagesOf(store).Page(null, 0, 10, false, TestContext.Current.CancellationToken);

        read.Value!.Entries.Select(entry => entry.ContainerName).ToList().ShouldBe(["Orders"]);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 1001)]
    [InlineData(-1, 10)]
    public async Task APageOutsideTheLimitsIsRefusedBeforeAnythingIsRead(int skip, int take)
    {
        var store = Stores([Container(1, "Orders", "Table", 10)]);

        var read = await PagesOf(store).Page(null, skip, take, false, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        store.Queries.ShouldBeEmpty();
    }
}

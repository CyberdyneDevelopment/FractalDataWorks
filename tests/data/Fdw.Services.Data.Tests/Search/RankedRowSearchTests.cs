using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Shouldly;
using Xunit;
using static Fdw.Services.Data.Tests.Search.InMemoryConfigurationStore;

namespace Fdw.Services.Data.Tests.Search;

/// <summary>
/// FDW-2444: a search over a container asks the database for ordered windows and keeps the closest rows. The closest rows are the ones the
/// in-memory rule would pick over the whole table, and the rows read do not grow with the table.
/// </summary>
public sealed class RankedRowSearchTests
{
    private const string Container = "DataSetField";

    private static (RankedRowSearch Search, InMemoryConfigurationStore Store) Over(IEnumerable<Dictionary<string, object?>> rows)
    {
        var store = new InMemoryConfigurationStore().Holds(Container, rows);
        var reader = new ConfigurationRowReader(store.Gateways, "PlatformConfiguration", "data", Container);
        return (new RankedRowSearch(reader, "Name", "Description", "RowId"), store);
    }

    private static Dictionary<string, object?> Field(int key, string name, string? description = null)
        => Row(("RowId", key), ("Name", name), ("Description", description));

    // The rule written out over the whole table: rank, then name ignoring case, then key.
    private static List<long> Expected(List<Dictionary<string, object?>> rows, string text, int window)
        => rows
            .Select(row => (Key: Convert.ToInt64(row["RowId"], CultureInfo.InvariantCulture), Name: (string)row["Name"]!, Rank: SearchRank.Of((string)row["Name"]!, (string?)row["Description"], text)))
            .Where(candidate => candidate.Rank != SearchRank.NoMatch)
            .OrderBy(candidate => candidate.Rank)
            .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Key)
            .Take(window)
            .Select(candidate => candidate.Key)
            .ToList();

    private static List<Dictionary<string, object?>> Table(int size)
    {
        var rows = new List<Dictionary<string, object?>>();
        for (var i = 0; i < size; i++)
        {
            rows.Add(Field(i + 1, $"col{i % 97}_{i}", i % 5 == 0 ? $"the amount of item {i % 31}" : null));
        }

        rows.Add(Field(size + 1, "Amount"));
        rows.Add(Field(size + 2, "amount", "again"));
        rows.Add(Field(size + 3, "AmountDue"));
        rows.Add(Field(size + 4, "TotalAmount"));
        rows.Add(Field(size + 5, "Net", "the amount after tax"));
        return rows;
    }

    [Theory]
    [InlineData("amount", 1)]
    [InlineData("amount", 3)]
    [InlineData("amount", 4)]
    [InlineData("amount", 10)]
    [InlineData("amount", 200)]
    [InlineData("col1_", 5)]
    [InlineData("item 3", 7)]
    [InlineData("zzz", 5)]
    public async Task TheClosestRowsAreTheOnesTheRuleWouldPickOverTheWholeTable(string text, int window)
    {
        var rows = Table(300);
        var (search, _) = Over(rows);

        var found = await search.Search(text, window, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        found.Value!.Rows.Select(row => row.Key).ToList().ShouldBe(Expected(rows, text, window));
    }

    [Fact]
    public async Task TheRanksAreTheRulesRanks()
    {
        var (search, _) = Over(Table(20));

        var found = await search.Search("amount", 5, TestContext.Current.CancellationToken);

        found.Value!.Rows.Select(row => row.Rank).ShouldBe([0, 0, 1, 2, 3]);
    }

    [Fact]
    public async Task TheNumberOfMatchesIsCountedAtTheStoreNotFromTheWindow()
    {
        var rows = Table(300);
        var (search, _) = Over(rows);

        var found = await search.Search("amount", 3, TestContext.Current.CancellationToken);

        found.Value!.Rows.Count.ShouldBe(3);
        found.Value.Matches.ShouldBe(rows.Count(row => SearchRank.Of((string)row["Name"]!, (string?)row["Description"], "amount") != SearchRank.NoMatch));
    }

    [Fact]
    public async Task ASearchWithNoTextListsTheFirstRowsByNameAndCountsAll()
    {
        var rows = Table(50);
        var (search, _) = Over(rows);

        var found = await search.Search(string.Empty, 4, TestContext.Current.CancellationToken);

        found.Value!.Rows.Select(row => row.Key).ToList().ShouldBe(Expected(rows, string.Empty, 4));
        found.Value.Matches.ShouldBe(rows.Count);
    }

    [Fact]
    public async Task TheRowsReadDoNotGrowWithTheTable()
    {
        var (smallSearch, smallStore) = Over(Table(400));
        var (largeSearch, largeStore) = Over(Table(20000));

        await smallSearch.Search("amount", 25, TestContext.Current.CancellationToken);
        await largeSearch.Search("amount", 25, TestContext.Current.CancellationToken);

        // The queries are three windows and the count, however many rows the table holds.
        largeStore.RowsReturned.ShouldBeLessThanOrEqualTo(5 * 25);
        largeStore.Queries.Count.ShouldBe(smallStore.Queries.Count);
        largeStore.RowsReturned.ShouldBe(smallStore.RowsReturned);
    }

    [Fact]
    public async Task AFullWindowOfRowsThatStartWithTheTextNeedsNoWiderQuery()
    {
        var rows = Enumerable.Range(1, 30).Select(i => Field(i, $"amount{i:00}")).ToList();
        var (search, store) = Over(rows);

        var found = await search.Search("amount", 10, TestContext.Current.CancellationToken);

        found.Value!.Rows.Count.ShouldBe(10);
        // One window of the names that start with the text, and the count.
        store.Queries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task RowsWithTheSameNameComeInTheOrderOfTheirKeys()
    {
        var rows = new[] { Field(30, "Id"), Field(10, "id"), Field(20, "ID"), Field(5, "Identity") };
        var (search, _) = Over(rows);

        var found = await search.Search("id", 4, TestContext.Current.CancellationToken);

        found.Value!.Rows.Select(row => row.Key).ShouldBe([10, 20, 30, 5]);
    }

    [Fact]
    public async Task ARetiredOrDeletedRowIsNeverFound()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            Field(1, "amount"),
            Row(("RowId", 2), ("Name", "amount old"), ("Description", null), ("IsCurrent", false)),
            Row(("RowId", 3), ("Name", "amount gone"), ("Description", null), ("IsDeleted", true)),
        };
        var (search, _) = Over(rows);

        var found = await search.Search("amount", 10, TestContext.Current.CancellationToken);

        found.Value!.Rows.Select(row => row.Key).ShouldBe([1]);
        found.Value.Matches.ShouldBe(1);
    }
}

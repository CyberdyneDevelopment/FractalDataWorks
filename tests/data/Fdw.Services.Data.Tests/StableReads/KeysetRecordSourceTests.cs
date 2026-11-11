using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Caching;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.StableReads;

/// <summary>
/// Tests for keyset pages (FDW-4911): a store read in the order of its key, each page after the last key, yields every row exactly once,
/// even when the order the store picks by itself changes from one read to the next.
/// </summary>
public sealed class KeysetRecordSourceTests
{
    // No test here reads more than a few dozen rows; a read past this did not stop, and the test fails rather than run out of memory.
    private const int RecordsBeforeTheReadIsRunaway = 10_000;

    private static readonly RecordSchema Schema = new([Field("Region", 0), Field("Id", 1), Field("Name", 2)]);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoreWhoseOwnOrderChangesOnEveryReadYieldsEveryRowExactlyOnce()
    {
        var store = new ShufflingStore(Rows(23));
        var source = Keyset(store.Read, ["Id"], pageSize: 5);

        var ids = await Ids(source);

        ids.Order().ShouldBe(Enumerable.Range(0, 23));
        ids.Distinct().Count().ShouldBe(23);
        store.Reads.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task OffsetPagesOverTheSameStoreRepeatOrDropRowsWhichIsWhatKeysetPagesPrevent()
    {
        var store = new ShufflingStore(Rows(23));
        var read = new List<int>();
        for (var skip = 0; skip < 23; skip += 5)
        {
            var page = await store.Read(new QueryCommand<Dictionary<string, object?>> { Paging = new PagingExpression { Skip = skip, Take = 5 } }, TestContext.Current.CancellationToken);
            read.AddRange(page.Value!.Select(row => (int)row["Id"]!));
        }

        read.Distinct().Count().ShouldBeLessThan(23);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ACompositeKeyWithRepeatedLeadingValuesIsPagedByEveryKeyField()
    {
        var rows = Enumerable.Range(0, 17).Select(i => Row(region: i % 3, id: i)).ToList();
        var store = new ShufflingStore(rows);
        var source = Keyset(store.Read, ["Region", "Id"], pageSize: 4);

        var ids = await Ids(source);

        ids.Order().ShouldBe(Enumerable.Range(0, 17));
        ids.Distinct().Count().ShouldBe(17);
        store.Orderings.ShouldAllBe(order => order == "Region,Id");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoreThatIgnoresTheKeyFilterStopsTheReadWithItsCodeRatherThanRepeatingRows()
    {
        var rows = Rows(10);
        Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>> IgnoresFilter(IDataCommand command, CancellationToken _) =>
            Task.FromResult<IGenericResult<IEnumerable<Dictionary<string, object?>>>>(
                GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows.Take(((IQueryCommand)command).Paging!.Take!.Value).ToList()));

        var results = await All(Keyset(IgnoresFilter, ["Id"], pageSize: 4));

        results.Count(r => r.IsSuccess).ShouldBe(4);
        var failure = results[^1];
        failure.IsFailure.ShouldBeTrue();
        failure.CurrentMessage.ShouldNotBeNull().ShouldContain("does not apply the filter keyset paging needs");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoreWhosePageStartsAtTheLastKeyStopsTheReadWithItsCode()
    {
        var rows = Rows(10);
        var reads = 0;
        Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>> Overlaps(IDataCommand command, CancellationToken _)
        {
            var take = ((IQueryCommand)command).Paging!.Take!.Value;
            var start = reads++ == 0 ? 0 : take - 1;
            return Task.FromResult<IGenericResult<IEnumerable<Dictionary<string, object?>>>>(
                GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows.Skip(start).Take(take).ToList()));
        }

        var results = await All(Keyset(Overlaps, ["Id"], pageSize: 4));

        results.Count(r => r.IsSuccess).ShouldBe(4);
        results[^1].IsFailure.ShouldBeTrue();
        results[^1].CurrentMessage.ShouldNotBeNull().ShouldContain("repeats key (3)");
        reads.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoreThatAnswersItsWholeSetToTheFirstAskIsReadOnce()
    {
        var rows = Rows(12);
        var reads = 0;
        Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>> Whole(IDataCommand command, CancellationToken _)
        {
            reads++;
            return Task.FromResult<IGenericResult<IEnumerable<Dictionary<string, object?>>>>(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
        }

        var ids = await Ids(Keyset(Whole, ["Id"], pageSize: 5));

        ids.Count.ShouldBe(12);
        reads.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARowWithNoKeyValueStopsTheReadWithItsCode()
    {
        var rows = Rows(5);
        rows[4]["Id"] = null;
        var store = new ShufflingStore(rows, compare: false);

        var results = await All(Keyset(store.Read, ["Id"], pageSize: 5));

        results[^1].IsFailure.ShouldBeTrue();
        results[^1].CurrentMessage.ShouldNotBeNull().ShouldContain("no value for key field 'Id'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task EveryPageKeepsTheCallersFilterAndNeverAsksForTheCache()
    {
        var store = new ShufflingStore(Rows(9));
        var callerFilter = new FilterExpression { Root = new FilterCondition { PropertyName = "Region", Operator = FilterOperators.ByName("Equal"), Value = 0 } };
        var source = new KeysetRecordSource(
            Schema, store.Read, new QueryCommand<Dictionary<string, object?>> { Filter = callerFilter }, ["Id"], includeKeys: false, pageSize: 2, "Orders", NullLogger.Instance);

        var ids = await Ids(source);

        ids.ShouldBe([0, 3, 6]);
        store.Commands.ShouldAllBe(command => !CachePolicy.IsEnabled(command));
    }

    private static KeysetRecordSource Keyset(
        Func<IDataCommand, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> read, IReadOnlyList<string> keys, int pageSize) =>
        new(Schema, read, new QueryCommand<Dictionary<string, object?>>(), keys, includeKeys: false, pageSize, "Orders", NullLogger.Instance);

    private static async Task<List<int>> Ids(IRecordSource<DataRecord> source) =>
        [.. (await All(source)).Select(record => { record.IsSuccess.ShouldBeTrue(record.CurrentMessage); return (int)record.Value["Id"]!; })];

    private static async Task<List<IGenericResult<DataRecord>>> All(IRecordSource<DataRecord> source)
    {
        var all = new List<IGenericResult<DataRecord>>();
        await foreach (var record in source.Read(TestContext.Current.CancellationToken))
        {
            all.Add(record);
            all.Count.ShouldBeLessThanOrEqualTo(RecordsBeforeTheReadIsRunaway, "the read did not stop");
        }

        return all;
    }

    private static List<Dictionary<string, object?>> Rows(int count) => [.. Enumerable.Range(0, count).Select(i => Row(region: i % 3, id: i))];

    private static Dictionary<string, object?> Row(int region, int id) => new() { ["Region"] = region, ["Id"] = id, ["Name"] = $"n{id}" };

    private static IDataField Field(string name, int ordinal) => Mock.Of<IDataField>(field => field.Name == name && field.Ordinal == ordinal);
}

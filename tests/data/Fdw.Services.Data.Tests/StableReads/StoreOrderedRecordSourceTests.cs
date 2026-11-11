using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;
using IDataField = Fdw.Data.Abstractions.IDataField;

namespace Fdw.Services.Data.Tests.StableReads;

/// <summary>
/// Tests for pages in a store's own order (FDW-4911): a store whose connection states that it keeps its order is paged with Skip/Take until
/// a short page, a store that answers its whole set to the first ask is read once, and a store that answers the same page again stops the read.
/// </summary>
public sealed class StoreOrderedRecordSourceTests
{
    // No test here reads more than a few dozen rows; a read past this did not stop, and the test fails rather than run out of memory.
    private const int RecordsBeforeTheReadIsRunaway = 10_000;

    private static readonly RecordSchema Schema = new([Mock.Of<IDataField>(field => field.Name == "Id")]);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoreThatKeepsItsOrderIsPagedUntilAShortPage()
    {
        var rows = Enumerable.Range(0, 12).Select(i => new Dictionary<string, object?> { ["Id"] = i }).ToList();
        var skips = new List<int>();
        Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>> Page(IDataCommand command, CancellationToken _)
        {
            var paging = ((IQueryCommand)command).Paging!;
            skips.Add(paging.Skip);
            return Task.FromResult<IGenericResult<IEnumerable<Dictionary<string, object?>>>>(
                GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows.Skip(paging.Skip).Take(paging.Take!.Value).ToList()));
        }

        var ids = await Ids(new StoreOrderedRecordSource(Schema, Page, new QueryCommand<Dictionary<string, object?>>(), pageSize: 5, "Orders"));

        ids.ShouldBe(Enumerable.Range(0, 12));
        skips.ShouldBe([0, 5, 10]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoreThatAnswersItsWholeSetToTheFirstAskIsReadOnce()
    {
        var rows = Enumerable.Range(0, 12).Select(i => new Dictionary<string, object?> { ["Id"] = i }).ToList();
        var reads = 0;
        Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>> Whole(IDataCommand command, CancellationToken _)
        {
            reads++;
            return Task.FromResult<IGenericResult<IEnumerable<Dictionary<string, object?>>>>(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
        }

        var ids = await Ids(new StoreOrderedRecordSource(Schema, Whole, new QueryCommand<Dictionary<string, object?>>(), pageSize: 5, "Orders"));

        ids.Count.ShouldBe(12);
        reads.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoreThatIgnoresSkipStopsTheReadWithItsCodeRatherThanRepeatingThePage()
    {
        var rows = Enumerable.Range(0, 12).Select(i => new Dictionary<string, object?> { ["Id"] = i }).ToList();
        var reads = 0;
        Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>> IgnoresSkip(IDataCommand command, CancellationToken _)
        {
            reads++;
            return Task.FromResult<IGenericResult<IEnumerable<Dictionary<string, object?>>>>(
                GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows.Take(((IQueryCommand)command).Paging!.Take!.Value).ToList()));
        }

        var results = await All(new StoreOrderedRecordSource(Schema, IgnoresSkip, new QueryCommand<Dictionary<string, object?>>(), pageSize: 5, "Orders"));

        results.Count(r => r.IsSuccess).ShouldBe(5);
        results[^1].IsFailure.ShouldBeTrue();
        results[^1].CurrentMessage.ShouldNotBeNull().ShouldContain("'Orders' answered the same page twice");
        reads.ShouldBe(2);
    }

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
}

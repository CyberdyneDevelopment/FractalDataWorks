using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.RowQuery;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests.RowQuery;

/// <summary>
/// Ordering and paging for sources that cannot express them.
/// </summary>
/// <remarks>
/// A file has no ORDER BY and no OFFSET. Before this, both were accepted and silently dropped — a
/// query for the top 10 by score against a CSV returned an arbitrary 10 rows in file order.
/// </remarks>
public sealed class RecordQueryEvaluatorOrderingPagingTests
{
    private static readonly IDataContainer Container =
        ContainerStub.Build("teams", ("team", false), ("wins", true), ("coach", true));

    private static IReadOnlyDictionary<string, object?> Row(string team, object? wins, string? coach = null) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        { ["team"] = team, ["wins"] = wins, ["coach"] = coach };

    private static IOrderingExpression By(string field, bool ascending = true) =>
        new OrderingExpression
        {
            OrderedFields = new IOrderedField[]
            {
                new OrderedField
                {
                    PropertyName = field,
                    Direction = ascending ? SortDirections.ByName("Ascending")! : SortDirections.ByName("Descending")!,
                },
            },
        };

    private static async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> Evaluate(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IOrderingExpression? ordering = null,
        IPagingExpression? paging = null)
    {
        var result = await RecordQueryEvaluator.Evaluate(
            rows, Container, filter: null, joins: [],
            loadJoinedRows: (_, _) => throw new InvalidOperationException("no join expected"),
            logger: NullLogger.Instance,
            ordering: ordering, paging: paging,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);

        result.IsSuccess.ShouldBeTrue();
        return result.Value!;
    }

    [Fact]
    public async Task WithoutAnOrderingTheFileOrderIsKept()
    {
        var rows = new[] { Row("Lions", 3), Row("Bears", 10), Row("Packers", 7) };
        (await Evaluate(rows)).Select(r => r["team"]).ShouldBe(new object?[] { "Lions", "Bears", "Packers" });
    }

    [Fact]
    public async Task AscendingSortsByTheField()
    {
        var rows = new[] { Row("Lions", 3), Row("Bears", 10), Row("Packers", 7) };
        (await Evaluate(rows, By("wins")))
            .Select(r => r["team"]).ShouldBe(new object?[] { "Lions", "Packers", "Bears" });
    }

    [Fact]
    public async Task DescendingReversesIt()
    {
        var rows = new[] { Row("Lions", 3), Row("Bears", 10), Row("Packers", 7) };
        (await Evaluate(rows, By("wins", ascending: false)))
            .Select(r => r["team"]).ShouldBe(new object?[] { "Bears", "Packers", "Lions" });
    }

    [Fact]
    public async Task NumbersSortNumericallyNotAsText()
    {
        // Why: as text "10" sorts before "3", which is the classic wrong answer.
        var rows = new[] { Row("Lions", 3), Row("Bears", 10), Row("Packers", 7) };
        (await Evaluate(rows, By("wins")))
            .Select(r => r["wins"]).ShouldBe(new object?[] { 3, 7, 10 });
    }

    [Fact]
    public async Task NullsSortFirstAscending()
    {
        // Matching SQL Server's default for ASC, so a file and a table agree.
        var rows = new[] { Row("Lions", 3), Row("Bears", null), Row("Packers", 7) };
        (await Evaluate(rows, By("wins")))
            .Select(r => r["team"]).ShouldBe(new object?[] { "Bears", "Lions", "Packers" });
    }

    [Fact]
    public async Task ASecondKeyBreaksTiesWithoutOverwritingTheFirst()
    {
        var ordering = new OrderingExpression
        {
            OrderedFields = new IOrderedField[]
            {
                new OrderedField { PropertyName = "wins", Direction = SortDirections.ByName("Descending")! },
                new OrderedField { PropertyName = "team", Direction = SortDirections.ByName("Ascending")! },
            },
        };

        var rows = new[] { Row("Vikings", 7), Row("Bears", 10), Row("Packers", 7) };
        (await Evaluate(rows, ordering))
            .Select(r => r["team"]).ShouldBe(new object?[] { "Bears", "Packers", "Vikings" });
    }

    [Fact]
    public async Task SkipDropsFromTheFront()
    {
        var rows = new[] { Row("Lions", 3), Row("Bears", 10), Row("Packers", 7) };
        (await Evaluate(rows, paging: new PagingExpression { Skip = 1 }))
            .Select(r => r["team"]).ShouldBe(new object?[] { "Bears", "Packers" });
    }

    [Fact]
    public async Task TakeLimitsTheCount()
    {
        var rows = new[] { Row("Lions", 3), Row("Bears", 10), Row("Packers", 7) };
        (await Evaluate(rows, paging: new PagingExpression { Skip = 0, Take = 2 })).Count.ShouldBe(2);
    }

    [Fact]
    public async Task OrderingIsAppliedBeforePaging()
    {
        // Why this is the test that matters: paging an unordered set returns an arbitrary window and
        // calls it page one. Top-2 by wins is Bears then Packers, never Lions.
        var rows = new[] { Row("Lions", 3), Row("Bears", 10), Row("Packers", 7) };
        (await Evaluate(rows, By("wins", ascending: false), new PagingExpression { Skip = 0, Take = 2 }))
            .Select(r => r["team"]).ShouldBe(new object?[] { "Bears", "Packers" });
    }

    [Fact]
    public async Task APageBeyondTheEndIsEmptyRatherThanAnError()
    {
        var rows = new[] { Row("Lions", 3) };
        (await Evaluate(rows, paging: new PagingExpression { Skip = 99, Take = 10 })).ShouldBeEmpty();
    }

    [Fact]
    public async Task ATakeWithNoValueMeansTheRemainder()
    {
        var rows = new[] { Row("Lions", 3), Row("Bears", 10), Row("Packers", 7) };
        (await Evaluate(rows, paging: new PagingExpression { Skip = 1 })).Count.ShouldBe(2);
    }
}

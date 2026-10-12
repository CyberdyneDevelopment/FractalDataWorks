using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// The route a database connection gives a command (FDW-1152): serial SQL up to the connection's MaxSerialInsert rows, the bulk
/// mode above it, and no reroute at all for a command where bulk does nothing.
/// </summary>
public sealed class BulkInsertRoutingTests
{
    private sealed record Row(int Id);

    private static BulkInsertCommand<Row> Rows(int count) => new(Enumerable.Range(1, count).Select(i => new Row(i)).ToList());

    [Theory]
    [InlineData(999, 1000, false)]
    [InlineData(1000, 1000, false)]
    [InlineData(1001, 1000, true)]
    [InlineData(1, 0, true)]
    [InlineData(0, 1000, false)]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ABulkInsertTakesTheBulkRouteOnlyAboveTheLimit(int rows, int maxSerialInsert, bool reroutes)
    {
        Rows(rows).ReroutesToBulk(maxSerialInsert).ShouldBe(reroutes);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALazySequenceIsReadOnceAndCountedBeforeTheRouteIsChosen()
    {
        var reads = 0;
        IEnumerable<Row> Lazy()
        {
            reads++;
            for (var i = 0; i < 5; i++)
                yield return new Row(i);
        }

        var command = new BulkInsertCommand<Row>(Lazy());

        command.ReroutesToBulk(4).ShouldBeTrue();
        command.ReroutesToBulk(5).ShouldBeFalse();
        command.Data.Count().ShouldBe(5);
        reads.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ACommandWhereBulkDoesNothingNeverReroutes()
    {
        new InsertCommand<Row>(new Row(1)).ShouldBeAssignableTo<IDatabaseConnectionCommand>().ShouldNotBeNull().ReroutesToBulk(0).ShouldBeFalse();
        new QueryCommand<Row>().ShouldBeAssignableTo<IDatabaseConnectionCommand>().ShouldNotBeNull().ReroutesToBulk(0).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TheRowsATranslatorReadsAreTheRowsTheRouteWasChosenOn()
    {
        var command = new BulkInsertCommand<Row>(Enumerable.Range(1, 3).Select(i => new Row(i)));

        command.ShouldBeAssignableTo<IDataCommandWithInput>().ShouldNotBeNull().InputData
            .ShouldNotBeNull()
            .ShouldBeAssignableTo<IReadOnlyCollection<Row>>().ShouldNotBeNull()
            .Count.ShouldBe(3);
    }
}

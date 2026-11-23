using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// The fields a connection's cursor builds its records over (FDW-6970): a read of one container gets the container's fields; a query that joins or stacks
/// containers gets its projected columns under their aliases, and a column without an alias is refused.
/// </summary>
public sealed class CursorFieldsTests
{
    [Fact]
    public void AReadOfOneContainerGetsTheContainersFields()
    {
        var fields = CursorFields.Of(Query([], null), Orders(), NullLogger.Instance);

        fields.IsSuccess.ShouldBeTrue(fields.CurrentMessage);
        fields.Value.ShouldNotBeNull().Select(field => field.Name).ShouldBe(["Id", "Total"]);
    }

    [Fact]
    public void ACommandThatIsNoQueryGetsTheContainersFields()
    {
        var fields = CursorFields.Of(new Mock<IDataCommand>().Object, Orders(), NullLogger.Instance);

        fields.IsSuccess.ShouldBeTrue(fields.CurrentMessage);
        fields.Value.ShouldNotBeNull().Select(field => field.Name).ShouldBe(["Id", "Total"]);
    }

    [Fact]
    public void AJoinedQueryGetsItsProjectedColumnsUnderTheirAliasesInOrder()
    {
        var fields = CursorFields.Of(
            Query([Join()], [Column("Id", "Id"), Column("Total", "Total"), Column("Id", "r_Id"), Column("Amount", "r_Amount")]), Orders(), NullLogger.Instance);

        fields.IsSuccess.ShouldBeTrue(fields.CurrentMessage);
        var read = fields.Value.ShouldNotBeNull();
        read.Select(field => field.Name).ShouldBe(["Id", "Total", "r_Id", "r_Amount"]);
        read.Select(field => field.Ordinal).ShouldBe([0, 1, 2, 3]);
    }

    [Fact]
    public void AJoinedQueryWithAColumnWithoutAnAliasIsRefusedSayingWhichColumn()
    {
        var fields = CursorFields.Of(Query([Join()], [Column("Id", "Id"), Column("Amount", null)]), Orders(), NullLogger.Instance);

        fields.IsFailure.ShouldBeTrue();
        fields.CurrentMessage.ShouldNotBeNull().ShouldContain("Amount");
    }

    [Fact]
    public void AJoinedQueryThatStatesNoProjectionIsRefused()
    {
        var fields = CursorFields.Of(Query([Join()], null), Orders(), NullLogger.Instance);

        fields.IsFailure.ShouldBeTrue();
        fields.CurrentMessage.ShouldNotBeNull().ShouldContain("orders");
    }

    private static IDataCommand Query(IReadOnlyList<IJoinExpression> joins, IReadOnlyList<ProjectionField>? columns)
    {
        var query = new Mock<IQueryCommand>();
        query.Setup(q => q.Joins).Returns(joins);
        query.Setup(q => q.Unions).Returns([]);
        query.Setup(q => q.Projection).Returns(columns is null ? null : new ProjectionExpression { Fields = columns });
        return query.Object;
    }

    private static JoinExpression Join() =>
        new() { TargetContainerName = "refunds", JoinType = "Inner", JoinConditions = [("Id", "OrderId")] };

    private static ProjectionField Column(string field, string? alias) => new() { PropertyName = field, Alias = alias };

    private static IDataContainer Orders()
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns([Field("Id", 0), Field("Total", 1)]);
        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("orders");
        container.Setup(c => c.Schema).Returns(schema.Object);
        return container.Object;
    }

    private static IField Field(string name, int ordinal)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        var data = field.As<IDataField>();
        data.Setup(f => f.Name).Returns(name);
        data.Setup(f => f.Ordinal).Returns(ordinal);
        return field.Object;
    }
}
